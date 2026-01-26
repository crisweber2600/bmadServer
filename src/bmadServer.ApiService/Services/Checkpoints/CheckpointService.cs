using bmadServer.ApiService.Data;
using bmadServer.ApiService.DTOs.Checkpoints;
using bmadServer.ApiService.Models.Workflows;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace bmadServer.ApiService.Services.Checkpoints;

public class CheckpointService : ICheckpointService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CheckpointService> _logger;

    public CheckpointService(
        ApplicationDbContext context,
        ILogger<CheckpointService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<WorkflowCheckpoint> CreateCheckpointAsync(
        Guid workflowId,
        string stepId,
        CheckpointType type,
        Guid triggeredBy,
        CancellationToken cancellationToken = default)
    {
        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // Load workflow with explicit lock for consistency
            var workflow = await _context.WorkflowInstances
                .FirstOrDefaultAsync(w => w.Id == workflowId, cancellationToken);

            if (workflow == null)
            {
                throw new InvalidOperationException($"Workflow {workflowId} not found");
            }

            // Get current version (latest checkpoint version + 1) within transaction for consistency
            var latestCheckpoint = await _context.WorkflowCheckpoints
                .Where(c => c.WorkflowId == workflowId)
                .OrderByDescending(c => c.Version)
                .FirstOrDefaultAsync(cancellationToken);
            
            var version = (latestCheckpoint?.Version ?? 0) + 1;

            // Capture current state snapshot
            var stateSnapshot = JsonDocument.Parse(JsonSerializer.Serialize(new
            {
                workflowId = workflow.Id,
                workflowDefinitionId = workflow.WorkflowDefinitionId,
                currentStep = workflow.CurrentStep,
                status = workflow.Status.ToString(),
                stepData = workflow.StepData,
                context = workflow.Context,
                createdAt = workflow.CreatedAt,
                updatedAt = workflow.UpdatedAt
            }));

            var checkpoint = new WorkflowCheckpoint
            {
                Id = Guid.NewGuid(),
                WorkflowId = workflowId,
                StepId = stepId,
                CheckpointType = type,
                StateSnapshot = stateSnapshot,
                Version = version,
                CreatedAt = DateTime.UtcNow,
                TriggeredBy = triggeredBy
            };

            _context.WorkflowCheckpoints.Add(checkpoint);
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Created checkpoint {CheckpointId} for workflow {WorkflowId} at step {StepId} (version {Version})",
                checkpoint.Id, workflowId, stepId, version);

            return checkpoint;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Failed to create checkpoint for workflow {WorkflowId} at step {StepId}",
                workflowId, stepId);
            throw;
        }
    }

    public async Task RestoreCheckpointAsync(
        Guid workflowId,
        Guid checkpointId,
        CancellationToken cancellationToken = default)
    {
        var checkpoint = await _context.WorkflowCheckpoints
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == checkpointId && c.WorkflowId == workflowId, cancellationToken);

        if (checkpoint == null)
        {
            throw new InvalidOperationException($"Checkpoint {checkpointId} not found for workflow {workflowId}");
        }

        var workflow = await _context.WorkflowInstances
            .FirstOrDefaultAsync(w => w.Id == workflowId, cancellationToken);

        if (workflow == null)
        {
            throw new InvalidOperationException($"Workflow {workflowId} not found");
        }

        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // Restore state from checkpoint
            var snapshot = checkpoint.StateSnapshot.RootElement;
            workflow.CurrentStep = snapshot.GetProperty("currentStep").GetInt32();
            workflow.Status = Enum.Parse<WorkflowStatus>(snapshot.GetProperty("status").GetString()!);
            
            if (snapshot.TryGetProperty("stepData", out var stepData) && stepData.ValueKind != JsonValueKind.Null)
            {
                workflow.StepData = JsonDocument.Parse(stepData.GetRawText());
            }
            
            if (snapshot.TryGetProperty("context", out var context) && context.ValueKind != JsonValueKind.Null)
            {
                workflow.Context = JsonDocument.Parse(context.GetRawText());
            }
            
            workflow.UpdatedAt = DateTime.UtcNow;

            // Reset any failed inputs for retry (preserve for AC#4 requirement)
            try
            {
                var failedInputs = await _context.QueuedInputs
                    .Where(q => q.WorkflowId == workflowId && q.Status == InputStatus.Failed)
                    .ToListAsync(cancellationToken);

                foreach (var input in failedInputs)
                {
                    input.Status = InputStatus.Queued;
                }

                if (failedInputs.Any())
                {
                    _logger.LogInformation(
                        "Reset {FailedCount} failed inputs for retry after checkpoint restore", 
                        failedInputs.Count);
                }
            }
            catch (Exception ex)
            {
                // Log but don't fail if we can't process queued inputs
                _logger.LogWarning(ex, 
                    "Could not process queued inputs during checkpoint restore (may not exist in this workflow)");
            }

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Restored workflow {WorkflowId} to checkpoint {CheckpointId} (version {Version})",
                workflowId, checkpointId, checkpoint.Version);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Failed to restore checkpoint {CheckpointId} for workflow {WorkflowId}", 
                checkpointId, workflowId);
            throw;
        }
    }

    public async Task<PagedResult<CheckpointResponse>> GetCheckpointsAsync(
        Guid workflowId,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _context.WorkflowCheckpoints
            .AsNoTracking()
            .Where(c => c.WorkflowId == workflowId)
            .OrderByDescending(c => c.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CheckpointResponse(
                c.Id,
                c.WorkflowId,
                c.StepId,
                c.CheckpointType.ToString(),
                c.Version,
                c.CreatedAt,
                c.TriggeredBy,
                c.Metadata
            ))
            .ToListAsync(cancellationToken);

        return new PagedResult<CheckpointResponse>(
            items,
            page,
            pageSize,
            totalCount,
            totalPages
        );
    }

    public async Task<WorkflowCheckpoint?> GetLatestCheckpointAsync(
        Guid workflowId,
        CancellationToken cancellationToken = default)
    {
        return await _context.WorkflowCheckpoints
            .AsNoTracking()
            .Where(c => c.WorkflowId == workflowId)
            .OrderByDescending(c => c.Version)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<WorkflowCheckpoint?> GetCheckpointByIdAsync(
        Guid checkpointId,
        CancellationToken cancellationToken = default)
    {
        return await _context.WorkflowCheckpoints
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == checkpointId, cancellationToken);
    }
}
