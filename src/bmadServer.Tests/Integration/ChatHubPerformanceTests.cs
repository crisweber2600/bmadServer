using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Json;
using Xunit;

namespace bmadServer.Tests.Integration;

/// <summary>
/// Performance tests for ChatHub to verify NFR1:
/// Messages must be acknowledged within 2 seconds.
/// </summary>
public class ChatHubPerformanceTests : IClassFixture<TestWebApplicationFactory>, IAsyncLifetime
{
    private readonly TestWebApplicationFactory _factory;
    private HubConnection? _connection;
    private string? _accessToken;

    public ChatHubPerformanceTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        // Register and login to get access token
        var client = _factory.CreateClient();
        
        var email = $"perf-test-{Guid.NewGuid()}@example.com";
        var registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            Email = email,
            Password = "Test123!@#",
            DisplayName = "Performance Test User"
        });
        
        Assert.True(registerResponse.IsSuccessStatusCode, $"Registration failed: {await registerResponse.Content.ReadAsStringAsync()}");
        
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            Email = email,
            Password = "Test123!@#"
        });
        
        Assert.True(loginResponse.IsSuccessStatusCode, "Login failed");
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<System.Text.Json.JsonDocument>();
        _accessToken = loginResult!.RootElement.GetProperty("accessToken").GetString();

        // Create SignalR connection using test server's handler
        _connection = new HubConnectionBuilder()
            .WithUrl($"{_factory.Server.BaseAddress}hubs/chat", options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                options.AccessTokenProvider = () => Task.FromResult(_accessToken)!;
            })
            .Build();

        await _connection.StartAsync();
    }

    public async Task DisposeAsync()
    {
        if (_connection != null)
        {
            await _connection.DisposeAsync();
        }
    }

    /// <summary>
    /// NFR1: Verify that message acknowledgment happens within 2 seconds.
    /// </summary>
    [Fact]
    public async Task SendMessage_ShouldAcknowledgeWithin2Seconds()
    {
        // Arrange
        var messageReceived = new TaskCompletionSource<bool>();
        var startTime = DateTime.UtcNow;
        
        _connection!.On<object>("ReceiveMessage", _ =>
        {
            messageReceived.SetResult(true);
        });

        // Act
        await _connection.InvokeAsync("SendMessage", "Performance test message");
        
        // Wait for acknowledgment with timeout
        var completed = await Task.WhenAny(
            messageReceived.Task,
            Task.Delay(TimeSpan.FromSeconds(3)));

        // Assert
        Assert.True(completed == messageReceived.Task, "Message was not acknowledged within timeout");
        
        var elapsed = (DateTime.UtcNow - startTime).TotalSeconds;
        Assert.True(elapsed < 2.0, $"NFR1 violated: Message acknowledgment took {elapsed:F3}s (must be < 2s)");
    }

    /// <summary>
    /// NFR1: Verify that SendMessage completes within 2 seconds under normal load.
    /// </summary>
    [Fact]
    public async Task SendMessage_ShouldCompleteWithin2Seconds()
    {
        // Arrange
        var startTime = DateTime.UtcNow;

        // Act
        await _connection!.InvokeAsync("SendMessage", "Test message");

        // Assert
        var elapsed = (DateTime.UtcNow - startTime).TotalSeconds;
        Assert.True(elapsed < 2.0, $"NFR1 violated: SendMessage took {elapsed:F3}s (must be < 2s)");
    }

    /// <summary>
    /// NFR1: Verify message acknowledgment performance under sequential load.
    /// </summary>
    [Fact]
    public async Task SendMessage_MultipleMessages_ShouldEachAcknowledgeWithin2Seconds()
    {
        // Arrange
        const int messageCount = 5;
        var times = new List<double>();
        TaskCompletionSource<bool>? messageReceived = null;

        _connection!.On<object>("ReceiveMessage", _ =>
        {
            messageReceived?.TrySetResult(true);
        });

        // Act
        for (int i = 0; i < messageCount; i++)
        {
            messageReceived = new TaskCompletionSource<bool>();
            var startTime = DateTime.UtcNow;

            await _connection.InvokeAsync("SendMessage", $"Message {i}");
            
            await Task.WhenAny(
                messageReceived.Task,
                Task.Delay(TimeSpan.FromSeconds(3)));

            var elapsed = (DateTime.UtcNow - startTime).TotalSeconds;
            times.Add(elapsed);
        }

        // Assert
        foreach (var time in times)
        {
            Assert.True(time < 2.0, $"NFR1 violated: One or more messages took {time:F3}s (must be < 2s)");
        }
    }
}
