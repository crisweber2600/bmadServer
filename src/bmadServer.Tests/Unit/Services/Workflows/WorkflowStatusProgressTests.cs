using bmadServer.ApiService.Data;
using bmadServer.ApiService.DTOs;
using bmadServer.ApiService.Models.Workflows;
using bmadServer.ApiService.Services.Workflows;
using bmadServer.ApiService.Services.Workflows.Agents;
using bmadServer.ServiceDefaults.Models.Workflows;
using bmadServer.ServiceDefaults.Services.Workflows;
using bmadServer.Tests.Helpers;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace bmadServer.Tests.Unit.Services.Workflows;

public class WorkflowStatusProgressTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly SqliteConnection _connection;
    private readonly Mock<IWorkflowRegistry> _registryMock;
    private readonly Mock<IAgentRegistry> _agentRegistryMock;
    private readonly Mock<IAgentHandoffService> _agentHandoffServiceMock;
    private readonly Mock<ILogger<WorkflowInstanceService>> _loggerMock;
    private readonly IWorkflowInstanceService _service;

    public WorkflowStatusProgressTests()
    {
        var options = TestDatabaseHelper.CreateSqliteOptions(out _connection);
        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();

        _registryMock = new Mock<IWorkflowRegistry>();
        _agentRegistryMock = new Mock<IAgentRegistry>();
        _agentHandoffServiceMock = new Mock<IAgentHandoffService>();
        _loggerMock = new Mock<ILogger<WorkflowInstanceService>>();
        _service = new WorkflowInstanceService(_context, _registryMock.Object, _agentRegistryMock.Object, _agentHandoffServiceMock.Object, _loggerMock.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task GetWorkflowStatusAsync_WithValidInstance_ShouldReturnStatus()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var workflowId = "create-prd";
        var instance = new WorkflowInstance
        {
            Id = Guid.NewGuid(),
            WorkflowDefinitionId = workflowId,
            UserId = userId,
            Status = WorkflowStatus.Running,
            CurrentStep = 2,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.WorkflowInstances.Add(instance);
        await _context.SaveChangesAsync();

        var workflowDef = new WorkflowDefinition
        {
            WorkflowId = workflowId,
            Name = "Create PRD",
            Description = "Test workflow",
            EstimatedDuration = TimeSpan.FromMinutes(30),
            RequiredRoles = new List<string>(),
            Steps = new List<WorkflowStep>
            {
                new() { StepId = "step1", Name = "Step 1", AgentId = "agent1", IsOptional = false, CanSkip = false },
                new() { StepId = "step2", Name = "Step 2", AgentId = "agent2", IsOptional = false, CanSkip = false },
                new() { StepId = "step3", Name = "Step 3", AgentId = "agent3", IsOptional = false, CanSkip = false }
            }
        };
        _registryMock.Setup(r => r.GetWorkflow(workflowId)).Returns(workflowDef);

        // Act
        var result = await _service.GetWorkflowStatusAsync(instance.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(instance.Id);
        result.WorkflowId.Should().Be(workflowId);
        result.Name.Should().Be("Create PRD");
        result.Status.Should().Be("Running");
        result.CurrentStep.Should().Be(2);
        result.TotalSteps.Should().Be(3);
        result.Steps.Should().HaveCount(3);
        result.Steps[0].Status.Should().Be("Completed");
        result.Steps[1].Status.Should().Be("Current");
        result.Steps[2].Status.Should().Be("Pending");
    }

    [Fact]
    public async Task GetWorkflowStatusAsync_WithStepHistory_ShouldIncludeCompletionTimes()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var workflowId = "create-prd";
        var instance = new WorkflowInstance
        {
            Id = Guid.NewGuid(),
            WorkflowDefinitionId = workflowId,
            UserId = userId,
            Status = WorkflowStatus.Running,
            CurrentStep = 2,
            CreatedAt = DateTime.UtcNow.AddMinutes(-10),
            UpdatedAt = DateTime.UtcNow
        };
        _context.WorkflowInstances.Add(instance);

        var completedAt = DateTime.UtcNow.AddMinutes(-5);
        var stepHistory = new WorkflowStepHistory
        {
            Id = Guid.NewGuid(),
            WorkflowInstanceId = instance.Id,
            StepId = "step1",
            StepName = "Step 1",
            StartedAt = DateTime.UtcNow.AddMinutes(-10),
            CompletedAt = completedAt,
            Status = StepExecutionStatus.Completed
        };
        _context.WorkflowStepHistories.Add(stepHistory);
        await _context.SaveChangesAsync();

        var workflowDef = new WorkflowDefinition
        {
            WorkflowId = workflowId,
            Name = "Create PRD",
            Description = "Test workflow",
            EstimatedDuration = TimeSpan.FromMinutes(30),
            RequiredRoles = new List<string>(),
            Steps = new List<WorkflowStep>
            {
                new() { StepId = "step1", Name = "Step 1", AgentId = "agent1", IsOptional = false, CanSkip = false },
                new() { StepId = "step2", Name = "Step 2", AgentId = "agent2", IsOptional = false, CanSkip = false }
            }
        };
        _registryMock.Setup(r => r.GetWorkflow(workflowId)).Returns(workflowDef);

        // Act
        var result = await _service.GetWorkflowStatusAsync(instance.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Steps[0].CompletedAt.Should().BeCloseTo(completedAt, TimeSpan.FromSeconds(1));
        result.Steps[0].Status.Should().Be("Completed");
    }

    [Fact]
    public void CalculateProgress_WithNoSteps_ShouldReturn0()
    {
        // Arrange
        var instance = new WorkflowInstance
        {
            Id = Guid.NewGuid(),
            WorkflowDefinitionId = "test",
            UserId = Guid.NewGuid(),
            Status = WorkflowStatus.Running,
            CurrentStep = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        var result = _service.CalculateProgress(instance, 0);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void CalculateProgress_WithCompletedWorkflow_ShouldReturn100()
    {
        // Arrange
        var instance = new WorkflowInstance
        {
            Id = Guid.NewGuid(),
            WorkflowDefinitionId = "test",
            UserId = Guid.NewGuid(),
            Status = WorkflowStatus.Completed,
            CurrentStep = 5,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        var result = _service.CalculateProgress(instance, 5);

        // Assert
        result.Should().Be(100);
    }

    [Fact]
    public void CalculateProgress_AtStep3Of5_ShouldReturn40Percent()
    {
        // Arrange - on step 3 means steps 1 and 2 completed
        var instance = new WorkflowInstance
        {
            Id = Guid.NewGuid(),
            WorkflowDefinitionId = "test",
            UserId = Guid.NewGuid(),
            Status = WorkflowStatus.Running,
            CurrentStep = 3,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        var result = _service.CalculateProgress(instance, 5);

        // Assert
        result.Should().Be(40); // 2 completed out of 5 = 40%
    }

    [Fact]
    public async Task EstimateCompletionAsync_WithCompletedSteps_ShouldUseAverageDuration()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var workflowId = "create-prd";
        var instance = new WorkflowInstance
        {
            Id = Guid.NewGuid(),
            WorkflowDefinitionId = workflowId,
            UserId = userId,
            Status = WorkflowStatus.Running,
            CurrentStep = 2,
            CreatedAt = DateTime.UtcNow.AddMinutes(-20),
            UpdatedAt = DateTime.UtcNow
        };
        _context.WorkflowInstances.Add(instance);

        // Add two completed steps with 10 minutes each
        var step1 = new WorkflowStepHistory
        {
            Id = Guid.NewGuid(),
            WorkflowInstanceId = instance.Id,
            StepId = "step1",
            StepName = "Step 1",
            StartedAt = DateTime.UtcNow.AddMinutes(-20),
            CompletedAt = DateTime.UtcNow.AddMinutes(-10),
            Status = StepExecutionStatus.Completed
        };
        var step2 = new WorkflowStepHistory
        {
            Id = Guid.NewGuid(),
            WorkflowInstanceId = instance.Id,
            StepId = "step2",
            StepName = "Step 2",
            StartedAt = DateTime.UtcNow.AddMinutes(-10),
            CompletedAt = DateTime.UtcNow,
            Status = StepExecutionStatus.Completed
        };
        _context.WorkflowStepHistories.AddRange(step1, step2);
        await _context.SaveChangesAsync();

        var workflowDef = new WorkflowDefinition
        {
            WorkflowId = workflowId,
            Name = "Create PRD",
            Description = "Test workflow",
            EstimatedDuration = TimeSpan.FromMinutes(60),
            RequiredRoles = new List<string>(),
            Steps = new List<WorkflowStep>
            {
                new() { StepId = "step1", Name = "Step 1", AgentId = "agent1", IsOptional = false, CanSkip = false },
                new() { StepId = "step2", Name = "Step 2", AgentId = "agent2", IsOptional = false, CanSkip = false },
                new() { StepId = "step3", Name = "Step 3", AgentId = "agent3", IsOptional = false, CanSkip = false },
                new() { StepId = "step4", Name = "Step 4", AgentId = "agent4", IsOptional = false, CanSkip = false }
            }
        };
        _registryMock.Setup(r => r.GetWorkflow(workflowId)).Returns(workflowDef);

        // Act
        var result = await _service.EstimateCompletionAsync(instance.Id);

        // Assert
        result.Should().NotBeNull();
        // Average is 10 minutes per step, 3 steps remaining (current step 2, so steps 2, 3, 4 remaining)
        // Should be around 30 minutes from now
        var expected = DateTime.UtcNow.AddMinutes(30);
        result.Should().BeCloseTo(expected, TimeSpan.FromMinutes(5));
    }

    [Fact]
    public async Task EstimateCompletionAsync_WithTerminalStatus_ShouldReturnNull()
    {
        // Arrange
        var instance = new WorkflowInstance
        {
            Id = Guid.NewGuid(),
            WorkflowDefinitionId = "test",
            UserId = Guid.NewGuid(),
            Status = WorkflowStatus.Completed,
            CurrentStep = 5,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.WorkflowInstances.Add(instance);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.EstimateCompletionAsync(instance.Id);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetFilteredWorkflowsAsync_WithStatusFilter_ShouldReturnMatchingWorkflows()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var runningInstance = new WorkflowInstance
        {
            Id = Guid.NewGuid(),
            WorkflowDefinitionId = "test",
            UserId = userId,
            Status = WorkflowStatus.Running,
            CurrentStep = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var pausedInstance = new WorkflowInstance
        {
            Id = Guid.NewGuid(),
            WorkflowDefinitionId = "test",
            UserId = userId,
            Status = WorkflowStatus.Paused,
            CurrentStep = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.WorkflowInstances.AddRange(runningInstance, pausedInstance);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetFilteredWorkflowsAsync(userId, status: WorkflowStatus.Running);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].Status.Should().Be(WorkflowStatus.Running);
        result.TotalItems.Should().Be(1);
    }

    [Fact]
    public async Task GetFilteredWorkflowsAsync_WithWorkflowTypeFilter_ShouldReturnMatchingWorkflows()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var prdInstance = new WorkflowInstance
        {
            Id = Guid.NewGuid(),
            WorkflowDefinitionId = "create-prd",
            UserId = userId,
            Status = WorkflowStatus.Running,
            CurrentStep = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var otherInstance = new WorkflowInstance
        {
            Id = Guid.NewGuid(),
            WorkflowDefinitionId = "other-workflow",
            UserId = userId,
            Status = WorkflowStatus.Running,
            CurrentStep = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.WorkflowInstances.AddRange(prdInstance, otherInstance);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetFilteredWorkflowsAsync(userId, workflowType: "create-prd");

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].WorkflowDefinitionId.Should().Be("create-prd");
    }

    [Fact]
    public async Task GetFilteredWorkflowsAsync_WithDateRangeFilter_ShouldReturnMatchingWorkflows()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var oldInstance = new WorkflowInstance
        {
            Id = Guid.NewGuid(),
            WorkflowDefinitionId = "test",
            UserId = userId,
            Status = WorkflowStatus.Running,
            CurrentStep = 1,
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            UpdatedAt = DateTime.UtcNow
        };
        var recentInstance = new WorkflowInstance
        {
            Id = Guid.NewGuid(),
            WorkflowDefinitionId = "test",
            UserId = userId,
            Status = WorkflowStatus.Running,
            CurrentStep = 1,
            CreatedAt = DateTime.UtcNow.AddHours(-1),
            UpdatedAt = DateTime.UtcNow
        };
        _context.WorkflowInstances.AddRange(oldInstance, recentInstance);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetFilteredWorkflowsAsync(
            userId, 
            createdAfter: DateTime.UtcNow.AddDays(-1));

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].Id.Should().Be(recentInstance.Id);
    }

    [Fact]
    public async Task GetFilteredWorkflowsAsync_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        var userId = Guid.NewGuid();
        for (int i = 0; i < 25; i++)
        {
            var instance = new WorkflowInstance
            {
                Id = Guid.NewGuid(),
                WorkflowDefinitionId = "test",
                UserId = userId,
                Status = WorkflowStatus.Running,
                CurrentStep = 1,
                CreatedAt = DateTime.UtcNow.AddMinutes(-i),
                UpdatedAt = DateTime.UtcNow
            };
            _context.WorkflowInstances.Add(instance);
        }
        await _context.SaveChangesAsync();

        // Act
        var page1 = await _service.GetFilteredWorkflowsAsync(userId, page: 1, pageSize: 10);
        var page2 = await _service.GetFilteredWorkflowsAsync(userId, page: 2, pageSize: 10);

        // Assert
        page1.Items.Should().HaveCount(10);
        page1.TotalItems.Should().Be(25);
        page1.TotalPages.Should().Be(3);
        page1.HasNext.Should().BeTrue();
        page1.HasPrevious.Should().BeFalse();

        page2.Items.Should().HaveCount(10);
        page2.Page.Should().Be(2);
        page2.HasNext.Should().BeTrue();
        page2.HasPrevious.Should().BeTrue();
    }
}
