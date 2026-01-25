using System.Text.Json;
using bmadServer.ApiService.Models.Agents;
using Microsoft.Extensions.Logging;

namespace bmadServer.ApiService.Services.Workflows.Agents;

/// <summary>
/// In-memory agent messaging service with timeout and retry support
/// </summary>
public class AgentMessaging : IAgentMessaging
{
    private readonly IAgentRegistry _agentRegistry;
    private readonly ILogger<AgentMessaging> _logger;
    private readonly List<AgentMessage> _messageHistory = new();
    private readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(30);
    private readonly int _maxRetries = 1;

    public AgentMessaging(IAgentRegistry agentRegistry, ILogger<AgentMessaging> logger)
    {
        _agentRegistry = agentRegistry;
        _logger = logger;
    }

    public async Task<AgentResponse> RequestFromAgentAsync(
        string targetAgentId,
        AgentRequest request,
        JsonDocument? context,
        CancellationToken cancellationToken = default)
    {
        // Validate target agent exists
        var targetAgent = _agentRegistry.GetAgent(targetAgentId);
        if (targetAgent == null)
        {
            _logger.LogWarning("Target agent {TargetAgentId} not found", targetAgentId);
            return new AgentResponse
            {
                Success = false,
                ErrorMessage = $"Target agent '{targetAgentId}' not found"
            };
        }

        // Create message
        var message = new AgentMessage
        {
            SourceAgent = request.SourceAgentId,
            TargetAgent = targetAgentId,
            MessageType = "request",
            Content = request.Payload.RootElement.GetRawText(),
            WorkflowInstanceId = request.WorkflowInstanceId
        };

        // Log the message
        _messageHistory.Add(message);
        _logger.LogInformation(
            "Agent message sent: {MessageId} from {SourceAgent} to {TargetAgent}",
            message.MessageId,
            message.SourceAgent,
            message.TargetAgent);

        // Attempt request with retry
        var attempt = 0;
        while (attempt <= _maxRetries)
        {
            try
            {
                using var cts = new CancellationTokenSource(_defaultTimeout);
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, cancellationToken);

                // Simulate agent processing (in real implementation, this would call the actual agent)
                var response = await ProcessAgentRequestAsync(targetAgent, request, context, linked.Token);

                // Log response message
                var responseMessage = new AgentMessage
                {
                    SourceAgent = targetAgentId,
                    TargetAgent = request.SourceAgentId,
                    MessageType = "response",
                    Content = response.Response?.RootElement.GetRawText() ?? "{}",
                    WorkflowInstanceId = message.WorkflowInstanceId
                };
                _messageHistory.Add(responseMessage);

                _logger.LogInformation(
                    "Agent response received: {MessageId} from {SourceAgent}",
                    responseMessage.MessageId,
                    responseMessage.SourceAgent);

                return response;
            }
            catch (OperationCanceledException) when (attempt < _maxRetries)
            {
                attempt++;
                _logger.LogWarning(
                    "Request to agent {TargetAgentId} timed out. Retry attempt {Attempt} of {MaxRetries}",
                    targetAgentId,
                    attempt,
                    _maxRetries);
            }
            catch (OperationCanceledException)
            {
                _logger.LogError(
                    "Request to agent {TargetAgentId} timed out after {Attempts} attempts",
                    targetAgentId,
                    attempt + 1);

                return new AgentResponse
                {
                    Success = false,
                    ErrorMessage = $"Request timed out after {attempt + 1} attempts",
                    TimedOut = true
                };
            }
        }

        return new AgentResponse
        {
            Success = false,
            ErrorMessage = "Unexpected error in request handling"
        };
    }

    public Task<IEnumerable<AgentMessage>> GetMessageHistoryAsync(
        Guid workflowInstanceId,
        CancellationToken cancellationToken = default)
    {
        var messages = _messageHistory
            .Where(m => m.WorkflowInstanceId == workflowInstanceId)
            .OrderBy(m => m.Timestamp)
            .AsEnumerable();

        return Task.FromResult(messages);
    }

    private async Task<AgentResponse> ProcessAgentRequestAsync(
        AgentDefinition agent,
        AgentRequest request,
        JsonDocument? context,
        CancellationToken cancellationToken)
    {
        // Simulate async processing
        await Task.Delay(100, cancellationToken);

        // In real implementation, this would invoke the actual agent handler
        // For now, return a mock successful response
        var responseData = JsonDocument.Parse(JsonSerializer.Serialize(new
        {
            message = "Request processed successfully",
            agentId = agent.AgentId,
            agentName = agent.Name,
            requestType = request.RequestType,
            timestamp = DateTime.UtcNow
        }));

        return new AgentResponse
        {
            Success = true,
            Response = responseData
        };
    }
}
