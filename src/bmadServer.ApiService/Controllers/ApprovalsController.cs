using bmadServer.ApiService.Agents;
using bmadServer.ApiService.Models;
using Microsoft.AspNetCore.Mvc;

namespace bmadServer.ApiService.Controllers;

/// <summary>
/// API controller for managing human approval requests for low-confidence agent decisions.
/// </summary>
[ApiController]
[Route("api/approvals")]
public class ApprovalsController : ControllerBase
{
    private readonly IApprovalService _approvalService;
    private readonly ILogger<ApprovalsController> _logger;

    public ApprovalsController(IApprovalService approvalService, ILogger<ApprovalsController> logger)
    {
        _approvalService = approvalService;
        _logger = logger;
    }

    /// <summary>
    /// Gets an approval request by ID.
    /// </summary>
    /// <param name="id">The approval request ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The approval request details.</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApprovalRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApprovalRequestDto>> GetApprovalRequest(
        Guid id,
        CancellationToken cancellationToken)
    {
        var approvalRequest = await _approvalService.GetApprovalRequestAsync(id, cancellationToken);

        if (approvalRequest == null)
        {
            return NotFound(new { message = "Approval request not found" });
        }

        var dto = new ApprovalRequestDto
        {
            Id = approvalRequest.Id,
            WorkflowInstanceId = approvalRequest.WorkflowInstanceId,
            AgentId = approvalRequest.AgentId,
            ProposedResponse = approvalRequest.ProposedResponse,
            ConfidenceScore = approvalRequest.ConfidenceScore,
            Reasoning = approvalRequest.Reasoning,
            Status = approvalRequest.Status,
            ApprovedByUserId = approvalRequest.ApprovedByUserId,
            CreatedAt = approvalRequest.CreatedAt,
            RespondedAt = approvalRequest.RespondedAt,
            FinalResponse = approvalRequest.FinalResponse,
            RejectionReason = approvalRequest.RejectionReason,
            LastReminderSentAt = approvalRequest.LastReminderSentAt
        };

        return Ok(dto);
    }

    /// <summary>
    /// Approves an approval request.
    /// </summary>
    /// <param name="id">The approval request ID.</param>
    /// <param name="request">The approval request data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success status.</returns>
    [HttpPost("{id}/approve")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Approve(
        Guid id,
        [FromBody] ApproveRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _approvalService.ApproveAsync(id, request.UserId, cancellationToken);

        if (!result)
        {
            return BadRequest(new { message = "Approval failed. Request may not exist or is not in pending state." });
        }

        return Ok(new { message = "Approval request approved successfully" });
    }

    /// <summary>
    /// Modifies an approval request.
    /// </summary>
    /// <param name="id">The approval request ID.</param>
    /// <param name="request">The modification request data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success status.</returns>
    [HttpPost("{id}/modify")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Modify(
        Guid id,
        [FromBody] ModifyRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _approvalService.ModifyAsync(
            id,
            request.UserId,
            request.ModifiedResponse,
            cancellationToken);

        if (!result)
        {
            return BadRequest(new { message = "Modification failed. Request may not exist or is not in pending state." });
        }

        return Ok(new { message = "Approval request modified successfully" });
    }

    /// <summary>
    /// Rejects an approval request.
    /// </summary>
    /// <param name="id">The approval request ID.</param>
    /// <param name="request">The rejection request data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success status.</returns>
    [HttpPost("{id}/reject")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Reject(
        Guid id,
        [FromBody] RejectRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _approvalService.RejectAsync(
            id,
            request.UserId,
            request.Reason,
            cancellationToken);

        if (!result)
        {
            return BadRequest(new { message = "Rejection failed. Request may not exist or is not in pending state." });
        }

        return Ok(new { message = "Approval request rejected successfully" });
    }

    /// <summary>
    /// Gets pending approval requests that need reminders.
    /// </summary>
    /// <param name="reminderThresholdHours">Hours since creation to trigger reminder (default: 24).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of approval requests needing reminders.</returns>
    [HttpGet("reminders")]
    [ProducesResponseType(typeof(List<ApprovalRequestDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ApprovalRequestDto>>> GetPendingReminders(
        [FromQuery] int reminderThresholdHours = 24,
        CancellationToken cancellationToken = default)
    {
        var requests = await _approvalService.GetPendingRequestsNeedingRemindersAsync(
            reminderThresholdHours,
            cancellationToken);

        var dtos = requests.Select(ar => new ApprovalRequestDto
        {
            Id = ar.Id,
            WorkflowInstanceId = ar.WorkflowInstanceId,
            AgentId = ar.AgentId,
            ProposedResponse = ar.ProposedResponse,
            ConfidenceScore = ar.ConfidenceScore,
            Reasoning = ar.Reasoning,
            Status = ar.Status,
            ApprovedByUserId = ar.ApprovedByUserId,
            CreatedAt = ar.CreatedAt,
            RespondedAt = ar.RespondedAt,
            FinalResponse = ar.FinalResponse,
            RejectionReason = ar.RejectionReason,
            LastReminderSentAt = ar.LastReminderSentAt
        }).ToList();

        return Ok(dtos);
    }

    /// <summary>
    /// Gets approval requests that have timed out.
    /// </summary>
    /// <param name="timeoutThresholdHours">Hours since creation to trigger timeout (default: 72).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of timed-out approval requests.</returns>
    [HttpGet("timeouts")]
    [ProducesResponseType(typeof(List<ApprovalRequestDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ApprovalRequestDto>>> GetTimedOutRequests(
        [FromQuery] int timeoutThresholdHours = 72,
        CancellationToken cancellationToken = default)
    {
        var requests = await _approvalService.GetTimedOutRequestsAsync(
            timeoutThresholdHours,
            cancellationToken);

        var dtos = requests.Select(ar => new ApprovalRequestDto
        {
            Id = ar.Id,
            WorkflowInstanceId = ar.WorkflowInstanceId,
            AgentId = ar.AgentId,
            ProposedResponse = ar.ProposedResponse,
            ConfidenceScore = ar.ConfidenceScore,
            Reasoning = ar.Reasoning,
            Status = ar.Status,
            ApprovedByUserId = ar.ApprovedByUserId,
            CreatedAt = ar.CreatedAt,
            RespondedAt = ar.RespondedAt,
            FinalResponse = ar.FinalResponse,
            RejectionReason = ar.RejectionReason,
            LastReminderSentAt = ar.LastReminderSentAt
        }).ToList();

        return Ok(dtos);
    }

    /// <summary>
    /// Marks that a reminder has been sent for an approval request.
    /// </summary>
    /// <param name="id">The approval request ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success status.</returns>
    [HttpPost("{id}/mark-reminder-sent")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkReminderSent(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _approvalService.MarkReminderSentAsync(id, cancellationToken);

        if (!result)
        {
            return NotFound(new { message = "Approval request not found or not in pending state" });
        }

        return Ok(new { message = "Reminder marked as sent" });
    }

    /// <summary>
    /// Times out an approval request.
    /// </summary>
    /// <param name="id">The approval request ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success status.</returns>
    [HttpPost("{id}/timeout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TimeoutRequest(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _approvalService.TimeoutRequestAsync(id, cancellationToken);

        if (!result)
        {
            return NotFound(new { message = "Approval request not found or not in pending state" });
        }

        return Ok(new { message = "Approval request timed out successfully" });
    }
}
