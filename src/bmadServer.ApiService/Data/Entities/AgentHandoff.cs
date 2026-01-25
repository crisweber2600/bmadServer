using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace bmadServer.ApiService.Data.Entities;

/// <summary>
/// Entity representing an agent handoff event in the audit log.
/// Tracks when workflow execution transfers from one agent to another.
/// </summary>
[Table("agent_handoffs")]
public class AgentHandoff
{
    /// <summary>
    /// Unique identifier for the handoff event.
    /// </summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// ID of the workflow instance where the handoff occurred.
    /// </summary>
    [Required]
    public Guid WorkflowInstanceId { get; set; }

    /// <summary>
    /// ID of the agent handing off control (nullable for initial agent).
    /// </summary>
    public string? FromAgent { get; set; }

    /// <summary>
    /// ID of the agent receiving control.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public required string ToAgent { get; set; }

    /// <summary>
    /// Timestamp when the handoff occurred.
    /// </summary>
    [Required]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Workflow step at which the handoff occurred.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public required string WorkflowStep { get; set; }

    /// <summary>
    /// Reason for the handoff (e.g., "Step requires product-manager capabilities").
    /// </summary>
    [MaxLength(500)]
    public string? Reason { get; set; }

    /// <summary>
    /// Additional metadata about the handoff (stored as JSON).
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? Metadata { get; set; }

    /// <summary>
    /// Name of the agent receiving control (denormalized for easy display).
    /// </summary>
    [Required]
    [MaxLength(200)]
    public required string ToAgentName { get; set; }

    /// <summary>
    /// Name of the agent handing off control (denormalized, nullable for initial).
    /// </summary>
    [MaxLength(200)]
    public string? FromAgentName { get; set; }
}
