using bmadServer.ApiService.Models;
using FluentAssertions;
using Xunit;

namespace bmadServer.Tests.Unit.Models;

public class ChatMessageAttributionTests
{
    [Fact]
    public void ChatMessage_Should_Have_UserId_Property()
    {
        // Arrange & Act
        var message = new ChatMessage
        {
            Id = "msg-1",
            Role = "user",
            Content = "Test message",
            Timestamp = DateTime.UtcNow,
            UserId = Guid.NewGuid()
        };

        // Assert
        message.UserId.Should().NotBeNull();
        message.UserId.Value.Should().NotBeEmpty();
    }

    [Fact]
    public void ChatMessage_Should_Have_DisplayName_Property()
    {
        // Arrange & Act
        var message = new ChatMessage
        {
            Id = "msg-1",
            Role = "user",
            Content = "Test message",
            Timestamp = DateTime.UtcNow,
            DisplayName = "Sarah Johnson"
        };

        // Assert
        message.DisplayName.Should().Be("Sarah Johnson");
        message.DisplayName.Should().BeOfType<string>();
    }

    [Fact]
    public void ChatMessage_Should_Have_AvatarUrl_Property()
    {
        // Arrange & Act
        var message = new ChatMessage
        {
            Id = "msg-1",
            Role = "user",
            Content = "Test message",
            Timestamp = DateTime.UtcNow,
            AvatarUrl = "https://example.com/avatar.jpg"
        };

        // Assert
        message.AvatarUrl.Should().Be("https://example.com/avatar.jpg");
    }

    [Fact]
    public void ChatMessage_Should_Have_InputType_Property()
    {
        // Arrange & Act
        var message = new ChatMessage
        {
            Id = "msg-1",
            Role = "user",
            Content = "Test message",
            Timestamp = DateTime.UtcNow,
            InputType = "Message"
        };

        // Assert
        message.InputType.Should().Be("Message");
    }

    [Fact]
    public void ChatMessage_Should_Have_WorkflowStep_Property()
    {
        // Arrange & Act
        var message = new ChatMessage
        {
            Id = "msg-1",
            Role = "user",
            Content = "Test message",
            Timestamp = DateTime.UtcNow,
            WorkflowStep = "requirements-gathering"
        };

        // Assert
        message.WorkflowStep.Should().Be("requirements-gathering");
    }

    [Fact]
    public void ChatMessage_Should_Support_Null_UserId_For_AgentMessages()
    {
        // Arrange & Act
        var agentMessage = new ChatMessage
        {
            Id = "msg-1",
            Role = "agent",
            Content = "Agent response",
            Timestamp = DateTime.UtcNow,
            AgentId = "agent-1",
            UserId = null
        };

        // Assert
        agentMessage.UserId.Should().BeNull();
        agentMessage.AgentId.Should().Be("agent-1");
    }

    [Fact]
    public void ChatMessage_Should_Support_Backward_Compatibility()
    {
        // Arrange & Act - Create message without attribution (legacy)
        var legacyMessage = new ChatMessage
        {
            Id = "msg-1",
            Role = "user",
            Content = "Legacy message",
            Timestamp = DateTime.UtcNow
        };

        // Assert - Should not throw and allow null attribution fields
        legacyMessage.UserId.Should().BeNull();
        legacyMessage.DisplayName.Should().BeNull();
        legacyMessage.AvatarUrl.Should().BeNull();
    }

    [Fact]
    public void ChatMessage_Should_Have_Default_InputType()
    {
        // Arrange & Act
        var message = new ChatMessage
        {
            Id = "msg-1",
            Role = "user",
            Content = "Test message",
            Timestamp = DateTime.UtcNow
        };

        // Assert - InputType should default to "Message"
        message.InputType.Should().Be("Message");
    }

    [Theory]
    [InlineData("Message")]
    [InlineData("Decision")]
    [InlineData("StepAdvance")]
    [InlineData("Checkpoint")]
    public void ChatMessage_Should_Support_All_InputTypes(string inputType)
    {
        // Arrange & Act
        var message = new ChatMessage
        {
            Id = "msg-1",
            Role = "user",
            Content = "Test",
            Timestamp = DateTime.UtcNow,
            InputType = inputType
        };

        // Assert
        message.InputType.Should().Be(inputType);
    }

    [Fact]
    public void ChatMessage_With_Full_Attribution_Should_Be_Valid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var timestamp = DateTime.UtcNow;

        // Act
        var message = new ChatMessage
        {
            Id = "msg-1",
            Role = "user",
            Content = "Test message with full attribution",
            Timestamp = timestamp,
            UserId = userId,
            DisplayName = "Sarah Johnson",
            AvatarUrl = "https://example.com/avatar.jpg",
            InputType = "Message",
            WorkflowStep = "requirements-gathering"
        };

        // Assert
        message.Id.Should().Be("msg-1");
        message.Role.Should().Be("user");
        message.Content.Should().Be("Test message with full attribution");
        message.Timestamp.Should().Be(timestamp);
        message.UserId.Should().Be(userId);
        message.DisplayName.Should().Be("Sarah Johnson");
        message.AvatarUrl.Should().Be("https://example.com/avatar.jpg");
        message.InputType.Should().Be("Message");
        message.WorkflowStep.Should().Be("requirements-gathering");
    }
}
