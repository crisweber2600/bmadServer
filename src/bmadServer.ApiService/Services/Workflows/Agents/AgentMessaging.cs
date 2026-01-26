using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using bmadServer.ApiService.Data;
using bmadServer.ApiService.Data.Entities;
using bmadServer.ApiService.Services.Workflows.Agents;

namespace bmadServer.ApiService.Services.Workflows.Agents;

public class AgentMessaging : IAgentMessaging
{
    private readonly IAgentRegistry _agentRegistry;
    private readonly IAgentRouter _agentRouter;
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<AgentMessaging> _logger;
    private readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(30);

    public AgentMessaging(
        IAgentRegistry agentRegistry,
        IAgentRouter agentRouter,
        ApplicationDbContext dbContext,
        ILogger<AgentMessaging> logger)
    {
        _agentRegistry = agentRegistry ?? throw new ArgumentNullException(nameof(agentRegistry));
        _agentRouter = agentRouter ?? throw new ArgumentNullException(nameof(agentRouter));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AgentResponse> RequestFromAgentAsync(
        string targetAgentId,
        string requestType,
        object payload,
        WorkflowContext context,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(targetAgentId))
            throw new ArgumentException("Target agent ID cannot be null or empty", nameof(targetAgentId));

        if (string.IsNullOrWhiteSpace(requestType))
            throw new ArgumentException("Request type cannot be null or empty", nameof(requestType));

        if (payload == null)
            throw new ArgumentNullException(nameof(payload));

        if (context == null)
            throw new ArgumentNullException(nameof(context));

        var timeoutDuration = timeout ?? _defaultTimeout;
        var correlationId = Guid.NewGuid().ToString();

        _logger.LogInformation(
            "Agent request initiated: {SourceAgent} -> {TargetAgent}, RequestType: {RequestType}, CorrelationId: {CorrelationId}, WorkflowId: {WorkflowId}",
            context.CurrentStepName, targetAgentId, requestType, correlationId, context.WorkflowInstanceId);

        var targetAgent = _agentRegistry.GetAgent(targetAgentId);
        if (targetAgent == null)
        {
            _logger.LogWarning(
                "Target agent not found: {TargetAgent}, CorrelationId: {CorrelationId}",
                targetAgentId, correlationId);

            return new AgentResponse
            {
                Success = false,
                ErrorMessage = $"Target agent not found: {targetAgentId}",
                IsRetryable = false
            };
        }

        var requestMessage = new AgentMessage
        {
            MessageId = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            SourceAgent = context.CurrentStepName,
            TargetAgent = targetAgentId,
            MessageType = MessageType.Request,
            Content = JsonSerializer.SerializeToDocument(payload),
            WorkflowInstanceId = context.WorkflowInstanceId,
            CorrelationId = correlationId
        };

        _ = LogMessageAsync(requestMessage, cancellationToken);

        var handler = _agentRouter.GetHandler(targetAgentId);
        if (handler == null)
        {
            _logger.LogWarning(
                "No handler registered for agent: {TargetAgent}, CorrelationId: {CorrelationId}",
                targetAgentId, correlationId);

            return new AgentResponse
            {
                Success = false,
                ErrorMessage = $"No handler registered for agent: {targetAgentId}",
                IsRetryable = false
            };
        }

        var attempt = 0;
        AgentResponse? response = null;

        while (attempt < 2 && response == null)
        {
            attempt++;

            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(timeoutDuration);

                var agentContext = ConvertToAgentContext(
                    context.CurrentStepName,
                    targetAgentId,
                    requestType,
                    payload,
                    context);

                var agentResult = await handler.ExecuteAsync(agentContext, cts.Token);
                response = ConvertToAgentResponse(agentResult);

                _logger.LogInformation(
                    "Agent request succeeded (attempt {Attempt}): {TargetAgent}, CorrelationId: {CorrelationId}",
                    attempt, targetAgentId, correlationId);
            }
            catch (OperationCanceledException) when (attempt == 1)
            {
                _logger.LogWarning(
                    "Agent request timeout (attempt {Attempt}), retrying: {TargetAgent}, CorrelationId: {CorrelationId}",
                    attempt, targetAgentId, correlationId);
                continue;
            }
            catch (OperationCanceledException)
            {
                _logger.LogError(
                    "Agent request timeout after retry: {TargetAgent}, CorrelationId: {CorrelationId}, TimeoutSeconds: {TimeoutSeconds}",
                    targetAgentId, correlationId, timeoutDuration.TotalSeconds);

                response = new AgentResponse
                {
                    Success = false,
                    ErrorMessage = $"Agent request timed out after {timeoutDuration.TotalSeconds}s and 1 retry",
                    IsRetryable = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Agent request failed (attempt {Attempt}): {TargetAgent}, CorrelationId: {CorrelationId}",
                    attempt, targetAgentId, correlationId);

                response = new AgentResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    IsRetryable = false
                };
            }
        }

        if (response != null)
        {
            var responseMessage = new AgentMessage
            {
                MessageId = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow,
                SourceAgent = targetAgentId,
                TargetAgent = context.CurrentStepName,
                MessageType = response.Success ? MessageType.Response : MessageType.Error,
                Content = JsonSerializer.SerializeToDocument(response),
                WorkflowInstanceId = context.WorkflowInstanceId,
                CorrelationId = correlationId
            };

            _ = LogMessageAsync(responseMessage, cancellationToken);
        }

        return response ?? new AgentResponse
        {
            Success = false,
            ErrorMessage = "Agent request failed to produce a response",
            IsRetryable = false
        };
    }

    public async Task<List<AgentMessage>> GetConversationHistoryAsync(
        Guid workflowInstanceId,
        CancellationToken cancellationToken = default)
    {
        var logs = await _dbContext.AgentMessageLogs
            .Where(m => m.WorkflowInstanceId == workflowInstanceId)
            .OrderBy(m => m.Timestamp)
            .ToListAsync(cancellationToken);

        return logs.Select(ConvertToAgentMessage).ToList();
    }

    private async Task LogMessageAsync(AgentMessage message, CancellationToken cancellationToken)
    {
        try
        {
            var log = new AgentMessageLog
            {
                Id = Guid.NewGuid(),
                MessageId = message.MessageId,
                Timestamp = message.Timestamp,
                SourceAgent = message.SourceAgent,
                TargetAgent = message.TargetAgent,
                MessageType = (int)message.MessageType,
                Content = message.Content,
                WorkflowInstanceId = message.WorkflowInstanceId,
                CorrelationId = message.CorrelationId
            };

            _dbContext.AgentMessageLogs.Add(log);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogDebug(
                "Agent message logged: {MessageId}, CorrelationId: {CorrelationId}, MessageType: {MessageType}",
                message.MessageId, message.CorrelationId, message.MessageType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to log agent message: {MessageId}, CorrelationId: {CorrelationId}",
                message.MessageId, message.CorrelationId);
        }
    }

    private AgentContext ConvertToAgentContext(
        string sourceStepName,
        string targetAgentId,
        string requestType,
        object payload,
        WorkflowContext context)
    {
        var payloadJson = JsonSerializer.SerializeToDocument(payload);
        var contextJson = JsonSerializer.SerializeToDocument(context);

        return new AgentContext
        {
            WorkflowInstanceId = context.WorkflowInstanceId,
            StepId = context.CurrentStepId.ToString(),
            StepName = sourceStepName,
            WorkflowContext = contextJson,
            StepData = payloadJson,
            StepParameters = JsonSerializer.SerializeToDocument(new { requestType, targetAgentId }),
            ConversationHistory = new(),
            UserInput = null
        };
    }

    private AgentResponse ConvertToAgentResponse(AgentResult result)
    {
        return new AgentResponse
        {
            Success = result.Success,
            Content = result.Output,
            ErrorMessage = result.ErrorMessage,
            IsRetryable = result.IsRetryable
        };
    }

    private AgentMessage ConvertToAgentMessage(AgentMessageLog log)
    {
        return new AgentMessage
        {
            MessageId = log.MessageId,
            Timestamp = log.Timestamp,
            SourceAgent = log.SourceAgent,
            TargetAgent = log.TargetAgent,
            MessageType = (MessageType)log.MessageType,
            Content = log.Content,
            WorkflowInstanceId = log.WorkflowInstanceId,
            CorrelationId = log.CorrelationId
        };
    }
}
