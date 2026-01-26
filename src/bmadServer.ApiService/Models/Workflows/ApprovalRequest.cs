using System.ComponentModel.DataAnnotations;

namespace bmadServer.ApiService.Models.Workflows;

public class ApprovalRequest
{
    public Guid Id { get; set; }
    
    [Required]
    public Guid WorkflowInstanceId { get; set; }
    
    public WorkflowInstance? WorkflowInstance { get; set; }
    
    [Required]
    [MaxLength(100)]
    public required string AgentId { get; set; }
    
    [Required]
    public required string ProposedResponse { get; set; }
    
    [Required]
    [Range(0.0, 1.0)]
    public double ConfidenceScore { get; set; }
    
    [MaxLength(2000)]
    public string? Reasoning { get; set; }
    
    [Required]
    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;
    
    [Required]
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? ResolvedAt { get; set; }
    
    [Required]
    public Guid RequestedBy { get; set; }
    
    public Guid? ResolvedBy { get; set; }
    
    public string? ModifiedResponse { get; set; }
    
    [MaxLength(2000)]
    public string? RejectionReason { get; set; }
    
    public int Version { get; set; } = 1;
    
    [Required]
    [MaxLength(100)]
    public required string StepId { get; set; }
}
