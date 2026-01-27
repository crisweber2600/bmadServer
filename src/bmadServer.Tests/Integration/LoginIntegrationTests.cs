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

public class LoginIntegrationTests : IClassFixture<TestWebApplicationFactory>, IDisposable
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public LoginIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsJwtToken()
    {
        // Arrange - register a user first
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var user = new User
        {
            Email = "sarah@example.com",
            DisplayName = "Sarah Johnson",
            PasswordHash = passwordHasher.Hash("SecurePass123!"),
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var loginRequest = new LoginRequest
        {
            Email = "sarah@example.com",
            Password = "SecurePass123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(loginResponse);
        Assert.NotEmpty(loginResponse.AccessToken);
        Assert.Equal("Bearer", loginResponse.TokenType);
        Assert.Equal(900, loginResponse.ExpiresIn); // 15 minutes * 60 seconds
        Assert.Equal(user.Email, loginResponse.User.Email);
        Assert.Equal(user.DisplayName, loginResponse.User.DisplayName);
    }

    [Fact]
    public async Task Login_InvalidPassword_ReturnsUnauthorized()
    {
        // Arrange - register a user first
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var user = new User
        {
            Email = "test@example.com",
            DisplayName = "Test User",
            PasswordHash = passwordHasher.Hash("CorrectPassword123!"),
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var loginRequest = new LoginRequest
        {
            Email = "test@example.com",
            Password = "WrongPassword123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var problemDetails = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
        Assert.NotNull(problemDetails);
        Assert.Equal("Invalid email or password", problemDetails.Detail);
    }

    [Fact]
    public async Task Login_NonExistentEmail_ReturnsUnauthorizedWithGenericMessage()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Email = "nonexistent@example.com",
            Password = "AnyPassword123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var problemDetails = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
        Assert.NotNull(problemDetails);
        Assert.Equal("Invalid email or password", problemDetails.Detail);
        // Same message as wrong password to prevent enumeration
    }

    [Fact]
    public async Task Login_CaseInsensitiveEmail_ReturnsJwtToken()
    {
        // Arrange - register a user with lowercase email
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var user = new User
        {
            Email = "user@example.com",
            DisplayName = "Test User",
            PasswordHash = passwordHasher.Hash("Password123!"),
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var loginRequest = new LoginRequest
        {
            Email = "USER@EXAMPLE.COM", // Different case
            Password = "Password123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(loginResponse);
        Assert.NotEmpty(loginResponse.AccessToken);
    }

    [Fact]
    public async Task Login_EmptyEmail_ReturnsBadRequest()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Email = "",
            Password = "Password123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_EmptyPassword_ReturnsBadRequest()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Email = "user@example.com",
            Password = ""
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
