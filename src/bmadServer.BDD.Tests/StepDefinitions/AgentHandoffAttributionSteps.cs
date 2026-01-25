using Reqnroll;
using Xunit;
using bmadServer.ApiService.Agents;
using bmadServer.ApiService.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace bmadServer.BDD.Tests.StepDefinitions;

[Binding]
public class AgentHandoffAttributionSteps
{
    private ApplicationDbContext _dbContext = null!;
    private IAgentHandoffService _handoffService = null!;
    private IAgentRegistry _agentRegistry = null!;
    private Guid _workflowInstanceId;
    private AgentHandoffRecord? _lastHandoff;
    private List<AgentHandoffRecord>? _handoffs;
    private string? _currentAgent;
    private AgentDetails? _agentDetails;
    private Exception? _lastException;

    [BeforeScenario]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _agentRegistry = new AgentRegistry();

        var logger = NullLogger<AgentHandoffService>.Instance;
        _handoffService = new AgentHandoffService(_dbContext, _agentRegistry, logger);

        _workflowInstanceId = Guid.NewGuid();
    }

    [AfterScenario]
    public void Cleanup()
    {
        _dbContext?.Dispose();
    }

    [Given(@"a workflow instance exists")]
    public void GivenAWorkflowInstanceExists()
    {
        _workflowInstanceId = Guid.NewGuid();
    }

    [Given(@"a workflow instance with multiple handoffs")]
    public async Task GivenAWorkflowInstanceWithMultipleHandoffs()
    {
        _workflowInstanceId = Guid.NewGuid();
        
        await _handoffService.RecordHandoffAsync(
            _workflowInstanceId, null, "product-manager", "requirements", "Initial");
        await Task.Delay(10);
        
        await _handoffService.RecordHandoffAsync(
            _workflowInstanceId, "product-manager", "architect", "design", "Design phase");
        await Task.Delay(10);
        
        await _handoffService.RecordHandoffAsync(
            _workflowInstanceId, "architect", "developer", "implement", "Implementation");
    }

    [Given(@"an agent ""(.*)"" exists in the registry")]
    public void GivenAnAgentExistsInTheRegistry(string agentId)
    {
        var agent = _agentRegistry.GetAgent(agentId);
        Assert.NotNull(agent);
    }

    [When(@"I record a handoff from ""(.*)"" to ""(.*)"" at step ""(.*)""")]
    public async Task WhenIRecordAHandoffFromToAtStep(string fromAgent, string toAgent, string step)
    {
        _lastHandoff = await _handoffService.RecordHandoffAsync(
            _workflowInstanceId, fromAgent, toAgent, step, $"Handoff to {toAgent}");
    }

    [When(@"I record a handoff from initial to ""(.*)"" at step ""(.*)""")]
    public async Task WhenIRecordAHandoffFromInitialToAtStep(string toAgent, string step)
    {
        _lastHandoff = await _handoffService.RecordHandoffAsync(
            _workflowInstanceId, null, toAgent, step, "Initial assignment");
    }

    [When(@"I query the handoffs for the workflow")]
    public async Task WhenIQueryTheHandoffsForTheWorkflow()
    {
        _handoffs = await _handoffService.GetHandoffsAsync(_workflowInstanceId);
    }

    [When(@"I query the current agent")]
    public async Task WhenIQueryTheCurrentAgent()
    {
        _currentAgent = await _handoffService.GetCurrentAgentAsync(_workflowInstanceId);
    }

    [When(@"I request agent details for ""([^""]+)""$")]
    public async Task WhenIRequestAgentDetailsFor(string agentId)
    {
        _agentDetails = await _handoffService.GetAgentDetailsAsync(agentId);
    }

    [When(@"I request agent details for ""(.*)"" with step ""(.*)""")]
    public async Task WhenIRequestAgentDetailsForWithStep(string agentId, string step)
    {
        _agentDetails = await _handoffService.GetAgentDetailsAsync(agentId, step);
    }

    [When(@"I attempt to record a handoff to invalid agent ""(.*)""")]
    public async Task WhenIAttemptToRecordAHandoffToInvalidAgent(string agentId)
    {
        try
        {
            await _handoffService.RecordHandoffAsync(
                _workflowInstanceId, "product-manager", agentId, "step", "Invalid");
        }
        catch (Exception ex)
        {
            _lastException = ex;
        }
    }

    [Then(@"the handoff should include fromAgent ""(.*)""")]
    public void ThenTheHandoffShouldIncludeFromAgent(string expectedFromAgent)
    {
        Assert.NotNull(_lastHandoff);
        Assert.Equal(expectedFromAgent, _lastHandoff!.FromAgent);
    }

    [Then(@"the handoff should include toAgent ""(.*)""")]
    public void ThenTheHandoffShouldIncludeToAgent(string expectedToAgent)
    {
        Assert.NotNull(_lastHandoff);
        Assert.Equal(expectedToAgent, _lastHandoff!.ToAgent);
    }

    [Then(@"the handoff should include workflowStep ""(.*)""")]
    public void ThenTheHandoffShouldIncludeWorkflowStep(string expectedStep)
    {
        Assert.NotNull(_lastHandoff);
        Assert.Equal(expectedStep, _lastHandoff!.WorkflowStep);
    }

    [Then(@"the handoff should include agent names")]
    public void ThenTheHandoffShouldIncludeAgentNames()
    {
        Assert.NotNull(_lastHandoff);
        Assert.NotEmpty(_lastHandoff!.ToAgentName);
        Assert.NotEmpty(_lastHandoff!.FromAgentName!);
    }

    [Then(@"the handoff should have null fromAgent")]
    public void ThenTheHandoffShouldHaveNullFromAgent()
    {
        Assert.NotNull(_lastHandoff);
        Assert.Null(_lastHandoff!.FromAgent);
    }

    [Then(@"I should receive all handoffs in chronological order")]
    public void ThenIShouldReceiveAllHandoffsInChronologicalOrder()
    {
        Assert.NotNull(_handoffs);
        Assert.Equal(3, _handoffs!.Count);
        
        for (int i = 0; i < _handoffs.Count - 1; i++)
        {
            Assert.True(_handoffs[i].Timestamp <= _handoffs[i + 1].Timestamp);
        }
    }

    [Then(@"I should receive the most recent agent")]
    public void ThenIShouldReceiveTheMostRecentAgent()
    {
        Assert.NotNull(_currentAgent);
        Assert.Equal("developer", _currentAgent);
    }

    [Then(@"I should receive the agent name")]
    public void ThenIShouldReceiveTheAgentName()
    {
        Assert.NotNull(_agentDetails);
        Assert.NotEmpty(_agentDetails!.Name);
    }

    [Then(@"I should receive the agent description")]
    public void ThenIShouldReceiveTheAgentDescription()
    {
        Assert.NotNull(_agentDetails);
        Assert.NotEmpty(_agentDetails!.Description);
    }

    [Then(@"I should receive the agent capabilities")]
    public void ThenIShouldReceiveTheAgentCapabilities()
    {
        Assert.NotNull(_agentDetails);
        Assert.NotEmpty(_agentDetails!.Capabilities);
    }

    [Then(@"I should receive an avatar identifier")]
    public void ThenIShouldReceiveAnAvatarIdentifier()
    {
        Assert.NotNull(_agentDetails);
        Assert.NotEmpty(_agentDetails!.Avatar!);
    }

    [Then(@"the details should include current step responsibility")]
    public void ThenTheDetailsShouldIncludeCurrentStepResponsibility()
    {
        Assert.NotNull(_agentDetails);
        Assert.NotNull(_agentDetails!.CurrentStepResponsibility);
    }

    [Then(@"the operation should throw InvalidOperationException")]
    public void ThenTheOperationShouldThrowInvalidOperationException()
    {
        Assert.NotNull(_lastException);
        Assert.IsType<InvalidOperationException>(_lastException);
    }

    [Then(@"the error should mention agent not found")]
    public void ThenTheErrorShouldMentionAgentNotFound()
    {
        Assert.NotNull(_lastException);
        Assert.Contains("not found", _lastException!.Message);
    }
}
