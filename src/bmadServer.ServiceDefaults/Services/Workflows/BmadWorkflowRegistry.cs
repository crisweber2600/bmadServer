using System.Globalization;
using System.Text.RegularExpressions;
using bmadServer.ServiceDefaults.Models.Workflows;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace bmadServer.ServiceDefaults.Services.Workflows;

/// <summary>
/// Workflow registry that dynamically loads workflows from the BMAD workflow-manifest.csv file
/// and parses workflow definitions from markdown/yaml files following BMAD micro-file architecture.
/// 
/// This registry provides:
/// <list type="bullet">
///   <item><description>CSV manifest parsing for workflow discovery</description></item>
///   <item><description>Module-based filtering via <see cref="BmadWorkflowOptions.EnabledModules"/></description></item>
///   <item><description>Lazy loading of workflow details from step files</description></item>
///   <item><description>Support for BMAD frontmatter metadata parsing</description></item>
/// </list>
/// 
/// Configuration is provided via <see cref="BmadWorkflowOptions"/> in appsettings.json:
/// <code>
/// {
///   "BmadWorkflow": {
///     "ManifestPath": "_bmad/_config/workflow-manifest.csv",
///     "EnabledModules": ["core", "bmm"],
///     "BasePath": ""
///   }
/// }
/// </code>
/// </summary>
/// <remarks>
/// Note: This registry is in ServiceDefaults to share workflow definitions across services.
/// The agent-specific registry (<c>BmadAgentRegistry</c>) is in ApiService as agents are
/// only used by the API layer.
/// </remarks>
public class BmadWorkflowRegistry : IWorkflowRegistry
{
    private readonly Dictionary<string, WorkflowDefinition> _workflows = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, WorkflowManifestEntry> _manifestEntries = new(StringComparer.OrdinalIgnoreCase);
    private readonly BmadWorkflowOptions _options;
    private readonly ILogger<BmadWorkflowRegistry> _logger;
    private readonly HashSet<string> _enabledModules;
    private bool _isLoaded;

    public BmadWorkflowRegistry(
        IOptions<BmadWorkflowOptions> options,
        ILogger<BmadWorkflowRegistry> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _enabledModules = new HashSet<string>(
            _options.EnabledModules,
            StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public IReadOnlyList<WorkflowDefinition> GetAllWorkflows()
    {
        EnsureLoaded();
        return _workflows.Values.ToList().AsReadOnly();
    }

    /// <inheritdoc />
    public WorkflowDefinition? GetWorkflow(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            _logger.LogWarning("GetWorkflow called with null or empty workflow id");
            return null;
        }

        EnsureLoaded();

        if (_workflows.TryGetValue(id, out var workflow))
        {
            if (workflow.Steps.Count == 0 && _manifestEntries.TryGetValue(id, out var manifestEntry))
            {
                var loadedWorkflow = LoadWorkflowFromFile(manifestEntry);
                if (loadedWorkflow != null)
                {
                    _workflows[id] = loadedWorkflow;
                    return loadedWorkflow;
                }
            }

            return workflow;
        }

        // Try lazy-loading from manifest if we have the entry but not the full definition
        if (_manifestEntries.TryGetValue(id, out var entry))
        {
            var loadedWorkflow = LoadWorkflowFromFile(entry);
            if (loadedWorkflow != null)
            {
                _workflows[id] = loadedWorkflow;
                return loadedWorkflow;
            }
        }

        _logger.LogWarning("Workflow with id '{WorkflowId}' not found", id);
        return null;
    }

    /// <inheritdoc />
    public bool ValidateWorkflow(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return false;
        }

        EnsureLoaded();
        return _workflows.ContainsKey(id) || _manifestEntries.ContainsKey(id);
    }

    private void EnsureLoaded()
    {
        if (_isLoaded) return;

        lock (_workflows)
        {
            if (_isLoaded) return;

            LoadManifest();
            _isLoaded = true;
        }
    }

    private void LoadManifest()
    {
        var manifestPath = GetManifestPath();

        if (!File.Exists(manifestPath))
        {
            _logger.LogWarning(
                "Workflow manifest not found at {ManifestPath}, using empty registry",
                manifestPath);
            return;
        }

        try
        {
            using var reader = new StreamReader(manifestPath);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HeaderValidated = null,
                MissingFieldFound = null,
                TrimOptions = TrimOptions.Trim
            });

            var entries = csv.GetRecords<WorkflowManifestEntry>().ToList();

            _logger.LogInformation(
                "Loaded {EntryCount} workflows from manifest {ManifestPath}",
                entries.Count, manifestPath);

            foreach (var entry in entries)
            {
                // Filter by enabled modules
                if (_enabledModules.Count > 0 && !_enabledModules.Contains(entry.Module))
                {
                    _logger.LogDebug(
                        "Skipping workflow {WorkflowId} - module {Module} not enabled",
                        entry.Name, entry.Module);
                    continue;
                }

                _manifestEntries[entry.Name] = entry;

                // Create a basic definition from manifest data
                // Full details loaded lazily from the workflow file
                var workflow = CreateWorkflowFromManifest(entry);
                _workflows[entry.Name] = workflow;
            }

            _logger.LogInformation(
                "Registered {WorkflowCount} workflows from enabled modules",
                _workflows.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load workflow manifest from {ManifestPath}", manifestPath);
            throw;
        }
    }

    private string GetManifestPath()
    {
        var basePath = string.IsNullOrEmpty(_options.BasePath)
            ? Directory.GetCurrentDirectory()
            : _options.BasePath;

        return Path.Combine(basePath, _options.ManifestPath);
    }

    private WorkflowDefinition CreateWorkflowFromManifest(WorkflowManifestEntry entry)
    {
        return new WorkflowDefinition
        {
            WorkflowId = entry.Name,
            Name = FormatWorkflowName(entry.Name),
            Description = entry.Description,
            EstimatedDuration = TimeSpan.FromHours(1), // Default, can be overridden by file
            RequiredRoles = new List<string> { "user" }.AsReadOnly(),
            Steps = new List<WorkflowStep>().AsReadOnly() // Lazy-loaded
        };
    }

    private WorkflowDefinition? LoadWorkflowFromFile(WorkflowManifestEntry entry)
    {
        var basePath = string.IsNullOrEmpty(_options.BasePath)
            ? Directory.GetCurrentDirectory()
            : _options.BasePath;

        var workflowPath = Path.Combine(basePath, entry.Path);

        if (!File.Exists(workflowPath))
        {
            _logger.LogWarning("Workflow file not found: {WorkflowPath}", workflowPath);
            return null;
        }

        try
        {
            var content = File.ReadAllText(workflowPath);
            var workflowDir = Path.GetDirectoryName(workflowPath)!;

            // Parse workflow metadata from frontmatter
            var metadata = ParseFrontmatter(content);

            // Discover steps from steps/ subdirectory
            var steps = DiscoverSteps(workflowDir, entry);

            return new WorkflowDefinition
            {
                WorkflowId = entry.Name,
                Name = metadata.GetValueOrDefault("name", FormatWorkflowName(entry.Name)),
                Description = metadata.GetValueOrDefault("description", entry.Description),
                EstimatedDuration = ParseDuration(metadata.GetValueOrDefault("estimatedDuration", "1h")),
                RequiredRoles = ParseRoles(metadata.GetValueOrDefault("requiredRoles", "user")),
                Steps = steps
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load workflow from file: {WorkflowPath}", workflowPath);
            return null;
        }
    }

    private static Dictionary<string, string> ParseFrontmatter(string content)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Match YAML frontmatter between --- markers
        var frontmatterMatch = Regex.Match(content, @"^---\s*\n(.*?)\n---", RegexOptions.Singleline);
        if (!frontmatterMatch.Success) return result;

        var frontmatter = frontmatterMatch.Groups[1].Value;
        var lines = frontmatter.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var colonIndex = line.IndexOf(':');
            if (colonIndex > 0)
            {
                var key = line[..colonIndex].Trim();
                var value = line[(colonIndex + 1)..].Trim().Trim('\'', '"');
                result[key] = value;
            }
        }

        return result;
    }

    private IReadOnlyList<WorkflowStep> DiscoverSteps(string workflowDir, WorkflowManifestEntry entry)
    {
        var stepsDir = Path.Combine(workflowDir, "steps");
        var steps = new List<WorkflowStep>();

        if (!Directory.Exists(stepsDir))
        {
            _logger.LogDebug(
                "No steps directory found for workflow {WorkflowId}, creating single-step workflow",
                entry.Name);

            // Single-step workflow
            return new List<WorkflowStep>
            {
                new WorkflowStep
                {
                    StepId = $"{entry.Name}-1",
                    Name = FormatWorkflowName(entry.Name),
                    AgentId = GetDefaultAgentForModule(entry.Module),
                    IsOptional = false,
                    CanSkip = false
                }
            }.AsReadOnly();
        }

        // Find step files (step-01-*.md, step-02-*.md, etc.)
        var stepFiles = Directory.GetFiles(stepsDir, "step-*.md")
            .OrderBy(f => f)
            .ToList();

        foreach (var stepFile in stepFiles)
        {
            var fileName = Path.GetFileNameWithoutExtension(stepFile);
            var stepContent = File.ReadAllText(stepFile);
            var stepMetadata = ParseFrontmatter(stepContent);

            // Extract step number and name from filename (e.g., "step-01-understand")
            var match = Regex.Match(fileName, @"step-(\d+)-(.+)");
            var stepNumber = match.Success ? match.Groups[1].Value : "1";
            var stepName = match.Success
                ? FormatWorkflowName(match.Groups[2].Value)
                : fileName;

            steps.Add(new WorkflowStep
            {
                StepId = $"{entry.Name}-{stepNumber}",
                Name = stepMetadata.TryGetValue("name", out var name) ? name : stepName,
                AgentId = stepMetadata.TryGetValue("agent", out var agent) ? agent : GetDefaultAgentForModule(entry.Module),
                InputSchema = stepMetadata.TryGetValue("inputSchema", out var inputSchema) ? inputSchema : null,
                OutputSchema = stepMetadata.TryGetValue("outputSchema", out var outputSchema) ? outputSchema : null,
                IsOptional = stepMetadata.TryGetValue("optional", out var optVal) && bool.TryParse(optVal, out var opt) && opt,
                CanSkip = stepMetadata.TryGetValue("canSkip", out var skipVal) && bool.TryParse(skipVal, out var skip) && skip
            });
        }

        return steps.AsReadOnly();
    }

    private static string GetDefaultAgentForModule(string module)
    {
        return module.ToLowerInvariant() switch
        {
            "core" => "bmad-master",
            "bmm" => "pm",
            "bmgd" => "game-designer",
            "bmb" => "workflow-builder",
            "cis" => "brainstorming-coach",
            _ => "bmad-master"
        };
    }

    private static string FormatWorkflowName(string name)
    {
        // Convert kebab-case to Title Case
        return string.Join(" ",
            name.Split('-', '_')
                .Select(word => char.ToUpperInvariant(word[0]) + word[1..]));
    }

    private static TimeSpan ParseDuration(string duration)
    {
        // Parse durations like "1h", "30m", "2h30m"
        var hours = 0;
        var minutes = 0;

        var hourMatch = Regex.Match(duration, @"(\d+)h", RegexOptions.IgnoreCase);
        if (hourMatch.Success)
        {
            hours = int.Parse(hourMatch.Groups[1].Value);
        }

        var minuteMatch = Regex.Match(duration, @"(\d+)m", RegexOptions.IgnoreCase);
        if (minuteMatch.Success)
        {
            minutes = int.Parse(minuteMatch.Groups[1].Value);
        }

        return hours == 0 && minutes == 0
            ? TimeSpan.FromHours(1)
            : TimeSpan.FromHours(hours) + TimeSpan.FromMinutes(minutes);
    }

    private static IReadOnlyList<string> ParseRoles(string roles)
    {
        return roles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList()
            .AsReadOnly();
    }
}

/// <summary>
/// Configuration options for BMAD workflow registry
/// </summary>
public class BmadWorkflowOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json
    /// </summary>
    public const string SectionName = "Bmad";

    /// <summary>
    /// Path to the workflow manifest CSV file
    /// </summary>
    public string ManifestPath { get; set; } = "_bmad/_config/workflow-manifest.csv";

    /// <summary>
    /// List of enabled modules (e.g., ["core", "bmm", "bmgd"])
    /// Empty list means all modules are enabled
    /// </summary>
    public List<string> EnabledModules { get; set; } = [];

    /// <summary>
    /// Base path for BMAD files (defaults to project root)
    /// </summary>
    public string BasePath { get; set; } = "";
}

/// <summary>
/// Represents a row in the workflow-manifest.csv file
/// </summary>
public class WorkflowManifestEntry
{
    [CsvHelper.Configuration.Attributes.Name("name")]
    public string Name { get; set; } = string.Empty;

    [CsvHelper.Configuration.Attributes.Name("description")]
    public string Description { get; set; } = string.Empty;

    [CsvHelper.Configuration.Attributes.Name("module")]
    public string Module { get; set; } = string.Empty;

    [CsvHelper.Configuration.Attributes.Name("path")]
    public string Path { get; set; } = string.Empty;
}
