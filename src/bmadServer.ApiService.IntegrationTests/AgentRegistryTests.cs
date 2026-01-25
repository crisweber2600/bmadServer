using System.Net;
using System.Net.Http.Json;
using bmadServer.ApiService.Agents;
using Microsoft.AspNetCore.Mvc.Testing;

namespace bmadServer.ApiService.IntegrationTests;

/// <summary>
/// Integration tests for agent registry API endpoints.
/// Verifies that the AgentsController correctly exposes agent registry functionality
/// as required by Story 5-1.
/// </summary>
public class AgentRegistryTests : IAsyncLifetime
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Test");
        });
        
        _client = _factory.CreateClient();
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        _factory?.Dispose();
        await Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that GET /api/v1/agents returns all BMAD agents.
    /// Story 5-1 AC: "I receive BMAD agents: ProductManager, Architect, Designer, Developer, Analyst, Orchestrator"
    /// </summary>
    [Fact]
    public async Task GetAllAgents_ReturnsAllBMADAgents()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/agents");
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var agents = await response.Content.ReadFromJsonAsync<List<AgentDefinition>>();
        Assert.NotNull(agents);
        Assert.Equal(6, agents.Count);
        
        // Verify all BMAD agents are present
        var expectedAgentIds = new[] { "product-manager", "architect", "designer", "developer", "analyst", "orchestrator" };
        foreach (var expectedId in expectedAgentIds)
        {
            Assert.Contains(agents, a => a.AgentId == expectedId);
        }
    }

    /// <summary>
    /// Verifies that GET /api/v1/agents/{id} returns the correct agent.
    /// Story 5-1 AC: "GetAgent(id)"
    /// </summary>
    [Fact]
    public async Task GetAgent_ReturnsCorrectAgent_WhenIdExists()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/agents/architect");
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var agent = await response.Content.ReadFromJsonAsync<AgentDefinition>();
        Assert.NotNull(agent);
        Assert.Equal("architect", agent.AgentId);
        Assert.Equal("Architect", agent.Name);
        Assert.NotEmpty(agent.Description);
        Assert.NotEmpty(agent.Capabilities);
        Assert.NotEmpty(agent.SystemPrompt);
        Assert.NotEmpty(agent.ModelPreference);
    }

    /// <summary>
    /// Verifies that GET /api/v1/agents/{id} returns 404 when agent doesn't exist.
    /// </summary>
    [Fact]
    public async Task GetAgent_Returns404_WhenAgentNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/agents/non-existent-agent");
        
        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// Verifies that GET /api/v1/agents/by-capability/{capability} returns matching agents.
    /// Story 5-1 AC: "GetAgentsByCapability(capability)"
    /// </summary>
    [Fact]
    public async Task GetAgentsByCapability_ReturnsMatchingAgents()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/agents/by-capability/create-architecture");
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var agents = await response.Content.ReadFromJsonAsync<List<AgentDefinition>>();
        Assert.NotNull(agents);
        Assert.NotEmpty(agents);
        
        // Architect should have create-architecture capability
        Assert.Contains(agents, a => a.AgentId == "architect");
        
        // All returned agents should have the capability
        Assert.All(agents, agent => 
            Assert.Contains("create-architecture", agent.Capabilities, StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Verifies that each agent has valid capabilities mapped to workflow steps.
    /// Story 5-1 AC: "capabilities map to workflow steps they can handle"
    /// </summary>
    [Fact]
    public async Task AllAgents_HaveValidCapabilities()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/agents");
        var agents = await response.Content.ReadFromJsonAsync<List<AgentDefinition>>();
        
        // Assert
        Assert.NotNull(agents);
        Assert.All(agents, agent =>
        {
            Assert.NotEmpty(agent.Capabilities);
            // Capabilities should be in kebab-case format (workflow step convention)
            Assert.All(agent.Capabilities, cap =>
            {
                Assert.False(string.IsNullOrWhiteSpace(cap));
                Assert.Matches(@"^[a-z]+(-[a-z]+)*$", cap);
            });
        });
    }

    /// <summary>
    /// Verifies that all agents have model preferences configured.
    /// Story 5-1 AC: "agents have model preferences"
    /// </summary>
    [Fact]
    public async Task AllAgents_HaveModelPreferences()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/agents");
        var agents = await response.Content.ReadFromJsonAsync<List<AgentDefinition>>();
        
        // Assert
        Assert.NotNull(agents);
        Assert.All(agents, agent =>
        {
            Assert.NotNull(agent.ModelPreference);
            Assert.NotEmpty(agent.ModelPreference);
            
            // Verify model preference is a valid model identifier
            var validPrefixes = new[] { "gpt", "claude", "o1", "gemini", "llama" };
            Assert.True(
                validPrefixes.Any(prefix => agent.ModelPreference.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)),
                $"Agent {agent.AgentId} has invalid model preference: {agent.ModelPreference}"
            );
        });
    }

    /// <summary>
    /// Verifies that Architect agent has "create-architecture" capability.
    /// Story 5-1 AC: "Architect handles 'create-architecture'"
    /// </summary>
    [Fact]
    public async Task ArchitectAgent_HasCreateArchitectureCapability()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/agents/architect");
        var agent = await response.Content.ReadFromJsonAsync<AgentDefinition>();
        
        // Assert
        Assert.NotNull(agent);
        Assert.Contains("create-architecture", agent.Capabilities, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifies that GetAgentsByCapability returns empty list for non-existent capability.
    /// </summary>
    [Fact]
    public async Task GetAgentsByCapability_ReturnsEmptyList_WhenNoMatch()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/agents/by-capability/non-existent-capability");
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var agents = await response.Content.ReadFromJsonAsync<List<AgentDefinition>>();
        Assert.NotNull(agents);
        Assert.Empty(agents);
    }
}
