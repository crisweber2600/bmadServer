namespace bmadServer.ApiService.Models.Workflows;

public class WorkflowParticipant
{
    public Guid Id { get; set; }
    public Guid WorkflowId { get; set; }
    public Guid UserId { get; set; }
    public ParticipantRole Role { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    public Guid AddedBy { get; set; }
    
    // Navigation properties
    public WorkflowInstance? Workflow { get; set; }
    public Data.Entities.User? User { get; set; }
}
