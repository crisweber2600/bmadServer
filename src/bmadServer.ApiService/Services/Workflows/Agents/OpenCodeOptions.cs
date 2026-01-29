namespace bmadServer.ApiService.Services.Workflows.Agents;

/// <summary>
/// Configuration options for OpenCode CLI integration
/// </summary>
public class OpenCodeOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json
    /// </summary>
    public const string SectionName = "Bmad:OpenCode";

    /// <summary>
    /// Default model name constant for consistency
    /// </summary>
    public const string DefaultModelName = "claude-sonnet-4-20250514";

    /// <summary>
    /// Path to the OpenCode executable (defaults to "opencode" in PATH)
    /// </summary>
    public string ExecutablePath { get; set; } = "opencode";

    /// <summary>
    /// Default model to use when agent has no preference.
    /// Supports: claude-sonnet-4-20250514, gpt-4-turbo, claude-opus, etc.
    /// Configure via appsettings.json under Bmad:OpenCode:DefaultModel
    /// </summary>
    public string DefaultModel { get; set; } = DefaultModelName;

    /// <summary>
    /// Timeout in seconds for OpenCode CLI calls
    /// </summary>
    public int TimeoutSeconds { get; set; } = 120;

    /// <summary>
    /// Working directory for OpenCode execution (defaults to project root)
    /// </summary>
    public string? WorkingDirectory { get; set; }

    /// <summary>
    /// Whether to enable verbose logging from OpenCode
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
}

/// <summary>
/// BMAD-specific configuration options
/// </summary>
public class BmadOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json
    /// </summary>
    public const string SectionName = "Bmad";

    /// <summary>
    /// Path to the agent manifest CSV file
    /// </summary>
    public string ManifestPath { get; set; } = "_bmad/_config/agent-manifest.csv";

    /// <summary>
    /// Path to the workflow manifest CSV file
    /// </summary>
    public string WorkflowManifestPath { get; set; } = "_bmad/_config/workflow-manifest.csv";

    /// <summary>
    /// List of enabled modules (e.g., ["core", "bmm", "bmgd"])
    /// Empty list means all modules are enabled
    /// </summary>
    public List<string> EnabledModules { get; set; } = [];

    /// <summary>
    /// Base path for BMAD files (defaults to project root)
    /// </summary>
    public string BasePath { get; set; } = "";

    /// <summary>
    /// Test mode: Mock (no LLM calls), Live (real calls), Replay (cached responses)
    /// </summary>
    public AgentTestMode TestMode { get; set; } = AgentTestMode.Live;

    /// <summary>
    /// OpenCode-specific configuration
    /// </summary>
    public OpenCodeOptions OpenCode { get; set; } = new();
}

/// <summary>
/// Test mode for agent execution
/// </summary>
public enum AgentTestMode
{
    /// <summary>
    /// Use MockAgentHandler with fake responses (fast, no LLM costs)
    /// </summary>
    Mock,

    /// <summary>
    /// Use real OpenCode CLI for LLM calls
    /// </summary>
    Live,

    /// <summary>
    /// Record responses to fixtures on first call, replay on subsequent calls
    /// </summary>
    Replay
}
