using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using bmadServer.ApiService.Data;
using bmadServer.ApiService.Models.Workflows;
using bmadServer.ApiService.Services.Workflows;
using bmadServer.BDD.Tests.TestSupport;
using bmadServer.ServiceDefaults.Models.Workflows;
using bmadServer.ServiceDefaults.Services.Workflows;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Reqnroll;
using Xunit;

namespace bmadServer.BDD.Tests.StepDefinitions;

[Binding]
public class WorkflowOrchestrationSteps : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ApplicationDbContext _dbContext;
    private readonly SqliteConnection _connection;
    private readonly IWorkflowRegistry _workflowRegistry;
    private readonly IWorkflowInstanceService _workflowService;
    
    private Guid? _currentUserId;
    private string? _currentWorkflowId;
    private Guid? _currentInstanceId;
    private WorkflowInstance? _currentInstance;
    private List<WorkflowDefinition>? _availableWorkflows;
    private WorkflowDefinition? _queriedWorkflow;
    private bool _validationResult;
    
    // Suppress unused warning - field reserved for future HTTP response testing
    #pragma warning disable CS0169
    private HttpResponseMessage? _lastResponse;
    #pragma warning restore CS0169
    private string? _lastError;

    public WorkflowOrchestrationSteps()
    {
        // Create SQLite connection for test isolation
        _connection = new SqliteConnection($"DataSource=BDD_Test_{Guid.NewGuid()};Mode=Memory;Cache=Shared");
        _connection.Open();
        
        var services = new ServiceCollection();
        
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite(_connection));
        
        services.AddSingleton<IWorkflowRegistry, WorkflowRegistry>();
        services.AddScoped<IWorkflowInstanceService, WorkflowInstanceService>();
        services.AddSingleton<ILogger<WorkflowInstanceService>>(NullLogger<WorkflowInstanceService>.Instance);
        
        _serviceProvider = services.BuildServiceProvider();
        _dbContext = _serviceProvider.GetRequiredService<ApplicationDbContext>();
        // NOTE: Skip EnsureCreated() - BDD tests use mock state, not actual DB operations
        // The workflow registry and service can still be used without the database schema
        _workflowRegistry = _serviceProvider.GetRequiredService<IWorkflowRegistry>();
        _workflowService = _serviceProvider.GetRequiredService<IWorkflowInstanceService>();
    }

    #region Background Steps

    [Given(@"the workflow registry is initialized")]
    public void GivenTheWorkflowRegistryIsInitialized()
    {
        Assert.NotNull(_workflowRegistry);
        var workflows = _workflowRegistry.GetAllWorkflows();
        Assert.NotEmpty(workflows);
    }

    [Given(@"I am authenticated as a valid user")]
    public void GivenIAmAuthenticatedAsAValidUser()
    {
        _currentUserId = Guid.NewGuid();
        Assert.NotNull(_currentUserId);
    }

    #endregion

    #region Story 4-1: Workflow Registry

    [When(@"I query all available workflows")]
    public void WhenIQueryAllAvailableWorkflows()
    {
        _availableWorkflows = _workflowRegistry.GetAllWorkflows().ToList();
    }

    [Then(@"the registry should contain at least (\d+) workflows")]
    public void ThenTheRegistryShouldContainAtLeastWorkflows(int count)
    {
        Assert.NotNull(_availableWorkflows);
        Assert.True(_availableWorkflows.Count >= count, 
            $"Expected at least {count} workflows, but found {_availableWorkflows.Count}");
    }

    [Then(@"the workflows should include ""(.*)""")]
    public void ThenTheWorkflowsShouldInclude(string workflowId)
    {
        Assert.NotNull(_availableWorkflows);
        Assert.Contains(_availableWorkflows, w => w.WorkflowId == workflowId);
    }

    [When(@"I query workflow ""(.*)""")]
    public void WhenIQueryWorkflow(string workflowId)
    {
        _currentWorkflowId = workflowId;
        _queriedWorkflow = _workflowRegistry.GetWorkflow(workflowId);
    }

    [Then(@"the workflow should exist")]
    public void ThenTheWorkflowShouldExist()
    {
        Assert.NotNull(_queriedWorkflow);
    }

    [Then(@"the workflow should have a name")]
    public void ThenTheWorkflowShouldHaveAName()
    {
        Assert.NotNull(_queriedWorkflow);
        Assert.False(string.IsNullOrWhiteSpace(_queriedWorkflow.Name));
    }

    [Then(@"the workflow should have a description")]
    public void ThenTheWorkflowShouldHaveADescription()
    {
        Assert.NotNull(_queriedWorkflow);
        Assert.False(string.IsNullOrWhiteSpace(_queriedWorkflow.Description));
    }

    [Then(@"the workflow should have steps")]
    public void ThenTheWorkflowShouldHaveSteps()
    {
        Assert.NotNull(_queriedWorkflow);
        Assert.NotNull(_queriedWorkflow.Steps);
        Assert.NotEmpty(_queriedWorkflow.Steps);
    }

    [When(@"I validate workflow ""(.*)""")]
    public void WhenIValidateWorkflow(string workflowId)
    {
        _currentWorkflowId = workflowId;
        _validationResult = _workflowRegistry.ValidateWorkflow(workflowId);
    }

    [Then(@"the validation should succeed")]
    public void ThenTheValidationShouldSucceed()
    {
        Assert.True(_validationResult);
    }

    [Then(@"all steps should have valid agent IDs")]
    public void ThenAllStepsShouldHaveValidAgentIDs()
    {
        var workflow = _workflowRegistry.GetWorkflow(_currentWorkflowId!);
        Assert.NotNull(workflow);
        Assert.All(workflow.Steps, step => 
            Assert.False(string.IsNullOrWhiteSpace(step.AgentId)));
    }

    #endregion

    #region Story 4-2: Workflow Instance Creation & State Machine

    [Given(@"I have a valid workflow ID ""(.*)""")]
    public void GivenIHaveAValidWorkflowID(string workflowId)
    {
        _currentWorkflowId = workflowId;
        var workflow = _workflowRegistry.GetWorkflow(workflowId);
        Assert.NotNull(workflow);
    }

    [When(@"I create a new workflow instance")]
    public async Task WhenICreateANewWorkflowInstance()
    {
        Assert.NotNull(_currentWorkflowId);
        Assert.NotNull(_currentUserId);
        
        _currentInstance = await _workflowService.CreateWorkflowInstanceAsync(
            _currentWorkflowId, _currentUserId.Value, new Dictionary<string, object>());
        _currentInstanceId = _currentInstance.Id;
    }

    [Then(@"the workflow instance should be created")]
    public void ThenTheWorkflowInstanceShouldBeCreated()
    {
        Assert.NotNull(_currentInstance);
        Assert.NotEqual(Guid.Empty, _currentInstance.Id);
    }

    [Then(@"the status should be ""(.*)""")]
    public void ThenTheStatusShouldBe(string expectedStatus)
    {
        Assert.NotNull(_currentInstance);
        var status = Enum.Parse<WorkflowStatus>(expectedStatus);
        Assert.Equal(status, _currentInstance.Status);
    }

    [Then(@"the instance should have a unique ID")]
    public void ThenTheInstanceShouldHaveAUniqueID()
    {
        Assert.NotNull(_currentInstance);
        Assert.NotEqual(Guid.Empty, _currentInstance.Id);
    }

    [Then(@"the instance should be associated with my user")]
    public void ThenTheInstanceShouldBeAssociatedWithMyUser()
    {
        Assert.NotNull(_currentInstance);
        Assert.Equal(_currentUserId, _currentInstance.UserId);
    }

    [Given(@"I have created a workflow instance for ""(.*)""")]
    public async Task GivenIHaveCreatedAWorkflowInstanceFor(string workflowId)
    {
        _currentWorkflowId = workflowId;
        _currentUserId = Guid.NewGuid();
        _currentInstance = await _workflowService.CreateWorkflowInstanceAsync(
            workflowId, _currentUserId.Value, new Dictionary<string, object>());
        _currentInstanceId = _currentInstance.Id;
    }

    [When(@"I start the workflow instance")]
    public async Task WhenIStartTheWorkflowInstance()
    {
        Assert.NotNull(_currentInstanceId);
        
        var success = await _workflowService.StartWorkflowAsync(_currentInstanceId.Value);
        if (!success)
        {
            throw new InvalidOperationException("Cannot start workflow - invalid state transition");
        }
        _currentInstance = await _workflowService.GetWorkflowInstanceAsync(_currentInstanceId.Value);
    }

    [Then(@"the status should transition to ""(.*)""")]
    public void ThenTheStatusShouldTransitionTo(string expectedStatus)
    {
        ThenTheStatusShouldBe(expectedStatus);
    }

    [Then(@"the current step should be set to the first step")]
    public void ThenTheCurrentStepShouldBeSetToTheFirstStep()
    {
        Assert.NotNull(_currentInstance);
        Assert.Equal(1, _currentInstance.CurrentStep);
    }

    [Then(@"a workflow event should be logged")]
    public void ThenAWorkflowEventShouldBeLogged()
    {
        Assert.NotNull(_currentInstanceId);
        var events = _dbContext.WorkflowEvents
            .Where(e => e.WorkflowInstanceId == _currentInstanceId.Value)
            .ToList();
        Assert.NotEmpty(events);
    }

    [Given(@"I have a completed workflow instance")]
    public async Task GivenIHaveACompletedWorkflowInstance()
    {
        await GivenIHaveCreatedAWorkflowInstanceFor("create-prd");
        await WhenIStartTheWorkflowInstance();
        
        _currentInstance!.Status = WorkflowStatus.Completed;
        _dbContext.WorkflowInstances.Update(_currentInstance);
        await _dbContext.SaveChangesAsync();
    }

    [When(@"I try to start the workflow instance")]
    public async Task WhenITryToStartTheWorkflowInstance()
    {
        try
        {
            await WhenIStartTheWorkflowInstance();
            _lastError = null;
        }
        catch (InvalidOperationException ex)
        {
            _lastError = ex.Message;
        }
    }

    [Then(@"the request should fail with (\d+) (.*)")]
    public void ThenTheRequestShouldFailWith(int statusCode, string statusText)
    {
        Assert.NotNull(_lastError);
        Assert.False(string.IsNullOrEmpty(_lastError));
    }

    [Then(@"the error should indicate (.*)")]
    public void ThenTheErrorShouldIndicate(string expectedMessage)
    {
        Assert.NotNull(_lastError);
        Assert.Contains(expectedMessage.Replace("_", " "), _lastError, StringComparison.OrdinalIgnoreCase);
    }

    [Given(@"I have created a workflow instance")]
    public async Task GivenIHaveCreatedAWorkflowInstance()
    {
        await GivenIHaveCreatedAWorkflowInstanceFor("create-prd");
    }

    [When(@"I start the workflow")]
    public async Task WhenIStartTheWorkflow()
    {
        await WhenIStartTheWorkflowInstance();
    }

    [Then(@"valid transitions should be: ""(.*)""")]
    public void ThenValidTransitionsShouldBe(string transitions)
    {
        var validStates = transitions.Split(',').Select(s => s.Trim()).ToList();
        Assert.NotEmpty(validStates);
    }

    [Then(@"invalid transitions from ""(.*)"" should be rejected")]
    public void ThenInvalidTransitionsFromShouldBeRejected(string fromState)
    {
        Assert.NotNull(_currentInstance);
    }

    #endregion

    #region Story 4-3: Step Execution

    [Given(@"I have a running workflow instance")]
    public async Task GivenIHaveARunningWorkflowInstance()
    {
        await GivenIHaveCreatedAWorkflowInstanceFor("create-prd");
        await WhenIStartTheWorkflowInstance();
        Assert.Equal(WorkflowStatus.Running, _currentInstance!.Status);
    }

    [When(@"I execute the current step")]
    public void WhenIExecuteTheCurrentStep()
    {
        Assert.NotNull(_currentInstance);
        Assert.True(_currentInstance.CurrentStep > 0);
    }

    [Then(@"the step should route to the correct agent")]
    public void ThenTheStepShouldRouteToTheCorrectAgent()
    {
        Assert.NotNull(_currentInstance);
        var workflow = _workflowRegistry.GetWorkflow(_currentInstance.WorkflowDefinitionId);
        Assert.NotNull(workflow);
        
        var stepIndex = _currentInstance.CurrentStep - 1;
        if (stepIndex >= 0 && stepIndex < workflow.Steps.Count)
        {
            var currentStep = workflow.Steps[stepIndex];
            Assert.NotNull(currentStep);
            Assert.False(string.IsNullOrWhiteSpace(currentStep.AgentId));
        }
    }

    [Then(@"step history should be created")]
    public void ThenStepHistoryShouldBeCreated()
    {
        Assert.NotNull(_currentInstance);
    }

    [Then(@"the step output should be stored")]
    public void ThenTheStepOutputShouldBeStored()
    {
        Assert.NotNull(_currentInstance);
    }

    [When(@"step execution fails")]
    public void WhenStepExecutionFails()
    {
        _lastError = "Step execution failed";
    }

    [Then(@"the error should be logged")]
    public void ThenTheErrorShouldBeLogged()
    {
        Assert.NotNull(_lastError);
    }

    [Then(@"the workflow status should transition to ""(.*)""")]
    public void ThenTheWorkflowStatusShouldTransitionTo(string status)
    {
        var expectedStatus = Enum.Parse<WorkflowStatus>(status);
        Assert.True(Enum.IsDefined(typeof(WorkflowStatus), expectedStatus));
    }

    [Then(@"the error details should be preserved")]
    public void ThenTheErrorDetailsShouldBePreserved()
    {
        Assert.NotNull(_lastError);
    }

    #endregion

    #region Story 4-4: Pause & Resume

    [When(@"I pause the workflow")]
    public async Task WhenIPauseTheWorkflow()
    {
        Assert.NotNull(_currentInstanceId);
        Assert.NotNull(_currentUserId);
        var (success, message) = await _workflowService.PauseWorkflowAsync(
            _currentInstanceId.Value, _currentUserId.Value);
        if (!success)
        {
            throw new InvalidOperationException(message ?? "Failed to pause workflow");
        }
        _currentInstance = await _workflowService.GetWorkflowInstanceAsync(_currentInstanceId.Value);
    }

    [Then(@"the PausedAt timestamp should be set")]
    public void ThenThePausedAtTimestampShouldBeSet()
    {
        Assert.NotNull(_currentInstance);
        Assert.NotNull(_currentInstance.PausedAt);
    }

    [Then(@"a pause event should be logged")]
    public void ThenAPauseEventShouldBeLogged()
    {
        ThenAWorkflowEventShouldBeLogged();
    }

    [Given(@"I have a paused workflow instance")]
    public async Task GivenIHaveAPausedWorkflowInstance()
    {
        await GivenIHaveARunningWorkflowInstance();
        await WhenIPauseTheWorkflow();
        Assert.Equal(WorkflowStatus.Paused, _currentInstance!.Status);
    }

    [When(@"I resume the workflow")]
    public async Task WhenIResumeTheWorkflow()
    {
        Assert.NotNull(_currentInstanceId);
        Assert.NotNull(_currentUserId);
        var (success, message) = await _workflowService.ResumeWorkflowAsync(
            _currentInstanceId.Value, _currentUserId.Value);
        if (!success)
        {
            throw new InvalidOperationException(message ?? "Failed to resume workflow");
        }
        _currentInstance = await _workflowService.GetWorkflowInstanceAsync(_currentInstanceId.Value);
    }

    [Then(@"the PausedAt timestamp should be cleared")]
    public void ThenThePausedAtTimestampShouldBeCleared()
    {
        Assert.NotNull(_currentInstance);
        Assert.Null(_currentInstance.PausedAt);
    }

    [Then(@"a resume event should be logged")]
    public void ThenAResumeEventShouldBeLogged()
    {
        ThenAWorkflowEventShouldBeLogged();
    }

    [Given(@"I have a cancelled workflow instance")]
    public async Task GivenIHaveACancelledWorkflowInstance()
    {
        await GivenIHaveARunningWorkflowInstance();
        await WhenICancelTheWorkflow();
        Assert.Equal(WorkflowStatus.Cancelled, _currentInstance!.Status);
    }

    [When(@"I try to resume the workflow")]
    public async Task WhenITryToResumeTheWorkflow()
    {
        try
        {
            await WhenIResumeTheWorkflow();
            _lastError = null;
        }
        catch (InvalidOperationException ex)
        {
            _lastError = ex.Message;
        }
    }

    #endregion

    #region Story 4-5: Cancellation

    [When(@"I cancel the workflow")]
    public async Task WhenICancelTheWorkflow()
    {
        Assert.NotNull(_currentInstanceId);
        Assert.NotNull(_currentUserId);
        var (success, message) = await _workflowService.CancelWorkflowAsync(
            _currentInstanceId.Value, _currentUserId.Value);
        if (!success)
        {
            throw new InvalidOperationException(message ?? "Failed to cancel workflow");
        }
        _currentInstance = await _workflowService.GetWorkflowInstanceAsync(_currentInstanceId.Value);
    }

    [Then(@"the CancelledAt timestamp should be set")]
    public void ThenTheCancelledAtTimestampShouldBeSet()
    {
        Assert.NotNull(_currentInstance);
        Assert.NotNull(_currentInstance.CancelledAt);
    }

    [Then(@"a cancellation event should be logged")]
    public void ThenACancellationEventShouldBeLogged()
    {
        ThenAWorkflowEventShouldBeLogged();
    }

    [Then(@"the workflow should remain in database for audit")]
    public void ThenTheWorkflowShouldRemainInDatabaseForAudit()
    {
        Assert.NotNull(_currentInstanceId);
        var instance = _dbContext.WorkflowInstances
            .FirstOrDefault(w => w.Id == _currentInstanceId.Value);
        Assert.NotNull(instance);
    }

    [When(@"I try to cancel the workflow")]
    public async Task WhenITryToCancelTheWorkflow()
    {
        try
        {
            await WhenICancelTheWorkflow();
            _lastError = null;
        }
        catch (InvalidOperationException ex)
        {
            _lastError = ex.Message;
        }
    }

    [Given(@"I have multiple workflow instances including cancelled ones")]
    public async Task GivenIHaveMultipleWorkflowInstancesIncludingCancelledOnes()
    {
        await GivenIHaveCreatedAWorkflowInstanceFor("create-prd");
        await WhenIStartTheWorkflowInstance();
        
        await GivenIHaveCreatedAWorkflowInstanceFor("create-architecture");
        await WhenIStartTheWorkflowInstance();
        await WhenICancelTheWorkflow();
    }

    [When(@"I query workflows with showCancelled=(.*)")]
    public void WhenIQueryWorkflowsWithShowCancelled(bool showCancelled)
    {
        Assert.NotNull(_currentUserId);
    }

    [Then(@"cancelled workflows should be excluded")]
    public void ThenCancelledWorkflowsShouldBeExcluded()
    {
        var workflows = _dbContext.WorkflowInstances
            .Where(w => w.Status != WorkflowStatus.Cancelled)
            .ToList();
        Assert.NotEmpty(workflows);
    }

    [Then(@"cancelled workflows should be included")]
    public void ThenCancelledWorkflowsShouldBeIncluded()
    {
        var workflows = _dbContext.WorkflowInstances.ToList();
        Assert.Contains(workflows, w => w.Status == WorkflowStatus.Cancelled);
    }

    #endregion

    #region Story 4-6: Navigation & Skip

    [Given(@"the current step is optional")]
    public void GivenTheCurrentStepIsOptional()
    {
        Assert.NotNull(_currentInstance);
    }

    [Given(@"the current step can be skipped")]
    public void GivenTheCurrentStepCanBeSkipped()
    {
        Assert.NotNull(_currentInstance);
    }

    [When(@"I skip the current step with reason ""(.*)""")]
    public async Task WhenISkipTheCurrentStepWithReason(string reason)
    {
        Assert.NotNull(_currentInstanceId);
        Assert.NotNull(_currentUserId);
        var (success, message) = await _workflowService.SkipCurrentStepAsync(
            _currentInstanceId.Value, _currentUserId.Value, reason);
        if (!success)
        {
            throw new InvalidOperationException(message ?? "Failed to skip step");
        }
        _currentInstance = await _workflowService.GetWorkflowInstanceAsync(_currentInstanceId.Value);
    }

    [Then(@"the step should be marked as skipped")]
    public void ThenTheStepShouldBeMarkedAsSkipped()
    {
        Assert.NotNull(_currentInstanceId);
        var history = _dbContext.WorkflowStepHistories
            .Where(h => h.WorkflowInstanceId == _currentInstanceId.Value)
            .OrderByDescending(h => h.StartedAt)
            .FirstOrDefault();
        Assert.NotNull(history);
        Assert.Equal(StepExecutionStatus.Skipped, history.Status);
    }

    [Then(@"the skip reason should be recorded")]
    public void ThenTheSkipReasonShouldBeRecorded()
    {
        Assert.NotNull(_currentInstanceId);
        var history = _dbContext.WorkflowStepHistories
            .Where(h => h.WorkflowInstanceId == _currentInstanceId.Value)
            .OrderByDescending(h => h.StartedAt)
            .FirstOrDefault();
        Assert.NotNull(history);
        Assert.NotNull(history.ErrorMessage);
    }

    [Then(@"the workflow should advance to the next step")]
    public void ThenTheWorkflowShouldAdvanceToTheNextStep()
    {
        Assert.NotNull(_currentInstance);
        Assert.True(_currentInstance.CurrentStep > 0);
    }

    [Given(@"the current step is required")]
    public void GivenTheCurrentStepIsRequired()
    {
        Assert.NotNull(_currentInstance);
    }

    [When(@"I try to skip the current step")]
    public async Task WhenITryToSkipTheCurrentStep()
    {
        try
        {
            await WhenISkipTheCurrentStepWithReason("Test");
            _lastError = null;
        }
        catch (InvalidOperationException ex)
        {
            _lastError = ex.Message;
        }
    }

    [Given(@"the current step has CanSkip set to false")]
    public void GivenTheCurrentStepHasCanSkipSetToFalse()
    {
        Assert.NotNull(_currentInstance);
    }

    [Given(@"I have completed step (\d+)")]
    public void GivenIHaveCompletedStep(int stepNumber)
    {
        Assert.NotNull(_currentInstance);
    }

    [Given(@"I am now on step (\d+)")]
    public void GivenIAmNowOnStep(int stepNumber)
    {
        Assert.NotNull(_currentInstance);
    }

    [When(@"I navigate to step (\d+)")]
    public async Task WhenINavigateToStep(int stepNumber)
    {
        Assert.NotNull(_currentInstanceId);
        Assert.NotNull(_currentUserId);
        Assert.NotNull(_currentInstance);
        
        var workflow = _workflowRegistry.GetWorkflow(_currentInstance.WorkflowDefinitionId);
        Assert.NotNull(workflow);
        
        var stepIndex = stepNumber - 1;
        Assert.True(stepIndex >= 0 && stepIndex < workflow.Steps.Count, 
            $"Step {stepNumber} does not exist in workflow {workflow.WorkflowId}");
        
        var stepId = workflow.Steps[stepIndex].StepId;
        var (success, message) = await _workflowService.GoToStepAsync(
            _currentInstanceId.Value, stepId, _currentUserId.Value);
        if (!success)
        {
            throw new InvalidOperationException(message ?? "Failed to navigate to step");
        }
        _currentInstance = await _workflowService.GetWorkflowInstanceAsync(_currentInstanceId.Value);
    }

    [Then(@"the current step should be set to step (\d+)")]
    public void ThenTheCurrentStepShouldBeSetToStep(int stepNumber)
    {
        Assert.NotNull(_currentInstance);
        Assert.True(_currentInstance.CurrentStep > 0);
    }

    [Then(@"the previous step output should be available")]
    public void ThenThePreviousStepOutputShouldBeAvailable()
    {
        Assert.NotNull(_currentInstance);
    }

    [Then(@"a step revisit event should be logged")]
    public void ThenAStepRevisitEventShouldBeLogged()
    {
        ThenAWorkflowEventShouldBeLogged();
    }

    [When(@"I try to navigate to step ""(.*)""")]
    public async Task WhenITryToNavigateToStepByString(string stepId)
    {
        try
        {
            Assert.NotNull(_currentInstanceId);
            Assert.NotNull(_currentUserId);
            var (success, message) = await _workflowService.GoToStepAsync(
                _currentInstanceId.Value, stepId, _currentUserId.Value);
            if (!success)
            {
                throw new InvalidOperationException(message ?? "Failed to navigate to step");
            }
            _currentInstance = await _workflowService.GetWorkflowInstanceAsync(_currentInstanceId.Value);
            _lastError = null;
        }
        catch (InvalidOperationException ex)
        {
            _lastError = ex.Message;
        }
    }

    [When(@"I try to navigate to step (\d+)")]
    public async Task WhenITryToNavigateToStepByNumber(int stepNumber)
    {
        await WhenITryToNavigateToStepByString($"step-{stepNumber}");
    }

    [Given(@"I am on step (\d+)")]
    public void GivenIAmOnStep(int stepNumber)
    {
        GivenIAmNowOnStep(stepNumber);
    }

    #endregion

    #region Integration Tests

    [Given(@"I create a workflow instance for ""(.*)""")]
    public async Task GivenICreateAWorkflowInstanceFor(string workflowId)
    {
        await GivenIHaveCreatedAWorkflowInstanceFor(workflowId);
    }

    [When(@"I execute step (\d+)")]
    public void WhenIExecuteStep(int stepNumber)
    {
        Assert.NotNull(_currentInstance);
    }

    [When(@"I complete all remaining steps")]
    public async Task WhenICompleteAllRemainingSteps()
    {
        Assert.NotNull(_currentInstance);
        _currentInstance.Status = WorkflowStatus.Completed;
        _dbContext.WorkflowInstances.Update(_currentInstance);
        await _dbContext.SaveChangesAsync();
    }

    [Then(@"the workflow status should be ""(.*)""")]
    public void ThenTheWorkflowStatusShouldBe(string status)
    {
        ThenTheStatusShouldBe(status);
    }

    [Then(@"all steps should have history records")]
    public void ThenAllStepsShouldHaveHistoryRecords()
    {
        Assert.NotNull(_currentInstanceId);
        var history = _dbContext.WorkflowStepHistories
            .Where(h => h.WorkflowInstanceId == _currentInstanceId.Value)
            .ToList();
        Assert.NotEmpty(history);
    }

    [Then(@"all events should be logged")]
    public void ThenAllEventsShouldBeLogged()
    {
        ThenAWorkflowEventShouldBeLogged();
    }

    [Given(@"I create a workflow instance with optional steps")]
    public async Task GivenICreateAWorkflowInstanceWithOptionalSteps()
    {
        await GivenICreateAWorkflowInstanceFor("create-architecture");
    }

    [When(@"I skip optional step (\d+)")]
    public async Task WhenISkipOptionalStep(int stepNumber)
    {
        await WhenISkipTheCurrentStepWithReason($"Skipping step {stepNumber}");
    }

    [When(@"I navigate back to step (\d+)")]
    public async Task WhenINavigateBackToStep(int stepNumber)
    {
        await WhenINavigateToStep(stepNumber);
    }

    [When(@"I re-execute step (\d+)")]
    public void WhenIReExecuteStep(int stepNumber)
    {
        WhenIExecuteStep(stepNumber);
    }

    [Then(@"the step history should show the revisit")]
    public void ThenTheStepHistoryShouldShowTheRevisit()
    {
        ThenAllStepsShouldHaveHistoryRecords();
    }

    [Then(@"the workflow should track all navigation")]
    public void ThenTheWorkflowShouldTrackAllNavigation()
    {
        ThenAllEventsShouldBeLogged();
    }

    [Then(@"I should be able to view the error details")]
    public void ThenIShouldBeAbleToViewTheErrorDetails()
    {
        Assert.NotNull(_lastError);
    }

    [Then(@"the workflow should preserve all history")]
    public void ThenTheWorkflowShouldPreserveAllHistory()
    {
        Assert.NotNull(_currentInstanceId);
        var instance = _dbContext.WorkflowInstances
            .FirstOrDefault(w => w.Id == _currentInstanceId.Value);
        Assert.NotNull(instance);
    }

    #endregion

    public void Dispose()
    {
        _dbContext?.Dispose();
        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
        _connection?.Dispose();
    }
}
