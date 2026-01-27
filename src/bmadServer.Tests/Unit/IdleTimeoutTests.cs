using bmadServer.ApiService.Configuration;
using bmadServer.ApiService.Data;
using bmadServer.ApiService.Data.Entities;
using bmadServer.ApiService.Middleware;
using bmadServer.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using Xunit;

namespace bmadServer.Tests.Unit;

public class IdleTimeoutTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly SqliteConnection _connection;
    private readonly User _testUser;
    private readonly Session _testSession;

    public IdleTimeoutTests()
    {
        var options = TestDatabaseHelper.CreateSqliteOptions(out _connection);
        _dbContext = new ApplicationDbContext(options);
        _dbContext.Database.EnsureCreated();

        _testUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            PasswordHash = "hash",
            DisplayName = "Test User"
        };
        _dbContext.Users.Add(_testUser);

        _testSession = new Session
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            ConnectionId = "test-connection",
            LastActivityAt = DateTime.UtcNow.AddMinutes(-5),
            CreatedAt = DateTime.UtcNow.AddHours(-1),
            ExpiresAt = DateTime.UtcNow.AddMinutes(25),
            IsActive = true
        };
        _dbContext.Sessions.Add(_testSession);
        _dbContext.SaveChanges();
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public void SessionSettings_HasCorrectDefaults()
    {
        var settings = new SessionSettings();

        Assert.Equal(60, settings.RecoveryWindowSeconds);
        Assert.Equal(30, settings.IdleTimeoutMinutes);
        Assert.Equal(28, settings.WarningTimeoutMinutes);
    }

    [Fact]
    public async Task Session_IsActive_WhenWithinTimeout()
    {
        var session = await _dbContext.Sessions.FindAsync(_testSession.Id);

        Assert.NotNull(session);
        Assert.True(session.IsActive);
        Assert.True(session.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task Session_CanBeExtended()
    {
        var session = await _dbContext.Sessions.FindAsync(_testSession.Id);
        Assert.NotNull(session);

        var originalExpiry = session.ExpiresAt;
        var originalActivity = session.LastActivityAt;

        session.LastActivityAt = DateTime.UtcNow;
        session.ExpiresAt = DateTime.UtcNow.AddMinutes(30);
        await _dbContext.SaveChangesAsync();

        var updatedSession = await _dbContext.Sessions.FindAsync(_testSession.Id);
        Assert.NotNull(updatedSession);
        Assert.True(updatedSession.LastActivityAt > originalActivity);
        Assert.True(updatedSession.ExpiresAt > originalExpiry);
    }

    [Fact]
    public async Task Session_CanBeMarkedInactive()
    {
        var session = await _dbContext.Sessions.FindAsync(_testSession.Id);
        Assert.NotNull(session);
        Assert.True(session.IsActive);

        session.IsActive = false;
        session.ConnectionId = null;
        await _dbContext.SaveChangesAsync();

        var updatedSession = await _dbContext.Sessions.FindAsync(_testSession.Id);
        Assert.NotNull(updatedSession);
        Assert.False(updatedSession.IsActive);
        Assert.Null(updatedSession.ConnectionId);
    }

    [Fact]
    public void Session_IsWithinRecoveryWindow_WhenRecentActivity()
    {
        var session = new Session
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            LastActivityAt = DateTime.UtcNow.AddSeconds(-30),
            CreatedAt = DateTime.UtcNow.AddHours(-1),
            ExpiresAt = DateTime.UtcNow.AddMinutes(30),
            IsActive = true
        };

        Assert.True(session.IsWithinRecoveryWindow);
    }

    [Fact]
    public void Session_IsNotWithinRecoveryWindow_WhenOldActivity()
    {
        var session = new Session
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            LastActivityAt = DateTime.UtcNow.AddSeconds(-90),
            CreatedAt = DateTime.UtcNow.AddHours(-1),
            ExpiresAt = DateTime.UtcNow.AddMinutes(30),
            IsActive = true
        };

        Assert.False(session.IsWithinRecoveryWindow);
    }

    [Fact]
    public async Task FindActiveSession_ReturnsSession_WhenExists()
    {
        var session = await _dbContext.Sessions
            .Where(s => s.UserId == _testUser.Id && s.IsActive)
            .FirstOrDefaultAsync();

        Assert.NotNull(session);
        Assert.Equal(_testSession.Id, session.Id);
    }

    [Fact]
    public async Task FindActiveSession_ReturnsNull_WhenNoActiveSession()
    {
        _testSession.IsActive = false;
        await _dbContext.SaveChangesAsync();

        var session = await _dbContext.Sessions
            .Where(s => s.UserId == _testUser.Id && s.IsActive)
            .FirstOrDefaultAsync();

        Assert.Null(session);
    }

    [Fact]
    public async Task ActivityTracking_UpdatesSession_WhenDebounceExceeded()
    {
        _testSession.LastActivityAt = DateTime.UtcNow.AddMinutes(-2);
        await _dbContext.SaveChangesAsync();

        var debounceThreshold = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(1));

        var session = await _dbContext.Sessions
            .Where(s => s.UserId == _testUser.Id && s.IsActive)
            .Where(s => s.LastActivityAt < debounceThreshold)
            .FirstOrDefaultAsync();

        Assert.NotNull(session);
    }

    [Fact]
    public async Task ActivityTracking_SkipsUpdate_WhenWithinDebounce()
    {
        _testSession.LastActivityAt = DateTime.UtcNow.AddSeconds(-30);
        await _dbContext.SaveChangesAsync();

        var debounceThreshold = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(1));

        var session = await _dbContext.Sessions
            .Where(s => s.UserId == _testUser.Id && s.IsActive)
            .Where(s => s.LastActivityAt < debounceThreshold)
            .FirstOrDefaultAsync();

        Assert.Null(session);
    }
}
