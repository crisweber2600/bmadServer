using System.Text.Json;
using bmadServer.ApiService.Models.Workflows;
using FluentAssertions;
using Xunit;

namespace bmadServer.Tests.Unit.Models;

public class WorkflowEventAttributionTests
{
    [Fact]
    public void WorkflowEvent_Should_Have_DisplayName_Property()
    {
        // Arrange & Act
        var workflowEvent = new WorkflowEvent
        {
            Id = Guid.NewGuid(),
            WorkflowInstanceId = Guid.NewGuid(),
            EventType = "DecisionMade",
            Timestamp = DateTime.UtcNow,
            UserId = Guid.NewGuid(),
            DisplayName = "Marcus Chen"
        };

        // Assert
        workflowEvent.DisplayName.Should().Be("Marcus Chen");
    }

    [Fact]
    public void WorkflowEvent_Should_Have_Payload_Property()
    {
        // Arrange
        var payload = JsonDocument.Parse("{\"decision\":\"Use OAuth 2.0\",\"confidence\":0.95}");
        
        // Act
        var workflowEvent = new WorkflowEvent
        {
            Id = Guid.NewGuid(),
            WorkflowInstanceId = Guid.NewGuid(),
            EventType = "DecisionMade",
            Timestamp = DateTime.UtcNow,
            UserId = Guid.NewGuid(),
            Payload = payload
        };

        // Assert
        workflowEvent.Payload.Should().NotBeNull();
        workflowEvent.Payload!.RootElement.GetProperty("decision").GetString().Should().Be("Use OAuth 2.0");
    }

    [Fact]
    public void WorkflowEvent_Should_Have_InputType_Property()
    {
        // Arrange & Act
        var workflowEvent = new WorkflowEvent
        {
            Id = Guid.NewGuid(),
            WorkflowInstanceId = Guid.NewGuid(),
            EventType = "DecisionMade",
            Timestamp = DateTime.UtcNow,
            UserId = Guid.NewGuid(),
            InputType = "Decision"
        };

        // Assert
        workflowEvent.InputType.Should().Be("Decision");
    }

    [Fact]
    public void WorkflowEvent_Should_Have_AlternativesConsidered_Property()
    {
        // Arrange
        var alternatives = JsonDocument.Parse("[\"Custom JWT\",\"SAML integration\"]");
        
        // Act
        var workflowEvent = new WorkflowEvent
        {
            Id = Guid.NewGuid(),
            WorkflowInstanceId = Guid.NewGuid(),
            EventType = "DecisionMade",
            Timestamp = DateTime.UtcNow,
            UserId = Guid.NewGuid(),
            AlternativesConsidered = alternatives
        };

        // Assert
        workflowEvent.AlternativesConsidered.Should().NotBeNull();
        workflowEvent.AlternativesConsidered!.RootElement.GetArrayLength().Should().Be(2);
    }

    [Fact]
    public void WorkflowEvent_Should_Support_Null_Attribution_Fields()
    {
        // Arrange & Act - Legacy event without attribution
        var legacyEvent = new WorkflowEvent
        {
            Id = Guid.NewGuid(),
            WorkflowInstanceId = Guid.NewGuid(),
            EventType = "StatusChanged",
            Timestamp = DateTime.UtcNow,
            UserId = Guid.NewGuid()
        };

        // Assert - Should not throw and allow null attribution fields
        legacyEvent.DisplayName.Should().BeNull();
        legacyEvent.Payload.Should().BeNull();
        legacyEvent.InputType.Should().BeNull();
        legacyEvent.AlternativesConsidered.Should().BeNull();
    }

    [Fact]
    public void WorkflowEvent_With_Full_Attribution_Should_Be_Valid()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var workflowId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var timestamp = DateTime.UtcNow;
        var payload = JsonDocument.Parse("{\"decision\":\"Use OAuth 2.0\",\"confidence\":0.95,\"rationale\":\"Industry standard\"}");
        var alternatives = JsonDocument.Parse("[\"Custom JWT\",\"SAML integration\"]");

        // Act
        var workflowEvent = new WorkflowEvent
        {
            Id = eventId,
            WorkflowInstanceId = workflowId,
            EventType = "DecisionMade",
            Timestamp = timestamp,
            UserId = userId,
            DisplayName = "Marcus Chen",
            Payload = payload,
            InputType = "Decision",
            AlternativesConsidered = alternatives
        };

        // Assert
        workflowEvent.Id.Should().Be(eventId);
        workflowEvent.WorkflowInstanceId.Should().Be(workflowId);
        workflowEvent.EventType.Should().Be("DecisionMade");
        workflowEvent.Timestamp.Should().Be(timestamp);
        workflowEvent.UserId.Should().Be(userId);
        workflowEvent.DisplayName.Should().Be("Marcus Chen");
        workflowEvent.Payload.Should().NotBeNull();
        workflowEvent.InputType.Should().Be("Decision");
        workflowEvent.AlternativesConsidered.Should().NotBeNull();
    }

    [Fact]
    public void WorkflowEvent_Payload_Should_Support_Complex_Objects()
    {
        // Arrange
        var complexPayload = JsonDocument.Parse(@"{
            ""decision"": ""Use OAuth 2.0"",
            ""confidence"": 0.95,
            ""rationale"": ""Industry standard, secure, widely supported"",
            ""impactAnalysis"": {
                ""security"": ""High"",
                ""complexity"": ""Medium"",
                ""timeline"": ""2 weeks""
            }
        }");

        // Act
        var workflowEvent = new WorkflowEvent
        {
            Id = Guid.NewGuid(),
            WorkflowInstanceId = Guid.NewGuid(),
            EventType = "DecisionMade",
            Timestamp = DateTime.UtcNow,
            UserId = Guid.NewGuid(),
            Payload = complexPayload
        };

        // Assert
        workflowEvent.Payload.Should().NotBeNull();
        workflowEvent.Payload!.RootElement.GetProperty("decision").GetString().Should().Be("Use OAuth 2.0");
        workflowEvent.Payload.RootElement.GetProperty("impactAnalysis").GetProperty("security").GetString().Should().Be("High");
    }
}
