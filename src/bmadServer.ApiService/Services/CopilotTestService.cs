using System.Text;
using GitHub.Copilot.SDK;
using Microsoft.Extensions.Options;
using bmadServer.ApiService.Services.Workflows.Agents;

namespace bmadServer.ApiService.Services;

/// <summary>
/// Service for testing Copilot SDK functionality with debug output.
/// </summary>
public interface ICopilotTestService
{
    Task<CopilotTestResponse> TestCopilotConnectionAsync(CopilotTestRequest request, CancellationToken cancellationToken = default);
}

public class CopilotTestService : ICopilotTestService
{
    private readonly CopilotOptions _options;
    private readonly ILogger<CopilotTestService> _logger;

    public CopilotTestService(IOptions<CopilotOptions> options, ILogger<CopilotTestService> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<CopilotTestResponse> TestCopilotConnectionAsync(CopilotTestRequest request, CancellationToken cancellationToken = default)
    {
        var response = new CopilotTestResponse
        {
            Success = false,
            Timestamp = DateTime.UtcNow,
            RequestedModel = request.Model ?? "gpt-4"
        };

        var debugLog = new List<string>();

        try
        {
            debugLog.Add($"[{DateTime.UtcNow:O}] Starting Copilot SDK test...");
            debugLog.Add($"Model: {response.RequestedModel}");
            debugLog.Add($"Prompt: {request.Prompt}");
            debugLog.Add($"System Message: {request.SystemMessage ?? "(default)"}");

            await using var client = new CopilotClient();
            debugLog.Add($"[{DateTime.UtcNow:O}] CopilotClient created");

            await client.StartAsync(cancellationToken);
            debugLog.Add($"[{DateTime.UtcNow:O}] Copilot client started");

            var sessionConfig = new SessionConfig
            {
                Model = response.RequestedModel,
                SystemMessage = !string.IsNullOrEmpty(request.SystemMessage)
                    ? new SystemMessageConfig
                    {
                        Mode = SystemMessageMode.Replace,
                        Content = request.SystemMessage
                    }
                    : null
            };

            debugLog.Add($"[{DateTime.UtcNow:O}] Creating session with config...");

            await using var session = await client.CreateSessionAsync(sessionConfig, cancellationToken);
            debugLog.Add($"[{DateTime.UtcNow:O}] Session created successfully");

            var responseBuilder = new StringBuilder();
            var eventLog = new List<string>();

            // Subscribe to events for debugging
            session.On(evt =>
            {
                var eventType = evt.GetType().Name;
                eventLog.Add($"[{DateTime.UtcNow:O}] Event: {eventType}");

                if (evt is AssistantMessageEvent msg && msg.Data?.Content != null)
                {
                    responseBuilder.Append(msg.Data.Content);
                    eventLog.Add($"  Content: {msg.Data.Content}");
                }
                else if (evt is SessionIdleEvent)
                {
                    eventLog.Add($"  Session is now idle");
                }
            });

            debugLog.Add($"[{DateTime.UtcNow:O}] Event handlers registered, sending message...");
            debugLog.Add($"Prompt to send: {request.Prompt}");

            await session.SendAsync(new MessageOptions { Prompt = request.Prompt }, cancellationToken);
            debugLog.Add($"[{DateTime.UtcNow:O}] Message sent, waiting for response...");

            // Wait for session to complete with timeout
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var timeoutSeconds = request.TimeoutSeconds ?? _options.TimeoutSeconds;
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

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
                debugLog.Add($"[{DateTime.UtcNow:O}] Session completed successfully");
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                debugLog.Add($"[{DateTime.UtcNow:O}] Session timed out after {timeoutSeconds}s");
                response.TimedOut = true;
            }

            response.Content = responseBuilder.ToString();
            response.Success = true;
            response.DebugLog = debugLog;
            response.EventLog = eventLog;

            debugLog.Add($"[{DateTime.UtcNow:O}] Test completed successfully");
            debugLog.Add($"Response length: {response.Content?.Length ?? 0} characters");

            _logger.LogInformation("Copilot test completed successfully. Response length: {Length}", response.Content?.Length ?? 0);
        }
        catch (Exception ex)
        {
            debugLog.Add($"[{DateTime.UtcNow:O}] ERROR: {ex.GetType().Name}: {ex.Message}");
            if (ex.InnerException != null)
            {
                debugLog.Add($"[{DateTime.UtcNow:O}] Inner Exception: {ex.InnerException.Message}");
            }
            debugLog.Add($"[{DateTime.UtcNow:O}] Stack Trace: {ex.StackTrace}");

            response.Success = false;
            response.Error = ex.Message;
            response.ErrorType = ex.GetType().Name;
            response.DebugLog = debugLog;

            _logger.LogError(ex, "Copilot test failed: {Error}", ex.Message);
        }

        return response;
    }
}

/// <summary>
/// Request model for testing Copilot SDK.
/// </summary>
public class CopilotTestRequest
{
    /// <summary>
    /// The prompt to send to Copilot.
    /// </summary>
    public required string Prompt { get; set; }

    /// <summary>
    /// Optional system message to override the default.
    /// </summary>
    public string? SystemMessage { get; set; }

    /// <summary>
    /// Model to use (e.g., "gpt-4", "gpt-3.5-turbo"). Defaults to configured model.
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// Optional timeout in seconds. Defaults to configured timeout.
    /// </summary>
    public int? TimeoutSeconds { get; set; }
}

/// <summary>
/// Response model for Copilot SDK test results.
/// </summary>
public class CopilotTestResponse
{
    /// <summary>
    /// Whether the test succeeded.
    /// </summary>
    public required bool Success { get; set; }

    /// <summary>
    /// The content returned by Copilot.
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// The model that was used.
    /// </summary>
    public string? RequestedModel { get; set; }

    /// <summary>
    /// Error message if test failed.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Type of error if test failed.
    /// </summary>
    public string? ErrorType { get; set; }

    /// <summary>
    /// Whether the session timed out.
    /// </summary>
    public bool TimedOut { get; set; }

    /// <summary>
    /// Debug log with timing information.
    /// </summary>
    public List<string> DebugLog { get; set; } = new();

    /// <summary>
    /// Event log showing all SDK events.
    /// </summary>
    public List<string> EventLog { get; set; } = new();

    /// <summary>
    /// When the test was performed.
    /// </summary>
    public DateTime Timestamp { get; set; }
}
