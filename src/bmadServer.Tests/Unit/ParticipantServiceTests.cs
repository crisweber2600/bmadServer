using bmadServer.ApiService.Data;
using bmadServer.ApiService.Data.Entities;
using bmadServer.ApiService.Models.Workflows;
using bmadServer.ApiService.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace bmadServer.Tests.Unit;

public class ParticipantServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ParticipantService _service;
    private readonly Guid _testUserId;
    private readonly Guid _testOwnerId;
    private readonly Guid _testWorkflowId;

    public ParticipantServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _service = new ParticipantService(_context);

        _testUserId = Guid.NewGuid();
        _testOwnerId = Guid.NewGuid();
        _testWorkflowId = Guid.NewGuid();

        // Seed test data
        _context.Users.AddRange(
            new User { Id = _testUserId, Email = "test@example.com", PasswordHash = "hash", DisplayName = "Test User" },
            new User { Id = _testOwnerId, Email = "owner@example.com", PasswordHash = "hash", DisplayName = "Owner User" }
        );
        _context.WorkflowInstances.Add(new WorkflowInstance
        {
            Id = _testWorkflowId,
            WorkflowDefinitionId = "test-workflow",
            UserId = _testOwnerId,
            Status = WorkflowStatus.Running,
            CurrentStep = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        _context.SaveChanges();
    }

    [Fact]
    public async Task AddParticipantAsync_ShouldAddParticipant_WhenValid()
    {
        // Act
        var result = await _service.AddParticipantAsync(
            _testWorkflowId, 
            _testUserId, 
            ParticipantRole.Contributor, 
            _testOwnerId
        );

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_testWorkflowId, result.WorkflowId);
        Assert.Equal(_testUserId, result.UserId);
        Assert.Equal(ParticipantRole.Contributor, result.Role);
        Assert.Equal(_testOwnerId, result.AddedBy);
    }

    [Fact]
    public async Task AddParticipantAsync_ShouldThrow_WhenUserNotFound()
    {
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _service.AddParticipantAsync(
                _testWorkflowId,
                Guid.NewGuid(),
                ParticipantRole.Contributor,
                _testOwnerId
            )
        );
    }

    [Fact]
    public async Task AddParticipantAsync_ShouldThrow_WhenParticipantAlreadyExists()
    {
        // Arrange
        await _service.AddParticipantAsync(
            _testWorkflowId,
            _testUserId,
            ParticipantRole.Contributor,
            _testOwnerId
        );

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _service.AddParticipantAsync(
                _testWorkflowId,
                _testUserId,
                ParticipantRole.Observer,
                _testOwnerId
            )
        );
    }

    [Fact]
    public async Task RemoveParticipantAsync_ShouldRemoveParticipant_WhenExists()
    {
        // Arrange
        await _service.AddParticipantAsync(
            _testWorkflowId,
            _testUserId,
            ParticipantRole.Contributor,
            _testOwnerId
        );

        // Act
        var result = await _service.RemoveParticipantAsync(_testWorkflowId, _testUserId);

        // Assert
        Assert.True(result);
        var participant = await _service.GetParticipantAsync(_testWorkflowId, _testUserId);
        Assert.Null(participant);
    }

    [Fact]
    public async Task GetParticipantsAsync_ShouldReturnAllParticipants()
    {
        // Arrange
        await _service.AddParticipantAsync(
            _testWorkflowId,
            _testUserId,
            ParticipantRole.Contributor,
            _testOwnerId
        );

        // Act
        var participants = await _service.GetParticipantsAsync(_testWorkflowId);

        // Assert
        Assert.Single(participants);
        Assert.Equal(_testUserId, participants.First().UserId);
    }

    [Fact]
    public async Task IsParticipantAsync_ShouldReturnTrue_WhenUserIsParticipant()
    {
        // Arrange
        await _service.AddParticipantAsync(
            _testWorkflowId,
            _testUserId,
            ParticipantRole.Contributor,
            _testOwnerId
        );

        // Act
        var result = await _service.IsParticipantAsync(_testWorkflowId, _testUserId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsParticipantAsync_ShouldReturnFalse_WhenUserIsNotParticipant()
    {
        // Act
        var result = await _service.IsParticipantAsync(_testWorkflowId, _testUserId);

        // Assert
        Assert.False(result);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
