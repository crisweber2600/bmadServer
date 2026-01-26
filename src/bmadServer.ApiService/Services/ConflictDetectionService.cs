using bmadServer.ApiService.Data;
using bmadServer.ApiService.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace bmadServer.ApiService.Services;

public class ConflictDetectionService : IConflictDetectionService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<ConflictDetectionService> _logger;

    public ConflictDetectionService(
        ApplicationDbContext dbContext,
        ILogger<ConflictDetectionService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Conflict?> DetectConflictAsync(
        Guid workflowId, 
        string fieldName, 
        BufferedInput newInput, 
        CancellationToken cancellationToken = default)
    {
        var existingInputs = await _dbContext.BufferedInputs
            .Where(bi => bi.WorkflowInstanceId == workflowId 
                      && bi.FieldName == fieldName
                      && !bi.IsApplied
                      && bi.ConflictId == null
                      && bi.UserId != newInput.UserId)
            .ToListAsync(cancellationToken);

        if (!existingInputs.Any())
        {
            return null;
        }

        // Check if values differ
        var existingValue = existingInputs.First().Value;
        if (existingValue == newInput.Value)
        {
            return null;
        }

        _logger.LogInformation(
            "Conflict detected on workflow {WorkflowId}, field {FieldName}", 
            workflowId, fieldName);

        // Create conflict
        var conflict = new Conflict
        {
            Id = Guid.NewGuid(),
            WorkflowInstanceId = workflowId,
            FieldName = fieldName,
            Type = ConflictType.FieldValue,
            Status = ConflictStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };

        var inputs = new List<ConflictInput>();
        foreach (var existing in existingInputs)
        {
            inputs.Add(new ConflictInput
            {
                UserId = existing.UserId,
                DisplayName = existing.DisplayName,
                Value = existing.Value,
                Timestamp = existing.Timestamp,
                BufferedInputId = existing.Id
            });
            existing.ConflictId = conflict.Id;
        }

        inputs.Add(new ConflictInput
        {
            UserId = newInput.UserId,
            DisplayName = newInput.DisplayName,
            Value = newInput.Value,
            Timestamp = newInput.Timestamp,
            BufferedInputId = newInput.Id
        });

        conflict.SetInputs(inputs);
        newInput.ConflictId = conflict.Id;

        _dbContext.Conflicts.Add(conflict);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return conflict;
    }

    public async Task<List<Conflict>> GetPendingConflictsAsync(
        Guid workflowId, 
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Conflicts
            .Where(c => c.WorkflowInstanceId == workflowId && c.Status == ConflictStatus.Pending)
            .ToListAsync(cancellationToken);
    }
}
