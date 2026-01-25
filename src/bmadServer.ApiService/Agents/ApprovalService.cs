using bmadServer.ApiService.Data;
using bmadServer.ApiService.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace bmadServer.ApiService.Agents;

/// <summary>
/// Service for managing human approval requests for low-confidence agent decisions.
/// </summary>
public class ApprovalService : IApprovalService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ApprovalService> _logger;

    public ApprovalService(ApplicationDbContext context, ILogger<ApprovalService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public bool RequiresApproval(double confidenceScore, double threshold = 0.7)
    {
        return confidenceScore < threshold;
    }

    /// <inheritdoc />
    public async Task<Guid> CreateApprovalRequestAsync(
        Guid workflowInstanceId,
        string agentId,
        string proposedResponse,
        double confidenceScore,
        string? reasoning = null,
        CancellationToken cancellationToken = default)
    {
        var approvalRequest = new ApprovalRequest
        {
            WorkflowInstanceId = workflowInstanceId,
            AgentId = agentId,
            ProposedResponse = proposedResponse,
            ConfidenceScore = confidenceScore,
            Reasoning = reasoning,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        _context.ApprovalRequests.Add(approvalRequest);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created approval request {ApprovalRequestId} for workflow {WorkflowInstanceId}, agent {AgentId}, confidence {ConfidenceScore}",
            approvalRequest.Id, workflowInstanceId, agentId, confidenceScore);

        return approvalRequest.Id;
    }

    /// <inheritdoc />
    public async Task<ApprovalRequest?> GetApprovalRequestAsync(
        Guid approvalRequestId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ApprovalRequests
            .FirstOrDefaultAsync(ar => ar.Id == approvalRequestId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ApproveAsync(
        Guid approvalRequestId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var approvalRequest = await _context.ApprovalRequests
            .FirstOrDefaultAsync(ar => ar.Id == approvalRequestId, cancellationToken);

        if (approvalRequest == null || approvalRequest.Status != "Pending")
        {
            return false;
        }

        approvalRequest.Status = "Approved";
        approvalRequest.ApprovedByUserId = userId;
        approvalRequest.RespondedAt = DateTime.UtcNow;
        approvalRequest.FinalResponse = approvalRequest.ProposedResponse;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Approval request {ApprovalRequestId} approved by user {UserId}",
            approvalRequestId, userId);

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> ModifyAsync(
        Guid approvalRequestId,
        Guid userId,
        string modifiedResponse,
        CancellationToken cancellationToken = default)
    {
        var approvalRequest = await _context.ApprovalRequests
            .FirstOrDefaultAsync(ar => ar.Id == approvalRequestId, cancellationToken);

        if (approvalRequest == null || approvalRequest.Status != "Pending")
        {
            return false;
        }

        approvalRequest.Status = "Modified";
        approvalRequest.ApprovedByUserId = userId;
        approvalRequest.RespondedAt = DateTime.UtcNow;
        approvalRequest.FinalResponse = modifiedResponse;

        _logger.LogInformation(
            "Approval request {ApprovalRequestId} modified by user {UserId}. Original: {OriginalLength} chars, Modified: {ModifiedLength} chars",
            approvalRequestId, userId, approvalRequest.ProposedResponse.Length, modifiedResponse.Length);

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> RejectAsync(
        Guid approvalRequestId,
        Guid userId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var approvalRequest = await _context.ApprovalRequests
            .FirstOrDefaultAsync(ar => ar.Id == approvalRequestId, cancellationToken);

        if (approvalRequest == null || approvalRequest.Status != "Pending")
        {
            return false;
        }

        approvalRequest.Status = "Rejected";
        approvalRequest.ApprovedByUserId = userId;
        approvalRequest.RespondedAt = DateTime.UtcNow;
        approvalRequest.RejectionReason = reason;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Approval request {ApprovalRequestId} rejected by user {UserId} with reason: {Reason}",
            approvalRequestId, userId, reason);

        return true;
    }

    /// <inheritdoc />
    public async Task<List<ApprovalRequest>> GetPendingRequestsNeedingRemindersAsync(
        int reminderThresholdHours = 24,
        CancellationToken cancellationToken = default)
    {
        var cutoffTime = DateTime.UtcNow.AddHours(-reminderThresholdHours);

        return await _context.ApprovalRequests
            .Where(ar => ar.Status == "Pending" &&
                         ar.CreatedAt <= cutoffTime &&
                         ar.LastReminderSentAt == null)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<ApprovalRequest>> GetTimedOutRequestsAsync(
        int timeoutThresholdHours = 72,
        CancellationToken cancellationToken = default)
    {
        var cutoffTime = DateTime.UtcNow.AddHours(-timeoutThresholdHours);

        return await _context.ApprovalRequests
            .Where(ar => ar.Status == "Pending" &&
                         ar.CreatedAt <= cutoffTime)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> MarkReminderSentAsync(
        Guid approvalRequestId,
        CancellationToken cancellationToken = default)
    {
        var approvalRequest = await _context.ApprovalRequests
            .FirstOrDefaultAsync(ar => ar.Id == approvalRequestId, cancellationToken);

        if (approvalRequest == null || approvalRequest.Status != "Pending")
        {
            return false;
        }

        approvalRequest.LastReminderSentAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Reminder marked as sent for approval request {ApprovalRequestId}",
            approvalRequestId);

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> TimeoutRequestAsync(
        Guid approvalRequestId,
        CancellationToken cancellationToken = default)
    {
        var approvalRequest = await _context.ApprovalRequests
            .FirstOrDefaultAsync(ar => ar.Id == approvalRequestId, cancellationToken);

        if (approvalRequest == null || approvalRequest.Status != "Pending")
        {
            return false;
        }

        approvalRequest.Status = "TimedOut";
        approvalRequest.RespondedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Approval request {ApprovalRequestId} timed out after 72 hours",
            approvalRequestId);

        return true;
    }
}
