using bmadServer.ApiService.DTOs;
using bmadServer.ApiService.Hubs;
using bmadServer.ApiService.Models.Workflows;
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
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly ILogger<WorkflowsController> _logger;

    public WorkflowsController(
        IWorkflowInstanceService workflowInstanceService,
        IWorkflowRegistry workflowRegistry,
        IAgentRegistry agentRegistry,
        IStepExecutor stepExecutor,
        IHubContext<ChatHub> hubContext,
        ILogger<WorkflowsController> logger)
    {
        _workflowInstanceService = workflowInstanceService;
        _workflowRegistry = workflowRegistry;
        _agentRegistry = agentRegistry;
        _stepExecutor = stepExecutor;
        _hubContext = hubContext;
        _logger = logger;
    }

    private async Task SendWorkflowStatusChangedNotification(Guid workflowId)
    {
        var status = await _workflowInstanceService.GetWorkflowStatusAsync(workflowId);
        if (status != null)
        {
            await _hubContext.Clients.All.SendAsync("WORKFLOW_STATUS_CHANGED", new
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
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WorkflowStatusResponse>> GetWorkflow(Guid id)
    {
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
    /// Get audit log of agent handoffs for a workflow instance with pagination support
    /// </summary>
    /// <param name="id">Workflow instance ID</param>
    /// <param name="page">Page number (1-based, default: 1)</param>
    /// <param name="pageSize">Number of items per page (default: 20, max: 100)</param>
    /// <param name="fromDate">Filter handoffs from this date (optional)</param>
    /// <param name="toDate">Filter handoffs until this date (optional)</param>
    /// <returns>Paginated list of agent handoff records with attribution details</returns>
    [HttpGet("{id}/handoffs")]
    [ProducesResponseType(typeof(PagedResult<AgentHandoffDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<AgentHandoffDto>>> GetWorkflowHandoffs(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        try
        {
            // Validate pagination parameters
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 100) pageSize = 100;

            // Get workflow instance to verify ownership and existence
            var instance = await _workflowInstanceService.GetWorkflowInstanceAsync(id);
            if (instance == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Workflow Not Found",
                    Detail = $"Workflow instance {id} not found"
                });
            }

            // Check authorization: user must own workflow or be admin
            var userId = GetUserIdFromClaims();
            if (instance.UserId != userId && !User.IsInRole("admin"))
            {
                return Forbid();
            }

            // Get handoffs with filtering
            var allHandoffs = await _workflowInstanceService.GetWorkflowHandoffsAsync(id);

            // Apply date range filtering if provided
            var filteredHandoffs = allHandoffs.AsEnumerable();
            if (fromDate.HasValue)
            {
                filteredHandoffs = filteredHandoffs.Where(h => h.Timestamp >= fromDate.Value);
            }
            if (toDate.HasValue)
            {
                filteredHandoffs = filteredHandoffs.Where(h => h.Timestamp <= toDate.Value);
            }

            // Convert to DTOs
            var handoffDtos = filteredHandoffs
                .Select(h => new AgentHandoffDto
                {
                    FromAgent = GetAgentAttribution(h.FromAgentId),
                    ToAgent = GetAgentAttribution(h.ToAgentId),
                    Timestamp = h.Timestamp,
                    StepName = h.WorkflowStepId,
                    StepId = h.WorkflowStepId,
                    Reason = h.Reason
                })
                .OrderByDescending(h => h.Timestamp)
                .ToList();

            // Apply pagination
            var totalItems = handoffDtos.Count;
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            var paginatedItems = handoffDtos
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return Ok(new PagedResult<AgentHandoffDto>
            {
                Items = paginatedItems,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages,
                HasPrevious = page > 1,
                HasNext = page < totalPages
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving handoffs for workflow instance {InstanceId}", id);
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error",
                detail: "Failed to retrieve workflow handoff audit log");
        }
    }

    /// <summary>
    /// Helper method to get agent attribution from registry or create placeholder
    /// </summary>
    private AgentAttributionDto GetAgentAttribution(string agentId)
    {
        var agentDef = _agentRegistry.GetAgent(agentId);
        if (agentDef != null)
        {
            return new AgentAttributionDto
            {
                AgentId = agentDef.AgentId,
                AgentName = agentDef.Name,
                AgentDescription = agentDef.Description ?? string.Empty,
                AgentAvatarUrl = null,
                Capabilities = agentDef.Capabilities.ToList(),
                CurrentStepResponsibility = null
            };
        }

        return new AgentAttributionDto
        {
            AgentId = agentId,
            AgentName = agentId,
            AgentDescription = "Unknown Agent",
            AgentAvatarUrl = null,
            Capabilities = new List<string>(),
            CurrentStepResponsibility = null
        };
    }

    /// <summary>
    /// Extract user ID from JWT claims
    /// </summary>
    private Guid GetUserIdFromClaims()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new InvalidOperationException("User ID not found in claims");
        }

        return userId;
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
