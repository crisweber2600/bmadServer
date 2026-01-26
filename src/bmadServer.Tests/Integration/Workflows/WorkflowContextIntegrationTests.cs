using bmadServer.ApiService.Services.Workflows.Agents;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using Xunit;
using FluentAssertions;

namespace bmadServer.Tests.Integration.Workflows;

public class WorkflowContextIntegrationTests
{
    private readonly IWorkflowContextManager _contextManager;

    public WorkflowContextIntegrationTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IWorkflowContextManager, WorkflowContextManager>();
        
        var serviceProvider = services.BuildServiceProvider();
        _contextManager = serviceProvider.GetRequiredService<IWorkflowContextManager>();
    }

    [Fact]
    public void GetOrCreateContext_CreatesNewContextForNewWorkflow()
    {
        // Arrange
        var workflowId = Guid.NewGuid();

        // Act
        var context = _contextManager.GetOrCreateContext(workflowId);

        // Assert
        context.Should().NotBeNull();
        context.Version.Should().Be(0);
    }

    [Fact]
    public void GetOrCreateContext_ReturnsSameInstanceForSameWorkflow()
    {
        // Arrange
        var workflowId = Guid.NewGuid();

        // Act
        var context1 = _contextManager.GetOrCreateContext(workflowId);
        context1.AddStepOutput("step-1", JsonDocument.Parse("{\"test\": true}"));
        
        var context2 = _contextManager.GetOrCreateContext(workflowId);

        // Assert
        context2.Should().BeSameAs(context1);
        context2.GetStepOutput("step-1").Should().NotBeNull();
    }

    [Fact]
    public void TryGetContext_ExistingWorkflow_ReturnsTrue()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        _contextManager.GetOrCreateContext(workflowId);

        // Act
        var found = _contextManager.TryGetContext(workflowId, out var context);

        // Assert
        found.Should().BeTrue();
        context.Should().NotBeNull();
    }

    [Fact]
    public void TryGetContext_NonExistentWorkflow_ReturnsFalse()
    {
        // Arrange
        var workflowId = Guid.NewGuid();

        // Act
        var found = _contextManager.TryGetContext(workflowId, out var context);

        // Assert
        found.Should().BeFalse();
        context.Should().BeNull();
    }

    [Fact]
    public void RemoveContext_RemovesContextSuccessfully()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        _contextManager.GetOrCreateContext(workflowId);

        // Act
        _contextManager.RemoveContext(workflowId);
        var found = _contextManager.TryGetContext(workflowId, out _);

        // Assert
        found.Should().BeFalse();
    }

    [Fact]
    public void GetActiveContextCount_TracksMultipleWorkflows()
    {
        // Arrange
        var workflow1 = Guid.NewGuid();
        var workflow2 = Guid.NewGuid();
        var workflow3 = Guid.NewGuid();

        // Act
        _contextManager.GetOrCreateContext(workflow1);
        _contextManager.GetOrCreateContext(workflow2);
        _contextManager.GetOrCreateContext(workflow3);
        var count = _contextManager.GetActiveContextCount();

        // Assert
        count.Should().BeGreaterOrEqualTo(3);
    }

    [Fact]
    public void ConcurrentAccess_MaintainsContextIntegrity()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var context = _contextManager.GetOrCreateContext(workflowId);

        // Act - Simulate concurrent access
        Parallel.For(0, 10, i =>
        {
            var stepOutput = JsonDocument.Parse($"{{\"step\": {i}}}");
            context.AddStepOutput($"step-{i}", stepOutput);
        });

        // Assert
        var allOutputs = context.GetAllStepOutputs();
        allOutputs.Should().HaveCount(10);
        context.Version.Should().Be(10);
    }
}
