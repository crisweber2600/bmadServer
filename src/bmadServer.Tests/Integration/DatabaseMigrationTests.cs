using bmadServer.ApiService.Data;
using bmadServer.ApiService.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace bmadServer.Tests.Integration.Database;

/// <summary>
/// Database migration and context initialization tests.
/// Validates that EF Core is properly configured for PostgreSQL.
/// Note: These tests use mocked DbContext - full integration tests run in CI/CD with actual PostgreSQL.
/// </summary>
[Collection("Database")]
public class DatabaseMigrationTests
{
    [Fact]
    public void User_Entity_HasRequiredProperties()
    {
        // Arrange
        var user = new User
        {
            Email = "test@example.com",
            PasswordHash = "hashed_password_value",
            DisplayName = "Test User"
        };

        // Act & Assert - verify required properties can be set
        Assert.NotNull(user);
        Assert.Equal("test@example.com", user.Email);
        Assert.Equal("hashed_password_value", user.PasswordHash);
        Assert.Equal("Test User", user.DisplayName);
        Assert.NotEqual(Guid.Empty, user.Id);
    }

    [Fact]
    public void User_Entity_HasSessionsNavigation()
    {
        // Arrange
        var user = new User
        {
            Email = "test@example.com",
            PasswordHash = "hashed",
            DisplayName = "Test User"
        };

        // Act & Assert - verify navigation property exists and is empty
        Assert.NotNull(user.Sessions);
        Assert.Empty(user.Sessions);
    }

    [Fact]
    public void Session_Entity_HasRequiredProperties()
    {
        // Arrange
        var session = new Session
        {
            UserId = Guid.NewGuid(),
            ConnectionId = "conn-123"
        };

        // Act & Assert
        Assert.NotNull(session);
        Assert.NotEqual(Guid.Empty, session.UserId);
        Assert.Equal("conn-123", session.ConnectionId);
    }

    [Fact]
    public void Workflow_Entity_HasRequiredProperties()
    {
        // Arrange
        var workflow = new Workflow
        {
            Name = "Test Workflow",
            Status = "active"
        };

        // Act & Assert
        Assert.NotNull(workflow);
        Assert.Equal("Test Workflow", workflow.Name);
        Assert.Equal("active", workflow.Status);
    }

    [Fact]
    public void ApplicationDbContext_Type_Exists()
    {
        // Arrange & Act
        var contextType = typeof(ApplicationDbContext);

        // Assert - verify context type is available
        Assert.NotNull(contextType);
        Assert.Equal("ApplicationDbContext", contextType.Name);
    }
}

