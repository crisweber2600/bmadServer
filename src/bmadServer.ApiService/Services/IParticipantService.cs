using bmadServer.ApiService.Models.Workflows;

namespace bmadServer.ApiService.Services;

public interface IParticipantService
{
    Task<WorkflowParticipant> AddParticipantAsync(Guid workflowId, Guid userId, ParticipantRole role, Guid addedBy);
    Task<bool> RemoveParticipantAsync(Guid workflowId, Guid userId);
    Task<List<WorkflowParticipant>> GetParticipantsAsync(Guid workflowId);
    Task<WorkflowParticipant?> GetParticipantAsync(Guid workflowId, Guid userId);
    Task<bool> IsParticipantAsync(Guid workflowId, Guid userId);
    Task<bool> IsWorkflowOwnerAsync(Guid workflowId, Guid userId);
}
