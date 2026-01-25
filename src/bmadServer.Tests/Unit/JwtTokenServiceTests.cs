using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using bmadServer.ApiService.Configuration;
using bmadServer.ApiService.Data.Entities;
using bmadServer.ApiService.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace bmadServer.Tests.Unit;

public class JwtTokenServiceTests
{
    private readonly JwtSettings _jwtSettings;
    private readonly JwtTokenService _tokenService;

    public JwtTokenServiceTests()
    {
        // NOTE: This key is for TESTING ONLY - never use in production
        _jwtSettings = new JwtSettings
        {
            SecretKey = "TEST_SECRET_KEY_32_CHARACTERS_MINIMUM_FOR_HS256_SIGNING",
            Issuer = "bmadServer",
            Audience = "bmadServer-api",
            AccessTokenExpirationMinutes = 15
        };

        var options = Options.Create(_jwtSettings);
        _tokenService = new JwtTokenService(options);
    }

    [Fact]
    public void GenerateAccessToken_ValidUser_ReturnsJwtToken()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            DisplayName = "Test User",
            PasswordHash = "hash"
        };

        // Act
        var token = _tokenService.GenerateAccessToken(user);

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);
        Assert.Contains(".", token); // JWT format has dots
    }

    [Fact]
    public void GenerateAccessToken_ValidUser_ContainsCorrectClaims()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            DisplayName = "Test User",
            PasswordHash = "hash"
        };

        // Act
        var token = _tokenService.GenerateAccessToken(user);

        // Assert - decode and verify claims
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        Assert.Equal(user.Id.ToString(), jwtToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal(user.Email, jwtToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value);
        Assert.NotNull(jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti));
        Assert.NotNull(jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Iat));
    }

    [Fact]
    public void GenerateAccessToken_ValidUser_ContainsCorrectIssuerAndAudience()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            DisplayName = "Test User",
            PasswordHash = "hash"
        };

        // Act
        var token = _tokenService.GenerateAccessToken(user);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        Assert.Equal(_jwtSettings.Issuer, jwtToken.Issuer);
        Assert.Contains(_jwtSettings.Audience, jwtToken.Audiences);
    }

    [Fact]
    public void GenerateAccessToken_ValidUser_ExpiresInConfiguredTime()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            DisplayName = "Test User",
            PasswordHash = "hash"
        };

        var beforeGeneration = DateTime.UtcNow;

        // Act
        var token = _tokenService.GenerateAccessToken(user);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        var expectedExpiry = beforeGeneration.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);
        
        // Allow 10 seconds tolerance for test execution time
        Assert.True(Math.Abs((jwtToken.ValidTo - expectedExpiry).TotalSeconds) < 10);
    }

    [Fact]
    public void Constructor_InvalidSecretKeyTooShort_ThrowsException()
    {
        // Arrange
        var invalidSettings = new JwtSettings
        {
            SecretKey = "short",
            Issuer = "bmadServer",
            Audience = "bmadServer-api",
            AccessTokenExpirationMinutes = 15
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            new JwtTokenService(Options.Create(invalidSettings)));

        Assert.Contains("at least 32 characters", exception.Message);
    }

    [Fact]
    public void Constructor_EmptySecretKey_ThrowsException()
    {
        // Arrange
        var invalidSettings = new JwtSettings
        {
            SecretKey = "",
            Issuer = "bmadServer",
            Audience = "bmadServer-api",
            AccessTokenExpirationMinutes = 15
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            new JwtTokenService(Options.Create(invalidSettings)));

        Assert.Contains("not configured", exception.Message);
    }
}
