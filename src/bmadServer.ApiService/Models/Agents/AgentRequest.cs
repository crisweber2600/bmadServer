using System.Text.Json;

namespace bmadServer.ApiService.Models.Agents;

/// <summary>
/// Represents a request from one agent to another
/// </summary>
public class AgentRequest
{
    /// <summary>
    /// Agent making the request
    /// </summary>
    public required string SourceAgentId { get; init; }

    /// <summary>
    /// Type of request being made
    /// </summary>
    public required string RequestType { get; init; }

    /// <summary>
    /// Request payload data
    /// </summary>
    public required JsonDocument Payload { get; init; }

    /// <summary>
    /// Workflow context for the request
    /// </summary>
    public required JsonDocument? WorkflowContext { get; init; }

    /// <summary>
    /// Conversation history up to this point
    /// </summary>
    public required List<ConversationEntry> ConversationHistory { get; init; }
}

/// <summary>
/// Entry in conversation history
/// </summary>
public class ConversationEntry
{
    public required string Role { get; init; }
    public required string Content { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
