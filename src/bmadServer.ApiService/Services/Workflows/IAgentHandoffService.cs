using bmadServer.ApiService.Models.Workflows;

namespace bmadServer.ApiService.Services.Workflows;

/// <summary>
/// Service for recording and retrieving agent handoff events.
/// Provides audit trail of when control transfers from one agent to another.
/// </summary>
public interface IAgentHandoffService
{
    /// <summary>
    /// Record a handoff event when control transfers between agents.
    /// Called before the new agent starts execution.
    /// Non-blocking operation with background persistence.
    /// </summary>
    /// <param name="workflowInstanceId">The workflow instance being transitioned</param>
    /// <param name="fromAgentId">Agent ID currently in control (kebab-case)</param>
    /// <param name="toAgentId">Agent ID assuming control (kebab-case)</param>
    /// <param name="stepId">Workflow step ID where handoff occurs</param>
    /// <param name="reason">Optional reason for handoff (non-PII description)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task RecordHandoffAsync(
        Guid workflowInstanceId,
        string fromAgentId,
        string toAgentId,
        string stepId,
        string? reason = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieve all handoff events for a workflow instance.
    /// Used for audit log display and verification.
    /// </summary>
    /// <param name="workflowInstanceId">The workflow instance to retrieve handoffs for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of handoff records in chronological order</returns>
    Task<List<AgentHandoff>> GetHandoffsAsync(
        Guid workflowInstanceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieve recent handoffs for a workflow instance with optional pagination.
    /// </summary>
    /// <param name="workflowInstanceId">The workflow instance</param>
    /// <param name="limit">Maximum number of handoffs to return (default 5)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of most recent handoff records</returns>
    Task<List<AgentHandoff>> GetRecentHandoffsAsync(
        Guid workflowInstanceId,
        int limit = 5,
        CancellationToken cancellationToken = default);
}
