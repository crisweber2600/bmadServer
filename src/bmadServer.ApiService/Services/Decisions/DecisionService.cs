using bmadServer.ApiService.Data;
using bmadServer.ApiService.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace bmadServer.ApiService.Services.Decisions;

/// <summary>
/// Service implementation for managing workflow decisions
/// </summary>
public class DecisionService : IDecisionService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DecisionService> _logger;

    public DecisionService(ApplicationDbContext context, ILogger<DecisionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<Decision> CreateDecisionAsync(Decision decision, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate that the workflow instance exists
            var workflowExists = await _context.WorkflowInstances
                .AnyAsync(w => w.Id == decision.WorkflowInstanceId, cancellationToken);

            if (!workflowExists)
            {
                throw new InvalidOperationException($"Workflow instance {decision.WorkflowInstanceId} does not exist");
            }

            _context.Decisions.Add(decision);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Decision {DecisionId} created for workflow {WorkflowInstanceId}, step {StepId}, type {DecisionType}",
                decision.Id,
                decision.WorkflowInstanceId,
                decision.StepId,
                decision.DecisionType
            );

            return decision;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Failed to create decision for workflow {WorkflowInstanceId}", 
                decision.WorkflowInstanceId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<List<Decision>> GetDecisionsByWorkflowInstanceAsync(
        Guid workflowInstanceId, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var decisions = await _context.Decisions
                .Where(d => d.WorkflowInstanceId == workflowInstanceId)
                .OrderBy(d => d.DecidedAt)
                .ToListAsync(cancellationToken);

            _logger.LogInformation(
                "Retrieved {Count} decisions for workflow {WorkflowInstanceId}",
                decisions.Count,
                workflowInstanceId
            );

            return decisions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Failed to retrieve decisions for workflow {WorkflowInstanceId}", 
                workflowInstanceId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Decision?> GetDecisionByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var decision = await _context.Decisions
                .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

            if (decision != null)
            {
                _logger.LogInformation("Retrieved decision {DecisionId}", id);
            }
            else
            {
                _logger.LogWarning("Decision {DecisionId} not found", id);
            }

            return decision;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve decision {DecisionId}", id);
            throw;
        }
    }
}
