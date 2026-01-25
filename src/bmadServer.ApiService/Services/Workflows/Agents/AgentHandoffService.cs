using bmadServer.ApiService.DTOs;
using bmadServer.ApiService.Models.Agents;
using Microsoft.Extensions.Logging;

namespace bmadServer.ApiService.Services.Workflows.Agents;

/// <summary>
/// Service for managing agent handoffs with audit logging
/// </summary>
public class AgentHandoffService : IAgentHandoffService
{
    private readonly IAgentRegistry _agentRegistry;
    private readonly ILogger<AgentHandoffService> _logger;
    private readonly Dictionary<Guid, List<AgentHandoff>> _handoffHistory = new();
    private readonly Dictionary<Guid, string> _currentAgents = new();
    private readonly SemaphoreSlim _lock = new(1, 1);

    public AgentHandoffService(IAgentRegistry agentRegistry, ILogger<AgentHandoffService> logger)
    {
        _agentRegistry = agentRegistry;
        _logger = logger;
    }

    public async Task<AgentHandoff> RecordHandoffAsync(
        string fromAgent,
        string toAgent,
        string workflowStep,
        string reason,
        Guid workflowInstanceId,
        CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var handoff = new AgentHandoff
            {
                FromAgent = fromAgent,
                ToAgent = toAgent,
                WorkflowStep = workflowStep,
                Reason = reason,
                WorkflowInstanceId = workflowInstanceId
            };

            if (!_handoffHistory.ContainsKey(workflowInstanceId))
            {
                _handoffHistory[workflowInstanceId] = new List<AgentHandoff>();
            }

            _handoffHistory[workflowInstanceId].Add(handoff);
            _currentAgents[workflowInstanceId] = toAgent;

            _logger.LogInformation(
                "Agent handoff recorded: {HandoffId} from {FromAgent} to {ToAgent} at step {WorkflowStep} in workflow {WorkflowInstanceId}",
                handoff.HandoffId,
                fromAgent,
                toAgent,
                workflowStep,
                workflowInstanceId);

            return handoff;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<IEnumerable<AgentHandoff>> GetHandoffHistoryAsync(
        Guid workflowInstanceId,
        CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_handoffHistory.TryGetValue(workflowInstanceId, out var history))
            {
                return history.OrderBy(h => h.Timestamp).ToList();
            }

            return Enumerable.Empty<AgentHandoff>();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<string?> GetCurrentAgentAsync(
        Guid workflowInstanceId,
        CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            _currentAgents.TryGetValue(workflowInstanceId, out var agent);
            return agent;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<AgentHandoffIndicator?> CreateHandoffIndicatorAsync(
        string agentId,
        string currentStep,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask; // Async for interface consistency

        var agent = _agentRegistry.GetAgent(agentId);
        if (agent == null)
        {
            _logger.LogWarning("Agent {AgentId} not found for handoff indicator", agentId);
            return null;
        }

        return new AgentHandoffIndicator
        {
            AgentName = agent.Name,
            Description = agent.Description,
            Capabilities = agent.Capabilities,
            CurrentStep = currentStep,
            Message = $"Handing off to {agent.Name}..."
        };
    }
}
