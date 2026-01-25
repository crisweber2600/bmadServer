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
}
