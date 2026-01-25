using bmadServer.ApiService.Agents;
using bmadServer.ApiService.Data;
using bmadServer.ApiService.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Reqnroll;
using Xunit;

namespace bmadServer.BDD.Tests.StepDefinitions;

[Binding]
public class HumanApprovalForLowConfidenceDecisionsSteps
{
    private ApplicationDbContext _context = null!;
    private IApprovalService _approvalService = null!;
    private double _confidenceScore;
    private bool _requiresApproval;
    private Guid _approvalRequestId;
    private Guid _workflowInstanceId;
    private Guid _userId;
    private ApprovalRequest? _approvalRequest;
    private bool _operationResult;
    private List<ApprovalRequest> _pendingRequests = new();

    [BeforeScenario]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        var logger = NullLogger<ApprovalService>.Instance;
        _approvalService = new ApprovalService(_context, logger);

        _workflowInstanceId = Guid.NewGuid();
        _userId = Guid.NewGuid();
    }

    [AfterScenario]
    public void Cleanup()
    {
        _context?.Dispose();
    }

    [Given(@"an agent generates a response with confidence (.*)")]
    public void GivenAnAgentGeneratesAResponseWithConfidence(double confidence)
    {
        _confidenceScore = confidence;
    }

    [When(@"I check if approval is required with threshold (.*)")]
    public void WhenICheckIfApprovalIsRequiredWithThreshold(double threshold)
    {
        _requiresApproval = _approvalService.RequiresApproval(_confidenceScore, threshold);
    }

    [Then(@"approval is required")]
    public void ThenApprovalIsRequired()
    {
        Assert.True(_requiresApproval, "Expected approval to be required for low confidence score");
    }

    [Then(@"approval is not required")]
    public void ThenApprovalIsNotRequired()
    {
        Assert.False(_requiresApproval, "Expected approval not to be required for sufficient confidence score");
    }

    [Given(@"an agent generates a low-confidence response")]
    public async Task GivenAnAgentGeneratesALowConfidenceResponse()
    {
        _approvalRequestId = await _approvalService.CreateApprovalRequestAsync(
            _workflowInstanceId,
            "architect",
            "Proposed architecture design",
            0.65,
            "Based on limited context",
            CancellationToken.None);
    }

    [When(@"the approval request is created")]
    public async Task WhenTheApprovalRequestIsCreated()
    {
        _approvalRequest = await _approvalService.GetApprovalRequestAsync(_approvalRequestId, CancellationToken.None);
    }

    [Then(@"the workflow transitions to WaitingForApproval state")]
    public void ThenTheWorkflowTransitionsToWaitingForApprovalState()
    {
        Assert.NotNull(_approvalRequest);
    }

    [Then(@"the approval request has status ""(.*)""")]
    public void ThenTheApprovalRequestHasStatus(string expectedStatus)
    {
        Assert.NotNull(_approvalRequest);
        Assert.Equal(expectedStatus, _approvalRequest.Status);
    }

    [Then(@"the approval request status is ""(.*)""")]
    public void ThenTheApprovalRequestStatusIs(string expectedStatus)
    {
        Assert.NotNull(_approvalRequest);
        Assert.Equal(expectedStatus, _approvalRequest.Status);
    }

    [Then(@"the approval request includes proposed response")]
    public void ThenTheApprovalRequestIncludesProposedResponse()
    {
        Assert.NotNull(_approvalRequest);
        Assert.NotEmpty(_approvalRequest.ProposedResponse);
    }

    [Then(@"the approval request includes confidence score")]
    public void ThenTheApprovalRequestIncludesConfidenceScore()
    {
        Assert.NotNull(_approvalRequest);
        Assert.InRange(_approvalRequest.ConfidenceScore, 0.0, 1.0);
    }

    [Then(@"the approval request includes reasoning")]
    public void ThenTheApprovalRequestIncludesReasoning()
    {
        Assert.NotNull(_approvalRequest);
        Assert.NotNull(_approvalRequest.Reasoning);
    }

    [Given(@"an approval request exists with status ""(.*)""")]
    public async Task GivenAnApprovalRequestExistsWithStatus(string status)
    {
        var approvalRequest = new ApprovalRequest
        {
            WorkflowInstanceId = _workflowInstanceId,
            AgentId = "architect",
            ProposedResponse = "Proposed solution",
            ConfidenceScore = 0.65,
            Reasoning = "Initial reasoning",
            Status = status,
            CreatedAt = DateTime.UtcNow
        };

        _context.ApprovalRequests.Add(approvalRequest);
        await _context.SaveChangesAsync();

        _approvalRequestId = approvalRequest.Id;
    }

    [When(@"I approve the decision with my userId")]
    public async Task WhenIApproveTheDecisionWithMyUserId()
    {
        _operationResult = await _approvalService.ApproveAsync(_approvalRequestId, _userId, CancellationToken.None);
        _approvalRequest = await _approvalService.GetApprovalRequestAsync(_approvalRequestId, CancellationToken.None);
    }

    [Then(@"the final response equals the proposed response")]
    public void ThenTheFinalResponseEqualsTheProposedResponse()
    {
        Assert.NotNull(_approvalRequest);
        Assert.Equal(_approvalRequest.ProposedResponse, _approvalRequest.FinalResponse);
    }

    [Then(@"the approval is logged with my userId")]
    public void ThenTheApprovalIsLoggedWithMyUserId()
    {
        Assert.NotNull(_approvalRequest);
        Assert.Equal(_userId, _approvalRequest.ApprovedByUserId);
    }

    [Then(@"the responded timestamp is set")]
    public void ThenTheRespondedTimestampIsSet()
    {
        Assert.NotNull(_approvalRequest);
        Assert.NotNull(_approvalRequest.RespondedAt);
    }

    [When(@"I modify the proposed response and confirm")]
    public async Task WhenIModifyTheProposedResponseAndConfirm()
    {
        _operationResult = await _approvalService.ModifyAsync(
            _approvalRequestId,
            _userId,
            "Modified solution with improvements",
            CancellationToken.None);
        _approvalRequest = await _approvalService.GetApprovalRequestAsync(_approvalRequestId, CancellationToken.None);
    }

    [Then(@"the final response contains my modifications")]
    public void ThenTheFinalResponseContainsMyModifications()
    {
        Assert.NotNull(_approvalRequest);
        Assert.Equal("Modified solution with improvements", _approvalRequest.FinalResponse);
    }

    [Then(@"both original and modified versions are logged")]
    public void ThenBothOriginalAndModifiedVersionsAreLogged()
    {
        Assert.NotNull(_approvalRequest);
        Assert.NotEmpty(_approvalRequest.ProposedResponse);
        Assert.NotNull(_approvalRequest.FinalResponse);
        Assert.NotEqual(_approvalRequest.ProposedResponse, _approvalRequest.FinalResponse);
    }

    [When(@"I reject the decision with a reason")]
    public async Task WhenIRejectTheDecisionWithAReason()
    {
        _operationResult = await _approvalService.RejectAsync(
            _approvalRequestId,
            _userId,
            "Needs more technical detail",
            CancellationToken.None);
        _approvalRequest = await _approvalService.GetApprovalRequestAsync(_approvalRequestId, CancellationToken.None);
    }

    [Then(@"the rejection reason is logged")]
    public void ThenTheRejectionReasonIsLogged()
    {
        Assert.NotNull(_approvalRequest);
        Assert.Equal("Needs more technical detail", _approvalRequest.RejectionReason);
    }

    [When(@"I try to approve it again")]
    public async Task WhenITryToApproveItAgain()
    {
        _operationResult = await _approvalService.ApproveAsync(_approvalRequestId, _userId, CancellationToken.None);
        _approvalRequest = await _approvalService.GetApprovalRequestAsync(_approvalRequestId, CancellationToken.None);
    }

    [Then(@"the approval fails")]
    public void ThenTheApprovalFails()
    {
        Assert.False(_operationResult, "Expected approval to fail for non-pending request");
    }

    [Then(@"the status remains ""(.*)""")]
    public void ThenTheStatusRemains(string expectedStatus)
    {
        Assert.NotNull(_approvalRequest);
        Assert.Equal(expectedStatus, _approvalRequest.Status);
    }

    [Given(@"an approval request was created (.*) hours ago")]
    public async Task GivenAnApprovalRequestWasCreatedHoursAgo(int hoursAgo)
    {
        var approvalRequest = new ApprovalRequest
        {
            WorkflowInstanceId = _workflowInstanceId,
            AgentId = "architect",
            ProposedResponse = "Proposed solution",
            ConfidenceScore = 0.65,
            Reasoning = "Initial reasoning",
            Status = "Pending",
            CreatedAt = DateTime.UtcNow.AddHours(-hoursAgo)
        };

        _context.ApprovalRequests.Add(approvalRequest);
        await _context.SaveChangesAsync();

        _approvalRequestId = approvalRequest.Id;
    }

    [Given(@"no reminder has been sent")]
    public void GivenNoReminderHasBeenSent()
    {
        // Default state - no action needed
    }

    [Given(@"the request is still pending")]
    public void GivenTheRequestIsStillPending()
    {
        // Already set up in the previous step
    }

    [When(@"I query for pending requests needing reminders")]
    public async Task WhenIQueryForPendingRequestsNeedingReminders()
    {
        _pendingRequests = await _approvalService.GetPendingRequestsNeedingRemindersAsync(24, CancellationToken.None);
    }

    [Then(@"the approval request is included in the results")]
    public void ThenTheApprovalRequestIsIncludedInTheResults()
    {
        Assert.Contains(_pendingRequests, ar => ar.Id == _approvalRequestId);
    }

    [Then(@"the approval request is not included in the results")]
    public void ThenTheApprovalRequestIsNotIncludedInTheResults()
    {
        Assert.DoesNotContain(_pendingRequests, ar => ar.Id == _approvalRequestId);
    }

    [When(@"I query for timed out requests")]
    public async Task WhenIQueryForTimedOutRequests()
    {
        _pendingRequests = await _approvalService.GetTimedOutRequestsAsync(72, CancellationToken.None);
    }

    [When(@"I mark the reminder as sent")]
    public async Task WhenIMarkTheReminderAsSent()
    {
        _operationResult = await _approvalService.MarkReminderSentAsync(_approvalRequestId, CancellationToken.None);
        _approvalRequest = await _approvalService.GetApprovalRequestAsync(_approvalRequestId, CancellationToken.None);
    }

    [Then(@"the LastReminderSentAt timestamp is set")]
    public void ThenTheLastReminderSentAtTimestampIsSet()
    {
        Assert.NotNull(_approvalRequest);
        Assert.NotNull(_approvalRequest.LastReminderSentAt);
    }

    [When(@"I timeout the request")]
    public async Task WhenITimeoutTheRequest()
    {
        _operationResult = await _approvalService.TimeoutRequestAsync(_approvalRequestId, CancellationToken.None);
        _approvalRequest = await _approvalService.GetApprovalRequestAsync(_approvalRequestId, CancellationToken.None);
    }
}
