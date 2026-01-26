using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using bmadServer.ApiService.Controllers;
using bmadServer.ApiService.Data.Entities;
using bmadServer.ApiService.DTOs;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace bmadServer.ApiService.IntegrationTests;

[Collection("Sequential")]
public class PersonaConfigurationIntegrationTests : IClassFixture<WebApplicationFactory<AuthController>>
{
    private readonly WebApplicationFactory<AuthController> _factory;
    private readonly HttpClient _client;

    public PersonaConfigurationIntegrationTests(WebApplicationFactory<AuthController> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetMe_WithBusinessPersona_ReturnsPersonaType()
    {
        // Arrange
        var token = await RegisterAndLoginAsync("business@example.com", "Password123!", "Business User");

        // Update persona to Business
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await _client.PatchAsJsonAsync("/api/v1/users/me/persona", new UpdatePersonaRequest
        {
            PersonaType = PersonaType.Business
        });

        // Act
        var response = await _client.GetAsync("/api/v1/users/me");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var user = await response.Content.ReadFromJsonAsync<UserResponse>();
        Assert.NotNull(user);
        Assert.Equal(PersonaType.Business, user.PersonaType);
    }

    [Fact]
    public async Task UpdatePersona_ChangesFromDefaultToTechnical_PersistsAcrossRequests()
    {
        // Arrange
        var token = await RegisterAndLoginAsync("tech@example.com", "Password123!", "Tech User");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Verify default is Hybrid
        var initialResponse = await _client.GetAsync("/api/v1/users/me");
        var initialUser = await initialResponse.Content.ReadFromJsonAsync<UserResponse>();
        Assert.Equal(PersonaType.Hybrid, initialUser!.PersonaType);

        // Act - Update to Technical
        var updateResponse = await _client.PatchAsJsonAsync("/api/v1/users/me/persona", new UpdatePersonaRequest
        {
            PersonaType = PersonaType.Technical
        });

        // Assert
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updatedUser = await updateResponse.Content.ReadFromJsonAsync<UserResponse>();
        Assert.Equal(PersonaType.Technical, updatedUser!.PersonaType);

        // Verify persistence - fetch again
        var verifyResponse = await _client.GetAsync("/api/v1/users/me");
        var verifiedUser = await verifyResponse.Content.ReadFromJsonAsync<UserResponse>();
        Assert.Equal(PersonaType.Technical, verifiedUser!.PersonaType);
    }

    [Theory]
    [InlineData(PersonaType.Business)]
    [InlineData(PersonaType.Technical)]
    [InlineData(PersonaType.Hybrid)]
    public async Task UpdatePersona_AllPersonaTypes_AcceptedSuccessfully(PersonaType personaType)
    {
        // Arrange
        var email = $"user-{Guid.NewGuid()}@example.com";
        var token = await RegisterAndLoginAsync(email, "Password123!", "Test User");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.PatchAsJsonAsync("/api/v1/users/me/persona", new UpdatePersonaRequest
        {
            PersonaType = personaType
        });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var user = await response.Content.ReadFromJsonAsync<UserResponse>();
        Assert.NotNull(user);
        Assert.Equal(personaType, user.PersonaType);
    }

    [Fact]
    public async Task UpdatePersona_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange - No auth header set
        var request = new UpdatePersonaRequest
        {
            PersonaType = PersonaType.Technical
        };

        // Act
        var response = await _client.PatchAsJsonAsync("/api/v1/users/me/persona", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMe_NewUser_HasHybridPersonaByDefault()
    {
        // Arrange
        var token = await RegisterAndLoginAsync("newuser@example.com", "Password123!", "New User");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/v1/users/me");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var user = await response.Content.ReadFromJsonAsync<UserResponse>();
        Assert.NotNull(user);
        Assert.Equal(PersonaType.Hybrid, user.PersonaType);
    }

    private async Task<string> RegisterAndLoginAsync(
        string email, 
        string password, 
        string displayName)
    {
        // Register
        var registerRequest = new
        {
            Email = email,
            Password = password,
            DisplayName = displayName
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);
        registerResponse.EnsureSuccessStatusCode();

        // Login
        var loginRequest = new
        {
            Email = email,
            Password = password
        };

        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        loginResponse.EnsureSuccessStatusCode();

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        return loginResult!.AccessToken;
    }
}
