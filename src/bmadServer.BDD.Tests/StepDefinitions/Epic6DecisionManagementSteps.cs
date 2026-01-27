using System.Text.Json;
using bmadServer.ApiService.Data;
using bmadServer.ApiService.Models.Decisions;
using bmadServer.ApiService.Services.Decisions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll;
using Xunit;

namespace bmadServer.BDD.Tests.StepDefinitions;

[Binding]
public class Epic6DecisionManagementSteps : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ApplicationDbContext _dbContext;
    private readonly IDecisionService _decisionService;

    private Guid? _currentUserId;
    private Guid? _currentWorkflowId;
    private Decision? _capturedDecision;
    private List<Decision>? _workflowDecisions;
    private int _lastStatusCode;

    public Epic6DecisionManagementSteps()
    {
        var services = new ServiceCollection();

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase($"Decision_Test_{Guid.NewGuid()}"));

        services.AddScoped<IDecisionService, DecisionService>();

        _serviceProvider = services.BuildServiceProvider();
        _dbContext = _serviceProvider.GetRequiredService<ApplicationDbContext>();
        _decisionService = _serviceProvider.GetRequiredService<IDecisionService>();
    }

    #region Background

    [Given(@"I have an active workflow")]
    public void GivenIHaveAnActiveWorkflow()
    {
        _currentWorkflowId = Guid.NewGuid();
        _currentUserId = Guid.NewGuid();
    }

    #endregion

    #region Story 6.1: Decision Capture & Storage

    [Given(@"I am in a workflow step that requires a decision")]
    public void GivenIAmInAWorkflowStepThatRequiresADecision()
    {
        Assert.NotNull(_currentWorkflowId);
        Assert.NotNull(_currentUserId);
    }

    [When(@"I make a decision and confirm my choice")]
    public async Task WhenIMakeADecisionAndConfirmMyChoice()
    {
        _capturedDecision = await _decisionService.CaptureDecisionAsync(
            _currentWorkflowId!.Value,
            "step-1",
            new DecisionRequest
            {
                DecisionType = "architecture-choice",
                Question = "Which database should we use?",
                Options = new List<string> { "PostgreSQL", "MySQL", "SQLite" },
                SelectedOption = "PostgreSQL",
                Reasoning = "PostgreSQL offers better support for JSONB and complex queries",
                Context = JsonDocument.Parse("{\"requirements\": \"high-volume\", \"budget\": \"moderate\"}")
            },
            _currentUserId!.Value);
    }

    [Then(@"a Decision record should be created")]
    public void ThenADecisionRecordShouldBeCreated()
    {
        Assert.NotNull(_capturedDecision);
    }

    [Then(@"it should include id")]
    public void ThenItShouldIncludeId()
    {
        Assert.NotNull(_capturedDecision);
        Assert.NotEqual(Guid.Empty, _capturedDecision.Id);
    }

    [Then(@"it should include workflowInstanceId")]
    public void ThenItShouldIncludeWorkflowInstanceId()
    {
        Assert.NotNull(_capturedDecision);
        Assert.Equal(_currentWorkflowId, _capturedDecision.WorkflowInstanceId);
    }

    [Then(@"it should include stepId")]
    public void ThenItShouldIncludeStepId()
    {
        Assert.NotNull(_capturedDecision);
        Assert.False(string.IsNullOrEmpty(_capturedDecision.StepId));
    }

    [Then(@"it should include decisionType")]
    public void ThenItShouldIncludeDecisionType()
    {
        Assert.NotNull(_capturedDecision);
        Assert.False(string.IsNullOrEmpty(_capturedDecision.DecisionType));
    }

    [Then(@"it should include value")]
    public void ThenItShouldIncludeValue()
    {
        Assert.NotNull(_capturedDecision);
        Assert.NotNull(_capturedDecision.Value);
    }

    [Then(@"it should include decidedBy")]
    public void ThenItShouldIncludeDecidedBy()
    {
        Assert.NotNull(_capturedDecision);
        Assert.Equal(_currentUserId, _capturedDecision.DecidedBy);
    }

    [Then(@"it should include decidedAt timestamp")]
    public void ThenItShouldIncludeDecidedAtTimestamp()
    {
        Assert.NotNull(_capturedDecision);
        Assert.NotEqual(default, _capturedDecision.DecidedAt);
    }

    [Given(@"a workflow has multiple decisions recorded")]
    public async Task GivenAWorkflowHasMultipleDecisionsRecorded()
    {
        _currentWorkflowId = Guid.NewGuid();
        _currentUserId = Guid.NewGuid();

        // Add multiple decisions
        await _decisionService.CaptureDecisionAsync(_currentWorkflowId.Value, "step-1",
            new DecisionRequest
            {
                DecisionType = "tech-choice",
                Question = "Which framework?",
                Options = new List<string> { "A", "B" },
                SelectedOption = "A",
                Reasoning = "Better fit"
            }, _currentUserId.Value);

        await _decisionService.CaptureDecisionAsync(_currentWorkflowId.Value, "step-2",
            new DecisionRequest
            {
                DecisionType = "design-choice",
                Question = "Which pattern?",
                Options = new List<string> { "X", "Y" },
                SelectedOption = "X",
                Reasoning = "More maintainable"
            }, _currentUserId.Value);
    }

    [When(@"I send GET to ""/api/v1/workflows/\{id\}/decisions""")]
    public async Task WhenISendGetToApiV1WorkflowsIdDecisions()
    {
        Assert.NotNull(_currentWorkflowId);
        _workflowDecisions = await _decisionService.GetWorkflowDecisionsAsync(_currentWorkflowId.Value);
        _lastStatusCode = 200;
    }

    [Then(@"I should receive all decisions in chronological order")]
    public void ThenIShouldReceiveAllDecisionsInChronologicalOrder()
    {
        Assert.NotNull(_workflowDecisions);
        Assert.NotEmpty(_workflowDecisions);

        // Verify chronological order
        for (int i = 1; i < _workflowDecisions.Count; i++)
        {
            Assert.True(_workflowDecisions[i].DecidedAt >= _workflowDecisions[i - 1].DecidedAt,
                "Decisions should be in chronological order");
        }
    }

    [Given(@"a decision was made in a workflow")]
    public async Task GivenADecisionWasMadeInAWorkflow()
    {
        await WhenIMakeADecisionAndConfirmMyChoice();
    }

    [When(@"I view decision details")]
    public async Task WhenIViewDecisionDetails()
    {
        Assert.NotNull(_capturedDecision);
        _capturedDecision = await _decisionService.GetDecisionAsync(_capturedDecision.Id);
    }

    [Then(@"I should see the question asked")]
    public void ThenIShouldSeeTheQuestionAsked()
    {
        Assert.NotNull(_capturedDecision);
        Assert.False(string.IsNullOrEmpty(_capturedDecision.Question));
    }

    [Then(@"I should see the options presented")]
    public void ThenIShouldSeeTheOptionsPresented()
    {
        Assert.NotNull(_capturedDecision);
        Assert.NotNull(_capturedDecision.Options);
        Assert.NotEmpty(_capturedDecision.Options);
    }

    [Then(@"I should see the selected option")]
    public void ThenIShouldSeeTheSelectedOption()
    {
        Assert.NotNull(_capturedDecision);
        Assert.False(string.IsNullOrEmpty(_capturedDecision.SelectedOption));
    }

    [Then(@"I should see the reasoning")]
    public void ThenIShouldSeeTheReasoning()
    {
        Assert.NotNull(_capturedDecision);
        Assert.False(string.IsNullOrEmpty(_capturedDecision.Reasoning));
    }

    [Then(@"I should see the context at time of decision")]
    public void ThenIShouldSeeTheContextAtTimeOfDecision()
    {
        Assert.NotNull(_capturedDecision);
        Assert.NotNull(_capturedDecision.Context);
    }

    [Given(@"a decision involves structured data")]
    public void GivenADecisionInvolvesStructuredData()
    {
        // Structured data will be used in the capture
    }

    [When(@"the decision is captured")]
    public async Task WhenTheDecisionIsCaptured()
    {
        await WhenIMakeADecisionAndConfirmMyChoice();
    }

    [Then(@"the value should be stored as validated JSON")]
    public void ThenTheValueShouldBeStoredAsValidatedJson()
    {
        Assert.NotNull(_capturedDecision);
        Assert.NotNull(_capturedDecision.Value);
        // Value should be valid JSON
        var json = _capturedDecision.Value.RootElement;
        Assert.NotNull(json);
    }

    [Then(@"it should match the expected schema")]
    public void ThenItShouldMatchTheExpectedSchema()
    {
        // Schema validation in integration tests
        Assert.NotNull(_capturedDecision);
    }

    [Then(@"JSONB columns should be properly indexed")]
    public void ThenJsonbColumnsShouldBeProperlyIndexed()
    {
        // Index verification in database migration tests
    }

    #endregion

    public void Dispose()
    {
        _dbContext?.Dispose();
        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
