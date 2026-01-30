namespace bmadServer.ApiService.Services.Workflows.Agents;

/// <summary>
/// Configuration options for GitHub Copilot SDK integration
/// </summary>
public class CopilotOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json
    /// </summary>
    public const string SectionName = "Bmad:Copilot";

    /// <summary>
    /// Default model name constant for consistency
    /// </summary>
    public const string DefaultModelName = "gpt-4.1";

    /// <summary>
    /// Default model to use when agent has no preference.
    /// Supports models available through Copilot: gpt-4.1, claude-sonnet-4-20250514, etc.
    /// Configure via appsettings.json under Bmad:Copilot:DefaultModel
    /// </summary>
    public string DefaultModel { get; set; } = DefaultModelName;

    /// <summary>
    /// Timeout in seconds for Copilot SDK calls
    /// </summary>
    public int TimeoutSeconds { get; set; } = 120;

    /// <summary>
    /// Whether to enable verbose logging
    /// </summary>
    public bool VerboseLogging { get; set; } = false;

    /// <summary>
    /// Maximum retries for transient failures
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Delay between retries in milliseconds
    /// </summary>
    public int RetryDelayMs { get; set; } = 1000;

    /// <summary>
    /// Session ID prefix for workflow sessions (enables session persistence)
    /// </summary>
    public string SessionIdPrefix { get; set; } = "bmad-workflow-";
}
