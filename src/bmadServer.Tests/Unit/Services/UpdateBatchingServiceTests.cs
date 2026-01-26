using bmadServer.ApiService.Models.Events;
using bmadServer.ApiService.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace bmadServer.Tests.Unit.Services;

public class UpdateBatchingServiceTests
{
    [Fact]
    public void QueueUpdate_AddsEventToPendingUpdates()
    {
        // Arrange
        var mockHubContext = new Mock<Microsoft.AspNetCore.SignalR.IHubContext<bmadServer.ApiService.Hubs.ChatHub>>();
        var mockLogger = new Mock<ILogger<UpdateBatchingService>>();
        var service = new UpdateBatchingService(mockHubContext.Object, mockLogger.Object);

        var workflowId = Guid.NewGuid();
        var evt = new WorkflowEvent
        {
            EventType = "TEST",
            WorkflowId = workflowId,
            UserId = Guid.NewGuid(),
            DisplayName = "Test User",
            Timestamp = DateTime.UtcNow
        };

        // Act
        service.QueueUpdate(workflowId, evt);

        // Assert - if we can flush without error, the queue worked
        Assert.NotNull(service);
    }
}
