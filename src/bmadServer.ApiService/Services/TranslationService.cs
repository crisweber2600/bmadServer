using System.Text.RegularExpressions;
using bmadServer.ApiService.Data;
using bmadServer.ApiService.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace bmadServer.ApiService.Services;

public class TranslationService : ITranslationService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<TranslationService> _logger;
    private Dictionary<string, string>? _translationCache;
    private DateTime _cacheLastUpdated = DateTime.MinValue;
    private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(5);

    public TranslationService(ApplicationDbContext dbContext, ILogger<TranslationService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<string> TranslateToBusinessLanguageAsync(string technicalContent, PersonaType personaType)
    {
        if (personaType == PersonaType.Technical)
        {
            return technicalContent;
        }

        if (personaType == PersonaType.Business || personaType == PersonaType.Hybrid)
        {
            await EnsureCacheLoadedAsync();
            
            if (_translationCache == null || _translationCache.Count == 0)
            {
                _logger.LogWarning("No translation mappings available");
                return technicalContent;
            }

            var translatedContent = technicalContent;

            // Sort by length descending to replace longer phrases first (e.g., "409 Conflict" before "API")
            foreach (var (technicalTerm, businessTerm) in _translationCache.OrderByDescending(kvp => kvp.Key.Length))
            {
                // Use word boundaries for whole-word matching to avoid partial replacements
                var pattern = $@"\b{Regex.Escape(technicalTerm)}\b";
                translatedContent = Regex.Replace(
                    translatedContent,
                    pattern,
                    businessTerm,
                    RegexOptions.IgnoreCase
                );
            }

            _logger.LogDebug("Translated content for {PersonaType} persona", personaType);
            return translatedContent;
        }

        return technicalContent;
    }

    public async Task<IEnumerable<TranslationMapping>> GetTranslationMappingsAsync()
    {
        return await _dbContext.TranslationMappings
            .Where(m => m.IsActive)
            .OrderBy(m => m.TechnicalTerm)
            .ToListAsync();
    }

    public async Task<TranslationMapping> AddTranslationMappingAsync(string technicalTerm, string businessTerm, string? context = null)
    {
        var mapping = new TranslationMapping
        {
            TechnicalTerm = technicalTerm,
            BusinessTerm = businessTerm,
            Context = context,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.TranslationMappings.Add(mapping);
        await _dbContext.SaveChangesAsync();
        
        InvalidateCache();
        _logger.LogInformation("Added translation mapping: {TechnicalTerm} -> {BusinessTerm}", technicalTerm, businessTerm);
        
        return mapping;
    }

    public async Task<TranslationMapping> UpdateTranslationMappingAsync(Guid id, string technicalTerm, string businessTerm, string? context = null)
    {
        var mapping = await _dbContext.TranslationMappings.FindAsync(id);
        if (mapping == null)
        {
            throw new InvalidOperationException($"Translation mapping with ID {id} not found");
        }

        mapping.TechnicalTerm = technicalTerm;
        mapping.BusinessTerm = businessTerm;
        mapping.Context = context;
        mapping.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
        
        InvalidateCache();
        _logger.LogInformation("Updated translation mapping {Id}: {TechnicalTerm} -> {BusinessTerm}", id, technicalTerm, businessTerm);
        
        return mapping;
    }

    public async Task<bool> DeleteTranslationMappingAsync(Guid id)
    {
        var mapping = await _dbContext.TranslationMappings.FindAsync(id);
        if (mapping == null)
        {
            return false;
        }

        _dbContext.TranslationMappings.Remove(mapping);
        await _dbContext.SaveChangesAsync();
        
        InvalidateCache();
        _logger.LogInformation("Deleted translation mapping {Id}: {TechnicalTerm}", id, mapping.TechnicalTerm);
        
        return true;
    }

    private async Task EnsureCacheLoadedAsync()
    {
        if (_translationCache != null && DateTime.UtcNow - _cacheLastUpdated < _cacheExpiry)
        {
            return;
        }

        var mappings = await _dbContext.TranslationMappings
            .Where(m => m.IsActive)
            .ToListAsync();

        _translationCache = mappings.ToDictionary(
            m => m.TechnicalTerm,
            m => m.BusinessTerm,
            StringComparer.OrdinalIgnoreCase
        );

        _cacheLastUpdated = DateTime.UtcNow;
        _logger.LogDebug("Loaded {Count} translation mappings into cache", _translationCache.Count);
    }

    private void InvalidateCache()
    {
        _translationCache = null;
        _cacheLastUpdated = DateTime.MinValue;
    }
}
