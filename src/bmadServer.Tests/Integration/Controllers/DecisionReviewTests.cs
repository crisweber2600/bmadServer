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
using DecisionReviewModelResponse = bmadServer.ApiService.Models.Decisions.DecisionReviewResponse;

namespace bmadServer.Tests.Integration.Controllers;

public class DecisionReviewTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private static readonly string _databaseName = "TestDb_DecisionReview_" + Guid.NewGuid();

    public DecisionReviewTests(WebApplicationFactory<Program> factory)
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
    public async Task RequestReview_Returns200OK()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var workflowId = await CreateWorkflowInstanceAsync();
        var decisionId = await CreateDecisionAsync(workflowId);
        var reviewerId = await CreateUserAsync();

        var request = new RequestReviewRequest
        {
            ReviewerIds = new List<Guid> { reviewerId }
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/v1/decisions/{decisionId}/request-review", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RequestReview_CreatesDecisionReviewRecord()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var workflowId = await CreateWorkflowInstanceAsync();
        var decisionId = await CreateDecisionAsync(workflowId);
        var reviewerId = await CreateUserAsync();

        var request = new RequestReviewRequest
        {
            ReviewerIds = new List<Guid> { reviewerId }
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/v1/decisions/{decisionId}/request-review", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var review = await response.Content.ReadFromJsonAsync<DecisionReviewModelResponse>();
        review.Should().NotBeNull();
        review!.DecisionId.Should().Be(decisionId);
    }

    [Fact]
    public async Task RequestReview_StoresReviewerIds()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var workflowId = await CreateWorkflowInstanceAsync();
        var decisionId = await CreateDecisionAsync(workflowId);
        var reviewer1 = await CreateUserAsync();
        var reviewer2 = await CreateUserAsync();

        var request = new RequestReviewRequest
        {
            ReviewerIds = new List<Guid> { reviewer1, reviewer2 }
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/v1/decisions/{decisionId}/request-review", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var review = await response.Content.ReadFromJsonAsync<DecisionReviewModelResponse>();
        review.Should().NotBeNull();
    }

    [Fact]
    public async Task ReviewerCanViewDecisionWhenInvited()
    {
        // Arrange
        var creatorToken = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", creatorToken);

        var workflowId = await CreateWorkflowInstanceAsync();
        var decisionId = await CreateDecisionAsync(workflowId);
        var reviewerId = await CreateUserAsync();

        var request = new RequestReviewRequest
        {
            ReviewerIds = new List<Guid> { reviewerId }
        };

        await _client.PostAsJsonAsync($"/api/v1/decisions/{decisionId}/request-review", request);

        // Act - Try to view as reviewer
        var reviewerClient = _factory.CreateClient();
        var reviewerToken = await GetAuthTokenForUserAsync(reviewerId);
        reviewerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", reviewerToken);

        var viewResponse = await reviewerClient.GetAsync($"/api/v1/decisions/{decisionId}");

        // Assert
        viewResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ApproveDecision_UpdatesStatusToApproved()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var workflowId = await CreateWorkflowInstanceAsync();
        var decisionId = await CreateDecisionAsync(workflowId);
        var reviewerId = await CreateUserAsync();

        var request = new RequestReviewRequest
        {
            ReviewerIds = new List<Guid> { reviewerId }
        };

        var reviewResponse = await _client.PostAsJsonAsync($"/api/v1/decisions/{decisionId}/request-review", request);
        var review = await reviewResponse.Content.ReadFromJsonAsync<DecisionReviewModelResponse>();

        // Act
        var approveRequest = new SubmitReviewResponse
        {
            ReviewId = review!.Id,
            Status = "Approved",
            Comments = "Looks good"
        };

        var approveResponse = await _client.PostAsJsonAsync(
            $"/api/v1/decisions/{decisionId}/review-response",
            approveRequest);

        // Assert
        approveResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ChangesRequestedDecision_ReturnsToDraft()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var workflowId = await CreateWorkflowInstanceAsync();
        var decisionId = await CreateDecisionAsync(workflowId);
        var reviewerId = await CreateUserAsync();

        var request = new RequestReviewRequest
        {
            ReviewerIds = new List<Guid> { reviewerId }
        };

        var reviewResponse = await _client.PostAsJsonAsync($"/api/v1/decisions/{decisionId}/request-review", request);
        var review = await reviewResponse.Content.ReadFromJsonAsync<DecisionReviewModelResponse>();

        // Act
        var changesRequest = new SubmitReviewResponse
        {
            ReviewId = review!.Id,
            Status = "ChangesRequested",
            Comments = "Please update"
        };

        var changesResponse = await _client.PostAsJsonAsync(
            $"/api/v1/decisions/{decisionId}/review-response",
            changesRequest);

        // Assert
        changesResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AllReviewersApprove_LocksDecision()
    {
        // Arrange
        var creatorToken = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", creatorToken);

        var workflowId = await CreateWorkflowInstanceAsync();
        var decisionId = await CreateDecisionAsync(workflowId);
        var reviewerId = await CreateUserAsync();

        var request = new RequestReviewRequest
        {
            ReviewerIds = new List<Guid> { reviewerId }
        };

        var reviewResponse = await _client.PostAsJsonAsync($"/api/v1/decisions/{decisionId}/request-review", request);
        var review = await reviewResponse.Content.ReadFromJsonAsync<DecisionReviewModelResponse>();

        // Act - Reviewer approves
        var reviewerClient = _factory.CreateClient();
        var reviewerToken = await GetAuthTokenForUserAsync(reviewerId);
        reviewerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", reviewerToken);

        var approveRequest = new SubmitReviewResponse
        {
            ReviewId = review!.Id,
            Status = "Approved",
            Comments = "Approved"
        };

        await reviewerClient.PostAsJsonAsync($"/api/v1/decisions/{decisionId}/review-response", approveRequest);

        // Assert - Decision should be locked
        var finalResponse = await _client.GetAsync($"/api/v1/decisions/{decisionId}");
        var decision = await finalResponse.Content.ReadFromJsonAsync<DecisionResponse>();
        decision!.IsLocked.Should().BeTrue();
    }

    [Fact]
    public async Task PartialApprovals_DontLock()
    {
        // Arrange
        var creatorToken = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", creatorToken);

        var workflowId = await CreateWorkflowInstanceAsync();
        var decisionId = await CreateDecisionAsync(workflowId);
        var reviewer1 = await CreateUserAsync();
        var reviewer2 = await CreateUserAsync();

        var request = new RequestReviewRequest
        {
            ReviewerIds = new List<Guid> { reviewer1, reviewer2 }
        };

        var reviewResponse = await _client.PostAsJsonAsync($"/api/v1/decisions/{decisionId}/request-review", request);
        var review = await reviewResponse.Content.ReadFromJsonAsync<DecisionReviewModelResponse>();

        // Act - Only one reviewer approves
        var reviewer1Client = _factory.CreateClient();
        var reviewer1Token = await GetAuthTokenForUserAsync(reviewer1);
        reviewer1Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", reviewer1Token);

        var approveRequest = new SubmitReviewResponse
        {
            ReviewId = review!.Id,
            Status = "Approved",
            Comments = "Approved"
        };

        await reviewer1Client.PostAsJsonAsync($"/api/v1/decisions/{decisionId}/review-response", approveRequest);

        // Assert
        var finalResponse = await _client.GetAsync($"/api/v1/decisions/{decisionId}");
        var decision = await finalResponse.Content.ReadFromJsonAsync<DecisionResponse>();
        decision!.IsLocked.Should().BeFalse();
    }

    [Fact]
    public async Task ReviewerCannotDoubleApprove()
    {
        // Arrange
        var creatorToken = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", creatorToken);

        var workflowId = await CreateWorkflowInstanceAsync();
        var decisionId = await CreateDecisionAsync(workflowId);
        var reviewerId = await CreateUserAsync();

        var request = new RequestReviewRequest
        {
            ReviewerIds = new List<Guid> { reviewerId }
        };

        var reviewResponse = await _client.PostAsJsonAsync($"/api/v1/decisions/{decisionId}/request-review", request);
        var review = await reviewResponse.Content.ReadFromJsonAsync<DecisionReviewModelResponse>();

        // First approval
        var reviewerClient = _factory.CreateClient();
        var reviewerToken = await GetAuthTokenForUserAsync(reviewerId);
        reviewerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", reviewerToken);

        var approveRequest = new SubmitReviewResponse
        {
            ReviewId = review!.Id,
            Status = "Approved",
            Comments = "Approved"
        };

        await reviewerClient.PostAsJsonAsync($"/api/v1/decisions/{decisionId}/review-response", approveRequest);

        // Act - Try to approve again
        var secondApproveResponse = await reviewerClient.PostAsJsonAsync(
            $"/api/v1/decisions/{decisionId}/review-response",
            approveRequest);

        // Assert - Should return error
        secondApproveResponse.StatusCode.Should().NotBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RequestReview_WithEmptyReviewerIds_ReturnsBadRequest()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var workflowId = await CreateWorkflowInstanceAsync();
        var decisionId = await CreateDecisionAsync(workflowId);

        var request = new RequestReviewRequest
        {
            ReviewerIds = new List<Guid>()
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/v1/decisions/{decisionId}/request-review", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RequestReview_WithoutAuthentication_Returns401()
    {
        // Arrange
        var decisionId = Guid.NewGuid();
        var request = new RequestReviewRequest
        {
            ReviewerIds = new List<Guid> { Guid.NewGuid() }
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/v1/decisions/{decisionId}/request-review", request);

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

    private async Task<string> GetAuthTokenForUserAsync(Guid userId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var jwtTokenService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();

        var user = await dbContext.Users.FirstAsync(u => u.Id == userId);
        return jwtTokenService.GenerateAccessToken(user);
    }

    private async Task<Guid> CreateUserAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var user = new User
        {
            Email = $"reviewer-{Guid.NewGuid()}@example.com",
            PasswordHash = "hash",
            DisplayName = "Reviewer",
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        return user.Id;
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
        var decision = await response.Content.ReadFromJsonAsync<DecisionResponse>();

        return decision!.Id;
    }
}
