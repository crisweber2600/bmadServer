using bmadServer.ApiService.Agents;
using Microsoft.AspNetCore.Mvc;

namespace bmadServer.ApiService.Controllers;

/// <summary>
/// API endpoints for agent handoff tracking and attribution.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AgentHandoffsController : ControllerBase
{
    private readonly IAgentHandoffService _handoffService;
    private readonly ILogger<AgentHandoffsController> _logger;

    public AgentHandoffsController(
        IAgentHandoffService handoffService,
        ILogger<AgentHandoffsController> logger)
    {
        _handoffService = handoffService;
        _logger = logger;
    }

    /// <summary>
    /// Records a new agent handoff.
    /// </summary>
    /// <param name="request">The handoff request details.</param>
    /// <returns>The created handoff record.</returns>
    [HttpPost]
    public async Task<ActionResult<AgentHandoffRecord>> RecordHandoff([FromBody] RecordHandoffRequest request)
    {
        try
        {
            var handoff = await _handoffService.RecordHandoffAsync(
                request.WorkflowInstanceId,
                request.FromAgent,
                request.ToAgent,
                request.WorkflowStep,
                request.Reason);

            return CreatedAtAction(
                nameof(GetHandoffs),
                new { workflowInstanceId = request.WorkflowInstanceId },
                handoff);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to record handoff for workflow {WorkflowId}", request.WorkflowInstanceId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Gets all handoffs for a workflow instance.
    /// </summary>
    /// <param name="workflowInstanceId">The workflow instance ID.</param>
    /// <returns>List of handoff records.</returns>
    [HttpGet("workflow/{workflowInstanceId}")]
    public async Task<ActionResult<List<AgentHandoffRecord>>> GetHandoffs(Guid workflowInstanceId)
    {
        var handoffs = await _handoffService.GetHandoffsAsync(workflowInstanceId);
        return Ok(handoffs);
    }

    /// <summary>
    /// Gets the current agent for a workflow instance.
    /// </summary>
    /// <param name="workflowInstanceId">The workflow instance ID.</param>
    /// <returns>The current agent ID.</returns>
    [HttpGet("workflow/{workflowInstanceId}/current")]
    public async Task<ActionResult<CurrentAgentResponse>> GetCurrentAgent(Guid workflowInstanceId)
    {
        var currentAgent = await _handoffService.GetCurrentAgentAsync(workflowInstanceId);
        if (currentAgent == null)
        {
            return NotFound(new { message = "No agent currently assigned to this workflow." });
        }

        return Ok(new CurrentAgentResponse { AgentId = currentAgent });
    }

    /// <summary>
    /// Gets agent details for display (e.g., in tooltips).
    /// </summary>
    /// <param name="agentId">The agent ID.</param>
    /// <param name="workflowStep">Optional workflow step for context.</param>
    /// <returns>Agent details.</returns>
    [HttpGet("agent/{agentId}/details")]
    public async Task<ActionResult<AgentDetails>> GetAgentDetails(
        string agentId,
        [FromQuery] string? workflowStep = null)
    {
        var details = await _handoffService.GetAgentDetailsAsync(agentId, workflowStep);
        if (details == null)
        {
            return NotFound(new { message = $"Agent '{agentId}' not found." });
        }

        return Ok(details);
    }
}

/// <summary>
/// Request model for recording a handoff.
/// </summary>
public class RecordHandoffRequest
{
    public required Guid WorkflowInstanceId { get; init; }
    public string? FromAgent { get; init; }
    public required string ToAgent { get; init; }
    public required string WorkflowStep { get; init; }
    public string? Reason { get; init; }
}

/// <summary>
/// Response model for current agent query.
/// </summary>
public class CurrentAgentResponse
{
    public required string AgentId { get; init; }
}
