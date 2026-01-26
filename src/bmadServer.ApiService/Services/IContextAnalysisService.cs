using bmadServer.ApiService.Data.Entities;

namespace bmadServer.ApiService.Services;

public interface IContextAnalysisService
{
    ContentContext AnalyzeContext(string content, string? workflowStep = null);
    bool ShouldTranslateForHybrid(ContentContext context);
}

public record ContentContext
{
    public required string ContentType { get; init; } // "technical", "business", "mixed"
    public int TechnicalIndicatorCount { get; init; }
    public int BusinessIndicatorCount { get; init; }
    public List<string> TechnicalKeywords { get; init; } = new();
    public List<string> BusinessKeywords { get; init; } = new();
    public string? WorkflowStepType { get; init; }
    public string? AdaptationReason { get; init; }
}
