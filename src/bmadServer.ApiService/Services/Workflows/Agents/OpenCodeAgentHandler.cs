using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using bmadServer.ApiService.Models.Workflows;
using Microsoft.Extensions.Options;

namespace bmadServer.ApiService.Services.Workflows.Agents;

/// <summary>
/// Agent handler that executes prompts via OpenCode CLI for real LLM integration
/// </summary>
public class OpenCodeAgentHandler : IAgentHandler
{
    private readonly OpenCodeOptions _options;
    private readonly ILogger<OpenCodeAgentHandler> _logger;
    private readonly AgentDefinition _agentDefinition;
    private readonly string? _modelOverride;

    public OpenCodeAgentHandler(
        AgentDefinition agentDefinition,
        IOptions<OpenCodeOptions> options,
        ILogger<OpenCodeAgentHandler> logger,
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
                "Executing agent {AgentId} with model {Model} for step {StepName}",
                _agentDefinition.AgentId, model, context.StepName);

            var result = await ExecuteOpenCodeAsync(prompt, model, cancellationToken);

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
            _logger.LogError(ex, "Agent {AgentId} execution failed", _agentDefinition.AgentId);
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
            "Starting streaming execution for agent {AgentId} with model {Model}",
            _agentDefinition.AgentId, model);

        yield return new StepProgress
        {
            Message = $"Starting {_agentDefinition.Name}...",
            PercentComplete = 0
        };

        var processInfo = CreateProcessStartInfo(prompt, model);
        using var process = new Process { StartInfo = processInfo };

        // Try to start the process - capture error outside of catch for yield
        string? startError = null;
        try
        {
            process.Start();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start OpenCode process for agent {AgentId}", _agentDefinition.AgentId);
            startError = ex.Message;
        }

        // Handle start error outside the catch block (can't yield in catch)
        if (startError != null)
        {
            yield return new StepProgress
            {
                Message = $"Error: Failed to start OpenCode - {startError}",
                PercentComplete = 100
            };
            yield break;
        }

        var outputBuilder = new StringBuilder();
        var lineCount = 0;

        // Read stdout line by line for streaming progress
        string? line;
        while ((line = await process.StandardOutput.ReadLineAsync(cancellationToken).ConfigureAwait(false)) != null)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill(entireProcessTree: true);
                        await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to kill OpenCode process on cancellation");
                }
                yield break;
            }

            outputBuilder.AppendLine(line);
            lineCount++;

            // Emit progress every few lines
            if (lineCount % 5 == 0)
            {
                yield return new StepProgress
                {
                    Message = $"Processing... ({lineCount} lines received)",
                    PercentComplete = Math.Min(90, lineCount * 2)
                };
            }
        }

        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        var exitCode = process.ExitCode;
        var stderr = await process.StandardError.ReadToEndAsync(cancellationToken).ConfigureAwait(false);

        if (exitCode != 0)
        {
            _logger.LogError(
                "OpenCode exited with code {ExitCode}: {StdErr}",
                exitCode, stderr);

            yield return new StepProgress
            {
                Message = $"Error: {stderr}",
                PercentComplete = 100
            };
        }
        else
        {
            yield return new StepProgress
            {
                Message = "Completed successfully",
                PercentComplete = 100
            };
        }
    }

    private string BuildPrompt(AgentContext context)
    {
        var promptBuilder = new StringBuilder();

        // Add system prompt context
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

        // Add conversation history
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
            // Include relevant shared context sections
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

    private ProcessStartInfo CreateProcessStartInfo(string prompt, string model)
    {
        var arguments = new StringBuilder();
        arguments.Append("chat ");

        // Add model flag
        arguments.Append($"--model \"{model}\" ");

        // Add system prompt
        if (!string.IsNullOrEmpty(_agentDefinition.SystemPrompt))
        {
            var escapedSystemPrompt = _agentDefinition.SystemPrompt
                .Replace("\"", "\\\"")
                .Replace("\n", " ");
            arguments.Append($"--system \"{escapedSystemPrompt}\" ");
        }

        // Add temperature if specified
        if (_agentDefinition.Temperature.HasValue)
        {
            arguments.Append($"--temperature {_agentDefinition.Temperature.Value} ");
        }

        // Add max tokens if specified
        if (_agentDefinition.MaxTokens.HasValue)
        {
            arguments.Append($"--max-tokens {_agentDefinition.MaxTokens.Value} ");
        }

        // Add verbose flag if enabled
        if (_options.VerboseLogging)
        {
            arguments.Append("--verbose ");
        }

        // Add JSON output format
        arguments.Append("--format json ");

        // Add the prompt (escaped)
        var escapedPrompt = prompt
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n");
        arguments.Append($"\"{escapedPrompt}\"");

        return new ProcessStartInfo
        {
            FileName = _options.ExecutablePath,
            Arguments = arguments.ToString(),
            WorkingDirectory = _options.WorkingDirectory ?? Directory.GetCurrentDirectory(),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };
    }

    private async Task<string> ExecuteOpenCodeAsync(
        string prompt,
        string model,
        CancellationToken cancellationToken)
    {
        var processInfo = CreateProcessStartInfo(prompt, model);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));

        using var process = new Process { StartInfo = processInfo };

        _logger.LogDebug(
            "Executing: {Executable} {Arguments}",
            processInfo.FileName, processInfo.Arguments.Substring(0, Math.Min(200, processInfo.Arguments.Length)));

        process.Start();

        var outputTask = process.StandardOutput.ReadToEndAsync(cts.Token);
        var errorTask = process.StandardError.ReadToEndAsync(cts.Token);

        await process.WaitForExitAsync(cts.Token);

        var output = await outputTask;
        var error = await errorTask;

        if (process.ExitCode != 0)
        {
            throw new OpenCodeExecutionException(
                $"OpenCode exited with code {process.ExitCode}: {error}",
                process.ExitCode,
                error);
        }

        return output;
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
            JsonDocument? outputDoc = null;
            if (root.TryGetProperty("output", out var outputProp))
            {
                outputDoc = JsonDocument.Parse(outputProp.GetRawText());
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
            _logger.LogWarning("OpenCode returned non-JSON output, wrapping as raw text");

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
            || (ex is OpenCodeExecutionException oce && oce.ExitCode == -1);
    }
}

/// <summary>
/// Exception thrown when OpenCode CLI execution fails
/// </summary>
public class OpenCodeExecutionException : Exception
{
    public int ExitCode { get; }
    public string StandardError { get; }

    public OpenCodeExecutionException(string message, int exitCode, string standardError)
        : base(message)
    {
        ExitCode = exitCode;
        StandardError = standardError;
    }
}
