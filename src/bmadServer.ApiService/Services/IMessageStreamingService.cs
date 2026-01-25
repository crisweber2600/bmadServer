namespace bmadServer.ApiService.Services;

/// <summary>
/// Service for streaming message responses in real-time via SignalR.
/// Handles chunked message delivery with interruption recovery.
/// </summary>
public interface IMessageStreamingService
{
    /// <summary>
    /// Stream response tokens to the client via SignalR.
    /// </summary>
    /// <param name="message">User message to respond to</param>
    /// <param name="messageId">Unique identifier for the message</param>
    /// <param name="onChunk">Callback to send each chunk (chunk, messageId, isComplete, agentId)</param>
    /// <param name="cancellationToken">Cancellation token for stopping generation</param>
    /// <returns>Message ID</returns>
    Task<string> StreamResponseAsync(
        string message,
        string messageId,
        Func<string, string, bool, string, Task> onChunk,
        CancellationToken cancellationToken);

    /// <summary>
    /// Cancel an ongoing streaming operation.
    /// </summary>
    /// <param name="messageId">Message ID to cancel</param>
    Task CancelStreamingAsync(string messageId);

    /// <summary>
    /// Get partial message content for recovery after interruption.
    /// </summary>
    /// <param name="messageId">Message ID to retrieve</param>
    Task<string?> GetPartialMessageAsync(string messageId);

    /// <summary>
    /// Get the last chunk index for resumption after reconnection.
    /// </summary>
    /// <param name="messageId">Message ID</param>
    Task<int> GetLastChunkIndexAsync(string messageId);
}
