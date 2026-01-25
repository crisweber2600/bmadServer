namespace bmadServer.ApiService.Models.Agents;

/// <summary>
/// Represents a message sent between agents
/// </summary>
public class AgentMessage
{
    /// <summary>
    /// Unique identifier for the message
    /// </summary>
    public Guid MessageId { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Timestamp when the message was created
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Agent that sent the message
    /// </summary>
    public required string SourceAgent { get; init; }

    /// <summary>
    /// Agent that will receive the message
    /// </summary>
    public required string TargetAgent { get; init; }

    /// <summary>
    /// Type of message (e.g., "request", "response", "notification")
    /// </summary>
    public required string MessageType { get; init; }

    /// <summary>
    /// Message content
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// Workflow instance ID this message belongs to
    /// </summary>
    public required Guid WorkflowInstanceId { get; init; }
}
