using bmadServer.ApiService.Data.Entities;
using Microsoft.Extensions.Logging;

namespace bmadServer.ApiService.Services;

public class ContextAnalysisService : IContextAnalysisService
{
    private readonly ILogger<ContextAnalysisService> _logger;

    // Technical indicators
    private static readonly string[] TechnicalKeywords = new[]
    {
        "API", "endpoint", "REST", "GraphQL", "HTTP", "HTTPS", "JSON", "XML",
        "database", "SQL", "query", "schema", "migration", "index",
        "cache", "Redis", "memcached", "CDN",
        "authentication", "authorization", "JWT", "OAuth", "token", "session",
        "microservices", "service", "container", "Docker", "Kubernetes", "pod",
        "architecture", "infrastructure", "deployment", "CI/CD", "pipeline",
        "algorithm", "data structure", "complexity", "performance", "optimization",
        "latency", "throughput", "bandwidth", "scalability", "load balancing",
        "code", "function", "class", "method", "interface", "abstract",
        "version", "dependency", "library", "framework", "SDK", "package",
        "git", "commit", "branch", "merge", "pull request", "repository",
        "test", "unit test", "integration test", "TDD", "BDD", "mock",
        "bug", "error", "exception", "stack trace", "debugging", "logging"
    };

    // Business indicators
    private static readonly string[] BusinessKeywords = new[]
    {
        "user", "customer", "client", "stakeholder", "team member",
        "business", "revenue", "cost", "profit", "ROI", "budget",
        "strategy", "goal", "objective", "KPI", "metric", "target",
        "market", "competition", "advantage", "opportunity", "risk",
        "product", "feature", "requirement", "specification", "use case",
        "workflow", "process", "procedure", "policy", "compliance",
        "decision", "approval", "review", "feedback", "iteration",
        "timeline", "deadline", "milestone", "deliverable", "schedule",
        "impact", "benefit", "value", "priority", "importance",
        "collaboration", "communication", "meeting", "presentation", "report",
        "quality", "satisfaction", "experience", "engagement", "adoption",
        "growth", "scale", "expansion", "innovation", "transformation"
    };

    // Technical workflow steps
    private static readonly string[] TechnicalWorkflowSteps = new[]
    {
        "architecture", "implementation", "code review", "testing",
        "deployment", "configuration", "debugging", "optimization",
        "integration", "migration", "refactoring", "setup"
    };

    // Business workflow steps
    private static readonly string[] BusinessWorkflowSteps = new[]
    {
        "planning", "requirements", "PRD", "specification", "review",
        "approval", "strategy", "analysis", "evaluation", "decision",
        "presentation", "documentation", "training"
    };

    public ContextAnalysisService(ILogger<ContextAnalysisService> logger)
    {
        _logger = logger;
    }

    public ContentContext AnalyzeContext(string content, string? workflowStep = null)
    {
        var technicalKeywords = TechnicalKeywords
            .Where(keyword => content.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            .Distinct()
            .ToList();

        var businessKeywords = BusinessKeywords
            .Where(keyword => content.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            .Distinct()
            .ToList();

        var technicalCount = technicalKeywords.Count;
        var businessCount = businessKeywords.Count;

        // Check for code blocks (strong technical indicator)
        if (content.Contains("```"))
        {
            technicalCount += 3;
        }

        // Analyze workflow step if provided
        string? stepType = null;
        if (!string.IsNullOrEmpty(workflowStep))
        {
            if (TechnicalWorkflowSteps.Any(s => workflowStep.Contains(s, StringComparison.OrdinalIgnoreCase)))
            {
                stepType = "technical";
                technicalCount += 2;
            }
            else if (BusinessWorkflowSteps.Any(s => workflowStep.Contains(s, StringComparison.OrdinalIgnoreCase)))
            {
                stepType = "business";
                businessCount += 2;
            }
        }

        // Determine content type
        string contentType;
        string? adaptationReason;

        if (technicalCount > businessCount * 2)
        {
            contentType = "technical";
            adaptationReason = technicalCount > 5 
                ? $"High technical content detected ({technicalCount} technical indicators)"
                : "Content leans technical";
        }
        else if (businessCount > technicalCount * 2)
        {
            contentType = "business";
            adaptationReason = businessCount > 5
                ? $"High business content detected ({businessCount} business indicators)"
                : "Content leans business";
        }
        else
        {
            contentType = "mixed";
            adaptationReason = "Balanced technical and business content";
        }

        _logger.LogDebug(
            "Analyzed context: Type={ContentType}, Technical={TechnicalCount}, Business={BusinessCount}",
            contentType, technicalCount, businessCount);

        return new ContentContext
        {
            ContentType = contentType,
            TechnicalIndicatorCount = technicalCount,
            BusinessIndicatorCount = businessCount,
            TechnicalKeywords = technicalKeywords,
            BusinessKeywords = businessKeywords,
            WorkflowStepType = stepType,
            AdaptationReason = adaptationReason
        };
    }

    public bool ShouldTranslateForHybrid(ContentContext context)
    {
        // For Hybrid mode:
        // - Translate technical content to business language
        // - Keep business content as is
        // - Mixed content: translate if more technical than business
        return context.ContentType == "technical" || 
               (context.ContentType == "mixed" && context.TechnicalIndicatorCount > context.BusinessIndicatorCount);
    }
}
