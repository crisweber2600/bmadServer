using bmadServer.ApiService.Data;
using bmadServer.ApiService.Models.Workflows;
using bmadServer.ServiceDefaults.Services.Workflows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace bmadServer.ApiService.Services.Workflows;

public class WorkflowInstanceService : IWorkflowInstanceService
{
    private readonly ApplicationDbContext _context;
    private readonly IWorkflowRegistry _workflowRegistry;
    private readonly ILogger<WorkflowInstanceService> _logger;

    public WorkflowInstanceService(
        ApplicationDbContext context,
        IWorkflowRegistry workflowRegistry,
        ILogger<WorkflowInstanceService> logger)
    {
        _context = context;
        _workflowRegistry = workflowRegistry;
        _logger = logger;
    }

    public async Task<WorkflowInstance> CreateWorkflowInstanceAsync(
        string workflowId, 
        Guid userId, 
        Dictionary<string, object> initialContext)
    {
        if (!_workflowRegistry.ValidateWorkflow(workflowId))
        {
            _logger.LogWarning("Attempted to create instance for non-existent workflow: {WorkflowId}", workflowId);
            throw new ArgumentException($"Workflow '{workflowId}' not found in registry", nameof(workflowId));
        }

        var instance = new WorkflowInstance
        {
            Id = Guid.NewGuid(),
            WorkflowDefinitionId = workflowId,
            UserId = userId,
            Status = WorkflowStatus.Created,
            CurrentStep = 0,
            Context = initialContext.Any() 
                ? JsonDocument.Parse(JsonSerializer.Serialize(initialContext))
                : null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.WorkflowInstances.Add(instance);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Created workflow instance {InstanceId} for workflow {WorkflowId} and user {UserId}",
            instance.Id, workflowId, userId);

        return instance;
    }

    public async Task<bool> StartWorkflowAsync(Guid instanceId)
    {
        var instance = await _context.WorkflowInstances.FindAsync(instanceId);
        if (instance == null)
        {
            _logger.LogWarning("Workflow instance {InstanceId} not found", instanceId);
            return false;
        }

        var success = await TransitionStateAsync(instanceId, WorkflowStatus.Running);
        if (success)
        {
            instance.CurrentStep = 1;
            instance.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Started workflow instance {InstanceId}", instanceId);
        }

        return success;
    }

    public async Task<bool> TransitionStateAsync(Guid instanceId, WorkflowStatus newStatus)
    {
        var instance = await _context.WorkflowInstances.FindAsync(instanceId);
        if (instance == null)
        {
            _logger.LogWarning("Workflow instance {InstanceId} not found", instanceId);
            return false;
        }

        if (!WorkflowStatusExtensions.ValidateTransition(instance.Status, newStatus))
        {
            _logger.LogWarning(
                "Invalid state transition for workflow instance {InstanceId}: {OldStatus} -> {NewStatus}",
                instanceId, instance.Status, newStatus);
            return false;
        }

        var oldStatus = instance.Status;
        instance.Status = newStatus;
        instance.UpdatedAt = DateTime.UtcNow;

        // Log the state transition event
        var workflowEvent = new WorkflowEvent
        {
            Id = Guid.NewGuid(),
            WorkflowInstanceId = instanceId,
            EventType = "StateTransition",
            OldStatus = oldStatus,
            NewStatus = newStatus,
            Timestamp = DateTime.UtcNow,
            UserId = instance.UserId
        };

        _context.WorkflowEvents.Add(workflowEvent);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Workflow instance {InstanceId} transitioned from {OldStatus} to {NewStatus}",
            instanceId, oldStatus, newStatus);

        return true;
    }

    public async Task<WorkflowInstance?> GetWorkflowInstanceAsync(Guid instanceId)
    {
        return await _context.WorkflowInstances.FindAsync(instanceId);
    }

    public async Task<(bool Success, string? Message)> PauseWorkflowAsync(Guid instanceId, Guid userId)
    {
        var instance = await _context.WorkflowInstances.FindAsync(instanceId);
        if (instance == null)
        {
            _logger.LogWarning("Workflow instance {InstanceId} not found", instanceId);
            return (false, "Workflow instance not found");
        }

        // Check if workflow is already paused
        if (instance.Status == WorkflowStatus.Paused)
        {
            _logger.LogWarning(
                "Attempted to pause already paused workflow instance {InstanceId}",
                instanceId);
            return (false, "Workflow is already paused");
        }

        // Validate transition to Paused state
        if (!WorkflowStatusExtensions.ValidateTransition(instance.Status, WorkflowStatus.Paused))
        {
            _logger.LogWarning(
                "Invalid state transition for workflow instance {InstanceId}: {OldStatus} -> Paused",
                instanceId, instance.Status);
            return (false, $"Cannot pause workflow in {instance.Status} state");
        }

        var oldStatus = instance.Status;
        instance.Status = WorkflowStatus.Paused;
        instance.PausedAt = DateTime.UtcNow;
        instance.UpdatedAt = DateTime.UtcNow;

        // Log the pause event
        var workflowEvent = new WorkflowEvent
        {
            Id = Guid.NewGuid(),
            WorkflowInstanceId = instanceId,
            EventType = "WorkflowPaused",
            OldStatus = oldStatus,
            NewStatus = WorkflowStatus.Paused,
            Timestamp = DateTime.UtcNow,
            UserId = userId
        };

        _context.WorkflowEvents.Add(workflowEvent);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Workflow instance {InstanceId} paused by user {UserId}",
            instanceId, userId);

        return (true, null);
    }

    public async Task<(bool Success, string? Message)> ResumeWorkflowAsync(Guid instanceId, Guid userId)
    {
        var instance = await _context.WorkflowInstances.FindAsync(instanceId);
        if (instance == null)
        {
            _logger.LogWarning("Workflow instance {InstanceId} not found", instanceId);
            return (false, "Workflow instance not found");
        }

        // Cannot resume cancelled workflows
        if (instance.Status == WorkflowStatus.Cancelled)
        {
            _logger.LogWarning(
                "Cannot resume cancelled workflow instance {InstanceId}",
                instanceId);
            return (false, "Cannot resume a cancelled workflow");
        }

        // Validate transition from Paused to Running
        if (!WorkflowStatusExtensions.ValidateTransition(instance.Status, WorkflowStatus.Running))
        {
            _logger.LogWarning(
                "Invalid state transition for workflow instance {InstanceId}: {OldStatus} -> Running",
                instanceId, instance.Status);
            return (false, $"Cannot resume workflow in {instance.Status} state");
        }

        // Check if context refresh is needed (paused for >24 hours)
        string? message = null;
        if (instance.PausedAt.HasValue)
        {
            var pauseDuration = DateTime.UtcNow - instance.PausedAt.Value;
            if (pauseDuration > TimeSpan.FromHours(24))
            {
                await RefreshWorkflowContextAsync(instance);
                message = "Workflow resumed. Context has been refreshed.";
                _logger.LogInformation(
                    "Context refreshed for workflow instance {InstanceId} (paused for {Duration})",
                    instanceId, pauseDuration);
            }
        }

        var oldStatus = instance.Status;
        instance.Status = WorkflowStatus.Running;
        instance.UpdatedAt = DateTime.UtcNow;
        // Keep PausedAt for historical tracking, but we could also clear it if needed

        // Log the resume event
        var workflowEvent = new WorkflowEvent
        {
            Id = Guid.NewGuid(),
            WorkflowInstanceId = instanceId,
            EventType = "WorkflowResumed",
            OldStatus = oldStatus,
            NewStatus = WorkflowStatus.Running,
            Timestamp = DateTime.UtcNow,
            UserId = userId
        };

        _context.WorkflowEvents.Add(workflowEvent);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Workflow instance {InstanceId} resumed by user {UserId}",
            instanceId, userId);

        return (true, message);
    }

    public async Task<(bool Success, string? Message)> CancelWorkflowAsync(Guid instanceId, Guid userId)
    {
        var instance = await _context.WorkflowInstances.FindAsync(instanceId);
        if (instance == null)
        {
            _logger.LogWarning("Workflow instance {InstanceId} not found", instanceId);
            return (false, "Workflow instance not found");
        }

        // Cannot cancel workflows in terminal states
        if (instance.Status == WorkflowStatus.Completed)
        {
            _logger.LogWarning(
                "Attempted to cancel completed workflow instance {InstanceId}",
                instanceId);
            return (false, "Cannot cancel a completed workflow");
        }

        if (instance.Status == WorkflowStatus.Failed)
        {
            _logger.LogWarning(
                "Attempted to cancel failed workflow instance {InstanceId}",
                instanceId);
            return (false, "Cannot cancel a failed workflow");
        }

        if (instance.Status == WorkflowStatus.Cancelled)
        {
            _logger.LogWarning(
                "Attempted to cancel already cancelled workflow instance {InstanceId}",
                instanceId);
            return (false, "Workflow is already cancelled");
        }

        // Validate transition to Cancelled state
        if (!WorkflowStatusExtensions.ValidateTransition(instance.Status, WorkflowStatus.Cancelled))
        {
            _logger.LogWarning(
                "Invalid state transition for workflow instance {InstanceId}: {OldStatus} -> Cancelled",
                instanceId, instance.Status);
            return (false, $"Cannot cancel workflow in {instance.Status} state");
        }

        var oldStatus = instance.Status;
        instance.Status = WorkflowStatus.Cancelled;
        instance.CancelledAt = DateTime.UtcNow;
        instance.UpdatedAt = DateTime.UtcNow;

        // Log the cancellation event
        var workflowEvent = new WorkflowEvent
        {
            Id = Guid.NewGuid(),
            WorkflowInstanceId = instanceId,
            EventType = "WorkflowCancelled",
            OldStatus = oldStatus,
            NewStatus = WorkflowStatus.Cancelled,
            Timestamp = DateTime.UtcNow,
            UserId = userId
        };

        _context.WorkflowEvents.Add(workflowEvent);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Workflow instance {InstanceId} cancelled by user {UserId}",
            instanceId, userId);

        return (true, null);
    }

    private async Task RefreshWorkflowContextAsync(WorkflowInstance instance)
    {
        // Context refresh logic - reload any stale data
        // This is a placeholder implementation that could be extended based on specific needs
        // For now, we just log that a refresh occurred
        _logger.LogInformation(
            "Refreshing context for workflow instance {InstanceId}",
            instance.Id);
        
        // In a real implementation, this might:
        // - Reload external data that might have changed
        // - Validate that referenced entities still exist
        // - Update cached values in the context
        
        await Task.CompletedTask;
    }

    public async Task<List<WorkflowInstance>> GetWorkflowInstancesAsync(Guid userId, bool showCancelled = false)
    {
        var query = _context.WorkflowInstances
            .Where(w => w.UserId == userId);

        if (!showCancelled)
        {
            query = query.Where(w => w.Status != WorkflowStatus.Cancelled);
        }

        var workflows = await query
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync();

        _logger.LogInformation(
            "Retrieved {Count} workflows for user {UserId} (showCancelled: {ShowCancelled})",
            workflows.Count, userId, showCancelled);

        return workflows;
    }
}
