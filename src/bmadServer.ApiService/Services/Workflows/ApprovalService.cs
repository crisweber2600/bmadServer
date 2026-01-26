using bmadServer.ApiService.Data;
using bmadServer.ApiService.Models.Workflows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace bmadServer.ApiService.Services.Workflows;

public class ApprovalService : IApprovalService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ApprovalService> _logger;

    public ApprovalService(
        ApplicationDbContext context,
        ILogger<ApprovalService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ApprovalRequest> CreateApprovalRequestAsync(
        Guid workflowInstanceId,
        string agentId,
        string stepId,
        string proposedResponse,
        double confidenceScore,
        string? reasoning,
        Guid requestedBy,
        CancellationToken cancellationToken = default)
    {
        if (workflowInstanceId == Guid.Empty)
            throw new ArgumentException("Workflow instance ID cannot be empty", nameof(workflowInstanceId));
        if (string.IsNullOrWhiteSpace(agentId))
            throw new ArgumentException("Agent ID cannot be null or empty", nameof(agentId));
        if (string.IsNullOrWhiteSpace(stepId))
            throw new ArgumentException("Step ID cannot be null or empty", nameof(stepId));
        if (string.IsNullOrWhiteSpace(proposedResponse))
            throw new ArgumentException("Proposed response cannot be null or empty", nameof(proposedResponse));
        if (confidenceScore < 0 || confidenceScore > 1)
            throw new ArgumentOutOfRangeException(nameof(confidenceScore), "Confidence score must be between 0 and 1");
        if (requestedBy == Guid.Empty)
            throw new ArgumentException("RequestedBy user ID cannot be empty", nameof(requestedBy));

        var approvalRequest = new ApprovalRequest
        {
            Id = Guid.NewGuid(),
            WorkflowInstanceId = workflowInstanceId,
            AgentId = agentId,
            StepId = stepId,
            ProposedResponse = proposedResponse,
            ConfidenceScore = confidenceScore,
            Reasoning = reasoning,
            Status = ApprovalStatus.Pending,
            RequestedAt = DateTime.UtcNow,
            RequestedBy = requestedBy,
            Version = 1
        };

        _context.ApprovalRequests.Add(approvalRequest);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created approval request {ApprovalId} for workflow {WorkflowId}, agent {AgentId}, confidence {Confidence:F2}",
            approvalRequest.Id, workflowInstanceId, agentId, confidenceScore);

        return approvalRequest;
    }

    public async Task<ApprovalRequest?> GetApprovalRequestAsync(
        Guid approvalRequestId,
        CancellationToken cancellationToken = default)
    {
        if (approvalRequestId == Guid.Empty)
            return null;

        return await _context.ApprovalRequests
            .Include(a => a.WorkflowInstance)
            .FirstOrDefaultAsync(a => a.Id == approvalRequestId, cancellationToken);
    }

    public async Task<ApprovalRequest?> GetPendingApprovalAsync(
        Guid workflowInstanceId,
        CancellationToken cancellationToken = default)
    {
        if (workflowInstanceId == Guid.Empty)
            return null;

        return await _context.ApprovalRequests
            .Where(a => a.WorkflowInstanceId == workflowInstanceId && a.Status == ApprovalStatus.Pending)
            .OrderBy(a => a.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<(bool Success, string? Message, ApprovalRequest? ApprovalRequest)> ApproveAsync(
        Guid approvalRequestId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var approval = await _context.ApprovalRequests
            .Include(a => a.WorkflowInstance)
            .FirstOrDefaultAsync(a => a.Id == approvalRequestId, cancellationToken);

        if (approval == null)
            return (false, "Approval request not found", null);

        if (approval.Status != ApprovalStatus.Pending)
            return (false, $"Approval request is not pending (current status: {approval.Status})", null);

        var ownerValidation = ValidateWorkflowOwner(approval, userId);
        if (!ownerValidation.Success)
            return (false, ownerValidation.Message, null);

        try
        {
            approval.Status = ApprovalStatus.Approved;
            approval.ResolvedAt = DateTime.UtcNow;
            approval.ResolvedBy = userId;
            approval.Version++;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Approval request {ApprovalId} approved by user {UserId} for workflow {WorkflowId}",
                approvalRequestId, userId, approval.WorkflowInstanceId);

            return (true, null, approval);
        }
        catch (DbUpdateConcurrencyException)
        {
            _logger.LogWarning(
                "Concurrency conflict approving request {ApprovalId} - another action was taken",
                approvalRequestId);
            return (false, "Approval request was modified by another process", null);
        }
    }

    public async Task<(bool Success, string? Message, ApprovalRequest? ApprovalRequest)> ModifyAndApproveAsync(
        Guid approvalRequestId,
        Guid userId,
        string modifiedResponse,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(modifiedResponse))
            return (false, "Modified response cannot be empty", null);

        var approval = await _context.ApprovalRequests
            .Include(a => a.WorkflowInstance)
            .FirstOrDefaultAsync(a => a.Id == approvalRequestId, cancellationToken);

        if (approval == null)
            return (false, "Approval request not found", null);

        if (approval.Status != ApprovalStatus.Pending)
            return (false, $"Approval request is not pending (current status: {approval.Status})", null);

        var ownerValidation = ValidateWorkflowOwner(approval, userId);
        if (!ownerValidation.Success)
            return (false, ownerValidation.Message, null);

        try
        {
            approval.Status = ApprovalStatus.Modified;
            approval.ModifiedResponse = modifiedResponse;
            approval.ResolvedAt = DateTime.UtcNow;
            approval.ResolvedBy = userId;
            approval.Version++;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Approval request {ApprovalId} modified and approved by user {UserId} for workflow {WorkflowId}",
                approvalRequestId, userId, approval.WorkflowInstanceId);

            return (true, null, approval);
        }
        catch (DbUpdateConcurrencyException)
        {
            _logger.LogWarning(
                "Concurrency conflict modifying request {ApprovalId} - another action was taken",
                approvalRequestId);
            return (false, "Approval request was modified by another process", null);
        }
    }

    public async Task<(bool Success, string? Message, ApprovalRequest? ApprovalRequest)> RejectAsync(
        Guid approvalRequestId,
        Guid userId,
        string rejectionReason,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rejectionReason))
            return (false, "Rejection reason cannot be empty", null);

        var approval = await _context.ApprovalRequests
            .Include(a => a.WorkflowInstance)
            .FirstOrDefaultAsync(a => a.Id == approvalRequestId, cancellationToken);

        if (approval == null)
            return (false, "Approval request not found", null);

        if (approval.Status != ApprovalStatus.Pending)
            return (false, $"Approval request is not pending (current status: {approval.Status})", null);

        var ownerValidation = ValidateWorkflowOwner(approval, userId);
        if (!ownerValidation.Success)
            return (false, ownerValidation.Message, null);

        try
        {
            approval.Status = ApprovalStatus.Rejected;
            approval.RejectionReason = rejectionReason;
            approval.ResolvedAt = DateTime.UtcNow;
            approval.ResolvedBy = userId;
            approval.Version++;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Approval request {ApprovalId} rejected by user {UserId} for workflow {WorkflowId}. Reason: {Reason}",
                approvalRequestId, userId, approval.WorkflowInstanceId, rejectionReason);

            return (true, null, approval);
        }
        catch (DbUpdateConcurrencyException)
        {
            _logger.LogWarning(
                "Concurrency conflict rejecting request {ApprovalId} - another action was taken",
                approvalRequestId);
            return (false, "Approval request was modified by another process", null);
        }
    }

    public async Task<(List<ApprovalRequest> NeedReminder, List<ApprovalRequest> TimedOut)> GetTimedOutApprovalsAsync(
        TimeSpan reminderThreshold,
        TimeSpan timeoutThreshold,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var reminderCutoff = now - reminderThreshold;
        var timeoutCutoff = now - timeoutThreshold;

        var pendingApprovals = await _context.ApprovalRequests
            .Include(a => a.WorkflowInstance)
            .Where(a => a.Status == ApprovalStatus.Pending && a.RequestedAt < reminderCutoff)
            .ToListAsync(cancellationToken);

        var needReminder = pendingApprovals
            .Where(a => a.RequestedAt < reminderCutoff && a.RequestedAt >= timeoutCutoff)
            .ToList();

        var timedOut = pendingApprovals
            .Where(a => a.RequestedAt < timeoutCutoff)
            .ToList();

        return (needReminder, timedOut);
    }

    public async Task<bool> MarkAsTimedOutAsync(
        Guid approvalRequestId,
        CancellationToken cancellationToken = default)
    {
        var approval = await _context.ApprovalRequests
            .FirstOrDefaultAsync(a => a.Id == approvalRequestId, cancellationToken);

        if (approval == null || approval.Status != ApprovalStatus.Pending)
            return false;

        try
        {
            approval.Status = ApprovalStatus.TimedOut;
            approval.ResolvedAt = DateTime.UtcNow;
            approval.Version++;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogWarning(
                "Approval request {ApprovalId} timed out for workflow {WorkflowId}",
                approvalRequestId, approval.WorkflowInstanceId);

            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            _logger.LogWarning(
                "Concurrency conflict marking timeout for request {ApprovalId}",
                approvalRequestId);
            return false;
        }
    }

    public async Task<List<ApprovalRequest>> GetApprovalHistoryAsync(
        Guid workflowInstanceId,
        CancellationToken cancellationToken = default)
    {
        if (workflowInstanceId == Guid.Empty)
            return [];

        return await _context.ApprovalRequests
            .Where(a => a.WorkflowInstanceId == workflowInstanceId)
            .OrderBy(a => a.RequestedAt)
            .ToListAsync(cancellationToken);
    }

    private static (bool Success, string? Message) ValidateWorkflowOwner(ApprovalRequest approval, Guid userId)
    {
        if (approval.WorkflowInstance == null)
            return (false, "Workflow instance not found");

        if (approval.WorkflowInstance.UserId != userId)
            return (false, "Only the workflow owner can act on approval requests");

        return (true, null);
    }
}
