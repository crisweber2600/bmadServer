using bmadServer.ApiService.Infrastructure.Policies;
using Microsoft.Extensions.Logging;
using Moq;
using Polly.Timeout;
using Xunit;

namespace bmadServer.Tests.Unit.Infrastructure.Policies;

public class AgentCallPolicyTests : IDisposable
{
    private readonly Mock<ILogger> _mockLogger;
    private readonly string _correlationId;

    public AgentCallPolicyTests()
    {
        _mockLogger = new Mock<ILogger>();
        _correlationId = Guid.NewGuid().ToString();
    }

    public void Dispose()
    {
        // Cleanup
    }

    [Fact]
    public async Task RetryPolicy_RetriesOnTransientHttpException()
    {
        // Arrange
        var policy = AgentCallPolicy.CreateRetryPolicy<string>(_mockLogger.Object, _correlationId);
        var attemptCount = 0;

        // Act
        var result = await policy.ExecuteAsync(async () =>
        {
            attemptCount++;
            if (attemptCount < 3)
            {
                throw new HttpRequestException("Transient error");
            }
            return await Task.FromResult("Success");
        });

        // Assert
        Assert.Equal(3, attemptCount);
        Assert.Equal("Success", result);
    }

    [Fact]
    public async Task RetryPolicy_RetriesOnTimeoutException()
    {
        // Arrange
        var policy = AgentCallPolicy.CreateRetryPolicy<string>(_mockLogger.Object, _correlationId);
        var attemptCount = 0;

        // Act
        var result = await policy.ExecuteAsync(async () =>
        {
            attemptCount++;
            if (attemptCount < 2)
            {
                throw new TimeoutException("Request timeout");
            }
            return await Task.FromResult("Success");
        });

        // Assert
        Assert.Equal(2, attemptCount);
        Assert.Equal("Success", result);
    }

    [Fact]
    public async Task RetryPolicy_RetriesOnOperationCanceled()
    {
        // Arrange
        var policy = AgentCallPolicy.CreateRetryPolicy<string>(_mockLogger.Object, _correlationId);
        var attemptCount = 0;

        // Act
        var result = await policy.ExecuteAsync(async () =>
        {
            attemptCount++;
            if (attemptCount < 3)
            {
                throw new OperationCanceledException("Operation canceled");
            }
            return await Task.FromResult("Success");
        });

        // Assert
        Assert.Equal(3, attemptCount);
        Assert.Equal("Success", result);
    }

    [Fact]
    public async Task RetryPolicy_ExhaustsAfterThreeRetries()
    {
        // Arrange
        var policy = AgentCallPolicy.CreateRetryPolicy<string>(_mockLogger.Object, _correlationId);
        var attemptCount = 0;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HttpRequestException>(async () =>
        {
            await policy.ExecuteAsync(async () =>
            {
                attemptCount++;
                throw new HttpRequestException("Persistent error");
            });
        });

        Assert.Equal(4, attemptCount); // Initial + 3 retries
        Assert.Equal("Persistent error", exception.Message);
    }

    [Fact]
    public async Task RetryPolicy_UsesExponentialBackoff()
    {
        // Arrange
        var policy = AgentCallPolicy.CreateRetryPolicy<string>(_mockLogger.Object, _correlationId);
        var attemptTimes = new List<DateTime>();

        // Act
        try
        {
            await policy.ExecuteAsync(async () =>
            {
                attemptTimes.Add(DateTime.UtcNow);
                throw new HttpRequestException("Test error");
            });
        }
        catch (HttpRequestException)
        {
            // Expected
        }

        // Assert
        Assert.Equal(4, attemptTimes.Count); // Initial + 3 retries

        // Verify delays are approximately 1s, 2s, 4s
        // Allow 500ms tolerance for test execution overhead
        var delay1 = (attemptTimes[1] - attemptTimes[0]).TotalMilliseconds;
        var delay2 = (attemptTimes[2] - attemptTimes[1]).TotalMilliseconds;
        var delay3 = (attemptTimes[3] - attemptTimes[2]).TotalMilliseconds;

        Assert.InRange(delay1, 500, 1500);   // ~1s
        Assert.InRange(delay2, 1500, 2500);  // ~2s
        Assert.InRange(delay3, 3500, 4500);  // ~4s
    }

    [Fact]
    public async Task RetryPolicy_LogsEachRetryAttempt()
    {
        // Arrange
        var policy = AgentCallPolicy.CreateRetryPolicy<string>(_mockLogger.Object, _correlationId);
        var attemptCount = 0;

        // Act
        try
        {
            await policy.ExecuteAsync(async () =>
            {
                attemptCount++;
                throw new HttpRequestException("Test error");
            });
        }
        catch (HttpRequestException)
        {
            // Expected
        }

        // Assert - 3 retry log entries (not the initial attempt)
        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(level => level == LogLevel.Warning),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Retry attempt")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(3));
    }

    [Fact]
    public async Task TimeoutPolicy_EnforcesTimeout()
    {
        // Arrange
        var timeoutPolicy = AgentCallPolicy.CreateTimeoutPolicy(TimeSpan.FromMilliseconds(100));

        // Act & Assert
        await Assert.ThrowsAsync<TimeoutRejectedException>(async () =>
        {
            await timeoutPolicy.ExecuteAsync(async () =>
            {
                await Task.Delay(500);
            });
        });
    }

    [Fact]
    public async Task TimeoutPolicy_AllowsOperationWithinTimeout()
    {
        // Arrange
        var timeoutPolicy = AgentCallPolicy.CreateTimeoutPolicy(TimeSpan.FromSeconds(1));

        // Act
        await timeoutPolicy.ExecuteAsync(async () =>
        {
            await Task.Delay(50);
        });

        // Assert - No exception thrown
    }

    [Fact]
    public async Task CombinedPolicy_AppliesTimeoutPerRetry()
    {
        // Arrange
        var policy = AgentCallPolicy.CreateCombinedPolicy<string>(
            _mockLogger.Object,
            _correlationId,
            TimeSpan.FromMilliseconds(100));

        var attemptCount = 0;

        // Act & Assert
        await Assert.ThrowsAsync<TimeoutRejectedException>(async () =>
        {
            await policy.ExecuteAsync(async () =>
            {
                attemptCount++;
                // Each attempt times out
                await Task.Delay(200);
                return "Success";
            });
        });

        // Should retry 3 times before giving up
        Assert.Equal(4, attemptCount); // Initial + 3 retries
    }

    [Fact]
    public async Task RetryPolicy_IncludesCorrelationIdInLogs()
    {
        // Arrange
        var testCorrelationId = "test-correlation-123";
        var policy = AgentCallPolicy.CreateRetryPolicy<string>(_mockLogger.Object, testCorrelationId);

        // Act
        try
        {
            await policy.ExecuteAsync(async () =>
            {
                throw new HttpRequestException("Test");
            });
        }
        catch
        {
            // Expected
        }

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(testCorrelationId)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce());
    }
}
