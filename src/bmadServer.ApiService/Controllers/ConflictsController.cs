using bmadServer.ApiService.Data;
using bmadServer.ApiService.Data.Entities;
using bmadServer.ApiService.DTOs;
using bmadServer.ApiService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace bmadServer.ApiService.Controllers;

[ApiController]
[Route("api/v1/workflows/{workflowId}/conflicts")]
[Authorize]
public class ConflictsController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IConflictResolutionService _resolutionService;
    private readonly IParticipantService _participantService;
    private readonly ILogger<ConflictsController> _logger;

    public ConflictsController(
        ApplicationDbContext dbContext,
        IConflictResolutionService resolutionService,
        IParticipantService participantService,
        ILogger<ConflictsController> logger)
    {
        _dbContext = dbContext;
        _resolutionService = resolutionService;
        _participantService = participantService;
        _logger = logger;
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
            ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    private async Task<bool> CanAccessWorkflowAsync(Guid workflowId)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty) return false;
        
        return await _participantService.IsParticipantAsync(workflowId, userId) 
            || await _participantService.IsWorkflowOwnerAsync(workflowId, userId);
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<ConflictDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<ConflictDto>>> GetConflicts(
        Guid workflowId,
        [FromQuery] string? status = null)
    {
        // Authorization check
        if (!await CanAccessWorkflowAsync(workflowId))
        {
            return Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Access Denied",
                detail: "You do not have permission to access this workflow's conflicts"
            );
        }

        var query = _dbContext.Conflicts
            .Where(c => c.WorkflowInstanceId == workflowId);

        if (!string.IsNullOrEmpty(status))
        {
            if (Enum.TryParse<ConflictStatus>(status, true, out var statusEnum))
            {
                query = query.Where(c => c.Status == statusEnum);
            }
        }

        var conflicts = await query.ToListAsync();

        var dtos = conflicts.Select(c => new ConflictDto
        {
            Id = c.Id,
            WorkflowInstanceId = c.WorkflowInstanceId,
            FieldName = c.FieldName,
            Type = c.Type.ToString(),
            Status = c.Status.ToString(),
            Inputs = c.GetInputs().Select(i => new ConflictInputDto
            {
                UserId = i.UserId,
                DisplayName = i.DisplayName,
                Value = i.Value,
                Timestamp = i.Timestamp
            }).ToList(),
            Resolution = c.GetResolution() == null ? null : new ConflictResolutionDto
            {
                ResolvedBy = c.GetResolution()!.ResolvedBy,
                ResolverDisplayName = c.GetResolution()!.ResolverDisplayName,
                Type = c.GetResolution()!.Type.ToString(),
                FinalValue = c.GetResolution()!.FinalValue,
                ResolvedAt = c.GetResolution()!.ResolvedAt,
                Reason = c.GetResolution()!.Reason
            },
            CreatedAt = c.CreatedAt,
            ExpiresAt = c.ExpiresAt,
            EscalatedAt = c.EscalatedAt
        }).ToList();

        return Ok(dtos);
    }

    [HttpGet("{conflictId}")]
    [ProducesResponseType(typeof(ConflictDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ConflictDto>> GetConflict(
        Guid workflowId,
        Guid conflictId)
    {
        // Authorization check
        if (!await CanAccessWorkflowAsync(workflowId))
        {
            return Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Access Denied",
                detail: "You do not have permission to access this workflow's conflicts"
            );
        }

        var conflict = await _dbContext.Conflicts
            .FirstOrDefaultAsync(c => c.Id == conflictId && c.WorkflowInstanceId == workflowId);

        if (conflict == null)
        {
            return NotFound();
        }

        var dto = new ConflictDto
        {
            Id = conflict.Id,
            WorkflowInstanceId = conflict.WorkflowInstanceId,
            FieldName = conflict.FieldName,
            Type = conflict.Type.ToString(),
            Status = conflict.Status.ToString(),
            Inputs = conflict.GetInputs().Select(i => new ConflictInputDto
            {
                UserId = i.UserId,
                DisplayName = i.DisplayName,
                Value = i.Value,
                Timestamp = i.Timestamp
            }).ToList(),
            Resolution = conflict.GetResolution() == null ? null : new ConflictResolutionDto
            {
                ResolvedBy = conflict.GetResolution()!.ResolvedBy,
                ResolverDisplayName = conflict.GetResolution()!.ResolverDisplayName,
                Type = conflict.GetResolution()!.Type.ToString(),
                FinalValue = conflict.GetResolution()!.FinalValue,
                ResolvedAt = conflict.GetResolution()!.ResolvedAt,
                Reason = conflict.GetResolution()!.Reason
            },
            CreatedAt = conflict.CreatedAt,
            ExpiresAt = conflict.ExpiresAt,
            EscalatedAt = conflict.EscalatedAt
        };

        return Ok(dto);
    }

    [HttpPost("{conflictId}/resolve")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResolveConflict(
        Guid workflowId,
        Guid conflictId,
        [FromBody] ResolveConflictRequest request)
    {
        // Authorization check: only participants can resolve
        if (!await CanAccessWorkflowAsync(workflowId))
        {
            return Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Access Denied",
                detail: "You do not have permission to resolve conflicts for this workflow"
            );
        }

        if (!Enum.TryParse<ResolutionType>(request.ResolutionType, true, out var resolutionType))
        {
            return BadRequest("Invalid resolution type");
        }

        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Unauthorized",
                detail: "User ID not found in claims"
            );
        }

        var displayName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";

        var success = await _resolutionService.ResolveConflictAsync(
            conflictId,
            userId,
            displayName,
            resolutionType,
            request.FinalValue,
            request.Reason);

        if (!success)
        {
            return NotFound();
        }

        return Ok();
    }
}
