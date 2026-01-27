using System.ComponentModel.DataAnnotations;

namespace bmadServer.ApiService.Models.Workflows;

/// <summary>
/// Represents a handoff event when control transfers from one agent to another during workflow execution.
/// Provides complete audit trail of agent transitions including timing, reason, and affected step.
/// </summary>
public class AgentHandoff
{
    /// <summary>
    /// Unique identifier for this handoff record.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Foreign key reference to the workflow instance this handoff belongs to.
    /// </summary>
    [Required]
    public Guid WorkflowInstanceId { get; set; }

    /// <summary>
    /// ID of the agent handing off responsibility.
    /// Typically a kebab-case string like "product-manager", "architect", "developer".
    /// </summary>
    [Required]
    public required string FromAgentId { get; set; }

    /// <summary>
    /// ID of the agent assuming responsibility.
    /// Typically a kebab-case string like "product-manager", "architect", "developer".
    /// </summary>
    [Required]
    public required string ToAgentId { get; set; }

    /// <summary>
    /// UTC timestamp when this handoff occurred.
    /// </summary>
    [Required]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// ID or index of the workflow step where this handoff occurred.
    /// Used to correlate handoffs with specific workflow progress.
    /// </summary>
    [Required]
    public required string WorkflowStepId { get; set; }

    /// <summary>
    /// Reason for the handoff (e.g., "Step requires Architect expertise" or "Parallel processing needed").
    /// Non-PII, generic descriptions suitable for audit logs.
    /// </summary>
    public string? Reason { get; set; }

    // ========== FOREIGN KEY RELATIONSHIPS ==========

    /// <summary>
    /// Navigation property to the workflow instance.
    /// Lazily loaded by EF Core.
    /// </summary>
    public WorkflowInstance? WorkflowInstance { get; set; }
}
