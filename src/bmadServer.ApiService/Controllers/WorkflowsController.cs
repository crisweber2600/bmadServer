using bmadServer.ApiService.Hubs;
using bmadServer.ApiService.Models.Workflows;
using bmadServer.ApiService.Services.Workflows;
using bmadServer.ServiceDefaults.Services.Workflows;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace bmadServer.ApiService.Controllers;

/// <summary>
/// Controller for workflow orchestration operations
/// </summary>
[ApiController]
[Route("api/v1/workflows")]
[Authorize]
public class WorkflowsController : ControllerBase
{
    private readonly IWorkflowInstanceService _workflowInstanceService;
    private readonly IWorkflowRegistry _workflowRegistry;
    private readonly IStepExecutor _stepExecutor;
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly ILogger<WorkflowsController> _logger;

    public WorkflowsController(
        IWorkflowInstanceService workflowInstanceService,
        IWorkflowRegistry workflowRegistry,
        IStepExecutor stepExecutor,
        IHubContext<ChatHub> hubContext,
        ILogger<WorkflowsController> logger)
    {
        _workflowInstanceService = workflowInstanceService;
        _workflowRegistry = workflowRegistry;
        _stepExecutor = stepExecutor;
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <summary>
    /// Get list of workflow instances for the authenticated user
    /// </summary>
    /// <param name="showCancelled">Include cancelled workflows in results (default: false)</param>
    /// <returns>List of workflow instances</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<WorkflowInstanceListItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<WorkflowInstanceListItem>>> GetWorkflows([FromQuery] bool showCancelled = false)
    {
        try
        {
            // Get user ID from claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Problem(
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: "Unauthorized",
                    detail: "User ID not found in token");
            }

            var workflows = await _workflowInstanceService.GetWorkflowInstancesAsync(userId, showCancelled);

            // Map to list items with display metadata
            var listItems = workflows.Select(w => new WorkflowInstanceListItem
            {
                Id = w.Id,
                WorkflowDefinitionId = w.WorkflowDefinitionId,
                Status = w.Status,
                CurrentStep = w.CurrentStep,
                CreatedAt = w.CreatedAt,
                UpdatedAt = w.UpdatedAt,
                PausedAt = w.PausedAt,
                CancelledAt = w.CancelledAt,
                IsCancelled = w.Status == WorkflowStatus.Cancelled,
                IsTerminal = w.Status.IsTerminal()
            }).ToList();

            _logger.LogInformation(
                "Retrieved {Count} workflows for user {UserId}",
                listItems.Count, userId);

            return Ok(listItems);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving workflows for user");
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error",
                detail: ex.Message);
        }
    }

    /// <summary>
    /// Create a new workflow instance
    /// </summary>
    /// <param name="request">Workflow creation request</param>
    /// <returns>Created workflow instance</returns>
    [HttpPost]
    [ProducesResponseType(typeof(WorkflowInstance), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<WorkflowInstance>> CreateWorkflow([FromBody] CreateWorkflowRequest request)
    {
        try
        {
            // Get user ID from claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Problem(
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: "Unauthorized",
                    detail: "User ID not found in token");
            }

            // Validate workflow exists
            if (!_workflowRegistry.ValidateWorkflow(request.WorkflowId))
            {
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid Workflow",
                    detail: $"Workflow '{request.WorkflowId}' not found");
            }

            // Create workflow instance
            var instance = await _workflowInstanceService.CreateWorkflowInstanceAsync(
                request.WorkflowId,
                userId,
                request.InitialContext ?? new Dictionary<string, object>());

            _logger.LogInformation(
                "Created workflow instance {InstanceId} for workflow {WorkflowId}",
                instance.Id, request.WorkflowId);

            return CreatedAtAction(
                nameof(GetWorkflow),
                new { id = instance.Id },
                instance);
        }
        catch (ArgumentException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad Request",
                detail: ex.Message);
        }
    }

    /// <summary>
    /// Get workflow instance by ID
    /// </summary>
    /// <param name="id">Workflow instance ID</param>
    /// <returns>Workflow instance</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(WorkflowInstance), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WorkflowInstance>> GetWorkflow(Guid id)
    {
        var instance = await _workflowInstanceService.GetWorkflowInstanceAsync(id);
        if (instance == null)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found",
                detail: $"Workflow instance '{id}' not found");
        }

        return Ok(instance);
    }

    /// <summary>
    /// Start a workflow instance
    /// </summary>
    /// <param name="id">Workflow instance ID</param>
    /// <returns>No content on success</returns>
    [HttpPost("{id}/start")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> StartWorkflow(Guid id)
    {
        var success = await _workflowInstanceService.StartWorkflowAsync(id);
        if (!success)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad Request",
                detail: "Unable to start workflow. Workflow may not exist or transition is invalid");
        }

        return NoContent();
    }

    /// <summary>
    /// Execute the current step of a workflow instance
    /// </summary>
    /// <param name="id">Workflow instance ID</param>
    /// <param name="request">Step execution request</param>
    /// <returns>Step execution result</returns>
    [HttpPost("{id}/steps/execute")]
    [ProducesResponseType(typeof(StepExecutionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StepExecutionResult>> ExecuteStep(
        Guid id, 
        [FromBody] ExecuteStepRequest? request = null)
    {
        try
        {
            var result = await _stepExecutor.ExecuteStepAsync(
                id, 
                request?.UserInput);

            if (!result.Success)
            {
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Step Execution Failed",
                    detail: result.ErrorMessage ?? "Step execution failed");
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing step for workflow {InstanceId}", id);
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error",
                detail: ex.Message);
        }
    }

    /// <summary>
    /// Pause a running workflow
    /// </summary>
    /// <param name="id">Workflow instance ID</param>
    /// <returns>OK with workflow state or error</returns>
    [HttpPost("{id}/pause")]
    [ProducesResponseType(typeof(WorkflowInstance), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<WorkflowInstance>> PauseWorkflow(Guid id)
    {
        try
        {
            // Get user ID from claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Problem(
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: "Unauthorized",
                    detail: "User ID not found in token");
            }

            // Attempt to pause the workflow
            var (success, message) = await _workflowInstanceService.PauseWorkflowAsync(id, userId);
            
            if (!success)
            {
                // Check if it's a "not found" error or validation error
                var instance = await _workflowInstanceService.GetWorkflowInstanceAsync(id);
                if (instance == null)
                {
                    return Problem(
                        statusCode: StatusCodes.Status404NotFound,
                        title: "Not Found",
                        detail: message ?? $"Workflow instance '{id}' not found");
                }

                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Bad Request",
                    detail: message ?? "Unable to pause workflow");
            }

            // Get updated workflow instance
            var updatedInstance = await _workflowInstanceService.GetWorkflowInstanceAsync(id);
            
            // Send SignalR notification to all workflow participants
            await _hubContext.Clients.All.SendAsync("WORKFLOW_PAUSED", new
            {
                eventType = "WORKFLOW_PAUSED",
                workflowId = id,
                status = "Paused",
                userId = userId,
                timestamp = DateTime.UtcNow
            });

            _logger.LogInformation(
                "Workflow {InstanceId} paused by user {UserId}",
                id, userId);

            return Ok(updatedInstance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pausing workflow {InstanceId}", id);
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error",
                detail: ex.Message);
        }
    }

    /// <summary>
    /// Resume a paused workflow
    /// </summary>
    /// <param name="id">Workflow instance ID</param>
    /// <returns>OK with workflow state or error</returns>
    [HttpPost("{id}/resume")]
    [ProducesResponseType(typeof(ResumeWorkflowResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ResumeWorkflowResponse>> ResumeWorkflow(Guid id)
    {
        try
        {
            // Get user ID from claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Problem(
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: "Unauthorized",
                    detail: "User ID not found in token");
            }

            // Attempt to resume the workflow
            var (success, message) = await _workflowInstanceService.ResumeWorkflowAsync(id, userId);
            
            if (!success)
            {
                // Check if it's a "not found" error or validation error
                var instance = await _workflowInstanceService.GetWorkflowInstanceAsync(id);
                if (instance == null)
                {
                    return Problem(
                        statusCode: StatusCodes.Status404NotFound,
                        title: "Not Found",
                        detail: message ?? $"Workflow instance '{id}' not found");
                }

                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Bad Request",
                    detail: message ?? "Unable to resume workflow");
            }

            // Get updated workflow instance
            var updatedInstance = await _workflowInstanceService.GetWorkflowInstanceAsync(id);
            
            // Send SignalR notification to all workflow participants
            await _hubContext.Clients.All.SendAsync("WORKFLOW_RESUMED", new
            {
                eventType = "WORKFLOW_RESUMED",
                workflowId = id,
                status = "Running",
                userId = userId,
                timestamp = DateTime.UtcNow
            });

            _logger.LogInformation(
                "Workflow {InstanceId} resumed by user {UserId}",
                id, userId);

            return Ok(new ResumeWorkflowResponse
            {
                Workflow = updatedInstance!,
                Message = message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resuming workflow {InstanceId}", id);
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error",
                detail: ex.Message);
        }
    }

    /// <summary>
    /// Cancel a workflow
    /// </summary>
    /// <param name="id">Workflow instance ID</param>
    /// <returns>OK with workflow state or error</returns>
    [HttpPost("{id}/cancel")]
    [ProducesResponseType(typeof(WorkflowInstance), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<WorkflowInstance>> CancelWorkflow(Guid id)
    {
        try
        {
            // Get user ID from claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Problem(
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: "Unauthorized",
                    detail: "User ID not found in token");
            }

            // Attempt to cancel the workflow
            var (success, message) = await _workflowInstanceService.CancelWorkflowAsync(id, userId);
            
            if (!success)
            {
                // Check if it's a "not found" error or validation error
                var instance = await _workflowInstanceService.GetWorkflowInstanceAsync(id);
                if (instance == null)
                {
                    return Problem(
                        statusCode: StatusCodes.Status404NotFound,
                        title: "Not Found",
                        detail: message ?? $"Workflow instance '{id}' not found");
                }

                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Bad Request",
                    detail: message ?? "Unable to cancel workflow");
            }

            // Get updated workflow instance
            var updatedInstance = await _workflowInstanceService.GetWorkflowInstanceAsync(id);
            
            // Send SignalR notification to all workflow participants
            await _hubContext.Clients.All.SendAsync("WORKFLOW_CANCELLED", new
            {
                eventType = "WORKFLOW_CANCELLED",
                workflowId = id,
                status = "Cancelled",
                userId = userId,
                timestamp = DateTime.UtcNow
            });

            _logger.LogInformation(
                "Workflow {InstanceId} cancelled by user {UserId}",
                id, userId);

            return Ok(updatedInstance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling workflow {InstanceId}", id);
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error",
                detail: ex.Message);
        }
    }
}

/// <summary>
/// Request model for creating a workflow
/// </summary>
public class CreateWorkflowRequest
{
    /// <summary>
    /// The workflow definition ID (e.g., "create-prd")
    /// </summary>
    public required string WorkflowId { get; set; }

    /// <summary>
    /// Initial context data for the workflow
    /// </summary>
    public Dictionary<string, object>? InitialContext { get; set; }
}

/// <summary>
/// Request model for executing a workflow step
/// </summary>
public class ExecuteStepRequest
{
    /// <summary>
    /// Optional user input for the step
    /// </summary>
    public string? UserInput { get; set; }
}

/// <summary>
/// Response model for resuming a workflow
/// </summary>
public class ResumeWorkflowResponse
{
    /// <summary>
    /// The updated workflow instance
    /// </summary>
    public required WorkflowInstance Workflow { get; set; }

    /// <summary>
    /// Optional message (e.g., context refresh notification)
    /// </summary>
    public string? Message { get; set; }
}

/// <summary>
/// Response model for workflow list items with display metadata
/// </summary>
public class WorkflowInstanceListItem
{
    /// <summary>
    /// Workflow instance ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Workflow definition ID
    /// </summary>
    public required string WorkflowDefinitionId { get; set; }

    /// <summary>
    /// Current workflow status
    /// </summary>
    public WorkflowStatus Status { get; set; }

    /// <summary>
    /// Current step number (1-based)
    /// </summary>
    public int CurrentStep { get; set; }

    /// <summary>
    /// When the workflow was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the workflow was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// When the workflow was paused (if applicable)
    /// </summary>
    public DateTime? PausedAt { get; set; }

    /// <summary>
    /// When the workflow was cancelled (if applicable)
    /// </summary>
    public DateTime? CancelledAt { get; set; }

    /// <summary>
    /// Whether the workflow is cancelled (for UI display)
    /// </summary>
    public bool IsCancelled { get; set; }

    /// <summary>
    /// Whether the workflow is in a terminal state (Completed, Failed, or Cancelled)
    /// </summary>
    public bool IsTerminal { get; set; }
}
