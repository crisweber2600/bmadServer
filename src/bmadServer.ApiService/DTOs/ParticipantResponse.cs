namespace bmadServer.ApiService.DTOs;

public class ParticipantResponse
{
    public required Guid Id { get; set; }
    public required Guid WorkflowId { get; set; }
    public required Guid UserId { get; set; }
    public required string UserDisplayName { get; set; }
    public required string UserEmail { get; set; }
    public required string Role { get; set; }
    public required DateTime AddedAt { get; set; }
    public required Guid AddedBy { get; set; }
}
