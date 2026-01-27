using bmadServer.ApiService.Controllers;
using bmadServer.ApiService.Data;
using bmadServer.ApiService.Data.Entities;
using bmadServer.ApiService.DTOs;
using bmadServer.ApiService.Hubs;
using bmadServer.ApiService.Models.Workflows;
using bmadServer.ApiService.Services;
using bmadServer.Tests.Integration;
using FluentAssertions;
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

public class WorkflowsControllerTests : IClassFixture<TestWebApplicationFactory>, IDisposable
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public WorkflowsControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
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
        var status = await response.Content.ReadFromJsonAsync<WorkflowStatusResponse>();
        status.Should().NotBeNull();
        status!.Id.Should().Be(createdInstance.Id);
        status.WorkflowId.Should().Be("create-prd");
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
        var status = await getResponse.Content.ReadFromJsonAsync<WorkflowStatusResponse>();
        status!.Status.Should().Be("Running");
        status.CurrentStep.Should().Be(1);
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
        var initialStatus = await initialResponse.Content.ReadFromJsonAsync<WorkflowStatusResponse>();
        initialStatus!.Status.Should().Be("Running");

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
        var finalStatus = await finalResponse.Content.ReadFromJsonAsync<WorkflowStatusResponse>();
        finalStatus!.Status.Should().Be("Running");
        finalStatus.CurrentStep.Should().Be(initialStatus.CurrentStep); // Step should be preserved
    }

    [Fact]
    public async Task SkipCurrentStep_WithOptionalSkippableStep_ShouldReturn200()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create a workflow with an optional step (create-architecture has optional step at index 2)
        var createRequest = new CreateWorkflowRequest
        {
            WorkflowId = "create-architecture"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/workflows", createRequest);
        var createdInstance = await createResponse.Content.ReadFromJsonAsync<WorkflowInstance>();

        // Start the workflow and manually set current step to 3 (arch-3, which is optional)
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var instance = await dbContext.WorkflowInstances.FindAsync(createdInstance!.Id);
        instance!.Status = WorkflowStatus.Running;
        instance.CurrentStep = 3;
        await dbContext.SaveChangesAsync();

        var skipRequest = new SkipStepRequest
        {
            Reason = "Not needed for this architecture"
        };

        // Act
        var response = await _client.PostAsJsonAsync(
            $"/api/v1/workflows/{createdInstance.Id}/steps/current/skip", 
            skipRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var skippedInstance = await response.Content.ReadFromJsonAsync<WorkflowInstance>();
        skippedInstance.Should().NotBeNull();
        skippedInstance!.CurrentStep.Should().Be(4); // Advanced to next step

        // Verify step history was created
        var stepHistory = await dbContext.WorkflowStepHistories
            .Where(h => h.WorkflowInstanceId == createdInstance.Id && h.StepId == "arch-3")
            .FirstOrDefaultAsync();
        stepHistory.Should().NotBeNull();
        stepHistory!.Status.Should().Be(StepExecutionStatus.Skipped);
        stepHistory.ErrorMessage.Should().Be("Not needed for this architecture");
    }

    [Fact]
    public async Task SkipCurrentStep_WithRequiredStep_ShouldReturn400()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create a workflow
        var createRequest = new CreateWorkflowRequest
        {
            WorkflowId = "create-prd"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/workflows", createRequest);
        var createdInstance = await createResponse.Content.ReadFromJsonAsync<WorkflowInstance>();

        // Start the workflow (current step 1 is required)
        await _client.PostAsync($"/api/v1/workflows/{createdInstance!.Id}/start", null);

        // Act
        var response = await _client.PostAsJsonAsync(
            $"/api/v1/workflows/{createdInstance.Id}/steps/current/skip", 
            new SkipStepRequest());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problemDetails = await response.Content.ReadAsStringAsync();
        problemDetails.Should().Contain("required and cannot be skipped");
    }

    [Fact]
    public async Task SkipCurrentStep_WithOptionalButNonSkippableStep_ShouldReturn400()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create a workflow (dev-story has optional but non-skippable step at index 3)
        var createRequest = new CreateWorkflowRequest
        {
            WorkflowId = "dev-story"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/workflows", createRequest);
        var createdInstance = await createResponse.Content.ReadFromJsonAsync<WorkflowInstance>();

        // Set workflow to running on step 4 (dev-4: optional but CanSkip=false)
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var instance = await dbContext.WorkflowInstances.FindAsync(createdInstance!.Id);
        instance!.Status = WorkflowStatus.Running;
        instance.CurrentStep = 4;
        await dbContext.SaveChangesAsync();

        // Act
        var response = await _client.PostAsJsonAsync(
            $"/api/v1/workflows/{createdInstance.Id}/steps/current/skip", 
            new SkipStepRequest());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problemDetails = await response.Content.ReadAsStringAsync();
        problemDetails.Should().Contain("cannot be skipped despite being optional");
    }

    [Fact]
    public async Task SkipCurrentStep_WhenWorkflowNotRunning_ShouldReturn400()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create a workflow (but don't start it)
        var createRequest = new CreateWorkflowRequest
        {
            WorkflowId = "create-architecture"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/workflows", createRequest);
        var createdInstance = await createResponse.Content.ReadFromJsonAsync<WorkflowInstance>();

        // Act
        var response = await _client.PostAsJsonAsync(
            $"/api/v1/workflows/{createdInstance!.Id}/steps/current/skip", 
            new SkipStepRequest());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problemDetails = await response.Content.ReadAsStringAsync();
        problemDetails.Should().Contain("Created state");
    }

    [Fact]
    public async Task GoToStep_WithPreviouslyVisitedStep_ShouldReturn200()
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

        // Manually create step history to simulate visiting step prd-1
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var stepHistory = new WorkflowStepHistory
        {
            Id = Guid.NewGuid(),
            WorkflowInstanceId = createdInstance.Id,
            StepId = "prd-1",
            StepName = "Define Project Vision",
            StartedAt = DateTime.UtcNow.AddHours(-1),
            CompletedAt = DateTime.UtcNow.AddMinutes(-50),
            Status = StepExecutionStatus.Completed
        };
        dbContext.WorkflowStepHistories.Add(stepHistory);
        
        // Set workflow to step 3
        var instance = await dbContext.WorkflowInstances.FindAsync(createdInstance.Id);
        instance!.CurrentStep = 3;
        await dbContext.SaveChangesAsync();

        // Act
        var response = await _client.PostAsync(
            $"/api/v1/workflows/{createdInstance.Id}/steps/prd-1/goto", 
            null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedInstance = await response.Content.ReadFromJsonAsync<WorkflowInstance>();
        updatedInstance.Should().NotBeNull();
        updatedInstance!.CurrentStep.Should().Be(1); // Should navigate to step 1

        // Verify event was logged
        var events = await dbContext.WorkflowEvents
            .Where(e => e.WorkflowInstanceId == createdInstance.Id && e.EventType == "StepRevisit")
            .ToListAsync();
        events.Should().HaveCount(1);
    }

    [Fact]
    public async Task GoToStep_WithNonVisitedStep_ShouldReturn400()
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

        // Act - Try to go to step that hasn't been visited
        var response = await _client.PostAsync(
            $"/api/v1/workflows/{createdInstance.Id}/steps/prd-3/goto", 
            null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problemDetails = await response.Content.ReadAsStringAsync();
        problemDetails.Should().Contain("previously visited steps");
    }

    [Fact]
    public async Task GoToStep_WithInvalidStepId_ShouldReturn400()
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

        // Act
        var response = await _client.PostAsync(
            $"/api/v1/workflows/{createdInstance.Id}/steps/invalid-step/goto", 
            null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problemDetails = await response.Content.ReadAsStringAsync();
        problemDetails.Should().Contain("not found in workflow definition");
    }

    [Fact]
    public async Task GoToStep_WhenWorkflowNotRunning_ShouldReturn400()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create a workflow (but don't start it)
        var createRequest = new CreateWorkflowRequest
        {
            WorkflowId = "create-prd"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/workflows", createRequest);
        var createdInstance = await createResponse.Content.ReadFromJsonAsync<WorkflowInstance>();

        // Act
        var response = await _client.PostAsync(
            $"/api/v1/workflows/{createdInstance!.Id}/steps/prd-1/goto", 
            null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problemDetails = await response.Content.ReadAsStringAsync();
        problemDetails.Should().Contain("Created state");
    }

    [Fact]
    public async Task GoToStep_PreservesPreviousOutput_WhenRevisiting()
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

        // Create step history with output
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var stepHistory = new WorkflowStepHistory
        {
            Id = Guid.NewGuid(),
            WorkflowInstanceId = createdInstance.Id,
            StepId = "prd-2",
            StepName = "Identify User Stories",
            StartedAt = DateTime.UtcNow.AddHours(-1),
            CompletedAt = DateTime.UtcNow.AddMinutes(-50),
            Status = StepExecutionStatus.Completed,
            Output = JsonDocument.Parse("{\"userStories\": [\"story1\", \"story2\"]}")
        };
        dbContext.WorkflowStepHistories.Add(stepHistory);
        
        var instance = await dbContext.WorkflowInstances.FindAsync(createdInstance.Id);
        instance!.CurrentStep = 3;
        await dbContext.SaveChangesAsync();

        // Act
        var response = await _client.PostAsync(
            $"/api/v1/workflows/{createdInstance.Id}/steps/prd-2/goto", 
            null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Verify step history output is preserved
        var preservedHistory = await dbContext.WorkflowStepHistories
            .Where(h => h.WorkflowInstanceId == createdInstance.Id && h.StepId == "prd-2")
            .FirstOrDefaultAsync();
        preservedHistory.Should().NotBeNull();
        preservedHistory!.Output.Should().NotBeNull();
        preservedHistory.Output!.RootElement.GetProperty("userStories").GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task SkipCurrentStep_WithoutAuthentication_ShouldReturn401()
    {
        // Arrange - No createRequest needed for this test

        // Act
        var response = await _client.PostAsJsonAsync(
            $"/api/v1/workflows/{Guid.NewGuid()}/steps/current/skip", 
            new SkipStepRequest());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GoToStep_WithoutAuthentication_ShouldReturn401()
    {
        // Act
        var response = await _client.PostAsync(
            $"/api/v1/workflows/{Guid.NewGuid()}/steps/prd-1/goto", 
            null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
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
