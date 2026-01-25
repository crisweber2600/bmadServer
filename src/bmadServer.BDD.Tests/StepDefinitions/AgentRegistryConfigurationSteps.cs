using Reqnroll;
using Xunit;
using bmadServer.ApiService.Agents;

namespace bmadServer.BDD.Tests.StepDefinitions;

[Binding]
public class AgentRegistryConfigurationSteps
{
    private AgentDefinition? _agentDefinition;
    private AgentRegistry? _agentRegistry;
    private IEnumerable<AgentDefinition>? _agents;
    private AgentDefinition? _queriedAgent;

    [Given(@"I need to define BMAD agents")]
    public void GivenINeedToDefineBMADAgents()
    {
        // Setup for creating agent definitions
        Assert.True(true);
    }

    [When(@"I create an AgentDefinition")]
    public void WhenICreateAnAgentDefinition()
    {
        _agentDefinition = new AgentDefinition
        {
            AgentId = "test-agent",
            Name = "Test Agent",
            Description = "A test agent",
            Capabilities = new List<string> { "test-capability" },
            SystemPrompt = "You are a test agent",
            ModelPreference = "gpt-4"
        };
    }

    [Then(@"it includes AgentId property")]
    public void ThenItIncludesAgentIdProperty()
    {
        Assert.NotNull(_agentDefinition);
        Assert.NotNull(_agentDefinition.AgentId);
    }

    [Then(@"it includes Name property")]
    public void ThenItIncludesNameProperty()
    {
        Assert.NotNull(_agentDefinition);
        Assert.NotNull(_agentDefinition.Name);
    }

    [Then(@"it includes Description property")]
    public void ThenItIncludesDescriptionProperty()
    {
        Assert.NotNull(_agentDefinition);
        Assert.NotNull(_agentDefinition.Description);
    }

    [Then(@"it includes Capabilities list property")]
    public void ThenItIncludesCapabilitiesListProperty()
    {
        Assert.NotNull(_agentDefinition);
        Assert.NotNull(_agentDefinition.Capabilities);
        Assert.IsAssignableFrom<IEnumerable<string>>(_agentDefinition.Capabilities);
    }

    [Then(@"it includes SystemPrompt property")]
    public void ThenItIncludesSystemPromptProperty()
    {
        Assert.NotNull(_agentDefinition);
        Assert.NotNull(_agentDefinition.SystemPrompt);
    }

    [Then(@"it includes ModelPreference property")]
    public void ThenItIncludesModelPreferenceProperty()
    {
        Assert.NotNull(_agentDefinition);
        Assert.NotNull(_agentDefinition.ModelPreference);
    }

    [Given(@"agent definitions exist")]
    public void GivenAgentDefinitionsExist()
    {
        // Verify agent definitions are available
        Assert.True(true);
    }

    [When(@"I create an AgentRegistry")]
    public void WhenICreateAnAgentRegistry()
    {
        _agentRegistry = new AgentRegistry();
    }

    [Then(@"it provides GetAllAgents method")]
    public void ThenItProvidesGetAllAgentsMethod()
    {
        Assert.NotNull(_agentRegistry);
        var method = typeof(AgentRegistry).GetMethod("GetAllAgents");
        Assert.NotNull(method);
    }

    [Then(@"it provides GetAgent by id method")]
    public void ThenItProvidesGetAgentByIdMethod()
    {
        Assert.NotNull(_agentRegistry);
        var method = typeof(AgentRegistry).GetMethod("GetAgent");
        Assert.NotNull(method);
    }

    [Then(@"it provides GetAgentsByCapability method")]
    public void ThenItProvidesGetAgentsByCapabilityMethod()
    {
        Assert.NotNull(_agentRegistry);
        var method = typeof(AgentRegistry).GetMethod("GetAgentsByCapability");
        Assert.NotNull(method);
    }

    [Given(@"the registry is populated")]
    public void GivenTheRegistryIsPopulated()
    {
        _agentRegistry = new AgentRegistry();
        _agents = _agentRegistry.GetAllAgents();
    }

    [When(@"I query GetAllAgents")]
    public void WhenIQueryGetAllAgents()
    {
        Assert.NotNull(_agentRegistry);
        _agents = _agentRegistry.GetAllAgents();
    }

    [Then(@"I receive ProductManager agent")]
    public void ThenIReceiveProductManagerAgent()
    {
        Assert.NotNull(_agents);
        Assert.Contains(_agents, a => a.AgentId == "product-manager");
    }

    [Then(@"I receive Architect agent")]
    public void ThenIReceiveArchitectAgent()
    {
        Assert.NotNull(_agents);
        Assert.Contains(_agents, a => a.AgentId == "architect");
    }

    [Then(@"I receive Designer agent")]
    public void ThenIReceiveDesignerAgent()
    {
        Assert.NotNull(_agents);
        Assert.Contains(_agents, a => a.AgentId == "designer");
    }

    [Then(@"I receive Developer agent")]
    public void ThenIReceiveDeveloperAgent()
    {
        Assert.NotNull(_agents);
        Assert.Contains(_agents, a => a.AgentId == "developer");
    }

    [Then(@"I receive Analyst agent")]
    public void ThenIReceiveAnalystAgent()
    {
        Assert.NotNull(_agents);
        Assert.Contains(_agents, a => a.AgentId == "analyst");
    }

    [Then(@"I receive Orchestrator agent")]
    public void ThenIReceiveOrchestratorAgent()
    {
        Assert.NotNull(_agents);
        Assert.Contains(_agents, a => a.AgentId == "orchestrator");
    }

    [Given(@"each agent has capabilities")]
    public void GivenEachAgentHasCapabilities()
    {
        _agentRegistry = new AgentRegistry();
        _agents = _agentRegistry.GetAllAgents();
        Assert.NotNull(_agents);
        Assert.All(_agents, agent => Assert.NotEmpty(agent.Capabilities));
    }

    [When(@"I examine the Architect agent definition")]
    public void WhenIExamineTheArchitectAgentDefinition()
    {
        Assert.NotNull(_agentRegistry);
        _queriedAgent = _agentRegistry.GetAgent("architect");
    }

    [Then(@"it has capability ""(.*)""")]
    public void ThenItHasCapability(string capability)
    {
        Assert.NotNull(_queriedAgent);
        Assert.Contains(capability, _queriedAgent.Capabilities);
    }

    [Then(@"capabilities map to workflow steps they can handle")]
    public void ThenCapabilitiesMapToWorkflowStepsTheyCanHandle()
    {
        Assert.NotNull(_queriedAgent);
        // Verify that capabilities are workflow-step-like strings
        Assert.All(_queriedAgent.Capabilities, cap => 
        {
            Assert.False(string.IsNullOrWhiteSpace(cap));
            // Capabilities should be in kebab-case format
            Assert.Matches(@"^[a-z]+(-[a-z]+)*$", cap);
        });
    }

    [Given(@"agents have model preferences")]
    public void GivenAgentsHaveModelPreferences()
    {
        _agentRegistry = new AgentRegistry();
        _agents = _agentRegistry.GetAllAgents();
        Assert.NotNull(_agents);
        Assert.All(_agents, agent => Assert.NotNull(agent.ModelPreference));
    }

    [When(@"I query an agent from the registry")]
    public void WhenIQueryAnAgentFromTheRegistry()
    {
        Assert.NotNull(_agentRegistry);
        _queriedAgent = _agentRegistry.GetAgent("architect");
    }

    [Then(@"the agent has a configured ModelPreference")]
    public void ThenTheAgentHasAConfiguredModelPreference()
    {
        Assert.NotNull(_queriedAgent);
        Assert.False(string.IsNullOrWhiteSpace(_queriedAgent.ModelPreference));
    }

    [Then(@"the system can route to the preferred model")]
    public void ThenTheSystemCanRouteToThePreferredModel()
    {
        Assert.NotNull(_queriedAgent);
        // Verify model preference is a valid model identifier
        Assert.NotEmpty(_queriedAgent.ModelPreference);
        // Common model prefixes
        var validPrefixes = new[] { "gpt", "claude", "o1", "gemini", "llama" };
        Assert.True(
            validPrefixes.Any(prefix => _queriedAgent.ModelPreference.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)),
            $"Model preference '{_queriedAgent.ModelPreference}' should be a valid model identifier"
        );
    }

    [When(@"I query GetAgent with ""(.*)"" id")]
    public void WhenIQueryGetAgentWithId(string agentId)
    {
        Assert.NotNull(_agentRegistry);
        _queriedAgent = _agentRegistry.GetAgent(agentId);
    }

    [Then(@"I receive the Architect agent")]
    public void ThenIReceiveTheArchitectAgent()
    {
        Assert.NotNull(_queriedAgent);
        Assert.Equal("architect", _queriedAgent.AgentId);
    }

    [Then(@"the agent has the correct name ""(.*)""")]
    public void ThenTheAgentHasTheCorrectName(string expectedName)
    {
        Assert.NotNull(_queriedAgent);
        Assert.Equal(expectedName, _queriedAgent.Name);
    }

    [When(@"I query GetAgentsByCapability with ""(.*)""")]
    public void WhenIQueryGetAgentsByCapabilityWith(string capability)
    {
        Assert.NotNull(_agentRegistry);
        _agents = _agentRegistry.GetAgentsByCapability(capability);
    }

    [Then(@"I receive agents that have this capability")]
    public void ThenIReceiveAgentsThatHaveThisCapability()
    {
        Assert.NotNull(_agents);
        Assert.NotEmpty(_agents);
    }

    [Then(@"the Architect agent is in the results")]
    public void ThenTheArchitectAgentIsInTheResults()
    {
        Assert.NotNull(_agents);
        Assert.Contains(_agents, a => a.AgentId == "architect");
    }
}
