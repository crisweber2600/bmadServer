using bmadServer.ApiService.DTOs;
using bmadServer.ApiService.Hubs;
using bmadServer.ApiService.Models.Workflows;
using bmadServer.ApiService.Services;
using bmadServer.ApiService.Services.Workflows;
using bmadServer.ApiService.Services.Workflows.Agents;
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
    private readonly IAgentRegistry _agentRegistry;
    private readonly IStepExecutor _stepExecutor;
    private readonly IApprovalService _approvalService;
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly ILogger<WorkflowsController> _logger;
    private readonly IParticipantService _participantService;

    public WorkflowsController(
        IWorkflowInstanceService workflowInstanceService,
        IWorkflowRegistry workflowRegistry,
        IAgentRegistry agentRegistry,
        IStepExecutor stepExecutor,
        IApprovalService approvalService,
        IHubContext<ChatHub> hubContext,
        ILogger<WorkflowsController> logger,
        IParticipantService participantService)
    {
        _workflowInstanceService = workflowInstanceService;
        _workflowRegistry = workflowRegistry;
        _agentRegistry = agentRegistry;
        _stepExecutor = stepExecutor;
        _approvalService = approvalService;
        _hubContext = hubContext;
        _logger = logger;
        _participantService = participantService;
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("User ID not found in claims");
        }
        return userId;
    }

    private async Task<bool> CanAccessWorkflowAsync(Guid workflowId, Guid userId)
    {
        var isParticipant = await _participantService.IsParticipantAsync(workflowId, userId);
        var isOwner = await _participantService.IsWorkflowOwnerAsync(workflowId, userId);
        return isParticipant || isOwner;
    }

    private async Task SendWorkflowStatusChangedNotification(Guid workflowId)
    {
        var status = await _workflowInstanceService.GetWorkflowStatusAsync(workflowId);
        if (status != null)
        {
            // Send to workflow group only, not all clients
            await _hubContext.Clients.Group($"workflow-{workflowId}").SendAsync("WORKFLOW_STATUS_CHANGED", new
            {
                eventType = "WORKFLOW_STATUS_CHANGED",
                workflowId = workflowId,
                status = status,
                timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Get list of workflow instances for the authenticated user
    /// </summary>
    /// <param name="showCancelled">Include cancelled workflows in results (default: false)</param>
    /// <param name="status">Filter by workflow status</param>
    /// <param name="workflowType">Filter by workflow definition ID</param>
    /// <param name="createdAfter">Filter workflows created after this date</param>
    /// <param name="createdBefore">Filter workflows created before this date</param>
    /// <param name="page">Page number (1-based, default: 1)</param>
    /// <param name="pageSize">Number of items per page (default: 20, max: 100)</param>
    /// <returns>Paginated list of workflow instances</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<WorkflowInstanceListItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResult<WorkflowInstanceListItem>>> GetWorkflows(
        [FromQuery] bool showCancelled = false,
        [FromQuery] WorkflowStatus? status = null,
        [FromQuery] string? workflowType = null,
        [FromQuery] DateTime? createdAfter = null,
        [FromQuery] DateTime? createdBefore = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
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

            // Use filtered query if any filters are provided
            if (status.HasValue || !string.IsNullOrEmpty(workflowType) || 
                createdAfter.HasValue || createdBefore.HasValue || page > 1 || pageSize != 20)
            {
                var pagedResult = await _workflowInstanceService.GetFilteredWorkflowsAsync(
                    userId, status, workflowType, createdAfter, createdBefore, page, pageSize);

                // Map to list items with display metadata
                var pagedListItems = new PagedResult<WorkflowInstanceListItem>
                {
                    Items = pagedResult.Items.Select(w => new WorkflowInstanceListItem
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
                    }).ToList(),
                    Page = pagedResult.Page,
                    PageSize = pagedResult.PageSize,
                    TotalItems = pagedResult.TotalItems,
                    TotalPages = pagedResult.TotalPages,
                    HasPrevious = pagedResult.HasPrevious,
                    HasNext = pagedResult.HasNext
                };

                _logger.LogInformation(
                    "Retrieved paginated workflows (page {Page}/{TotalPages}, {Count} items) for user {UserId}",
                    pagedListItems.Page, pagedListItems.TotalPages, pagedListItems.Items.Count, userId);

                return Ok(pagedListItems);
            }

            // Legacy behavior for backward compatibility
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

            // Return as paged result for consistency
            var result = new PagedResult<WorkflowInstanceListItem>
            {
                Items = listItems,
                Page = 1,
                PageSize = listItems.Count,
                TotalItems = listItems.Count,
                TotalPages = 1,
                HasPrevious = false,
                HasNext = false
            };

            _logger.LogInformation(
                "Retrieved {Count} workflows for user {UserId}",
                listItems.Count, userId);

            return Ok(result);
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
    /// Get workflow instance status with detailed progress information
    /// </summary>
    /// <param name="id">Workflow instance ID</param>
    /// <returns>Workflow status with step progress</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(WorkflowStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WorkflowStatusResponse>> GetWorkflow(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!await CanAccessWorkflowAsync(id, userId))
            {
                return Problem(
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "Forbidden",
                    detail: "You don't have access to this workflow");
            }
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }

        var status = await _workflowInstanceService.GetWorkflowStatusAsync(id);
        if (status == null)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found",
                detail: $"Workflow instance '{id}' not found");
        }

        return Ok(status);
    }

    /// <summary>
    /// Start a workflow instance
    /// </summary>
    /// <param name="id">Workflow instance ID</param>
    /// <returns>No content on success</returns>
    [HttpPost("{id}/start")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> StartWorkflow(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            // Only owner can start workflows
            if (!await _participantService.IsWorkflowOwnerAsync(id, userId))
            {
                return Problem(
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "Forbidden",
                    detail: "Only the workflow owner can start it");
            }
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }

        var success = await _workflowInstanceService.StartWorkflowAsync(id);
        if (!success)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad Request",
                detail: "Unable to start workflow. Workflow may not exist or transition is invalid");
        }

        // Send status change notification
        await SendWorkflowStatusChangedNotification(id);

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
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StepExecutionResult>> ExecuteStep(
        Guid id, 
        [FromBody] ExecuteStepRequest? request = null)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!await CanAccessWorkflowAsync(id, userId))
            {
                return Problem(
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "Forbidden",
                    detail: "You don't have access to this workflow");
            }

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
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
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
            
            // Send SignalR notification with full status
            await SendWorkflowStatusChangedNotification(id);

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
            
            // Send SignalR notification with full status
            await SendWorkflowStatusChangedNotification(id);

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
            
            // Send SignalR notification with full status
            await SendWorkflowStatusChangedNotification(id);

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

    /// <summary>
    /// Skip the current workflow step (only for optional steps)
    /// </summary>
    /// <param name="id">Workflow instance ID</param>
    /// <param name="request">Skip request with optional reason</param>
    /// <returns>OK with updated workflow or error</returns>
    [HttpPost("{id}/steps/current/skip")]
    [ProducesResponseType(typeof(WorkflowInstance), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<WorkflowInstance>> SkipCurrentStep(Guid id, [FromBody] SkipStepRequest? request = null)
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

            // Attempt to skip the current step
            var (success, message) = await _workflowInstanceService.SkipCurrentStepAsync(
                id, 
                userId, 
                request?.Reason);
            
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
                    detail: message ?? "Unable to skip step");
            }

            // Get updated workflow instance
            var updatedInstance = await _workflowInstanceService.GetWorkflowInstanceAsync(id);

            // Send status change notification
            await SendWorkflowStatusChangedNotification(id);

            _logger.LogInformation(
                "Step skipped for workflow {InstanceId} by user {UserId}",
                id, userId);

            return Ok(updatedInstance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error skipping step for workflow {InstanceId}", id);
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error",
                detail: ex.Message);
        }
    }

    /// <summary>
    /// Go to a specific step in the workflow (step must be in history)
    /// </summary>
    /// <param name="id">Workflow instance ID</param>
    /// <param name="stepId">Step ID to navigate to</param>
    /// <returns>OK with updated workflow or error</returns>
    [HttpPost("{id}/steps/{stepId}/goto")]
    [ProducesResponseType(typeof(WorkflowInstance), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<WorkflowInstance>> GoToStep(Guid id, string stepId)
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

            // Attempt to navigate to the step
            var (success, message) = await _workflowInstanceService.GoToStepAsync(id, stepId, userId);
            
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
                    detail: message ?? "Unable to navigate to step");
            }

            // Get updated workflow instance
            var updatedInstance = await _workflowInstanceService.GetWorkflowInstanceAsync(id);

            // Send status change notification
            await SendWorkflowStatusChangedNotification(id);

            _logger.LogInformation(
                "Navigated to step {StepId} for workflow {InstanceId} by user {UserId}",
                stepId, id, userId);

            return Ok(updatedInstance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error navigating to step {StepId} for workflow {InstanceId}", stepId, id);
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error",
                detail: ex.Message);
        }
    }

    /// <summary>
    /// Add a participant to a workflow
    /// </summary>
    /// <param name="id">Workflow instance ID</param>
    /// <param name="request">Participant details</param>
    /// <returns>Created participant</returns>
    [HttpPost("{id}/participants")]
    [ProducesResponseType(typeof(ParticipantResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ParticipantResponse>> AddParticipant(
        Guid id, 
        [FromBody] AddParticipantRequest request)
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

            // Check if user is workflow owner
            var isOwner = await _participantService.IsWorkflowOwnerAsync(id, userId);
            if (!isOwner)
            {
                return Problem(
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "Forbidden",
                    detail: "Only workflow owner can add participants");
            }

            // Parse role
            if (!Enum.TryParse<ParticipantRole>(request.Role, out var role))
            {
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid Role",
                    detail: "Role must be one of: Owner, Contributor, Observer");
            }

            // Add participant
            var participant = await _participantService.AddParticipantAsync(
                id, 
                request.UserId, 
                role, 
                userId);

            // Send notification via SignalR
            await _hubContext.Clients.Group($"workflow-{id}").SendAsync("PARTICIPANT_ADDED", new
            {
                eventType = "PARTICIPANT_ADDED",
                workflowId = id,
                participantId = participant.Id,
                userId = participant.UserId,
                role = participant.Role.ToString(),
                timestamp = DateTime.UtcNow
            });

            var response = new ParticipantResponse
            {
                Id = participant.Id,
                WorkflowId = participant.WorkflowId,
                UserId = participant.UserId,
                UserDisplayName = participant.User?.DisplayName ?? "",
                UserEmail = participant.User?.Email ?? "",
                Role = participant.Role.ToString(),
                AddedAt = participant.AddedAt,
                AddedBy = participant.AddedBy
            };

            _logger.LogInformation(
                "Added participant {UserId} to workflow {WorkflowId} with role {Role}",
                request.UserId, id, role);

            return CreatedAtAction(
                nameof(GetParticipants),
                new { id },
                response);
        }
        catch (InvalidOperationException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad Request",
                detail: ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding participant to workflow {WorkflowId}", id);
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error",
                detail: ex.Message);
        }
    }

    /// <summary>
    /// Get all participants for a workflow
    /// </summary>
    /// <param name="id">Workflow instance ID</param>
    /// <returns>List of participants</returns>
    [HttpGet("{id}/participants")]
    [ProducesResponseType(typeof(List<ParticipantResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<ParticipantResponse>>> GetParticipants(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            
            // Authorization check: only participants or owners can see participant list
            if (!await CanAccessWorkflowAsync(id, userId))
            {
                _logger.LogWarning("User {UserId} attempted to access participants for workflow {WorkflowId} without access",
                    userId, id);
                return Forbid();
            }

            var participants = await _participantService.GetParticipantsAsync(id);

            var responses = participants.Select(p => new ParticipantResponse
            {
                Id = p.Id,
                WorkflowId = p.WorkflowId,
                UserId = p.UserId,
                UserDisplayName = p.User?.DisplayName ?? "",
                UserEmail = p.User?.Email ?? "",
                Role = p.Role.ToString(),
                AddedAt = p.AddedAt,
                AddedBy = p.AddedBy
            }).ToList();

            return Ok(responses);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving participants for workflow {WorkflowId}", id);
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error",
                detail: ex.Message);
        }
    }

    /// <summary>
    /// Remove a participant from a workflow
    /// </summary>
    /// <param name="id">Workflow instance ID</param>
    /// <param name="userId">User ID to remove</param>
    /// <returns>No content on success</returns>
    [HttpDelete("{id}/participants/{userId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveParticipant(Guid id, Guid userId)
    {
        try
        {
            // Get user ID from claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var currentUserId))
            {
                return Problem(
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: "Unauthorized",
                    detail: "User ID not found in token");
            }

            // Check if user is workflow owner
            var isOwner = await _participantService.IsWorkflowOwnerAsync(id, currentUserId);
            if (!isOwner)
            {
                return Problem(
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "Forbidden",
                    detail: "Only workflow owner can remove participants");
            }

            // Remove participant
            var result = await _participantService.RemoveParticipantAsync(id, userId);
            if (!result)
            {
                return Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Not Found",
                    detail: $"Participant {userId} not found in workflow {id}");
            }

            // Send notification via SignalR
            await _hubContext.Clients.Group($"workflow-{id}").SendAsync("PARTICIPANT_REMOVED", new
            {
                eventType = "PARTICIPANT_REMOVED",
                workflowId = id,
                userId,
                timestamp = DateTime.UtcNow
            });

            _logger.LogInformation(
                "Removed participant {UserId} from workflow {WorkflowId}",
                userId, id);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing participant from workflow {WorkflowId}", id);
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error",
                detail: ex.Message);
        }
    }

    /// <summary>
    /// Get contribution metrics for a workflow (Story 7.3 AC#4)
    /// </summary>
    /// <param name="id">Workflow ID</param>
    /// <returns>Per-user contribution metrics</returns>
    /// <response code="200">Returns contribution metrics</response>
    /// <response code="403">User is not a participant</response>
    /// <response code="404">Workflow not found</response>
    [HttpGet("{id}/contributions")]
    [ProducesResponseType(typeof(ContributionMetricsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetContributions(Guid id, [FromServices] IContributionMetricsService contributionMetricsService)
    {
        try
        {
            // Get current user ID
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            // Verify workflow exists
            var workflowInstance = await _workflowInstanceService.GetWorkflowInstanceAsync(id);
            if (workflowInstance == null)
            {
                return NotFound(new ProblemDetails
                {
                    Type = "https://bmadserver.api/errors/workflow-not-found",
                    Title = "Workflow Not Found",
                    Status = StatusCodes.Status404NotFound,
                    Detail = $"Workflow {id} does not exist"
                });
            }

            // Verify user is owner or participant
            var isParticipant = await _participantService.IsParticipantAsync(id, userId);
            if (!isParticipant && workflowInstance.UserId != userId)
            {
                return Problem(
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "Access Denied",
                    detail: "You must be a participant or owner to view contribution metrics",
                    type: "https://bmadserver.api/errors/access-denied");
            }

            // Get contribution metrics
            var metrics = await contributionMetricsService.GetContributionMetricsAsync(id);
            
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting contribution metrics for workflow {WorkflowId}", id);
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
/// Request model for skipping a workflow step
/// </summary>
public class SkipStepRequest
{
    /// <summary>
    /// Optional reason for skipping the step
    /// </summary>
    public string? Reason { get; set; }
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
