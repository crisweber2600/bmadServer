namespace bmadServer.ApiService.DTOs;

public record TranslationMappingRequest
{
    public required string TechnicalTerm { get; init; }
    public required string BusinessTerm { get; init; }
    public string? Context { get; init; }
}
