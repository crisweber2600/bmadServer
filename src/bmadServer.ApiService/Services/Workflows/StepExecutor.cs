using bmadServer.ApiService.Data;
using bmadServer.ApiService.Hubs;
using bmadServer.ApiService.Models.Workflows;
using bmadServer.ApiService.Services.Workflows.Agents;
using bmadServer.ServiceDefaults.Services.Workflows;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NJsonSchema;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace bmadServer.ApiService.Services.Workflows;

/// <summary>
/// Service for executing workflow steps with agent routing and validation
/// </summary>
public class StepExecutor : IStepExecutor
{
    private readonly ApplicationDbContext _context;
    private readonly IAgentRouter _agentRouter;
    private readonly IWorkflowRegistry _workflowRegistry;
    private readonly IWorkflowInstanceService _workflowInstanceService;
    private readonly ISharedContextService _sharedContextService;
    private readonly IAgentHandoffService _agentHandoffService;
    private readonly IApprovalService _approvalService;
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly ILogger<StepExecutor> _logger;
    
    private const int StreamingThresholdSeconds = 5;

    public StepExecutor(
        ApplicationDbContext context,
        IAgentRouter agentRouter,
        IWorkflowRegistry workflowRegistry,
        IWorkflowInstanceService workflowInstanceService,
        ISharedContextService sharedContextService,
        IAgentHandoffService agentHandoffService,
        IApprovalService approvalService,
        IHubContext<ChatHub> hubContext,
        ILogger<StepExecutor> logger)
    {
        _context = context;
        _agentRouter = agentRouter;
        _workflowRegistry = workflowRegistry;
        _workflowInstanceService = workflowInstanceService;
        _sharedContextService = sharedContextService;
        _agentHandoffService = agentHandoffService;
        _approvalService = approvalService;
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<StepExecutionResult> ExecuteStepAsync(
        Guid workflowInstanceId, 
        string? userInput = null, 
        CancellationToken cancellationToken = default)
    {
        var instance = await _workflowInstanceService.GetWorkflowInstanceAsync(workflowInstanceId);
        if (instance == null)
        {
            _logger.LogWarning("Workflow instance {InstanceId} not found", workflowInstanceId);
            return new StepExecutionResult
            {
                Success = false,
                Status = StepExecutionStatus.Failed,
                ErrorMessage = "Workflow instance not found"
            };
        }

        // Get workflow definition
        var definition = _workflowRegistry.GetWorkflow(instance.WorkflowDefinitionId);
        if (definition == null)
        {
            _logger.LogError("Workflow definition {WorkflowId} not found", instance.WorkflowDefinitionId);
            return new StepExecutionResult
            {
                Success = false,
                Status = StepExecutionStatus.Failed,
                ErrorMessage = "Workflow definition not found"
            };
        }

        // Get current step (CurrentStep is 1-based)
        if (instance.CurrentStep < 1 || instance.CurrentStep > definition.Steps.Count)
        {
            _logger.LogError("Invalid current step {CurrentStep} for workflow {InstanceId}", 
                instance.CurrentStep, workflowInstanceId);
            return new StepExecutionResult
            {
                Success = false,
                Status = StepExecutionStatus.Failed,
                ErrorMessage = "Invalid current step"
            };
        }

        var step = definition.Steps[instance.CurrentStep - 1];

        // Create step history record
        var stepHistory = new WorkflowStepHistory
        {
            Id = Guid.NewGuid(),
            WorkflowInstanceId = workflowInstanceId,
            StepId = step.StepId,
            StepName = step.Name,
            StartedAt = DateTime.UtcNow,
            Status = StepExecutionStatus.Running,
            Input = userInput != null 
                ? JsonDocument.Parse(JsonSerializer.Serialize(new { userInput }))
                : null
        };

        _context.WorkflowStepHistories.Add(stepHistory);
        await _context.SaveChangesAsync(cancellationToken);

        try
        {
            // Get agent handler
            var handler = _agentRouter.GetHandler(step.AgentId);
            if (handler == null)
            {
                _logger.LogError("No handler found for agent {AgentId} in step {StepId}", 
                    step.AgentId, step.StepId);
                
                await UpdateStepHistoryFailure(stepHistory, "No handler found for agent", cancellationToken);
                
                return new StepExecutionResult
                {
                    Success = false,
                    StepId = step.StepId,
                    StepName = step.Name,
                    Status = StepExecutionStatus.Failed,
                    ErrorMessage = $"No handler found for agent {step.AgentId}"
                };
            }

            // Record agent handoff if agent changed
            var previousStep = instance.CurrentStep > 1 
                ? definition.Steps[instance.CurrentStep - 2] 
                : null;
            
            if (previousStep != null && previousStep.AgentId != step.AgentId)
            {
                try
                {
                    // Generate handoff reason based on step metadata
                    var handoffReason = $"Step requires {step.AgentId} expertise";
                    
                    // Record handoff (non-blocking, logs errors)
                    await _agentHandoffService.RecordHandoffAsync(
                        workflowInstanceId,
                        previousStep.AgentId,
                        step.AgentId,
                        step.StepId,
                        handoffReason,
                        cancellationToken);
                    
                    _logger.LogInformation(
                        "Recorded handoff from {FromAgent} to {ToAgent} for step {StepId}",
                        previousStep.AgentId, step.AgentId, step.StepId);
                    
                    // Emit AGENT_HANDOFF event to connected clients (non-blocking)
                    try
                    {
                        await _hubContext.Clients.All.SendAsync("AGENT_HANDOFF", new
                        {
                            FromAgentId = previousStep.AgentId,
                            ToAgentId = step.AgentId,
                            StepName = step.Name,
                            Timestamp = DateTimeOffset.UtcNow,
                            Message = $"Handing off to {step.AgentId}..."
                        }, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        // Non-blocking SignalR error - log but don't fail workflow
                        _logger.LogWarning(ex, 
                            "Failed to emit AGENT_HANDOFF event for handoff from {FromAgent} to {ToAgent}",
                            previousStep.AgentId, step.AgentId);
                    }
                }
                catch (Exception ex)
                {
                    // Non-blocking error handling - log but continue workflow
                    _logger.LogWarning(ex, 
                        "Failed to record handoff from {FromAgent} to {ToAgent}",
                        previousStep.AgentId, step.AgentId);
                }
            }

            // Load shared context before execution
            var sharedContext = await _sharedContextService.GetContextAsync(workflowInstanceId, cancellationToken);
            
            // Prepare agent context
            var agentContext = PrepareAgentContext(instance, step, userInput, sharedContext);

            // Execute step via agent handler
            var agentResult = await handler.ExecuteAsync(agentContext, cancellationToken);

            if (agentResult.Success)
            {
                // Validate output against schema
                if (!string.IsNullOrWhiteSpace(step.OutputSchema) && agentResult.Output != null)
                {
                    var validationErrors = await ValidateOutputAsync(step.OutputSchema, agentResult.Output);
                    if (validationErrors.Any())
                    {
                        var errorMessage = $"Output validation failed: {string.Join(", ", validationErrors)}";
                        _logger.LogWarning("Step {StepId} output validation failed: {Errors}", 
                            step.StepId, errorMessage);
                        
                        await UpdateStepHistoryFailure(stepHistory, errorMessage, cancellationToken);
                        await _workflowInstanceService.TransitionStateAsync(workflowInstanceId, WorkflowStatus.Failed);
                        
                        return new StepExecutionResult
                        {
                            Success = false,
                            StepId = step.StepId,
                            StepName = step.Name,
                            Status = StepExecutionStatus.Failed,
                            ErrorMessage = errorMessage,
                            NewWorkflowStatus = WorkflowStatus.Failed
                        };
                    }
                }

                // Check if agent result requires human approval (low confidence)
                if (agentResult.RequiresHumanApproval)
                {
                    _logger.LogInformation(
                        "Step {StepId} requires human approval (confidence: {Confidence:F2}) for workflow {InstanceId}",
                        step.StepId, agentResult.ConfidenceScore, workflowInstanceId);

                    // Store proposed response in step history
                    stepHistory.Status = StepExecutionStatus.WaitingForApproval;
                    stepHistory.Output = agentResult.Output;
                    await _context.SaveChangesAsync(cancellationToken);

                    // Store proposed response in StepData for later retrieval
                    var proposedResponseJson = agentResult.Output?.RootElement.GetRawText() ?? "{}";
                    instance.StepData = MergeStepData(
                        instance.StepData, 
                        $"{step.StepId}_pending_approval", 
                        JsonDocument.Parse(JsonSerializer.Serialize(new 
                        { 
                            proposedResponse = proposedResponseJson,
                            confidenceScore = agentResult.ConfidenceScore,
                            reasoning = agentResult.Reasoning
                        })));
                    instance.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync(cancellationToken);

                    // Create approval request
                    var approvalRequest = await _approvalService.CreateApprovalRequestAsync(
                        workflowInstanceId,
                        step.AgentId,
                        step.StepId,
                        proposedResponseJson,
                        agentResult.ConfidenceScore,
                        agentResult.Reasoning,
                        instance.UserId,
                        cancellationToken);

                    // Transition workflow to WaitingForApproval
                    await _workflowInstanceService.TransitionStateAsync(workflowInstanceId, WorkflowStatus.WaitingForApproval);

                    // Emit APPROVAL_REQUIRED SignalR event
                    try
                    {
                        await _hubContext.Clients.All.SendAsync("APPROVAL_REQUIRED", new
                        {
                            ApprovalRequestId = approvalRequest.Id,
                            WorkflowInstanceId = workflowInstanceId,
                            AgentId = step.AgentId,
                            StepId = step.StepId,
                            StepName = step.Name,
                            ProposedResponse = proposedResponseJson,
                            ConfidenceScore = agentResult.ConfidenceScore,
                            Reasoning = agentResult.Reasoning,
                            RequestedAt = approvalRequest.RequestedAt,
                            Message = $"Agent {step.AgentId} needs your approval (confidence: {agentResult.ConfidenceScore:P0})"
                        }, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to emit APPROVAL_REQUIRED event for approval {ApprovalId}", approvalRequest.Id);
                    }

                    return new StepExecutionResult
                    {
                        Success = true,
                        StepId = step.StepId,
                        StepName = step.Name,
                        Status = StepExecutionStatus.WaitingForApproval,
                        NewWorkflowStatus = WorkflowStatus.WaitingForApproval,
                        RequiresApproval = true,
                        PendingApprovalId = approvalRequest.Id
                    };
                }

                // Update step history with success
                stepHistory.CompletedAt = DateTime.UtcNow;
                stepHistory.Status = StepExecutionStatus.Completed;
                stepHistory.Output = agentResult.Output;
                await _context.SaveChangesAsync(cancellationToken);

                // Persist agent output to shared context
                if (agentResult.Output != null)
                {
                    try
                    {
                        await _sharedContextService.AddStepOutputAsync(
                            workflowInstanceId,
                            step.StepId,
                            agentResult.Output,
                            cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to persist step output to shared context for step {StepId}", step.StepId);
                    }
                }

                // Update workflow instance
                var nextStep = instance.CurrentStep + 1;
                var isLastStep = nextStep > definition.Steps.Count;
                
                instance.CurrentStep = nextStep;
                instance.UpdatedAt = DateTime.UtcNow;
                
                // Merge output into StepData
                if (agentResult.Output != null)
                {
                    instance.StepData = MergeStepData(instance.StepData, step.StepId, agentResult.Output);
                }

                await _context.SaveChangesAsync(cancellationToken);

                // Transition workflow state
                WorkflowStatus newStatus;
                if (isLastStep)
                {
                    newStatus = WorkflowStatus.Completed;
                    await _workflowInstanceService.TransitionStateAsync(workflowInstanceId, newStatus);
                }
                else
                {
                    newStatus = instance.Status;
                }

                _logger.LogInformation(
                    "Step {StepId} completed successfully for workflow {InstanceId}", 
                    step.StepId, workflowInstanceId);

                return new StepExecutionResult
                {
                    Success = true,
                    StepId = step.StepId,
                    StepName = step.Name,
                    Status = StepExecutionStatus.Completed,
                    NewWorkflowStatus = newStatus,
                    NextStep = isLastStep ? null : nextStep
                };
            }
            else
            {
                // Agent execution failed
                _logger.LogWarning(
                    "Step {StepId} failed for workflow {InstanceId}: {Error}", 
                    step.StepId, workflowInstanceId, agentResult.ErrorMessage);

                await UpdateStepHistoryFailure(stepHistory, agentResult.ErrorMessage ?? "Unknown error", cancellationToken);

                // Determine workflow status based on whether error is retryable
                var newStatus = agentResult.IsRetryable 
                    ? WorkflowStatus.WaitingForInput 
                    : WorkflowStatus.Failed;
                
                await _workflowInstanceService.TransitionStateAsync(workflowInstanceId, newStatus);

                return new StepExecutionResult
                {
                    Success = false,
                    StepId = step.StepId,
                    StepName = step.Name,
                    Status = StepExecutionStatus.Failed,
                    ErrorMessage = agentResult.ErrorMessage,
                    NewWorkflowStatus = newStatus
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error executing step {StepId} for workflow {InstanceId}", 
                step.StepId, workflowInstanceId);

            await UpdateStepHistoryFailure(stepHistory, $"Unexpected error: {ex.Message}", cancellationToken);
            await _workflowInstanceService.TransitionStateAsync(workflowInstanceId, WorkflowStatus.Failed);

            return new StepExecutionResult
            {
                Success = false,
                StepId = step.StepId,
                StepName = step.Name,
                Status = StepExecutionStatus.Failed,
                ErrorMessage = ex.Message,
                NewWorkflowStatus = WorkflowStatus.Failed
            };
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<StepProgress> ExecuteStepWithStreamingAsync(
        Guid workflowInstanceId, 
        string? userInput = null, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var instance = await _workflowInstanceService.GetWorkflowInstanceAsync(workflowInstanceId);
        if (instance == null)
        {
            yield return new StepProgress
            {
                Message = "Workflow instance not found",
                PercentComplete = 0
            };
            yield break;
        }

        var definition = _workflowRegistry.GetWorkflow(instance.WorkflowDefinitionId);
        if (definition == null || instance.CurrentStep < 1 || instance.CurrentStep > definition.Steps.Count)
        {
            yield return new StepProgress
            {
                Message = "Invalid workflow state",
                PercentComplete = 0
            };
            yield break;
        }

        var step = definition.Steps[instance.CurrentStep - 1];
        var handler = _agentRouter.GetHandler(step.AgentId);
        
        if (handler == null)
        {
            yield return new StepProgress
            {
                Message = $"No handler found for agent {step.AgentId}",
                PercentComplete = 0
            };
            yield break;
        }

        var sharedContext = await _sharedContextService.GetContextAsync(workflowInstanceId, cancellationToken);
        var agentContext = PrepareAgentContext(instance, step, userInput, sharedContext);
        
        var stopwatch = Stopwatch.StartNew();

        await foreach (var progress in handler.ExecuteWithStreamingAsync(agentContext, cancellationToken))
        {
            if (stopwatch.Elapsed.TotalSeconds >= StreamingThresholdSeconds)
            {
                yield return progress;
            }
        }
    }

    private AgentContext PrepareAgentContext(
        WorkflowInstance instance, 
        ServiceDefaults.Models.Workflows.WorkflowStep step, 
        string? userInput,
        SharedContext? sharedContext)
    {
        // Parse step parameters from InputSchema (if any)
        JsonDocument? stepParameters = null;
        if (!string.IsNullOrWhiteSpace(step.InputSchema))
        {
            try
            {
                stepParameters = JsonDocument.Parse(step.InputSchema);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse InputSchema for step {StepId}", step.StepId);
            }
        }

        return new AgentContext
        {
            WorkflowInstanceId = instance.Id,
            StepId = step.StepId,
            StepName = step.Name,
            WorkflowContext = instance.Context,
            StepData = instance.StepData,
            StepParameters = stepParameters,
            ConversationHistory = new List<ConversationMessage>(),
            UserInput = userInput,
            SharedContext = sharedContext
        };
    }

    private async Task<List<string>> ValidateOutputAsync(string outputSchema, JsonDocument output)
    {
        var errors = new List<string>();
        
        try
        {
            var schema = await JsonSchema.FromJsonAsync(outputSchema);
            var json = output.RootElement.GetRawText();
            var validationErrors = schema.Validate(json);
            
            foreach (var error in validationErrors)
            {
                errors.Add($"{error.Path}: {error.Kind}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating output schema");
            errors.Add($"Schema validation error: {ex.Message}");
        }

        return errors;
    }

    private JsonDocument MergeStepData(JsonDocument? existing, string stepId, JsonDocument newData)
    {
        var stepDataDict = new Dictionary<string, object>();
        
        if (existing != null)
        {
            try
            {
                var existingDict = JsonSerializer.Deserialize<Dictionary<string, object>>(existing.RootElement.GetRawText());
                if (existingDict != null)
                {
                    stepDataDict = existingDict;
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize existing step data");
            }
        }

        stepDataDict[stepId] = JsonSerializer.Deserialize<object>(newData.RootElement.GetRawText()) ?? new { };
        
        return JsonDocument.Parse(JsonSerializer.Serialize(stepDataDict));
    }

    private async Task UpdateStepHistoryFailure(
        WorkflowStepHistory stepHistory, 
        string errorMessage, 
        CancellationToken cancellationToken)
    {
        stepHistory.CompletedAt = DateTime.UtcNow;
        stepHistory.Status = StepExecutionStatus.Failed;
        stepHistory.ErrorMessage = errorMessage;
        await _context.SaveChangesAsync(cancellationToken);
    }
}
