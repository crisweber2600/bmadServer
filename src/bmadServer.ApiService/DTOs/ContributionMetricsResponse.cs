namespace bmadServer.ApiService.DTOs;

/// <summary>
/// Contribution metrics response for workflow (Story 7.3 AC#4)
/// </summary>
public class ContributionMetricsResponse
{
    public Guid WorkflowId { get; set; }
    public List<UserContribution> Contributors { get; set; } = new();
    public ContributionSummary Summary { get; set; } = new();
}

/// <summary>
/// Per-user contribution metrics
/// </summary>
public class UserContribution
{
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public int MessagesSent { get; set; }
    public int DecisionsMade { get; set; }
    public TimeSpan TimeSpent { get; set; }
    public DateTime FirstContribution { get; set; }
    public DateTime LastContribution { get; set; }
}

/// <summary>
/// Aggregate metrics summary
/// </summary>
public class ContributionSummary
{
    public int TotalMessages { get; set; }
    public int TotalDecisions { get; set; }
    public int TotalContributors { get; set; }
    public TimeSpan TotalTimeSpent { get; set; }
}
