namespace bmadServer.ApiService.DTOs;

/// <summary>
/// User registration request
/// </summary>
public class RegisterRequest
{
    /// <summary>
    /// User email address (must be valid email format)
    /// </summary>
    public required string Email { get; set; }
    
    /// <summary>
    /// User password (minimum 8 characters, must contain number and special character)
    /// </summary>
    public required string Password { get; set; }
    
    /// <summary>
    /// User display name (maximum 100 characters)
    /// </summary>
    public required string DisplayName { get; set; }
}
