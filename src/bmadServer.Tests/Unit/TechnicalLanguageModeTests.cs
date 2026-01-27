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

public class TechnicalLanguageModeTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly SqliteConnection _connection;
    private readonly Mock<ILogger<TranslationService>> _loggerMock;
    private readonly Mock<ILogger<ContextAnalysisService>> _contextLoggerMock;
    private readonly ContextAnalysisService _contextAnalysisService;
    private readonly TranslationService _translationService;
    private readonly IMemoryCache _cache;

    public TechnicalLanguageModeTests()
    {
        var options = TestDatabaseHelper.CreateSqliteOptions(out _connection);
        _dbContext = new ApplicationDbContext(options);
        _dbContext.Database.EnsureCreated();
        _loggerMock = new Mock<ILogger<TranslationService>>();
        _contextLoggerMock = new Mock<ILogger<ContextAnalysisService>>();
        _contextAnalysisService = new ContextAnalysisService(_contextLoggerMock.Object);
        _cache = new MemoryCache(new MemoryCacheOptions());
        _translationService = new TranslationService(_dbContext, _contextAnalysisService, _loggerMock.Object, _cache);
        
        SeedTestData();
    }

    private void SeedTestData()
    {
        var mappings = new[]
        {
            new TranslationMapping { TechnicalTerm = "API", BusinessTerm = "system connection", IsActive = true },
            new TranslationMapping { TechnicalTerm = "REST endpoint", BusinessTerm = "web service point", IsActive = true },
            new TranslationMapping { TechnicalTerm = "microservices", BusinessTerm = "modular components", IsActive = true },
            new TranslationMapping { TechnicalTerm = "authentication", BusinessTerm = "identity check", IsActive = true },
        };

        _dbContext.TranslationMappings.AddRange(mappings);
        _dbContext.SaveChanges();
    }

    [Fact]
    public async Task TechnicalPersona_ReceivesFullTechnicalDetails()
    {
        // Arrange
        var technicalContent = @"Configure the REST endpoint at /api/v1/users with authentication using JWT tokens. 
The API uses microservices architecture with service discovery.";

        // Act
        var translationResult = await _translationService.TranslateToBusinessLanguageAsync(technicalContent, PersonaType.Technical);

        // Assert - Technical content should be unchanged
        Assert.Equal(technicalContent, translationResult.Content);
        Assert.Contains("REST endpoint", translationResult.Content);
        Assert.Contains("API", translationResult.Content);
        Assert.Contains("microservices", translationResult.Content);
        Assert.Contains("authentication", translationResult.Content);
    }

    [Fact]
    public async Task TechnicalPersona_ReceivesCodeSnippets()
    {
        // Arrange
        var contentWithCode = @"Here's the API configuration:
```csharp
public class ApiConfig
{
    public string Endpoint { get; set; }
    public int Timeout { get; set; }
}
```
Use REST endpoint for authentication.";

        // Act
        var translationResult = await _translationService.TranslateToBusinessLanguageAsync(contentWithCode, PersonaType.Technical);

        // Assert - Code snippets should remain intact
        Assert.Equal(contentWithCode, translationResult.Content);
        Assert.Contains("```csharp", translationResult.Content);
        Assert.Contains("public class ApiConfig", translationResult.Content);
        Assert.Contains("REST endpoint", translationResult.Content);
    }

    [Fact]
    public async Task TechnicalPersona_ReceivesVersionNumbers()
    {
        // Arrange
        var contentWithVersions = "Use .NET 10.0 with Entity Framework Core 10.0.1 and configure the API endpoint.";

        // Act
        var translationResult = await _translationService.TranslateToBusinessLanguageAsync(contentWithVersions, PersonaType.Technical);

        // Assert - Version numbers should be preserved
        Assert.Equal(contentWithVersions, translationResult.Content);
        Assert.Contains(".NET 10.0", translationResult.Content);
        Assert.Contains("Entity Framework Core 10.0.1", translationResult.Content);
        Assert.Contains("API", translationResult.Content);
    }

    [Fact]
    public async Task TechnicalPersona_ReceivesArchitectureDetails()
    {
        // Arrange
        var architectureContent = @"Architecture overview:
- API Gateway: Kong (version 3.5)
- Microservices: .NET 10.0
- Authentication: OAuth2 with JWT
- Database: PostgreSQL 16
- Cache: Redis 7.2";

        // Act
        var translationResult = await _translationService.TranslateToBusinessLanguageAsync(architectureContent, PersonaType.Technical);

        // Assert - All technical details preserved
        Assert.Equal(architectureContent, translationResult.Content);
        Assert.Contains("API Gateway", translationResult.Content);
        Assert.Contains("Microservices", translationResult.Content);
        Assert.Contains("Authentication", translationResult.Content);
        Assert.Contains("OAuth2", translationResult.Content);
        Assert.Contains("PostgreSQL 16", translationResult.Content);
    }

    [Fact]
    public async Task TechnicalPersona_ReceivesSecurityImplications()
    {
        // Arrange
        var securityContent = @"Security considerations:
1. Use HTTPS for all API endpoints
2. Implement rate limiting (100 requests/minute)
3. Enable authentication with JWT tokens (HS256)
4. Apply CORS policy for microservices";

        // Act
        var translationResult = await _translationService.TranslateToBusinessLanguageAsync(securityContent, PersonaType.Technical);

        // Assert
        Assert.Equal(securityContent, translationResult.Content);
        Assert.Contains("HTTPS", translationResult.Content);
        Assert.Contains("API", translationResult.Content);
        Assert.Contains("authentication", translationResult.Content);
        Assert.Contains("JWT", translationResult.Content);
        Assert.Contains("microservices", translationResult.Content);
    }

    [Fact]
    public async Task TechnicalPersona_ReceivesPerformanceMetrics()
    {
        // Arrange
        var performanceContent = @"Performance benchmarks:
- API response time: <100ms (p95)
- Microservices throughput: 10k req/s
- Database query time: <50ms
- Authentication latency: <20ms";

        // Act
        var translationResult = await _translationService.TranslateToBusinessLanguageAsync(performanceContent, PersonaType.Technical);

        // Assert
        Assert.Equal(performanceContent, translationResult.Content);
        Assert.Contains("API response time", translationResult.Content);
        Assert.Contains("Microservices throughput", translationResult.Content);
        Assert.Contains("Database query time", translationResult.Content);
        Assert.Contains("Authentication latency", translationResult.Content);
    }

    [Fact]
    public async Task BusinessPersona_ReceivesTranslatedContent()
    {
        // Arrange
        var technicalContent = "Configure the REST endpoint with authentication for the API.";

        // Act
        var translationResult = await _translationService.TranslateToBusinessLanguageAsync(technicalContent, PersonaType.Business);

        // Assert - Business content should be translated
        Assert.NotEqual(technicalContent, translationResult.Content);
        Assert.Contains("web service point", translationResult.Content);
        Assert.Contains("identity check", translationResult.Content);
        Assert.Contains("system connection", translationResult.Content);
        Assert.DoesNotContain("REST endpoint", translationResult.Content);
        Assert.DoesNotContain("authentication", translationResult.Content);
        Assert.DoesNotContain("API", translationResult.Content);
    }

    [Fact]
    public async Task MultiplePersonas_SeeAppropriateContent()
    {
        // Arrange
        var content = "The API uses microservices with authentication.";

        // Act
        var technicalResult = await _translationService.TranslateToBusinessLanguageAsync(content, PersonaType.Technical);
        var businessResult = await _translationService.TranslateToBusinessLanguageAsync(content, PersonaType.Business);

        // Assert - Technical sees original
        Assert.Equal(content, technicalResult.Content);
        Assert.Contains("API", technicalResult.Content);
        Assert.Contains("microservices", technicalResult.Content);
        Assert.Contains("authentication", technicalResult.Content);

        // Assert - Business sees translation
        Assert.NotEqual(content, businessResult.Content);
        Assert.Contains("system connection", businessResult.Content);
        Assert.Contains("modular components", businessResult.Content);
        Assert.Contains("identity check", businessResult.Content);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }
}
