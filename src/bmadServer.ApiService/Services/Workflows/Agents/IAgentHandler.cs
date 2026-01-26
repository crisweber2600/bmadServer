using System.Text.Json;
using bmadServer.ApiService.Models.Workflows;

namespace bmadServer.ApiService.Services.Workflows.Agents;

public record class AgentContext
{
    public required Guid WorkflowInstanceId { get; init; }
    public required string StepId { get; init; }
    public required string StepName { get; init; }
    public required JsonDocument? WorkflowContext { get; init; }
    public required JsonDocument? StepData { get; init; }
    public required JsonDocument? StepParameters { get; init; }
    public required List<ConversationMessage> ConversationHistory { get; init; }
    public required string? UserInput { get; init; }
    public SharedContext? SharedContext { get; init; }
}

/// <summary>
/// Conversation message for agent context
/// </summary>
public class ConversationMessage
{
    public required string Role { get; init; }
    public required string Content { get; init; }
    public DateTime Timestamp { get; init; }
}

/// <summary>
/// Result from agent execution
/// </summary>
public class AgentResult
{
    public bool Success { get; init; }
    public JsonDocument? Output { get; init; }
    public string? ErrorMessage { get; init; }
    public bool IsRetryable { get; init; }
}

/// <summary>
/// Interface for agent handlers that execute workflow steps
/// </summary>
public interface IAgentHandler
{
    /// <summary>
    /// Execute a step with the given context
    /// </summary>
    Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Execute a step with streaming progress updates
    /// </summary>
    IAsyncEnumerable<StepProgress> ExecuteWithStreamingAsync(AgentContext context, CancellationToken cancellationToken = default);
}

/// <summary>
/// Progress update for long-running steps
/// </summary>
public class StepProgress
{
    public string? Message { get; init; }
    public int? PercentComplete { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
