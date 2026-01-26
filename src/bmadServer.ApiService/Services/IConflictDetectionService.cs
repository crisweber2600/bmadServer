using bmadServer.ApiService.Data.Entities;

namespace bmadServer.ApiService.Services;

public interface IConflictDetectionService
{
    Task<Conflict?> DetectConflictAsync(
        Guid workflowId, 
        string fieldName, 
        BufferedInput newInput, 
        CancellationToken cancellationToken = default);
    
    Task<List<Conflict>> GetPendingConflictsAsync(
        Guid workflowId, 
        CancellationToken cancellationToken = default);
}
