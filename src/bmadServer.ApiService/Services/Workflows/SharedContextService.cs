using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using bmadServer.ApiService.Data;
using bmadServer.ApiService.Models.Workflows;

namespace bmadServer.ApiService.Services.Workflows;

public class SharedContextService : ISharedContextService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<SharedContextService> _logger;

    public SharedContextService(ApplicationDbContext dbContext, ILogger<SharedContextService> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<SharedContext?> GetContextAsync(Guid workflowInstanceId, CancellationToken cancellationToken = default)
    {
        if (workflowInstanceId == Guid.Empty)
        {
            _logger.LogWarning("GetContextAsync called with empty workflow instance ID");
            return null;
        }

        var workflow = await _dbContext.WorkflowInstances
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == workflowInstanceId, cancellationToken);

        if (workflow == null)
        {
            _logger.LogInformation("Workflow instance {WorkflowInstanceId} not found", workflowInstanceId);
            return null;
        }

        if (workflow.SharedContextJson == null)
        {
            _logger.LogInformation("Workflow instance {WorkflowInstanceId} has no context yet", workflowInstanceId);
            return null;
        }

        try
        {
            var context = JsonSerializer.Deserialize<SharedContext>(workflow.SharedContextJson.RootElement.GetRawText());
            if (context == null)
            {
                _logger.LogWarning("Failed to deserialize SharedContext for workflow {WorkflowInstanceId}", workflowInstanceId);
                return null;
            }

            return context;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error deserializing SharedContext for workflow {WorkflowInstanceId}", workflowInstanceId);
            return null;
        }
    }

    public async Task<JsonDocument?> GetStepOutputAsync(Guid workflowInstanceId, string stepId, CancellationToken cancellationToken = default)
    {
        if (workflowInstanceId == Guid.Empty)
        {
            _logger.LogWarning("GetStepOutputAsync called with empty workflow instance ID");
            return null;
        }

        if (string.IsNullOrWhiteSpace(stepId))
        {
            _logger.LogWarning("GetStepOutputAsync called with empty step ID");
            return null;
        }

        var context = await GetContextAsync(workflowInstanceId, cancellationToken);
        if (context == null)
        {
            return null;
        }

        if (!context.StepOutputs.TryGetValue(stepId, out var output))
        {
            _logger.LogInformation("Step {StepId} output not found in context for workflow {WorkflowInstanceId}", stepId, workflowInstanceId);
            return null;
        }

        return output;
    }

    public async Task AddStepOutputAsync(Guid workflowInstanceId, string stepId, JsonDocument output, CancellationToken cancellationToken = default)
    {
        if (workflowInstanceId == Guid.Empty)
            throw new ArgumentException("Workflow instance ID cannot be empty", nameof(workflowInstanceId));

        if (string.IsNullOrWhiteSpace(stepId))
            throw new ArgumentException("Step ID cannot be null or empty", nameof(stepId));

        if (output == null)
            throw new ArgumentNullException(nameof(output));

        const int maxRetries = 3;
        int attempt = 0;

        while (attempt < maxRetries)
        {
            attempt++;
            
            var workflow = await _dbContext.WorkflowInstances
                .FirstOrDefaultAsync(w => w.Id == workflowInstanceId, cancellationToken);

            if (workflow == null)
            {
                throw new InvalidOperationException($"Workflow instance {workflowInstanceId} not found");
            }

            var context = await GetContextAsync(workflowInstanceId, cancellationToken) ?? new SharedContext();
            var originalVersion = context.Version;

            context.StepOutputs[stepId] = output;
            context.Version++;
            context.LastModifiedAt = DateTime.UtcNow;
            context.LastModifiedBy = "system";

            try
            {
                workflow.SharedContextJson = JsonDocument.Parse(JsonSerializer.Serialize(context));
                await _dbContext.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Added step output for step {StepId} to workflow {WorkflowInstanceId} (version {Version})",
                    stepId, workflowInstanceId, context.Version);
                return;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(
                    "Concurrency conflict on attempt {Attempt} for workflow {WorkflowInstanceId}, retrying...",
                    attempt, workflowInstanceId);

                if (attempt >= maxRetries)
                {
                    _logger.LogError(ex, 
                        "Max retries exceeded for AddStepOutputAsync on workflow {WorkflowInstanceId}", 
                        workflowInstanceId);
                    throw;
                }

                // Detach the entity and retry
                _dbContext.Entry(workflow).State = EntityState.Detached;
                await Task.Delay(100 * attempt, cancellationToken); // Exponential backoff
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while adding step output for workflow {WorkflowInstanceId}", workflowInstanceId);
                throw;
            }
        }
    }

    public async Task<bool> UpdateContextAsync(Guid workflowInstanceId, SharedContext context, CancellationToken cancellationToken = default)
    {
        if (workflowInstanceId == Guid.Empty)
        {
            _logger.LogWarning("UpdateContextAsync called with empty workflow instance ID");
            return false;
        }

        if (context == null)
            throw new ArgumentNullException(nameof(context));

        var workflow = await _dbContext.WorkflowInstances
            .FirstOrDefaultAsync(w => w.Id == workflowInstanceId, cancellationToken);

        if (workflow == null)
        {
            _logger.LogWarning("Workflow instance {WorkflowInstanceId} not found for context update", workflowInstanceId);
            return false;
        }

        var currentContext = await GetContextAsync(workflowInstanceId, cancellationToken);
        if (currentContext == null)
        {
            _logger.LogWarning("Cannot update context for workflow {WorkflowInstanceId} - context does not exist", workflowInstanceId);
            return false;
        }

        if (currentContext.Version != context.Version)
        {
            _logger.LogWarning(
                "Version conflict for workflow {WorkflowInstanceId}. Expected version {Expected}, got {Actual}",
                workflowInstanceId, currentContext.Version, context.Version);
            return false;
        }

        context.Version++;
        context.LastModifiedAt = DateTime.UtcNow;

        try
        {
            workflow.SharedContextJson = JsonDocument.Parse(JsonSerializer.Serialize(context));
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Updated SharedContext for workflow {WorkflowInstanceId} to version {Version}",
                workflowInstanceId, context.Version);
            return true;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error while updating context for workflow {WorkflowInstanceId}", workflowInstanceId);
            throw;
        }
    }
}
