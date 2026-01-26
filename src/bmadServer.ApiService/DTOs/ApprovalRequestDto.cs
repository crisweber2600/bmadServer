using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace bmadServer.ApiService.DTOs;

public class ApprovalRequestDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }
    
    [JsonPropertyName("workflowInstanceId")]
    public Guid WorkflowInstanceId { get; set; }
    
    [JsonPropertyName("agentId")]
    public string AgentId { get; set; } = string.Empty;
    
    [JsonPropertyName("stepId")]
    public string? StepId { get; set; }
    
    [JsonPropertyName("proposedResponse")]
    public string ProposedResponse { get; set; } = string.Empty;
    
    [JsonPropertyName("confidenceScore")]
    public double ConfidenceScore { get; set; }
    
    [JsonPropertyName("reasoning")]
    public string? Reasoning { get; set; }
    
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
    
    [JsonPropertyName("requestedAt")]
    public DateTime RequestedAt { get; set; }
    
    [JsonPropertyName("resolvedAt")]
    public DateTime? ResolvedAt { get; set; }
    
    [JsonPropertyName("resolvedBy")]
    public Guid? ResolvedBy { get; set; }
    
    [JsonPropertyName("modifiedResponse")]
    public string? ModifiedResponse { get; set; }
    
    [JsonPropertyName("rejectionReason")]
    public string? RejectionReason { get; set; }
}

public class ApprovalModifyRequest
{
    [Required]
    [MinLength(1)]
    [JsonPropertyName("modifiedResponse")]
    public string ModifiedResponse { get; set; } = string.Empty;
}

public class ApprovalRejectRequest
{
    [Required]
    [MinLength(1)]
    [MaxLength(2000)]
    [JsonPropertyName("rejectionReason")]
    public string RejectionReason { get; set; } = string.Empty;
}
