using bmadServer.ApiService.Services.Workflows.Agents;
using System.Text.Json;
using Xunit;
using FluentAssertions;

namespace bmadServer.Tests.Services.Workflows.Agents;

public class SharedWorkflowContextTests
{
    private readonly SharedWorkflowContext _context;

    public SharedWorkflowContextTests()
    {
        _context = new SharedWorkflowContext();
    }

    [Fact]
    public void AddStepOutput_ValidStep_StoresOutput()
    {
        // Arrange
        var stepId = "step-1";
        var output = JsonDocument.Parse("{\"result\": \"success\"}");

        // Act
        _context.AddStepOutput(stepId, output);
        var retrieved = _context.GetStepOutput(stepId);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.RootElement.GetProperty("result").GetString().Should().Be("success");
    }

    [Fact]
    public void GetStepOutput_NonExistentStep_ReturnsNull()
    {
        // Arrange
        var stepId = "nonexistent";

        // Act
        var result = _context.GetStepOutput(stepId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetAllStepOutputs_AfterMultipleAdds_ReturnsAll()
    {
        // Arrange
        _context.AddStepOutput("step-1", JsonDocument.Parse("{\"data\": \"one\"}"));
        _context.AddStepOutput("step-2", JsonDocument.Parse("{\"data\": \"two\"}"));
        _context.AddStepOutput("step-3", JsonDocument.Parse("{\"data\": \"three\"}"));

        // Act
        var all = _context.GetAllStepOutputs();

        // Assert
        all.Should().HaveCount(3);
        all.Should().ContainKey("step-1");
        all.Should().ContainKey("step-2");
        all.Should().ContainKey("step-3");
    }

    [Fact]
    public void AddDecision_ValidDecision_StoresInHistory()
    {
        // Arrange
        var decision = new WorkflowDecision
        {
            DecisionId = "decision-1",
            StepId = "step-1",
            DecisionType = "approval",
            Outcome = "approved",
            Timestamp = DateTime.UtcNow
        };

        // Act
        _context.AddDecision(decision);
        var history = _context.GetDecisionHistory();

        // Assert
        history.Should().ContainSingle();
        history.First().DecisionId.Should().Be("decision-1");
    }

    [Fact]
    public void AddUserPreference_ValidPreference_Stores()
    {
        // Arrange
        var key = "theme";
        var value = "dark";

        // Act
        _context.AddUserPreference(key, value);
        var retrieved = _context.GetUserPreference(key);

        // Assert
        retrieved.Should().Be(value);
    }

    [Fact]
    public void AddArtifactReference_ValidReference_Stores()
    {
        // Arrange
        var artifactId = "artifact-123";
        var path = "/artifacts/design.pdf";

        // Act
        _context.AddArtifactReference(artifactId, path);
        var references = _context.GetArtifactReferences();

        // Assert
        references.Should().ContainKey(artifactId);
        references[artifactId].Should().Be(path);
    }

    [Fact]
    public void VersionNumber_AfterChanges_Increments()
    {
        // Arrange
        var initialVersion = _context.Version;

        // Act
        _context.AddStepOutput("step-1", JsonDocument.Parse("{\"test\": true}"));
        var afterAdd = _context.Version;

        _context.AddDecision(new WorkflowDecision
        {
            DecisionId = "decision-1",
            StepId = "step-1",
            DecisionType = "test",
            Outcome = "pass",
            Timestamp = DateTime.UtcNow
        });
        var afterDecision = _context.Version;

        // Assert
        afterAdd.Should().BeGreaterThan(initialVersion);
        afterDecision.Should().BeGreaterThan(afterAdd);
    }

    [Fact]
    public void ToJson_WithData_SerializesSuccessfully()
    {
        // Arrange
        _context.AddStepOutput("step-1", JsonDocument.Parse("{\"test\": true}"));
        _context.AddUserPreference("theme", "dark");
        _context.AddDecision(new WorkflowDecision
        {
            DecisionId = "decision-1",
            StepId = "step-1",
            DecisionType = "test",
            Outcome = "pass",
            Timestamp = DateTime.UtcNow
        });

        // Act
        var json = _context.ToJson();

        // Assert
        json.Should().NotBeNull();
        json.RootElement.GetProperty("version").GetInt32().Should().BeGreaterThan(0);
        json.RootElement.GetProperty("stepOutputs").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public void FromJson_WithValidJson_RestoresContext()
    {
        // Arrange
        var original = new SharedWorkflowContext();
        original.AddStepOutput("step-1", JsonDocument.Parse("{\"test\": true}"));
        original.AddUserPreference("theme", "dark");
        var json = original.ToJson();

        // Act
        var restored = SharedWorkflowContext.FromJson(json);

        // Assert
        restored.Version.Should().Be(original.Version);
        restored.GetStepOutput("step-1").Should().NotBeNull();
        restored.GetUserPreference("theme").Should().Be("dark");
    }
}
