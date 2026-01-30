using Polly;
using Polly.Retry;
using Polly.Timeout;

namespace bmadServer.ApiService.Infrastructure.Policies;

/// <summary>
/// Polly-based resilience policies for agent-to-agent calls
/// </summary>
public static class AgentCallPolicy
{
    /// <summary>
    /// Create a retry policy with exponential backoff for transient failures
    /// Retries: 3 attempts with 1s, 2s, 4s delays
    /// </summary>
    public static AsyncRetryPolicy<TResult> CreateRetryPolicy<TResult>(
        ILogger logger,
        string correlationId)
    {
        return Policy<TResult>
            .Handle<HttpRequestException>()
            .Or<TimeoutException>()
            .Or<TimeoutRejectedException>()
            .Or<OperationCanceledException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt - 1)),
                onRetry: (outcome, timespan, retryAttempt, context) =>
                {
                    logger.LogWarning(
                        "Retry attempt {RetryAttempt} after {Delay}s due to: {Exception}, CorrelationId: {CorrelationId}",
                        retryAttempt,
                        timespan.TotalSeconds,
                        outcome.Exception?.GetType().Name ?? "Unknown",
                        correlationId);
                });
    }

    /// <summary>
    /// Create a timeout policy for agent calls
    /// Default: 30 seconds
    /// </summary>
    public static AsyncTimeoutPolicy CreateTimeoutPolicy(TimeSpan timeout)
    {
        return Policy
            .TimeoutAsync(timeout, TimeoutStrategy.Pessimistic);
    }

    /// <summary>
    /// Create a combined policy wrapping timeout with retry
    /// </summary>
    public static IAsyncPolicy<TResult> CreateCombinedPolicy<TResult>(
        ILogger logger,
        string correlationId,
        TimeSpan timeout)
    {
        var retryPolicy = CreateRetryPolicy<TResult>(logger, correlationId);
        var timeoutPolicy = CreateTimeoutPolicy(timeout);

        // Wrap timeout inside retry - each retry gets its own timeout
        return retryPolicy.WrapAsync(timeoutPolicy);
    }
}
