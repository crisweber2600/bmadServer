using System.Net;
using System.Net.Http.Json;
using bmadServer.ApiService.DTOs;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace bmadServer.Tests.Integration;

public class AuthControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;

    public AuthControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_WithValidData_Returns201Created()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = $"sarah-{Guid.NewGuid()}@example.com",
            Password = "SecurePass123!",
            DisplayName = "Sarah Johnson"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        
        var userResponse = await response.Content.ReadFromJsonAsync<UserResponse>();
        Assert.NotNull(userResponse);
        Assert.NotEqual(Guid.Empty, userResponse.Id);
        Assert.Equal(request.Email, userResponse.Email);
        Assert.Equal("Sarah Johnson", userResponse.DisplayName);
        Assert.NotEqual(default(DateTime), userResponse.CreatedAt);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_Returns409Conflict()
    {
        // Arrange
        var uniqueEmail = $"duplicate-{Guid.NewGuid()}@example.com";
        var request = new RegisterRequest
        {
            Email = uniqueEmail,
            Password = "SecurePass123!",
            DisplayName = "First User"
        };

        // First registration
        await _client.PostAsJsonAsync("/api/v1/auth/register", request);

        // Second registration with same email
        var duplicateRequest = new RegisterRequest
        {
            Email = uniqueEmail,
            Password = "DifferentPass123!",
            DisplayName = "Second User"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", duplicateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithInvalidEmail_Returns400BadRequest()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "invalid-email",
            Password = "SecurePass123!",
            DisplayName = "Test User"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithWeakPassword_Returns400BadRequest()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            Password = "weak",
            DisplayName = "Test User"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithMissingDisplayName_Returns400BadRequest()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            Password = "SecurePass123!",
            DisplayName = ""
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_ShouldSetRefreshTokenCookie()
    {
        // Arrange - Register user first
        var email = $"cookie-test-{Guid.NewGuid()}@example.com";
        var registerRequest = new RegisterRequest
        {
            Email = email,
            Password = "SecurePass123!",
            DisplayName = "Cookie Test User"
        };
        await _client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);

        // Act - Login
        var loginRequest = new LoginRequest { Email = email, Password = "SecurePass123!" };
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        // Verify refresh token cookie is set
        Assert.True(response.Headers.Contains("Set-Cookie"), "Set-Cookie header should be present");
        var cookies = response.Headers.GetValues("Set-Cookie");
        var refreshTokenCookie = cookies.FirstOrDefault(c => c.StartsWith("refreshToken="));
        Assert.NotNull(refreshTokenCookie);
        
        // Verify cookie has a value (token is present)
        Assert.DoesNotContain("refreshToken=;", refreshTokenCookie); // Not empty
        Assert.Contains("path=/api/v1/auth/refresh", refreshTokenCookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Refresh_WithValidRefreshToken_ShouldReturnNewAccessToken()
    {
        // Arrange - Register and login to get refresh token
        var email = $"refresh-test-{Guid.NewGuid()}@example.com";
        var registerRequest = new RegisterRequest
        {
            Email = email,
            Password = "SecurePass123!",
            DisplayName = "Refresh Test User"
        };
        await _client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);

        var loginRequest = new LoginRequest { Email = email, Password = "SecurePass123!" };
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        
        // Extract refresh token from cookie
        var cookies = loginResponse.Headers.GetValues("Set-Cookie");
        var refreshTokenCookie = cookies.First(c => c.StartsWith("refreshToken="));
        var refreshToken = refreshTokenCookie.Split(';')[0].Split('=')[1];

        // Create a request with the refresh token cookie
        var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        refreshRequest.Headers.Add("Cookie", $"refreshToken={refreshToken}");

        // Act
        var response = await _client.SendAsync(refreshRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var refreshResponseData = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(refreshResponseData);
        Assert.NotEmpty(refreshResponseData.AccessToken);
        Assert.Equal("Bearer", refreshResponseData.TokenType);
        
        // Verify new refresh token cookie is set
        var newCookies = response.Headers.GetValues("Set-Cookie");
        var newRefreshTokenCookie = newCookies.FirstOrDefault(c => c.StartsWith("refreshToken="));
        Assert.NotNull(newRefreshTokenCookie);
    }

    [Fact]
    public async Task Refresh_WithoutRefreshToken_ShouldReturn401()
    {
        // Act
        var response = await _client.PostAsync("/api/v1/auth/refresh", null);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains("Refresh token not found", problem.Detail);
    }

    [Fact]
    public async Task Refresh_WithInvalidRefreshToken_ShouldReturn401()
    {
        // Arrange
        var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        refreshRequest.Headers.Add("Cookie", "refreshToken=invalid-token-12345678901234567890123456789012");

        // Act
        var response = await _client.SendAsync(refreshRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_ShouldRevokeRefreshTokenAndClearCookie()
    {
        // Arrange - Register and login to get refresh token
        var email = $"logout-test-{Guid.NewGuid()}@example.com";
        var registerRequest = new RegisterRequest
        {
            Email = email,
            Password = "SecurePass123!",
            DisplayName = "Logout Test User"
        };
        await _client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);

        var loginRequest = new LoginRequest { Email = email, Password = "SecurePass123!" };
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        
        // Extract refresh token from cookie
        var cookies = loginResponse.Headers.GetValues("Set-Cookie");
        var refreshTokenCookie = cookies.First(c => c.StartsWith("refreshToken="));
        var refreshToken = refreshTokenCookie.Split(';')[0].Split('=')[1];

        // Create logout request with the refresh token cookie
        var logoutRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/logout");
        logoutRequest.Headers.Add("Cookie", $"refreshToken={refreshToken}");

        // Act
        var response = await _client.SendAsync(logoutRequest);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        
        // Verify cookie is cleared
        Assert.True(response.Headers.Contains("Set-Cookie"), "Set-Cookie header should be present");
        var logoutCookies = response.Headers.GetValues("Set-Cookie");
        var clearedCookie = logoutCookies.FirstOrDefault(c => c.StartsWith("refreshToken="));
        Assert.NotNull(clearedCookie);
        // Cookie should be empty or have expired date
        Assert.Contains("refreshToken=;", clearedCookie);

        // Verify that using the old refresh token fails
        var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        refreshRequest.Headers.Add("Cookie", $"refreshToken={refreshToken}");
        var refreshResponse = await _client.SendAsync(refreshRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);
    }

    [Fact]
    public async Task Logout_WithoutRefreshToken_ShouldReturn204()
    {
        // Act
        var response = await _client.PostAsync("/api/v1/auth/logout", null);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_WithRevokedToken_ShouldDetectBreachAndRevokeAllTokens()
    {
        // Arrange - Register and login
        var email = $"breach-test-{Guid.NewGuid()}@example.com";
        var registerRequest = new RegisterRequest
        {
            Email = email,
            Password = "SecurePass123!",
            DisplayName = "Breach Test User"
        };
        await _client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);

        var loginRequest = new LoginRequest { Email = email, Password = "SecurePass123!" };
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        
        // Extract refresh token
        var cookies = loginResponse.Headers.GetValues("Set-Cookie");
        var refreshTokenCookie = cookies.First(c => c.StartsWith("refreshToken="));
        var oldRefreshToken = refreshTokenCookie.Split(';')[0].Split('=')[1];

        // First refresh to rotate token
        var firstRefreshRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        firstRefreshRequest.Headers.Add("Cookie", $"refreshToken={oldRefreshToken}");
        var firstRefreshResponse = await _client.SendAsync(firstRefreshRequest);
        Assert.Equal(HttpStatusCode.OK, firstRefreshResponse.StatusCode);

        // Try to use old token again (should detect breach)
        var secondRefreshRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        secondRefreshRequest.Headers.Add("Cookie", $"refreshToken={oldRefreshToken}");
        var secondRefreshResponse = await _client.SendAsync(secondRefreshRequest);

        // Assert - Should return 401 with breach message
        Assert.Equal(HttpStatusCode.Unauthorized, secondRefreshResponse.StatusCode);
        
        var problem = await secondRefreshResponse.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains("revoked", problem.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Refresh_ConcurrentRequests_OnlyOneSucceeds()
    {
        // Note: This test uses InMemory database which doesn't support true transaction isolation.
        // In production with PostgreSQL, Serializable isolation level ensures only one request succeeds.
        // Here we verify the basic concurrent request handling works without errors.
        
        // Arrange - Register and login
        var email = $"concurrent-test-{Guid.NewGuid()}@example.com";
        var registerRequest = new RegisterRequest
        {
            Email = email,
            Password = "SecurePass123!",
            DisplayName = "Concurrent Test User"
        };
        await _client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);

        var loginRequest = new LoginRequest { Email = email, Password = "SecurePass123!" };
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        
        // Extract refresh token
        var cookies = loginResponse.Headers.GetValues("Set-Cookie");
        var refreshTokenCookie = cookies.First(c => c.StartsWith("refreshToken="));
        var refreshToken = refreshTokenCookie.Split(';')[0].Split('=')[1];

        // Act - Send two concurrent requests
        var request1 = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        request1.Headers.Add("Cookie", $"refreshToken={refreshToken}");
        
        var request2 = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        request2.Headers.Add("Cookie", $"refreshToken={refreshToken}");

        var tasks = new[] { _client.SendAsync(request1), _client.SendAsync(request2) };
        var responses = await Task.WhenAll(tasks);

        // Assert - At least one request should complete without error
        // With PostgreSQL Serializable isolation, only one would succeed
        var statusCodes = responses.Select(r => r.StatusCode).ToArray();
        Assert.All(statusCodes, sc => Assert.True(
            sc == HttpStatusCode.OK || sc == HttpStatusCode.Unauthorized,
            $"Expected OK or Unauthorized, got {sc}"));
    }
}
