using bmadServer.ApiService.Data.Entities;

namespace bmadServer.ApiService.Services;

public interface IResponseMetadataService
{
    ResponseMetadata CreateMetadata(string content, PersonaType personaType, bool wasTranslated);
}

public record ResponseMetadata
{
    public required string ContentType { get; init; }
    public required PersonaType PersonaType { get; init; }
    public bool WasTranslated { get; init; }
    public bool HasTechnicalDetails { get; init; }
    public List<string> TechnicalTermsFound { get; init; } = new();
}
