using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using bmadServer.ApiService.Configuration;
using bmadServer.ApiService.Data;
using bmadServer.ApiService.Data.Entities;
using bmadServer.ApiService.DTOs;
using bmadServer.ApiService.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace bmadServer.Tests.Integration;

public class UsersMeIntegrationTests : IClassFixture<TestWebApplicationFactory>, IDisposable
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public UsersMeIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
    }

    [Fact]
    public async Task GetCurrentUser_ValidToken_ReturnsUserProfile()
    {
        // Arrange - create a user and generate token
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var jwtTokenService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();

        var user = new User
        {
            Email = "john@example.com",
            DisplayName = "John Doe",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var token = jwtTokenService.GenerateAccessToken(user);

        // Act
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.GetAsync("/api/v1/users/me");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var userResponse = await response.Content.ReadFromJsonAsync<UserResponse>();
        Assert.NotNull(userResponse);
        Assert.Equal(user.Id, userResponse.Id);
        Assert.Equal(user.Email, userResponse.Email);
        Assert.Equal(user.DisplayName, userResponse.DisplayName);
    }

    [Fact]
    public async Task GetCurrentUser_NoToken_ReturnsUnauthorized()
    {
        // Act - no authorization header
        var response = await _client.GetAsync("/api/v1/users/me");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetCurrentUser_InvalidToken_ReturnsUnauthorized()
    {
        // Arrange - invalid token
        var invalidToken = "invalid.jwt.token";

        // Act
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", invalidToken);
        var response = await _client.GetAsync("/api/v1/users/me");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetCurrentUser_ExpiredToken_ReturnsUnauthorized()
    {
        // Arrange - create an expired token
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var jwtSettings = scope.ServiceProvider.GetRequiredService<IOptions<JwtSettings>>().Value;

        var user = new User
        {
            Email = "expired@example.com",
            DisplayName = "Expired User",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        // Generate an expired token manually
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email)
        };

        var expiredToken = new JwtSecurityToken(
            issuer: jwtSettings.Issuer,
            audience: jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(-10), // Already expired
            signingCredentials: credentials);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(expiredToken);

        // Act
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenString);
        var response = await _client.GetAsync("/api/v1/users/me");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        
        var problemDetails = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
        Assert.NotNull(problemDetails);
        Assert.Contains("expired", problemDetails.Detail?.ToLower() ?? "");
    }

    [Fact]
    public async Task GetCurrentUser_TamperedToken_ReturnsUnauthorized()
    {
        // Arrange - create a user and generate valid token
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var jwtTokenService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();

        var user = new User
        {
            Email = "tamper@example.com",
            DisplayName = "Tamper Test",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var validToken = jwtTokenService.GenerateAccessToken(user);
        
        // Tamper with the token by modifying the payload (change user ID)
        var parts = validToken.Split('.');
        if (parts.Length == 3)
        {
            // Decode, modify, and re-encode the payload
            var payloadBytes = Convert.FromBase64String(parts[1].Replace('-', '+').Replace('_', '/').PadRight(parts[1].Length + (4 - parts[1].Length % 4) % 4, '='));
            var payload = System.Text.Encoding.UTF8.GetString(payloadBytes);
            
            // Modify the user ID in the payload
            payload = payload.Replace(user.Id.ToString(), Guid.NewGuid().ToString());
            
            // Re-encode the modified payload
            var modifiedPayloadBytes = System.Text.Encoding.UTF8.GetBytes(payload);
            parts[1] = Convert.ToBase64String(modifiedPayloadBytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }
        var tamperedToken = string.Join('.', parts);

        // Act
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tamperedToken);
        var response = await _client.GetAsync("/api/v1/users/me");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetCurrentUser_DeletedUser_ReturnsNotFound()
    {
        // Arrange - create a user, generate token, then delete user
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var jwtTokenService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();

        var user = new User
        {
            Email = "deleted@example.com",
            DisplayName = "Deleted User",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var token = jwtTokenService.GenerateAccessToken(user);

        // Delete the user
        dbContext.Users.Remove(user);
        await dbContext.SaveChangesAsync();

        // Act
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.GetAsync("/api/v1/users/me");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var problemDetails = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
        Assert.NotNull(problemDetails);
        Assert.Contains("no longer exists", problemDetails.Detail ?? "");
    }
}
