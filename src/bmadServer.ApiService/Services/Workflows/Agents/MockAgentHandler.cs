using bmadServer.ApiService.Services.Workflows.Agents;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace bmadServer.ApiService.Services.Workflows.Agents;

/// <summary>
/// Mock agent handler for testing purposes
/// </summary>
public class MockAgentHandler : IAgentHandler
{
    private readonly Func<AgentContext, Task<AgentResult>>? _executeFunc;
    private readonly bool _shouldSucceed;
    private readonly bool _isRetryable;
    private readonly string? _errorMessage;
    private readonly int _delaySeconds;

    public MockAgentHandler(
        bool shouldSucceed = true, 
        bool isRetryable = false,
        string? errorMessage = null,
        int delaySeconds = 0,
        Func<AgentContext, Task<AgentResult>>? executeFunc = null)
    {
        _shouldSucceed = shouldSucceed;
        _isRetryable = isRetryable;
        _errorMessage = errorMessage;
        _delaySeconds = delaySeconds;
        _executeFunc = executeFunc;
    }

    public async Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken cancellationToken = default)
    {
        if (_executeFunc != null)
        {
            return await _executeFunc(context);
        }

        if (_delaySeconds > 0)
        {
            await Task.Delay(TimeSpan.FromSeconds(_delaySeconds), cancellationToken);
        }

        if (_shouldSucceed)
        {
            var output = JsonDocument.Parse(JsonSerializer.Serialize(new
            {
                message = "Step completed successfully",
                processedInput = context.UserInput ?? "no input",
                timestamp = DateTime.UtcNow
            }));

            return new AgentResult
            {
                Success = true,
                Output = output
            };
        }
        else
        {
            return new AgentResult
            {
                Success = false,
                ErrorMessage = _errorMessage ?? "Mock agent failed",
                IsRetryable = _isRetryable
            };
        }
    }

    public async IAsyncEnumerable<StepProgress> ExecuteWithStreamingAsync(
        AgentContext context, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        for (int i = 0; i <= 100; i += 10)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                yield break;
            }

            yield return new StepProgress
            {
                Message = $"Processing step {context.StepName}... {i}%",
                PercentComplete = i
            };

            await Task.Delay(500, cancellationToken);
        }

        yield return new StepProgress
        {
            Message = "Step completed",
            PercentComplete = 100
        };
    }
}
