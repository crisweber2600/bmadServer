using bmadServer.ApiService.Data;
using bmadServer.ApiService.Data.Entities;
using bmadServer.ApiService.Services;
using bmadServer.Tests.Helpers;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace bmadServer.Tests.Unit;

public class ContextAwareResponseAdaptationTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly SqliteConnection _connection;
    private readonly Mock<ILogger<TranslationService>> _translationLoggerMock;
    private readonly Mock<ILogger<ContextAnalysisService>> _contextLoggerMock;
    private readonly ContextAnalysisService _contextAnalysisService;
    private readonly TranslationService _translationService;
    private readonly IMemoryCache _cache;

    public ContextAwareResponseAdaptationTests()
    {
        var options = TestDatabaseHelper.CreateSqliteOptions(out _connection);
        _dbContext = new ApplicationDbContext(options);
        _dbContext.Database.EnsureCreated();
        _translationLoggerMock = new Mock<ILogger<TranslationService>>();
        _contextLoggerMock = new Mock<ILogger<ContextAnalysisService>>();
        
        _contextAnalysisService = new ContextAnalysisService(_contextLoggerMock.Object);
        _cache = new MemoryCache(new MemoryCacheOptions());
        _translationService = new TranslationService(_dbContext, _contextAnalysisService, _translationLoggerMock.Object, _cache);
        
        SeedTestData();
    }

    private void SeedTestData()
    {
        var mappings = new[]
        {
            new TranslationMapping { TechnicalTerm = "API", BusinessTerm = "system connection", IsActive = true },
            new TranslationMapping { TechnicalTerm = "database", BusinessTerm = "data storage", IsActive = true },
            new TranslationMapping { TechnicalTerm = "microservices", BusinessTerm = "modular components", IsActive = true },
        };

        _dbContext.TranslationMappings.AddRange(mappings);
        _dbContext.SaveChanges();
    }

    [Fact]
    public async Task HybridMode_WithTechnicalContent_TranslatesToBusiness()
    {
        // Arrange - Technical content
        var technicalContent = "The API uses microservices architecture with database replication.";

        // Act
        var result = await _translationService.TranslateToBusinessLanguageAsync(
            technicalContent, 
            PersonaType.Hybrid,
            workflowStep: "architecture review"
        );

        // Assert
        Assert.True(result.WasTranslated);
        Assert.Contains("system connection", result.Content);
        Assert.Contains("modular components", result.Content);
        Assert.Contains("data storage", result.Content);
        Assert.NotNull(result.Context);
        Assert.Equal("technical", result.Context.ContentType);
        Assert.Contains("Hybrid mode", result.AdaptationReason);
    }

    [Fact]
    public async Task HybridMode_WithBusinessContent_NoTranslation()
    {
        // Arrange - Business content
        var businessContent = "The product strategy focuses on user satisfaction and market growth.";

        // Act
        var result = await _translationService.TranslateToBusinessLanguageAsync(
            businessContent, 
            PersonaType.Hybrid,
            workflowStep: "PRD review"
        );

        // Assert
        Assert.False(result.WasTranslated);
        Assert.Equal(businessContent, result.Content);
        Assert.NotNull(result.Context);
        Assert.Equal("business", result.Context.ContentType);
        Assert.Contains("no translation needed", result.AdaptationReason);
    }

    [Fact]
    public void ContextAnalysis_IdentifiesTechnicalContent()
    {
        // Arrange
        var technicalContent = "Configure the API endpoint with database authentication and microservices integration.";

        // Act
        var context = _contextAnalysisService.AnalyzeContext(technicalContent);

        // Assert
        Assert.Equal("technical", context.ContentType);
        Assert.True(context.TechnicalIndicatorCount > context.BusinessIndicatorCount);
        Assert.Contains("API", context.TechnicalKeywords);
        Assert.Contains("database", context.TechnicalKeywords);
        Assert.Contains("microservices", context.TechnicalKeywords);
    }

    [Fact]
    public void ContextAnalysis_IdentifiesBusinessContent()
    {
        // Arrange
        var businessContent = "The strategy aims to increase customer satisfaction and market share with improved user experience.";

        // Act
        var context = _contextAnalysisService.AnalyzeContext(businessContent);

        // Assert
        Assert.Equal("business", context.ContentType);
        Assert.True(context.BusinessIndicatorCount > context.TechnicalIndicatorCount);
        Assert.Contains("strategy", context.BusinessKeywords);
        Assert.Contains("customer", context.BusinessKeywords);
        Assert.Contains("user", context.BusinessKeywords);
    }

    [Fact]
    public void ContextAnalysis_WorkflowStepInfluencesContext()
    {
        // Arrange
        var mixedContent = "Review the implementation details and user impact.";

        // Act - Technical workflow step
        var technicalContext = _contextAnalysisService.AnalyzeContext(mixedContent, "implementation review");

        // Assert - Workflow step type should be technical
        Assert.Equal("technical", technicalContext.WorkflowStepType);
        
        // Technical indicators should be boosted by workflow step (+2)
        // The word "implementation" is in TechnicalWorkflowSteps, so it gets +2 boost
        Assert.True(technicalContext.TechnicalIndicatorCount >= 2);
    }

    [Fact]
    public async Task HybridMode_ResultIncludesAdaptationDetails()
    {
        // Arrange
        var content = "The API microservices provide improved scalability for users.";

        // Act
        var result = await _translationService.TranslateToBusinessLanguageAsync(
            content, 
            PersonaType.Hybrid
        );

        // Assert
        Assert.NotNull(result.Context);
        Assert.NotNull(result.AdaptationReason);
        Assert.Equal(content, result.OriginalContent);
        Assert.Contains("Hybrid mode", result.AdaptationReason);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }
}
