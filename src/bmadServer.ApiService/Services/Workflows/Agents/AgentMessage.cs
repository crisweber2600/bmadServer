using System.Text.Json;

namespace bmadServer.ApiService.Services.Workflows.Agents;

/// <summary>
/// Message type for agent-to-agent communication
/// </summary>
public enum MessageType
{
    /// <summary>Request message from one agent to another</summary>
    Request = 0,
    
    /// <summary>Response message from target agent back to source</summary>
    Response = 1,
    
    /// <summary>Error message indicating request failure</summary>
    Error = 2
}

/// <summary>
/// Standardized message format for all agent-to-agent communication
/// </summary>
public class AgentMessage
{
    /// <summary>
    /// Unique identifier for this message
    /// </summary>
    public required Guid MessageId { get; init; }

    /// <summary>
    /// Timestamp when the message was created (UTC)
    /// </summary>
    public required DateTime Timestamp { get; init; }

    /// <summary>
    /// Identifier or name of the agent that sent this message
    /// </summary>
    public required string SourceAgent { get; init; }

    /// <summary>
    /// Identifier or name of the agent that should receive this message
    /// </summary>
    public required string TargetAgent { get; init; }

    /// <summary>
    /// Type of message (Request, Response, or Error)
    /// </summary>
    public required MessageType MessageType { get; init; }

    /// <summary>
    /// Message content as JSON
    /// </summary>
    public required JsonDocument Content { get; init; }

    /// <summary>
    /// Workflow instance this message is associated with
    /// </summary>
    public required Guid WorkflowInstanceId { get; init; }

    /// <summary>
    /// Correlation ID linking request/response messages together
    /// </summary>
    public string? CorrelationId { get; init; }
}
