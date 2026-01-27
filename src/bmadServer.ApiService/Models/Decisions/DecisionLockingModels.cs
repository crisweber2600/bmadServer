using System.ComponentModel.DataAnnotations;

namespace bmadServer.ApiService.Models.Decisions;

/// <summary>
/// Request model for locking a decision
/// </summary>
public class LockDecisionRequest
{
    /// <summary>
    /// Optional reason for locking
    /// </summary>
    [StringLength(500)]
    public string? Reason { get; set; }
}

/// <summary>
/// Request model for unlocking a decision
/// </summary>
public class UnlockDecisionRequest
{
    /// <summary>
    /// Required reason for unlocking
    /// </summary>
    [Required(ErrorMessage = "Unlock reason is required")]
    [StringLength(500, MinimumLength = 5, ErrorMessage = "Unlock reason must be between 5 and 500 characters")]
    public required string Reason { get; set; }
}
