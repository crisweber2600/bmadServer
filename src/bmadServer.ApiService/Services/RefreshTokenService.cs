using System.Data;
using System.Security.Cryptography;
using System.Text;
using bmadServer.ApiService.Data;
using bmadServer.ApiService.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace bmadServer.ApiService.Services;

public class RefreshTokenService : IRefreshTokenService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<RefreshTokenService> _logger;

    public RefreshTokenService(ApplicationDbContext dbContext, ILogger<RefreshTokenService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public string GenerateRefreshToken()
    {
        return Guid.NewGuid().ToString("N"); // UUID v4 without dashes
    }

    public string HashToken(string token)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    public async Task<(RefreshToken token, string plainToken)> CreateRefreshTokenAsync(User user)
    {
        var plainToken = GenerateRefreshToken();
        var tokenHash = HashToken(plainToken);
        
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            TokenHash = tokenHash,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };
        
        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync();
        
        _logger.LogInformation("Refresh token created for user: {UserId}", user.Id);
        
        return (refreshToken, plainToken);
    }

    public async Task<(RefreshToken? token, string? error)> ValidateAndRotateAsync(string plainToken)
    {
        var tokenHash = HashToken(plainToken);
        
        await using var transaction = await _dbContext.Database
            .BeginTransactionAsync(IsolationLevel.Serializable);
        
        try
        {
            var token = await _dbContext.RefreshTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);
            
            if (token == null)
            {
                _logger.LogWarning("Invalid refresh token attempt");
                return (null, "Invalid refresh token");
            }
            
            if (token.IsRevoked)
            {
                // Token reuse detected - security breach!
                _logger.LogWarning("Refresh token reuse detected for user: {UserId}", token.UserId);
                await RevokeAllUserTokensAsync(token.UserId, "breach");
                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();
                return (null, "Refresh token has been revoked. All sessions terminated.");
            }
            
            if (token.IsExpired)
            {
                _logger.LogInformation("Expired refresh token for user: {UserId}", token.UserId);
                token.RevokedAt = DateTime.UtcNow;
                token.RevokedReason = "expired";
                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();
                return (null, "Refresh token expired. Please login again.");
            }
            
            // Revoke old token and create new one (rotation)
            token.RevokedAt = DateTime.UtcNow;
            token.RevokedReason = "rotation";
            
            var (newToken, newPlainToken) = await CreateRefreshTokenAsync(token.User);
            token.ReplacedByTokenId = newToken.Id.ToString();
            
            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
            
            _logger.LogInformation("Refresh token rotated for user: {UserId}", token.UserId);
            
            // Return the new token with plain text for cookie setting
            newToken.TokenHash = newPlainToken; // Temporarily store plain token for cookie
            return (newToken, null);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();
            _logger.LogWarning("Concurrent refresh token request detected");
            return (null, "Token already refreshed. Please retry.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error during refresh token validation and rotation");
            throw;
        }
    }

    public async Task RevokeRefreshTokenAsync(string tokenHash, string reason)
    {
        var token = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);
        
        if (token != null && token.RevokedAt == null)
        {
            token.RevokedAt = DateTime.UtcNow;
            token.RevokedReason = reason;
            await _dbContext.SaveChangesAsync();
            
            _logger.LogInformation("Refresh token revoked: {TokenId}, Reason: {Reason}", token.Id, reason);
        }
    }

    public async Task RevokeAllUserTokensAsync(Guid userId, string reason)
    {
        var activeTokens = await _dbContext.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync();
        
        foreach (var token in activeTokens)
        {
            token.RevokedAt = DateTime.UtcNow;
            token.RevokedReason = reason;
        }
        
        await _dbContext.SaveChangesAsync();
        
        _logger.LogWarning("All refresh tokens revoked for user: {UserId}, Reason: {Reason}, Count: {Count}", 
            userId, reason, activeTokens.Count);
    }
}
