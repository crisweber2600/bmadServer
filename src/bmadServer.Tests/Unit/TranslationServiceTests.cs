using bmadServer.ApiService.Data;
using bmadServer.ApiService.Data.Entities;
using bmadServer.ApiService.Services;
using bmadServer.Tests.Helpers;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace bmadServer.Tests.Unit;

public class TranslationServiceTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly SqliteConnection _connection;
    private readonly Mock<ILogger<TranslationService>> _loggerMock;
    private readonly Mock<ILogger<ContextAnalysisService>> _contextLoggerMock;
    private readonly ContextAnalysisService _contextAnalysisService;
    private readonly TranslationService _translationService;

    public TranslationServiceTests()
    {
        var options = TestDatabaseHelper.CreateSqliteOptions(out _connection);
        _dbContext = new ApplicationDbContext(options);
        _dbContext.Database.EnsureCreated();
        _loggerMock = new Mock<ILogger<TranslationService>>();
        _contextLoggerMock = new Mock<ILogger<ContextAnalysisService>>();
        _contextAnalysisService = new ContextAnalysisService(_contextLoggerMock.Object);
        _translationService = new TranslationService(_dbContext, _contextAnalysisService, _loggerMock.Object);
        
        SeedTestData();
    }

    private void SeedTestData()
    {
        var mappings = new[]
        {
            new TranslationMapping { TechnicalTerm = "API", BusinessTerm = "system connection", IsActive = true },
            new TranslationMapping { TechnicalTerm = "database", BusinessTerm = "data storage", IsActive = true },
            new TranslationMapping { TechnicalTerm = "endpoint", BusinessTerm = "connection point", IsActive = true },
            new TranslationMapping { TechnicalTerm = "caching", BusinessTerm = "temporary storage for faster access", IsActive = true },
            new TranslationMapping { TechnicalTerm = "cache", BusinessTerm = "temporary storage", IsActive = true },
            new TranslationMapping { TechnicalTerm = "authentication", BusinessTerm = "identity verification", IsActive = true },
            new TranslationMapping { TechnicalTerm = "authorization", BusinessTerm = "permission check", IsActive = true },
            new TranslationMapping { TechnicalTerm = "409 Conflict", BusinessTerm = "another team member is editing this", IsActive = true },
            new TranslationMapping { TechnicalTerm = "CDN caching layer", BusinessTerm = "faster loading times for users", IsActive = true },
            new TranslationMapping { TechnicalTerm = "microservices", BusinessTerm = "modular application components", IsActive = true },
            new TranslationMapping { TechnicalTerm = "deprecated", BusinessTerm = "no longer supported", IsActive = false }
        };

        _dbContext.TranslationMappings.AddRange(mappings);
        _dbContext.SaveChanges();
    }

    [Fact]
    public async Task TranslateToBusinessLanguage_WithBusinessPersona_TranslatesTechnicalTerms()
    {
        // Arrange
        var technicalContent = "The API endpoint uses database caching for better performance.";

        // Act
        var translationResult = await _translationService.TranslateToBusinessLanguageAsync(technicalContent, PersonaType.Business);

        // Assert
        Assert.Contains("system connection", translationResult.Content);
        Assert.Contains("connection point", translationResult.Content);
        Assert.Contains("data storage", translationResult.Content);
        Assert.Contains("temporary storage", translationResult.Content);
        Assert.DoesNotContain("API", translationResult.Content);
        Assert.DoesNotContain("endpoint", translationResult.Content);
        Assert.DoesNotContain("database", translationResult.Content);
    }

    [Fact]
    public async Task TranslateToBusinessLanguage_WithTechnicalPersona_ReturnsOriginalContent()
    {
        // Arrange
        var technicalContent = "The API endpoint uses database caching for better performance.";

        // Act
        var translationResult = await _translationService.TranslateToBusinessLanguageAsync(technicalContent, PersonaType.Technical);

        // Assert
        Assert.Equal(technicalContent, translationResult.Content);
    }

    [Fact]
    public async Task TranslateToBusinessLanguage_WithErrorMessage_TranslatesToPlainLanguage()
    {
        // Arrange
        var errorContent = "Failed with 409 Conflict: optimistic concurrency violation";

        // Act
        var translationResult = await _translationService.TranslateToBusinessLanguageAsync(errorContent, PersonaType.Business);

        // Assert
        Assert.Contains("another team member is editing this", translationResult.Content);
        Assert.DoesNotContain("409 Conflict", translationResult.Content);
    }

    [Fact]
    public async Task TranslateToBusinessLanguage_WithArchitectureDecision_TranslatesToBusinessImpact()
    {
        // Arrange
        var architectureContent = "Implementing CDN caching layer to improve response times.";

        // Act
        var translationResult = await _translationService.TranslateToBusinessLanguageAsync(architectureContent, PersonaType.Business);

        // Assert
        Assert.Contains("faster loading times for users", translationResult.Content);
        Assert.DoesNotContain("CDN caching layer", translationResult.Content);
    }

    [Fact]
    public async Task TranslateToBusinessLanguage_OnlyUsesActiveMappings()
    {
        // Arrange
        var content = "This API is deprecated and uses microservices.";

        // Act
        var translationResult = await _translationService.TranslateToBusinessLanguageAsync(content, PersonaType.Business);

        // Assert
        Assert.Contains("modular application components", translationResult.Content);
        Assert.Contains("deprecated", translationResult.Content); // Should not be translated (inactive)
    }

    [Fact]
    public async Task TranslateToBusinessLanguage_CaseInsensitiveMatching()
    {
        // Arrange
        var content = "The api uses DATABASE and caching.";

        // Act
        var translationResult = await _translationService.TranslateToBusinessLanguageAsync(content, PersonaType.Business);

        // Assert
        Assert.Contains("system connection", translationResult.Content);
        Assert.Contains("data storage", translationResult.Content);
        Assert.Contains("temporary storage for faster access", translationResult.Content);
    }

    [Fact]
    public async Task GetTranslationMappings_ReturnsAllActiveMappings()
    {
        // Act
        var mappings = await _translationService.GetTranslationMappingsAsync();

        // Assert
        var mappingsList = mappings.ToList();
        Assert.NotEmpty(mappingsList);
        Assert.All(mappingsList, m => Assert.True(m.IsActive));
    }

    [Fact]
    public async Task AddTranslationMapping_CreatesNewMapping()
    {
        // Act
        var mapping = await _translationService.AddTranslationMappingAsync(
            "REST API",
            "web service",
            "HTTP communication"
        );

        // Assert
        Assert.NotEqual(Guid.Empty, mapping.Id);
        Assert.Equal("REST API", mapping.TechnicalTerm);
        Assert.Equal("web service", mapping.BusinessTerm);
        Assert.Equal("HTTP communication", mapping.Context);
        Assert.True(mapping.IsActive);

        var saved = await _dbContext.TranslationMappings.FindAsync(mapping.Id);
        Assert.NotNull(saved);
    }

    [Fact]
    public async Task UpdateTranslationMapping_UpdatesExistingMapping()
    {
        // Arrange
        var existingMapping = await _dbContext.TranslationMappings.FirstAsync(m => m.TechnicalTerm == "API");
        var originalCreatedAt = existingMapping.CreatedAt;

        // Act
        var updated = await _translationService.UpdateTranslationMappingAsync(
            existingMapping.Id,
            "API",
            "digital connection",
            "updated context"
        );

        // Assert
        Assert.Equal("digital connection", updated.BusinessTerm);
        Assert.Equal("updated context", updated.Context);
        Assert.Equal(originalCreatedAt, updated.CreatedAt);
        Assert.True(updated.UpdatedAt > originalCreatedAt);
    }

    [Fact]
    public async Task DeleteTranslationMapping_RemovesMapping()
    {
        // Arrange
        var existingMapping = await _dbContext.TranslationMappings.FirstAsync(m => m.TechnicalTerm == "API");

        // Act
        var result = await _translationService.DeleteTranslationMappingAsync(existingMapping.Id);

        // Assert
        Assert.True(result);
        var deleted = await _dbContext.TranslationMappings.FindAsync(existingMapping.Id);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteTranslationMapping_WithInvalidId_ReturnsFalse()
    {
        // Act
        var result = await _translationService.DeleteTranslationMappingAsync(Guid.NewGuid());

        // Assert
        Assert.False(result);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }
}
