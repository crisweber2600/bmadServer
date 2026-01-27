using bmadServer.ApiService.DTOs;

namespace bmadServer.ApiService.Services;

/// <summary>
/// Service for calculating contribution metrics from workflow history (Story 7.3)
/// </summary>
public interface IContributionMetricsService
{
    /// <summary>
    /// Get contribution metrics for all participants in a workflow
    /// </summary>
    Task<ContributionMetricsResponse> GetContributionMetricsAsync(
        Guid workflowId, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get contribution metrics for a specific user in a workflow
    /// </summary>
    Task<UserContribution> GetUserContributionAsync(
        Guid workflowId, 
        Guid userId, 
        CancellationToken cancellationToken = default);
}
