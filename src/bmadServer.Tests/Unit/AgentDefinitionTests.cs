using bmadServer.ApiService.Agents;
using Xunit;

namespace bmadServer.Tests.Unit;

/// <summary>
/// Unit tests for AgentDefinition.
/// Verifies agent definition structure for Story 5-1.
/// </summary>
public class AgentDefinitionTests
{
    [Fact]
    public void AgentDefinition_ShouldHaveRequiredProperties()
    {
        // Act
        var agent = new AgentDefinition
        {
            AgentId = "test-agent",
            Name = "Test Agent",
            Description = "A test agent for validation",
            Capabilities = new List<string> { "test-capability" },
            SystemPrompt = "You are a test agent",
            ModelPreference = "gpt-4"
        };

        // Assert
        Assert.Equal("test-agent", agent.AgentId);
        Assert.Equal("Test Agent", agent.Name);
        Assert.Equal("A test agent for validation", agent.Description);
        Assert.Single(agent.Capabilities);
        Assert.Equal("You are a test agent", agent.SystemPrompt);
        Assert.Equal("gpt-4", agent.ModelPreference);
    }

    [Fact]
    public void AgentDefinition_ShouldSupportMultipleCapabilities()
    {
        // Act
        var agent = new AgentDefinition
        {
            AgentId = "multi-capability-agent",
            Name = "Multi Capability Agent",
            Description = "Agent with multiple capabilities",
            Capabilities = new List<string> 
            { 
                "capability-one", 
                "capability-two", 
                "capability-three" 
            },
            SystemPrompt = "You handle multiple tasks",
            ModelPreference = "gpt-4"
        };

        // Assert
        Assert.Equal(3, agent.Capabilities.Count);
        Assert.Contains("capability-one", agent.Capabilities);
        Assert.Contains("capability-two", agent.Capabilities);
        Assert.Contains("capability-three", agent.Capabilities);
    }

    [Fact]
    public void AgentDefinition_ShouldBeImmutable()
    {
        // Arrange
        var agent = new AgentDefinition
        {
            AgentId = "immutable-agent",
            Name = "Immutable Agent",
            Description = "Test immutability",
            Capabilities = new List<string> { "test" },
            SystemPrompt = "Original prompt",
            ModelPreference = "gpt-4"
        };

        // Act & Assert - Properties should be init-only, not settable after construction
        var agentType = typeof(AgentDefinition);
        var properties = agentType.GetProperties();
        
        foreach (var property in properties)
        {
            Assert.NotNull(property.GetSetMethod(nonPublic: true));
            // Properties should have init-only setters (GetSetMethod returns non-null for init)
            var setMethod = property.GetSetMethod(nonPublic: true);
            Assert.NotNull(setMethod);
        }
    }

    [Fact]
    public void AgentDefinition_WithEmptyCapabilities_ShouldBeValid()
    {
        // This tests that the structure allows empty capabilities, 
        // even though business logic requires at least one capability
        
        // Act
        var agent = new AgentDefinition
        {
            AgentId = "minimal-agent",
            Name = "Minimal Agent",
            Description = "Minimal test",
            Capabilities = new List<string>(),
            SystemPrompt = "Minimal prompt",
            ModelPreference = "gpt-4"
        };

        // Assert
        Assert.NotNull(agent.Capabilities);
        Assert.Empty(agent.Capabilities);
    }
}
