using bmadServer.ApiService.Data;
using bmadServer.ApiService.Data.Entities;
using bmadServer.ApiService.DTOs;
using bmadServer.ApiService.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using Xunit;

namespace bmadServer.Tests.Integration;

public class BusinessLanguageTranslationIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public BusinessLanguageTranslationIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TranslationService_TranslatesContentBasedOnPersona()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var translationService = scope.ServiceProvider.GetRequiredService<ITranslationService>();

        // Add test mapping
        var mapping = new TranslationMapping
        {
            TechnicalTerm = "Kubernetes",
            BusinessTerm = "container orchestration platform",
            IsActive = true
        };
        dbContext.TranslationMappings.Add(mapping);
        await dbContext.SaveChangesAsync();

        var technicalContent = "Deploy to Kubernetes for scalability.";

        // Act - Business persona
        var businessResult = await translationService.TranslateToBusinessLanguageAsync(
            technicalContent, 
            PersonaType.Business);

        // Act - Technical persona
        var technicalResult = await translationService.TranslateToBusinessLanguageAsync(
            technicalContent, 
            PersonaType.Technical);

        // Assert
        Assert.Contains("container orchestration platform", businessResult);
        Assert.DoesNotContain("Kubernetes", businessResult);
        Assert.Equal(technicalContent, technicalResult); // Technical persona gets original
    }

    [Fact]
    public async Task TranslationService_CRUD_Operations()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var translationService = scope.ServiceProvider.GetRequiredService<ITranslationService>();

        // Act - Create
        var created = await translationService.AddTranslationMappingAsync(
            "load balancer",
            "traffic distributor",
            "Infrastructure"
        );

        // Assert - Create
        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Equal("load balancer", created.TechnicalTerm);
        Assert.Equal("traffic distributor", created.BusinessTerm);
        Assert.True(created.IsActive);

        // Act - Get all
        var mappings = await translationService.GetTranslationMappingsAsync();

        // Assert - Get
        var mappingsList = mappings.ToList();
        Assert.Contains(mappingsList, m => m.TechnicalTerm == "load balancer");

        // Act - Update
        var updated = await translationService.UpdateTranslationMappingAsync(
            created.Id,
            "load balancer",
            "request router",
            "Updated context"
        );

        // Assert - Update
        Assert.Equal("request router", updated.BusinessTerm);
        Assert.Equal("Updated context", updated.Context);

        // Act - Delete
        var deleteResult = await translationService.DeleteTranslationMappingAsync(created.Id);

        // Assert - Delete
        Assert.True(deleteResult);

        // Verify deletion
        var finalMappings = await translationService.GetTranslationMappingsAsync();
        Assert.DoesNotContain(finalMappings, m => m.Id == created.Id);
    }

    [Fact]
    public async Task UserPersonaType_PersistsCorrectly()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var businessUser = new User
        {
            Email = $"business-{Guid.NewGuid()}@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            DisplayName = "Business User",
            PersonaType = PersonaType.Business
        };

        var technicalUser = new User
        {
            Email = $"technical-{Guid.NewGuid()}@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            DisplayName = "Technical User",
            PersonaType = PersonaType.Technical
        };

        dbContext.Users.AddRange(businessUser, technicalUser);
        await dbContext.SaveChangesAsync();

        // Act - Retrieve users
        var retrievedBusiness = await dbContext.Users.FindAsync(businessUser.Id);
        var retrievedTechnical = await dbContext.Users.FindAsync(technicalUser.Id);

        // Assert
        Assert.NotNull(retrievedBusiness);
        Assert.Equal(PersonaType.Business, retrievedBusiness.PersonaType);
        Assert.NotNull(retrievedTechnical);
        Assert.Equal(PersonaType.Technical, retrievedTechnical.PersonaType);
    }
}
