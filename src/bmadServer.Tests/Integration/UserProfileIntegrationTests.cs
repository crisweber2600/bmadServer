using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using bmadServer.ApiService.Data;
using bmadServer.ApiService.Data.Entities;
using bmadServer.ApiService.DTOs;
using bmadServer.ApiService.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace bmadServer.Tests.Integration;

public class UserProfileIntegrationTests : IClassFixture<TestWebApplicationFactory>, IDisposable
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public UserProfileIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
    }

    [Fact]
    public async Task GetUserProfile_WithValidUserId_ReturnsProfile()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var jwtTokenService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();

        var user = new User
        {
            Email = "profile.user@test.com",
            DisplayName = "ProfileUser",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var token = jwtTokenService.GenerateAccessToken(user);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync($"/api/v1/users/{user.Id}/profile");

        // Assert
        if (response.StatusCode != HttpStatusCode.OK)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Expected OK but got {response.StatusCode}: {errorContent}");
        }
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = await response.Content.ReadFromJsonAsync<UserProfileResponse>();
        Assert.NotNull(profile);
        Assert.Equal(user.Id, profile.UserId);
        Assert.Equal("ProfileUser", profile.DisplayName);
        Assert.True((DateTime.UtcNow - profile.JoinedAt).TotalMinutes < 1);
    }

    [Fact]
    public async Task GetUserProfile_WithInvalidUserId_ReturnsNotFound()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var jwtTokenService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();

        var user = new User
        {
            Email = "test@test.com",
            DisplayName = "TestUser",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var token = jwtTokenService.GenerateAccessToken(user);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var invalidUserId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/v1/users/{invalidUserId}/profile");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetUserProfile_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/v1/users/{userId}/profile");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetUserProfile_CachesResult()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var jwtTokenService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();

        var user = new User
        {
            Email = "cache@test.com",
            DisplayName = "CacheUser",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var token = jwtTokenService.GenerateAccessToken(user);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act - First request
        var response1 = await _client.GetAsync($"/api/v1/users/{user.Id}/profile");
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);

        // Act - Second request (should use cache)
        var response2 = await _client.GetAsync($"/api/v1/users/{user.Id}/profile");
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
        var profile = await response2.Content.ReadFromJsonAsync<UserProfileResponse>();
        Assert.NotNull(profile);
        Assert.Equal("CacheUser", profile.DisplayName);
    }
}
