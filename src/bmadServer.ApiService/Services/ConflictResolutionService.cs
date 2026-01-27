using bmadServer.ApiService.Data;
using bmadServer.ApiService.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace bmadServer.ApiService.Services;

public class ConflictResolutionService : IConflictResolutionService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<ConflictResolutionService> _logger;

    public ConflictResolutionService(
        ApplicationDbContext dbContext,
        ILogger<ConflictResolutionService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<bool> ResolveConflictAsync(
        Guid conflictId,
        Guid userId,
        string displayName,
        ResolutionType resolutionType,
        string? finalValue,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var conflict = await _dbContext.Conflicts
            .FirstOrDefaultAsync(c => c.Id == conflictId, cancellationToken);

        if (conflict == null || conflict.Status != ConflictStatus.Pending)
        {
            return false;
        }

        var inputs = conflict.GetInputs();
        string resolvedValue;

        switch (resolutionType)
        {
            case ResolutionType.AcceptA:
                resolvedValue = inputs.First().Value;
                break;
            case ResolutionType.AcceptB:
                resolvedValue = inputs.Last().Value;
                break;
            case ResolutionType.Merge:
                resolvedValue = finalValue ?? string.Empty;
                break;
            case ResolutionType.RejectBoth:
                resolvedValue = string.Empty;
                break;
            default:
                return false;
        }

        var resolution = new ConflictResolution
        {
            ResolvedBy = userId,
            ResolverDisplayName = displayName,
            Type = resolutionType,
            FinalValue = resolvedValue,
            ResolvedAt = DateTime.UtcNow,
            Reason = reason
        };

        conflict.SetResolution(resolution);
        conflict.Status = ConflictStatus.Resolved;

        // Mark buffered inputs based on resolution
        if (resolutionType != ResolutionType.RejectBoth)
        {
            var bufferedInputs = await _dbContext.BufferedInputs
                .Where(bi => bi.ConflictId == conflictId)
                .ToListAsync(cancellationToken);

            foreach (var input in bufferedInputs)
            {
                if (resolutionType == ResolutionType.AcceptA && input.Id == inputs.First().BufferedInputId)
                {
                    input.IsApplied = false; // Will be applied at checkpoint
                }
                else if (resolutionType == ResolutionType.AcceptB && input.Id == inputs.Last().BufferedInputId)
                {
                    input.IsApplied = false; // Will be applied at checkpoint
                }
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Conflict {ConflictId} resolved by {UserId} with {ResolutionType}",
            conflictId, userId, resolutionType);

        return true;
    }
}
