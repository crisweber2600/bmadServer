using bmadServer.ApiService.Models.Workflows;
using Xunit;

namespace bmadServer.Tests.Unit;

public class WorkflowParticipantTests
{
    [Fact]
    public void WorkflowParticipant_ShouldHaveRequiredProperties()
    {
        // Arrange & Act
        var participant = new WorkflowParticipant
        {
            Id = Guid.NewGuid(),
            WorkflowId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Role = ParticipantRole.Contributor,
            AddedBy = Guid.NewGuid()
        };

        // Assert
        Assert.NotEqual(Guid.Empty, participant.Id);
        Assert.NotEqual(Guid.Empty, participant.WorkflowId);
        Assert.NotEqual(Guid.Empty, participant.UserId);
        Assert.Equal(ParticipantRole.Contributor, participant.Role);
        Assert.True(participant.AddedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void ParticipantRole_ShouldHaveValidValues()
    {
        // Assert
        Assert.Equal(0, (int)ParticipantRole.Owner);
        Assert.Equal(1, (int)ParticipantRole.Contributor);
        Assert.Equal(2, (int)ParticipantRole.Observer);
    }
}
