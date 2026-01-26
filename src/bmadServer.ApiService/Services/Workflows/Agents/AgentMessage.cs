namespace bmadServer.ApiService.Services.Workflows.Agents;

/// <summary>
/// Context information for agent-to-agent messaging
/// </summary>
public class AgentMessageContext
{
    public required Guid WorkflowInstanceId { get; init; }
    public required string SourceAgentId { get; init; }
    public required List<string> ConversationHistory { get; init; }
}

/// <summary>
/// Agent message structure
/// </summary>
public class AgentMessage
{
    public required string MessageId { get; init; }
    public required DateTime Timestamp { get; init; }
    public required string SourceAgent { get; init; }
    public required string TargetAgent { get; init; }
    public required string MessageType { get; init; }
    public required string Content { get; init; }
    public required Guid WorkflowInstanceId { get; init; }
    public required bool Success { get; init; }
    public string? Response { get; init; }
    public string? ErrorMessage { get; init; }
}
