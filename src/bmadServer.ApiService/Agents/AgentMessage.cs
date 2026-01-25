namespace bmadServer.ApiService.Agents;

/// <summary>
/// Represents a message exchanged between agents in the BMAD system.
/// </summary>
public class AgentMessage
{
    /// <summary>
    /// Unique identifier for this message.
    /// </summary>
    public required string MessageId { get; init; }

    /// <summary>
    /// Timestamp when the message was created.
    /// </summary>
    public required DateTime Timestamp { get; init; }

    /// <summary>
    /// ID of the agent sending the message.
    /// </summary>
    public required string SourceAgent { get; init; }

    /// <summary>
    /// ID of the agent receiving the message.
    /// </summary>
    public required string TargetAgent { get; init; }

    /// <summary>
    /// Type of message (e.g., "request", "response").
    /// </summary>
    public required string MessageType { get; init; }

    /// <summary>
    /// The actual message content/payload.
    /// </summary>
    public required object Content { get; init; }

    /// <summary>
    /// ID of the workflow instance this message belongs to.
    /// </summary>
    public required string WorkflowInstanceId { get; init; }
}
