using bmadServer.ApiService.Data.Entities;
using bmadServer.ApiService.Models.Decisions;
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
    /// Get the diff between two versions of a decision
    /// </summary>
    /// <param name="decisionId">The decision ID</param>
    /// <param name="fromVersion">The from version number</param>
    /// <param name="toVersion">The to version number</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Diff response showing changes</returns>
    Task<DecisionVersionDiffResponse> GetVersionDiffAsync(Guid decisionId, int fromVersion, int toVersion, CancellationToken cancellationToken = default);

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

    /// <summary>
    /// Request a review for a decision
    /// </summary>
    /// <param name="id">The decision ID</param>
    /// <param name="userId">The user requesting the review</param>
    /// <param name="reviewerIds">List of reviewer user IDs</param>
    /// <param name="deadline">Optional deadline for review</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created review</returns>
    Task<DecisionReview> RequestReviewAsync(
        Guid id,
        Guid userId,
        List<Guid> reviewerIds,
        DateTime? deadline,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Submit a review response (approve or request changes)
    /// </summary>
    /// <param name="reviewId">The review ID</param>
    /// <param name="userId">The reviewer user ID</param>
    /// <param name="responseType">Response type ("Approved" or "ChangesRequested")</param>
    /// <param name="comments">Optional comments</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated review</returns>
    Task<DecisionReview> SubmitReviewResponseAsync(
        Guid reviewId,
        Guid userId,
        string responseType,
        string? comments,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get review for a decision
    /// </summary>
    /// <param name="id">The decision ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The review, or null if not found</returns>
    Task<DecisionReview?> GetDecisionReviewAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get conflicts for a workflow
    /// </summary>
    /// <param name="workflowId">The workflow instance ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of conflicts</returns>
    Task<List<DecisionConflict>> GetConflictsForWorkflowAsync(Guid workflowId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get conflict details with side-by-side comparison
    /// </summary>
    /// <param name="conflictId">The conflict ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Conflict with decision details</returns>
    Task<(DecisionConflict conflict, Decision decision1, Decision decision2)?> GetConflictDetailsAsync(Guid conflictId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolve a conflict
    /// </summary>
    /// <param name="conflictId">The conflict ID</param>
    /// <param name="userId">User resolving the conflict</param>
    /// <param name="resolution">Resolution description</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The resolved conflict</returns>
    Task<DecisionConflict> ResolveConflictAsync(Guid conflictId, Guid userId, string resolution, CancellationToken cancellationToken = default);

    /// <summary>
    /// Override a conflict warning
    /// </summary>
    /// <param name="conflictId">The conflict ID</param>
    /// <param name="userId">User overriding</param>
    /// <param name="justification">Justification for override</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The overridden conflict</returns>
    Task<DecisionConflict> OverrideConflictAsync(Guid conflictId, Guid userId, string justification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all conflict rules
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of conflict rules</returns>
    Task<List<ConflictRule>> GetConflictRulesAsync(CancellationToken cancellationToken = default);
}
