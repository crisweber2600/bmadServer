using bmadServer.ApiService.Services.Workflows.Agents;

namespace bmadServer.ApiService.Services.Workflows;

/// <summary>
/// Service for routing workflow steps to appropriate agent handlers with model preference support
/// </summary>
public class AgentRouter : IAgentRouter
{
    private readonly Dictionary<string, IAgentHandler> _handlers = new();
    private readonly IAgentRegistry _agentRegistry;
    private readonly ILogger<AgentRouter> _logger;
    private string? _modelOverride;

    public AgentRouter(IAgentRegistry agentRegistry, ILogger<AgentRouter> logger)
    {
        _agentRegistry = agentRegistry;
        _logger = logger;
    }

    /// <inheritdoc />
    public IAgentHandler? GetHandler(string agentId)
    {
        if (string.IsNullOrWhiteSpace(agentId))
        {
            _logger.LogWarning("Attempted to get handler with null or empty agentId");
            return null;
        }

        if (_handlers.TryGetValue(agentId, out var handler))
        {
            _logger.LogDebug("Found handler for agent {AgentId}", agentId);
            return handler;
        }

        _logger.LogWarning("No handler registered for agent {AgentId}", agentId);
        return null;
    }

    /// <inheritdoc />
    public void RegisterHandler(string agentId, IAgentHandler handler)
    {
        if (string.IsNullOrWhiteSpace(agentId))
        {
            throw new ArgumentException("Agent ID cannot be null or empty", nameof(agentId));
        }

        if (handler == null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        _handlers[agentId] = handler;
        _logger.LogInformation("Registered handler for agent {AgentId}", agentId);
    }

    /// <summary>
    /// Get the model preference for an agent, respecting any configured override
    /// </summary>
    /// <param name="agentId">The agent ID</param>
    /// <returns>The model preference, or null if not configured</returns>
    public string? GetModelPreference(string agentId)
    {
        if (!string.IsNullOrEmpty(_modelOverride))
        {
            _logger.LogDebug("Using model override {ModelOverride} for agent {AgentId}", _modelOverride, agentId);
            return _modelOverride;
        }

        var agent = _agentRegistry.GetAgent(agentId);
        if (agent?.ModelPreference != null)
        {
            _logger.LogDebug("Using model preference {ModelPreference} for agent {AgentId}", agent.ModelPreference, agentId);
            return agent.ModelPreference;
        }

        _logger.LogDebug("No model preference configured for agent {AgentId}", agentId);
        return null;
    }

    /// <summary>
    /// Set a global model override for all agents (for cost/quality tradeoffs)
    /// </summary>
    /// <param name="modelName">The model to use for all agents, or null to disable override</param>
    public void SetModelOverride(string? modelName)
    {
        _modelOverride = modelName;
        _logger.LogInformation("Set model override to {ModelOverride}", modelName ?? "none");
    }
}
