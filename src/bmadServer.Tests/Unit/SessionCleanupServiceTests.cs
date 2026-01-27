using bmadServer.ApiService.BackgroundServices;
using bmadServer.ApiService.Data;
using bmadServer.ApiService.Data.Entities;
using bmadServer.Tests.Helpers;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace bmadServer.Tests.Unit;

/// <summary>
/// Unit tests for SessionCleanupService background worker.
/// Tests automatic expiration of idle sessions while preserving audit trail.
/// </summary>
public class SessionCleanupServiceTests : IDisposable
{
    private SqliteConnection? _connection;

    private ApplicationDbContext CreateSqliteDbContext()
    {
        var options = TestDatabaseHelper.CreateSqliteOptions(out _connection);
        var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }

    [Fact]
    public async Task CleanupService_Should_Expire_Sessions_Past_ExpiresAt()
    {
        // Arrange
        await using var dbContext = CreateSqliteDbContext();
        
        var user = new User
        {
            Email = "test@example.com",
            PasswordHash = "hash",
            DisplayName = "Test User"
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        // Create expired session
        var expiredSession = new Session
        {
            UserId = user.Id,
            ConnectionId = "conn-old",
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddMinutes(-35),
            LastActivityAt = DateTime.UtcNow.AddMinutes(-31),
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1) // Expired 1 minute ago
        };

        // Create active session
        var activeSession = new Session
        {
            UserId = user.Id,
            ConnectionId = "conn-active",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(30)
        };

        dbContext.Sessions.AddRange(expiredSession, activeSession);
        await dbContext.SaveChangesAsync();

        // Create service provider with the in-memory dbContext
        var services = new ServiceCollection();
        services.AddSingleton(dbContext);
        services.AddLogging();
        var serviceProvider = services.BuildServiceProvider();

        var logger = serviceProvider.GetRequiredService<ILogger<SessionCleanupService>>();
        var cleanupService = new SessionCleanupService(serviceProvider, logger);

        // Act - Use reflection to call private cleanup method
        var method = typeof(SessionCleanupService)
            .GetMethod("CleanupExpiredSessionsAsync", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        await (Task)method!.Invoke(cleanupService, new object[] { CancellationToken.None })!;

        // Assert
        var expiredCheck = await dbContext.Sessions.FindAsync(expiredSession.Id);
        var activeCheck = await dbContext.Sessions.FindAsync(activeSession.Id);

        Assert.NotNull(expiredCheck);
        Assert.False(expiredCheck.IsActive); // Should be marked inactive
        Assert.Null(expiredCheck.ConnectionId); // Connection cleared

        Assert.NotNull(activeCheck);
        Assert.True(activeCheck.IsActive); // Should still be active
        Assert.NotNull(activeCheck.ConnectionId); // Connection preserved
    }

    [Fact]
    public async Task CleanupService_Should_Preserve_Session_For_Audit_Trail()
    {
        // Arrange
        await using var dbContext = CreateSqliteDbContext();
        
        var user = new User
        {
            Email = "test@example.com",
            PasswordHash = "hash",
            DisplayName = "Test User"
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var expiredSession = new Session
        {
            UserId = user.Id,
            ConnectionId = "conn-old",
            IsActive = true,
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1)
        };

        dbContext.Sessions.Add(expiredSession);
        await dbContext.SaveChangesAsync();

        var originalCount = await dbContext.Sessions.CountAsync();

        // Create service
        var services = new ServiceCollection();
        services.AddSingleton(dbContext);
        services.AddLogging();
        var serviceProvider = services.BuildServiceProvider();

        var logger = serviceProvider.GetRequiredService<ILogger<SessionCleanupService>>();
        var cleanupService = new SessionCleanupService(serviceProvider, logger);

        // Act
        var method = typeof(SessionCleanupService)
            .GetMethod("CleanupExpiredSessionsAsync", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        await (Task)method!.Invoke(cleanupService, new object[] { CancellationToken.None })!;

        // Assert
        var finalCount = await dbContext.Sessions.CountAsync();
        Assert.Equal(originalCount, finalCount); // Session not deleted, just marked inactive
        
        var session = await dbContext.Sessions.FindAsync(expiredSession.Id);
        Assert.NotNull(session); // Still exists
        Assert.False(session.IsActive); // But inactive
    }

    [Fact]
    public async Task CleanupService_Should_Not_Affect_Already_Inactive_Sessions()
    {
        // Arrange
        await using var dbContext = CreateSqliteDbContext();
        
        var user = new User
        {
            Email = "test@example.com",
            PasswordHash = "hash",
            DisplayName = "Test User"
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var inactiveSession = new Session
        {
            UserId = user.Id,
            ConnectionId = null,
            IsActive = false, // Already inactive
            ExpiresAt = DateTime.UtcNow.AddMinutes(-10)
        };

        dbContext.Sessions.Add(inactiveSession);
        await dbContext.SaveChangesAsync();

        // Create service
        var services = new ServiceCollection();
        services.AddSingleton(dbContext);
        services.AddLogging();
        var serviceProvider = services.BuildServiceProvider();

        var logger = serviceProvider.GetRequiredService<ILogger<SessionCleanupService>>();
        var cleanupService = new SessionCleanupService(serviceProvider, logger);

        // Act
        var method = typeof(SessionCleanupService)
            .GetMethod("CleanupExpiredSessionsAsync", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        await (Task)method!.Invoke(cleanupService, new object[] { CancellationToken.None })!;

        // Assert - Should not throw or modify inactive sessions
        var session = await dbContext.Sessions.FindAsync(inactiveSession.Id);
        Assert.NotNull(session);
        Assert.False(session.IsActive);
    }
}
