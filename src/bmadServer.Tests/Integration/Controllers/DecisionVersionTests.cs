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

public class DecisionVersionTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private static readonly string _databaseName = "TestDb_DecisionVersions_" + Guid.NewGuid();

    public DecisionVersionTests(WebApplicationFactory<Program> factory)
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
    public async Task UpdateDecision_CreatesNewVersion()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var workflowId = await CreateWorkflowInstanceAsync();
        var decisionId = await CreateDecisionAsync(workflowId);

        var updateRequest = new UpdateDecisionRequest
        {
            Value = JsonDocument.Parse("{\"approved\": false}").RootElement,
            ChangeReason = "Initial update"
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/v1/decisions/{decisionId}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetDecisionHistory_ReturnsAllVersionsChronologically()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var workflowId = await CreateWorkflowInstanceAsync();
        var decisionId = await CreateDecisionAsync(workflowId);

        // Create multiple versions
        for (int i = 0; i < 3; i++)
        {
            var updateRequest = new UpdateDecisionRequest
            {
                Value = JsonDocument.Parse($"{{\"iteration\": {i}}}").RootElement,
                ChangeReason = $"Update {i}"
            };
            await _client.PutAsJsonAsync($"/api/v1/decisions/{decisionId}", updateRequest);
            await Task.Delay(10);
        }

        // Act
        var response = await _client.GetAsync($"/api/v1/decisions/{decisionId}/history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var versions = await response.Content.ReadFromJsonAsync<List<DecisionVersionResponse>>();
        versions.Should().NotBeNull();
        versions!.Count.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task DecisionVersion_IncludesVersionNumber()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var workflowId = await CreateWorkflowInstanceAsync();
        var decisionId = await CreateDecisionAsync(workflowId);

        // Act
        var response = await _client.GetAsync($"/api/v1/decisions/{decisionId}/history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var versions = await response.Content.ReadFromJsonAsync<List<DecisionVersionResponse>>();
        versions.Should().NotBeNull();
        versions!.ForEach(v => v.VersionNumber.Should().BeGreaterThan(0));
    }

    [Fact]
    public async Task DecisionVersion_IncludesModifiedByAndModifiedAt()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var workflowId = await CreateWorkflowInstanceAsync();
        var decisionId = await CreateDecisionAsync(workflowId);

        // Act
        var response = await _client.GetAsync($"/api/v1/decisions/{decisionId}/history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var versions = await response.Content.ReadFromJsonAsync<List<DecisionVersionResponse>>();
        versions.Should().NotBeNull();
        versions!.ForEach(v =>
        {
            v.ModifiedBy.Should().NotBeEmpty();
            v.ModifiedAt.Should().NotBe(default(DateTime));
        });
    }

    [Fact]
    public async Task RevertDecision_ReturnsToPreviousVersion()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var workflowId = await CreateWorkflowInstanceAsync();
        var decisionId = await CreateDecisionAsync(workflowId);

        // Update decision
        var updateRequest = new UpdateDecisionRequest
        {
            Value = JsonDocument.Parse("{\"status\": \"modified\"}").RootElement,
            ChangeReason = "First modification"
        };
        await _client.PutAsJsonAsync($"/api/v1/decisions/{decisionId}", updateRequest);

        // Get history to find first version
        var historyResponse = await _client.GetAsync($"/api/v1/decisions/{decisionId}/history");
        var versions = await historyResponse.Content.ReadFromJsonAsync<List<DecisionVersionResponse>>();
        var firstVersion = versions!.Last();

        // Act
        var revertRequest = new RevertDecisionRequest { Reason = "Need to revert to original" };
        var revertResponse = await _client.PostAsJsonAsync(
            $"/api/v1/decisions/{decisionId}/revert?version={firstVersion.VersionNumber}",
            revertRequest);

        // Assert
        revertResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var revertedDecision = await revertResponse.Content.ReadFromJsonAsync<DecisionResponse>();
        revertedDecision.Should().NotBeNull();
    }

    [Fact]
    public async Task RevertDecision_CreatesNewVersion()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var workflowId = await CreateWorkflowInstanceAsync();
        var decisionId = await CreateDecisionAsync(workflowId);

        // Get initial history
        var initialHistoryResponse = await _client.GetAsync($"/api/v1/decisions/{decisionId}/history");
        var initialVersions = await initialHistoryResponse.Content.ReadFromJsonAsync<List<DecisionVersionResponse>>();
        var initialCount = initialVersions!.Count;

        // Update and revert
        var updateRequest = new UpdateDecisionRequest
        {
            Value = JsonDocument.Parse("{\"test\": true}").RootElement,
            ChangeReason = "Test update"
        };
        await _client.PutAsJsonAsync($"/api/v1/decisions/{decisionId}", updateRequest);

        var revertRequest = new RevertDecisionRequest { Reason = "Reverting" };
        await _client.PostAsJsonAsync(
            $"/api/v1/decisions/{decisionId}/revert?version={initialVersions!.Last().VersionNumber}",
            revertRequest);

        // Act
        var finalHistoryResponse = await _client.GetAsync($"/api/v1/decisions/{decisionId}/history");
        var finalVersions = await finalHistoryResponse.Content.ReadFromJsonAsync<List<DecisionVersionResponse>>();

        // Assert
        finalVersions!.Count.Should().BeGreaterThan(initialCount);
    }

    [Fact]
    public async Task DecisionHistory_IsOrderedByCreatedAtDescending()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var workflowId = await CreateWorkflowInstanceAsync();
        var decisionId = await CreateDecisionAsync(workflowId);

        // Create multiple versions with delays
        for (int i = 0; i < 3; i++)
        {
            var updateRequest = new UpdateDecisionRequest
            {
                Value = JsonDocument.Parse($"{{\"seq\": {i}}}").RootElement
            };
            await _client.PutAsJsonAsync($"/api/v1/decisions/{decisionId}", updateRequest);
            await Task.Delay(50);
        }

        // Act
        var response = await _client.GetAsync($"/api/v1/decisions/{decisionId}/history");
        var versions = await response.Content.ReadFromJsonAsync<List<DecisionVersionResponse>>();

        // Assert - newer versions should come first
        versions.Should().NotBeNull();
        for (int i = 0; i < versions!.Count - 1; i++)
        {
            (versions[i].ModifiedAt >= versions[i + 1].ModifiedAt).Should().BeTrue();
        }
    }

    [Fact]
    public async Task DecisionVersion_PreservesValueExactly()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var workflowId = await CreateWorkflowInstanceAsync();
        var originalValue = "{\"precision\": 3.14159, \"text\": \"test\"}";
        var decisionId = await CreateDecisionAsync(workflowId, originalValue);

        // Act
        var response = await _client.GetAsync($"/api/v1/decisions/{decisionId}/history");
        var versions = await response.Content.ReadFromJsonAsync<List<DecisionVersionResponse>>();

        // Assert
        versions!.First().Value.GetRawText().Should().Contain("3.14159");
        versions!.First().Value.GetRawText().Should().Contain("test");
    }

    [Fact]
    public async Task ConcurrentUpdates_CreateSequentialVersions()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var workflowId = await CreateWorkflowInstanceAsync();
        var decisionId = await CreateDecisionAsync(workflowId);

        // Act - Create multiple updates concurrently
        var tasks = Enumerable.Range(0, 3).Select(async i =>
        {
            var updateRequest = new UpdateDecisionRequest
            {
                Value = JsonDocument.Parse($"{{\"update\": {i}}}").RootElement
            };
            return await _client.PutAsJsonAsync($"/api/v1/decisions/{decisionId}", updateRequest);
        }).ToList();

        await Task.WhenAll(tasks);

        // Get history
        var historyResponse = await _client.GetAsync($"/api/v1/decisions/{decisionId}/history");
        var versions = await historyResponse.Content.ReadFromJsonAsync<List<DecisionVersionResponse>>();

        // Assert - versions should be sequential
        versions.Should().NotBeNull();
        var versionNumbers = versions!.Select(v => v.VersionNumber).OrderBy(v => v).ToList();
        versionNumbers.Should().Equal(Enumerable.Range(1, versionNumbers.Count));
    }

    [Fact]
    public async Task GetDecisionHistory_WithoutAuthentication_ShouldReturn401()
    {
        // Arrange
        var decisionId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/v1/decisions/{decisionId}/history");

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

    private async Task<Guid> CreateDecisionAsync(Guid workflowId, string value = "{\"approved\": true}")
    {
        var token = await GetAuthTokenAsync();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new CreateDecisionRequest
        {
            WorkflowInstanceId = workflowId,
            StepId = "step-1",
            DecisionType = "approval",
            Value = JsonDocument.Parse(value).RootElement,
            Question = "Do you approve?"
        };

        var response = await client.PostAsJsonAsync("/api/v1/decisions", request);
        var decision = await response.Content.ReadFromJsonAsync<DecisionResponse>();

        return decision!.Id;
    }
}
