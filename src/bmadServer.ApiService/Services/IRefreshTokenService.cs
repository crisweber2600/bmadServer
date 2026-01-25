using bmadServer.ApiService.Data.Entities;

namespace bmadServer.ApiService.Services;

public interface IRefreshTokenService
{
    string GenerateRefreshToken();
    string HashToken(string token);
    Task<(RefreshToken token, string plainToken)> CreateRefreshTokenAsync(User user);
    Task<RefreshTokenResult> ValidateAndRotateAsync(string plainToken);
    Task RevokeRefreshTokenAsync(string tokenHash, string reason);
    Task RevokeAllUserTokensAsync(Guid userId, string reason);
}
