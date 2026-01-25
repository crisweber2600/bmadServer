namespace bmadServer.ApiService.Agents;

/// <summary>
/// Represents a response from an agent to a request.
/// </summary>
public class AgentResponse
{
    /// <summary>
    /// Indicates if the request was successful.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// The response data from the agent.
    /// </summary>
    public object? Data { get; init; }

    /// <summary>
    /// Error message if the request failed.
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// ID of the agent that generated this response.
    /// </summary>
    public required string RespondingAgentId { get; init; }

    /// <summary>
    /// Timestamp when the response was generated.
    /// </summary>
    public required DateTime Timestamp { get; init; }
}
