using System.Collections.Concurrent;

namespace bmadServer.ApiService.Services;

/// <summary>
/// In-memory implementation of message streaming service.
/// For production: Replace with database-backed implementation for persistence.
/// </summary>
public class MessageStreamingService : IMessageStreamingService
{
    private readonly ILogger<MessageStreamingService> _logger;
    private readonly ConcurrentDictionary<string, StreamingContext> _activeStreams = new();
    private readonly ConcurrentDictionary<string, PartialMessage> _partialMessages = new();

    public MessageStreamingService(ILogger<MessageStreamingService> logger)
    {
        _logger = logger;
    }

    public async Task<string> StreamResponseAsync(
        string message,
        string messageId,
        Func<string, string, bool, string, Task> onChunk,
        CancellationToken cancellationToken)
    {
        var context = new StreamingContext
        {
            MessageId = messageId,
            CancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
        };

        _activeStreams.TryAdd(messageId, context);

        try
        {
            // Simulate AI response streaming (replace with actual AI integration)
            var response = $"This is a simulated streaming response to: {message}";
            var words = response.Split(' ');
            var agentId = "bmad-agent-1";

            var partialMessage = new PartialMessage
            {
                MessageId = messageId,
                Content = string.Empty,
                ChunkIndex = 0
            };

            for (int i = 0; i < words.Length; i++)
            {
                if (context.CancellationTokenSource.Token.IsCancellationRequested)
                {
                    _logger.LogInformation("Streaming cancelled for message {MessageId}", messageId);
                    await onChunk("(Stopped)", messageId, true, agentId);
                    break;
                }

                var chunk = i == 0 ? words[i] : $" {words[i]}";
                var isComplete = i == words.Length - 1;

                // Update partial message for recovery
                partialMessage.Content += chunk;
                partialMessage.ChunkIndex = i;
                _partialMessages.AddOrUpdate(messageId, partialMessage, (k, v) => partialMessage);

                // Send chunk to client
                await onChunk(chunk, messageId, isComplete, agentId);

                // Simulate token delay (replace with actual streaming delay)
                if (!isComplete)
                {
                    await Task.Delay(50, context.CancellationTokenSource.Token);
                }
            }

            _logger.LogInformation("Streaming completed for message {MessageId}", messageId);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Streaming operation cancelled for message {MessageId}", messageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during streaming for message {MessageId}", messageId);
            throw;
        }
        finally
        {
            _activeStreams.TryRemove(messageId, out _);
        }

        return messageId;
    }

    public Task CancelStreamingAsync(string messageId)
    {
        if (_activeStreams.TryGetValue(messageId, out var context))
        {
            context.CancellationTokenSource.Cancel();
            _logger.LogInformation("Cancelled streaming for message {MessageId}", messageId);
        }

        return Task.CompletedTask;
    }

    public Task<string?> GetPartialMessageAsync(string messageId)
    {
        if (_partialMessages.TryGetValue(messageId, out var partial))
        {
            return Task.FromResult<string?>(partial.Content);
        }

        return Task.FromResult<string?>(null);
    }

    public Task<int> GetLastChunkIndexAsync(string messageId)
    {
        if (_partialMessages.TryGetValue(messageId, out var partial))
        {
            return Task.FromResult(partial.ChunkIndex);
        }

        return Task.FromResult(0);
    }

    private class StreamingContext
    {
        public required string MessageId { get; set; }
        public required CancellationTokenSource CancellationTokenSource { get; set; }
    }

    private class PartialMessage
    {
        public required string MessageId { get; set; }
        public required string Content { get; set; }
        public required int ChunkIndex { get; set; }
    }
}
