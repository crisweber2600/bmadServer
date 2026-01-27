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

    /// <summary>
    /// Update an existing decision
    /// </summary>
    /// <param name="id">The decision ID</param>
    /// <param name="request">The update request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated decision</returns>
    [HttpPut("decisions/{id:guid}")]
    [ProducesResponseType(typeof(DecisionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DecisionResponse>> UpdateDecision(
        Guid id,
        [FromBody] UpdateDecisionRequest request,
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

            // Check if decision is locked
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

            if (decision.IsLocked)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
                {
                    Title = "Decision is locked",
                    Detail = "Cannot update a locked decision",
                    Status = StatusCodes.Status403Forbidden
                });
            }

            var updatedDecision = await _decisionService.UpdateDecisionAsync(
                id,
                userId,
                JsonDocument.Parse(request.Value.GetRawText()),
                request.Question,
                request.Options.HasValue ? JsonDocument.Parse(request.Options.Value.GetRawText()) : null,
                request.Reasoning,
                request.Context.HasValue ? JsonDocument.Parse(request.Context.Value.GetRawText()) : null,
                request.ChangeReason,
                cancellationToken);

            var response = MapToDecisionResponse(updatedDecision);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when updating decision {DecisionId}", id);
            return NotFound(new ProblemDetails
            {
                Title = "Decision not found",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating decision {DecisionId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ProblemDetails
                {
                    Title = "Error updating decision",
                    Detail = ex.Message,
                    Status = StatusCodes.Status500InternalServerError
                });
        }
    }

    /// <summary>
    /// Get version history for a decision
    /// </summary>
    /// <param name="id">The decision ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of decision versions</returns>
    [HttpGet("decisions/{id:guid}/history")]
    [ProducesResponseType(typeof(List<DecisionVersionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<DecisionVersionResponse>>> GetDecisionHistory(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var versions = await _decisionService.GetDecisionHistoryAsync(id, cancellationToken);

            var responses = versions.Select(v => new DecisionVersionResponse
            {
                Id = v.Id,
                VersionNumber = v.VersionNumber,
                Value = v.Value?.RootElement ?? default,
                ModifiedBy = v.ModifiedBy,
                ModifiedAt = v.ModifiedAt,
                ChangeReason = v.ChangeReason,
                Question = v.Question,
                Options = v.Options?.RootElement,
                Reasoning = v.Reasoning,
                Context = v.Context?.RootElement
            }).ToList();

            return Ok(responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving version history for decision {DecisionId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ProblemDetails
                {
                    Title = "Error retrieving version history",
                    Detail = ex.Message,
                    Status = StatusCodes.Status500InternalServerError
                });
        }
    }

    /// <summary>
    /// Revert a decision to a previous version
    /// </summary>
    /// <param name="id">The decision ID</param>
    /// <param name="version">The version number to revert to</param>
    /// <param name="request">The revert request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated decision</returns>
    [HttpPost("decisions/{id:guid}/revert")]
    [ProducesResponseType(typeof(DecisionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DecisionResponse>> RevertDecision(
        Guid id,
        [FromQuery] int version,
        [FromBody] RevertDecisionRequest request,
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

            var revertedDecision = await _decisionService.RevertDecisionAsync(
                id,
                version,
                userId,
                request.Reason,
                cancellationToken);

            var response = MapToDecisionResponse(revertedDecision);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when reverting decision {DecisionId} to version {Version}", id, version);
            return NotFound(new ProblemDetails
            {
                Title = "Not found",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reverting decision {DecisionId} to version {Version}", id, version);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ProblemDetails
                {
                    Title = "Error reverting decision",
                    Detail = ex.Message,
                    Status = StatusCodes.Status500InternalServerError
                });
        }
    }

    /// <summary>
    /// Get the diff between two versions of a decision
    /// </summary>
    /// <param name="id">The decision ID</param>
    /// <param name="from">The from version number</param>
    /// <param name="to">The to version number</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Diff showing changes between versions</returns>
    [HttpGet("decisions/{id:guid}/diff")]
    [ProducesResponseType(typeof(DecisionVersionDiffResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DecisionVersionDiffResponse>> GetVersionDiff(
        Guid id,
        [FromQuery] int from,
        [FromQuery] int to,
        CancellationToken cancellationToken)
    {
        try
        {
            var diff = await _decisionService.GetVersionDiffAsync(id, from, to, cancellationToken);
            return Ok(diff);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when getting diff for decision {DecisionId}", id);
            return NotFound(new ProblemDetails
            {
                Title = "Version not found",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting diff for decision {DecisionId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ProblemDetails
                {
                    Title = "Error getting version diff",
                    Detail = ex.Message,
                    Status = StatusCodes.Status500InternalServerError
                });
        }
    }

    /// <summary>
    /// Lock a decision to prevent modifications
    /// </summary>
    /// <param name="id">The decision ID</param>
    /// <param name="request">The lock request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The locked decision</returns>
    [HttpPost("decisions/{id:guid}/lock")]
    [ProducesResponseType(typeof(DecisionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DecisionResponse>> LockDecision(
        Guid id,
        [FromBody] LockDecisionRequest request,
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

            var lockedDecision = await _decisionService.LockDecisionAsync(
                id,
                userId,
                request.Reason,
                cancellationToken);

            var response = MapToDecisionResponse(lockedDecision);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when locking decision {DecisionId}", id);
            
            if (ex.Message.Contains("not found"))
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Decision not found",
                    Detail = ex.Message,
                    Status = StatusCodes.Status404NotFound
                });
            }
            
            return BadRequest(new ProblemDetails
            {
                Title = "Cannot lock decision",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error locking decision {DecisionId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ProblemDetails
                {
                    Title = "Error locking decision",
                    Detail = ex.Message,
                    Status = StatusCodes.Status500InternalServerError
                });
        }
    }

    /// <summary>
    /// Unlock a decision to allow modifications
    /// </summary>
    /// <param name="id">The decision ID</param>
    /// <param name="request">The unlock request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The unlocked decision</returns>
    [HttpPost("decisions/{id:guid}/unlock")]
    [ProducesResponseType(typeof(DecisionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DecisionResponse>> UnlockDecision(
        Guid id,
        [FromBody] UnlockDecisionRequest request,
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

            var unlockedDecision = await _decisionService.UnlockDecisionAsync(
                id,
                userId,
                request.Reason,
                cancellationToken);

            var response = MapToDecisionResponse(unlockedDecision);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when unlocking decision {DecisionId}", id);
            
            if (ex.Message.Contains("not found"))
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Decision not found",
                    Detail = ex.Message,
                    Status = StatusCodes.Status404NotFound
                });
            }
            
            return BadRequest(new ProblemDetails
            {
                Title = "Cannot unlock decision",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unlocking decision {DecisionId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ProblemDetails
                {
                    Title = "Error unlocking decision",
                    Detail = ex.Message,
                    Status = StatusCodes.Status500InternalServerError
                });
        }
    }

    /// <summary>
    /// Request a review for a decision
    /// </summary>
    /// <param name="id">The decision ID</param>
    /// <param name="request">The review request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created review</returns>
    [HttpPost("decisions/{id:guid}/request-review")]
    [ProducesResponseType(typeof(Models.Decisions.DecisionReviewResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Models.Decisions.DecisionReviewResponse>> RequestReview(
        Guid id,
        [FromBody] RequestReviewRequest request,
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

            var review = await _decisionService.RequestReviewAsync(
                id,
                userId,
                request.ReviewerIds,
                request.Deadline,
                cancellationToken);

            var response = new Models.Decisions.DecisionReviewResponse
            {
                Id = review.Id,
                DecisionId = review.DecisionId,
                RequestedBy = review.RequestedBy,
                RequestedAt = review.RequestedAt,
                Deadline = review.Deadline,
                Status = review.Status,
                CompletedAt = review.CompletedAt,
                Responses = review.Responses.Select(r => new ReviewerResponseInfo
                {
                    ReviewerId = r.ReviewerId,
                    ResponseType = r.ResponseType,
                    Comments = r.Comments,
                    RespondedAt = r.RespondedAt
                }).ToList()
            };

            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when requesting review for decision {DecisionId}", id);
            
            if (ex.Message.Contains("not found"))
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Decision not found",
                    Detail = ex.Message,
                    Status = StatusCodes.Status404NotFound
                });
            }
            
            return BadRequest(new ProblemDetails
            {
                Title = "Cannot request review",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting review for decision {DecisionId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ProblemDetails
                {
                    Title = "Error requesting review",
                    Detail = ex.Message,
                    Status = StatusCodes.Status500InternalServerError
                });
        }
    }

    /// <summary>
    /// Submit a review response
    /// </summary>
    /// <param name="reviewId">The review ID</param>
    /// <param name="request">The review response</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated review</returns>
    [HttpPost("reviews/{reviewId:guid}/respond")]
    [ProducesResponseType(typeof(Models.Decisions.DecisionReviewResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Models.Decisions.DecisionReviewResponse>> SubmitReviewResponse(
        Guid reviewId,
        [FromBody] SubmitReviewRequest request,
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

            var review = await _decisionService.SubmitReviewResponseAsync(
                reviewId,
                userId,
                request.ResponseType,
                request.Comments,
                cancellationToken);

            var response = new Models.Decisions.DecisionReviewResponse
            {
                Id = review.Id,
                DecisionId = review.DecisionId,
                RequestedBy = review.RequestedBy,
                RequestedAt = review.RequestedAt,
                Deadline = review.Deadline,
                Status = review.Status,
                CompletedAt = review.CompletedAt,
                Responses = review.Responses.Select(r => new ReviewerResponseInfo
                {
                    ReviewerId = r.ReviewerId,
                    ResponseType = r.ResponseType,
                    Comments = r.Comments,
                    RespondedAt = r.RespondedAt
                }).ToList()
            };

            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when submitting review response for review {ReviewId}", reviewId);
            
            if (ex.Message.Contains("not found"))
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Review not found",
                    Detail = ex.Message,
                    Status = StatusCodes.Status404NotFound
                });
            }
            
            return BadRequest(new ProblemDetails
            {
                Title = "Cannot submit review response",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting review response for review {ReviewId}", reviewId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ProblemDetails
                {
                    Title = "Error submitting review response",
                    Detail = ex.Message,
                    Status = StatusCodes.Status500InternalServerError
                });
        }
    }

    /// <summary>
    /// Get review for a decision
    /// </summary>
    /// <param name="id">The decision ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The review information</returns>
    [HttpGet("decisions/{id:guid}/review")]
    [ProducesResponseType(typeof(Models.Decisions.DecisionReviewResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Models.Decisions.DecisionReviewResponse>> GetDecisionReview(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var review = await _decisionService.GetDecisionReviewAsync(id, cancellationToken);

            if (review == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Review not found",
                    Detail = $"No review found for decision {id}",
                    Status = StatusCodes.Status404NotFound
                });
            }

            var response = new Models.Decisions.DecisionReviewResponse
            {
                Id = review.Id,
                DecisionId = review.DecisionId,
                RequestedBy = review.RequestedBy,
                RequestedAt = review.RequestedAt,
                Deadline = review.Deadline,
                Status = review.Status,
                CompletedAt = review.CompletedAt,
                Responses = review.Responses.Select(r => new ReviewerResponseInfo
                {
                    ReviewerId = r.ReviewerId,
                    ResponseType = r.ResponseType,
                    Comments = r.Comments,
                    RespondedAt = r.RespondedAt
                }).ToList()
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving review for decision {DecisionId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ProblemDetails
                {
                    Title = "Error retrieving review",
                    Detail = ex.Message,
                    Status = StatusCodes.Status500InternalServerError
                });
        }
    }

    /// <summary>
    /// Submit a review response for a decision
    /// </summary>
    /// <param name="id">The decision ID</param>
    /// <param name="request">The review response</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated review</returns>
    [HttpPost("decisions/{id:guid}/review-response")]
    [ProducesResponseType(typeof(Models.Decisions.DecisionReviewResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Models.Decisions.DecisionReviewResponse>> SubmitReviewResponseForDecision(
        Guid id,
        [FromBody] Models.Decisions.SubmitReviewResponse request,
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

            var review = await _decisionService.GetDecisionReviewAsync(id, cancellationToken);
            if (review == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Review not found",
                    Detail = $"No review found for decision {id}",
                    Status = StatusCodes.Status404NotFound
                });
            }

            var updatedReview = await _decisionService.SubmitReviewResponseAsync(
                review.Id,
                userId,
                request.Status,
                request.Comments,
                cancellationToken);

            var response = new Models.Decisions.DecisionReviewResponse
            {
                Id = updatedReview.Id,
                DecisionId = updatedReview.DecisionId,
                RequestedBy = updatedReview.RequestedBy,
                RequestedAt = updatedReview.RequestedAt,
                Deadline = updatedReview.Deadline,
                Status = updatedReview.Status,
                CompletedAt = updatedReview.CompletedAt,
                Responses = updatedReview.Responses.Select(r => new ReviewerResponseInfo
                {
                    ReviewerId = r.ReviewerId,
                    ResponseType = r.ResponseType,
                    Comments = r.Comments,
                    RespondedAt = r.RespondedAt
                }).ToList()
            };

            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when submitting review response for decision {DecisionId}", id);
            
            if (ex.Message.Contains("not found"))
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Review not found",
                    Detail = ex.Message,
                    Status = StatusCodes.Status404NotFound
                });
            }
            
            return BadRequest(new ProblemDetails
            {
                Title = "Cannot submit review response",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting review response for decision {DecisionId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ProblemDetails
                {
                    Title = "Error submitting review response",
                    Detail = ex.Message,
                    Status = StatusCodes.Status500InternalServerError
                });
        }
    }

    /// <summary>
    /// Get conflicts for a workflow
    /// </summary>
    /// <param name="workflowId">The workflow instance ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of conflicts</returns>
    [HttpGet("workflows/{workflowId:guid}/conflicts")]
    [ProducesResponseType(typeof(List<DecisionConflictResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<DecisionConflictResponse>>> GetWorkflowConflicts(
        Guid workflowId,
        CancellationToken cancellationToken)
    {
        try
        {
            var conflicts = await _decisionService.GetConflictsForWorkflowAsync(workflowId, cancellationToken);

            var responses = conflicts.Select(c => new DecisionConflictResponse
            {
                Id = c.Id,
                DecisionId1 = c.DecisionId1,
                DecisionId2 = c.DecisionId2,
                ConflictType = c.ConflictType,
                Description = c.Description,
                Severity = c.Severity,
                Status = c.Status,
                DetectedAt = c.DetectedAt,
                ResolvedAt = c.ResolvedAt,
                ResolvedBy = c.ResolvedBy,
                Resolution = c.Resolution,
                OverrideJustification = c.OverrideJustification,
                Nature = c.ConflictType
            }).ToList();

            return Ok(responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving conflicts for workflow {WorkflowId}", workflowId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ProblemDetails
                {
                    Title = "Error retrieving conflicts",
                    Detail = ex.Message,
                    Status = StatusCodes.Status500InternalServerError
                });
        }
    }

    /// <summary>
    /// Get conflict details with side-by-side comparison
    /// </summary>
    /// <param name="conflictId">The conflict ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Conflict comparison</returns>
    [HttpGet("conflicts/{conflictId:guid}")]
    [ProducesResponseType(typeof(ConflictComparisonResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ConflictComparisonResponse>> GetConflictDetails(
        Guid conflictId,
        CancellationToken cancellationToken)
    {
        try
        {
            var details = await _decisionService.GetConflictDetailsAsync(conflictId, cancellationToken);

            if (details == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Conflict not found",
                    Detail = $"Conflict {conflictId} not found",
                    Status = StatusCodes.Status404NotFound
                });
            }

            var (conflict, decision1, decision2) = details.Value;

            var response = new ConflictComparisonResponse
            {
                Conflict = new ConflictResponse
                {
                    Id = conflict.Id,
                    DecisionId1 = conflict.DecisionId1,
                    DecisionId2 = conflict.DecisionId2,
                    ConflictType = conflict.ConflictType,
                    Description = conflict.Description,
                    Severity = conflict.Severity,
                    Status = conflict.Status,
                    DetectedAt = conflict.DetectedAt,
                    ResolvedAt = conflict.ResolvedAt,
                    ResolvedBy = conflict.ResolvedBy,
                    Resolution = conflict.Resolution,
                    OverrideJustification = conflict.OverrideJustification
                },
                Decision1 = MapToDecisionResponse(decision1),
                Decision2 = MapToDecisionResponse(decision2),
                SuggestedResolutions = new List<string>
                {
                    "Update Decision 1 to match Decision 2",
                    "Update Decision 2 to match Decision 1",
                    "Create a new decision that reconciles both"
                }
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving conflict details for {ConflictId}", conflictId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ProblemDetails
                {
                    Title = "Error retrieving conflict details",
                    Detail = ex.Message,
                    Status = StatusCodes.Status500InternalServerError
                });
        }
    }

    /// <summary>
    /// Resolve a conflict
    /// </summary>
    /// <param name="conflictId">The conflict ID</param>
    /// <param name="request">Resolution request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Resolved conflict</returns>
    [HttpPost("conflicts/{conflictId:guid}/resolve")]
    [ProducesResponseType(typeof(ConflictResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ConflictResponse>> ResolveConflict(
        Guid conflictId,
        [FromBody] ResolveConflictRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized();
            }

            var conflict = await _decisionService.ResolveConflictAsync(conflictId, userId, request.Resolution, cancellationToken);

            var response = new ConflictResponse
            {
                Id = conflict.Id,
                DecisionId1 = conflict.DecisionId1,
                DecisionId2 = conflict.DecisionId2,
                ConflictType = conflict.ConflictType,
                Description = conflict.Description,
                Severity = conflict.Severity,
                Status = conflict.Status,
                DetectedAt = conflict.DetectedAt,
                ResolvedAt = conflict.ResolvedAt,
                ResolvedBy = conflict.ResolvedBy,
                Resolution = conflict.Resolution,
                OverrideJustification = conflict.OverrideJustification
            };

            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when resolving conflict {ConflictId}", conflictId);
            return BadRequest(new ProblemDetails { Title = "Cannot resolve conflict", Detail = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving conflict {ConflictId}", conflictId);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Override a conflict warning
    /// </summary>
    /// <param name="conflictId">The conflict ID</param>
    /// <param name="request">Override request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Overridden conflict</returns>
    [HttpPost("conflicts/{conflictId:guid}/override")]
    [ProducesResponseType(typeof(ConflictResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ConflictResponse>> OverrideConflict(
        Guid conflictId,
        [FromBody] OverrideConflictRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized();
            }

            var conflict = await _decisionService.OverrideConflictAsync(conflictId, userId, request.Justification, cancellationToken);

            var response = new ConflictResponse
            {
                Id = conflict.Id,
                DecisionId1 = conflict.DecisionId1,
                DecisionId2 = conflict.DecisionId2,
                ConflictType = conflict.ConflictType,
                Description = conflict.Description,
                Severity = conflict.Severity,
                Status = conflict.Status,
                DetectedAt = conflict.DetectedAt,
                ResolvedAt = conflict.ResolvedAt,
                ResolvedBy = conflict.ResolvedBy,
                Resolution = conflict.Resolution,
                OverrideJustification = conflict.OverrideJustification
            };

            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when overriding conflict {ConflictId}", conflictId);
            return BadRequest(new ProblemDetails { Title = "Cannot override conflict", Detail = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error overriding conflict {ConflictId}", conflictId);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Get all active conflict rules
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of conflict rules</returns>
    [HttpGet("conflict-rules")]
    [ProducesResponseType(typeof(List<ConflictRuleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<ConflictRuleResponse>>> GetConflictRulesList(CancellationToken cancellationToken)
    {
        try
        {
            var rules = await _decisionService.GetConflictRulesAsync(cancellationToken);

            var responses = rules.Select(r => new ConflictRuleResponse
            {
                Id = r.Id,
                Name = r.Name,
                ConflictType = r.ConflictType,
                Description = r.Description,
                Configuration = r.Configuration?.RootElement,
                IsActive = r.IsActive,
                Severity = r.Severity,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            }).ToList();

            return Ok(responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving conflict rules");
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Get all active conflict rules (alias route for backward compatibility)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of conflict rules</returns>
    [HttpGet("conflicts/rules")]
    [ProducesResponseType(typeof(List<ConflictRuleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<ConflictRuleResponse>>> GetConflictRules(CancellationToken cancellationToken)
    {
        try
        {
            var rules = await _decisionService.GetConflictRulesAsync(cancellationToken);

            var responses = rules.Select(r => new ConflictRuleResponse
            {
                Id = r.Id,
                Name = r.Name,
                ConflictType = r.ConflictType,
                Description = r.Description,
                Configuration = r.Configuration?.RootElement,
                IsActive = r.IsActive,
                Severity = r.Severity,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            }).ToList();

            return Ok(responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving conflict rules");
            return StatusCode(StatusCodes.Status500InternalServerError);
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
            Context = decision.Context?.RootElement,
            CurrentVersion = decision.CurrentVersion,
            UpdatedAt = decision.UpdatedAt,
            UpdatedBy = decision.UpdatedBy,
            IsLocked = decision.IsLocked,
            LockedBy = decision.LockedBy,
            LockedAt = decision.LockedAt,
            LockReason = decision.LockReason,
            Status = decision.Status.ToString()
        };
    }
}
