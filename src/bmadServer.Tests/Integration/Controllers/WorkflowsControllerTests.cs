using bmadServer.ApiService.Controllers;
using bmadServer.ApiService.Data;
using bmadServer.ApiService.Data.Entities;
using bmadServer.ApiService.Hubs;
using bmadServer.ApiService.Models.Workflows;
using bmadServer.ApiService.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace bmadServer.Tests.Integration.Controllers;

public class WorkflowsControllerTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private static readonly string _databaseName = "TestDb_Workflows_" + Guid.NewGuid();

    public WorkflowsControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase(_databaseName);
                });
            });

            builder.UseEnvironment("Test");
        });

        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
    }

    [Fact]
    public async Task CreateWorkflow_WithoutAuthentication_ShouldReturn401()
    {
        // Arrange
        var request = new CreateWorkflowRequest
        {
            WorkflowId = "create-prd"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/workflows", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateWorkflow_WithInvalidWorkflowId_ShouldReturn400()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new CreateWorkflowRequest
        {
            WorkflowId = "invalid-workflow"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/workflows", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateWorkflow_WithValidRequest_ShouldReturn201()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new CreateWorkflowRequest
        {
            WorkflowId = "create-prd",
            InitialContext = new Dictionary<string, object>
            {
                ["projectName"] = "Test Project",
                ["description"] = "Test Description"
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/workflows", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var instance = await response.Content.ReadFromJsonAsync<WorkflowInstance>();
        instance.Should().NotBeNull();
        instance!.WorkflowDefinitionId.Should().Be("create-prd");
        instance.Status.Should().Be(WorkflowStatus.Created);
    }

    [Fact]
    public async Task GetWorkflow_WithValidId_ShouldReturn200()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create a workflow first
        var createRequest = new CreateWorkflowRequest
        {
            WorkflowId = "create-prd"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/workflows", createRequest);
        var createdInstance = await createResponse.Content.ReadFromJsonAsync<WorkflowInstance>();

        // Act
        var response = await _client.GetAsync($"/api/v1/workflows/{createdInstance!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var instance = await response.Content.ReadFromJsonAsync<WorkflowInstance>();
        instance.Should().NotBeNull();
        instance!.Id.Should().Be(createdInstance.Id);
    }

    [Fact]
    public async Task GetWorkflow_WithInvalidId_ShouldReturn404()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync($"/api/v1/workflows/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task StartWorkflow_WithValidId_ShouldReturn204()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create a workflow first
        var createRequest = new CreateWorkflowRequest
        {
            WorkflowId = "create-prd"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/workflows", createRequest);
        var createdInstance = await createResponse.Content.ReadFromJsonAsync<WorkflowInstance>();

        // Act
        var response = await _client.PostAsync($"/api/v1/workflows/{createdInstance!.Id}/start", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify workflow status changed
        var getResponse = await _client.GetAsync($"/api/v1/workflows/{createdInstance.Id}");
        var instance = await getResponse.Content.ReadFromJsonAsync<WorkflowInstance>();
        instance!.Status.Should().Be(WorkflowStatus.Running);
        instance.CurrentStep.Should().Be(1);
    }

    [Fact]
    public async Task PauseWorkflow_WithRunningWorkflow_ShouldReturn200()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create and start a workflow
        var createRequest = new CreateWorkflowRequest
        {
            WorkflowId = "create-prd"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/workflows", createRequest);
        var createdInstance = await createResponse.Content.ReadFromJsonAsync<WorkflowInstance>();

        await _client.PostAsync($"/api/v1/workflows/{createdInstance!.Id}/start", null);

        // Act - Pause the workflow
        var response = await _client.PostAsync($"/api/v1/workflows/{createdInstance.Id}/pause", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var pausedInstance = await response.Content.ReadFromJsonAsync<WorkflowInstance>();
        pausedInstance.Should().NotBeNull();
        pausedInstance!.Status.Should().Be(WorkflowStatus.Paused);
        pausedInstance.PausedAt.Should().NotBeNull();
        pausedInstance.PausedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task PauseWorkflow_WhenAlreadyPaused_ShouldReturn400()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create, start, and pause a workflow
        var createRequest = new CreateWorkflowRequest
        {
            WorkflowId = "create-prd"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/workflows", createRequest);
        var createdInstance = await createResponse.Content.ReadFromJsonAsync<WorkflowInstance>();

        await _client.PostAsync($"/api/v1/workflows/{createdInstance!.Id}/start", null);
        await _client.PostAsync($"/api/v1/workflows/{createdInstance.Id}/pause", null);

        // Act - Try to pause again
        var response = await _client.PostAsync($"/api/v1/workflows/{createdInstance.Id}/pause", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problemDetails = await response.Content.ReadAsStringAsync();
        problemDetails.Should().Contain("already paused");
    }

    [Fact]
    public async Task PauseWorkflow_WithNonExistentWorkflow_ShouldReturn404()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.PostAsync($"/api/v1/workflows/{nonExistentId}/pause", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PauseWorkflow_WithoutAuthentication_ShouldReturn401()
    {
        // Arrange
        var workflowId = Guid.NewGuid();

        // Act
        var response = await _client.PostAsync($"/api/v1/workflows/{workflowId}/pause", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ResumeWorkflow_WithPausedWorkflow_ShouldReturn200()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create, start, and pause a workflow
        var createRequest = new CreateWorkflowRequest
        {
            WorkflowId = "create-prd"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/workflows", createRequest);
        var createdInstance = await createResponse.Content.ReadFromJsonAsync<WorkflowInstance>();

        await _client.PostAsync($"/api/v1/workflows/{createdInstance!.Id}/start", null);
        await _client.PostAsync($"/api/v1/workflows/{createdInstance.Id}/pause", null);

        // Act - Resume the workflow
        var response = await _client.PostAsync($"/api/v1/workflows/{createdInstance.Id}/resume", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var resumeResponse = await response.Content.ReadFromJsonAsync<ResumeWorkflowResponse>();
        resumeResponse.Should().NotBeNull();
        resumeResponse!.Workflow.Status.Should().Be(WorkflowStatus.Running);
        resumeResponse.Message.Should().BeNull(); // No context refresh for short pause
    }

    [Fact]
    public async Task ResumeWorkflow_WhenNotPaused_ShouldReturn400()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create and start a workflow (but don't pause it)
        var createRequest = new CreateWorkflowRequest
        {
            WorkflowId = "create-prd"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/workflows", createRequest);
        var createdInstance = await createResponse.Content.ReadFromJsonAsync<WorkflowInstance>();

        await _client.PostAsync($"/api/v1/workflows/{createdInstance!.Id}/start", null);

        // Act - Try to resume a running workflow
        var response = await _client.PostAsync($"/api/v1/workflows/{createdInstance.Id}/resume", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problemDetails = await response.Content.ReadAsStringAsync();
        problemDetails.Should().Contain("Cannot resume");
    }

    [Fact]
    public async Task ResumeWorkflow_WithNonExistentWorkflow_ShouldReturn404()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.PostAsync($"/api/v1/workflows/{nonExistentId}/resume", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ResumeWorkflow_WithoutAuthentication_ShouldReturn401()
    {
        // Arrange
        var workflowId = Guid.NewGuid();

        // Act
        var response = await _client.PostAsync($"/api/v1/workflows/{workflowId}/resume", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PauseAndResumeWorkflow_FullCycle_ShouldWorkCorrectly()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create and start a workflow
        var createRequest = new CreateWorkflowRequest
        {
            WorkflowId = "create-prd"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/workflows", createRequest);
        var createdInstance = await createResponse.Content.ReadFromJsonAsync<WorkflowInstance>();

        await _client.PostAsync($"/api/v1/workflows/{createdInstance!.Id}/start", null);

        // Get initial state
        var initialResponse = await _client.GetAsync($"/api/v1/workflows/{createdInstance.Id}");
        var initialInstance = await initialResponse.Content.ReadFromJsonAsync<WorkflowInstance>();
        initialInstance!.Status.Should().Be(WorkflowStatus.Running);

        // Pause the workflow
        var pauseResponse = await _client.PostAsync($"/api/v1/workflows/{createdInstance.Id}/pause", null);
        pauseResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var pausedInstance = await pauseResponse.Content.ReadFromJsonAsync<WorkflowInstance>();
        pausedInstance!.Status.Should().Be(WorkflowStatus.Paused);
        pausedInstance.PausedAt.Should().NotBeNull();

        // Resume the workflow
        var resumeResponse = await _client.PostAsync($"/api/v1/workflows/{createdInstance.Id}/resume", null);
        resumeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var resumedWorkflowResponse = await resumeResponse.Content.ReadFromJsonAsync<ResumeWorkflowResponse>();
        resumedWorkflowResponse!.Workflow.Status.Should().Be(WorkflowStatus.Running);
        resumedWorkflowResponse.Message.Should().BeNull(); // No context refresh for short pause

        // Verify final state
        var finalResponse = await _client.GetAsync($"/api/v1/workflows/{createdInstance.Id}");
        var finalInstance = await finalResponse.Content.ReadFromJsonAsync<WorkflowInstance>();
        finalInstance!.Status.Should().Be(WorkflowStatus.Running);
        finalInstance.CurrentStep.Should().Be(initialInstance.CurrentStep); // Step should be preserved
    }

    private async Task<string> GetAuthTokenAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var jwtTokenService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();

        var user = new User
        {
            Email = $"test-{Guid.NewGuid()}@example.com",
            PasswordHash = "hash",
            DisplayName = "Test User",
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        return jwtTokenService.GenerateAccessToken(user);
    }
}
