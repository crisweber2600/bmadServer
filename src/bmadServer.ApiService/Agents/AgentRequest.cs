namespace bmadServer.ApiService.Agents;

/// <summary>
/// Represents a request from one agent to another.
/// Contains all information needed for the target agent to process the request.
/// </summary>
public class AgentRequest
{
    /// <summary>
    /// ID of the agent making the request.
    /// </summary>
    public required string SourceAgentId { get; init; }

    /// <summary>
    /// Type of request being made (e.g., "gather-requirements", "review-architecture").
    /// </summary>
    public required string RequestType { get; init; }

    /// <summary>
    /// The request payload containing specific data or questions.
    /// </summary>
    public required object Payload { get; init; }

    /// <summary>
    /// Contextual information about the workflow.
    /// </summary>
    public required Dictionary<string, object> WorkflowContext { get; init; }

    /// <summary>
    /// History of the conversation/workflow so far.
    /// </summary>
    public required List<string> ConversationHistory { get; init; }
}
