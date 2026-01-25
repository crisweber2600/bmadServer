using System.Net;
using System.Net.Http.Json;
using bmadServer.ApiService.DTOs;
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
}
