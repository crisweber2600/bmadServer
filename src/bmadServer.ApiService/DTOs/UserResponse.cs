using bmadServer.ApiService.Data.Entities;

namespace bmadServer.ApiService.DTOs;

/// <summary>
/// User response (excludes sensitive data like password hash)
/// </summary>
public class UserResponse
{
    /// <summary>
    /// Unique user identifier
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// User email address
    /// </summary>
    public required string Email { get; set; }
    
    /// <summary>
    /// User display name
    /// </summary>
    public required string DisplayName { get; set; }
    
    /// <summary>
    /// Account creation timestamp (UTC)
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Persona type for communication preferences
    /// </summary>
    public PersonaType PersonaType { get; set; }
}
