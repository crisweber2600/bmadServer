using System.Collections.Concurrent;

namespace bmadServer.ApiService.Services.Workflows.Agents;

/// <summary>
/// Tracks agent handoffs for workflow transparency
/// </summary>
public class AgentHandoffTracker : IAgentHandoffTracker
{
    private readonly ILogger<AgentHandoffTracker> _logger;
    private readonly ConcurrentDictionary<Guid, List<AgentHandoff>> _handoffHistory;

    public AgentHandoffTracker(ILogger<AgentHandoffTracker> logger)
    {
        _logger = logger;
        _handoffHistory = new ConcurrentDictionary<Guid, List<AgentHandoff>>();
    }

    /// <inheritdoc />
    public void RecordHandoff(AgentHandoff handoff)
    {
        if (handoff == null)
            throw new ArgumentNullException(nameof(handoff));

        _logger.LogInformation(
            "Agent handoff: {FromAgent} -> {ToAgent} for workflow {WorkflowInstanceId}, step: {WorkflowStep}",
            handoff.FromAgent,
            handoff.ToAgent,
            handoff.WorkflowInstanceId,
            handoff.WorkflowStep);

        _handoffHistory.AddOrUpdate(
            handoff.WorkflowInstanceId,
            _ => new List<AgentHandoff> { handoff },
            (_, existing) =>
            {
                existing.Add(handoff);
                return existing;
            });
    }

    /// <inheritdoc />
    public IReadOnlyList<AgentHandoff> GetHandoffHistory(Guid workflowInstanceId)
    {
        if (_handoffHistory.TryGetValue(workflowInstanceId, out var history))
        {
            return history.OrderBy(h => h.Timestamp).ToList().AsReadOnly();
        }

        return Array.Empty<AgentHandoff>();
    }

    /// <inheritdoc />
    public string? GetCurrentAgent(Guid workflowInstanceId)
    {
        var history = GetHandoffHistory(workflowInstanceId);
        return history.Count > 0 ? history.Last().ToAgent : null;
    }
}
