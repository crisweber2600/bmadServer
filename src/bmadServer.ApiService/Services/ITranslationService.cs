using bmadServer.ApiService.Data.Entities;

namespace bmadServer.ApiService.Services;

public interface ITranslationService
{
    Task<TranslationResult> TranslateToBusinessLanguageAsync(string technicalContent, PersonaType personaType);
    Task<IEnumerable<TranslationMapping>> GetTranslationMappingsAsync();
    Task<TranslationMapping> AddTranslationMappingAsync(string technicalTerm, string businessTerm, string? context = null);
    Task<TranslationMapping> UpdateTranslationMappingAsync(Guid id, string technicalTerm, string businessTerm, string? context = null);
    Task<bool> DeleteTranslationMappingAsync(Guid id);
}

public record TranslationResult
{
    public required string Content { get; init; }
    public required string OriginalContent { get; init; }
    public bool WasTranslated { get; init; }
    public PersonaType PersonaType { get; init; }
}

