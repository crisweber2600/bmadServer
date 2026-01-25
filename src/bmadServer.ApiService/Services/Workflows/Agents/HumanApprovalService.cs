using bmadServer.ApiService.Models.Agents;
using Microsoft.Extensions.Logging;

namespace bmadServer.ApiService.Services.Workflows.Agents;

/// <summary>
/// Service for managing human approval workflow
/// </summary>
public class HumanApprovalService : IHumanApprovalService
{
    private readonly ILogger<HumanApprovalService> _logger;
    private readonly Dictionary<Guid, ApprovalRequest> _approvalRequests = new();
    private readonly SemaphoreSlim _lock = new(1, 1);
    private const double ConfidenceThreshold = 0.7;

    public HumanApprovalService(ILogger<HumanApprovalService> logger)
    {
        _logger = logger;
    }

    public async Task<ApprovalRequest> CreateApprovalRequestAsync(
        Guid workflowInstanceId,
        string agentId,
        string proposedResponse,
        double confidenceScore,
        string reasoning,
        CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var request = new ApprovalRequest
            {
                WorkflowInstanceId = workflowInstanceId,
                AgentId = agentId,
                ProposedResponse = proposedResponse,
                ConfidenceScore = confidenceScore,
                Reasoning = reasoning
            };

            _approvalRequests[request.ApprovalRequestId] = request;

            _logger.LogInformation(
                "Approval request created: {ApprovalRequestId} for workflow {WorkflowInstanceId} by agent {AgentId} with confidence {ConfidenceScore}",
                request.ApprovalRequestId,
                workflowInstanceId,
                agentId,
                confidenceScore);

            return request;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<ApprovalRequest?> GetApprovalRequestAsync(
        Guid approvalRequestId,
        CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            _approvalRequests.TryGetValue(approvalRequestId, out var request);
            return request;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<IEnumerable<ApprovalRequest>> GetPendingApprovalsAsync(
        Guid workflowInstanceId,
        CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            return _approvalRequests.Values
                .Where(r => r.WorkflowInstanceId == workflowInstanceId && r.Status == ApprovalStatus.Pending)
                .OrderBy(r => r.CreatedAt)
                .ToList();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<bool> ApproveAsync(
        Guid approvalRequestId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (!_approvalRequests.TryGetValue(approvalRequestId, out var request))
            {
                _logger.LogWarning("Approval request {ApprovalRequestId} not found", approvalRequestId);
                return false;
            }

            if (request.Status != ApprovalStatus.Pending)
            {
                _logger.LogWarning("Approval request {ApprovalRequestId} is not pending", approvalRequestId);
                return false;
            }

            request.Status = ApprovalStatus.Approved;
            request.RespondedByUserId = userId;
            request.RespondedAt = DateTime.UtcNow;
            request.ApprovedResponse = request.ProposedResponse;

            _logger.LogInformation(
                "Approval request {ApprovalRequestId} approved by user {UserId}",
                approvalRequestId,
                userId);

            return true;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<bool> ModifyAndApproveAsync(
        Guid approvalRequestId,
        Guid userId,
        string modifiedResponse,
        CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (!_approvalRequests.TryGetValue(approvalRequestId, out var request))
            {
                _logger.LogWarning("Approval request {ApprovalRequestId} not found", approvalRequestId);
                return false;
            }

            if (request.Status != ApprovalStatus.Pending)
            {
                _logger.LogWarning("Approval request {ApprovalRequestId} is not pending", approvalRequestId);
                return false;
            }

            request.Status = ApprovalStatus.Modified;
            request.RespondedByUserId = userId;
            request.RespondedAt = DateTime.UtcNow;
            request.ApprovedResponse = modifiedResponse;

            _logger.LogInformation(
                "Approval request {ApprovalRequestId} modified and approved by user {UserId}",
                approvalRequestId,
                userId);

            return true;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<bool> RejectAsync(
        Guid approvalRequestId,
        Guid userId,
        string reason,
        string? additionalGuidance = null,
        CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (!_approvalRequests.TryGetValue(approvalRequestId, out var request))
            {
                _logger.LogWarning("Approval request {ApprovalRequestId} not found", approvalRequestId);
                return false;
            }

            if (request.Status != ApprovalStatus.Pending)
            {
                _logger.LogWarning("Approval request {ApprovalRequestId} is not pending", approvalRequestId);
                return false;
            }

            request.Status = ApprovalStatus.Rejected;
            request.RespondedByUserId = userId;
            request.RespondedAt = DateTime.UtcNow;
            request.RejectionReason = reason;
            request.AdditionalGuidance = additionalGuidance;

            _logger.LogInformation(
                "Approval request {ApprovalRequestId} rejected by user {UserId} with reason: {Reason}",
                approvalRequestId,
                userId,
                reason);

            return true;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<IEnumerable<ApprovalRequest>> CheckTimeoutsAsync(
        TimeSpan reminderThreshold,
        TimeSpan autoTimeoutThreshold,
        CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var now = DateTime.UtcNow;
            var timedOut = new List<ApprovalRequest>();

            foreach (var request in _approvalRequests.Values.Where(r => r.Status == ApprovalStatus.Pending))
            {
                var age = now - request.CreatedAt;

                if (age >= autoTimeoutThreshold)
                {
                    request.Status = ApprovalStatus.TimedOut;
                    timedOut.Add(request);

                    _logger.LogWarning(
                        "Approval request {ApprovalRequestId} auto-timed out after {Age}",
                        request.ApprovalRequestId,
                        age);
                }
                else if (age >= reminderThreshold)
                {
                    timedOut.Add(request); // Add to reminder list
                    _logger.LogInformation(
                        "Approval request {ApprovalRequestId} needs reminder, age: {Age}",
                        request.ApprovalRequestId,
                        age);
                }
            }

            return timedOut;
        }
        finally
        {
            _lock.Release();
        }
    }
}
