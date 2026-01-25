using bmadServer.ApiService.Data.Entities;
using bmadServer.ApiService.Models.Decisions;
using bmadServer.ApiService.Services.Decisions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace bmadServer.ApiService.Controllers;

/// <summary>
/// Controller for managing workflow decisions
/// </summary>
[ApiController]
[Route("api/v1")]
[Authorize]
public class DecisionsController : ControllerBase
{
    private readonly IDecisionService _decisionService;
    private readonly ILogger<DecisionsController> _logger;

    public DecisionsController(
        IDecisionService decisionService,
        ILogger<DecisionsController> logger)
    {
        _decisionService = decisionService;
        _logger = logger;
    }

    /// <summary>
    /// Get all decisions for a specific workflow instance
    /// </summary>
    /// <param name="id">The workflow instance ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of decisions in chronological order</returns>
    [HttpGet("workflows/{id:guid}/decisions")]
    [ProducesResponseType(typeof(List<DecisionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<DecisionResponse>>> GetDecisionsByWorkflowInstance(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var decisions = await _decisionService.GetDecisionsByWorkflowInstanceAsync(id, cancellationToken);

            var responses = decisions.Select(d => MapToDecisionResponse(d)).ToList();

            return Ok(responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving decisions for workflow {WorkflowId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new ProblemDetails 
                { 
                    Title = "Error retrieving decisions",
                    Detail = ex.Message,
                    Status = StatusCodes.Status500InternalServerError
                });
        }
    }

    /// <summary>
    /// Create a new decision for a workflow instance
    /// </summary>
    /// <param name="request">The decision creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created decision</returns>
    [HttpPost("decisions")]
    [ProducesResponseType(typeof(DecisionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<DecisionResponse>> CreateDecision(
        [FromBody] CreateDecisionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get the authenticated user ID
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized(new ProblemDetails
                {
                    Title = "Unauthorized",
                    Detail = "User ID not found in claims",
                    Status = StatusCodes.Status401Unauthorized
                });
            }

            // Create the decision entity
            var decision = new Decision
            {
                WorkflowInstanceId = request.WorkflowInstanceId,
                StepId = request.StepId,
                DecisionType = request.DecisionType,
                Value = JsonDocument.Parse(request.Value.GetRawText()),
                DecidedBy = userId,
                DecidedAt = DateTime.UtcNow,
                Question = request.Question,
                Options = request.Options.HasValue 
                    ? JsonDocument.Parse(request.Options.Value.GetRawText()) 
                    : null,
                Reasoning = request.Reasoning,
                Context = request.Context.HasValue 
                    ? JsonDocument.Parse(request.Context.Value.GetRawText()) 
                    : null
            };

            var createdDecision = await _decisionService.CreateDecisionAsync(decision, cancellationToken);

            var response = MapToDecisionResponse(createdDecision);

            return CreatedAtAction(
                nameof(GetDecisionById),
                new { id = createdDecision.Id },
                response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when creating decision");
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid request",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating decision");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ProblemDetails
                {
                    Title = "Error creating decision",
                    Detail = ex.Message,
                    Status = StatusCodes.Status500InternalServerError
                });
        }
    }

    /// <summary>
    /// Get a specific decision by ID
    /// </summary>
    /// <param name="id">The decision ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The decision details</returns>
    [HttpGet("decisions/{id:guid}")]
    [ProducesResponseType(typeof(DecisionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DecisionResponse>> GetDecisionById(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var decision = await _decisionService.GetDecisionByIdAsync(id, cancellationToken);

            if (decision == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Decision not found",
                    Detail = $"Decision with ID {id} was not found",
                    Status = StatusCodes.Status404NotFound
                });
            }

            var response = MapToDecisionResponse(decision);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving decision {DecisionId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ProblemDetails
                {
                    Title = "Error retrieving decision",
                    Detail = ex.Message,
                    Status = StatusCodes.Status500InternalServerError
                });
        }
    }

    private static DecisionResponse MapToDecisionResponse(Decision decision)
    {
        return new DecisionResponse
        {
            Id = decision.Id,
            WorkflowInstanceId = decision.WorkflowInstanceId,
            StepId = decision.StepId,
            DecisionType = decision.DecisionType,
            Value = decision.Value?.RootElement ?? default,
            DecidedBy = decision.DecidedBy,
            DecidedAt = decision.DecidedAt,
            Question = decision.Question,
            Options = decision.Options?.RootElement,
            Reasoning = decision.Reasoning,
            Context = decision.Context?.RootElement
        };
    }
}
