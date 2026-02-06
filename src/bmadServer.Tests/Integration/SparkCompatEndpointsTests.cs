using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace bmadServer.Tests.Integration;

public class SparkCompatEndpointsTests : IClassFixture<TestWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly HttpClient _client;

    public SparkCompatEndpointsTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task AuthMe_Unauthorized_UsesEnvelopeFormat()
    {
        var response = await _client.GetAsync("/v1/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var envelope = await response.Content.ReadFromJsonAsync<ResponseEnvelope<JsonElement>>(JsonOptions);
        Assert.NotNull(envelope);
        Assert.False(envelope!.Success);
        Assert.Equal(401, envelope.StatusCode);
        Assert.Equal("Invalid or missing authentication token.", envelope.Message);
    }

    [Fact]
    public async Task AuthChatAndTranslate_RoundTrip_Works()
    {
        var session = await SignUpAndAuthenticateAsync("business");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.Token);

        var chatResponse = await _client.PostAsJsonAsync("/v1/chats", new
        {
            title = "Contract Alignment",
            domain = "BMAD",
            service = "CHAT",
            feature = "Spark Compatibility"
        });
        Assert.Equal(HttpStatusCode.Created, chatResponse.StatusCode);
        var chatEnvelope = await chatResponse.Content.ReadFromJsonAsync<ResponseEnvelope<JsonElement>>(JsonOptions);
        var chatId = chatEnvelope!.Data.GetProperty("id").GetString();
        Assert.False(string.IsNullOrWhiteSpace(chatId));

        var sendResponse = await _client.PostAsJsonAsync($"/v1/chats/{chatId}/messages", new
        {
            content = "Please summarize OAuth callback handling for a non-technical founder.",
            personaOverride = "business"
        });
        Assert.Equal(HttpStatusCode.Created, sendResponse.StatusCode);
        var sendEnvelope = await sendResponse.Content.ReadFromJsonAsync<ResponseEnvelope<JsonElement>>(JsonOptions);
        var userMessageId = sendEnvelope!.Data.GetProperty("userMessage").GetProperty("id").GetString();
        Assert.False(string.IsNullOrWhiteSpace(userMessageId));

        var translateResponse = await _client.PostAsJsonAsync($"/v1/chats/{chatId}/messages/{userMessageId}/translate", new
        {
            role = "business"
        });
        Assert.Equal(HttpStatusCode.OK, translateResponse.StatusCode);
        var translateEnvelope = await translateResponse.Content.ReadFromJsonAsync<ResponseEnvelope<JsonElement>>(JsonOptions);
        var segments = translateEnvelope!.Data.GetProperty("translation").GetProperty("segments");
        Assert.True(segments.GetArrayLength() >= 1);
    }

    [Fact]
    public async Task PullRequestLifecycle_ApproveAndMerge_Works()
    {
        var author = await SignUpAndAuthenticateAsync("technical");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", author.Token);

        var chatResponse = await _client.PostAsJsonAsync("/v1/chats", new
        {
            title = "PR Workflow",
            domain = "BMAD",
            service = "CHAT",
            feature = "PR Lifecycle"
        });
        var chatEnvelope = await chatResponse.Content.ReadFromJsonAsync<ResponseEnvelope<JsonElement>>(JsonOptions);
        var chatId = chatEnvelope!.Data.GetProperty("id").GetString()!;

        var prCreateResponse = await _client.PostAsJsonAsync("/v1/pull-requests", new
        {
            title = "Add rollout docs",
            description = "Adds rollout sequencing and feature flags.",
            chatId,
            fileChanges = new[]
            {
                new
                {
                    path = ".bmad/decisions/rollout.md",
                    additions = new[] { "# Rollout", "Stage 1: internal" },
                    deletions = Array.Empty<string>(),
                    status = "staged"
                }
            }
        });
        Assert.Equal(HttpStatusCode.Created, prCreateResponse.StatusCode);
        var prEnvelope = await prCreateResponse.Content.ReadFromJsonAsync<ResponseEnvelope<JsonElement>>(JsonOptions);
        var prId = prEnvelope!.Data.GetProperty("id").GetString()!;

        var reviewer = await SignUpAndAuthenticateAsync("business");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", reviewer.Token);

        var approveResponse = await _client.PostAsJsonAsync($"/v1/pull-requests/{prId}/approve", new { });
        Assert.Equal(HttpStatusCode.OK, approveResponse.StatusCode);

        var mergeResponse = await _client.PostAsJsonAsync($"/v1/pull-requests/{prId}/merge", new { });
        Assert.Equal(HttpStatusCode.OK, mergeResponse.StatusCode);
        var mergeEnvelope = await mergeResponse.Content.ReadFromJsonAsync<ResponseEnvelope<JsonElement>>(JsonOptions);
        Assert.Equal("merged", mergeEnvelope!.Data.GetProperty("status").GetString());

        var lineCommentResponse = await _client.PostAsJsonAsync($"/v1/pull-requests/{prId}/files/{Uri.EscapeDataString(".bmad/decisions/rollout.md")}/comments", new
        {
            lineNumber = 1,
            lineType = "addition",
            content = "Looks good",
        });
        Assert.Equal(HttpStatusCode.Created, lineCommentResponse.StatusCode);
    }

    [Fact]
    public async Task CollaborationEvents_FilteredBySince_StableOrder()
    {
        var user = await SignUpAndAuthenticateAsync("business");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.Token);
        var runMarker = Guid.NewGuid().ToString("N");

        var firstResponse = await _client.PostAsJsonAsync("/v1/collaboration-events", new
        {
            type = "message_sent",
            metadata = new { seq = 1, runMarker }
        });
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        var firstEnvelope = await firstResponse.Content.ReadFromJsonAsync<ResponseEnvelope<JsonElement>>(JsonOptions);
        var firstTimestamp = firstEnvelope!.Data.GetProperty("timestamp").GetInt64();

        await Task.Delay(5);

        var secondResponse = await _client.PostAsJsonAsync("/v1/collaboration-events", new
        {
            type = "pr_created",
            metadata = new { seq = 2, runMarker }
        });
        Assert.Equal(HttpStatusCode.Created, secondResponse.StatusCode);

        var listResponse = await _client.GetAsync($"/v1/collaboration-events?since={firstTimestamp}");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var listEnvelope = await listResponse.Content.ReadFromJsonAsync<ResponseEnvelope<JsonElement>>(JsonOptions);
        var events = listEnvelope!.Data.GetProperty("events");
        Assert.True(events.GetArrayLength() >= 1);

        long previousTimestamp = long.MinValue;
        var matchedSecondEvent = false;
        foreach (var evt in events.EnumerateArray())
        {
            var timestamp = evt.GetProperty("timestamp").GetInt64();
            Assert.True(timestamp >= previousTimestamp);
            previousTimestamp = timestamp;

            if (evt.GetProperty("type").GetString() != "pr_created")
            {
                continue;
            }

            if (!evt.TryGetProperty("metadata", out var metadata))
            {
                continue;
            }

            if (metadata.TryGetProperty("runMarker", out var marker) && marker.GetString() == runMarker)
            {
                matchedSecondEvent = true;
                break;
            }
        }

        Assert.True(matchedSecondEvent);
    }

    [Fact]
    public async Task DecisionCenter_LockHistoryConflictFlow_Works()
    {
        var user = await SignUpAndAuthenticateAsync("technical");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.Token);

        var chatResponse = await _client.PostAsJsonAsync("/v1/chats", new
        {
            title = "Decision Testing",
            domain = "BMAD",
            service = "CHAT",
            feature = "Decision Center"
        });
        var chatEnvelope = await chatResponse.Content.ReadFromJsonAsync<ResponseEnvelope<JsonElement>>(JsonOptions);
        var chatId = chatEnvelope!.Data.GetProperty("id").GetString()!;

        var decisionOneResponse = await _client.PostAsJsonAsync("/v1/decisions", new
        {
            chatId,
            title = "Auth strategy",
            value = new { option = "jwt" },
            reason = "initial"
        });
        Assert.Equal(HttpStatusCode.Created, decisionOneResponse.StatusCode);
        var decisionOneEnvelope = await decisionOneResponse.Content.ReadFromJsonAsync<ResponseEnvelope<JsonElement>>(JsonOptions);
        var decisionId = decisionOneEnvelope!.Data.GetProperty("id").GetString()!;

        var lockResponse = await _client.PostAsJsonAsync($"/v1/decisions/{decisionId}/lock", new { reason = "editing" });
        Assert.Equal(HttpStatusCode.OK, lockResponse.StatusCode);

        var updateResponse = await _client.PatchAsJsonAsync($"/v1/decisions/{decisionId}", new
        {
            value = new { option = "session-jwt" },
            reason = "refine"
        });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var unlockResponse = await _client.PostAsJsonAsync($"/v1/decisions/{decisionId}/unlock", new { });
        Assert.Equal(HttpStatusCode.OK, unlockResponse.StatusCode);

        var decisionTwoResponse = await _client.PostAsJsonAsync("/v1/decisions", new
        {
            chatId,
            title = "Auth strategy",
            value = new { option = "oauth" },
            reason = "alternative"
        });
        Assert.Equal(HttpStatusCode.Created, decisionTwoResponse.StatusCode);

        var historyResponse = await _client.GetAsync($"/v1/decisions/{decisionId}/history");
        Assert.Equal(HttpStatusCode.OK, historyResponse.StatusCode);
        var historyEnvelope = await historyResponse.Content.ReadFromJsonAsync<ResponseEnvelope<JsonElement>>(JsonOptions);
        Assert.True(historyEnvelope!.Data.GetProperty("versions").GetArrayLength() >= 2);

        var conflictsResponse = await _client.GetAsync($"/v1/decisions/{decisionId}/conflicts");
        Assert.Equal(HttpStatusCode.OK, conflictsResponse.StatusCode);
        var conflictsEnvelope = await conflictsResponse.Content.ReadFromJsonAsync<ResponseEnvelope<JsonElement>>(JsonOptions);
        var conflicts = conflictsEnvelope!.Data.GetProperty("conflicts");
        if (conflicts.GetArrayLength() > 0)
        {
            var conflictId = conflicts[0].GetProperty("id").GetGuid();
            var resolveResponse = await _client.PostAsJsonAsync($"/v1/decisions/{decisionId}/conflicts/{conflictId}/resolve", new
            {
                resolution = "team-aligned"
            });
            Assert.Equal(HttpStatusCode.OK, resolveResponse.StatusCode);
        }
    }

    private async Task<AuthSession> SignUpAndAuthenticateAsync(string role)
    {
        var email = $"{Guid.NewGuid():N}@example.com";
        var signUpResponse = await _client.PostAsJsonAsync("/v1/auth/signup", new
        {
            email,
            password = "SecurePass123!",
            name = $"User-{Guid.NewGuid():N}".Substring(0, 12),
            role
        });

        Assert.True(signUpResponse.StatusCode == HttpStatusCode.Created || signUpResponse.StatusCode == HttpStatusCode.OK);

        var envelope = await signUpResponse.Content.ReadFromJsonAsync<ResponseEnvelope<JsonElement>>(JsonOptions);
        Assert.NotNull(envelope);

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
}
