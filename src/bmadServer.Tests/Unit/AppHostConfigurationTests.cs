using Xunit;
using Microsoft.Extensions.Configuration;

namespace bmadServer.Tests.Unit;

/// <summary>
/// Tests for AppHost configuration, specifically the PgAdmin credentials feature flag
/// </summary>
public class AppHostConfigurationTests
{
    [Fact]
    public void PgAdminUseCredentials_DefaultsToFalse_InDevelopmentConfiguration()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        // Act
        var useCredentials = configuration.GetValue<bool>("PgAdmin:UseCredentials");

        // Assert
        Assert.False(useCredentials, "PgAdmin:UseCredentials should default to false for debugging scenarios");
    }

    [Fact]
    public void PgAdminUseCredentials_CanBeReadFromConfiguration()
    {
        // Arrange
        var configurationData = new Dictionary<string, string?>
        {
            { "PgAdmin:UseCredentials", "true" }
        };
        
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationData)
            .Build();

        // Act
        var useCredentials = configuration.GetValue<bool>("PgAdmin:UseCredentials");

        // Assert
        Assert.True(useCredentials, "PgAdmin:UseCredentials should be readable from configuration");
    }

    [Fact]
    public void PgAdminUseCredentials_ReturnsFalseWhenNotConfigured()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        // Act
        var useCredentials = configuration.GetValue<bool>("PgAdmin:UseCredentials");

        // Assert
        Assert.False(useCredentials, "PgAdmin:UseCredentials should default to false when not configured");
    }
}
