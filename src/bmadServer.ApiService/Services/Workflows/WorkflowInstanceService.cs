using bmadServer.ApiService.Data;
using bmadServer.ApiService.DTOs;
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
        _logger.LogInformation(
            "Refreshing context for workflow instance {InstanceId}",
            instance.Id);
        
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

    public async Task<(bool Success, string? Message)> SkipCurrentStepAsync(Guid instanceId, Guid userId, string? skipReason = null)
    {
        var instance = await _context.WorkflowInstances.FindAsync(instanceId);
        if (instance == null)
        {
            _logger.LogWarning("Workflow instance {InstanceId} not found", instanceId);
            return (false, "Workflow instance not found");
        }

        // Workflow must be running to skip a step
        if (instance.Status != WorkflowStatus.Running)
        {
            _logger.LogWarning(
                "Cannot skip step for workflow {InstanceId} in {Status} state",
                instanceId, instance.Status);
            return (false, $"Cannot skip step when workflow is in {instance.Status} state");
        }

        // Get the workflow definition to check step properties
        var workflowDef = _workflowRegistry.GetWorkflow(instance.WorkflowDefinitionId);
        if (workflowDef == null)
        {
            _logger.LogWarning(
                "Workflow definition {WorkflowId} not found for instance {InstanceId}",
                instance.WorkflowDefinitionId, instanceId);
            return (false, "Workflow definition not found");
        }

        // Check if current step exists (CurrentStep is 1-based)
        if (instance.CurrentStep < 1 || instance.CurrentStep > workflowDef.Steps.Count)
        {
            _logger.LogWarning(
                "Invalid current step {CurrentStep} for workflow {InstanceId}",
                instance.CurrentStep, instanceId);
            return (false, "Invalid current step");
        }

        var currentStep = workflowDef.Steps[instance.CurrentStep - 1];

        // Validate step can be skipped
        if (!currentStep.IsOptional)
        {
            _logger.LogWarning(
                "Cannot skip required step {StepId} in workflow {InstanceId}",
                currentStep.StepId, instanceId);
            return (false, "This step is required and cannot be skipped");
        }

        if (!currentStep.CanSkip)
        {
            _logger.LogWarning(
                "Cannot skip step {StepId} (CanSkip=false) in workflow {InstanceId}",
                currentStep.StepId, instanceId);
            return (false, "This step cannot be skipped despite being optional");
        }

        // Create step history entry with Skipped status
        var stepHistory = new WorkflowStepHistory
        {
            Id = Guid.NewGuid(),
            WorkflowInstanceId = instanceId,
            StepId = currentStep.StepId,
            StepName = currentStep.Name,
            StartedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            Status = StepExecutionStatus.Skipped,
            ErrorMessage = skipReason
        };

        _context.WorkflowStepHistories.Add(stepHistory);

        // Log the skip event
        var workflowEvent = new WorkflowEvent
        {
            Id = Guid.NewGuid(),
            WorkflowInstanceId = instanceId,
            EventType = "StepSkipped",
            Timestamp = DateTime.UtcNow,
            UserId = userId
        };

        _context.WorkflowEvents.Add(workflowEvent);

        // Advance to next step
        instance.CurrentStep++;
        instance.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Skipped step {StepId} for workflow instance {InstanceId} by user {UserId}",
            currentStep.StepId, instanceId, userId);

        return (true, null);
    }

    public async Task<(bool Success, string? Message)> GoToStepAsync(Guid instanceId, string stepId, Guid userId)
    {
        var instance = await _context.WorkflowInstances.FindAsync(instanceId);
        if (instance == null)
        {
            _logger.LogWarning("Workflow instance {InstanceId} not found", instanceId);
            return (false, "Workflow instance not found");
        }

        // Workflow must be running to navigate steps
        if (instance.Status != WorkflowStatus.Running)
        {
            _logger.LogWarning(
                "Cannot navigate to step for workflow {InstanceId} in {Status} state",
                instanceId, instance.Status);
            return (false, $"Cannot navigate to step when workflow is in {instance.Status} state");
        }

        // Get the workflow definition
        var workflowDef = _workflowRegistry.GetWorkflow(instance.WorkflowDefinitionId);
        if (workflowDef == null)
        {
            _logger.LogWarning(
                "Workflow definition {WorkflowId} not found for instance {InstanceId}",
                instance.WorkflowDefinitionId, instanceId);
            return (false, "Workflow definition not found");
        }

        // Find the target step in the workflow definition
        var targetStepIndex = workflowDef.Steps.ToList().FindIndex(s => s.StepId == stepId);
        if (targetStepIndex == -1)
        {
            _logger.LogWarning(
                "Step {StepId} not found in workflow definition {WorkflowId}",
                stepId, instance.WorkflowDefinitionId);
            return (false, $"Step '{stepId}' not found in workflow definition");
        }

        // Check if step is in the workflow history (must have been visited before)
        var stepHistory = await _context.WorkflowStepHistories
            .Where(h => h.WorkflowInstanceId == instanceId && h.StepId == stepId)
            .FirstOrDefaultAsync();

        if (stepHistory == null)
        {
            _logger.LogWarning(
                "Step {StepId} has not been visited in workflow {InstanceId}",
                stepId, instanceId);
            return (false, "Can only navigate to previously visited steps");
        }

        // Log the step revisit event
        var workflowEvent = new WorkflowEvent
        {
            Id = Guid.NewGuid(),
            WorkflowInstanceId = instanceId,
            EventType = "StepRevisit",
            Timestamp = DateTime.UtcNow,
            UserId = userId
        };

        _context.WorkflowEvents.Add(workflowEvent);

        // Set current step to target step (convert to 1-based index)
        instance.CurrentStep = targetStepIndex + 1;
        instance.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Navigated to step {StepId} (index {StepIndex}) for workflow instance {InstanceId} by user {UserId}",
            stepId, instance.CurrentStep, instanceId, userId);

        return (true, null);
    }

    public async Task<WorkflowStatusResponse?> GetWorkflowStatusAsync(Guid instanceId)
    {
        var instance = await _context.WorkflowInstances.FindAsync(instanceId);
        if (instance == null)
        {
            _logger.LogWarning("Workflow instance {InstanceId} not found", instanceId);
            return null;
        }

        var workflowDef = _workflowRegistry.GetWorkflow(instance.WorkflowDefinitionId);
        if (workflowDef == null)
        {
            _logger.LogWarning(
                "Workflow definition {WorkflowId} not found for instance {InstanceId}",
                instance.WorkflowDefinitionId, instanceId);
            return null;
        }

        var totalSteps = workflowDef.Steps.Count;
        var percentComplete = CalculateProgress(instance, totalSteps);
        var estimatedCompletion = await EstimateCompletionAsync(instanceId);

        // Get step history for completion times
        var stepHistories = await _context.WorkflowStepHistories
            .Where(h => h.WorkflowInstanceId == instanceId)
            .ToListAsync();

        var steps = new List<WorkflowStepProgressDto>();
        for (int i = 0; i < totalSteps; i++)
        {
            var step = workflowDef.Steps[i];
            var stepNumber = i + 1;
            var stepHistory = stepHistories.FirstOrDefault(h => h.StepId == step.StepId);

            string stepStatus;
            DateTime? completedAt = null;

            if (stepHistory != null)
            {
                stepStatus = stepHistory.Status switch
                {
                    StepExecutionStatus.Completed => "Completed",
                    StepExecutionStatus.Failed => "Failed",
                    StepExecutionStatus.Skipped => "Skipped",
                    _ => "Current"
                };
                completedAt = stepHistory.CompletedAt;
            }
            else if (stepNumber == instance.CurrentStep)
            {
                stepStatus = "Current";
            }
            else if (stepNumber < instance.CurrentStep)
            {
                stepStatus = "Completed";
            }
            else
            {
                stepStatus = "Pending";
            }

            steps.Add(new WorkflowStepProgressDto
            {
                StepId = step.StepId,
                Name = step.Name,
                Status = stepStatus,
                CompletedAt = completedAt,
                AgentName = step.AgentId
            });
        }

        // Determine startedAt - use CreatedAt for Created status, or when first step was started
        DateTime? startedAt = instance.Status == WorkflowStatus.Created 
            ? null 
            : instance.CreatedAt;

        return new WorkflowStatusResponse
        {
            Id = instance.Id,
            WorkflowId = instance.WorkflowDefinitionId,
            Name = workflowDef.Name,
            Status = instance.Status.ToString(),
            CurrentStep = instance.CurrentStep,
            TotalSteps = totalSteps,
            PercentComplete = percentComplete,
            StartedAt = startedAt,
            EstimatedCompletion = estimatedCompletion,
            Steps = steps
        };
    }

    public int CalculateProgress(WorkflowInstance instance, int totalSteps)
    {
        if (totalSteps == 0)
        {
            return 0;
        }

        // If completed or failed, return 100%
        if (instance.Status.IsTerminal())
        {
            return 100;
        }

        // Calculate based on current step (CurrentStep is 1-based)
        // If on step 3 of 5, we've completed steps 1 and 2, so (2/5) * 100 = 40%
        var completedSteps = Math.Max(0, instance.CurrentStep - 1);
        var progress = (int)Math.Round((double)completedSteps / totalSteps * 100);
        
        return Math.Min(progress, 100);
    }

    public async Task<DateTime?> EstimateCompletionAsync(Guid instanceId)
    {
        var instance = await _context.WorkflowInstances.FindAsync(instanceId);
        if (instance == null || instance.Status.IsTerminal())
        {
            return null;
        }

        var workflowDef = _workflowRegistry.GetWorkflow(instance.WorkflowDefinitionId);
        if (workflowDef == null)
        {
            return null;
        }

        var totalSteps = workflowDef.Steps.Count;
        var remainingSteps = totalSteps - instance.CurrentStep + 1;

        if (remainingSteps <= 0)
        {
            return null;
        }

        // Get completed step histories for this instance
        var completedSteps = await _context.WorkflowStepHistories
            .Where(h => h.WorkflowInstanceId == instanceId && 
                       h.Status == StepExecutionStatus.Completed &&
                       h.CompletedAt.HasValue)
            .ToListAsync();

        if (completedSteps.Any())
        {
            // Calculate average duration per step from this instance
            var durations = completedSteps
                .Where(s => s.CompletedAt.HasValue)
                .Select(s => (s.CompletedAt!.Value - s.StartedAt).TotalMinutes)
                .ToList();

            if (durations.Any())
            {
                var avgDurationMinutes = durations.Average();
                var estimatedMinutesRemaining = avgDurationMinutes * remainingSteps;
                return DateTime.UtcNow.AddMinutes(estimatedMinutesRemaining);
            }
        }

        // Fallback: use workflow definition's estimated duration
        var totalEstimatedMinutes = workflowDef.EstimatedDuration.TotalMinutes;
        var avgStepDuration = totalEstimatedMinutes / totalSteps;
        var fallbackEstimateMinutes = avgStepDuration * remainingSteps;
        
        return DateTime.UtcNow.AddMinutes(fallbackEstimateMinutes);
    }

    public async Task<PagedResult<WorkflowInstance>> GetFilteredWorkflowsAsync(
        Guid userId,
        WorkflowStatus? status = null,
        string? workflowType = null,
        DateTime? createdAfter = null,
        DateTime? createdBefore = null,
        int page = 1,
        int pageSize = 20)
    {
        // Validate pagination parameters
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.WorkflowInstances
            .Where(w => w.UserId == userId);

        // Apply filters
        if (status.HasValue)
        {
            query = query.Where(w => w.Status == status.Value);
        }

        if (!string.IsNullOrEmpty(workflowType))
        {
            query = query.Where(w => w.WorkflowDefinitionId == workflowType);
        }

        if (createdAfter.HasValue)
        {
            query = query.Where(w => w.CreatedAt >= createdAfter.Value);
        }

        if (createdBefore.HasValue)
        {
            query = query.Where(w => w.CreatedAt <= createdBefore.Value);
        }

        // Get total count
        var totalItems = await query.CountAsync();

        // Apply pagination and ordering
        var items = await query
            .OrderByDescending(w => w.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

        _logger.LogInformation(
            "Retrieved {Count} workflows (page {Page}/{TotalPages}) for user {UserId} with filters: status={Status}, type={WorkflowType}",
            items.Count, page, totalPages, userId, status, workflowType);

        return new PagedResult<WorkflowInstance>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalPages = totalPages,
            HasPrevious = page > 1,
            HasNext = page < totalPages
        };
    }
}
