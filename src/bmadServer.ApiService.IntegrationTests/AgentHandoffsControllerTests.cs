using System.Net;
using System.Net.Http.Json;
using bmadServer.ApiService.Agents;
using bmadServer.ApiService.Controllers;
using bmadServer.ApiService.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace bmadServer.ApiService.IntegrationTests;

public class AgentHandoffsControllerTests : IAsyncLifetime
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
                // Remove any existing DbContext registration
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Add in-memory database for testing with a persistent name
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
    public async Task RecordHandoff_WithValidRequest_ReturnsCreated()
    {
        // Arrange
        var workflowInstanceId = Guid.NewGuid();
        var request = new RecordHandoffRequest
        {
            WorkflowInstanceId = workflowInstanceId,
            FromAgent = null,
            ToAgent = "product-manager",
            WorkflowStep = "gather-requirements",
            Reason = "Initial agent"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/agenthandoffs", request);

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var handoff = await response.Content.ReadFromJsonAsync<AgentHandoffRecord>();
        Assert.NotNull(handoff);
        Assert.Equal("product-manager", handoff!.ToAgent);
        Assert.Equal("Product Manager", handoff.ToAgentName);
    }

    [Fact]
    public async Task RecordHandoff_WithInvalidAgent_ReturnsBadRequest()
    {
        // Arrange
        var workflowInstanceId = Guid.NewGuid();
        var request = new RecordHandoffRequest
        {
            WorkflowInstanceId = workflowInstanceId,
            FromAgent = null,
            ToAgent = "invalid-agent",
            WorkflowStep = "step",
            Reason = "Test"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/agenthandoffs", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetHandoffs_ReturnsAllHandoffsForWorkflow()
    {
        // Arrange
        var workflowInstanceId = Guid.NewGuid();

        // Record multiple handoffs
        await _client.PostAsJsonAsync("/api/agenthandoffs", new RecordHandoffRequest
        {
            WorkflowInstanceId = workflowInstanceId,
            FromAgent = null,
            ToAgent = "product-manager",
            WorkflowStep = "requirements",
            Reason = "First"
        });

        await _client.PostAsJsonAsync("/api/agenthandoffs", new RecordHandoffRequest
        {
            WorkflowInstanceId = workflowInstanceId,
            FromAgent = "product-manager",
            ToAgent = "architect",
            WorkflowStep = "design",
            Reason = "Second"
        });

        // Act
        var response = await _client.GetAsync($"/api/agenthandoffs/workflow/{workflowInstanceId}");

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        var handoffs = await response.Content.ReadFromJsonAsync<List<AgentHandoffRecord>>();
        Assert.NotNull(handoffs);
        Assert.Equal(2, handoffs!.Count);
        Assert.Equal("product-manager", handoffs[0].ToAgent);
        Assert.Equal("architect", handoffs[1].ToAgent);
    }

    [Fact]
    public async Task GetCurrentAgent_ReturnsLatestAgent()
    {
        // Arrange
        var workflowInstanceId = Guid.NewGuid();

        await _client.PostAsJsonAsync("/api/agenthandoffs", new RecordHandoffRequest
        {
            WorkflowInstanceId = workflowInstanceId,
            FromAgent = null,
            ToAgent = "product-manager",
            WorkflowStep = "requirements",
            Reason = "First"
        });

        await _client.PostAsJsonAsync("/api/agenthandoffs", new RecordHandoffRequest
        {
            WorkflowInstanceId = workflowInstanceId,
            FromAgent = "product-manager",
            ToAgent = "architect",
            WorkflowStep = "design",
            Reason = "Second"
        });

        // Act
        var response = await _client.GetAsync($"/api/agenthandoffs/workflow/{workflowInstanceId}/current");

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        var current = await response.Content.ReadFromJsonAsync<CurrentAgentResponse>();
        Assert.NotNull(current);
        Assert.Equal("architect", current!.AgentId);
    }

    [Fact]
    public async Task GetCurrentAgent_WithNoHandoffs_ReturnsNotFound()
    {
        // Arrange
        var workflowInstanceId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/agenthandoffs/workflow/{workflowInstanceId}/current");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAgentDetails_ReturnsAgentInformation()
    {
        // Act
        var response = await _client.GetAsync("/api/agenthandoffs/agent/architect/details");

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        var details = await response.Content.ReadFromJsonAsync<AgentDetails>();
        Assert.NotNull(details);
        Assert.Equal("architect", details!.AgentId);
        Assert.Equal("Architect", details.Name);
        Assert.NotEmpty(details.Description);
        Assert.NotEmpty(details.Capabilities);
        Assert.NotEmpty(details.Avatar!);
    }

    [Fact]
    public async Task GetAgentDetails_WithWorkflowStep_IncludesResponsibility()
    {
        // Act
        var response = await _client.GetAsync("/api/agenthandoffs/agent/architect/details?workflowStep=create-architecture");

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        var details = await response.Content.ReadFromJsonAsync<AgentDetails>();
        Assert.NotNull(details);
        Assert.NotNull(details!.CurrentStepResponsibility);
    }

    [Fact]
    public async Task GetAgentDetails_WithInvalidAgent_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/agenthandoffs/agent/invalid-agent/details");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task HandoffSequence_MaintainsCorrectOrder()
    {
        // Arrange
        var workflowInstanceId = Guid.NewGuid();
        var agents = new[] { "product-manager", "architect", "developer", "analyst" };

        // Act - Record handoffs in sequence
        for (int i = 0; i < agents.Length; i++)
        {
            await _client.PostAsJsonAsync("/api/agenthandoffs", new RecordHandoffRequest
            {
                WorkflowInstanceId = workflowInstanceId,
                FromAgent = i > 0 ? agents[i - 1] : null,
                ToAgent = agents[i],
                WorkflowStep = $"step-{i}",
                Reason = $"Handoff {i}"
            });
        }

        // Assert
        var response = await _client.GetAsync($"/api/agenthandoffs/workflow/{workflowInstanceId}");
        Assert.True(response.IsSuccessStatusCode);

        var handoffs = await response.Content.ReadFromJsonAsync<List<AgentHandoffRecord>>();
        Assert.NotNull(handoffs);
        Assert.Equal(4, handoffs!.Count);

        for (int i = 0; i < agents.Length; i++)
        {
            Assert.Equal(agents[i], handoffs[i].ToAgent);
        }
    }
}
