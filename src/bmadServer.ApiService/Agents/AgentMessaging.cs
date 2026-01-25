using Microsoft.Extensions.Logging;

namespace bmadServer.ApiService.Agents;

/// <summary>
/// Implementation of agent-to-agent messaging with timeout and retry logic.
/// </summary>
public class AgentMessaging : IAgentMessaging
{
    private readonly IAgentRegistry _agentRegistry;
    private readonly ILogger<AgentMessaging> _logger;
    private const int TimeoutSeconds = 30;
    private const int MaxRetries = 1;

    public AgentMessaging(IAgentRegistry agentRegistry, ILogger<AgentMessaging> logger)
    {
        _agentRegistry = agentRegistry;
        _logger = logger;
    }

    /// <summary>
    /// Sends a request from one agent to another with timeout and retry logic.
    /// </summary>
    public async Task<AgentResponse> RequestFromAgentAsync(
        string targetAgentId,
        AgentRequest request,
        Dictionary<string, object> context,
        CancellationToken cancellationToken = default)
    {
        // Validate target agent exists
        var targetAgent = _agentRegistry.GetAgent(targetAgentId);
        if (targetAgent == null)
        {
            _logger.LogError("Target agent {TargetAgentId} not found in registry", targetAgentId);
            return new AgentResponse
            {
                Success = false,
                Error = $"Target agent '{targetAgentId}' not found",
                RespondingAgentId = targetAgentId,
                Timestamp = DateTime.UtcNow
            };
        }

        // Log the request
        var messageId = Guid.NewGuid().ToString();
        _logger.LogInformation(
            "Agent request initiated: MessageId={MessageId}, Source={SourceAgent}, Target={TargetAgent}, RequestType={RequestType}",
            messageId, request.SourceAgentId, targetAgentId, request.RequestType);

        // Execute with timeout and retry
        var retryCount = 0;
        while (retryCount <= MaxRetries)
        {
            try
            {
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(TimeoutSeconds));
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

                var response = await ProcessAgentRequestAsync(targetAgent, request, context, linkedCts.Token);

                // Log successful response
                _logger.LogInformation(
                    "Agent request completed successfully: MessageId={MessageId}, Source={SourceAgent}, Target={TargetAgent}, Attempt={Attempt}",
                    messageId, request.SourceAgentId, targetAgentId, retryCount + 1);

                return response;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // External cancellation - don't retry
                _logger.LogWarning(
                    "Agent request cancelled externally: MessageId={MessageId}, Source={SourceAgent}, Target={TargetAgent}",
                    messageId, request.SourceAgentId, targetAgentId);
                
                return new AgentResponse
                {
                    Success = false,
                    Error = "Request was cancelled",
                    RespondingAgentId = targetAgentId,
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (OperationCanceledException)
            {
                // Timeout occurred
                retryCount++;
                
                if (retryCount > MaxRetries)
                {
                    _logger.LogError(
                        "Agent request timed out after {Retries} attempts: MessageId={MessageId}, Source={SourceAgent}, Target={TargetAgent}, TimeoutSeconds={TimeoutSeconds}",
                        MaxRetries + 1, messageId, request.SourceAgentId, targetAgentId, TimeoutSeconds);

                    return new AgentResponse
                    {
                        Success = false,
                        Error = $"Request timed out after {MaxRetries + 1} attempts ({TimeoutSeconds} seconds each)",
                        RespondingAgentId = targetAgentId,
                        Timestamp = DateTime.UtcNow
                    };
                }

                _logger.LogWarning(
                    "Agent request timed out, retrying: MessageId={MessageId}, Source={SourceAgent}, Target={TargetAgent}, Attempt={Attempt}, TimeoutSeconds={TimeoutSeconds}",
                    messageId, request.SourceAgentId, targetAgentId, retryCount, TimeoutSeconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Agent request failed with exception: MessageId={MessageId}, Source={SourceAgent}, Target={TargetAgent}",
                    messageId, request.SourceAgentId, targetAgentId);

                return new AgentResponse
                {
                    Success = false,
                    Error = $"Request failed: {ex.Message}",
                    RespondingAgentId = targetAgentId,
                    Timestamp = DateTime.UtcNow
                };
            }
        }

        // Should never reach here, but just in case
        return new AgentResponse
        {
            Success = false,
            Error = "Unexpected error in request processing",
            RespondingAgentId = targetAgentId,
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Processes the actual agent request. 
    /// In MVP, this is a stub that will be replaced with actual AI model invocation.
    /// </summary>
    private async Task<AgentResponse> ProcessAgentRequestAsync(
        AgentDefinition targetAgent,
        AgentRequest request,
        Dictionary<string, object> context,
        CancellationToken cancellationToken)
    {
        // MVP: Simulate agent processing
        // In Phase 2, this will invoke the actual AI model with the agent's system prompt
        await Task.Delay(100, cancellationToken); // Simulate processing time

        _logger.LogInformation(
            "Processing request for agent {AgentId} with request type {RequestType}",
            targetAgent.AgentId, request.RequestType);

        return new AgentResponse
        {
            Success = true,
            Data = new
            {
                agentId = targetAgent.AgentId,
                agentName = targetAgent.Name,
                requestType = request.RequestType,
                message = $"Agent {targetAgent.Name} processed request of type '{request.RequestType}'"
            },
            RespondingAgentId = targetAgent.AgentId,
            Timestamp = DateTime.UtcNow
        };
    }
}
