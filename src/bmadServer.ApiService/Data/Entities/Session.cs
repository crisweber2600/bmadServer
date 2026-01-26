using bmadServer.ApiService.Models;

namespace bmadServer.ApiService.Data.Entities;

/// <summary>
/// Represents a user session with SignalR connection and workflow state persistence.
/// Supports session recovery within 60 seconds (NFR6) and 30-minute idle timeout.
/// </summary>
public class Session
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string? ConnectionId { get; set; } // SignalR connection ID, nullable for expired sessions
    public WorkflowState? WorkflowState { get; set; } // JSONB column storing workflow context
    public PersonaType? SessionPersona { get; set; } // Session-level persona override (Story 8.4)
    public int PersonaSwitchCount { get; set; } = 0; // Track persona switches for analytics
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddMinutes(30);
    public bool IsActive { get; set; } = true;
    
    public User User { get; set; } = null!;
    
    /// <summary>
    /// Computed property for NFR6 (60-second recovery window).
    /// Returns true if the session can be directly recovered with same session ID.
    /// </summary>
    public bool IsWithinRecoveryWindow => 
        DateTime.UtcNow.Subtract(LastActivityAt).TotalSeconds <= 60;
}
