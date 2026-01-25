using bmadServer.ApiService.Data.Entities;

namespace bmadServer.ApiService.Services;

/// <summary>
/// Service for generating and managing JWT tokens
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Generates a JWT access token for the specified user
    /// </summary>
    /// <param name="user">User to generate token for</param>
    /// <returns>JWT token string</returns>
    string GenerateAccessToken(User user);
}
