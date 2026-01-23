# ADR-003: In-Process Agent Router with Queue-Ready Interface

**Date:** 2026-01-23  
**Status:** ACCEPTED  
**Category:** 2 - Security & Authorization  
**Decision ID:** 2.5 (Relates to Infrastructure)

---

## Context

bmadServer must route user requests to BMAD agents (PM Agent, Architect Agent, Dev Agent, etc.) with these requirements:

1. **MVP Speed:** Sub-millisecond routing for local agents
2. **Message Integrity:** Prevent agent spoofing or message tampering
3. **Deadlock Prevention:** Detect circular agent dependencies (Agent A → B → A)
4. **Timeout Safety:** Prevent runaway agent calls from hanging the system
5. **Future Scalability:** Path to distributed message queues (Phase 2+)

**Key Tension:** In-process routing is fast but not distributed. Message queues are distributed but slower. We must choose one for MVP.

---

## Decision

**In-Process Agent Router for MVP with Queue-Ready Interface:**

1. **Routing:** All agent calls routed through `IAgentRouter` mediator (in-process)
2. **Message Format:** Signed messages with HMAC + timestamp (prevent tampering)
3. **Deadlock Detection:** Call stack tracking, circular reference detection
4. **Timeout Management:** All agent calls wrapped with 30-second timeout
5. **Logging:** Full reasoning trace logged (prompt, response, confidence, contradictions)
6. **Interface Abstraction:** `IAgentRouter` enables future `ServiceBusAgentRouter` (Phase 2)

---

## Rationale

### Why In-Process?
- ✅ **MVP speed** - sub-millisecond latency (no network overhead)
- ✅ **Operational simplicity** - single deployment unit, no message broker setup
- ✅ **Self-hosted fit** - no dependency on cloud services
- ✅ **Type-safe** - direct C# method calls, compile-time checking
- ✅ **Debugging** - full stack traces available locally

### Why Not Message Queue (Kafka, RabbitMQ)?
- ❌ **MVP overkill** - adds operational complexity (another service to manage)
- ❌ **Network latency** - requests cross process boundary (slower)
- ❌ **Operational burden** - self-hosted message broker requires management
- ❌ **MVP team size** - Cris doesn't have bandwidth to manage broker
- ✅ **Phase 2 path** - interface abstraction enables easy migration

### Why Message Signing?
- ✅ **Prevents agent spoofing** - malicious code can't inject fake agent responses
- ✅ **Audit trail** - every agent call is cryptographically signed
- ✅ **Replay attack prevention** - timestamp prevents old messages from being replayed
- ✅ **Lightweight** - HMAC is fast, no public-key cryptography needed

---

## Implementation Pattern

### 1. IAgentRouter Interface (Abstraction Layer)

```csharp
// Abstraction layer - enables future distribution
public interface IAgentRouter
{
    /// <summary>
    /// Route a request to a specific agent with message signing and deadlock detection
    /// </summary>
    Task<AgentResponse> RouteAsync(
        string agentId,
        AgentRequest request,
        TimeSpan? timeout = null,
        CancellationToken ct = default);

    /// <summary>
    /// Broadcast an event to all subscribed agents
    /// </summary>
    Task BroadcastAsync(
        AgentEvent @event,
        CancellationToken ct = default);
}

// Request/Response contracts
public record AgentRequest
{
    public string AgentId { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public Dictionary<string, object> Payload { get; init; } = new();
    public string? Signature { get; init; }  // HMAC-SHA256 signature
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public string? CorrelationId { get; init; }  // Trace across agent calls
}

public record AgentResponse
{
    public string AgentId { get; init; } = string.Empty;
    public bool Success { get; init; }
    public object? Result { get; init; }
    public string? Error { get; init; }
    public double Confidence { get; init; } = 1.0;  // [0-1] confidence score
    public DateTime RespondedAt { get; init; } = DateTime.UtcNow;
}

public record AgentEvent
{
    public string EventType { get; init; } = string.Empty;
    public Dictionary<string, object> Payload { get; init; } = new();
    public string SourceAgentId { get; init; } = string.Empty;
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
```

### 2. In-Process Implementation

```csharp
// MVP Implementation: In-process router with deadlock detection
public class InProcessAgentRouter : IAgentRouter
{
    private readonly IAgentRegistry _agentRegistry;
    private readonly IDeadlockDetector _deadlockDetector;
    private readonly IReasoningTraceLogger _traceLogger;
    private readonly ILogger<InProcessAgentRouter> _logger;
    private readonly byte[] _signingKey;
    private readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(30);

    public InProcessAgentRouter(
        IAgentRegistry agentRegistry,
        IDeadlockDetector deadlockDetector,
        IReasoningTraceLogger traceLogger,
        ILogger<InProcessAgentRouter> logger,
        IOptions<SecurityOptions> securityOptions)
    {
        _agentRegistry = agentRegistry;
        _deadlockDetector = deadlockDetector;
        _traceLogger = traceLogger;
        _logger = logger;
        _signingKey = Encoding.UTF8.GetBytes(securityOptions.Value.AgentSigningKey);
    }

    public async Task<AgentResponse> RouteAsync(
        string agentId,
        AgentRequest request,
        TimeSpan? timeout = null,
        CancellationToken ct = default)
    {
        var callTimeout = timeout ?? _defaultTimeout;
        var correlationId = request.CorrelationId ?? Guid.NewGuid().ToString();

        try
        {
            // Step 1: Deadlock detection
            if (_deadlockDetector.WouldCreateCycle(agentId))
            {
                _logger.LogWarning(
                    "Circular agent dependency detected: {AgentId} -> {RequestedAgent}",
                    _deadlockDetector.CurrentAgentId,
                    agentId);

                return new AgentResponse
                {
                    AgentId = agentId,
                    Success = false,
                    Error = $"Circular dependency detected: agent {agentId} is already in the call stack",
                    Confidence = 0.0
                };
            }

            // Step 2: Message signing
            var signature = SignMessage(request);
            var signedRequest = request with { Signature = signature };

            // Step 3: Lookup agent
            var agent = _agentRegistry.GetAgent(agentId)
                ?? throw new AgentNotFoundException(agentId);

            // Step 4: Record call in deadlock detector
            using var callScope = _deadlockDetector.EnterAgentCall(agentId);

            // Step 5: Execute with timeout
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(callTimeout);

            _logger.LogInformation(
                "Agent call initiated: {AgentId}.{Action} [Correlation: {CorrelationId}]",
                agentId,
                request.Action,
                correlationId);

            var response = await agent.ExecuteAsync(signedRequest, cts.Token);

            // Step 6: Reasoning trace logging
            _traceLogger.LogAgentResponse(
                agentId: agentId,
                request: signedRequest,
                response: response,
                correlationId: correlationId,
                executionTime: DateTime.UtcNow);

            // Step 7: Contradiction detection (check against prior decisions)
            await _traceLogger.CheckForContradictionsAsync(
                agentId: agentId,
                response: response);

            _logger.LogInformation(
                "Agent call completed: {AgentId}.{Action} [Success: {Success}, Confidence: {Confidence}]",
                agentId,
                request.Action,
                response.Success,
                response.Confidence);

            return response;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogError("Agent call timed out: {AgentId}.{Action} (timeout: {Timeout})",
                agentId,
                request.Action,
                callTimeout);

            return new AgentResponse
            {
                AgentId = agentId,
                Success = false,
                Error = $"Agent call timed out after {callTimeout.TotalSeconds:F1} seconds",
                Confidence = 0.0
            };
        }
        catch (AgentNotFoundException ex)
        {
            _logger.LogWarning(ex, "Agent not found: {AgentId}", agentId);
            return new AgentResponse
            {
                AgentId = agentId,
                Success = false,
                Error = $"Agent {agentId} not found",
                Confidence = 0.0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error in agent call: {AgentId}.{Action} [Correlation: {CorrelationId}]",
                agentId,
                request.Action,
                correlationId);

            return new AgentResponse
            {
                AgentId = agentId,
                Success = false,
                Error = $"Internal agent error: {ex.Message}",
                Confidence = 0.0
            };
        }
    }

    public async Task BroadcastAsync(
        AgentEvent @event,
        CancellationToken ct = default)
    {
        var agents = _agentRegistry.GetAllAgents();

        var tasks = agents.Select(agent =>
            agent.OnEventAsync(@event, ct));

        await Task.WhenAll(tasks);

        _logger.LogInformation(
            "Broadcast event to {AgentCount} agents: {EventType}",
            agents.Count,
            @event.EventType);
    }

    private string SignMessage(AgentRequest request)
    {
        var message = $"{request.AgentId}|{request.Action}|{request.Timestamp:O}";
        using var hmac = new HMACSHA256(_signingKey);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
        return Convert.ToBase64String(hash);
    }
}
```

### 3. Deadlock Detector

```csharp
// Prevent circular agent dependencies
public interface IDeadlockDetector
{
    string CurrentAgentId { get; }
    bool WouldCreateCycle(string agentId);
    IDisposable EnterAgentCall(string agentId);
}

public class CallStackDeadlockDetector : IDeadlockDetector
{
    private readonly AsyncLocal<Stack<string>> _callStack = new();
    private readonly ILogger<CallStackDeadlockDetector> _logger;

    public string CurrentAgentId => _callStack.Value?.Peek() ?? "none";

    public CallStackDeadlockDetector(ILogger<CallStackDeadlockDetector> logger)
    {
        _logger = logger;
        _callStack.Value = new Stack<string>();
    }

    public bool WouldCreateCycle(string agentId)
    {
        var stack = _callStack.Value ?? new Stack<string>();
        return stack.Contains(agentId);
    }

    public IDisposable EnterAgentCall(string agentId)
    {
        var stack = _callStack.Value ?? new Stack<string>();
        stack.Push(agentId);

        return new CallExitScope(this, agentId, _logger);
    }

    private class CallExitScope : IDisposable
    {
        private readonly CallStackDeadlockDetector _detector;
        private readonly string _agentId;
        private readonly ILogger _logger;

        public CallExitScope(CallStackDeadlockDetector detector, string agentId, ILogger logger)
        {
            _detector = detector;
            _agentId = agentId;
            _logger = logger;
        }

        public void Dispose()
        {
            var stack = _detector._callStack.Value ?? new Stack<string>();
            if (stack.Count > 0)
            {
                var popped = stack.Pop();
                if (popped != _agentId)
                {
                    _logger.LogWarning(
                        "Deadlock detector stack mismatch: expected {Expected}, got {Actual}",
                        _agentId,
                        popped);
                }
            }
        }
    }
}
```

### 4. Reasoning Trace Logger

```csharp
// Capture full agent decision context for audit trail
public interface IReasoningTraceLogger
{
    Task LogAgentResponse(
        string agentId,
        AgentRequest request,
        AgentResponse response,
        string correlationId,
        DateTime executionTime);

    Task CheckForContradictionsAsync(
        string agentId,
        AgentResponse response);
}

public class ReasoningTraceLogger : IReasoningTraceLogger
{
    private readonly BmadServerContext _context;
    private readonly ILogger<ReasoningTraceLogger> _logger;

    public ReasoningTraceLogger(
        BmadServerContext context,
        ILogger<ReasoningTraceLogger> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task LogAgentResponse(
        string agentId,
        AgentRequest request,
        AgentResponse response,
        string correlationId,
        DateTime executionTime)
    {
        var trace = new AgentReasoningTrace
        {
            Id = Guid.NewGuid(),
            AgentId = agentId,
            CorrelationId = correlationId,
            RequestPayload = JsonSerializer.Serialize(request.Payload),
            ResponsePayload = JsonSerializer.Serialize(response.Result),
            Success = response.Success,
            Confidence = response.Confidence,
            ErrorMessage = response.Error,
            ExecutedAt = executionTime,
            CreatedAt = DateTime.UtcNow
        };

        _context.AgentReasoningTraces.Add(trace);
        await _context.SaveChangesAsync();

        if (!response.Success)
        {
            _logger.LogWarning(
                "Agent response failed: {AgentId} - {Error}",
                agentId,
                response.Error);
        }
    }

    public async Task CheckForContradictionsAsync(
        string agentId,
        AgentResponse response)
    {
        // Query prior agent responses in same workflow context
        var priorTraces = await _context.AgentReasoningTraces
            .Where(t => t.AgentId != agentId && t.CreatedAt > DateTime.UtcNow.AddHours(-1))
            .OrderByDescending(t => t.CreatedAt)
            .Take(10)
            .ToListAsync();

        // Simple contradiction detection: check for conflicting decisions
        var responseText = response.Result?.ToString() ?? string.Empty;
        foreach (var trace in priorTraces)
        {
            var priorText = trace.ResponsePayload ?? string.Empty;

            // Flag if high confidence responses differ significantly
            if (response.Confidence > 0.8 && trace.Confidence > 0.8)
            {
                if (ResponsesConflict(responseText, priorText))
                {
                    _logger.LogWarning(
                        "Potential agent contradiction detected: {AgentId} vs {PriorAgent}",
                        agentId,
                        trace.AgentId);
                }
            }
        }
    }

    private bool ResponsesConflict(string response1, string response2)
    {
        // Simple heuristic: check for negating keywords
        var negations = new[] { "don't", "cannot", "won't", "invalid", "reject", "no" };
        var affirmations = new[] { "must", "should", "do", "use", "yes", "accept" };

        var resp1Negative = negations.Any(n => response1.Contains(n, StringComparison.OrdinalIgnoreCase));
        var resp2Affirmative = affirmations.Any(a => response2.Contains(a, StringComparison.OrdinalIgnoreCase));

        return resp1Negative && resp2Affirmative;
    }
}
```

### 5. Service Registration

```csharp
// Program.cs
builder.Services
    .AddSingleton<IAgentRegistry, AgentRegistry>()
    .AddSingleton<IDeadlockDetector, CallStackDeadlockDetector>()
    .AddScoped<IReasoningTraceLogger, ReasoningTraceLogger>()
    .AddScoped<IAgentRouter, InProcessAgentRouter>();

// Configure signing key (from secrets)
builder.Services.Configure<SecurityOptions>(options =>
{
    options.AgentSigningKey = builder.Configuration["Security:AgentSigningKey"]
        ?? throw new InvalidOperationException("AgentSigningKey not configured");
});
```

---

## Phase 2: Future Message Queue Implementation

```csharp
// Future: Distributed agent router (Phase 2+)
public class ServiceBusAgentRouter : IAgentRouter
{
    private readonly ServiceBusClient _serviceBusClient;
    private readonly IDeadlockDetector _deadlockDetector;

    public async Task<AgentResponse> RouteAsync(
        string agentId,
        AgentRequest request,
        TimeSpan? timeout = null,
        CancellationToken ct = default)
    {
        // Send message to service bus topic
        var sender = _serviceBusClient.CreateSender($"agents/{agentId}");
        var message = new ServiceBusMessage(JsonSerializer.Serialize(request));
        await sender.SendMessageAsync(message, ct);

        // Wait for response (with timeout)
        // Implementation details...
    }
}

// Configuration for Phase 2
// builder.Services.AddServiceBus(options =>
// {
//     options.ConnectionString = builder.Configuration["ServiceBus:ConnectionString"];
// });
```

---

## Consequences

### Positive ✅
- **MVP speed** - in-process routing is sub-millisecond
- **Type safety** - direct C# calls with compile-time checking
- **Message integrity** - HMAC signing prevents tampering
- **Deadlock safety** - call stack tracking prevents infinite loops
- **Audit trail** - reasoning traces logged for every agent call
- **Extensible** - interface enables future message queue implementation

### Negative ⚠️
- **Single-process limitation** - can't scale to multiple servers without refactoring
- **Memory overhead** - agent state kept in-memory (not persistent)
- **Dependency coupling** - agents must be in same process
- **Complexity** - deadlock detection + message signing adds code

### Mitigations (Phase 2)
- Interface abstraction enables clean migration to message queues
- No application-level code changes required (just swap implementation)
- Can add Redis caching for agent state if needed

---

## Related Decisions

- **ADR-004:** Real-Time Communication with SignalR (complements agent routing)
- **Category 1 - D1.4:** Caching Strategy (agent registry cached in memory)
- **Category 2 - D2.5:** Session Security (tokens used to authenticate agent calls)

---

## Implementation Checklist

- [ ] Implement `IAgentRouter` interface
- [ ] Create `InProcessAgentRouter` with deadlock detection
- [ ] Implement message signing with HMAC-SHA256
- [ ] Create `IReasoningTraceLogger` for audit trail
- [ ] Add `CallStackDeadlockDetector` for circular dependency prevention
- [ ] Write tests for deadlock scenarios
- [ ] Write tests for timeout scenarios
- [ ] Write tests for message signing validation
- [ ] Document agent routing contract
- [ ] Create Phase 2 service bus implementation stub

---

## References

- [HMAC-SHA256 Documentation](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.hmacsha256)
- [Distributed tracing patterns](https://opentelemetry.io/docs/)
- [Service Bus messaging patterns](https://learn.microsoft.com/en-us/azure/service-bus-messaging/)
