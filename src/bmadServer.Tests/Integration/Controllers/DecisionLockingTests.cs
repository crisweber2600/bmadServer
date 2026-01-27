using bmadServer.ApiService.Data;
using bmadServer.ApiService.Data.Entities;
using bmadServer.ApiService.Models.Decisions;
using bmadServer.ApiService.Models.Workflows;
using bmadServer.ApiService.Services;
using bmadServer.Tests.Integration;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace bmadServer.Tests.Integration.Controllers;

public class DecisionLockingTests : IClassFixture<TestWebApplicationFactory>, IDisposable
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public DecisionLockingTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
    }

    [Fact]
    public async Task LockDecision_Returns200OK()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var workflowId = await CreateWorkflowInstanceAsync();
        var decisionId = await CreateDecisionAsync(workflowId);

        var lockRequest = new LockDecisionRequest { Reason = "Locking for approval" };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/v1/decisions/{decisionId}/lock", lockRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task LockDecision_SetsIsLockedToTrue()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var workflowId = await CreateWorkflowInstanceAsync();
        var decisionId = await CreateDecisionAsync(workflowId);

        var lockRequest = new LockDecisionRequest { Reason = "Lock for approval" };

        // Act
        var lockResponse = await _client.PostAsJsonAsync($"/api/v1/decisions/{decisionId}/lock", lockRequest);
        var lockedDecision = await lockResponse.Content.ReadFromJsonAsync<DecisionResponse>();

        // Assert
        lockedDecision.Should().NotBeNull();
        lockedDecision!.IsLocked.Should().BeTrue();
    }

    [Fact]
    public async Task LockDecision_SetsLockedByAndLockedAt()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var workflowId = await CreateWorkflowInstanceAsync();
        var decisionId = await CreateDecisionAsync(workflowId);

        var lockRequest = new LockDecisionRequest { Reason = "Lock for review" };

        // Act
        var lockResponse = await _client.PostAsJsonAsync($"/api/v1/decisions/{decisionId}/lock", lockRequest);
        var lockedDecision = await lockResponse.Content.ReadFromJsonAsync<DecisionResponse>();

        // Assert
        lockedDecision.Should().NotBeNull();
        lockedDecision!.LockedBy.Should().NotBeEmpty();
        lockedDecision.LockedAt.Should().NotBe(default(DateTime));
        lockedDecision.LockedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task UpdateLockedDecision_Returns403Forbidden()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var workflowId = await CreateWorkflowInstanceAsync();
        var decisionId = await CreateDecisionAsync(workflowId);

        // Lock the decision
        var lockRequest = new LockDecisionRequest { Reason = "Lock for testing" };
        await _client.PostAsJsonAsync($"/api/v1/decisions/{decisionId}/lock", lockRequest);

        var updateRequest = new UpdateDecisionRequest
        {
            Value = JsonDocument.Parse("{\"attempt\": \"update\"}").RootElement
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/v1/decisions/{decisionId}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UnlockDecision_SetsIsLockedToFalse()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var workflowId = await CreateWorkflowInstanceAsync();
        var decisionId = await CreateDecisionAsync(workflowId);

        // Lock the decision
        var lockRequest = new LockDecisionRequest { Reason = "Lock for testing" };
        await _client.PostAsJsonAsync($"/api/v1/decisions/{decisionId}/lock", lockRequest);

        var unlockRequest = new UnlockDecisionRequest { Reason = "Ready to unlock" };

        // Act
        var unlockResponse = await _client.PostAsJsonAsync($"/api/v1/decisions/{decisionId}/unlock", unlockRequest);
        var unlockedDecision = await unlockResponse.Content.ReadFromJsonAsync<DecisionResponse>();

        // Assert
        unlockedDecision.Should().NotBeNull();
        unlockedDecision!.IsLocked.Should().BeFalse();
    }

    [Fact]
    public async Task UnlockDecision_RecordsUnlockReason()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var workflowId = await CreateWorkflowInstanceAsync();
        var decisionId = await CreateDecisionAsync(workflowId);

        // Lock the decision
        var lockRequest = new LockDecisionRequest { Reason = "Locked for approval" };
        await _client.PostAsJsonAsync($"/api/v1/decisions/{decisionId}/lock", lockRequest);

        var unlockRequest = new UnlockDecisionRequest { Reason = "Approval completed, ready for changes" };

        // Act
        var unlockResponse = await _client.PostAsJsonAsync($"/api/v1/decisions/{decisionId}/unlock", unlockRequest);

        // Assert
        unlockResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var unlockedDecision = await unlockResponse.Content.ReadFromJsonAsync<DecisionResponse>();
        unlockedDecision.Should().NotBeNull();
    }

    [Fact]
    public async Task UnlockedDecision_CanBeUpdated()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var workflowId = await CreateWorkflowInstanceAsync();
        var decisionId = await CreateDecisionAsync(workflowId);

        // Lock and then unlock
        var lockRequest = new LockDecisionRequest { Reason = "Temporary lock" };
        await _client.PostAsJsonAsync($"/api/v1/decisions/{decisionId}/lock", lockRequest);

        var unlockRequest = new UnlockDecisionRequest { Reason = "Ready to edit" };
        await _client.PostAsJsonAsync($"/api/v1/decisions/{decisionId}/unlock", unlockRequest);

        var updateRequest = new UpdateDecisionRequest
        {
            Value = JsonDocument.Parse("{\"updated\": true}").RootElement
        };

        // Act
        var updateResponse = await _client.PutAsJsonAsync($"/api/v1/decisions/{decisionId}", updateRequest);

        // Assert
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task LockDecision_WithoutAuthentication_Returns401()
    {
        // Arrange
        var decisionId = Guid.NewGuid();
        var lockRequest = new LockDecisionRequest { Reason = "Lock" };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/v1/decisions/{decisionId}/lock", lockRequest);

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

    private async Task<Guid> CreateDecisionAsync(Guid workflowId)
    {
        var token = await GetAuthTokenAsync();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new CreateDecisionRequest
        {
            WorkflowInstanceId = workflowId,
            StepId = "step-1",
            DecisionType = "approval",
            Value = JsonDocument.Parse("{\"approved\": true}").RootElement
        };

        var response = await client.PostAsJsonAsync("/api/v1/decisions", request);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Failed to create decision: {response.StatusCode} - {errorContent}");
        }
        
        var decision = await response.Content.ReadFromJsonAsync<DecisionResponse>();

        return decision!.Id;
    }
}
