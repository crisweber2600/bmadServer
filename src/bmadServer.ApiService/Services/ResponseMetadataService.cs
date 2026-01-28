using bmadServer.ApiService.Constants;
using bmadServer.ApiService.Data.Entities;
using Microsoft.Extensions.Logging;

namespace bmadServer.ApiService.Services;

public class ResponseMetadataService : IResponseMetadataService
{
    private readonly ILogger<ResponseMetadataService> _logger;

    public ResponseMetadataService(ILogger<ResponseMetadataService> logger)
    {
        _logger = logger;
    }

    public ResponseMetadata CreateMetadata(string content, PersonaType personaType, bool wasTranslated)
    {
        var technicalTermsFound = PersonaKeywords.TechnicalKeywords
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
