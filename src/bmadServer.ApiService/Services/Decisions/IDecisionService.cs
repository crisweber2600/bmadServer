using bmadServer.ApiService.Data.Entities;

namespace bmadServer.ApiService.Services.Decisions;

/// <summary>
/// Service interface for managing workflow decisions
/// </summary>
public interface IDecisionService
{
    /// <summary>
    /// Create a new decision for a workflow instance
    /// </summary>
    /// <param name="decision">The decision entity to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created decision with generated ID</returns>
    Task<Decision> CreateDecisionAsync(Decision decision, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all decisions for a specific workflow instance
    /// </summary>
    /// <param name="workflowInstanceId">The workflow instance ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of decisions in chronological order</returns>
    Task<List<Decision>> GetDecisionsByWorkflowInstanceAsync(Guid workflowInstanceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a specific decision by ID
    /// </summary>
    /// <param name="id">The decision ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The decision, or null if not found</returns>
    Task<Decision?> GetDecisionByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
