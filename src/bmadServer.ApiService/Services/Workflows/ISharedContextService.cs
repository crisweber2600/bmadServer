using System.Text.Json;
using bmadServer.ApiService.Models.Workflows;

namespace bmadServer.ApiService.Services.Workflows;

public interface ISharedContextService
{
    /// <summary>
    /// Get the full shared context for a workflow instance.
    /// Returns null if the workflow doesn't exist or has no context yet.
    /// </summary>
    Task<SharedContext?> GetContextAsync(Guid workflowInstanceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get output from a specific completed step.
    /// Returns null if the step hasn't completed or the workflow doesn't exist.
    /// </summary>
    Task<JsonDocument?> GetStepOutputAsync(Guid workflowInstanceId, string stepId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add output from a completed step to the shared context.
    /// Initializes context if it doesn't exist. Increments version number.
    /// Throws ArgumentException if workflowInstanceId is invalid or stepId is null/empty.
    /// </summary>
    Task AddStepOutputAsync(Guid workflowInstanceId, string stepId, JsonDocument output, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update shared context with optimistic concurrency control.
    /// Returns true if update succeeded (version matched).
    /// Returns false if version conflict detected (caller should reload and retry).
    /// </summary>
    Task<bool> UpdateContextAsync(Guid workflowInstanceId, SharedContext context, CancellationToken cancellationToken = default);
}
