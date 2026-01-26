namespace bmadServer.ApiService.Services.Workflows.Agents;

/// <summary>
/// Status of an approval request
/// </summary>
public enum ApprovalStatus
{
    Pending,
    Approved,
    Modified,
    Rejected
}

/// <summary>
/// Request for human approval
/// </summary>
public class ApprovalRequest
{
    public required Guid RequestId { get; init; }
    public required Guid WorkflowInstanceId { get; init; }
    public required string AgentId { get; init; }
    public required string ProposedResponse { get; init; }
    public required double ConfidenceScore { get; init; }
    public required string Reasoning { get; init; }
    public required DateTime RequestedAt { get; init; }
}

/// <summary>
/// Result of an approval decision
/// </summary>
public class ApprovalDecision
{
    public required Guid RequestId { get; init; }
    public required ApprovalStatus Status { get; init; }
    public required Guid ApprovedBy { get; init; }
    public required DateTime DecidedAt { get; init; }
    public required string OriginalResponse { get; init; }
    public required string FinalResponse { get; init; }
    public string? RejectionReason { get; init; }
}
