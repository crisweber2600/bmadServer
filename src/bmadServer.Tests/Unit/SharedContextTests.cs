using bmadServer.ApiService.WorkflowContext;
using Xunit;

namespace bmadServer.Tests.Unit;

public class SharedContextTests
{
    [Fact]
    public void SharedContext_ShouldInitializeWithRequiredProperties()
    {
        // Arrange & Act
        var workflowId = Guid.NewGuid();
        var context = new SharedContext
        {
            WorkflowInstanceId = workflowId
        };

        // Assert
        Assert.Equal(workflowId, context.WorkflowInstanceId);
        Assert.Empty(context.StepOutputs);
        Assert.Empty(context.DecisionHistory);
        Assert.Empty(context.ArtifactReferences);
        Assert.Equal(1, context.Version);
        Assert.Null(context.UserPreferences);
    }

    [Fact]
    public void GetStepOutput_ShouldReturnOutput_WhenStepCompleted()
    {
        // Arrange
        var context = new SharedContext
        {
            WorkflowInstanceId = Guid.NewGuid()
        };

        var output = new StepOutput
        {
            StepId = "test-step",
            Data = new { Result = "Success" },
            CompletedAt = DateTime.UtcNow,
            CompletedByAgent = "test-agent"
        };

        context.AddStepOutput("test-step", output);

        // Act
        var result = context.GetStepOutput("test-step");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test-step", result.StepId);
        Assert.Equal("test-agent", result.CompletedByAgent);
    }

    [Fact]
    public void GetStepOutput_ShouldReturnNull_WhenStepNotCompleted()
    {
        // Arrange
        var context = new SharedContext
        {
            WorkflowInstanceId = Guid.NewGuid()
        };

        // Act
        var result = context.GetStepOutput("non-existent-step");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void AddStepOutput_ShouldIncrementVersion()
    {
        // Arrange
        var context = new SharedContext
        {
            WorkflowInstanceId = Guid.NewGuid()
        };
        var initialVersion = context.Version;

        var output = new StepOutput
        {
            StepId = "test-step",
            Data = new { },
            CompletedAt = DateTime.UtcNow,
            CompletedByAgent = "test-agent"
        };

        // Act
        context.AddStepOutput("test-step", output);

        // Assert
        Assert.Equal(initialVersion + 1, context.Version);
    }

    [Fact]
    public void AddStepOutput_ShouldUpdateLastModifiedAt()
    {
        // Arrange
        var context = new SharedContext
        {
            WorkflowInstanceId = Guid.NewGuid()
        };
        var initialTime = context.LastModifiedAt;

        Thread.Sleep(10); // Ensure time difference

        var output = new StepOutput
        {
            StepId = "test-step",
            Data = new { },
            CompletedAt = DateTime.UtcNow,
            CompletedByAgent = "test-agent"
        };

        // Act
        context.AddStepOutput("test-step", output);

        // Assert
        Assert.True(context.LastModifiedAt > initialTime);
    }

    [Fact]
    public void AddDecision_ShouldAddToHistory()
    {
        // Arrange
        var context = new SharedContext
        {
            WorkflowInstanceId = Guid.NewGuid()
        };

        var decision = new Decision
        {
            DecisionType = "test-decision",
            MadeBy = "test-agent",
            Timestamp = DateTime.UtcNow,
            Rationale = "Test rationale",
            DecisionValue = "test-value"
        };

        // Act
        context.AddDecision(decision);

        // Assert
        Assert.Single(context.DecisionHistory);
        Assert.Equal("test-decision", context.DecisionHistory[0].DecisionType);
    }

    [Fact]
    public void AddDecision_ShouldIncrementVersion()
    {
        // Arrange
        var context = new SharedContext
        {
            WorkflowInstanceId = Guid.NewGuid()
        };
        var initialVersion = context.Version;

        var decision = new Decision
        {
            DecisionType = "test-decision",
            MadeBy = "test-agent",
            Timestamp = DateTime.UtcNow,
            Rationale = "Test rationale",
            DecisionValue = "test-value"
        };

        // Act
        context.AddDecision(decision);

        // Assert
        Assert.Equal(initialVersion + 1, context.Version);
    }

    [Fact]
    public void AddArtifactReference_ShouldAddToReferences()
    {
        // Arrange
        var context = new SharedContext
        {
            WorkflowInstanceId = Guid.NewGuid()
        };

        var artifact = new ArtifactReference
        {
            ArtifactType = "diagram",
            StorageLocation = "/path/to/artifact",
            CreatedAt = DateTime.UtcNow,
            CreatedByStep = "test-step"
        };

        // Act
        context.AddArtifactReference(artifact);

        // Assert
        Assert.Single(context.ArtifactReferences);
        Assert.Equal("diagram", context.ArtifactReferences[0].ArtifactType);
    }

    [Fact]
    public void AddArtifactReference_ShouldIncrementVersion()
    {
        // Arrange
        var context = new SharedContext
        {
            WorkflowInstanceId = Guid.NewGuid()
        };
        var initialVersion = context.Version;

        var artifact = new ArtifactReference
        {
            ArtifactType = "diagram",
            StorageLocation = "/path/to/artifact",
            CreatedAt = DateTime.UtcNow,
            CreatedByStep = "test-step"
        };

        // Act
        context.AddArtifactReference(artifact);

        // Assert
        Assert.Equal(initialVersion + 1, context.Version);
    }

    [Fact]
    public void ContextSummarization_ShouldTrigger_WhenExceedingTokenLimit()
    {
        // Arrange
        var context = new SharedContext
        {
            WorkflowInstanceId = Guid.NewGuid()
        };

        // Act - Add many large outputs to exceed token limit
        for (int i = 0; i < 100; i++)
        {
            var largeData = new string('x', 1000);
            context.AddStepOutput($"step-{i}", new StepOutput
            {
                StepId = $"step-{i}",
                Data = new { Content = largeData },
                CompletedAt = DateTime.UtcNow.AddMinutes(-i),
                CompletedByAgent = "test-agent",
                Summary = $"Summary {i}"
            });
        }

        // Assert
        Assert.NotNull(context.ContextSummary);
        Assert.NotEmpty(context.ContextSummary);
        Assert.True(context.EstimatedTokenCount > SharedContext.MaxTokenCount);
    }

    [Fact]
    public void ContextSummarization_ShouldPreserveKeyDecisions()
    {
        // Arrange
        var context = new SharedContext
        {
            WorkflowInstanceId = Guid.NewGuid()
        };

        context.AddDecision(new Decision
        {
            DecisionType = "important-decision",
            MadeBy = "architect",
            Timestamp = DateTime.UtcNow,
            Rationale = "Critical choice",
            DecisionValue = "value"
        });

        // Act - Trigger summarization
        for (int i = 0; i < 100; i++)
        {
            var largeData = new string('x', 1000);
            context.AddStepOutput($"step-{i}", new StepOutput
            {
                StepId = $"step-{i}",
                Data = new { Content = largeData },
                CompletedAt = DateTime.UtcNow.AddMinutes(-i),
                CompletedByAgent = "test-agent",
                Summary = $"Summary {i}"
            });
        }

        // Assert
        Assert.NotNull(context.ContextSummary);
        Assert.Contains("Key Decisions:", context.ContextSummary);
        Assert.Contains("important-decision", context.ContextSummary);
    }

    [Fact]
    public void StepOutput_ShouldHaveRequiredProperties()
    {
        // Arrange & Act
        var output = new StepOutput
        {
            StepId = "test-step",
            Data = new { Result = "Success" },
            CompletedAt = DateTime.UtcNow,
            CompletedByAgent = "test-agent",
            Summary = "Test summary"
        };

        // Assert
        Assert.Equal("test-step", output.StepId);
        Assert.NotNull(output.Data);
        Assert.NotEqual(default(DateTime), output.CompletedAt);
        Assert.Equal("test-agent", output.CompletedByAgent);
        Assert.Equal("Test summary", output.Summary);
    }

    [Fact]
    public void Decision_ShouldHaveUniqueId()
    {
        // Arrange & Act
        var decision1 = new Decision
        {
            DecisionType = "test",
            MadeBy = "agent",
            Timestamp = DateTime.UtcNow,
            Rationale = "reason",
            DecisionValue = "value"
        };

        var decision2 = new Decision
        {
            DecisionType = "test",
            MadeBy = "agent",
            Timestamp = DateTime.UtcNow,
            Rationale = "reason",
            DecisionValue = "value"
        };

        // Assert
        Assert.NotEqual(decision1.Id, decision2.Id);
    }

    [Fact]
    public void UserPreferences_ShouldSupportDisplaySettings()
    {
        // Arrange & Act
        var preferences = new UserPreferences
        {
            DisplaySettings = new Dictionary<string, object>
            {
                { "theme", "dark" },
                { "verbosity", "detailed" }
            }
        };

        // Assert
        Assert.Equal(2, preferences.DisplaySettings.Count);
        Assert.Equal("dark", preferences.DisplaySettings["theme"]);
    }

    [Fact]
    public void UserPreferences_ShouldSupportModelPreferences()
    {
        // Arrange & Act
        var preferences = new UserPreferences
        {
            ModelPreferences = new Dictionary<string, string>
            {
                { "architect", "gpt-4" },
                { "developer", "claude-3" }
            }
        };

        // Assert
        Assert.Equal(2, preferences.ModelPreferences.Count);
        Assert.Equal("gpt-4", preferences.ModelPreferences["architect"]);
    }

    [Fact]
    public void ArtifactReference_ShouldHaveUniqueId()
    {
        // Arrange & Act
        var artifact1 = new ArtifactReference
        {
            ArtifactType = "diagram",
            StorageLocation = "/path1",
            CreatedAt = DateTime.UtcNow,
            CreatedByStep = "step-1"
        };

        var artifact2 = new ArtifactReference
        {
            ArtifactType = "diagram",
            StorageLocation = "/path2",
            CreatedAt = DateTime.UtcNow,
            CreatedByStep = "step-2"
        };

        // Assert
        Assert.NotEqual(artifact1.Id, artifact2.Id);
    }

    [Fact]
    public void SharedContext_ShouldTrackMultipleVersionChanges()
    {
        // Arrange
        var context = new SharedContext
        {
            WorkflowInstanceId = Guid.NewGuid()
        };
        var initialVersion = context.Version;

        // Act
        context.AddStepOutput("step-1", new StepOutput
        {
            StepId = "step-1",
            Data = new { },
            CompletedAt = DateTime.UtcNow,
            CompletedByAgent = "agent-1"
        });

        context.AddDecision(new Decision
        {
            DecisionType = "decision-1",
            MadeBy = "agent-1",
            Timestamp = DateTime.UtcNow,
            Rationale = "reason",
            DecisionValue = "value"
        });

        context.AddArtifactReference(new ArtifactReference
        {
            ArtifactType = "artifact",
            StorageLocation = "/path",
            CreatedAt = DateTime.UtcNow,
            CreatedByStep = "step-1"
        });

        // Assert
        Assert.Equal(initialVersion + 3, context.Version);
    }

    [Fact]
    public void SharedContext_ShouldAllowReplacingStepOutput()
    {
        // Arrange
        var context = new SharedContext
        {
            WorkflowInstanceId = Guid.NewGuid()
        };

        var output1 = new StepOutput
        {
            StepId = "step-1",
            Data = new { Value = "First" },
            CompletedAt = DateTime.UtcNow,
            CompletedByAgent = "agent-1"
        };

        var output2 = new StepOutput
        {
            StepId = "step-1",
            Data = new { Value = "Second" },
            CompletedAt = DateTime.UtcNow,
            CompletedByAgent = "agent-1"
        };

        // Act
        context.AddStepOutput("step-1", output1);
        context.AddStepOutput("step-1", output2);

        // Assert
        Assert.Single(context.StepOutputs);
        var result = context.GetStepOutput("step-1");
        Assert.NotNull(result);
        Assert.Equal(output2.Data, result.Data);
    }
}
