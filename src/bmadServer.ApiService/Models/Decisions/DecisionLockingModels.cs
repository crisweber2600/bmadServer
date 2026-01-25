namespace bmadServer.ApiService.Models.Decisions;

/// <summary>
/// Request model for locking a decision
/// </summary>
public class LockDecisionRequest
{
    /// <summary>
    /// Optional reason for locking
    /// </summary>
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
    public required string Reason { get; set; }
}
