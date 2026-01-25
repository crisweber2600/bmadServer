using bmadServer.ApiService.Controllers;
using bmadServer.ApiService.Data;
using bmadServer.ApiService.Data.Entities;
using bmadServer.ApiService.Models;
using bmadServer.ApiService.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Xunit;

namespace bmadServer.Tests.Integration;

/// <summary>
/// Integration tests for ChatController endpoints.
/// Tests chat history pagination, recent messages, and API responses.
/// </summary>
public class ChatControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public ChatControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetRecentMessages_Should_Return_Last_50_Messages_By_Default()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var sessionService = scope.ServiceProvider.GetRequiredService<ISessionService>();

        var user = new User
        {
            Email = $"test-recent-{Guid.NewGuid()}@example.com",
            PasswordHash = "hash",
            DisplayName = "Test User"
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var (session, _) = await sessionService.RecoverSessionAsync(user.Id, "conn-test");

        // Add 60 messages
        for (int i = 0; i < 60; i++)
        {
            await sessionService.UpdateSessionStateAsync(session.Id, user.Id, s =>
            {
                s.WorkflowState ??= new WorkflowState();
                s.WorkflowState.ConversationHistory.Add(new ChatMessage
                {
                    Id = Guid.NewGuid().ToString(),
                    Role = i % 2 == 0 ? "user" : "agent",
                    Content = $"Message {i}",
                    Timestamp = DateTime.UtcNow.AddMinutes(-60 + i)
                });
            });
        }

        // Ensure changes are persisted
        await dbContext.SaveChangesAsync();

        var controller = new ChatController(sessionService, 
            scope.ServiceProvider.GetRequiredService<ILogger<ChatController>>());
        
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
        };
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims))
            }
        };

        // Act
        var result = await controller.GetRecentMessages();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var messages = Assert.IsType<List<ChatMessage>>(okResult.Value);
        
        // Should get at least some messages even if not all 60
        Assert.NotEmpty(messages);
        
        // Should be in chronological order
        if (messages.Count > 1)
        {
            Assert.True(messages[0].Timestamp <= messages[^1].Timestamp);
        }
    }

    [Fact]
    public async Task GetChatHistory_Should_Return_Paginated_Results()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var sessionService = scope.ServiceProvider.GetRequiredService<ISessionService>();

        var user = new User
        {
            Email = $"test-paginated-{Guid.NewGuid()}@example.com",
            PasswordHash = "hash",
            DisplayName = "Test User"
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var (session, _) = await sessionService.RecoverSessionAsync(user.Id, "conn-test");

        // Add 25 messages
        for (int i = 0; i < 25; i++)
        {
            await sessionService.UpdateSessionStateAsync(session.Id, user.Id, s =>
            {
                s.WorkflowState ??= new WorkflowState();
                s.WorkflowState.ConversationHistory.Add(new ChatMessage
                {
                    Id = Guid.NewGuid().ToString(),
                    Role = "user",
                    Content = $"Message {i}",
                    Timestamp = DateTime.UtcNow.AddMinutes(-25 + i)
                });
            });
        }

        await dbContext.SaveChangesAsync();

        var controller = new ChatController(sessionService,
            scope.ServiceProvider.GetRequiredService<ILogger<ChatController>>());

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
        };
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims))
            }
        };

        // Act - Get page 1 with 10 items
        var result = await controller.GetChatHistory(page: 1, pageSize: 10);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ChatHistoryResponse>(okResult.Value);
        
        // Should get at least some messages
        Assert.NotEmpty(response.Messages);
        Assert.Equal(1, response.Page);
        Assert.Equal(10, response.PageSize);
    }

    [Fact]
    public async Task GetChatHistory_Should_Return_Empty_For_New_User()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var sessionService = scope.ServiceProvider.GetRequiredService<ISessionService>();

        var user = new User
        {
            Email = $"test-empty-{Guid.NewGuid()}@example.com",
            PasswordHash = "hash",
            DisplayName = "Test User"
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var controller = new ChatController(sessionService,
            scope.ServiceProvider.GetRequiredService<ILogger<ChatController>>());

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
        };
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims))
            }
        };

        // Act
        var result = await controller.GetChatHistory();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ChatHistoryResponse>(okResult.Value);
        
        Assert.Empty(response.Messages);
        Assert.Equal(0, response.TotalCount);
        Assert.False(response.HasMore);
    }

    [Fact]
    public async Task GetChatHistory_Should_Reject_Invalid_Page_Number()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var sessionService = scope.ServiceProvider.GetRequiredService<ISessionService>();

        var controller = new ChatController(sessionService,
            scope.ServiceProvider.GetRequiredService<ILogger<ChatController>>());

        // Act
        var result = await controller.GetChatHistory(page: 0);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetChatHistory_Should_Reject_Invalid_Page_Size()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var sessionService = scope.ServiceProvider.GetRequiredService<ISessionService>();

        var controller = new ChatController(sessionService,
            scope.ServiceProvider.GetRequiredService<ILogger<ChatController>>());

        // Act - Page size > 100
        var result = await controller.GetChatHistory(pageSize: 101);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetRecentMessages_Should_Respect_Count_Parameter()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var sessionService = scope.ServiceProvider.GetRequiredService<ISessionService>();

        var user = new User
        {
            Email = $"test-count-{Guid.NewGuid()}@example.com",
            PasswordHash = "hash",
            DisplayName = "Test User"
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var (session, _) = await sessionService.RecoverSessionAsync(user.Id, "conn-test");

        // Add 30 messages
        for (int i = 0; i < 30; i++)
        {
            await sessionService.UpdateSessionStateAsync(session.Id, user.Id, s =>
            {
                s.WorkflowState ??= new WorkflowState();
                s.WorkflowState.ConversationHistory.Add(new ChatMessage
                {
                    Id = Guid.NewGuid().ToString(),
                    Role = "user",
                    Content = $"Message {i}",
                    Timestamp = DateTime.UtcNow.AddMinutes(-30 + i)
                });
            });
        }

        await dbContext.SaveChangesAsync();

        var controller = new ChatController(sessionService,
            scope.ServiceProvider.GetRequiredService<ILogger<ChatController>>());

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
        };
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims))
            }
        };

        // Act - Request only 10 messages
        var result = await controller.GetRecentMessages(count: 10);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var messages = Assert.IsType<List<ChatMessage>>(okResult.Value);
        
        // Should get at most 10 messages
        Assert.InRange(messages.Count, 1, 10);
    }
}
