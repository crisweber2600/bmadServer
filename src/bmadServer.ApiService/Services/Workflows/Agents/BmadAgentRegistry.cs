using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Options;

namespace bmadServer.ApiService.Services.Workflows.Agents;

/// <summary>
/// Agent registry that loads agents from the BMAD agent-manifest.csv file
/// </summary>
public class BmadAgentRegistry : IAgentRegistry
{
    private readonly Dictionary<string, AgentDefinition> _agents = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, AgentManifestEntry> _manifestEntries = new(StringComparer.OrdinalIgnoreCase);
    private BmadOptions _options;
    private readonly ILogger<BmadAgentRegistry> _logger;
    private HashSet<string> _enabledModules;
    private bool _isLoaded;

    public BmadAgentRegistry(
        IOptions<BmadOptions> options,
        ILogger<BmadAgentRegistry> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _enabledModules = new HashSet<string>(
            _options.EnabledModules,
            StringComparer.OrdinalIgnoreCase);
    }

    public void Reload(BmadOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        lock (_agents)
        {
            _options = options;
            _enabledModules = new HashSet<string>(
                _options.EnabledModules,
                StringComparer.OrdinalIgnoreCase);
            _agents.Clear();
            _manifestEntries.Clear();
            _isLoaded = false;
        }

        _logger.LogInformation("Reloaded BMAD agent registry with BasePath {BasePath}", _options.BasePath);
    }

    /// <inheritdoc />
    public IReadOnlyList<AgentDefinition> GetAllAgents()
    {
        EnsureLoaded();
        return _agents.Values.ToList().AsReadOnly();
    }

    /// <inheritdoc />
    public AgentDefinition? GetAgent(string agentId)
    {
        if (string.IsNullOrWhiteSpace(agentId))
        {
            _logger.LogWarning("Attempted to get agent with null or empty ID");
            return null;
        }

        EnsureLoaded();

        if (_agents.TryGetValue(agentId, out var agent))
        {
            return agent;
        }

        // Try lazy-loading from manifest if we have the entry but not the full definition
        if (_manifestEntries.TryGetValue(agentId, out var entry))
        {
            var loadedAgent = LoadAgentFromFile(entry);
            if (loadedAgent != null)
            {
                _agents[agentId] = loadedAgent;
                return loadedAgent;
            }
        }

        _logger.LogWarning("Agent not found: {AgentId}", agentId);
        return null;
    }

    /// <inheritdoc />
    public IReadOnlyList<AgentDefinition> GetAgentsByCapability(string capability)
    {
        if (string.IsNullOrWhiteSpace(capability))
        {
            _logger.LogWarning("Attempted to filter agents by null or empty capability");
            return [];
        }

        EnsureLoaded();

        var matchingAgents = _agents.Values
            .Where(a => a.Capabilities.Contains(capability, StringComparer.OrdinalIgnoreCase))
            .ToList();

        _logger.LogDebug(
            "Found {AgentCount} agents with capability {Capability}",
            matchingAgents.Count, capability);

        return matchingAgents.AsReadOnly();
    }

    /// <inheritdoc />
    public void RegisterAgent(AgentDefinition agent)
    {
        if (agent == null)
        {
            throw new ArgumentNullException(nameof(agent));
        }

        if (string.IsNullOrWhiteSpace(agent.AgentId))
        {
            throw new ArgumentException("Agent ID cannot be null or empty", nameof(agent));
        }

        _agents[agent.AgentId] = agent;
        _logger.LogInformation(
            "Registered agent {AgentId} with {CapabilityCount} capabilities",
            agent.AgentId, agent.Capabilities.Count);
    }

    private void EnsureLoaded()
    {
        if (_isLoaded) return;

        lock (_agents)
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
                "Agent manifest not found at {ManifestPath}, using empty registry",
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

            var entries = csv.GetRecords<AgentManifestEntry>().ToList();

            _logger.LogInformation(
                "Loaded {EntryCount} agents from manifest {ManifestPath}",
                entries.Count, manifestPath);

            foreach (var entry in entries)
            {
                // Filter by enabled modules
                if (_enabledModules.Count > 0 && !_enabledModules.Contains(entry.Module))
                {
                    _logger.LogDebug(
                        "Skipping agent {AgentId} - module {Module} not enabled",
                        entry.Name, entry.Module);
                    continue;
                }

                _manifestEntries[entry.Name] = entry;

                // Create a basic definition from manifest data
                // Full details loaded lazily from the .md file
                var agent = CreateAgentFromManifest(entry);
                _agents[entry.Name] = agent;
            }

            _logger.LogInformation(
                "Registered {AgentCount} agents from enabled modules",
                _agents.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load agent manifest from {ManifestPath}", manifestPath);
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

    private AgentDefinition CreateAgentFromManifest(AgentManifestEntry entry)
    {
        // Parse capabilities from role field (comma-separated in some cases)
        var capabilities = ParseCapabilities(entry.Role);

        return new AgentDefinition
        {
            AgentId = entry.Name,
            Name = entry.DisplayName,
            Description = entry.Identity,
            SystemPrompt = BuildSystemPrompt(entry),
            Capabilities = capabilities,
            ModelPreference = null, // Can be set from config or agent file
            Temperature = 0.7m
        };
    }

    private AgentDefinition? LoadAgentFromFile(AgentManifestEntry entry)
    {
        var basePath = string.IsNullOrEmpty(_options.BasePath)
            ? Directory.GetCurrentDirectory()
            : _options.BasePath;

        var agentPath = Path.Combine(basePath, entry.Path);

        if (!File.Exists(agentPath))
        {
            _logger.LogWarning("Agent file not found: {AgentPath}", agentPath);
            return null;
        }

        try
        {
            var content = File.ReadAllText(agentPath);
            var systemPrompt = ExtractSystemPromptFromMarkdown(content, entry);

            return new AgentDefinition
            {
                AgentId = entry.Name,
                Name = entry.DisplayName,
                Description = entry.Identity,
                SystemPrompt = systemPrompt,
                Capabilities = ParseCapabilities(entry.Role),
                ModelPreference = ExtractModelPreference(content),
                Temperature = ExtractTemperature(content)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load agent from file: {AgentPath}", agentPath);
            return null;
        }
    }

    private static string BuildSystemPrompt(AgentManifestEntry entry)
    {
        return $"""
            You are {entry.DisplayName}, {entry.Title}.

            ## Your Identity
            {entry.Identity}

            ## Communication Style
            {entry.CommunicationStyle}

            ## Core Principles
            {entry.Principles}

            ## Your Role
            {entry.Role}
            """;
    }

    private static string ExtractSystemPromptFromMarkdown(string content, AgentManifestEntry entry)
    {
        // If the markdown file has a system prompt section, use it
        // Otherwise, build from manifest data
        var systemPromptStart = content.IndexOf("## System Prompt", StringComparison.OrdinalIgnoreCase);
        if (systemPromptStart >= 0)
        {
            var sectionEnd = content.IndexOf("\n## ", systemPromptStart + 1, StringComparison.OrdinalIgnoreCase);
            if (sectionEnd < 0) sectionEnd = content.Length;

            return content.Substring(
                systemPromptStart + "## System Prompt".Length,
                sectionEnd - systemPromptStart - "## System Prompt".Length).Trim();
        }

        return BuildSystemPrompt(entry);
    }

    private static string? ExtractModelPreference(string content)
    {
        // Look for model preference in frontmatter or content
        var modelMatch = System.Text.RegularExpressions.Regex.Match(
            content,
            @"model(?:Preference)?:\s*['""]?([^'""'\n]+)['""]?",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        return modelMatch.Success ? modelMatch.Groups[1].Value.Trim() : null;
    }

    private static decimal ExtractTemperature(string content)
    {
        var tempMatch = System.Text.RegularExpressions.Regex.Match(
            content,
            @"temperature:\s*(\d+\.?\d*)",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        return tempMatch.Success && decimal.TryParse(tempMatch.Groups[1].Value, out var temp)
            ? temp
            : 0.7m;
    }

    private static List<string> ParseCapabilities(string role)
    {
        // Extract capabilities from role description
        // Common patterns: "Expert + Specialist" -> ["expert", "specialist"]
        var capabilities = new List<string>();

        if (string.IsNullOrWhiteSpace(role)) return capabilities;

        // Split by common delimiters
        var parts = role.Split(['+', ',', '|'], StringSplitOptions.RemoveEmptyEntries);

        foreach (var part in parts)
        {
            var cleaned = part.Trim().ToLowerInvariant()
                .Replace(" ", "-")
                .Replace("_", "-");

            if (!string.IsNullOrEmpty(cleaned))
            {
                capabilities.Add(cleaned);
            }
        }

        return capabilities;
    }
}

/// <summary>
/// Represents a row in the agent-manifest.csv file
/// </summary>
public class AgentManifestEntry
{
    [CsvHelper.Configuration.Attributes.Name("name")]
    public string Name { get; set; } = string.Empty;

    [CsvHelper.Configuration.Attributes.Name("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [CsvHelper.Configuration.Attributes.Name("title")]
    public string Title { get; set; } = string.Empty;

    [CsvHelper.Configuration.Attributes.Name("icon")]
    public string Icon { get; set; } = string.Empty;

    [CsvHelper.Configuration.Attributes.Name("role")]
    public string Role { get; set; } = string.Empty;

    [CsvHelper.Configuration.Attributes.Name("identity")]
    public string Identity { get; set; } = string.Empty;

    [CsvHelper.Configuration.Attributes.Name("communicationStyle")]
    public string CommunicationStyle { get; set; } = string.Empty;

    [CsvHelper.Configuration.Attributes.Name("principles")]
    public string Principles { get; set; } = string.Empty;

    [CsvHelper.Configuration.Attributes.Name("module")]
    public string Module { get; set; } = string.Empty;

    [CsvHelper.Configuration.Attributes.Name("path")]
    public string Path { get; set; } = string.Empty;
}
