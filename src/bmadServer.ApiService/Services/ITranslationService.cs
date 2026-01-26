using bmadServer.ApiService.Data.Entities;

namespace bmadServer.ApiService.Services;

public interface ITranslationService
{
    Task<string> TranslateToBusinessLanguageAsync(string technicalContent, PersonaType personaType);
    Task<IEnumerable<TranslationMapping>> GetTranslationMappingsAsync();
    Task<TranslationMapping> AddTranslationMappingAsync(string technicalTerm, string businessTerm, string? context = null);
    Task<TranslationMapping> UpdateTranslationMappingAsync(Guid id, string technicalTerm, string businessTerm, string? context = null);
    Task<bool> DeleteTranslationMappingAsync(Guid id);
}
