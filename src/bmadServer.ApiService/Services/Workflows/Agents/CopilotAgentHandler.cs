using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using bmadServer.ApiService.Models.Workflows;
using GitHub.Copilot.SDK;
using Microsoft.Extensions.Options;

namespace bmadServer.ApiService.Services.Workflows.Agents;

/// <summary>
/// Agent handler that executes prompts via GitHub Copilot SDK for real LLM integration.
/// Replaces OpenCodeAgentHandler with a more robust JSON-RPC based implementation.
/// </summary>
public class CopilotAgentHandler : IAgentHandler
{
    private readonly CopilotOptions _options;
    private readonly ILogger<CopilotAgentHandler> _logger;
    private readonly AgentDefinition _agentDefinition;
    private readonly string? _modelOverride;

    public CopilotAgentHandler(
        AgentDefinition agentDefinition,
        IOptions<CopilotOptions> options,
        ILogger<CopilotAgentHandler> logger,
        string? modelOverride = null)
    {
        _agentDefinition = agentDefinition ?? throw new ArgumentNullException(nameof(agentDefinition));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _modelOverride = modelOverride;
    }

    /// <inheritdoc />
    public async Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var prompt = BuildPrompt(context);
            var model = GetEffectiveModel();

            _logger.LogInformation(
                "Executing agent {AgentId} with model {Model} via Copilot SDK for step {StepName}",
                _agentDefinition.AgentId, model, context.StepName);

            await using var client = new CopilotClient();
            await client.StartAsync(cancellationToken);

            var sessionConfig = new SessionConfig
            {
                Model = model,
                SystemMessage = !string.IsNullOrEmpty(_agentDefinition.SystemPrompt)
                    ? new SystemMessageConfig
                    {
                        Mode = SystemMessageMode.Replace,
                        Content = _agentDefinition.SystemPrompt
                    }
                    : null
            };

            await using var session = await client.CreateSessionAsync(sessionConfig, cancellationToken);

            // Collect the response content
            var responseBuilder = new StringBuilder();
            
            session.On(evt =>
            {
                if (evt is AssistantMessageEvent msg && msg.Data?.Content != null)
                {
                    responseBuilder.Append(msg.Data.Content);
                }
            });

            await session.SendAsync(new MessageOptions { Prompt = prompt }, cancellationToken);

            // Wait for the session to complete processing
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));

            var completionSource = new TaskCompletionSource<bool>();
            session.On(evt =>
            {
                if (evt is SessionIdleEvent)
                {
                    completionSource.TrySetResult(true);
                }
            });

            try
            {
                await completionSource.Task.WaitAsync(timeoutCts.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Copilot session timed out after {Timeout}s", _options.TimeoutSeconds);
            }

            var result = responseBuilder.ToString();
            
            if (string.IsNullOrWhiteSpace(result))
            {
                return new AgentResult
                {
                    Success = false,
                    ErrorMessage = "No response received from Copilot",
                    IsRetryable = true
                };
            }

            return ParseResult(result);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Agent {AgentId} execution was cancelled", _agentDefinition.AgentId);
            return new AgentResult
            {
                Success = false,
                ErrorMessage = "Execution was cancelled",
                IsRetryable = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Agent {AgentId} execution failed via Copilot SDK", _agentDefinition.AgentId);
            return new AgentResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                IsRetryable = IsRetryableException(ex)
            };
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<StepProgress> ExecuteWithStreamingAsync(
        AgentContext context,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var prompt = BuildPrompt(context);
        var model = GetEffectiveModel();

        _logger.LogInformation(
            "Starting streaming execution for agent {AgentId} with model {Model} via Copilot SDK",
            _agentDefinition.AgentId, model);

        yield return new StepProgress
        {
            Message = $"Starting {_agentDefinition.Name} via Copilot SDK...",
            PercentComplete = 0
        };

        CopilotClient? client = null;
        CopilotSession? session = null;

        try
        {
            client = new CopilotClient();
            await client.StartAsync(cancellationToken);

            yield return new StepProgress
            {
                Message = "Connected to Copilot...",
                PercentComplete = 10
            };

            var sessionConfig = new SessionConfig
            {
                Model = model,
                SystemMessage = !string.IsNullOrEmpty(_agentDefinition.SystemPrompt)
                    ? new SystemMessageConfig
                    {
                        Mode = SystemMessageMode.Replace,
                        Content = _agentDefinition.SystemPrompt
                    }
                    : null
            };

            session = await client.CreateSessionAsync(sessionConfig, cancellationToken);

            var contentBuilder = new StringBuilder();
            var chunkCount = 0;
            var isComplete = false;

            session.On(evt =>
            {
                switch (evt)
                {
                    case AssistantMessageEvent msg when msg.Data?.Content != null:
                        contentBuilder.Append(msg.Data.Content);
                        chunkCount++;
                        break;
                    case SessionIdleEvent:
                        isComplete = true;
                        break;
                }
            });

            await session.SendAsync(new MessageOptions { Prompt = prompt }, cancellationToken);

            // Poll for progress updates while waiting for completion
            var startTime = DateTime.UtcNow;
            var timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);

            while (!isComplete && !cancellationToken.IsCancellationRequested)
            {
                if (DateTime.UtcNow - startTime > timeout)
                {
                    _logger.LogWarning("Streaming timed out after {Timeout}s", _options.TimeoutSeconds);
                    break;
                }

                await Task.Delay(500, cancellationToken);

                if (chunkCount > 0)
                {
                    yield return new StepProgress
                    {
                        Message = $"Processing... ({chunkCount} chunks received)",
                        PercentComplete = Math.Min(90, 10 + chunkCount * 2)
                    };
                }
            }

            if (cancellationToken.IsCancellationRequested)
            {
                yield return new StepProgress
                {
                    Message = "Cancelled by user",
                    PercentComplete = 100
                };
                yield break;
            }

            yield return new StepProgress
            {
                Message = isComplete ? "Completed successfully" : "Completed (timeout)",
                PercentComplete = 100
            };
        }
        finally
        {
            if (session != null)
            {
                await session.DisposeAsync();
            }
            if (client != null)
            {
                await client.StopAsync();
                await client.DisposeAsync();
            }
        }
    }

    private string BuildPrompt(AgentContext context)
    {
        var promptBuilder = new StringBuilder();

        // Add agent context header
        promptBuilder.AppendLine("### AGENT CONTEXT ###");
        promptBuilder.AppendLine($"Agent: {_agentDefinition.Name}");
        promptBuilder.AppendLine($"Step: {context.StepName} ({context.StepId})");
        promptBuilder.AppendLine();

        // Add workflow context if available
        if (context.WorkflowContext != null)
        {
            promptBuilder.AppendLine("### WORKFLOW CONTEXT ###");
            promptBuilder.AppendLine(context.WorkflowContext.RootElement.ToString());
            promptBuilder.AppendLine();
        }

        // Add step parameters if available
        if (context.StepParameters != null)
        {
            promptBuilder.AppendLine("### STEP PARAMETERS ###");
            promptBuilder.AppendLine(context.StepParameters.RootElement.ToString());
            promptBuilder.AppendLine();
        }

        // Add conversation history (last 10 messages)
        if (context.ConversationHistory.Count > 0)
        {
            promptBuilder.AppendLine("### CONVERSATION HISTORY ###");
            foreach (var message in context.ConversationHistory.TakeLast(10))
            {
                promptBuilder.AppendLine($"[{message.Role}]: {message.Content}");
            }
            promptBuilder.AppendLine();
        }

        // Add shared context if available
        if (context.SharedContext != null)
        {
            promptBuilder.AppendLine("### SHARED CONTEXT ###");
            if (context.SharedContext.StepOutputs.Count > 0)
            {
                promptBuilder.AppendLine("Previous Step Outputs:");
                foreach (var (stepId, output) in context.SharedContext.StepOutputs)
                {
                    promptBuilder.AppendLine($"  [{stepId}]: {output.RootElement}");
                }
            }
            if (context.SharedContext.UserPreferences.Count > 0)
            {
                promptBuilder.AppendLine("User Preferences:");
                foreach (var (key, value) in context.SharedContext.UserPreferences)
                {
                    promptBuilder.AppendLine($"  {key}: {value}");
                }
            }
            promptBuilder.AppendLine();
        }

        // Add current user input
        if (!string.IsNullOrEmpty(context.UserInput))
        {
            promptBuilder.AppendLine("### USER INPUT ###");
            promptBuilder.AppendLine(context.UserInput);
            promptBuilder.AppendLine();
        }

        // Add expected output format
        promptBuilder.AppendLine("### EXPECTED OUTPUT ###");
        promptBuilder.AppendLine("Respond with a JSON object containing:");
        promptBuilder.AppendLine("- success: boolean indicating if the step completed successfully");
        promptBuilder.AppendLine("- output: the result data for this step");
        promptBuilder.AppendLine("- reasoning: brief explanation of your approach");
        promptBuilder.AppendLine("- confidence: 0.0-1.0 score for your confidence in this response");

        return promptBuilder.ToString();
    }

    private string GetEffectiveModel()
    {
        // Priority: override > agent preference > default
        return _modelOverride
            ?? _agentDefinition.ModelPreference
            ?? _options.DefaultModel;
    }

    private AgentResult ParseResult(string output)
    {
        try
        {
            // Try to parse as JSON
            var jsonDoc = JsonDocument.Parse(output);
            var root = jsonDoc.RootElement;

            var success = root.TryGetProperty("success", out var successProp)
                && successProp.GetBoolean();

            var confidence = root.TryGetProperty("confidence", out var confidenceProp)
                ? confidenceProp.GetDouble()
                : 1.0;

            var reasoning = root.TryGetProperty("reasoning", out var reasoningProp)
                ? reasoningProp.GetString()
                : null;

            // Extract the output property or use the whole response
            JsonDocument? outputDoc;
            if (root.TryGetProperty("output", out var outputProp))
            {
                outputDoc = JsonDocument.Parse(outputProp.GetRawText());
                jsonDoc.Dispose(); // Dispose the original document since we're using a new one
            }
            else
            {
                outputDoc = jsonDoc;
            }

            return new AgentResult
            {
                Success = success,
                Output = outputDoc,
                ConfidenceScore = confidence,
                Reasoning = reasoning
            };
        }
        catch (JsonException)
        {
            // If not JSON, wrap the raw output
            _logger.LogWarning("Copilot returned non-JSON output, wrapping as raw text");

            var wrappedOutput = JsonDocument.Parse(JsonSerializer.Serialize(new
            {
                rawOutput = output,
                timestamp = DateTime.UtcNow
            }));

            return new AgentResult
            {
                Success = true,
                Output = wrappedOutput,
                ConfidenceScore = 0.8,
                Reasoning = "Output was not in expected JSON format"
            };
        }
    }

    private static bool IsRetryableException(Exception ex)
    {
        return ex is TimeoutException
            || ex is IOException
            || ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase)
            || ex.Message.Contains("connection", StringComparison.OrdinalIgnoreCase);
    }
}
