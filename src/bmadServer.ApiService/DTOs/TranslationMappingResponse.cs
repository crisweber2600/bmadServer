namespace bmadServer.ApiService.DTOs;

public record TranslationMappingResponse
{
    public required Guid Id { get; init; }
    public required string TechnicalTerm { get; init; }
    public required string BusinessTerm { get; init; }
    public string? Context { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
