using bmadServer.ApiService.Data.Entities;
using System.Text.Json;

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

    /// <summary>
    /// Update an existing decision and create a new version
    /// </summary>
    /// <param name="id">The decision ID</param>
    /// <param name="userId">The user making the update</param>
    /// <param name="value">New value</param>
    /// <param name="question">New question (optional)</param>
    /// <param name="options">New options (optional)</param>
    /// <param name="reasoning">New reasoning (optional)</param>
    /// <param name="context">New context (optional)</param>
    /// <param name="changeReason">Reason for the change</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated decision</returns>
    Task<Decision> UpdateDecisionAsync(
        Guid id,
        Guid userId,
        JsonDocument value,
        string? question,
        JsonDocument? options,
        string? reasoning,
        JsonDocument? context,
        string? changeReason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get version history for a decision
    /// </summary>
    /// <param name="id">The decision ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of versions</returns>
    Task<List<DecisionVersion>> GetDecisionHistoryAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a specific version of a decision
    /// </summary>
    /// <param name="id">The decision ID</param>
    /// <param name="versionNumber">The version number</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The version, or null if not found</returns>
    Task<DecisionVersion?> GetDecisionVersionAsync(Guid id, int versionNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revert a decision to a previous version
    /// </summary>
    /// <param name="id">The decision ID</param>
    /// <param name="versionNumber">The version to revert to</param>
    /// <param name="userId">The user performing the revert</param>
    /// <param name="reason">Reason for reverting</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated decision</returns>
    Task<Decision> RevertDecisionAsync(
        Guid id,
        int versionNumber,
        Guid userId,
        string? reason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lock a decision to prevent modifications
    /// </summary>
    /// <param name="id">The decision ID</param>
    /// <param name="userId">The user locking the decision</param>
    /// <param name="reason">Reason for locking (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The locked decision</returns>
    Task<Decision> LockDecisionAsync(
        Guid id,
        Guid userId,
        string? reason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Unlock a decision to allow modifications
    /// </summary>
    /// <param name="id">The decision ID</param>
    /// <param name="userId">The user unlocking the decision</param>
    /// <param name="reason">Reason for unlocking</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The unlocked decision</returns>
    Task<Decision> UnlockDecisionAsync(
        Guid id,
        Guid userId,
        string reason,
        CancellationToken cancellationToken = default);
}
