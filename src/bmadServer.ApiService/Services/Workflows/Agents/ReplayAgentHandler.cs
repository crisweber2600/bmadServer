using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace bmadServer.ApiService.Services.Workflows.Agents;

/// <summary>
/// Agent handler that records responses on first call and replays them on subsequent calls.
/// Useful for integration testing with deterministic, cached LLM responses.
/// </summary>
public class ReplayAgentHandler : IAgentHandler
{
    private readonly OpenCodeAgentHandler _liveHandler;
    private readonly string _fixturesPath;
    private readonly ILogger<ReplayAgentHandler> _logger;

    public ReplayAgentHandler(
        AgentDefinition agentDefinition,
        IOptions<OpenCodeOptions> options,
        IOptions<BmadOptions> bmadOptions,
        ILogger<ReplayAgentHandler> logger,
        ILoggerFactory loggerFactory,
        string? modelOverride = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Create the underlying live handler for recording new responses
        _liveHandler = new OpenCodeAgentHandler(
            agentDefinition,
            options,
            loggerFactory.CreateLogger<OpenCodeAgentHandler>(),
            modelOverride);

        // Determine fixtures path
        var basePath = string.IsNullOrEmpty(bmadOptions.Value.BasePath)
            ? Directory.GetCurrentDirectory()
            : bmadOptions.Value.BasePath;

        _fixturesPath = Path.Combine(basePath, "test-fixtures", "agent-responses");
        Directory.CreateDirectory(_fixturesPath);
    }

    /// <inheritdoc />
    public async Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken cancellationToken = default)
    {
        var cacheKey = GenerateCacheKey(context);
        var cachePath = Path.Combine(_fixturesPath, $"{cacheKey}.json");

        // Try to load from cache first
        if (File.Exists(cachePath))
        {
            _logger.LogDebug("Replaying cached response for {CacheKey}", cacheKey);
            var cachedJson = await File.ReadAllTextAsync(cachePath, cancellationToken);
            return DeserializeResult(cachedJson);
        }

        // Execute live and cache the result
        _logger.LogInformation("Recording new response for {CacheKey}", cacheKey);
        var result = await _liveHandler.ExecuteAsync(context, cancellationToken);

        // Only cache successful results
        if (result.Success)
        {
            var serialized = SerializeResult(result);
            await File.WriteAllTextAsync(cachePath, serialized, cancellationToken);
        }

        return result;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<StepProgress> ExecuteWithStreamingAsync(
        AgentContext context,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // For streaming, we just delegate to the live handler
        // Replay mode doesn't support streaming (too complex to serialize)
        await foreach (var progress in _liveHandler.ExecuteWithStreamingAsync(context, cancellationToken))
        {
            yield return progress;
        }
    }

    private string GenerateCacheKey(AgentContext context)
    {
        // Create a deterministic hash based on the context
        var keyMaterial = new StringBuilder();
        keyMaterial.Append(context.StepId);
        keyMaterial.Append('|');
        keyMaterial.Append(context.StepName);
        keyMaterial.Append('|');
        keyMaterial.Append(context.UserInput ?? "");

        // Include step parameters in the hash
        if (context.StepParameters != null)
        {
            keyMaterial.Append('|');
            keyMaterial.Append(context.StepParameters.RootElement.ToString());
        }

        // Generate a short hash
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(keyMaterial.ToString()));
        var hashString = Convert.ToHexString(hash)[..16].ToLowerInvariant();

        // Create a readable filename with the hash
        var safeName = MakeSafeFileName(context.StepName);
        return $"{safeName}-{hashString}";
    }

    private static string MakeSafeFileName(string name)
    {
        var safe = new StringBuilder();
        foreach (var c in name.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(c) || c == '-' || c == '_')
            {
                safe.Append(c);
            }
            else if (c == ' ')
            {
                safe.Append('-');
            }
        }
        return safe.ToString();
    }

    private static string SerializeResult(AgentResult result)
    {
        var wrapper = new CachedAgentResult
        {
            Success = result.Success,
            OutputJson = result.Output?.RootElement.ToString(),
            ErrorMessage = result.ErrorMessage,
            IsRetryable = result.IsRetryable,
            ConfidenceScore = result.ConfidenceScore,
            Reasoning = result.Reasoning,
            CachedAt = DateTime.UtcNow
        };

        return JsonSerializer.Serialize(wrapper, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    private static AgentResult DeserializeResult(string json)
    {
        var wrapper = JsonSerializer.Deserialize<CachedAgentResult>(json)
            ?? throw new InvalidOperationException("Failed to deserialize cached result");

        JsonDocument? output = null;
        if (!string.IsNullOrEmpty(wrapper.OutputJson))
        {
            output = JsonDocument.Parse(wrapper.OutputJson);
        }

        return new AgentResult
        {
            Success = wrapper.Success,
            Output = output,
            ErrorMessage = wrapper.ErrorMessage,
            IsRetryable = wrapper.IsRetryable,
            ConfidenceScore = wrapper.ConfidenceScore,
            Reasoning = wrapper.Reasoning
        };
    }

    /// <summary>
    /// Clears all cached responses for testing purposes
    /// </summary>
    public void ClearCache()
    {
        if (Directory.Exists(_fixturesPath))
        {
            foreach (var file in Directory.GetFiles(_fixturesPath, "*.json"))
            {
                File.Delete(file);
            }
            _logger.LogInformation("Cleared agent response cache at {Path}", _fixturesPath);
        }
    }
}

/// <summary>
/// Serializable wrapper for cached agent results
/// </summary>
internal class CachedAgentResult
{
    public bool Success { get; set; }
    public string? OutputJson { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsRetryable { get; set; }
    public double ConfidenceScore { get; set; }
    public string? Reasoning { get; set; }
    public DateTime CachedAt { get; set; }
}
