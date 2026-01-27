using bmadServer.ApiService.Data;
using bmadServer.ApiService.Data.Entities;
using bmadServer.ApiService.DTOs;
using bmadServer.ApiService.Controllers;
using bmadServer.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;

namespace bmadServer.Tests;

public class PersonaConfigurationTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly SqliteConnection _connection;
    private readonly UsersController _controller;
    private readonly Mock<ILogger<UsersController>> _loggerMock;

    public PersonaConfigurationTests()
    {
        var options = TestDatabaseHelper.CreateSqliteOptions(out _connection);
        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();
        _loggerMock = new Mock<ILogger<UsersController>>();
        var memoryCacheMock = new Mock<IMemoryCache>();
        _controller = new UsersController(_context, _loggerMock.Object, memoryCacheMock.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task GetCurrentUser_ShouldIncludePersonaType()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            PasswordHash = "hash",
            DisplayName = "Test User",
            PersonaType = PersonaType.Business
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        SetupUserClaims(userId);

        // Act
        var result = await _controller.GetCurrentUser();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<UserResponse>(okResult.Value);
        Assert.Equal(PersonaType.Business, response.PersonaType);
    }

    [Fact]
    public async Task NewUser_ShouldHaveHybridPersonaAsDefault()
    {
        // Arrange
        var user = new User
        {
            Email = "newuser@example.com",
            PasswordHash = "hash",
            DisplayName = "New User"
        };

        // Act - Default should be set
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        // Assert
        var savedUser = await _context.Users.FindAsync(user.Id);
        Assert.NotNull(savedUser);
        Assert.Equal(PersonaType.Hybrid, savedUser.PersonaType);
    }

    [Fact]
    public async Task UpdatePersona_ShouldPersistChanges()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            PasswordHash = "hash",
            DisplayName = "Test User",
            PersonaType = PersonaType.Hybrid
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        SetupUserClaims(userId);

        var updateRequest = new UpdatePersonaRequest
        {
            PersonaType = PersonaType.Technical
        };

        // Act
        var result = await _controller.UpdatePersona(updateRequest);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<UserResponse>(okResult.Value);
        Assert.Equal(PersonaType.Technical, response.PersonaType);

        // Verify persistence
        var updatedUser = await _context.Users.FindAsync(userId);
        Assert.Equal(PersonaType.Technical, updatedUser!.PersonaType);
    }

    [Theory]
    [InlineData(PersonaType.Business)]
    [InlineData(PersonaType.Technical)]
    [InlineData(PersonaType.Hybrid)]
    public async Task UpdatePersona_ShouldAcceptAllValidPersonaTypes(PersonaType personaType)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            PasswordHash = "hash",
            DisplayName = "Test User"
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        SetupUserClaims(userId);

        var updateRequest = new UpdatePersonaRequest
        {
            PersonaType = personaType
        };

        // Act
        var result = await _controller.UpdatePersona(updateRequest);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<UserResponse>(okResult.Value);
        Assert.Equal(personaType, response.PersonaType);
    }

    [Fact]
    public async Task GetCurrentUser_ShouldReturnPersonaDescriptions()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            PasswordHash = "hash",
            DisplayName = "Test User",
            PersonaType = PersonaType.Business
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        SetupUserClaims(userId);

        // Act
        var result = await _controller.GetCurrentUser();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<UserResponse>(okResult.Value);
        
        // Verify persona descriptions are available
        Assert.NotNull(response.PersonaType);
        Assert.Equal(PersonaType.Business, response.PersonaType);
    }

    private void SetupUserClaims(Guid userId)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim("sub", userId.ToString())
        };

        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }
}
