using bmadServer.ApiService.Data;
using bmadServer.ApiService.Data.Entities;
using bmadServer.ApiService.Services;
using bmadServer.Tests.Helpers;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace bmadServer.Tests.Unit;

public class InSessionPersonaSwitchingTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly SqliteConnection _connection;
    private readonly Mock<ILogger<SessionService>> _loggerMock;
    private readonly SessionService _sessionService;
    private readonly User _testUser;
    private readonly Session _testSession;

    public InSessionPersonaSwitchingTests()
    {
        var options = TestDatabaseHelper.CreateSqliteOptions(out _connection);
        _dbContext = new ApplicationDbContext(options);
        _dbContext.Database.EnsureCreated();
        _loggerMock = new Mock<ILogger<SessionService>>();
        _sessionService = new SessionService(_dbContext, _loggerMock.Object);

        _testUser = new User
        {
            Email = "test@example.com",
            PasswordHash = "hash",
            DisplayName = "Test User",
            PersonaType = PersonaType.Technical // User's default is Technical
        };

        _dbContext.Users.Add(_testUser);
        _dbContext.SaveChanges();

        _testSession = new Session
        {
            UserId = _testUser.Id,
            ConnectionId = "conn-123",
            IsActive = true
        };

        _dbContext.Sessions.Add(_testSession);
        _dbContext.SaveChanges();
    }

    [Fact]
    public async Task SwitchSessionPersona_ChangesSessionPersona_NotUserDefault()
    {
        // Arrange - User default is Technical
        Assert.Equal(PersonaType.Technical, _testUser.PersonaType);
        Assert.Null(_testSession.SessionPersona);

        // Act - Switch session to Business
        var result = await _sessionService.SwitchSessionPersonaAsync(
            _testSession.Id,
            _testUser.Id,
            PersonaType.Business
        );

        // Assert
        Assert.True(result.Success);
        Assert.Equal(PersonaType.Business, result.NewPersona);
        Assert.Equal(PersonaType.Technical, result.PreviousPersona); // Previous was user default
        Assert.Equal(1, result.SwitchCount);

        // Verify session updated
        var session = await _dbContext.Sessions.FindAsync(_testSession.Id);
        Assert.NotNull(session);
        Assert.Equal(PersonaType.Business, session.SessionPersona);
        Assert.Equal(1, session.PersonaSwitchCount);

        // Verify user default unchanged
        var user = await _dbContext.Users.FindAsync(_testUser.Id);
        Assert.NotNull(user);
        Assert.Equal(PersonaType.Technical, user.PersonaType);
    }

    [Fact]
    public async Task SwitchSessionPersona_MultipleChanges_IncrementsCounter()
    {
        // Act - Switch 1: Technical -> Business
        var result1 = await _sessionService.SwitchSessionPersonaAsync(
            _testSession.Id,
            _testUser.Id,
            PersonaType.Business
        );

        // Assert 1
        Assert.Equal(PersonaType.Business, result1.NewPersona);
        Assert.Equal(1, result1.SwitchCount);
        Assert.Null(result1.SuggestionMessage);

        // Act - Switch 2: Business -> Hybrid
        var result2 = await _sessionService.SwitchSessionPersonaAsync(
            _testSession.Id,
            _testUser.Id,
            PersonaType.Hybrid
        );

        // Assert 2
        Assert.Equal(PersonaType.Hybrid, result2.NewPersona);
        Assert.Equal(PersonaType.Business, result2.PreviousPersona);
        Assert.Equal(2, result2.SwitchCount);
        Assert.Null(result2.SuggestionMessage);

        // Act - Switch 3: Hybrid -> Technical
        var result3 = await _sessionService.SwitchSessionPersonaAsync(
            _testSession.Id,
            _testUser.Id,
            PersonaType.Technical
        );

        // Assert 3
        Assert.Equal(PersonaType.Technical, result3.NewPersona);
        Assert.Equal(3, result3.SwitchCount);
        Assert.Null(result3.SuggestionMessage);

        // Act - Switch 4: Should suggest Hybrid
        var result4 = await _sessionService.SwitchSessionPersonaAsync(
            _testSession.Id,
            _testUser.Id,
            PersonaType.Business
        );

        // Assert 4 - After 3+ switches, suggest Hybrid
        Assert.Equal(4, result4.SwitchCount);
        Assert.NotNull(result4.SuggestionMessage);
        Assert.Contains("Hybrid mode", result4.SuggestionMessage);
    }

    [Fact]
    public async Task GetEffectivePersona_UsesSessionPersonaIfSet()
    {
        // Arrange - Set session persona
        await _sessionService.SwitchSessionPersonaAsync(
            _testSession.Id,
            _testUser.Id,
            PersonaType.Business
        );

        // Act
        var effectivePersona = await _sessionService.GetEffectivePersonaAsync(_testSession.Id, _testUser.Id);

        // Assert - Should use session persona, not user default
        Assert.Equal(PersonaType.Business, effectivePersona);
    }

    [Fact]
    public async Task GetEffectivePersona_UsesUserDefaultIfNoSessionPersona()
    {
        // Arrange - No session persona set
        Assert.Null(_testSession.SessionPersona);

        // Act
        var effectivePersona = await _sessionService.GetEffectivePersonaAsync(_testSession.Id, _testUser.Id);

        // Assert - Should use user default
        Assert.Equal(PersonaType.Technical, effectivePersona);
    }

    [Fact]
    public async Task SwitchSessionPersona_LogsAnalytics()
    {
        // Act
        await _sessionService.SwitchSessionPersonaAsync(
            _testSession.Id,
            _testUser.Id,
            PersonaType.Hybrid
        );

        // Assert - Verify logging occurred
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Switched session persona")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SwitchSessionPersona_WithInvalidSession_ReturnsFalse()
    {
        // Act
        var result = await _sessionService.SwitchSessionPersonaAsync(
            Guid.NewGuid(),
            _testUser.Id,
            PersonaType.Business
        );

        // Assert
        Assert.False(result.Success);
    }

    [Fact]
    public async Task SwitchSessionPersona_WithDifferentUser_ReturnsFalse()
    {
        // Act - Try to switch another user's session
        var result = await _sessionService.SwitchSessionPersonaAsync(
            _testSession.Id,
            Guid.NewGuid(),
            PersonaType.Business
        );

        // Assert
        Assert.False(result.Success);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }
}
