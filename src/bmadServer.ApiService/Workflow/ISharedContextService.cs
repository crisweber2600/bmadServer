using bmadServer.ApiService.Data;
using bmadServer.ApiService.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace bmadServer.ApiService.WorkflowContext;

/// <summary>
/// Service for managing shared workflow context with database persistence.
/// Implements optimistic concurrency control.
/// </summary>
public interface ISharedContextService
{
    /// <summary>
    /// Gets the shared context for a workflow instance.
    /// </summary>
    Task<SharedContext?> GetContextAsync(Guid workflowInstanceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new shared context for a workflow instance.
    /// </summary>
    Task<SharedContext> CreateContextAsync(Guid workflowInstanceId, UserPreferences? preferences = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the shared context with optimistic concurrency control.
    /// </summary>
    /// <exception cref="DbUpdateConcurrencyException">Thrown when version mismatch is detected.</exception>
    Task<SharedContext> UpdateContextAsync(SharedContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a step output to the context.
    /// </summary>
    Task AddStepOutputAsync(Guid workflowInstanceId, string stepId, StepOutput output, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of the shared context service with database persistence.
/// </summary>
public class SharedContextService : ISharedContextService
{
    private readonly ApplicationDbContext _context;

    public SharedContextService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SharedContext?> GetContextAsync(Guid workflowInstanceId, CancellationToken cancellationToken = default)
    {
        var entity = await _context.WorkflowContexts
            .FirstOrDefaultAsync(wc => wc.WorkflowInstanceId == workflowInstanceId, cancellationToken);

        return entity?.ToSharedContext();
    }

    public async Task<SharedContext> CreateContextAsync(Guid workflowInstanceId, UserPreferences? preferences = null, CancellationToken cancellationToken = default)
    {
        var sharedContext = new SharedContext
        {
            WorkflowInstanceId = workflowInstanceId,
            UserPreferences = preferences
        };

        var entity = WorkflowContextEntity.FromSharedContext(sharedContext);
        _context.WorkflowContexts.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return sharedContext;
    }

    public async Task<SharedContext> UpdateContextAsync(SharedContext context, CancellationToken cancellationToken = default)
    {
        var entity = await _context.WorkflowContexts
            .FirstOrDefaultAsync(wc => wc.WorkflowInstanceId == context.WorkflowInstanceId, cancellationToken);

        if (entity == null)
        {
            throw new InvalidOperationException($"Workflow context not found for instance {context.WorkflowInstanceId}");
        }

        // Check version for optimistic concurrency
        if (entity.Version != context.Version - 1)
        {
            throw new DbUpdateConcurrencyException("Version mismatch detected. Context was modified by another process.");
        }

        entity.UpdateFromSharedContext(context);
        await _context.SaveChangesAsync(cancellationToken);

        return context;
    }

    public async Task AddStepOutputAsync(Guid workflowInstanceId, string stepId, StepOutput output, CancellationToken cancellationToken = default)
    {
        var context = await GetContextAsync(workflowInstanceId, cancellationToken);
        if (context == null)
        {
            throw new InvalidOperationException($"Workflow context not found for instance {workflowInstanceId}");
        }

        context.AddStepOutput(stepId, output);
        await UpdateContextAsync(context, cancellationToken);
    }
}
