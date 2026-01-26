using bmadServer.ApiService.Data;
using bmadServer.ApiService.Models.Workflows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace bmadServer.ApiService.Services.Workflows;

/// <summary>
/// Implementation of agent handoff tracking service.
/// Records all agent transitions for audit trail and provides query methods for history retrieval.
/// </summary>
public class AgentHandoffService : IAgentHandoffService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AgentHandoffService> _logger;

    public AgentHandoffService(
        ApplicationDbContext context,
        ILogger<AgentHandoffService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task RecordHandoffAsync(
        Guid workflowInstanceId,
        string fromAgentId,
        string toAgentId,
        string stepId,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        if (workflowInstanceId == Guid.Empty)
            throw new ArgumentException("Workflow instance ID cannot be empty", nameof(workflowInstanceId));

        if (string.IsNullOrWhiteSpace(fromAgentId))
            throw new ArgumentException("From agent ID cannot be null or empty", nameof(fromAgentId));

        if (string.IsNullOrWhiteSpace(toAgentId))
            throw new ArgumentException("To agent ID cannot be null or empty", nameof(toAgentId));

        if (string.IsNullOrWhiteSpace(stepId))
            throw new ArgumentException("Step ID cannot be null or empty", nameof(stepId));

        try
        {
            var handoff = new AgentHandoff
            {
                Id = Guid.NewGuid(),
                WorkflowInstanceId = workflowInstanceId,
                FromAgentId = fromAgentId,
                ToAgentId = toAgentId,
                Timestamp = DateTime.UtcNow,
                WorkflowStepId = stepId,
                Reason = reason
            };

            _context.AgentHandoffs.Add(handoff);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Recorded agent handoff: {FromAgentId} -> {ToAgentId} for workflow {WorkflowId} step {StepId}",
                fromAgentId, toAgentId, workflowInstanceId, stepId);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Handoff recording cancelled for workflow {WorkflowId}", workflowInstanceId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error recording handoff for workflow {WorkflowId}: {Error}",
                workflowInstanceId, ex.Message);
            // Non-critical operation - log but don't rethrow
            // Workflow execution should continue even if handoff tracking fails
        }
    }

    public async Task<List<AgentHandoff>> GetHandoffsAsync(
        Guid workflowInstanceId,
        CancellationToken cancellationToken = default)
    {
        if (workflowInstanceId == Guid.Empty)
            throw new ArgumentException("Workflow instance ID cannot be empty", nameof(workflowInstanceId));

        return await _context.AgentHandoffs
            .Where(h => h.WorkflowInstanceId == workflowInstanceId)
            .OrderBy(h => h.Timestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<AgentHandoff>> GetRecentHandoffsAsync(
        Guid workflowInstanceId,
        int limit = 5,
        CancellationToken cancellationToken = default)
    {
        if (workflowInstanceId == Guid.Empty)
            throw new ArgumentException("Workflow instance ID cannot be empty", nameof(workflowInstanceId));

        if (limit <= 0)
            throw new ArgumentException("Limit must be greater than 0", nameof(limit));

        return await _context.AgentHandoffs
            .Where(h => h.WorkflowInstanceId == workflowInstanceId)
            .OrderByDescending(h => h.Timestamp)
            .Take(limit)
            .OrderBy(h => h.Timestamp)
            .ToListAsync(cancellationToken);
    }
}
