using System.Net.Http.Json;
using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Microsoft.AspNetCore.SignalR.Client;

namespace bmadServer.ApiService.IntegrationTests;

public class ChatHubAspireTests : IAsyncLifetime
{
    private DistributedApplication? _app;
    private HttpClient? _httpClient;
    private string? _accessToken;

    public async Task InitializeAsync()
    {
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.bmadServer_AppHost>();

        _app = await appHost.BuildAsync();
        await _app.StartAsync();

        _httpClient = _app.CreateHttpClient("apiservice");
    }

    public async Task DisposeAsync()
    {
        _httpClient?.Dispose();
        if (_app != null)
        {
            await _app.DisposeAsync();
        }
    }

    private async Task<string> GetAccessTokenAsync()
    {
        if (_accessToken != null) return _accessToken;

        var email = $"test-signalr-{Guid.NewGuid()}@example.com";
        var password = "Test123!@#";

        var registerResponse = await _httpClient!.PostAsJsonAsync("/api/auth/register", new
        {
            Email = email,
            Password = password
        });
        registerResponse.EnsureSuccessStatusCode();

        var loginResponse = await _httpClient.PostAsJsonAsync("/api/auth/login", new
        {
            Email = email,
            Password = password
        });
        loginResponse.EnsureSuccessStatusCode();

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        _accessToken = loginResult!.AccessToken;
        return _accessToken;
    }

    private async Task<HubConnection> CreateHubConnectionAsync()
    {
        var token = await GetAccessTokenAsync();
        var baseUrl = _httpClient!.BaseAddress!.ToString().TrimEnd('/');

        var connection = new HubConnectionBuilder()
            .WithUrl($"{baseUrl}/hubs/chat", options =>
            {
                options.AccessTokenProvider = () => Task.FromResult<string?>(token);
            })
            .Build();

        return connection;
    }

    [Fact]
    public async Task ChatHub_ConnectWithValidToken_ShouldSucceed()
    {
        var connection = await CreateHubConnectionAsync();

        await connection.StartAsync();

        Assert.Equal(HubConnectionState.Connected, connection.State);

        await connection.DisposeAsync();
    }

    [Fact]
    public async Task ChatHub_SendMessage_ShouldReceiveUserMessageBack()
    {
        var connection = await CreateHubConnectionAsync();
        var messageReceived = new TaskCompletionSource<object>();

        connection.On<object>("ReceiveMessage", msg =>
        {
            messageReceived.SetResult(msg);
        });

        await connection.StartAsync();

        await connection.InvokeAsync("SendMessage", "Hello from integration test");

        var result = await Task.WhenAny(
            messageReceived.Task,
            Task.Delay(TimeSpan.FromSeconds(5)));

        Assert.Equal(messageReceived.Task, result);

        await connection.DisposeAsync();
    }

    [Fact]
    public async Task ChatHub_SendMessage_ShouldStreamAgentResponse()
    {
        var connection = await CreateHubConnectionAsync();
        var chunksReceived = new List<object>();
        var streamComplete = new TaskCompletionSource<bool>();

        connection.On<object>("MESSAGE_CHUNK", chunk =>
        {
            chunksReceived.Add(chunk);
            
            var chunkDict = chunk as IDictionary<string, object>;
            if (chunkDict != null && chunkDict.TryGetValue("IsComplete", out var isComplete) && 
                isComplete is bool complete && complete)
            {
                streamComplete.SetResult(true);
            }
        });

        await connection.StartAsync();

        await connection.InvokeAsync("SendMessage", "Tell me about help");

        var result = await Task.WhenAny(
            streamComplete.Task,
            Task.Delay(TimeSpan.FromSeconds(15)));

        Assert.True(chunksReceived.Count > 0, "Should have received at least one chunk");
        Assert.Equal(streamComplete.Task, result);

        await connection.DisposeAsync();
    }

    [Fact]
    public async Task ChatHub_SendMessage_ShouldAcknowledgeWithin2Seconds()
    {
        var connection = await CreateHubConnectionAsync();
        var messageReceived = new TaskCompletionSource<bool>();
        var startTime = DateTime.UtcNow;

        connection.On<object>("ReceiveMessage", _ =>
        {
            messageReceived.SetResult(true);
        });

        await connection.StartAsync();

        await connection.InvokeAsync("SendMessage", "Performance test message");

        var completed = await Task.WhenAny(
            messageReceived.Task,
            Task.Delay(TimeSpan.FromSeconds(3)));

        var elapsed = (DateTime.UtcNow - startTime).TotalSeconds;

        Assert.True(completed == messageReceived.Task, "Message was not acknowledged within timeout");
        Assert.True(elapsed < 2.0, $"NFR1 violated: Message acknowledgment took {elapsed:F3}s (must be < 2s)");

        await connection.DisposeAsync();
    }

    [Fact]
    public async Task ChatHub_StopGenerating_ShouldStopStream()
    {
        var connection = await CreateHubConnectionAsync();
        var stoppedReceived = new TaskCompletionSource<object>();
        string? messageId = null;

        connection.On<object>("MESSAGE_CHUNK", chunk =>
        {
            var chunkDict = chunk as IDictionary<string, object>;
            if (chunkDict != null && chunkDict.TryGetValue("MessageId", out var id))
            {
                messageId = id?.ToString();
            }
        });

        connection.On<object>("GENERATION_STOPPED", data =>
        {
            stoppedReceived.SetResult(data);
        });

        await connection.StartAsync();

        await connection.InvokeAsync("SendMessage", "Give me a long response about code");

        await Task.Delay(200);

        if (messageId != null)
        {
            await connection.InvokeAsync("StopGenerating", messageId);
        }

        var result = await Task.WhenAny(
            stoppedReceived.Task,
            Task.Delay(TimeSpan.FromSeconds(5)));

        Assert.Equal(stoppedReceived.Task, result);

        await connection.DisposeAsync();
    }

    [Fact]
    public async Task ChatHub_SessionRecovery_ShouldRestoreWithin60Seconds()
    {
        var connection1 = await CreateHubConnectionAsync();
        var sessionRestored = new TaskCompletionSource<object>();

        await connection1.StartAsync();

        await connection1.InvokeAsync("SendMessage", "Initial message for session");

        await connection1.DisposeAsync();

        await Task.Delay(500);

        var connection2 = await CreateHubConnectionAsync();
        
        connection2.On<object>("SESSION_RESTORED", data =>
        {
            sessionRestored.SetResult(data);
        });

        await connection2.StartAsync();

        var result = await Task.WhenAny(
            sessionRestored.Task,
            Task.Delay(TimeSpan.FromSeconds(5)));

        Assert.Equal(sessionRestored.Task, result);

        await connection2.DisposeAsync();
    }

    [Fact]
    public async Task ChatHub_JoinWorkflow_ShouldSucceed()
    {
        var connection = await CreateHubConnectionAsync();

        await connection.StartAsync();

        await connection.InvokeAsync("JoinWorkflow", "test-workflow");

        Assert.Equal(HubConnectionState.Connected, connection.State);

        await connection.DisposeAsync();
    }

    [Fact]
    public async Task ChatHub_LeaveWorkflow_ShouldSucceed()
    {
        var connection = await CreateHubConnectionAsync();

        await connection.StartAsync();

        await connection.InvokeAsync("JoinWorkflow", "test-workflow");
        await connection.InvokeAsync("LeaveWorkflow", "test-workflow");

        Assert.Equal(HubConnectionState.Connected, connection.State);

        await connection.DisposeAsync();
    }

    private record LoginResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt);
}
