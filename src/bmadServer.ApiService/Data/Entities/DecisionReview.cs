namespace bmadServer.ApiService.Data.Entities;

/// <summary>
/// Represents the status of a decision in the review workflow
/// </summary>
public enum DecisionStatus
{
    /// <summary>
    /// Decision is in draft state
    /// </summary>
    Draft,
    
    /// <summary>
    /// Decision is under review
    /// </summary>
    UnderReview,
    
    /// <summary>
    /// Decision is approved and can be locked
    /// </summary>
    Approved,
    
    /// <summary>
    /// Decision has been rejected/changes requested
    /// </summary>
    ChangesRequested,
    
    /// <summary>
    /// Decision is finalized (locked)
    /// </summary>
    Finalized
}

/// <summary>
/// Represents a review request for a decision
/// </summary>
public class DecisionReview
{
    /// <summary>
    /// Unique identifier for this review
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Decision being reviewed
    /// </summary>
    public Guid DecisionId { get; set; }

    /// <summary>
    /// User who requested the review
    /// </summary>
    public Guid RequestedBy { get; set; }

    /// <summary>
    /// When the review was requested
    /// </summary>
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Optional deadline for review completion
    /// </summary>
    public DateTime? Deadline { get; set; }

    /// <summary>
    /// Current status of the review
    /// </summary>
    public string Status { get; set; } = "Pending";

    /// <summary>
    /// When the review was completed
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Navigation property to the decision
    /// </summary>
    public Decision? Decision { get; set; }

    /// <summary>
    /// Navigation property to the requester
    /// </summary>
    public User? Requester { get; set; }

    /// <summary>
    /// Navigation property to individual reviewer responses
    /// </summary>
    public ICollection<DecisionReviewResponse> Responses { get; set; } = new List<DecisionReviewResponse>();
}

/// <summary>
/// Represents an individual reviewer's response to a review request
/// </summary>
public class DecisionReviewResponse
{
    /// <summary>
    /// Unique identifier for this response
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Review this response belongs to
    /// </summary>
    public Guid ReviewId { get; set; }

    /// <summary>
    /// User providing the review
    /// </summary>
    public Guid ReviewerId { get; set; }

    /// <summary>
    /// Response type: "Approved" or "ChangesRequested"
    /// </summary>
    public required string ResponseType { get; set; }

    /// <summary>
    /// Optional comments from the reviewer
    /// </summary>
    public string? Comments { get; set; }

    /// <summary>
    /// When the response was submitted
    /// </summary>
    public DateTime RespondedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property to the review
    /// </summary>
    public DecisionReview? Review { get; set; }

    /// <summary>
    /// Navigation property to the reviewer
    /// </summary>
    public User? Reviewer { get; set; }
}
