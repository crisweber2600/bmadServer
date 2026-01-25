using System.Text.Json;
using bmadServer.ApiService.Models.Agents;
using Microsoft.Extensions.Logging;

namespace bmadServer.ApiService.Services.Workflows.Agents;

/// <summary>
/// In-memory shared context service with optimistic concurrency control
/// </summary>
public class SharedContextService : ISharedContextService
{
    private readonly Dictionary<Guid, SharedContext> _contexts = new();
    private readonly ILogger<SharedContextService> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public SharedContextService(ILogger<SharedContextService> logger)
    {
        _logger = logger;
    }

    public async Task<SharedContext?> GetContextAsync(Guid workflowInstanceId, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            _contexts.TryGetValue(workflowInstanceId, out var context);
            return context;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<SharedContext> CreateContextAsync(Guid workflowInstanceId, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_contexts.ContainsKey(workflowInstanceId))
            {
                _logger.LogWarning("Context for workflow {WorkflowInstanceId} already exists", workflowInstanceId);
                return _contexts[workflowInstanceId];
            }

            var context = new SharedContext
            {
                WorkflowInstanceId = workflowInstanceId,
                Version = 1
            };

            _contexts[workflowInstanceId] = context;
            _logger.LogInformation("Created shared context for workflow {WorkflowInstanceId}", workflowInstanceId);

            return context;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<JsonDocument?> GetStepOutputAsync(Guid workflowInstanceId, string stepId, CancellationToken cancellationToken = default)
    {
        var context = await GetContextAsync(workflowInstanceId, cancellationToken);
        if (context == null)
        {
            _logger.LogWarning("No context found for workflow {WorkflowInstanceId}", workflowInstanceId);
            return null;
        }

        context.StepOutputs.TryGetValue(stepId, out var output);
        return output;
    }

    public async Task<bool> AddStepOutputAsync(Guid workflowInstanceId, string stepId, JsonDocument output, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var context = await GetContextAsync(workflowInstanceId, cancellationToken);
            if (context == null)
            {
                _logger.LogError("No context found for workflow {WorkflowInstanceId}", workflowInstanceId);
                return false;
            }

            context.StepOutputs[stepId] = output;
            context.Version++;
            context.LastUpdated = DateTime.UtcNow;

            _logger.LogInformation(
                "Added step output for step {StepId} in workflow {WorkflowInstanceId}",
                stepId,
                workflowInstanceId);

            return true;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<bool> AddDecisionAsync(Guid workflowInstanceId, DecisionRecord decision, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var context = await GetContextAsync(workflowInstanceId, cancellationToken);
            if (context == null)
            {
                _logger.LogError("No context found for workflow {WorkflowInstanceId}", workflowInstanceId);
                return false;
            }

            context.DecisionHistory.Add(decision);
            context.Version++;
            context.LastUpdated = DateTime.UtcNow;

            _logger.LogInformation(
                "Added decision {DecisionId} by agent {AgentId} in workflow {WorkflowInstanceId}",
                decision.DecisionId,
                decision.AgentId,
                workflowInstanceId);

            return true;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<bool> UpdateContextAsync(SharedContext context, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (!_contexts.TryGetValue(context.WorkflowInstanceId, out var existingContext))
            {
                _logger.LogError("No context found for workflow {WorkflowInstanceId}", context.WorkflowInstanceId);
                return false;
            }

            // Optimistic concurrency check
            if (existingContext.Version != context.Version)
            {
                _logger.LogWarning(
                    "Version conflict for workflow {WorkflowInstanceId}. Expected: {ExpectedVersion}, Actual: {ActualVersion}",
                    context.WorkflowInstanceId,
                    context.Version,
                    existingContext.Version);
                return false;
            }

            // Update context
            context.Version++;
            context.LastUpdated = DateTime.UtcNow;
            _contexts[context.WorkflowInstanceId] = context;

            _logger.LogInformation(
                "Updated context for workflow {WorkflowInstanceId} to version {Version}",
                context.WorkflowInstanceId,
                context.Version);

            return true;
        }
        finally
        {
            _lock.Release();
        }
    }
}
