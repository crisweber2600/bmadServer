using bmadServer.ApiService.Data.Entities;
using Microsoft.Extensions.Logging;

namespace bmadServer.ApiService.Services;

public class ResponseMetadataService : IResponseMetadataService
{
    private readonly ILogger<ResponseMetadataService> _logger;

    // Technical indicators
    private static readonly string[] TechnicalKeywords = new[]
    {
        "API", "endpoint", "REST", "HTTP", "HTTPS", "JSON", "XML",
        "database", "SQL", "query", "cache", "Redis", "PostgreSQL",
        "authentication", "authorization", "JWT", "OAuth", "token",
        "microservices", "service", "container", "Docker", "Kubernetes",
        "architecture", "infrastructure", "deployment", "CI/CD",
        "algorithm", "data structure", "complexity", "performance",
        "latency", "throughput", "bandwidth", "scalability",
        "code", "function", "class", "method", "interface",
        "version", "dependency", "library", "framework", "SDK"
    };

    public ResponseMetadataService(ILogger<ResponseMetadataService> logger)
    {
        _logger = logger;
    }

    public ResponseMetadata CreateMetadata(string content, PersonaType personaType, bool wasTranslated)
    {
        var technicalTermsFound = TechnicalKeywords
            .Where(keyword => content.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            .Distinct()
            .ToList();

        var hasTechnicalDetails = technicalTermsFound.Count > 0 ||
                                  content.Contains("```") || // Code blocks
                                  content.Contains("http://") ||
                                  content.Contains("https://");

        var contentType = DetermineContentType(personaType, wasTranslated, hasTechnicalDetails);

        _logger.LogDebug(
            "Created metadata: ContentType={ContentType}, PersonaType={PersonaType}, WasTranslated={WasTranslated}, TechnicalTerms={Count}",
            contentType, personaType, wasTranslated, technicalTermsFound.Count);

        return new ResponseMetadata
        {
            ContentType = contentType,
            PersonaType = personaType,
            WasTranslated = wasTranslated,
            HasTechnicalDetails = hasTechnicalDetails,
            TechnicalTermsFound = technicalTermsFound
        };
    }

    private static string DetermineContentType(PersonaType personaType, bool wasTranslated, bool hasTechnicalDetails)
    {
        return personaType switch
        {
            PersonaType.Technical => "technical",
            PersonaType.Business when wasTranslated => "business-translated",
            PersonaType.Business => "business",
            PersonaType.Hybrid when hasTechnicalDetails => "hybrid-technical",
            PersonaType.Hybrid => "hybrid-business",
            _ => "unknown"
        };
    }
}
