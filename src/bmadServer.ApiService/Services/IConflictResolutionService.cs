using bmadServer.ApiService.Data.Entities;

namespace bmadServer.ApiService.Services;

public interface IConflictResolutionService
{
    Task<bool> ResolveConflictAsync(
        Guid conflictId,
        Guid userId,
        string displayName,
        ResolutionType resolutionType,
        string? finalValue,
        string reason,
        CancellationToken cancellationToken = default);
}
