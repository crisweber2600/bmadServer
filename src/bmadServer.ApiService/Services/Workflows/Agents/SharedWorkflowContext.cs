using System.Collections.Concurrent;
using System.Text.Json;

namespace bmadServer.ApiService.Services.Workflows.Agents;

/// <summary>
/// Shared context available to all agents in a workflow
/// </summary>
public class SharedWorkflowContext
{
    private readonly ConcurrentDictionary<string, JsonDocument> _stepOutputs;
    private readonly ConcurrentDictionary<string, string> _userPreferences;
    private readonly ConcurrentDictionary<string, string> _artifactReferences;
    private readonly List<WorkflowDecision> _decisionHistory;
    private readonly object _lock = new();
    private int _version;

    public SharedWorkflowContext()
    {
        _stepOutputs = new ConcurrentDictionary<string, JsonDocument>();
        _userPreferences = new ConcurrentDictionary<string, string>();
        _artifactReferences = new ConcurrentDictionary<string, string>();
        _decisionHistory = new List<WorkflowDecision>();
        _version = 0;
    }

    /// <summary>
    /// Current version number for optimistic concurrency control
    /// </summary>
    public int Version => _version;

    /// <summary>
    /// Add output from a completed step
    /// </summary>
    public void AddStepOutput(string stepId, JsonDocument output)
    {
        if (string.IsNullOrWhiteSpace(stepId))
            throw new ArgumentException("Step ID cannot be null or empty", nameof(stepId));

        if (output == null)
            throw new ArgumentNullException(nameof(output));

        _stepOutputs[stepId] = output;
        IncrementVersion();
    }

    /// <summary>
    /// Get output from a specific step
    /// </summary>
    public JsonDocument? GetStepOutput(string stepId)
    {
        if (string.IsNullOrWhiteSpace(stepId))
            return null;

        return _stepOutputs.TryGetValue(stepId, out var output) ? output : null;
    }

    /// <summary>
    /// Get all step outputs
    /// </summary>
    public IReadOnlyDictionary<string, JsonDocument> GetAllStepOutputs()
    {
        return _stepOutputs.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    /// <summary>
    /// Add a decision to history
    /// </summary>
    public void AddDecision(WorkflowDecision decision)
    {
        if (decision == null)
            throw new ArgumentNullException(nameof(decision));

        lock (_lock)
        {
            _decisionHistory.Add(decision);
            IncrementVersion();
        }
    }

    /// <summary>
    /// Get decision history
    /// </summary>
    public IReadOnlyList<WorkflowDecision> GetDecisionHistory()
    {
        lock (_lock)
        {
            return _decisionHistory.ToList().AsReadOnly();
        }
    }

    /// <summary>
    /// Add user preference
    /// </summary>
    public void AddUserPreference(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or empty", nameof(key));

        _userPreferences[key] = value ?? string.Empty;
        IncrementVersion();
    }

    /// <summary>
    /// Get user preference
    /// </summary>
    public string? GetUserPreference(string key)
    {
        return _userPreferences.TryGetValue(key, out var value) ? value : null;
    }

    /// <summary>
    /// Add artifact reference
    /// </summary>
    public void AddArtifactReference(string artifactId, string path)
    {
        if (string.IsNullOrWhiteSpace(artifactId))
            throw new ArgumentException("Artifact ID cannot be null or empty", nameof(artifactId));

        _artifactReferences[artifactId] = path ?? string.Empty;
        IncrementVersion();
    }

    /// <summary>
    /// Get all artifact references
    /// </summary>
    public IReadOnlyDictionary<string, string> GetArtifactReferences()
    {
        return _artifactReferences.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    /// <summary>
    /// Serialize context to JSON
    /// </summary>
    public JsonDocument ToJson()
    {
        var contextData = new
        {
            version = _version,
            stepOutputs = _stepOutputs.Select(kvp => new
            {
                stepId = kvp.Key,
                output = JsonSerializer.Deserialize<JsonElement>(kvp.Value)
            }).ToList(),
            userPreferences = _userPreferences,
            artifactReferences = _artifactReferences,
            decisionHistory = _decisionHistory
        };

        var json = JsonSerializer.Serialize(contextData);
        return JsonDocument.Parse(json);
    }

    /// <summary>
    /// Deserialize context from JSON
    /// </summary>
    public static SharedWorkflowContext FromJson(JsonDocument json)
    {
        var context = new SharedWorkflowContext();

        if (json.RootElement.TryGetProperty("version", out var versionElement))
        {
            context._version = versionElement.GetInt32();
        }

        if (json.RootElement.TryGetProperty("stepOutputs", out var stepOutputsElement))
        {
            foreach (var item in stepOutputsElement.EnumerateArray())
            {
                var stepId = item.GetProperty("stepId").GetString();
                var output = item.GetProperty("output");
                if (stepId != null)
                {
                    var outputJson = JsonDocument.Parse(output.GetRawText());
                    context._stepOutputs[stepId] = outputJson;
                }
            }
        }

        if (json.RootElement.TryGetProperty("userPreferences", out var preferencesElement))
        {
            foreach (var prop in preferencesElement.EnumerateObject())
            {
                context._userPreferences[prop.Name] = prop.Value.GetString() ?? string.Empty;
            }
        }

        if (json.RootElement.TryGetProperty("artifactReferences", out var artifactsElement))
        {
            foreach (var prop in artifactsElement.EnumerateObject())
            {
                context._artifactReferences[prop.Name] = prop.Value.GetString() ?? string.Empty;
            }
        }

        if (json.RootElement.TryGetProperty("decisionHistory", out var decisionsElement))
        {
            foreach (var item in decisionsElement.EnumerateArray())
            {
                var decision = JsonSerializer.Deserialize<WorkflowDecision>(item.GetRawText());
                if (decision != null)
                {
                    context._decisionHistory.Add(decision);
                }
            }
        }

        return context;
    }

    private void IncrementVersion()
    {
        Interlocked.Increment(ref _version);
    }
}
