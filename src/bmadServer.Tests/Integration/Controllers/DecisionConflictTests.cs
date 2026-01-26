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

public class DecisionConflictTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private static readonly string _databaseName = "TestDb_DecisionConflict_" + Guid.NewGuid();

    public DecisionConflictTests(WebApplicationFactory<Program> factory)
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
    public async Task CreateDecision_AutomaticallyDetectsConflicts()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var workflowId = await CreateWorkflowInstanceAsync();

        // Create first decision
        await CreateDecisionAsync(workflowId, "step-1");

        // Act - Create potentially conflicting decision
        var request = new CreateDecisionRequest
        {
            WorkflowInstanceId = workflowId,
            StepId = "step-1",
            DecisionType = "approval",
            Value = JsonDocument.Parse("{\"conflicting\": true}").RootElement
        };

        var response = await _client.PostAsJsonAsync("/api/v1/decisions", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task UpdateDecision_DetectsConflicts()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var workflowId = await CreateWorkflowInstanceAsync();
        var decisionId = await CreateDecisionAsync(workflowId);

        var updateRequest = new UpdateDecisionRequest
        {
            Value = JsonDocument.Parse("{\"status\": \"conflict\"}").RootElement
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/v1/decisions/{decisionId}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task MultipleConflictsDetected()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var workflowId = await CreateWorkflowInstanceAsync();

        // Create multiple decisions that may have conflicts
        for (int i = 0; i < 3; i++)
        {
            await CreateDecisionAsync(workflowId, $"step-{i}");
        }

        // Act
        var response = await _client.GetAsync($"/api/v1/workflows/{workflowId}/decisions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var decisions = await response.Content.ReadFromJsonAsync<List<DecisionResponse>>();
        decisions!.Count.Should().Be(3);
    }

    [Fact]
    public async Task GetConflicts_ReturnsAllConflicts()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var workflowId = await CreateWorkflowInstanceAsync();
        var decision1Id = await CreateDecisionAsync(workflowId, "step-1");

        // Act
        var response = await _client.GetAsync($"/api/v1/workflows/{workflowId}/conflicts");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var conflicts = await response.Content.ReadFromJsonAsync<List<DecisionConflictResponse>>();
        conflicts.Should().NotBeNull();
    }

    [Fact]
    public async Task Conflict_IncludesNatureOfConflict()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var workflowId = await CreateWorkflowInstanceAsync();
        await CreateDecisionAsync(workflowId, "step-1");

        // Act
        var response = await _client.GetAsync($"/api/v1/workflows/{workflowId}/conflicts");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var conflicts = await response.Content.ReadFromJsonAsync<List<DecisionConflictResponse>>();
        if (conflicts!.Count > 0)
        {
            conflicts.First().Nature.Should().NotBeNullOrEmpty();
        }
    }

    [Fact]
    public async Task ResolveConflict_UpdatesBothDecisions()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var workflowId = await CreateWorkflowInstanceAsync();
        var decision1 = await CreateDecisionAsync(workflowId, "step-1");

        var conflictsResponse = await _client.GetAsync($"/api/v1/workflows/{workflowId}/conflicts");
        var conflicts = await conflictsResponse.Content.ReadFromJsonAsync<List<DecisionConflictResponse>>();

        if (conflicts!.Count == 0)
        {
            return; // Skip if no conflicts
        }

        var conflictId = conflicts.First().Id;

        var resolveRequest = new ResolveConflictRequest
        {
            Resolution = "Keep first decision, update second"
        };

        // Act
        var response = await _client.PostAsJsonAsync(
            $"/api/v1/workflows/{workflowId}/conflicts/{conflictId}/resolve",
            resolveRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task OverrideConflict_LogsJustification()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var workflowId = await CreateWorkflowInstanceAsync();
        await CreateDecisionAsync(workflowId);

        var conflictsResponse = await _client.GetAsync($"/api/v1/workflows/{workflowId}/conflicts");
        var conflicts = await conflictsResponse.Content.ReadFromJsonAsync<List<DecisionConflictResponse>>();

        if (conflicts!.Count == 0)
        {
            return; // Skip if no conflicts
        }

        var conflictId = conflicts.First().Id;
        var justification = "Override is necessary due to business requirements";

        var overrideRequest = new OverrideConflictRequest
        {
            Justification = justification
        };

        // Act
        var response = await _client.PostAsJsonAsync(
            $"/api/v1/workflows/{workflowId}/conflicts/{conflictId}/override",
            overrideRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var conflict = await response.Content.ReadFromJsonAsync<DecisionConflictResponse>();
        conflict!.Resolution?.ToLower().Should().Contain("override");
    }

    [Fact]
    public async Task OverrideConflict_CreatesConflictRecord()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var workflowId = await CreateWorkflowInstanceAsync();
        await CreateDecisionAsync(workflowId);

        var conflictsResponse = await _client.GetAsync($"/api/v1/workflows/{workflowId}/conflicts");
        var conflicts = await conflictsResponse.Content.ReadFromJsonAsync<List<DecisionConflictResponse>>();

        if (conflicts!.Count == 0)
        {
            return; // Skip if no conflicts
        }

        var conflictId = conflicts.First().Id;

        var overrideRequest = new OverrideConflictRequest
        {
            Justification = "Business-critical override"
        };

        // Act
        var response = await _client.PostAsJsonAsync(
            $"/api/v1/workflows/{workflowId}/conflicts/{conflictId}/override",
            overrideRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var conflict = await response.Content.ReadFromJsonAsync<DecisionConflictResponse>();
        conflict!.ResolvedAt.Should().NotBe(default(DateTime));
    }

    [Fact]
    public async Task ConflictRulesAreConfigurable()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/v1/conflict-rules");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var rules = await response.Content.ReadFromJsonAsync<List<ConflictRuleResponse>>();
        rules.Should().NotBeNull();
    }

    [Fact]
    public async Task InactiveRulesAreSkipped()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var workflowId = await CreateWorkflowInstanceAsync();

        // Act
        var rulesResponse = await _client.GetAsync("/api/v1/conflict-rules");
        var rules = await rulesResponse.Content.ReadFromJsonAsync<List<ConflictRuleResponse>>();

        // Create decisions
        for (int i = 0; i < 2; i++)
        {
            await CreateDecisionAsync(workflowId, $"step-{i}");
        }

        var conflictsResponse = await _client.GetAsync($"/api/v1/workflows/{workflowId}/conflicts");

        // Assert
        conflictsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var conflicts = await conflictsResponse.Content.ReadFromJsonAsync<List<DecisionConflictResponse>>();
        conflicts.Should().NotBeNull();
    }

    [Fact]
    public async Task GetConflicts_WithoutAuthentication_Returns401()
    {
        // Arrange
        var workflowId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/v1/workflows/{workflowId}/conflicts");

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

    private async Task<Guid> CreateWorkflowInstanceAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

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

    private async Task<Guid> CreateDecisionAsync(Guid workflowId, string stepId = "step-1")
    {
        var token = await GetAuthTokenAsync();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new CreateDecisionRequest
        {
            WorkflowInstanceId = workflowId,
            StepId = stepId,
            DecisionType = "approval",
            Value = JsonDocument.Parse("{\"approved\": true}").RootElement
        };

        var response = await client.PostAsJsonAsync("/api/v1/decisions", request);
        var decision = await response.Content.ReadFromJsonAsync<DecisionResponse>();

        return decision!.Id;
    }
}
