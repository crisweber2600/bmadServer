using FluentAssertions;
using bmadServer.ServiceDefaults.Models.Workflows;
using bmadServer.ServiceDefaults.Services.Workflows;
using Xunit;

namespace bmadServer.Tests.Unit.Services.Workflows;

public class WorkflowRegistryTests
{
    private readonly IWorkflowRegistry _registry;

    public WorkflowRegistryTests()
    {
        _registry = new WorkflowRegistry();
    }

    [Fact]
    public void GetAllWorkflows_ShouldReturnAllRegisteredWorkflows()
    {
        // Act
        var workflows = _registry.GetAllWorkflows();

        // Assert
        workflows.Should().NotBeEmpty();
        workflows.Should().Contain(w => w.WorkflowId == "create-prd");
        workflows.Should().Contain(w => w.WorkflowId == "create-architecture");
        workflows.Should().Contain(w => w.WorkflowId == "create-stories");
        workflows.Should().Contain(w => w.WorkflowId == "design-ux");
    }

    [Fact]
    public void GetWorkflow_WithValidId_ShouldReturnWorkflowDefinition()
    {
        // Arrange
        const string workflowId = "create-prd";

        // Act
        var workflow = _registry.GetWorkflow(workflowId);

        // Assert
        workflow.Should().NotBeNull();
        workflow!.WorkflowId.Should().Be(workflowId);
        workflow.Name.Should().NotBeNullOrEmpty();
        workflow.Description.Should().NotBeNullOrEmpty();
        workflow.Steps.Should().NotBeEmpty();
    }

    [Fact]
    public void GetWorkflow_WithInvalidId_ShouldReturnNull()
    {
        // Arrange
        const string invalidId = "non-existent-workflow";

        // Act
        var workflow = _registry.GetWorkflow(invalidId);

        // Assert
        workflow.Should().BeNull();
    }

    [Fact]
    public void GetWorkflow_WithNullId_ShouldReturnNull()
    {
        // Act
        var workflow = _registry.GetWorkflow(null!);

        // Assert
        workflow.Should().BeNull();
    }

    [Fact]
    public void GetWorkflow_WithEmptyId_ShouldReturnNull()
    {
        // Act
        var workflow = _registry.GetWorkflow(string.Empty);

        // Assert
        workflow.Should().BeNull();
    }

    [Fact]
    public void ValidateWorkflow_WithValidId_ShouldReturnTrue()
    {
        // Arrange
        const string workflowId = "create-prd";

        // Act
        var isValid = _registry.ValidateWorkflow(workflowId);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateWorkflow_WithInvalidId_ShouldReturnFalse()
    {
        // Arrange
        const string invalidId = "non-existent-workflow";

        // Act
        var isValid = _registry.ValidateWorkflow(invalidId);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void WorkflowDefinition_ShouldHaveRequiredProperties()
    {
        // Arrange
        var workflow = _registry.GetWorkflow("create-prd");

        // Assert
        workflow.Should().NotBeNull();
        workflow!.WorkflowId.Should().NotBeNullOrEmpty();
        workflow.Name.Should().NotBeNullOrEmpty();
        workflow.Description.Should().NotBeNullOrEmpty();
        workflow.EstimatedDuration.Should().BeGreaterThan(TimeSpan.Zero);
        workflow.RequiredRoles.Should().NotBeNull();
        workflow.Steps.Should().NotBeEmpty();
    }

    [Fact]
    public void WorkflowStep_ShouldHaveRequiredProperties()
    {
        // Arrange
        var workflow = _registry.GetWorkflow("create-prd");

        // Assert
        workflow.Should().NotBeNull();
        var step = workflow!.Steps.First();
        step.StepId.Should().NotBeNullOrEmpty();
        step.Name.Should().NotBeNullOrEmpty();
        step.AgentId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GetAllWorkflows_ShouldReturnImmutableCollection()
    {
        // Act
        var workflows = _registry.GetAllWorkflows();

        // Assert
        workflows.Should().BeAssignableTo<IReadOnlyList<WorkflowDefinition>>();
    }
}
