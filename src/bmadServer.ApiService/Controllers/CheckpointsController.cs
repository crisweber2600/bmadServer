using bmadServer.ApiService.DTOs.Checkpoints;
using bmadServer.ApiService.Models.Workflows;
using bmadServer.ApiService.Services;
using bmadServer.ApiService.Services.Checkpoints;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace bmadServer.ApiService.Controllers;

[ApiController]
[Route("api/v1/workflows/{workflowId:guid}/checkpoints")]
[Authorize]
public class CheckpointsController : ControllerBase
{
    private readonly ICheckpointService _checkpointService;
    private readonly IInputQueueService _inputQueueService;
    private readonly IParticipantService _participantService;
    private readonly ILogger<CheckpointsController> _logger;

    public CheckpointsController(
        ICheckpointService checkpointService,
        IInputQueueService inputQueueService,
        IParticipantService participantService,
        ILogger<CheckpointsController> logger)
    {
        _checkpointService = checkpointService;
        _inputQueueService = inputQueueService;
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

    /// <summary>
    /// Get all checkpoints for a workflow
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<CheckpointResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<CheckpointResponse>>> GetCheckpoints(
        [FromRoute] Guid workflowId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        // Authorization check: user must be participant or owner
        if (!await CanAccessWorkflowAsync(workflowId))
        {
            return Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Access Denied",
                detail: "You do not have permission to access this workflow's checkpoints"
            );
        }

        try
        {
            var result = await _checkpointService.GetCheckpointsAsync(workflowId, page, pageSize, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving checkpoints for workflow {WorkflowId}", workflowId);
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Error Retrieving Checkpoints",
                detail: ex.Message,
                type: "https://bmadserver.api/errors/checkpoint-retrieval-failed"
            );
        }
    }

    /// <summary>
    /// Get a specific checkpoint by ID
    /// </summary>
    [HttpGet("{checkpointId:guid}")]
    [ProducesResponseType(typeof(WorkflowCheckpoint), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WorkflowCheckpoint>> GetCheckpoint(
        [FromRoute] Guid workflowId,
        [FromRoute] Guid checkpointId,
        CancellationToken cancellationToken = default)
    {
        // Authorization check
        if (!await CanAccessWorkflowAsync(workflowId))
        {
            return Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Access Denied",
                detail: "You do not have permission to access this workflow's checkpoints"
            );
        }

        var checkpoint = await _checkpointService.GetCheckpointByIdAsync(checkpointId, cancellationToken);
        
        if (checkpoint == null || checkpoint.WorkflowId != workflowId)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Checkpoint Not Found",
                detail: $"Checkpoint {checkpointId} does not exist for workflow {workflowId}",
                type: "https://bmadserver.api/errors/checkpoint-not-found"
            );
        }

        return Ok(checkpoint);
    }

    /// <summary>
    /// Create an explicit checkpoint (manual save point)
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(WorkflowCheckpoint), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<WorkflowCheckpoint>> CreateCheckpoint(
        [FromRoute] Guid workflowId,
        [FromBody] CreateCheckpointRequest? request,
        CancellationToken cancellationToken = default)
    {
        // Authorization check
        if (!await CanAccessWorkflowAsync(workflowId))
        {
            return Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Access Denied",
                detail: "You do not have permission to create checkpoints for this workflow"
            );
        }

        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Unauthorized",
                detail: "User ID not found in claims",
                type: "https://bmadserver.api/errors/unauthorized"
            );
        }

        try
        {
            var stepId = request?.StepId ?? "manual-checkpoint";
            var checkpoint = await _checkpointService.CreateCheckpointAsync(
                workflowId, 
                stepId, 
                CheckpointType.ExplicitSave, 
                userId, 
                cancellationToken);

            return CreatedAtAction(
                nameof(GetCheckpoint),
                new { workflowId, checkpointId = checkpoint.Id },
                checkpoint);
        }
        catch (InvalidOperationException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Workflow Not Found",
                detail: ex.Message,
                type: "https://bmadserver.api/errors/workflow-not-found"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating checkpoint for workflow {WorkflowId}", workflowId);
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Error Creating Checkpoint",
                detail: ex.Message,
                type: "https://bmadserver.api/errors/checkpoint-creation-failed"
            );
        }
    }

    /// <summary>
    /// Restore workflow to a specific checkpoint
    /// </summary>
    [HttpPost("{checkpointId:guid}/restore")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RestoreCheckpoint(
        [FromRoute] Guid workflowId,
        [FromRoute] Guid checkpointId,
        CancellationToken cancellationToken = default)
    {
        // Authorization check: only owner can restore
        var userId = GetCurrentUserId();
        if (!await _participantService.IsWorkflowOwnerAsync(workflowId, userId))
        {
            return Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Access Denied",
                detail: "Only the workflow owner can restore checkpoints"
            );
        }

        try
        {
            await _checkpointService.RestoreCheckpointAsync(workflowId, checkpointId, cancellationToken);
            return Ok(new { message = "Workflow restored to checkpoint successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Checkpoint Not Found",
                detail: ex.Message,
                type: "https://bmadserver.api/errors/checkpoint-not-found"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring checkpoint {CheckpointId} for workflow {WorkflowId}", 
                checkpointId, workflowId);
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Error Restoring Checkpoint",
                detail: ex.Message,
                type: "https://bmadserver.api/errors/checkpoint-restore-failed"
            );
        }
    }

    /// <summary>
    /// Queue an input for processing at the next checkpoint
    /// </summary>
    [HttpPost("~/api/v1/workflows/{workflowId:guid}/inputs/queue")]
    [ProducesResponseType(typeof(QueuedInput), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<QueuedInput>> QueueInput(
        [FromRoute] Guid workflowId,
        [FromBody] QueueInputRequest request,
        CancellationToken cancellationToken = default)
    {
        // Authorization check
        if (!await CanAccessWorkflowAsync(workflowId))
        {
            return Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Access Denied",
                detail: "You do not have permission to queue inputs for this workflow"
            );
        }

        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Unauthorized",
                detail: "User ID not found in claims",
                type: "https://bmadserver.api/errors/unauthorized"
            );
        }

        try
        {
            var queuedInput = await _inputQueueService.EnqueueInputAsync(
                workflowId, 
                userId, 
                request.InputType, 
                request.Content, 
                cancellationToken);

            return Created($"/api/v1/workflows/{workflowId}/inputs/{queuedInput.Id}", queuedInput);
        }
        catch (InvalidOperationException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Workflow Not Found",
                detail: ex.Message,
                type: "https://bmadserver.api/errors/workflow-not-found"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error queuing input for workflow {WorkflowId}", workflowId);
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Error Queuing Input",
                detail: ex.Message,
                type: "https://bmadserver.api/errors/input-queue-failed"
            );
        }
    }
}

public record CreateCheckpointRequest(string? StepId);
