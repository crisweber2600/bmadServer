using bmadServer.ApiService.Controllers;
using bmadServer.ApiService.Data;
using bmadServer.ApiService.DTOs;
using bmadServer.ApiService.Models.Workflows;
using bmadServer.ServiceDefaults.Models.Workflows;
using bmadServer.ServiceDefaults.Services.Workflows;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace bmadServer.Tests.Integration.Controllers;

public class WorkflowStatusProgressIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private static readonly string _databaseName = "TestDb_WorkflowStatusProgress_" + Guid.NewGuid();
    private static readonly JsonSerializerOptions _jsonOptions = new() 
    { 
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public WorkflowStatusProgressIntegrationTests(WebApplicationFactory<Program> factory)
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
    public async Task GetWorkflow_WithValidId_ShouldReturnWorkflowStatus()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createRequest = new CreateWorkflowRequest
        {
            WorkflowId = "create-prd"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/workflows", createRequest);
        var createdWorkflow = await createResponse.Content.ReadFromJsonAsync<WorkflowInstance>(_jsonOptions);
        
        // Act
        var response = await _client.GetAsync($"/api/v1/workflows/{createdWorkflow!.Id}");
        var status = await response.Content.ReadFromJsonAsync<WorkflowStatusResponse>(_jsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        status.Should().NotBeNull();
        status!.Id.Should().Be(createdWorkflow.Id);
        status.WorkflowId.Should().Be("create-prd");
        status.Name.Should().NotBeNullOrEmpty();
        status.Status.Should().Be("Created");
        status.CurrentStep.Should().Be(0);
        status.TotalSteps.Should().BeGreaterThan(0);
        status.PercentComplete.Should().BeGreaterOrEqualTo(0);
        status.Steps.Should().NotBeEmpty();
        status.Steps.Should().AllSatisfy(step =>
        {
            step.StepId.Should().NotBeNullOrEmpty();
            step.Name.Should().NotBeNullOrEmpty();
            step.Status.Should().NotBeNullOrEmpty();
            step.AgentName.Should().NotBeNullOrEmpty();
        });
    }

    [Fact]
    public async Task GetWorkflow_AfterStarting_ShouldShowProgress()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createRequest = new CreateWorkflowRequest
        {
            WorkflowId = "create-prd"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/workflows", createRequest);
        var createdWorkflow = await createResponse.Content.ReadFromJsonAsync<WorkflowInstance>(_jsonOptions);

        // Start the workflow
        await _client.PostAsync($"/api/v1/workflows/{createdWorkflow!.Id}/start", null);

        // Act
        var response = await _client.GetAsync($"/api/v1/workflows/{createdWorkflow.Id}");
        var status = await response.Content.ReadFromJsonAsync<WorkflowStatusResponse>(_jsonOptions);

        // Assert
        status.Should().NotBeNull();
        status!.Status.Should().Be("Running");
        status.CurrentStep.Should().Be(1);
        status.StartedAt.Should().NotBeNull();
        status.Steps.Should().Contain(s => s.Status == "Current");
    }

    [Fact]
    public async Task GetWorkflows_WithNoFilters_ShouldReturnPagedResult()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create a workflow
        var createRequest = new CreateWorkflowRequest
        {
            WorkflowId = "create-prd"
        };
        await _client.PostAsJsonAsync("/api/v1/workflows", createRequest);

        // Act
        var response = await _client.GetAsync("/api/v1/workflows");
        var result = await response.Content.ReadFromJsonAsync<PagedResult<WorkflowInstanceListItem>>(_jsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Items.Should().NotBeEmpty();
        result.Page.Should().Be(1);
        result.TotalItems.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetWorkflows_WithStatusFilter_ShouldReturnFilteredResults()
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
        var createdWorkflow = await createResponse.Content.ReadFromJsonAsync<WorkflowInstance>(_jsonOptions);
        await _client.PostAsync($"/api/v1/workflows/{createdWorkflow!.Id}/start", null);

        // Create another workflow but don't start it
        await _client.PostAsJsonAsync("/api/v1/workflows", createRequest);

        // Act
        var response = await _client.GetAsync("/api/v1/workflows?status=Running");
        var result = await response.Content.ReadFromJsonAsync<PagedResult<WorkflowInstanceListItem>>(_jsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Items.Should().NotBeEmpty();
        result.Items.Should().AllSatisfy(w => w.Status.Should().Be(WorkflowStatus.Running));
    }

    [Fact]
    public async Task GetWorkflows_WithWorkflowTypeFilter_ShouldReturnFilteredResults()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createRequest = new CreateWorkflowRequest
        {
            WorkflowId = "create-prd"
        };
        await _client.PostAsJsonAsync("/api/v1/workflows", createRequest);

        // Act
        var response = await _client.GetAsync("/api/v1/workflows?workflowType=create-prd");
        var result = await response.Content.ReadFromJsonAsync<PagedResult<WorkflowInstanceListItem>>(_jsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Items.Should().NotBeEmpty();
        result.Items.Should().AllSatisfy(w => 
            w.WorkflowDefinitionId.Should().Be("create-prd"));
    }

    [Fact]
    public async Task GetWorkflows_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create multiple workflows
        for (int i = 0; i < 5; i++)
        {
            var createRequest = new CreateWorkflowRequest
            {
                WorkflowId = "create-prd"
            };
            await _client.PostAsJsonAsync("/api/v1/workflows", createRequest);
        }

        // Act
        var page1Response = await _client.GetAsync("/api/v1/workflows?page=1&pageSize=2");
        var page1 = await page1Response.Content.ReadFromJsonAsync<PagedResult<WorkflowInstanceListItem>>(_jsonOptions);

        var page2Response = await _client.GetAsync("/api/v1/workflows?page=2&pageSize=2");
        var page2 = await page2Response.Content.ReadFromJsonAsync<PagedResult<WorkflowInstanceListItem>>(_jsonOptions);

        // Assert
        page1.Should().NotBeNull();
        page1!.Items.Should().HaveCount(2);
        page1.Page.Should().Be(1);
        page1.HasNext.Should().BeTrue();
        page1.HasPrevious.Should().BeFalse();

        page2.Should().NotBeNull();
        page2!.Items.Should().HaveCount(2);
        page2.Page.Should().Be(2);
        page2.HasPrevious.Should().BeTrue();
    }

    [Fact]
    public async Task GetWorkflows_WithDateFilter_ShouldReturnFilteredResults()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createRequest = new CreateWorkflowRequest
        {
            WorkflowId = "create-prd"
        };
        await _client.PostAsJsonAsync("/api/v1/workflows", createRequest);

        var oneHourAgo = DateTime.UtcNow.AddHours(-1);
        var oneHourFromNow = DateTime.UtcNow.AddHours(1);

        // Act
        var response = await _client.GetAsync(
            $"/api/v1/workflows?createdAfter={oneHourAgo:O}&createdBefore={oneHourFromNow:O}");
        var result = await response.Content.ReadFromJsonAsync<PagedResult<WorkflowInstanceListItem>>(_jsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task WorkflowStatusChange_ShouldEmitSignalRNotification()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create SignalR connection
        var hubConnection = new HubConnectionBuilder()
            .WithUrl($"{_factory.Server.BaseAddress}hubs/chat", options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                options.AccessTokenProvider = () => Task.FromResult<string?>(token);
            })
            .Build();

        var notificationReceived = new TaskCompletionSource<object>(_jsonOptions);
        object? receivedNotification = null;

        hubConnection.On<object>("WORKFLOW_STATUS_CHANGED", notification =>
        {
            receivedNotification = notification;
            notificationReceived.SetResult(notification);
        });

        await hubConnection.StartAsync();

        // Create and start workflow
        var createRequest = new CreateWorkflowRequest
        {
            WorkflowId = "create-prd"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/workflows", createRequest);
        var createdWorkflow = await createResponse.Content.ReadFromJsonAsync<WorkflowInstance>(_jsonOptions);

        // Act
        await _client.PostAsync($"/api/v1/workflows/{createdWorkflow!.Id}/start", null);

        // Wait for notification (with timeout)
        var completedTask = await Task.WhenAny(
            notificationReceived.Task,
            Task.Delay(TimeSpan.FromSeconds(5))
        );

        // Assert
        completedTask.Should().Be(notificationReceived.Task, "SignalR notification should be received");
        receivedNotification.Should().NotBeNull();

        await hubConnection.StopAsync();
        await hubConnection.DisposeAsync();
    }

    [Fact]
    public async Task PauseWorkflow_ShouldEmitStatusChangedNotification()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createRequest = new CreateWorkflowRequest
        {
            WorkflowId = "create-prd"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/workflows", createRequest);
        var createdWorkflow = await createResponse.Content.ReadFromJsonAsync<WorkflowInstance>(_jsonOptions);
        await _client.PostAsync($"/api/v1/workflows/{createdWorkflow!.Id}/start", null);

        // Act
        var pauseResponse = await _client.PostAsync($"/api/v1/workflows/{createdWorkflow.Id}/pause", null);
        
        // Get status and verify
        var statusResponse = await _client.GetAsync($"/api/v1/workflows/{createdWorkflow.Id}");
        var status = await statusResponse.Content.ReadFromJsonAsync<WorkflowStatusResponse>(_jsonOptions);

        // Assert
        pauseResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        status.Should().NotBeNull();
        status!.Status.Should().Be("Paused");
    }

    private async Task<string> GetAuthTokenAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var existingUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "test@example.com");
        if (existingUser != null)
        {
            var loginRequest = new { Email = "test@example.com", Password = "TestPassword123!" };
            var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
            var loginResult = await loginResponse.Content.ReadFromJsonAsync<bmadServer.ApiService.DTOs.LoginResponse>(_jsonOptions);
            return loginResult!.AccessToken;
        }

        var registerRequest = new
        {
            Name = "Test User",
            Email = "test@example.com",
            Password = "TestPassword123!"
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);
        var result = await registerResponse.Content.ReadFromJsonAsync<bmadServer.ApiService.DTOs.LoginResponse>(_jsonOptions);
        return result!.AccessToken;
    }
}
