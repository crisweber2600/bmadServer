using bmadServer.ApiService.Services;
using Xunit;

namespace bmadServer.Tests.Unit;

public class PasswordHasherTests
{
    private readonly IPasswordHasher _passwordHasher;

    public PasswordHasherTests()
    {
        _passwordHasher = new PasswordHasher();
    }

    [Fact]
    public void Hash_ShouldReturnNonEmptyHash()
    {
        // Arrange
        var password = "SecurePass123!";

        // Act
        var hash = _passwordHasher.Hash(password);

        // Assert
        Assert.NotNull(hash);
        Assert.NotEmpty(hash);
        Assert.NotEqual(password, hash);
    }

    [Fact]
    public void Hash_ShouldReturnDifferentHashesForSamePassword()
    {
        // Arrange
        var password = "SecurePass123!";

        // Act
        var hash1 = _passwordHasher.Hash(password);
        var hash2 = _passwordHasher.Hash(password);

        // Assert - bcrypt generates different salts
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void Verify_ShouldReturnTrueForCorrectPassword()
    {
        // Arrange
        var password = "SecurePass123!";
        var hash = _passwordHasher.Hash(password);

        // Act
        var result = _passwordHasher.Verify(password, hash);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Verify_ShouldReturnFalseForIncorrectPassword()
    {
        // Arrange
        var correctPassword = "SecurePass123!";
        var incorrectPassword = "WrongPassword!";
        var hash = _passwordHasher.Hash(correctPassword);

        // Act
        var result = _passwordHasher.Verify(incorrectPassword, hash);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Hash_ShouldUseBcryptWorkFactor12()
    {
        // Arrange
        var password = "SecurePass123!";

        // Act
        var hash = _passwordHasher.Hash(password);

        // Assert - bcrypt hashes start with $2a$ or $2b$ followed by cost factor
        Assert.Matches(@"^\$2[ab]\$12\$", hash);
    }
}
