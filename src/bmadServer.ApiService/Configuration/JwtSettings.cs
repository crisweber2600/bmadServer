namespace bmadServer.ApiService.Configuration;

/// <summary>
/// JWT authentication configuration settings
/// </summary>
public class JwtSettings
{
    public const string SectionName = "Jwt";

    /// <summary>
    /// Secret key for signing JWT tokens (minimum 32 characters for HS256)
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Token issuer identifier
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// Token audience identifier
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Access token expiration time in minutes
    /// </summary>
    public int AccessTokenExpirationMinutes { get; set; } = 15;

    /// <summary>
    /// Validates that all required settings are properly configured
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(SecretKey))
            throw new InvalidOperationException("JWT SecretKey is not configured");

        if (SecretKey.Length < 32)
            throw new InvalidOperationException("JWT SecretKey must be at least 32 characters for security");

        if (string.IsNullOrWhiteSpace(Issuer))
            throw new InvalidOperationException("JWT Issuer is not configured");

        if (string.IsNullOrWhiteSpace(Audience))
            throw new InvalidOperationException("JWT Audience is not configured");

        if (AccessTokenExpirationMinutes <= 0)
            throw new InvalidOperationException("JWT AccessTokenExpirationMinutes must be greater than 0");
    }
}
