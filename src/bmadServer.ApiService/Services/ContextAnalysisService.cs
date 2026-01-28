using bmadServer.ApiService.Constants;
using bmadServer.ApiService.Data.Entities;
using Microsoft.Extensions.Logging;

namespace bmadServer.ApiService.Services;

public class ContextAnalysisService : IContextAnalysisService
{
    private readonly ILogger<ContextAnalysisService> _logger;

    public ContextAnalysisService(ILogger<ContextAnalysisService> logger)
    {
        _logger = logger;
    }

    public ContentContext AnalyzeContext(string content, string? workflowStep = null)
    {
        var technicalKeywords = PersonaKeywords.TechnicalKeywords
            .Where(keyword => content.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            .Distinct()
            .ToList();

        var businessKeywords = PersonaKeywords.BusinessKeywords
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
            if (PersonaKeywords.TechnicalWorkflowSteps.Any(s => workflowStep.Contains(s, StringComparison.OrdinalIgnoreCase)))
            {
                stepType = "technical";
                technicalCount += 2;
            }
            else if (PersonaKeywords.BusinessWorkflowSteps.Any(s => workflowStep.Contains(s, StringComparison.OrdinalIgnoreCase)))
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
