using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using bmadServer.ApiService.Configuration;
using bmadServer.ApiService.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace bmadServer.Tests.Integration;

/// <summary>
/// Tests that SparkCompatRollout feature flags correctly gate endpoints.
/// When a flag is disabled, the corresponding controller returns 404.
/// Also covers the presence controller happy path (missing from existing tests).
/// </summary>
public class SparkCompatFeatureFlagTests : IClassFixture<TestWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly TestWebApplicationFactory _factory;

    public SparkCompatFeatureFlagTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ──────────────────────────────────────────────────
    // Happy-path: Presence controller (update + list)
    // ──────────────────────────────────────────────────

    [Fact]
    public async Task Presence_UpdateAndList_Works()
    {
        var client = _factory.CreateClient();
        var session = await SignUpAndAuthenticateAsync(client, "business");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.Token);

        // Update presence
        var updateResponse = await client.PutAsJsonAsync($"/v1/users/{session.UserId}/presence", new
        {
            status = "online",
            domain = "chat",
            isTyping = false
        });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updateEnvelope = await updateResponse.Content.ReadFromJsonAsync<ResponseEnvelope<JsonElement>>(JsonOptions);
        Assert.NotNull(updateEnvelope);
        Assert.True(updateEnvelope!.Success);
        Assert.Equal("online", updateEnvelope.Data.GetProperty("status").GetString());

        // List presence
        var listResponse = await client.GetAsync("/v1/presence");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var listEnvelope = await listResponse.Content.ReadFromJsonAsync<ResponseEnvelope<JsonElement>>(JsonOptions);
        Assert.NotNull(listEnvelope);
        Assert.True(listEnvelope!.Success);
        var users = listEnvelope.Data.GetProperty("users");
        Assert.True(users.GetArrayLength() >= 1);
    }

    // ──────────────────────────────────────────────────
    // Feature-flag gating: disabling a flag returns 404
    // ──────────────────────────────────────────────────

    [Fact]
    public async Task ChatsDisabled_ListChats_Returns404()
    {
        await AssertFeatureFlagGated(
            configure: opts => opts.EnableChats = false,
            requestAsync: async client =>
            {
                var session = await SignUpAndAuthenticateAsync(client, "business");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.Token);
                return await client.GetAsync("/v1/chats");
            });
    }

    [Fact]
    public async Task AuthDisabled_SignUp_Returns404()
    {
        await AssertFeatureFlagGated(
            configure: opts => opts.EnableAuth = false,
            requestAsync: async client =>
            {
                // Auth signup doesn't require a token, but with the flag off it should 404
                return await client.PostAsJsonAsync("/v1/auth/signup", new
                {
                    email = $"{Guid.NewGuid():N}@example.com",
                    password = "SecurePass123!",
                    name = "FlagTest",
                    role = "business"
                });
            });
    }

    [Fact]
    public async Task PresenceDisabled_ListPresence_Returns404()
    {
        await AssertFeatureFlagGated(
            configure: opts => opts.EnablePresence = false,
            requestAsync: async client =>
            {
                // Need to auth first since presence requires [Authorize]
                // Sign up with default (enabled) auth, then hit disabled presence
                var session = await SignUpWithFlagFactory(client);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.Token);
                return await client.GetAsync("/v1/presence");
            });
    }

    [Fact]
    public async Task PullRequestsDisabled_ListPRs_Returns404()
    {
        await AssertFeatureFlagGated(
            configure: opts => opts.EnablePullRequests = false,
            requestAsync: async client =>
            {
                var session = await SignUpWithFlagFactory(client);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.Token);
                return await client.GetAsync("/v1/pull-requests");
            });
    }

    [Fact]
    public async Task CollaborationEventsDisabled_ListEvents_Returns404()
    {
        await AssertFeatureFlagGated(
            configure: opts => opts.EnableCollaborationEvents = false,
            requestAsync: async client =>
            {
                var session = await SignUpWithFlagFactory(client);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.Token);
                return await client.GetAsync("/v1/collaboration-events");
            });
    }

    [Fact]
    public async Task DecisionCenterDisabled_ListDecisions_Returns404()
    {
        await AssertFeatureFlagGated(
            configure: opts => opts.EnableDecisionCenter = false,
            requestAsync: async client =>
            {
                var session = await SignUpWithFlagFactory(client);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.Token);
                return await client.GetAsync("/v1/decisions?chatId=test-chat");
            });
    }

    [Fact]
    public async Task MasterToggleDisabled_AllEndpoints_Return404()
    {
        await AssertFeatureFlagGated(
            configure: opts => opts.EnableCompatV1 = false,
            requestAsync: async client =>
            {
                return await client.PostAsJsonAsync("/v1/auth/signup", new
                {
                    email = $"{Guid.NewGuid():N}@example.com",
                    password = "SecurePass123!",
                    name = "MasterOff",
                    role = "business"
                });
            });
    }

    // ──────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────

    /// <summary>
    /// Creates a custom factory with a specific feature flag disabled, sends a request,
    /// and asserts 404 is returned with the DisabledResponse envelope.
    /// </summary>
    private async Task AssertFeatureFlagGated(
        Action<SparkCompatRolloutOptions> configure,
        Func<HttpClient, Task<HttpResponseMessage>> requestAsync)
    {
        await using var customFactory = new FeatureFlagTestFactory(configure);
        var client = customFactory.CreateClient();
        var response = await requestAsync(client);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var envelope = await response.Content.ReadFromJsonAsync<ResponseEnvelope<JsonElement>>(JsonOptions);
        Assert.NotNull(envelope);
        Assert.False(envelope!.Success);
        Assert.Equal(404, envelope.StatusCode);
        Assert.Contains("disabled", envelope.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Sign up via a flag-test factory client. Auth flags must still be enabled.
    /// </summary>
    private static async Task<AuthSession> SignUpWithFlagFactory(HttpClient client)
    {
        var email = $"{Guid.NewGuid():N}@example.com";
        var response = await client.PostAsJsonAsync("/v1/auth/signup", new
        {
            email,
            password = "SecurePass123!",
            name = $"User-{Guid.NewGuid():N}"[..12],
            role = "business"
        });

        Assert.True(response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.OK,
            $"Signup failed with {response.StatusCode}");

        var envelope = await response.Content.ReadFromJsonAsync<ResponseEnvelope<JsonElement>>(JsonOptions);
        return new AuthSession
        {
            UserId = envelope!.Data.GetProperty("user").GetProperty("id").GetString()!,
            Token = envelope.Data.GetProperty("token").GetString()!
        };
    }

    private static async Task<AuthSession> SignUpAndAuthenticateAsync(HttpClient client, string role)
    {
        var email = $"{Guid.NewGuid():N}@example.com";
        var response = await client.PostAsJsonAsync("/v1/auth/signup", new
        {
            email,
            password = "SecurePass123!",
            name = $"User-{Guid.NewGuid():N}"[..12],
            role
        });

        Assert.True(response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.OK);
        var envelope = await response.Content.ReadFromJsonAsync<ResponseEnvelope<JsonElement>>(JsonOptions);
        return new AuthSession
        {
            UserId = envelope!.Data.GetProperty("user").GetProperty("id").GetString()!,
            Token = envelope.Data.GetProperty("token").GetString()!
        };
    }

    private sealed class AuthSession
    {
        public string UserId { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
    }

    private sealed class ResponseEnvelope<T>
    {
        public bool Success { get; set; }
        public int StatusCode { get; set; }
        public string Message { get; set; } = string.Empty;
        public T Data { get; set; } = default!;
    }

    /// <summary>
    /// A custom WebApplicationFactory that overrides SparkCompatRolloutOptions
    /// to disable specific feature flags for testing.
    /// </summary>
    private sealed class FeatureFlagTestFactory : WebApplicationFactory<bmadServer.ApiService.Program>
    {
        private readonly Action<SparkCompatRolloutOptions> _configureOptions;
        private readonly SqliteConnection _connection;
        private readonly string _dbName;

        public FeatureFlagTestFactory(Action<SparkCompatRolloutOptions> configureOptions)
        {
            _configureOptions = configureOptions;
            _dbName = $"test_ff_{Guid.NewGuid():N}.db";
            _connection = new SqliteConnection($"DataSource={_dbName};Mode=Memory;Cache=Shared");
            _connection.Open();

            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "PRAGMA foreign_keys = OFF;";
            cmd.ExecuteNonQuery();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Remove existing DbContext registrations
                var descriptors = services.Where(d =>
                    d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                    d.ServiceType == typeof(ApplicationDbContext) ||
                    (d.ServiceType.IsGenericType &&
                     d.ServiceType.GetGenericTypeDefinition() == typeof(DbContextOptions<>))).ToList();

                foreach (var descriptor in descriptors)
                {
                    services.Remove(descriptor);
                }

                var connectionString = $"DataSource={_dbName};Mode=Memory;Cache=Shared;Foreign Keys=False";
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseSqlite(connectionString);
                });

                // Override the feature flag options
                services.Configure<SparkCompatRolloutOptions>(_configureOptions);
            });

            builder.UseEnvironment("Test");
        }

        protected override IHost CreateHost(IHostBuilder builder)
        {
            var host = base.CreateHost(builder);

            using var scope = host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.EnsureCreated();
            db.Database.ExecuteSqlRaw("PRAGMA foreign_keys = OFF;");

            return host;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                _connection.Dispose();
                if (File.Exists(_dbName))
                {
                    try { File.Delete(_dbName); } catch { /* best effort */ }
                }
            }
        }
    }
}
