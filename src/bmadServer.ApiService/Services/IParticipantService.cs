using bmadServer.ApiService.Models.Workflows;

namespace bmadServer.ApiService.Services;

public interface IParticipantService
{
    Task<WorkflowParticipant> AddParticipantAsync(Guid workflowId, Guid userId, ParticipantRole role, Guid addedBy, CancellationToken cancellationToken = default);
    Task<bool> RemoveParticipantAsync(Guid workflowId, Guid userId, CancellationToken cancellationToken = default);
    Task<List<WorkflowParticipant>> GetParticipantsAsync(Guid workflowId, CancellationToken cancellationToken = default);
    Task<WorkflowParticipant?> GetParticipantAsync(Guid workflowId, Guid userId, CancellationToken cancellationToken = default);
    Task<bool> IsParticipantAsync(Guid workflowId, Guid userId, CancellationToken cancellationToken = default);
    Task<bool> IsWorkflowOwnerAsync(Guid workflowId, Guid userId, CancellationToken cancellationToken = default);
}
