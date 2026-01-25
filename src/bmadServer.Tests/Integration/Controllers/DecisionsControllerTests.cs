using bmadServer.ApiService.Data;
using bmadServer.ApiService.Data.Entities;
using bmadServer.ApiService.Models.Decisions;
using bmadServer.ApiService.Models.Workflows;
using bmadServer.ApiService.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace bmadServer.Tests.Integration.Controllers;

public class DecisionsControllerTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private static readonly string _databaseName = "TestDb_Decisions_" + Guid.NewGuid();

    public DecisionsControllerTests(WebApplicationFactory<Program> factory)
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
    public async Task CreateDecision_WithoutAuthentication_ShouldReturn401()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var request = new CreateDecisionRequest
        {
            WorkflowInstanceId = workflowId,
            StepId = "step-1",
            DecisionType = "approval",
            Value = JsonDocument.Parse("{\"approved\": true}").RootElement
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/decisions", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateDecision_WithValidRequest_ShouldReturn201()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create a workflow first
        var workflowId = await CreateWorkflowInstanceAsync();

        var request = new CreateDecisionRequest
        {
            WorkflowInstanceId = workflowId,
            StepId = "step-1",
            DecisionType = "approval",
            Value = JsonDocument.Parse("{\"approved\": true}").RootElement,
            Question = "Do you approve this change?",
            Options = JsonDocument.Parse("[\"Yes\", \"No\"]").RootElement,
            Reasoning = "This change aligns with our goals",
            Context = JsonDocument.Parse("{\"project\": \"Test Project\"}").RootElement
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/decisions", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var decision = await response.Content.ReadFromJsonAsync<DecisionResponse>();
        decision.Should().NotBeNull();
        decision!.WorkflowInstanceId.Should().Be(workflowId);
        decision.StepId.Should().Be("step-1");
        decision.DecisionType.Should().Be("approval");
        decision.Value.GetProperty("approved").GetBoolean().Should().BeTrue();
        decision.Question.Should().Be("Do you approve this change?");
        decision.Reasoning.Should().Be("This change aligns with our goals");
    }

    [Fact]
    public async Task CreateDecision_WithNonExistentWorkflow_ShouldReturn400()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var nonExistentWorkflowId = Guid.NewGuid();
        var request = new CreateDecisionRequest
        {
            WorkflowInstanceId = nonExistentWorkflowId,
            StepId = "step-1",
            DecisionType = "approval",
            Value = JsonDocument.Parse("{\"approved\": true}").RootElement
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/decisions", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetDecisionsByWorkflowInstance_WithValidWorkflowId_ShouldReturn200()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var workflowId = await CreateWorkflowInstanceAsync();

        // Create multiple decisions
        for (int i = 1; i <= 3; i++)
        {
            var request = new CreateDecisionRequest
            {
                WorkflowInstanceId = workflowId,
                StepId = $"step-{i}",
                DecisionType = "approval",
                Value = JsonDocument.Parse($"{{\"step\": {i}}}").RootElement
            };
            await _client.PostAsJsonAsync("/api/v1/decisions", request);
            await Task.Delay(10); // Small delay to ensure different timestamps
        }

        // Act
        var response = await _client.GetAsync($"/api/v1/workflows/{workflowId}/decisions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var decisions = await response.Content.ReadFromJsonAsync<List<DecisionResponse>>();
        decisions.Should().NotBeNull();
        decisions!.Count.Should().Be(3);
        decisions.Should().BeInAscendingOrder(d => d.DecidedAt);
        decisions[0].StepId.Should().Be("step-1");
        decisions[1].StepId.Should().Be("step-2");
        decisions[2].StepId.Should().Be("step-3");
    }

    [Fact]
    public async Task GetDecisionsByWorkflowInstance_WithNoDecisions_ShouldReturnEmptyList()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var workflowId = await CreateWorkflowInstanceAsync();

        // Act
        var response = await _client.GetAsync($"/api/v1/workflows/{workflowId}/decisions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var decisions = await response.Content.ReadFromJsonAsync<List<DecisionResponse>>();
        decisions.Should().NotBeNull();
        decisions!.Count.Should().Be(0);
    }

    [Fact]
    public async Task GetDecisionById_WithValidId_ShouldReturn200()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var workflowId = await CreateWorkflowInstanceAsync();

        // Create a decision
        var createRequest = new CreateDecisionRequest
        {
            WorkflowInstanceId = workflowId,
            StepId = "step-1",
            DecisionType = "selection",
            Value = JsonDocument.Parse("{\"option\": \"Option A\"}").RootElement,
            Question = "Select an option",
            Options = JsonDocument.Parse("[\"Option A\", \"Option B\"]").RootElement
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/decisions", createRequest);
        var createdDecision = await createResponse.Content.ReadFromJsonAsync<DecisionResponse>();

        // Act
        var response = await _client.GetAsync($"/api/v1/decisions/{createdDecision!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var decision = await response.Content.ReadFromJsonAsync<DecisionResponse>();
        decision.Should().NotBeNull();
        decision!.Id.Should().Be(createdDecision.Id);
        decision.StepId.Should().Be("step-1");
        decision.DecisionType.Should().Be("selection");
        decision.Question.Should().Be("Select an option");
    }

    [Fact]
    public async Task GetDecisionById_WithInvalidId_ShouldReturn404()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/v1/decisions/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateDecision_WithComplexStructuredData_ShouldStoreAndRetrieveCorrectly()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var workflowId = await CreateWorkflowInstanceAsync();

        var complexValue = JsonDocument.Parse(@"{
            ""budget"": 50000,
            ""timeline"": ""6 months"",
            ""team"": [""Alice"", ""Bob"", ""Charlie""],
            ""milestones"": {
                ""phase1"": ""Complete"",
                ""phase2"": ""In Progress""
            }
        }");

        var request = new CreateDecisionRequest
        {
            WorkflowInstanceId = workflowId,
            StepId = "planning",
            DecisionType = "configuration",
            Value = complexValue.RootElement
        };

        // Act
        var createResponse = await _client.PostAsJsonAsync("/api/v1/decisions", request);
        var createdDecision = await createResponse.Content.ReadFromJsonAsync<DecisionResponse>();

        var getResponse = await _client.GetAsync($"/api/v1/decisions/{createdDecision!.Id}");
        var retrievedDecision = await getResponse.Content.ReadFromJsonAsync<DecisionResponse>();

        // Assert
        retrievedDecision.Should().NotBeNull();
        retrievedDecision!.Value.GetProperty("budget").GetInt32().Should().Be(50000);
        retrievedDecision.Value.GetProperty("timeline").GetString().Should().Be("6 months");
        retrievedDecision.Value.GetProperty("team").GetArrayLength().Should().Be(3);
        retrievedDecision.Value.GetProperty("milestones").GetProperty("phase1").GetString().Should().Be("Complete");
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

    private async Task<Guid> CreateWorkflowInstanceAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Get the first user from the database
        var user = await dbContext.Users.FirstAsync();

        var instance = new WorkflowInstance
        {
            Id = Guid.NewGuid(),
            WorkflowDefinitionId = "test-workflow",
            UserId = user.Id,
            Status = WorkflowStatus.Running,
            CurrentStep = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        dbContext.WorkflowInstances.Add(instance);
        await dbContext.SaveChangesAsync();

        return instance.Id;
    }
}
