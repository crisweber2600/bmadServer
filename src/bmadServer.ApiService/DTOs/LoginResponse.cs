namespace bmadServer.ApiService.DTOs;

/// <summary>
/// Response model for successful login
/// </summary>
public class LoginResponse
{
    /// <summary>
    /// JWT access token
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Token type (always "Bearer" for JWT)
    /// </summary>
    public string TokenType { get; set; } = "Bearer";

    /// <summary>
    /// Token expiration time in seconds
    /// </summary>
    public int ExpiresIn { get; set; }

    /// <summary>
    /// User information
    /// </summary>
    public UserResponse User { get; set; } = null!;
}
