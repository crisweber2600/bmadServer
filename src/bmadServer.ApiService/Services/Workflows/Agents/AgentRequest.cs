using System.Text.Json;

namespace bmadServer.ApiService.Services.Workflows.Agents;

/// <summary>
/// Workflow context passed in agent-to-agent requests
/// </summary>
public class WorkflowContext
{
    /// <summary>
    /// The workflow instance ID
    /// </summary>
    public required Guid WorkflowInstanceId { get; init; }

    /// <summary>
    /// The current step ID
    /// </summary>
    public required Guid CurrentStepId { get; init; }

    /// <summary>
    /// The current step name
    /// </summary>
    public required string CurrentStepName { get; init; }

    /// <summary>
    /// Step outputs from previous steps
    /// </summary>
    public Dictionary<string, object> StepOutputs { get; init; } = new();

    /// <summary>
    /// Workflow state (shared across steps)
    /// </summary>
    public Dictionary<string, object> WorkflowState { get; init; } = new();
}

/// <summary>
/// Request sent from one agent to another
/// </summary>
public class AgentRequest
{
    /// <summary>
    /// ID of the agent making the request
    /// </summary>
    public required string SourceAgentId { get; init; }

    /// <summary>
    /// Type of request (e.g., "get-architecture-input", "validate-requirements")
    /// </summary>
    public required string RequestType { get; init; }

    /// <summary>
    /// Request payload as JSON
    /// </summary>
    public required JsonDocument Payload { get; init; }

    /// <summary>
    /// Workflow context for the agent to understand the step and workflow state
    /// </summary>
    public required WorkflowContext WorkflowContext { get; init; }

    /// <summary>
    /// Conversation history for context (previous messages in this workflow)
    /// </summary>
    public List<AgentMessage> ConversationHistory { get; init; } = new();
}

/// <summary>
/// Response returned from an agent request
/// </summary>
public class AgentResponse
{
    /// <summary>
    /// Whether the request was successful
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Response content as JSON (null if error)
    /// </summary>
    public JsonDocument? Content { get; init; }

    /// <summary>
    /// Metadata about the response
    /// </summary>
    public Dictionary<string, string> Metadata { get; init; } = new();

    /// <summary>
    /// Error message (only populated if Success = false)
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Whether the error is transient and can be retried
    /// </summary>
    public bool IsRetryable { get; init; }
}
