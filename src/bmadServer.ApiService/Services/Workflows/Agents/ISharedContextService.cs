using System.Text.Json;
using bmadServer.ApiService.Models.Agents;

namespace bmadServer.ApiService.Services.Workflows.Agents;

/// <summary>
/// Service for managing shared workflow context
/// </summary>
public interface ISharedContextService
{
    /// <summary>
    /// Get the shared context for a workflow instance
    /// </summary>
    Task<SharedContext?> GetContextAsync(Guid workflowInstanceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new shared context for a workflow instance
    /// </summary>
    Task<SharedContext> CreateContextAsync(Guid workflowInstanceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get output from a specific step
    /// </summary>
    Task<JsonDocument?> GetStepOutputAsync(Guid workflowInstanceId, string stepId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add step output to the shared context
    /// </summary>
    Task<bool> AddStepOutputAsync(Guid workflowInstanceId, string stepId, JsonDocument output, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add a decision to the shared context
    /// </summary>
    Task<bool> AddDecisionAsync(Guid workflowInstanceId, DecisionRecord decision, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update the shared context with optimistic concurrency control
    /// </summary>
    Task<bool> UpdateContextAsync(SharedContext context, CancellationToken cancellationToken = default);
}
