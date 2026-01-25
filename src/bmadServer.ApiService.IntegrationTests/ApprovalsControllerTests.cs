using System.Net;
using System.Net.Http.Json;
using bmadServer.ApiService.Agents;
using bmadServer.ApiService.Data;
using bmadServer.ApiService.Data.Entities;
using bmadServer.ApiService.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace bmadServer.ApiService.IntegrationTests;

public class ApprovalsControllerTests : IAsyncLifetime
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private readonly string _databaseName = Guid.NewGuid().ToString();

    public async Task InitializeAsync()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Test");
            
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
                    options.UseInMemoryDatabase(databaseName: _databaseName);
                });
            });
        });
        
        _client = _factory.CreateClient();
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task GetApprovalRequest_ExistingRequest_ReturnsOk()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var approvalRequest = new ApprovalRequest
        {
            WorkflowInstanceId = Guid.NewGuid(),
            AgentId = "architect",
            ProposedResponse = "Proposed architecture design",
            ConfidenceScore = 0.65,
            Reasoning = "Based on limited context",
            Status = "Pending"
        };
        
        context.ApprovalRequests.Add(approvalRequest);
        await context.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync($"/api/approvals/{approvalRequest.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<ApprovalRequestDto>();
        Assert.NotNull(dto);
        Assert.Equal(approvalRequest.Id, dto.Id);
        Assert.Equal(approvalRequest.ConfidenceScore, dto.ConfidenceScore);
        Assert.Equal("Pending", dto.Status);
    }

    [Fact]
    public async Task GetApprovalRequest_NonExistent_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/approvals/{nonExistentId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Approve_PendingRequest_ReturnsOk()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var approvalRequest = new ApprovalRequest
        {
            WorkflowInstanceId = Guid.NewGuid(),
            AgentId = "architect",
            ProposedResponse = "Proposed solution",
            ConfidenceScore = 0.65,
            Status = "Pending"
        };
        
        context.ApprovalRequests.Add(approvalRequest);
        await context.SaveChangesAsync();
        
        var userId = Guid.NewGuid();
        var approveDto = new ApproveRequestDto { UserId = userId };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/approvals/{approvalRequest.Id}/approve", approveDto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        // Create a new scope to get a fresh context
        using var verifyScope = _factory.Services.CreateScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var updated = await verifyContext.ApprovalRequests.FirstOrDefaultAsync(ar => ar.Id == approvalRequest.Id);
        Assert.NotNull(updated);
        Assert.Equal("Approved", updated.Status);
        Assert.Equal(userId, updated.ApprovedByUserId);
    }

    [Fact]
    public async Task Approve_NonPendingRequest_ReturnsBadRequest()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var approvalRequest = new ApprovalRequest
        {
            WorkflowInstanceId = Guid.NewGuid(),
            AgentId = "architect",
            ProposedResponse = "Proposed solution",
            ConfidenceScore = 0.65,
            Status = "Approved"
        };
        
        context.ApprovalRequests.Add(approvalRequest);
        await context.SaveChangesAsync();
        
        var approveDto = new ApproveRequestDto { UserId = Guid.NewGuid() };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/approvals/{approvalRequest.Id}/approve", approveDto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Modify_PendingRequest_ReturnsOk()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var approvalRequest = new ApprovalRequest
        {
            WorkflowInstanceId = Guid.NewGuid(),
            AgentId = "architect",
            ProposedResponse = "Original solution",
            ConfidenceScore = 0.65,
            Status = "Pending"
        };
        
        context.ApprovalRequests.Add(approvalRequest);
        await context.SaveChangesAsync();
        
        var userId = Guid.NewGuid();
        var modifyDto = new ModifyRequestDto 
        { 
            UserId = userId,
            ModifiedResponse = "Modified solution with improvements"
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/approvals/{approvalRequest.Id}/modify", modifyDto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        // Create a new scope to get a fresh context
        using var verifyScope = _factory.Services.CreateScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var updated = await verifyContext.ApprovalRequests.FirstOrDefaultAsync(ar => ar.Id == approvalRequest.Id);
        Assert.NotNull(updated);
        Assert.Equal("Modified", updated.Status);
        Assert.Equal(userId, updated.ApprovedByUserId);
        Assert.Equal("Modified solution with improvements", updated.FinalResponse);
        Assert.Equal("Original solution", updated.ProposedResponse);
    }

    [Fact]
    public async Task Reject_PendingRequest_ReturnsOk()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var approvalRequest = new ApprovalRequest
        {
            WorkflowInstanceId = Guid.NewGuid(),
            AgentId = "architect",
            ProposedResponse = "Proposed solution",
            ConfidenceScore = 0.65,
            Status = "Pending"
        };
        
        context.ApprovalRequests.Add(approvalRequest);
        await context.SaveChangesAsync();
        
        var userId = Guid.NewGuid();
        var rejectDto = new RejectRequestDto 
        { 
            UserId = userId,
            Reason = "Needs more technical detail"
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/approvals/{approvalRequest.Id}/reject", rejectDto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        // Create a new scope to get a fresh context
        using var verifyScope = _factory.Services.CreateScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var updated = await verifyContext.ApprovalRequests.FirstOrDefaultAsync(ar => ar.Id == approvalRequest.Id);
        Assert.NotNull(updated);
        Assert.Equal("Rejected", updated.Status);
        Assert.Equal(userId, updated.ApprovedByUserId);
        Assert.Equal("Needs more technical detail", updated.RejectionReason);
    }

    [Fact]
    public async Task GetPendingReminders_ReturnsRequestsNeedingReminders()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var oldRequest = new ApprovalRequest
        {
            WorkflowInstanceId = Guid.NewGuid(),
            AgentId = "architect",
            ProposedResponse = "Old request",
            ConfidenceScore = 0.65,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow.AddHours(-25)
        };
        
        context.ApprovalRequests.Add(oldRequest);
        await context.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/api/approvals/reminders?reminderThresholdHours=24");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dtos = await response.Content.ReadFromJsonAsync<List<ApprovalRequestDto>>();
        Assert.NotNull(dtos);
        Assert.Single(dtos);
        Assert.Equal(oldRequest.Id, dtos[0].Id);
    }

    [Fact]
    public async Task GetTimedOutRequests_ReturnsTimedOutRequests()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var timedOutRequest = new ApprovalRequest
        {
            WorkflowInstanceId = Guid.NewGuid(),
            AgentId = "architect",
            ProposedResponse = "Timed out request",
            ConfidenceScore = 0.65,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow.AddHours(-73)
        };
        
        context.ApprovalRequests.Add(timedOutRequest);
        await context.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/api/approvals/timeouts?timeoutThresholdHours=72");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dtos = await response.Content.ReadFromJsonAsync<List<ApprovalRequestDto>>();
        Assert.NotNull(dtos);
        Assert.Single(dtos);
        Assert.Equal(timedOutRequest.Id, dtos[0].Id);
    }

    [Fact]
    public async Task MarkReminderSent_UpdatesTimestamp()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var approvalRequest = new ApprovalRequest
        {
            WorkflowInstanceId = Guid.NewGuid(),
            AgentId = "architect",
            ProposedResponse = "Proposed solution",
            ConfidenceScore = 0.65,
            Status = "Pending"
        };
        
        context.ApprovalRequests.Add(approvalRequest);
        await context.SaveChangesAsync();

        // Act
        var response = await _client.PostAsync($"/api/approvals/{approvalRequest.Id}/mark-reminder-sent", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        // Create a new scope to get a fresh context
        using var verifyScope = _factory.Services.CreateScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var updated = await verifyContext.ApprovalRequests.FirstOrDefaultAsync(ar => ar.Id == approvalRequest.Id);
        Assert.NotNull(updated);
        Assert.NotNull(updated.LastReminderSentAt);
    }

    [Fact]
    public async Task TimeoutRequest_UpdatesStatusToTimedOut()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var approvalRequest = new ApprovalRequest
        {
            WorkflowInstanceId = Guid.NewGuid(),
            AgentId = "architect",
            ProposedResponse = "Proposed solution",
            ConfidenceScore = 0.65,
            Status = "Pending"
        };
        
        context.ApprovalRequests.Add(approvalRequest);
        await context.SaveChangesAsync();

        // Act
        var response = await _client.PostAsync($"/api/approvals/{approvalRequest.Id}/timeout", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        // Create a new scope to get a fresh context
        using var verifyScope = _factory.Services.CreateScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var updated = await verifyContext.ApprovalRequests.FirstOrDefaultAsync(ar => ar.Id == approvalRequest.Id);
        Assert.NotNull(updated);
        Assert.Equal("TimedOut", updated.Status);
        Assert.NotNull(updated.RespondedAt);
    }
}
