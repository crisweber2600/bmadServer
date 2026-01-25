using bmadServer.ApiService.Services.Workflows.Agents;

namespace bmadServer.ApiService.Services.Workflows;

/// <summary>
/// Service for routing workflow steps to appropriate agent handlers
/// </summary>
public class AgentRouter : IAgentRouter
{
    private readonly Dictionary<string, IAgentHandler> _handlers = new();
    private readonly ILogger<AgentRouter> _logger;

    public AgentRouter(ILogger<AgentRouter> logger)
    {
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
}
