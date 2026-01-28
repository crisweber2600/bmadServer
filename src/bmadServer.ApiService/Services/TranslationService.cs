using System.Text.RegularExpressions;
using bmadServer.ApiService.Data;
using bmadServer.ApiService.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace bmadServer.ApiService.Services;

public class TranslationService : ITranslationService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IContextAnalysisService _contextAnalysisService;
    private readonly ILogger<TranslationService> _logger;
    private readonly IMemoryCache _cache;
    private const string CacheKey = "TranslationMappings";
    private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(5);

    public TranslationService(
        ApplicationDbContext dbContext, 
        IContextAnalysisService contextAnalysisService,
        ILogger<TranslationService> logger,
        IMemoryCache cache)
    {
        _dbContext = dbContext;
        _contextAnalysisService = contextAnalysisService;
        _logger = logger;
        _cache = cache;
    }

    public async Task<TranslationResult> TranslateToBusinessLanguageAsync(string technicalContent, PersonaType personaType, string? workflowStep = null)
    {
        // Analyze context
        var context = _contextAnalysisService.AnalyzeContext(technicalContent, workflowStep);

        // Technical persona: always return original
        if (personaType == PersonaType.Technical)
        {
            return new TranslationResult
            {
                Content = technicalContent,
                OriginalContent = technicalContent,
                WasTranslated = false,
                PersonaType = personaType,
                Context = context,
                AdaptationReason = "Technical persona: no translation applied"
            };
        }

        // Business persona: always translate
        // Hybrid persona: translate based on context
        bool shouldTranslate = personaType == PersonaType.Business || 
                               (personaType == PersonaType.Hybrid && _contextAnalysisService.ShouldTranslateForHybrid(context));

        if (shouldTranslate)
        {
            await EnsureCacheLoadedAsync();
            
            if (!_cache.TryGetValue(CacheKey, out Dictionary<string, string>? translationCache) || 
                translationCache == null || 
                translationCache.Count == 0)
            {
                _logger.LogWarning("No translation mappings available");
                return new TranslationResult
                {
                    Content = technicalContent,
                    OriginalContent = technicalContent,
                    WasTranslated = false,
                    PersonaType = personaType,
                    Context = context,
                    AdaptationReason = "No translation mappings available"
                };
            }

            var translatedContent = technicalContent;

            // Sort by length descending to replace longer phrases first
            foreach (var (technicalTerm, businessTerm) in translationCache.OrderByDescending(kvp => kvp.Key.Length))
            {
                var pattern = $@"\b{Regex.Escape(technicalTerm)}\b";
                // Escape the replacement string to prevent regex backreference injection
                var safeReplacement = businessTerm.Replace("$", "$$");
                translatedContent = Regex.Replace(
                    translatedContent,
                    pattern,
                    safeReplacement,
                    RegexOptions.IgnoreCase
                );
            }

            var wasTranslated = translatedContent != technicalContent;
            var adaptationReason = personaType == PersonaType.Hybrid
                ? $"Hybrid mode: {context.AdaptationReason} - translation applied"
                : $"Business persona: translation applied";

            _logger.LogDebug("Translated content for {PersonaType} persona (Changed: {WasTranslated})", personaType, wasTranslated);
            
            return new TranslationResult
            {
                Content = translatedContent,
                OriginalContent = technicalContent,
                WasTranslated = wasTranslated,
                PersonaType = personaType,
                Context = context,
                AdaptationReason = adaptationReason
            };
        }

        // Hybrid mode but content is business-oriented: no translation
        return new TranslationResult
        {
            Content = technicalContent,
            OriginalContent = technicalContent,
            WasTranslated = false,
            PersonaType = personaType,
            Context = context,
            AdaptationReason = $"Hybrid mode: {context.AdaptationReason} - no translation needed"
        };
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
        if (_cache.TryGetValue(CacheKey, out Dictionary<string, string>? cachedMappings) && cachedMappings != null)
        {
            return;
        }

        var mappings = await _dbContext.TranslationMappings
            .Where(m => m.IsActive)
            .ToListAsync();

        var dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        
        // Handle potential duplicates by keeping the first occurrence
        foreach (var mapping in mappings)
        {
            if (!dictionary.ContainsKey(mapping.TechnicalTerm))
            {
                dictionary[mapping.TechnicalTerm] = mapping.BusinessTerm;
            }
            else
            {
                _logger.LogWarning("Duplicate TechnicalTerm found: {Term}. Using first occurrence.", mapping.TechnicalTerm);
            }
        }

        _cache.Set(CacheKey, dictionary, _cacheExpiry);
        _logger.LogDebug("Loaded {Count} translation mappings into cache", dictionary.Count);
    }

    private void InvalidateCache()
    {
        _cache.Remove(CacheKey);
        _logger.LogDebug("Translation cache invalidated");
    }
}
