using System.Collections.Concurrent;

namespace bmadServer.ApiService.Services.Workflows.Agents;

/// <summary>
/// Service for managing human approval of agent decisions
/// </summary>
public class HumanApprovalService : IHumanApprovalService
{
    private readonly ILogger<HumanApprovalService> _logger;
    private readonly ConcurrentDictionary<Guid, ApprovalRequest> _pendingApprovals;
    private readonly ConcurrentDictionary<Guid, List<ApprovalDecision>> _approvalHistory;
    private const double ConfidenceThreshold = 0.7;

    public HumanApprovalService(ILogger<HumanApprovalService> logger)
    {
        _logger = logger;
        _pendingApprovals = new ConcurrentDictionary<Guid, ApprovalRequest>();
        _approvalHistory = new ConcurrentDictionary<Guid, List<ApprovalDecision>>();
    }

    /// <inheritdoc />
    public void RequestApproval(ApprovalRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        _logger.LogInformation(
            "Approval requested for workflow {WorkflowInstanceId}, agent {AgentId}, confidence: {ConfidenceScore}",
            request.WorkflowInstanceId,
            request.AgentId,
            request.ConfidenceScore);

        _pendingApprovals[request.RequestId] = request;
    }

    /// <inheritdoc />
    public ApprovalDecision Approve(Guid requestId, Guid userId)
    {
        if (!_pendingApprovals.TryRemove(requestId, out var request))
        {
            throw new InvalidOperationException($"Approval request {requestId} not found");
        }

        var decision = new ApprovalDecision
        {
            RequestId = requestId,
            Status = ApprovalStatus.Approved,
            ApprovedBy = userId,
            DecidedAt = DateTime.UtcNow,
            OriginalResponse = request.ProposedResponse,
            FinalResponse = request.ProposedResponse
        };

        LogDecision(request.WorkflowInstanceId, decision);
        return decision;
    }

    /// <inheritdoc />
    public ApprovalDecision Modify(Guid requestId, string modifiedResponse, Guid userId)
    {
        if (!_pendingApprovals.TryRemove(requestId, out var request))
        {
            throw new InvalidOperationException($"Approval request {requestId} not found");
        }

        var decision = new ApprovalDecision
        {
            RequestId = requestId,
            Status = ApprovalStatus.Modified,
            ApprovedBy = userId,
            DecidedAt = DateTime.UtcNow,
            OriginalResponse = request.ProposedResponse,
            FinalResponse = modifiedResponse
        };

        _logger.LogInformation(
            "Response modified by user {UserId} for request {RequestId}",
            userId,
            requestId);

        LogDecision(request.WorkflowInstanceId, decision);
        return decision;
    }

    /// <inheritdoc />
    public ApprovalDecision Reject(Guid requestId, string reason, Guid userId)
    {
        if (!_pendingApprovals.TryRemove(requestId, out var request))
        {
            throw new InvalidOperationException($"Approval request {requestId} not found");
        }

        var decision = new ApprovalDecision
        {
            RequestId = requestId,
            Status = ApprovalStatus.Rejected,
            ApprovedBy = userId,
            DecidedAt = DateTime.UtcNow,
            OriginalResponse = request.ProposedResponse,
            FinalResponse = string.Empty,
            RejectionReason = reason
        };

        _logger.LogWarning(
            "Response rejected by user {UserId} for request {RequestId}: {Reason}",
            userId,
            requestId,
            reason);

        LogDecision(request.WorkflowInstanceId, decision);
        return decision;
    }

    /// <inheritdoc />
    public IReadOnlyList<ApprovalRequest> GetPendingApprovals(Guid workflowInstanceId)
    {
        return _pendingApprovals.Values
            .Where(r => r.WorkflowInstanceId == workflowInstanceId)
            .OrderBy(r => r.RequestedAt)
            .ToList()
            .AsReadOnly();
    }

    /// <inheritdoc />
    public IReadOnlyList<ApprovalDecision> GetApprovalHistory(Guid workflowInstanceId)
    {
        if (_approvalHistory.TryGetValue(workflowInstanceId, out var history))
        {
            return history.OrderBy(d => d.DecidedAt).ToList().AsReadOnly();
        }

        return Array.Empty<ApprovalDecision>();
    }

    /// <inheritdoc />
    public bool IsApprovalNeeded(double confidenceScore)
    {
        return confidenceScore < ConfidenceThreshold;
    }

    private void LogDecision(Guid workflowInstanceId, ApprovalDecision decision)
    {
        _approvalHistory.AddOrUpdate(
            workflowInstanceId,
            _ => new List<ApprovalDecision> { decision },
            (_, existing) =>
            {
                existing.Add(decision);
                return existing;
            });
    }
}
