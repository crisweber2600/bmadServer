using bmadServer.ApiService.Data.Entities;

namespace bmadServer.ApiService.Services;

/// <summary>
/// Result of refresh token validation and rotation operation.
/// </summary>
public class RefreshTokenResult
{
    /// <summary>
    /// The new refresh token entity if operation succeeded.
    /// </summary>
    public RefreshToken? Token { get; set; }
    
    /// <summary>
    /// The plain text token value for client storage.
    /// </summary>
    public string? PlainToken { get; set; }
    
    /// <summary>
    /// Error message if operation failed.
    /// </summary>
    public string? Error { get; set; }
    
    /// <summary>
    /// Indicates whether the operation succeeded.
    /// </summary>
    public bool IsSuccess => Token != null && PlainToken != null && Error == null;
}
