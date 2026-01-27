using System.Text.Json;
using Microsoft.Extensions.Logging;
using bmadServer.ApiService.Models.Workflows;

namespace bmadServer.ApiService.Services.Workflows;

/// <summary>
/// Service for managing context size to stay within token limits.
/// Implements smart summarization that preserves critical information (decisions, recent outputs)
/// while condensing older step outputs.
/// </summary>
public interface IContextSummarizationService
{
    /// <summary>
    /// Estimate token count for JSON content using character/4 approximation.
    /// This provides a fast estimate suitable for token limit checks.
    /// </summary>
    /// <param name="jsonContent">The JSON string to estimate tokens for</param>
    /// <returns>Estimated token count</returns>
    int EstimateTokenCount(string jsonContent);
    
    /// <summary>
    /// Summarize context if it exceeds token limit.
    /// Strategy:
    /// - Preserves ALL decision history (never summarized)
    /// - Preserves most recent 3 step outputs in full
    /// - Summarizes older outputs to key fields only
    /// - Returns original context if within token limit
    /// </summary>
    /// <param name="context">The shared context to potentially summarize</param>
    /// <param name="tokenLimit">Maximum token count allowed</param>
    /// <returns>Summarized context (or original if within limit)</returns>
    SharedContext SummarizeIfNeeded(SharedContext context, int tokenLimit);
}

public class ContextSummarizationService : IContextSummarizationService
{
    private readonly ILogger<ContextSummarizationService> _logger;

    public ContextSummarizationService(ILogger<ContextSummarizationService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public int EstimateTokenCount(string jsonContent)
    {
        if (string.IsNullOrWhiteSpace(jsonContent))
        {
            return 0;
        }
        
        // Approximate: 1 token ≈ 4 characters (common LLM tokenization)
        return (int)Math.Ceiling(jsonContent.Length / 4.0);
    }

    public SharedContext SummarizeIfNeeded(SharedContext context, int tokenLimit)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        var serialized = JsonSerializer.Serialize(context);
        var tokenCount = EstimateTokenCount(serialized);

        if (tokenCount <= tokenLimit)
        {
            return context;
        }

        _logger.LogInformation(
            "Context exceeds token limit: {TokenCount} > {TokenLimit}. Summarizing...",
            tokenCount, tokenLimit);

        var summarized = new SharedContext
        {
            DecisionHistory = context.DecisionHistory,
            UserPreferences = context.UserPreferences,
            ArtifactReferences = context.ArtifactReferences,
            Version = context.Version,
            LastModifiedAt = context.LastModifiedAt,
            LastModifiedBy = context.LastModifiedBy,
            StepOutputs = new Dictionary<string, JsonDocument>()
        };

        if (context.StepOutputs.Count <= 3)
        {
            summarized.StepOutputs = context.StepOutputs;
            return summarized;
        }

        var stepIds = context.StepOutputs.Keys.ToList();
        var recentCount = Math.Min(3, stepIds.Count);
        var recentStepIds = stepIds.Skip(stepIds.Count - recentCount).ToList();

        for (int i = 0; i < stepIds.Count; i++)
        {
            var stepId = stepIds[i];
            var output = context.StepOutputs[stepId];

            if (recentStepIds.Contains(stepId))
            {
                summarized.StepOutputs[stepId] = output;
            }
            else
            {
                try
                {
                    var summarizedOutput = SummarizeStepOutput(output);
                    summarized.StepOutputs[stepId] = summarizedOutput;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to summarize output for step {StepId}", stepId);
                    summarized.StepOutputs[stepId] = output;
                }
            }
        }

        var finalSerialized = JsonSerializer.Serialize(summarized);
        var finalTokenCount = EstimateTokenCount(finalSerialized);
        
        _logger.LogInformation(
            "Context summarized: {OriginalTokens} → {SummarizedTokens} tokens. Preserved {RecentCount} recent steps.",
            tokenCount, finalTokenCount, recentCount);

        return summarized;
    }

    private JsonDocument SummarizeStepOutput(JsonDocument output)
    {
        try
        {
            var element = output.RootElement;
            
            if (element.ValueKind == JsonValueKind.Object)
            {
                var summarized = new Dictionary<string, object?>();
                
                foreach (var property in element.EnumerateObject())
                {
                    if (property.Name.StartsWith("_") || property.Name.Equals("id", StringComparison.OrdinalIgnoreCase))
                    {
                        summarized[property.Name] = property.Value.GetString() ?? property.Value.ToString();
                    }
                    else if (property.Value.ValueKind == JsonValueKind.String)
                    {
                        var str = property.Value.GetString();
                        if (str?.Length > 100)
                        {
                            summarized[property.Name] = str.Substring(0, 100) + "...";
                        }
                        else
                        {
                            summarized[property.Name] = str;
                        }
                    }
                    else if (property.Value.ValueKind == JsonValueKind.Array)
                    {
                        var count = property.Value.GetArrayLength();
                        summarized[property.Name] = $"[Array with {count} items]";
                    }
                    else
                    {
                        summarized[property.Name] = property.Value.ToString();
                    }
                }

                var json = JsonSerializer.Serialize(summarized);
                return JsonDocument.Parse(json);
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse output for summarization");
        }

        return output;
    }
}
