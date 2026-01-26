using bmadServer.ApiService.Data;
using bmadServer.ApiService.DTOs.Checkpoints;
using bmadServer.ApiService.Models.Workflows;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace bmadServer.ApiService.Services.Checkpoints;

public class InputQueueService : IInputQueueService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<InputQueueService> _logger;

    public InputQueueService(
        ApplicationDbContext context,
        ILogger<InputQueueService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<QueuedInput> EnqueueInputAsync(
        Guid workflowId,
        Guid userId,
        string inputType,
        JsonDocument content,
        CancellationToken cancellationToken = default)
    {
        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var workflow = await _context.WorkflowInstances
                .FirstOrDefaultAsync(w => w.Id == workflowId, cancellationToken);

            if (workflow == null)
            {
                throw new InvalidOperationException($"Workflow {workflowId} not found");
            }

            var queuedInput = new QueuedInput
            {
                Id = Guid.NewGuid(),
                WorkflowId = workflowId,
                UserId = userId,
                InputType = inputType,
                Content = content,
                QueuedAt = DateTime.UtcNow,
                Status = InputStatus.Queued
            };

            _context.QueuedInputs.Add(queuedInput);
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Enqueued input {InputId} of type {InputType} for workflow {WorkflowId} by user {UserId}",
                queuedInput.Id, inputType, workflowId, userId);

            return queuedInput;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Failed to enqueue input for workflow {WorkflowId}", workflowId);
            throw;
        }
    }

    public async Task<InputProcessingResult> ProcessQueuedInputsAsync(
        Guid workflowId,
        CancellationToken cancellationToken = default)
    {
        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var queuedInputs = await _context.QueuedInputs
                .Where(qi => qi.WorkflowId == workflowId && qi.Status == InputStatus.Queued)
                .OrderBy(qi => qi.SequenceNumber)
                .ToListAsync(cancellationToken);

            int processedCount = 0;
            int rejectedCount = 0;
            var errors = new List<string>();

            foreach (var input in queuedInputs)
            {
                try
                {
                    // Validate input before processing
                    var isValid = await ValidateInputAsync(input, cancellationToken);
                    
                    if (isValid)
                    {
                        // Mark as processed
                        input.Status = InputStatus.Processed;
                        input.ProcessedAt = DateTime.UtcNow;
                        processedCount++;

                        _logger.LogInformation(
                            "Processed queued input {InputId} for workflow {WorkflowId}",
                            input.Id, workflowId);
                    }
                    else
                    {
                        // Mark as rejected
                        input.Status = InputStatus.Rejected;
                        input.RejectionReason = "Input validation failed";
                        input.ProcessedAt = DateTime.UtcNow;
                        rejectedCount++;
                        errors.Add($"Input {input.Id} rejected: validation failed");

                        _logger.LogWarning(
                            "Rejected queued input {InputId} for workflow {WorkflowId}: validation failed",
                            input.Id, workflowId);
                    }
                }
                catch (Exception ex)
                {
                    input.Status = InputStatus.Failed;
                    input.RejectionReason = ex.Message;
                    input.ProcessedAt = DateTime.UtcNow;
                    errors.Add($"Input {input.Id} failed: {ex.Message}");

                    _logger.LogError(ex,
                        "Failed to process queued input {InputId} for workflow {WorkflowId}",
                        input.Id, workflowId);
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Completed processing {ProcessedCount} inputs, {RejectedCount} rejected for workflow {WorkflowId}",
                processedCount, rejectedCount, workflowId);

            return new InputProcessingResult(processedCount, rejectedCount, errors);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Failed to process queued inputs for workflow {WorkflowId}", workflowId);
            throw;
        }
    }

    public async Task<List<QueuedInput>> GetQueuedInputsAsync(
        Guid workflowId,
        CancellationToken cancellationToken = default)
    {
        return await _context.QueuedInputs
            .AsNoTracking()
            .Where(qi => qi.WorkflowId == workflowId && qi.Status == InputStatus.Queued)
            .OrderBy(qi => qi.SequenceNumber)
            .ToListAsync(cancellationToken);
    }

    private async Task<bool> ValidateInputAsync(QueuedInput input, CancellationToken cancellationToken)
    {
        // Basic validation - check if workflow still exists and is in valid state
        var workflow = await _context.WorkflowInstances
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == input.WorkflowId, cancellationToken);

        if (workflow == null)
        {
            return false;
        }

        // Additional validation can be added here based on input type
        // For now, just ensure the content is valid JSON
        try
        {
            var content = input.Content.RootElement;
            return content.ValueKind != JsonValueKind.Undefined;
        }
        catch
        {
            return false;
        }
    }
}
