using System.Collections.Concurrent;

namespace bmadServer.ApiService.Services.Workflows.Agents;

/// <summary>
/// Service for agent-to-agent messaging
/// </summary>
public class AgentMessaging : IAgentMessaging
{
    private readonly ILogger<AgentMessaging> _logger;
    private readonly IAgentRegistry _agentRegistry;
    private readonly ConcurrentDictionary<Guid, List<AgentMessage>> _messageHistory;

    public AgentMessaging(ILogger<AgentMessaging> logger, IAgentRegistry agentRegistry)
    {
        _logger = logger;
        _agentRegistry = agentRegistry;
        _messageHistory = new ConcurrentDictionary<Guid, List<AgentMessage>>();
    }

    /// <inheritdoc />
    public async Task<AgentMessage> RequestFromAgent(
        string targetAgentId,
        string request,
        AgentMessageContext context,
        int timeoutSeconds = 30)
    {
        var messageId = Guid.NewGuid().ToString();
        var timestamp = DateTime.UtcNow;

        _logger.LogInformation(
            "Agent message: {SourceAgent} -> {TargetAgent}, MessageId: {MessageId}",
            context.SourceAgentId,
            targetAgentId,
            messageId);

        // Validate target agent exists
        var targetAgent = _agentRegistry.GetAgent(targetAgentId);
        if (targetAgent == null)
        {
            var errorMessage = CreateErrorMessage(messageId, timestamp, context, targetAgentId, request,
                $"Target agent '{targetAgentId}' not found");
            LogMessage(context.WorkflowInstanceId, errorMessage);
            return errorMessage;
        }

        try
        {
            // Simulate agent processing with timeout and retry logic
            var response = await ExecuteWithRetry(
                async () => await ProcessAgentRequest(targetAgent, request, context),
                timeoutSeconds);

            var successMessage = new AgentMessage
            {
                MessageId = messageId,
                Timestamp = timestamp,
                SourceAgent = context.SourceAgentId,
                TargetAgent = targetAgentId,
                MessageType = "request",
                Content = request,
                WorkflowInstanceId = context.WorkflowInstanceId,
                Success = true,
                Response = response
            };

            LogMessage(context.WorkflowInstanceId, successMessage);
            return successMessage;
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "Agent request timed out after retries: {MessageId}", messageId);
            var timeoutMessage = CreateErrorMessage(messageId, timestamp, context, targetAgentId, request,
                $"Request timed out after {timeoutSeconds}s with retry");
            LogMessage(context.WorkflowInstanceId, timeoutMessage);
            return timeoutMessage;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing agent request: {MessageId}", messageId);
            var errorMessage = CreateErrorMessage(messageId, timestamp, context, targetAgentId, request,
                $"Error processing request: {ex.Message}");
            LogMessage(context.WorkflowInstanceId, errorMessage);
            return errorMessage;
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<AgentMessage> GetMessageHistory(Guid workflowInstanceId)
    {
        if (_messageHistory.TryGetValue(workflowInstanceId, out var history))
        {
            return history.AsReadOnly();
        }

        return Array.Empty<AgentMessage>();
    }

    private async Task<string> ExecuteWithRetry(Func<Task<string>> operation, int timeoutSeconds)
    {
        var attempts = 0;
        var maxAttempts = 2; // Original + 1 retry

        while (attempts < maxAttempts)
        {
            attempts++;
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
                return await operation();
            }
            catch (OperationCanceledException) when (attempts < maxAttempts)
            {
                _logger.LogWarning("Agent request attempt {Attempt} timed out, retrying...", attempts);
                continue;
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException($"Operation timed out after {maxAttempts} attempts");
            }
        }

        throw new TimeoutException($"Operation failed after {maxAttempts} attempts");
    }

    private async Task<string> ProcessAgentRequest(
        AgentDefinition targetAgent,
        string request,
        AgentMessageContext context)
    {
        // Simulate agent processing
        await Task.Delay(100);

        // In real implementation, this would invoke the actual agent
        // For now, return a simulated response based on agent type
        return $"Agent {targetAgent.Name} processed request: {request}";
    }

    private AgentMessage CreateErrorMessage(
        string messageId,
        DateTime timestamp,
        AgentMessageContext context,
        string targetAgentId,
        string request,
        string errorMessage)
    {
        return new AgentMessage
        {
            MessageId = messageId,
            Timestamp = timestamp,
            SourceAgent = context.SourceAgentId,
            TargetAgent = targetAgentId,
            MessageType = "request",
            Content = request,
            WorkflowInstanceId = context.WorkflowInstanceId,
            Success = false,
            ErrorMessage = errorMessage
        };
    }

    private void LogMessage(Guid workflowInstanceId, AgentMessage message)
    {
        _messageHistory.AddOrUpdate(
            workflowInstanceId,
            _ => new List<AgentMessage> { message },
            (_, existing) =>
            {
                existing.Add(message);
                return existing;
            });
    }
}
