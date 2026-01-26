using System.Net;
using System.Net.Http.Json;
using bmadServer.ApiService.DTOs;
using bmadServer.ApiService.Models.Workflows;
using Xunit;

namespace bmadServer.Tests.Integration;

public class WorkflowParticipantsIntegrationTests : IClassFixture<TestWebApplicationFactory>, IAsyncLifetime
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private string? _authToken;
    private Guid _testWorkflowId;
    private Guid _testUserId;

    public WorkflowParticipantsIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        // Register and login as workflow owner
        var registerRequest = new
        {
            email = $"workflow-test-{Guid.NewGuid()}@example.com",
            password = "Test123!",
            displayName = "Workflow Test User"
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);
        registerResponse.EnsureSuccessStatusCode();
        
        // Login to get token
        var loginRequest = new
        {
            email = registerRequest.email,
            password = registerRequest.password
        };
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        var loginData = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        _authToken = loginData?.AccessToken;

        // Create test workflow
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _authToken);

        var createWorkflowRequest = new { WorkflowId = "create-prd" };
        var workflowResponse = await _client.PostAsJsonAsync("/api/v1/workflows", createWorkflowRequest);
        var workflow = await workflowResponse.Content.ReadFromJsonAsync<WorkflowInstance>();
        _testWorkflowId = workflow!.Id;

        // Create another user to add as participant
        var participantRegisterRequest = new
        {
            email = $"participant-{Guid.NewGuid()}@example.com",
            password = "Test123!",
            displayName = "Participant User"
        };
        
        // Need to use a separate client without auth header for registration
        using var tempClient = _factory.CreateClient();
        var participantResponse = await tempClient.PostAsJsonAsync("/api/v1/auth/register", participantRegisterRequest);
        var participantData = await participantResponse.Content.ReadFromJsonAsync<UserResponse>();
        _testUserId = participantData!.Id;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task AddParticipant_ShouldReturn201_WhenValid()
    {
        // Arrange
        var request = new AddParticipantRequest
        {
            UserId = _testUserId,
            Role = "Contributor"
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/v1/workflows/{_testWorkflowId}/participants", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var participant = await response.Content.ReadFromJsonAsync<ParticipantResponse>();
        Assert.NotNull(participant);
        Assert.Equal(_testWorkflowId, participant.WorkflowId);
        Assert.Equal(_testUserId, participant.UserId);
        Assert.Equal("Contributor", participant.Role);
    }

    [Fact]
    public async Task AddParticipant_ShouldReturn400_WhenUserAlreadyParticipant()
    {
        // Arrange
        var request = new AddParticipantRequest
        {
            UserId = _testUserId,
            Role = "Contributor"
        };

        // Add participant first time
        await _client.PostAsJsonAsync($"/api/v1/workflows/{_testWorkflowId}/participants", request);

        // Act - try to add again
        var response = await _client.PostAsJsonAsync($"/api/v1/workflows/{_testWorkflowId}/participants", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetParticipants_ShouldReturnList_WhenParticipantsExist()
    {
        // Arrange
        var request = new AddParticipantRequest
        {
            UserId = _testUserId,
            Role = "Observer"
        };
        await _client.PostAsJsonAsync($"/api/v1/workflows/{_testWorkflowId}/participants", request);

        // Act
        var response = await _client.GetAsync($"/api/v1/workflows/{_testWorkflowId}/participants");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var participants = await response.Content.ReadFromJsonAsync<List<ParticipantResponse>>();
        Assert.NotNull(participants);
        Assert.Single(participants);
        Assert.Equal(_testUserId, participants[0].UserId);
    }

    [Fact]
    public async Task RemoveParticipant_ShouldReturn204_WhenParticipantExists()
    {
        // Arrange
        var request = new AddParticipantRequest
        {
            UserId = _testUserId,
            Role = "Contributor"
        };
        await _client.PostAsJsonAsync($"/api/v1/workflows/{_testWorkflowId}/participants", request);

        // Act
        var response = await _client.DeleteAsync($"/api/v1/workflows/{_testWorkflowId}/participants/{_testUserId}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify participant is removed
        var getResponse = await _client.GetAsync($"/api/v1/workflows/{_testWorkflowId}/participants");
        var participants = await getResponse.Content.ReadFromJsonAsync<List<ParticipantResponse>>();
        Assert.Empty(participants!);
    }

    [Fact]
    public async Task RemoveParticipant_ShouldReturn404_WhenParticipantNotFound()
    {
        // Act
        var response = await _client.DeleteAsync($"/api/v1/workflows/{_testWorkflowId}/participants/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private class LoginResponse
    {
        public string AccessToken { get; set; } = "";
        public string RefreshToken { get; set; } = "";
    }

    private class UserResponse
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = "";
        public string DisplayName { get; set; } = "";
    }
}
