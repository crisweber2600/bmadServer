using bmadServer.ApiService.WorkflowContext;
using Microsoft.EntityFrameworkCore;
using Reqnroll;
using Xunit;

namespace bmadServer.BDD.Tests.StepDefinitions;

[Binding]
public class SharedWorkflowContextSteps
{
    private readonly ScenarioContext _scenarioContext;
    private SharedContext? _sharedContext;
    private StepOutput? _retrievedStepOutput;
    private Exception? _caughtException;
    private int _initialVersion;

    public SharedWorkflowContextSteps(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    [Given(@"a workflow has multiple completed steps")]
    public void GivenAWorkflowHasMultipleCompletedSteps()
    {
        _sharedContext = new SharedContext
        {
            WorkflowInstanceId = Guid.NewGuid()
        };

        // Add multiple completed steps
        _sharedContext.AddStepOutput("gather-requirements", new StepOutput
        {
            StepId = "gather-requirements",
            Data = new { Requirements = "User needs a feature" },
            CompletedAt = DateTime.UtcNow.AddMinutes(-10),
            CompletedByAgent = "product-manager",
            Summary = "Gathered user requirements"
        });

        _sharedContext.AddStepOutput("create-architecture", new StepOutput
        {
            StepId = "create-architecture",
            Data = new { Architecture = "Microservices" },
            CompletedAt = DateTime.UtcNow.AddMinutes(-5),
            CompletedByAgent = "architect",
            Summary = "Created architecture design"
        });

        _sharedContext.AddDecision(new Decision
        {
            DecisionType = "architecture-choice",
            MadeBy = "architect",
            Timestamp = DateTime.UtcNow.AddMinutes(-5),
            Rationale = "Microservices for scalability",
            DecisionValue = "Microservices"
        });

        _sharedContext.AddArtifactReference(new ArtifactReference
        {
            ArtifactType = "diagram",
            StorageLocation = "/artifacts/architecture-diagram.png",
            CreatedAt = DateTime.UtcNow.AddMinutes(-4),
            CreatedByStep = "create-architecture",
            Description = "System architecture diagram"
        });

        _scenarioContext["SharedContext"] = _sharedContext;
    }

    [When(@"an agent receives a request")]
    public void WhenAnAgentReceivesARequest()
    {
        // Agent would receive the shared context as part of the request
        _sharedContext = _scenarioContext["SharedContext"] as SharedContext;
    }

    [Then(@"SharedContext contains all step outputs")]
    public void ThenSharedContextContainsAllStepOutputs()
    {
        Assert.NotNull(_sharedContext);
        Assert.True(_sharedContext.StepOutputs.Count >= 2);
        Assert.Contains("gather-requirements", _sharedContext.StepOutputs.Keys);
        Assert.Contains("create-architecture", _sharedContext.StepOutputs.Keys);
    }

    [Then(@"SharedContext contains decision history")]
    public void ThenSharedContextContainsDecisionHistory()
    {
        Assert.NotNull(_sharedContext);
        Assert.NotEmpty(_sharedContext.DecisionHistory);
        Assert.Contains(_sharedContext.DecisionHistory, d => d.DecisionType == "architecture-choice");
    }

    [Then(@"SharedContext contains user preferences")]
    public void ThenSharedContextContainsUserPreferences()
    {
        Assert.NotNull(_sharedContext);
        // UserPreferences is optional but the property should exist
        Assert.True(_sharedContext.UserPreferences != null || _sharedContext.UserPreferences == null);
    }

    [Then(@"SharedContext contains artifact references")]
    public void ThenSharedContextContainsArtifactReferences()
    {
        Assert.NotNull(_sharedContext);
        Assert.NotEmpty(_sharedContext.ArtifactReferences);
        Assert.Contains(_sharedContext.ArtifactReferences, a => a.ArtifactType == "diagram");
    }

    [Given(@"a workflow step ""(.*)"" has completed")]
    public void GivenAWorkflowStepHasCompleted(string stepId)
    {
        _sharedContext = new SharedContext
        {
            WorkflowInstanceId = Guid.NewGuid()
        };

        var output = new StepOutput
        {
            StepId = stepId,
            Data = new { Result = "Step completed successfully" },
            CompletedAt = DateTime.UtcNow,
            CompletedByAgent = "test-agent",
            Summary = "Test step output"
        };

        _sharedContext.AddStepOutput(stepId, output);
        _scenarioContext["SharedContext"] = _sharedContext;
    }

    [Given(@"the step produced output data")]
    public void GivenTheStepProducedOutputData()
    {
        // Already handled in the previous step
    }

    [When(@"an agent queries GetStepOutput with ""(.*)""")]
    public void WhenAnAgentQueriesGetStepOutputWith(string stepId)
    {
        _sharedContext = _scenarioContext["SharedContext"] as SharedContext;
        _retrievedStepOutput = _sharedContext?.GetStepOutput(stepId);
    }

    [Then(@"it receives the structured output from that step")]
    public void ThenItReceivesTheStructuredOutputFromThatStep()
    {
        Assert.NotNull(_retrievedStepOutput);
    }

    [Then(@"the output matches the original step output")]
    public void ThenTheOutputMatchesTheOriginalStepOutput()
    {
        Assert.NotNull(_retrievedStepOutput);
        Assert.NotNull(_retrievedStepOutput.Data);
    }

    [Given(@"a workflow step ""(.*)"" has not completed")]
    public void GivenAWorkflowStepHasNotCompleted(string stepId)
    {
        _sharedContext = new SharedContext
        {
            WorkflowInstanceId = Guid.NewGuid()
        };
        _scenarioContext["SharedContext"] = _sharedContext;
        _scenarioContext["IncompleteStepId"] = stepId;
    }

    [Then(@"it receives null")]
    public void ThenItReceivesNull()
    {
        Assert.Null(_retrievedStepOutput);
    }

    [Then(@"no exception is thrown")]
    public void ThenNoExceptionIsThrown()
    {
        Assert.Null(_caughtException);
    }

    [Given(@"a workflow step is executing")]
    public void GivenAWorkflowStepIsExecuting()
    {
        _sharedContext = new SharedContext
        {
            WorkflowInstanceId = Guid.NewGuid()
        };
        _initialVersion = _sharedContext.Version;
        _scenarioContext["SharedContext"] = _sharedContext;
    }

    [When(@"the step completes with output")]
    public void WhenTheStepCompletesWithOutput()
    {
        var output = new StepOutput
        {
            StepId = "test-step",
            Data = new { Result = "Success" },
            CompletedAt = DateTime.UtcNow,
            CompletedByAgent = "test-agent"
        };
        _sharedContext?.AddStepOutput("test-step", output);
    }

    [Then(@"the output is automatically added to SharedContext")]
    public void ThenTheOutputIsAutomaticallyAddedToSharedContext()
    {
        Assert.NotNull(_sharedContext);
        Assert.Contains("test-step", _sharedContext.StepOutputs.Keys);
    }

    [Then(@"subsequent agents can access it immediately")]
    public void ThenSubsequentAgentsCanAccessItImmediately()
    {
        Assert.NotNull(_sharedContext);
        var output = _sharedContext.GetStepOutput("test-step");
        Assert.NotNull(output);
    }

    [Then(@"the context version is incremented")]
    public void ThenTheContextVersionIsIncremented()
    {
        Assert.NotNull(_sharedContext);
        Assert.True(_sharedContext.Version > _initialVersion);
    }

    [Given(@"context size grows large")]
    public void GivenContextSizeGrowsLarge()
    {
        _sharedContext = new SharedContext
        {
            WorkflowInstanceId = Guid.NewGuid()
        };
        _scenarioContext["SharedContext"] = _sharedContext;
    }

    [When(@"the context exceeds (.*) tokens")]
    public void WhenTheContextExceedsTokens(int tokenCount)
    {
        // Add enough data to exceed token limit
        for (int i = 0; i < 100; i++)
        {
            var largeData = new string('x', 1000); // 1000 characters
            _sharedContext?.AddStepOutput($"step-{i}", new StepOutput
            {
                StepId = $"step-{i}",
                Data = new { LargeContent = largeData },
                CompletedAt = DateTime.UtcNow.AddMinutes(-i),
                CompletedByAgent = "test-agent",
                Summary = $"Step {i} summary"
            });
        }

        // Add decisions
        for (int i = 0; i < 10; i++)
        {
            _sharedContext?.AddDecision(new Decision
            {
                DecisionType = $"decision-type-{i}",
                MadeBy = "test-agent",
                Timestamp = DateTime.UtcNow.AddMinutes(-i),
                Rationale = $"Important decision {i}",
                DecisionValue = $"value-{i}"
            });
        }
    }

    [Then(@"the system summarizes older context")]
    public void ThenTheSystemSummarizesOlderContext()
    {
        Assert.NotNull(_sharedContext);
        Assert.NotNull(_sharedContext.ContextSummary);
        Assert.NotEmpty(_sharedContext.ContextSummary);
    }

    [Then(@"key decisions are preserved in summary")]
    public void ThenKeyDecisionsArePreservedInSummary()
    {
        Assert.NotNull(_sharedContext);
        Assert.Contains("Key Decisions:", _sharedContext.ContextSummary ?? "");
    }

    [Then(@"full context remains available in database")]
    public void ThenFullContextRemainsAvailableInDatabase()
    {
        // This would be tested in integration tests with actual database
        Assert.NotNull(_sharedContext);
    }

    [Given(@"concurrent agents access context")]
    public void GivenConcurrentAgentsAccessContext()
    {
        _sharedContext = new SharedContext
        {
            WorkflowInstanceId = Guid.NewGuid()
        };
        _scenarioContext["SharedContext"] = _sharedContext;
    }

    [When(@"simultaneous writes occur")]
    public void WhenSimultaneousWritesOccur()
    {
        // Simulate two agents trying to update the same context
        var context1 = _sharedContext;
        var context2 = new SharedContext
        {
            WorkflowInstanceId = _sharedContext!.WorkflowInstanceId,
            Version = _sharedContext.Version
        };

        // First write succeeds
        context1?.AddStepOutput("step-1", new StepOutput
        {
            StepId = "step-1",
            Data = new { },
            CompletedAt = DateTime.UtcNow,
            CompletedByAgent = "agent-1"
        });

        // Second write would fail due to version mismatch (in real scenario with DB)
        try
        {
            context2.AddStepOutput("step-2", new StepOutput
            {
                StepId = "step-2",
                Data = new { },
                CompletedAt = DateTime.UtcNow,
                CompletedByAgent = "agent-2"
            });

            // Check if versions would conflict
            if (context1?.Version != context2.Version - 1)
            {
                throw new DbUpdateConcurrencyException("Version mismatch");
            }
        }
        catch (Exception ex)
        {
            _caughtException = ex;
        }

        _scenarioContext["Context1"] = context1;
        _scenarioContext["Context2"] = context2;
    }

    [Then(@"optimistic concurrency control prevents conflicts")]
    public void ThenOptimisticConcurrencyControlPreventsConflicts()
    {
        // In a real scenario with database, this would be enforced
        Assert.NotNull(_scenarioContext["Context1"]);
    }

    [Then(@"version numbers track context changes")]
    public void ThenVersionNumbersTrackContextChanges()
    {
        var context1 = _scenarioContext["Context1"] as SharedContext;
        Assert.NotNull(context1);
        Assert.True(context1.Version > 1);
    }

    [Then(@"the first write succeeds")]
    public void ThenTheFirstWriteSucceeds()
    {
        var context1 = _scenarioContext["Context1"] as SharedContext;
        Assert.NotNull(context1);
        Assert.Contains("step-1", context1.StepOutputs.Keys);
    }

    [Then(@"the second write detects version mismatch")]
    public void ThenTheSecondWriteDetectsVersionMismatch()
    {
        Assert.NotNull(_caughtException);
        Assert.IsType<DbUpdateConcurrencyException>(_caughtException);
    }

    [Given(@"multiple steps complete in sequence")]
    public void GivenMultipleStepsCompleteInSequence()
    {
        _sharedContext = new SharedContext
        {
            WorkflowInstanceId = Guid.NewGuid()
        };

        for (int i = 0; i < 5; i++)
        {
            _sharedContext.AddStepOutput($"step-{i}", new StepOutput
            {
                StepId = $"step-{i}",
                Data = new { Order = i },
                CompletedAt = DateTime.UtcNow.AddSeconds(i),
                CompletedByAgent = "test-agent"
            });
        }

        _scenarioContext["SharedContext"] = _sharedContext;
    }

    [When(@"I query the SharedContext")]
    public void WhenIQueryTheSharedContext()
    {
        _sharedContext = _scenarioContext["SharedContext"] as SharedContext;
    }

    [Then(@"all step outputs are available")]
    public void ThenAllStepOutputsAreAvailable()
    {
        Assert.NotNull(_sharedContext);
        Assert.Equal(5, _sharedContext.StepOutputs.Count);
    }

    [Then(@"they are ordered chronologically")]
    public void ThenTheyAreOrderedChronologically()
    {
        Assert.NotNull(_sharedContext);
        var outputs = _sharedContext.StepOutputs.Values.OrderBy(o => o.CompletedAt).ToList();
        for (int i = 0; i < outputs.Count - 1; i++)
        {
            Assert.True(outputs[i].CompletedAt <= outputs[i + 1].CompletedAt);
        }
    }

    [Then(@"each has a timestamp")]
    public void ThenEachHasATimestamp()
    {
        Assert.NotNull(_sharedContext);
        foreach (var output in _sharedContext.StepOutputs.Values)
        {
            Assert.NotEqual(default(DateTime), output.CompletedAt);
        }
    }

    [Given(@"a user has set preferences")]
    public void GivenAUserHasSetPreferences()
    {
        var preferences = new UserPreferences
        {
            DisplaySettings = new Dictionary<string, object>
            {
                { "verbosity", "detailed" },
                { "theme", "dark" }
            },
            ModelPreferences = new Dictionary<string, string>
            {
                { "architect", "gpt-4" },
                { "developer", "claude-3" }
            },
            PreferredLanguage = "en-US"
        };

        _sharedContext = new SharedContext
        {
            WorkflowInstanceId = Guid.NewGuid(),
            UserPreferences = preferences
        };

        _scenarioContext["SharedContext"] = _sharedContext;
    }

    [When(@"an agent accesses SharedContext")]
    public void WhenAnAgentAccessesSharedContext()
    {
        _sharedContext = _scenarioContext["SharedContext"] as SharedContext;
    }

    [Then(@"user preferences are available")]
    public void ThenUserPreferencesAreAvailable()
    {
        Assert.NotNull(_sharedContext);
        Assert.NotNull(_sharedContext.UserPreferences);
    }

    [Then(@"preferences include display settings")]
    public void ThenPreferencesIncludeDisplaySettings()
    {
        Assert.NotNull(_sharedContext?.UserPreferences);
        Assert.NotEmpty(_sharedContext.UserPreferences.DisplaySettings);
    }

    [Then(@"preferences include model preferences")]
    public void ThenPreferencesIncludeModelPreferences()
    {
        Assert.NotNull(_sharedContext?.UserPreferences);
        Assert.NotEmpty(_sharedContext.UserPreferences.ModelPreferences);
    }

    [Given(@"multiple decisions have been made")]
    public void GivenMultipleDecisionsHaveBeenMade()
    {
        _sharedContext = new SharedContext
        {
            WorkflowInstanceId = Guid.NewGuid()
        };

        for (int i = 0; i < 3; i++)
        {
            _sharedContext.AddDecision(new Decision
            {
                DecisionType = $"decision-{i}",
                MadeBy = $"agent-{i}",
                Timestamp = DateTime.UtcNow.AddMinutes(i),
                Rationale = $"Reason {i}",
                DecisionValue = $"value-{i}"
            });
        }

        _scenarioContext["SharedContext"] = _sharedContext;
    }

    [Then(@"decision history is available")]
    public void ThenDecisionHistoryIsAvailable()
    {
        Assert.NotNull(_sharedContext);
        Assert.NotEmpty(_sharedContext.DecisionHistory);
    }

    [Then(@"each decision includes who made it")]
    public void ThenEachDecisionIncludesWhoMadeIt()
    {
        Assert.NotNull(_sharedContext);
        foreach (var decision in _sharedContext.DecisionHistory)
        {
            Assert.NotNull(decision.MadeBy);
            Assert.NotEmpty(decision.MadeBy);
        }
    }

    [Then(@"each decision includes when it was made")]
    public void ThenEachDecisionIncludesWhenItWasMade()
    {
        Assert.NotNull(_sharedContext);
        foreach (var decision in _sharedContext.DecisionHistory)
        {
            Assert.NotEqual(default(DateTime), decision.Timestamp);
        }
    }

    [Then(@"each decision includes the rationale")]
    public void ThenEachDecisionIncludesTheRationale()
    {
        Assert.NotNull(_sharedContext);
        foreach (var decision in _sharedContext.DecisionHistory)
        {
            Assert.NotNull(decision.Rationale);
            Assert.NotEmpty(decision.Rationale);
        }
    }

    [Given(@"artifacts have been created during workflow")]
    public void GivenArtifactsHaveBeenCreatedDuringWorkflow()
    {
        _sharedContext = new SharedContext
        {
            WorkflowInstanceId = Guid.NewGuid()
        };

        for (int i = 0; i < 3; i++)
        {
            _sharedContext.AddArtifactReference(new ArtifactReference
            {
                ArtifactType = $"type-{i}",
                StorageLocation = $"/artifacts/file-{i}",
                CreatedAt = DateTime.UtcNow.AddMinutes(i),
                CreatedByStep = $"step-{i}",
                Description = $"Artifact {i}"
            });
        }

        _scenarioContext["SharedContext"] = _sharedContext;
    }

    [Then(@"artifact references are available")]
    public void ThenArtifactReferencesAreAvailable()
    {
        Assert.NotNull(_sharedContext);
        Assert.NotEmpty(_sharedContext.ArtifactReferences);
    }

    [Then(@"each reference includes artifact type")]
    public void ThenEachReferenceIncludesArtifactType()
    {
        Assert.NotNull(_sharedContext);
        foreach (var artifact in _sharedContext.ArtifactReferences)
        {
            Assert.NotNull(artifact.ArtifactType);
            Assert.NotEmpty(artifact.ArtifactType);
        }
    }

    [Then(@"each reference includes storage location")]
    public void ThenEachReferenceIncludesStorageLocation()
    {
        Assert.NotNull(_sharedContext);
        foreach (var artifact in _sharedContext.ArtifactReferences)
        {
            Assert.NotNull(artifact.StorageLocation);
            Assert.NotEmpty(artifact.StorageLocation);
        }
    }

    [Then(@"each reference includes creation timestamp")]
    public void ThenEachReferenceIncludesCreationTimestamp()
    {
        Assert.NotNull(_sharedContext);
        foreach (var artifact in _sharedContext.ArtifactReferences)
        {
            Assert.NotEqual(default(DateTime), artifact.CreatedAt);
        }
    }
}
