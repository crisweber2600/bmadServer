using System.ComponentModel.DataAnnotations;

namespace bmadServer.ApiService.Models.Decisions;

/// <summary>
/// Request model for requesting a decision review
/// </summary>
public class RequestReviewRequest
{
    /// <summary>
    /// List of reviewer user IDs
    /// </summary>
    [Required(ErrorMessage = "ReviewerIds is required")]
    [MinLength(1, ErrorMessage = "At least one reviewer is required")]
    public required List<Guid> ReviewerIds { get; set; }

    /// <summary>
    /// Optional deadline for review completion
    /// </summary>
    public DateTime? Deadline { get; set; }
}

/// <summary>
/// Request model for submitting a review response
/// </summary>
public class SubmitReviewRequest
{
    /// <summary>
    /// Response type: "Approved" or "ChangesRequested"
    /// </summary>
    [Required(ErrorMessage = "ResponseType is required")]
    [RegularExpression("^(Approved|ChangesRequested)$", ErrorMessage = "ResponseType must be 'Approved' or 'ChangesRequested'")]
    public required string ResponseType { get; set; }

    /// <summary>
    /// Optional comments from the reviewer
    /// </summary>
    public string? Comments { get; set; }
}

/// <summary>
/// Alternate name for SubmitReviewRequest for test compatibility
/// </summary>
public class SubmitReviewResponse
{
    /// <summary>
    /// Review ID
    /// </summary>
    public Guid ReviewId { get; set; }

    /// <summary>
    /// Response status
    /// </summary>
    [Required(ErrorMessage = "Status is required")]
    [RegularExpression("^(Approved|ChangesRequested)$", ErrorMessage = "Status must be 'Approved' or 'ChangesRequested'")]
    public required string Status { get; set; }

    /// <summary>
    /// Optional comments
    /// </summary>
    public string? Comments { get; set; }
}

/// <summary>
/// Response model for review information
/// </summary>
public class DecisionReviewResponse
{
    /// <summary>
    /// Review ID
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Decision ID being reviewed
    /// </summary>
    public required Guid DecisionId { get; set; }

    /// <summary>
    /// User who requested the review
    /// </summary>
    public required Guid RequestedBy { get; set; }

    /// <summary>
    /// When the review was requested
    /// </summary>
    public required DateTime RequestedAt { get; set; }

    /// <summary>
    /// Optional deadline
    /// </summary>
    public DateTime? Deadline { get; set; }

    /// <summary>
    /// Current status
    /// </summary>
    public required string Status { get; set; }

    /// <summary>
    /// When completed (if applicable)
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// List of reviewer responses
    /// </summary>
    public required List<ReviewerResponseInfo> Responses { get; set; }
}

/// <summary>
/// Information about a reviewer's response
/// </summary>
public class ReviewerResponseInfo
{
    /// <summary>
    /// Reviewer user ID
    /// </summary>
    public required Guid ReviewerId { get; set; }

    /// <summary>
    /// Response type
    /// </summary>
    public required string ResponseType { get; set; }

    /// <summary>
    /// Comments
    /// </summary>
    public string? Comments { get; set; }

    /// <summary>
    /// When responded
    /// </summary>
    public required DateTime RespondedAt { get; set; }
}
