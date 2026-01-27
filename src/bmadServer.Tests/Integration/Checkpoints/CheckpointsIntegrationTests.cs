using bmadServer.ApiService.Data.Entities;
using bmadServer.ApiService.DTOs.Checkpoints;
using bmadServer.ApiService.Models.Workflows;
using bmadServer.Tests.Integration;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace bmadServer.Tests.Integration.Checkpoints;

public record CreateCheckpointRequest(string? StepId);

public class CheckpointsIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public CheckpointsIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task CreateCheckpoint_Should_Return_201_And_Checkpoint()
    {
        // Arrange
        var (token, user) = await AuthenticateAsync();
        var workflow = await CreateTestWorkflowAsync(token);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new CreateCheckpointRequest("test-step");

        // Act
        var response = await _client.PostAsJsonAsync(
            $"/api/v1/workflows/{workflow.Id}/checkpoints",
            request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        
        var checkpoint = await response.Content.ReadFromJsonAsync<WorkflowCheckpoint>();
        Assert.NotNull(checkpoint);
        Assert.Equal(workflow.Id, checkpoint.WorkflowId);
        Assert.Equal("test-step", checkpoint.StepId);
        Assert.Equal(CheckpointType.ExplicitSave, checkpoint.CheckpointType);
    }

    [Fact]
    public async Task GetCheckpoints_Should_Return_Paged_Results()
    {
        // Arrange
        var (token, user) = await AuthenticateAsync();
        var workflow = await CreateTestWorkflowAsync(token);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create multiple checkpoints
        for (int i = 1; i <= 3; i++)
        {
            await _client.PostAsJsonAsync(
                $"/api/v1/workflows/{workflow.Id}/checkpoints",
                new CreateCheckpointRequest($"step-{i}"));
        }

        // Act
        var response = await _client.GetAsync(
            $"/api/v1/workflows/{workflow.Id}/checkpoints?page=1&pageSize=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<PagedResult<CheckpointResponse>>();
        Assert.NotNull(result);
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(3, result.Items.Count);
    }

    [Fact]
    public async Task QueueInput_Should_Return_201_And_Queue_Input()
    {
        // Arrange
        var (token, user) = await AuthenticateAsync();
        var workflow = await CreateTestWorkflowAsync(token);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var content = JsonDocument.Parse("{\"message\": \"test\"}");
        var request = new QueueInputRequest("message", content);

        // Act
        var response = await _client.PostAsJsonAsync(
            $"/api/v1/workflows/{workflow.Id}/inputs/queue",
            request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        
        var queuedInput = await response.Content.ReadFromJsonAsync<QueuedInput>();
        Assert.NotNull(queuedInput);
        Assert.Equal(workflow.Id, queuedInput.WorkflowId);
        Assert.Equal(user.Id, queuedInput.UserId);
        Assert.Equal("message", queuedInput.InputType);
        Assert.Equal(InputStatus.Queued, queuedInput.Status);
    }

    [Fact]
    public async Task RestoreCheckpoint_Should_Return_200()
    {
        // Arrange
        var (token, user) = await AuthenticateAsync();
        var workflow = await CreateTestWorkflowAsync(token);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create checkpoint
        var createResponse = await _client.PostAsJsonAsync(
            $"/api/v1/workflows/{workflow.Id}/checkpoints",
            new CreateCheckpointRequest("restore-test"));
        
        var checkpoint = await createResponse.Content.ReadFromJsonAsync<WorkflowCheckpoint>();

        // Act
        var response = await _client.PostAsync(
            $"/api/v1/workflows/{workflow.Id}/checkpoints/{checkpoint!.Id}/restore",
            null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateCheckpoint_Without_Auth_Should_Return_401()
    {
        // Arrange
        var workflowId = Guid.NewGuid();

        // Act
        var response = await _client.PostAsJsonAsync(
            $"/api/v1/workflows/{workflowId}/checkpoints",
            new CreateCheckpointRequest("test"));

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<(string Token, User User)> AuthenticateAsync()
    {
        var email = $"test-{Guid.NewGuid()}@example.com";
        var password = "Test123!@#";

        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password,
            displayName = "Test User"
        });

        if (!registerResponse.IsSuccessStatusCode)
        {
            var errorContent = await registerResponse.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Registration failed: {registerResponse.StatusCode} - {errorContent}");
        }

        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email,
            password
        });

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<JsonDocument>();
        var token = loginResult!.RootElement.GetProperty("accessToken").GetString()!;
        var userId = Guid.Parse(loginResult.RootElement.GetProperty("user").GetProperty("id").GetString()!);

        return (token, new User 
        { 
            Id = userId, 
            Email = email, 
            PasswordHash = "dummy",
            DisplayName = "Test User"
        });
    }

    private async Task<WorkflowInstance> CreateTestWorkflowAsync(string token)
    {
        // Set auth header for this request
        var previousAuth = _client.DefaultRequestHeaders.Authorization;
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        
        try
        {
            // Create workflow via API using a valid workflow definition
            var createResponse = await _client.PostAsJsonAsync("/api/v1/workflows", new
            {
                WorkflowId = "create-prd",
                InitialContext = new Dictionary<string, object>()
            });

            if (createResponse.IsSuccessStatusCode)
            {
                var workflow = await createResponse.Content.ReadFromJsonAsync<WorkflowInstance>();
                return workflow!;
            }

            // Log the error for debugging
            var errorContent = await createResponse.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Workflow creation failed: {createResponse.StatusCode} - {errorContent}");
        }
        finally
        {
            // Restore previous auth header
            _client.DefaultRequestHeaders.Authorization = previousAuth;
        }
    }
}
