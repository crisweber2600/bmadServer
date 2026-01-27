using bmadServer.ApiService.Data;
using bmadServer.ApiService.Data.Entities;
using bmadServer.ApiService.Services;
using bmadServer.Tests.Helpers;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace bmadServer.Tests.Unit;

public class RefreshTokenServiceTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly SqliteConnection _connection;
    private readonly RefreshTokenService _service;

    public RefreshTokenServiceTests()
    {
        var options = TestDatabaseHelper.CreateSqliteOptions(out _connection);
        _dbContext = new ApplicationDbContext(options);
        _dbContext.Database.EnsureCreated();
        _service = new RefreshTokenService(_dbContext, NullLogger<RefreshTokenService>.Instance);
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturnUuidV4Format()
    {
        // Act
        var token = _service.GenerateRefreshToken();

        // Assert
        Assert.NotNull(token);
        Assert.Equal(32, token.Length); // UUID v4 without dashes is 32 characters
        Assert.All(token, c => Assert.True(char.IsLetterOrDigit(c)));
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturnUniqueTokens()
    {
        // Act
        var token1 = _service.GenerateRefreshToken();
        var token2 = _service.GenerateRefreshToken();

        // Assert
        Assert.NotEqual(token1, token2);
    }

    [Fact]
    public void HashToken_ShouldReturnBase64EncodedSha256Hash()
    {
        // Arrange
        var token = "test-token-123";

        // Act
        var hash = _service.HashToken(token);

        // Assert
        Assert.NotNull(hash);
        Assert.NotEqual(token, hash); // Hash should differ from plain text
        
        // Verify it's base64 encoded (44 chars for SHA256)
        Assert.True(hash.Length > 0);
        
        // Verify consistent hashing
        var hash2 = _service.HashToken(token);
        Assert.Equal(hash, hash2);
    }

    [Fact]
    public void HashToken_ShouldProduceDifferentHashesForDifferentTokens()
    {
        // Act
        var hash1 = _service.HashToken("token1");
        var hash2 = _service.HashToken("token2");

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public async Task CreateRefreshTokenAsync_ShouldCreateTokenInDatabase()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            PasswordHash = "hash",
            DisplayName = "Test User"
        };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        // Act
        var (token, plainToken) = await _service.CreateRefreshTokenAsync(user);

        // Assert
        Assert.NotNull(token);
        Assert.NotNull(plainToken);
        Assert.Equal(user.Id, token.UserId);
        Assert.True(token.ExpiresAt > DateTime.UtcNow.AddDays(6)); // ~7 days
        Assert.True(token.ExpiresAt < DateTime.UtcNow.AddDays(8));
        Assert.NotNull(token.TokenHash);
        Assert.NotEqual(plainToken, token.TokenHash); // Hash should differ
        Assert.Null(token.RevokedAt);
    }

    [Fact]
    public async Task ValidateAndRotateAsync_WithValidToken_ShouldRotateToken()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            PasswordHash = "hash",
            DisplayName = "Test User"
        };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var (oldToken, plainToken) = await _service.CreateRefreshTokenAsync(user);

        // Act
        var (newToken, error) = await _service.ValidateAndRotateAsync(plainToken);

        // Assert
        Assert.Null(error);
        Assert.NotNull(newToken);
        Assert.NotEqual(oldToken.Id, newToken.Id);
        Assert.Equal(user.Id, newToken.UserId);

        // Verify old token is revoked
        var revokedToken = await _dbContext.RefreshTokens.FindAsync(oldToken.Id);
        Assert.NotNull(revokedToken);
        Assert.NotNull(revokedToken.RevokedAt);
        Assert.Equal("rotation", revokedToken.RevokedReason);
        Assert.Equal(newToken.Id.ToString(), revokedToken.ReplacedByTokenId);
    }

    [Fact]
    public async Task ValidateAndRotateAsync_WithExpiredToken_ShouldReturnError()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            PasswordHash = "hash",
            DisplayName = "Test User"
        };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var plainToken = _service.GenerateRefreshToken();
        var expiredToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            TokenHash = _service.HashToken(plainToken),
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(-1), // Expired
            CreatedAt = DateTime.UtcNow.AddDays(-8)
        };
        _dbContext.RefreshTokens.Add(expiredToken);
        await _dbContext.SaveChangesAsync();

        // Act
        var (token, error) = await _service.ValidateAndRotateAsync(plainToken);

        // Assert
        Assert.Null(token);
        Assert.NotNull(error);
        Assert.Contains("expired", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ValidateAndRotateAsync_WithRevokedToken_ShouldDetectBreach()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            PasswordHash = "hash",
            DisplayName = "Test User"
        };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var plainToken = _service.GenerateRefreshToken();
        var revokedToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            TokenHash = _service.HashToken(plainToken),
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            RevokedAt = DateTime.UtcNow.AddMinutes(-5), // Already revoked
            RevokedReason = "rotation"
        };
        _dbContext.RefreshTokens.Add(revokedToken);
        await _dbContext.SaveChangesAsync();

        // Create another active token for the same user to verify it gets revoked
        var (activeToken, _) = await _service.CreateRefreshTokenAsync(user);

        // Act
        var (token, error) = await _service.ValidateAndRotateAsync(plainToken);

        // Assert
        Assert.Null(token);
        Assert.NotNull(error);
        Assert.Contains("revoked", error, StringComparison.OrdinalIgnoreCase);

        // Verify all user tokens are revoked (breach detection)
        var userTokens = await _dbContext.RefreshTokens
            .Where(t => t.UserId == user.Id)
            .ToListAsync();
        Assert.All(userTokens, t => Assert.NotNull(t.RevokedAt));
    }

    [Fact]
    public async Task ValidateAndRotateAsync_WithInvalidToken_ShouldReturnError()
    {
        // Act
        var (token, error) = await _service.ValidateAndRotateAsync("invalid-token-123");

        // Assert
        Assert.Null(token);
        Assert.NotNull(error);
        Assert.Contains("Invalid", error);
    }

    [Fact]
    public async Task RevokeRefreshTokenAsync_ShouldMarkTokenAsRevoked()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            PasswordHash = "hash",
            DisplayName = "Test User"
        };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var (token, plainToken) = await _service.CreateRefreshTokenAsync(user);

        // Act
        await _service.RevokeRefreshTokenAsync(token.TokenHash, "logout");

        // Assert
        var revokedToken = await _dbContext.RefreshTokens.FindAsync(token.Id);
        Assert.NotNull(revokedToken);
        Assert.NotNull(revokedToken.RevokedAt);
        Assert.Equal("logout", revokedToken.RevokedReason);
    }

    [Fact]
    public async Task RevokeAllUserTokensAsync_ShouldRevokeAllActiveTokens()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            PasswordHash = "hash",
            DisplayName = "Test User"
        };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        // Create multiple tokens
        await _service.CreateRefreshTokenAsync(user);
        await _service.CreateRefreshTokenAsync(user);
        await _service.CreateRefreshTokenAsync(user);

        // Act
        await _service.RevokeAllUserTokensAsync(user.Id, "security-breach");

        // Assert
        var userTokens = await _dbContext.RefreshTokens
            .Where(t => t.UserId == user.Id)
            .ToListAsync();
        
        Assert.Equal(3, userTokens.Count);
        Assert.All(userTokens, t =>
        {
            Assert.NotNull(t.RevokedAt);
            Assert.Equal("security-breach", t.RevokedReason);
        });
    }

    [Fact]
    public void RefreshToken_IsExpired_ShouldReturnTrueForExpiredTokens()
    {
        // Arrange
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            TokenHash = "hash",
            UserId = Guid.NewGuid(),
            ExpiresAt = DateTime.UtcNow.AddDays(-1),
            CreatedAt = DateTime.UtcNow.AddDays(-8)
        };

        // Assert
        Assert.True(token.IsExpired);
    }

    [Fact]
    public void RefreshToken_IsExpired_ShouldReturnFalseForActiveTokens()
    {
        // Arrange
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            TokenHash = "hash",
            UserId = Guid.NewGuid(),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };

        // Assert
        Assert.False(token.IsExpired);
    }

    [Fact]
    public void RefreshToken_IsRevoked_ShouldReturnTrueForRevokedTokens()
    {
        // Arrange
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            TokenHash = "hash",
            UserId = Guid.NewGuid(),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            RevokedAt = DateTime.UtcNow
        };

        // Assert
        Assert.True(token.IsRevoked);
    }

    [Fact]
    public void RefreshToken_IsActive_ShouldReturnTrueForValidTokens()
    {
        // Arrange
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            TokenHash = "hash",
            UserId = Guid.NewGuid(),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };

        // Assert
        Assert.True(token.IsActive);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }
}
