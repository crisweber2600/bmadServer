using bmadServer.ApiService.Models.Workflows;
using bmadServer.ApiService.Services.Workflows;
using bmadServer.ServiceDefaults.Services.Workflows;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    private readonly ILogger<WorkflowsController> _logger;

    public WorkflowsController(
        IWorkflowInstanceService workflowInstanceService,
        IWorkflowRegistry workflowRegistry,
        IStepExecutor stepExecutor,
        ILogger<WorkflowsController> logger)
    {
        _workflowInstanceService = workflowInstanceService;
        _workflowRegistry = workflowRegistry;
        _stepExecutor = stepExecutor;
        _logger = logger;
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
