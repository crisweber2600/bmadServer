# Story 10.5: Graceful Degradation Under Load

**Status:** ready-for-dev

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

---

**🎯 FINAL STORY IN EPIC 10: Error Handling & Recovery**

This story completes the comprehensive error handling and recovery system, building on all previous stories in Epic 10. After implementation, Epic 10 will provide complete resilience infrastructure for bmadServer.

**Epic 10 Progress:** 5/5 stories ready for development
- ✅ Story 10.1: Graceful Error Handling (ProblemDetails, logging, metrics)
- ✅ Story 10.2: Connection Recovery & Retry (SignalR reconnection, exponential backoff)
- ✅ Story 10.3: Workflow Recovery After Failure (Polly retry, checkpoints, auto-recovery)
- ✅ Story 10.4: Conversation Stall Recovery (timeout detection, auto-retry, circular detection)
- ✅ Story 10.5: Graceful Degradation Under Load (this story - capacity, queueing, failover)

---

## Story

As an operator, I want the system to degrade gracefully under heavy load, so that core functionality remains available.

## Acceptance Criteria

**Given** system load approaches capacity  
**When** concurrent users exceed 80% of limit (20 of 25)  
**Then** new workflow starts are queued  
**And** existing workflows continue normally  
**And** users see: "High demand - new workflows may be delayed"

**Given** the queue has waiting workflows  
**When** capacity becomes available  
**Then** queued workflows start in order  
**And** users are notified: "Your workflow is starting"

**Given** non-essential features exist  
**When** under extreme load  
**Then** features like typing indicators, presence updates are disabled first  
**And** core workflow execution is preserved

**Given** a provider (LLM API) is slow or down  
**When** the issue is detected  
**Then** the system switches to backup provider if configured  
**And** users see: "Using alternative provider - responses may vary slightly"

**Given** degradation occurs  
**When** I check the status page  
**Then** I see current system status and any known issues  
**And** estimated time to resolution (if known)

## Tasks / Subtasks

- [ ] Task 1: Concurrent User & Workflow Capacity Tracking (AC: 1, 2)
  - [ ] Add concurrent user counter with thread-safe increment/decrement
  - [ ] Add active workflow counter to track concurrent workflows
  - [ ] Create capacity monitoring service (20/25 user threshold = 80%)
  - [ ] Implement workflow queue data structure (FIFO with priority support)
  - [ ] Add queue position tracking for users
  - [ ] Create SignalR event "CAPACITY_WARNING" for 80% threshold
  - [ ] Add Prometheus metrics for capacity tracking
- [ ] Task 2: Workflow Queue Management System (AC: 1, 2)
  - [ ] Create WorkflowQueueService for queue operations
  - [ ] Implement EnqueueWorkflowAsync(userId, workflowRequest) method
  - [ ] Implement DequeueWorkflowAsync() with FIFO ordering
  - [ ] Add background service to process queue when capacity available
  - [ ] Send "WORKFLOW_QUEUED" SignalR notification with position
  - [ ] Send "WORKFLOW_STARTING" notification when dequeued
  - [ ] Add queue timeout (5 minutes) with cancellation notification
  - [ ] Store queue state in PostgreSQL for persistence across restarts
- [ ] Task 3: Non-Essential Feature Degradation (AC: 3)
  - [ ] Create feature flag system with degradation levels
  - [ ] Identify non-essential features: typing indicators, presence, read receipts
  - [ ] Add degradation triggers at 90% capacity (23/25 users)
  - [ ] Disable typing indicators when degraded
  - [ ] Disable presence updates when degraded
  - [ ] Keep core features active: workflow execution, chat messaging, session recovery
  - [ ] Add "DEGRADED_MODE_ACTIVE" SignalR broadcast
  - [ ] Test feature degradation doesn't affect core workflows
- [ ] Task 4: LLM Provider Failover and Circuit Breaker (AC: 4)
  - [ ] Create LLM provider abstraction interface (ILlmProvider)
  - [ ] Implement primary provider client (OpenAI, Azure OpenAI, etc.)
  - [ ] Implement backup provider client (configurable alternative)
  - [ ] Add circuit breaker pattern using Polly for provider health
  - [ ] Detect slow/down providers (timeout > 10s or 3 consecutive failures)
  - [ ] Implement automatic failover to backup provider
  - [ ] Send "PROVIDER_SWITCHED" notification to user
  - [ ] Add provider health metrics to Prometheus
  - [ ] Test failover with simulated provider outages
- [ ] Task 5: System Status Page and Health Monitoring (AC: 5)
  - [ ] Create GET `/api/v1/system/status` endpoint (public)
  - [ ] Return system health: Healthy, Degraded, or Down
  - [ ] Include capacity metrics: active users, active workflows, queue length
  - [ ] Include feature status: which features are degraded
  - [ ] Include provider status: primary/backup status
  - [ ] Add estimated time to resolution (if known from incidents)
  - [ ] Create simple status page UI component
  - [ ] Add health check endpoint for monitoring tools
- [ ] Task 6: Load Testing and Capacity Validation
  - [ ] Create load test script to simulate 25+ concurrent users
  - [ ] Test workflow queueing at 80% capacity (20 users)
  - [ ] Test feature degradation at 90% capacity (23 users)
  - [ ] Test provider failover under load
  - [ ] Validate queue processing when capacity becomes available
  - [ ] Measure performance impact of capacity monitoring
  - [ ] Document load testing results
- [ ] Task 7: Testing and Validation
  - [ ] Unit tests for capacity tracking logic
  - [ ] Unit tests for queue operations (enqueue, dequeue, timeout)
  - [ ] Unit tests for feature degradation triggers
  - [ ] Unit tests for provider failover logic
  - [ ] Integration tests for workflow queueing flow
  - [ ] Integration tests for status page endpoint
  - [ ] BDD tests for all acceptance criteria
  - [ ] Manual testing with multiple concurrent users
  - [ ] Performance testing with load simulation

## Dev Notes

### 🎯 CRITICAL IMPLEMENTATION REQUIREMENTS

#### Epic 10 Context: Error Handling & Recovery - FINAL STORY (5 of 5)

This is the **final story** in Epic 10, completing the comprehensive error handling and recovery system. It builds on ALL previous stories:

- **Story 10.1 (Graceful Error Handling):** ProblemDetails RFC 7807, structured logging with correlation IDs, error metrics in Prometheus/Grafana, user-friendly error messages
- **Story 10.2 (Connection Recovery):** SignalR reconnection with exponential backoff (0s, 2s, 10s, 30s), session restoration via ChatHub, message queueing during disconnection
- **Story 10.3 (Workflow Recovery):** Polly retry policies for transient failures, checkpoint restoration, automatic recovery after server restart, Failed workflow state
- **Story 10.4 (Conversation Stall Recovery):** 30-second timeout detection, 60-second auto-retry, circular conversation detection, step context reset

**Key Integration Points:**
- Use Polly resilience library (already integrated in Stories 10.2 and 10.3) for circuit breaker pattern
- Leverage SignalR notifications (Stories 10.2, 10.3, 10.4) for queue/degradation/provider alerts
- Build on Prometheus/Grafana monitoring (Story 10.1) for capacity and health metrics
- Follow correlation ID patterns (Story 10.1) for capacity tracking events

### 🏗️ Architecture Requirements from architecture.md

**Scalability Targets (NFR10, NFR11):**
- **MVP Capacity:** 25 concurrent users, 10 concurrent workflows
- **Queue Threshold:** 80% of capacity = 20 of 25 users triggers queueing
- **Load Testing Baseline:** "500 req/sec, 100 WebSocket, 10 concurrent ops"
- **Scaling Strategy:** "Single Server → Docker Swarm → Kubernetes"

**Built-in ASP.NET Core Rate Limiting:**
```csharp
// From architecture.md - Use System.Threading.RateLimiting (built-in)
// Already configured for per-user rate limiting in Story 11.1 (Epic 11)
// This story focuses on CAPACITY-based limiting, not rate limiting
```

**Existing Infrastructure to Leverage:**
- **SignalR:** Already configured in Epic 3 with ChatHub
- **Prometheus Metrics:** Configured in Epic 1 (Story 1.5 - cancelled, but basics in place)
- **PostgreSQL:** State persistence via JSONB (Epic 9 patterns)
- **Polly Resilience:** Already added in Stories 10.2 and 10.3

### 📊 Capacity Tracking Implementation Pattern

**Concurrent User Tracking:**
```csharp
// Create thread-safe counter service
// src/bmadServer.ApiService/Services/CapacityMonitoringService.cs

public class CapacityMonitoringService
{
    private int _activeUsers = 0;
    private int _activeWorkflows = 0;
    private readonly int _maxUsers = 25; // From NFR10
    private readonly int _maxWorkflows = 10; // From NFR10
    private readonly double _warningThreshold = 0.8; // 80%
    
    public int IncrementUsers() => Interlocked.Increment(ref _activeUsers);
    public int DecrementUsers() => Interlocked.Decrement(ref _activeUsers);
    
    public bool IsAtCapacity() => _activeUsers >= _maxUsers;
    public bool IsNearCapacity() => _activeUsers >= (_maxUsers * _warningThreshold);
    public int GetQueuePosition() { /* FIFO logic */ }
}
```

**Integration Points:**
- Track users in ChatHub.OnConnectedAsync() / OnDisconnectedAsync()
- Track workflows in WorkflowExecutionService.StartWorkflowAsync()
- Check capacity before allowing new workflow starts

### 🔄 Workflow Queue Architecture

**Queue Design:**
```csharp
// src/bmadServer.ApiService/Data/Entities/WorkflowQueue.cs
public class WorkflowQueue
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string WorkflowName { get; set; }
    public string RequestPayload { get; set; } // JSON
    public DateTime QueuedAt { get; set; }
    public int Position { get; set; }
    public WorkflowQueueStatus Status { get; set; } // Queued, Processing, TimedOut, Cancelled
}

public enum WorkflowQueueStatus
{
    Queued,
    Processing,
    Started,
    TimedOut,
    Cancelled
}
```

**Queue Processing Pattern:**
```csharp
// Background service pattern (similar to Epic 4 workflow execution)
// src/bmadServer.ApiService/Services/WorkflowQueueProcessorService.cs

public class WorkflowQueueProcessorService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (!_capacityMonitor.IsAtCapacity())
            {
                var nextInQueue = await _queueService.DequeueNextAsync();
                if (nextInQueue != null)
                {
                    await _workflowService.StartWorkflowAsync(nextInQueue);
                    await _hubContext.Clients.User(nextInQueue.UserId)
                        .SendAsync("WORKFLOW_STARTING", nextInQueue.WorkflowName);
                }
            }
            await Task.Delay(1000, stoppingToken); // Poll every second
        }
    }
}
```

### 🎚️ Feature Degradation Strategy

**Non-Essential Features (from acceptance criteria):**
1. **Typing Indicators:** SignalR "UserTyping" events
2. **Presence Updates:** User online/offline status broadcasts
3. **Read Receipts:** Message read confirmations

**Essential Features (ALWAYS ON):**
1. **Workflow Execution:** Core orchestration
2. **Chat Messaging:** Send/receive messages
3. **Session Recovery:** Connection recovery from Story 10.2
4. **Error Handling:** All error handling from Story 10.1

**Degradation Trigger:**
```csharp
// Trigger at 90% capacity (23 of 25 users)
public bool ShouldDegrade() => _activeUsers >= (_maxUsers * 0.9);

// Feature flag system
public class FeatureFlagService
{
    private bool _typingIndicatorsEnabled = true;
    private bool _presenceUpdatesEnabled = true;
    
    public void EnterDegradedMode()
    {
        _typingIndicatorsEnabled = false;
        _presenceUpdatesEnabled = false;
        _logger.LogWarning("System entered degraded mode - non-essential features disabled");
    }
    
    public void ExitDegradedMode()
    {
        _typingIndicatorsEnabled = true;
        _presenceUpdatesEnabled = true;
        _logger.LogInformation("System exited degraded mode - all features re-enabled");
    }
}
```

### 🔌 LLM Provider Failover with Circuit Breaker

**Provider Abstraction:**
```csharp
// src/bmadServer.ApiService/Services/LLM/ILlmProvider.cs
public interface ILlmProvider
{
    string ProviderName { get; }
    Task<LlmResponse> GenerateAsync(LlmRequest request, CancellationToken ct);
    Task<bool> HealthCheckAsync();
}

// Primary implementation: OpenAI
// Backup implementation: Azure OpenAI or other provider
```

**Circuit Breaker Pattern with Polly:**
```csharp
// Use Polly (already added in Stories 10.2 and 10.3)
// NuGet: Polly 8.0+

public class LlmProviderService
{
    private readonly ILlmProvider _primaryProvider;
    private readonly ILlmProvider _backupProvider;
    private readonly AsyncCircuitBreakerPolicy _circuitBreaker;
    
    public LlmProviderService()
    {
        _circuitBreaker = Policy
            .Handle<HttpRequestException>()
            .Or<TimeoutException>()
            .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking: 3,
                durationOfBreak: TimeSpan.FromMinutes(1),
                onBreak: (ex, duration) => 
                {
                    _logger.LogWarning("Circuit breaker opened - switching to backup provider");
                },
                onReset: () => 
                {
                    _logger.LogInformation("Circuit breaker reset - returning to primary provider");
                }
            );
    }
    
    public async Task<LlmResponse> GenerateAsync(LlmRequest request)
    {
        try
        {
            return await _circuitBreaker.ExecuteAsync(async () => 
                await _primaryProvider.GenerateAsync(request));
        }
        catch (BrokenCircuitException)
        {
            // Use backup provider
            await _hubContext.Clients.User(request.UserId)
                .SendAsync("PROVIDER_SWITCHED", new {
                    Message = "Using alternative provider - responses may vary slightly",
                    Provider = _backupProvider.ProviderName
                });
            return await _backupProvider.GenerateAsync(request);
        }
    }
}
```

**Provider Detection Criteria (from AC 4):**
- **Slow:** Response time > 10 seconds
- **Down:** 3 consecutive failures or HTTP 5xx errors
- **Circuit Breaker:** Opens after 3 failures, stays open for 1 minute

### 📈 System Status Page

**Status API Endpoint:**
```csharp
// src/bmadServer.ApiService/Controllers/SystemController.cs
[HttpGet("api/v1/system/status")]
[AllowAnonymous] // Public endpoint
public async Task<ActionResult<SystemStatus>> GetSystemStatus()
{
    var status = new SystemStatus
    {
        Health = _capacityMonitor.IsAtCapacity() ? "Degraded" : "Healthy",
        Timestamp = DateTime.UtcNow,
        Capacity = new CapacityMetrics
        {
            ActiveUsers = _capacityMonitor.GetActiveUsers(),
            MaxUsers = 25,
            ActiveWorkflows = _capacityMonitor.GetActiveWorkflows(),
            MaxWorkflows = 10,
            QueueLength = await _queueService.GetQueueLengthAsync()
        },
        Features = new FeatureStatus
        {
            TypingIndicators = _featureFlags.AreTypingIndicatorsEnabled(),
            PresenceUpdates = _featureFlags.ArePresenceUpdatesEnabled()
        },
        Providers = new ProviderStatus
        {
            Primary = await _primaryProvider.HealthCheckAsync() ? "Healthy" : "Down",
            Backup = await _backupProvider.HealthCheckAsync() ? "Healthy" : "Down",
            ActiveProvider = _circuitBreaker.CircuitState == CircuitState.Open 
                ? "Backup" : "Primary"
        }
    };
    return Ok(status);
}
```

### 📊 Prometheus Metrics

**Add Capacity Metrics:**
```csharp
// Follow pattern from Story 10.1 error metrics
// Use .NET built-in System.Diagnostics.Metrics

public class CapacityMetrics
{
    private readonly Counter<int> _activeUsersCounter;
    private readonly Counter<int> _activeWorkflowsCounter;
    private readonly Counter<int> _queuedWorkflowsCounter;
    private readonly Histogram<double> _queueWaitTimeHistogram;
    
    public CapacityMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("bmadServer.Capacity");
        
        _activeUsersCounter = meter.CreateCounter<int>(
            "active_users", 
            description: "Number of active concurrent users");
            
        _activeWorkflowsCounter = meter.CreateCounter<int>(
            "active_workflows", 
            description: "Number of active concurrent workflows");
            
        _queuedWorkflowsCounter = meter.CreateCounter<int>(
            "queued_workflows", 
            description: "Number of workflows waiting in queue");
            
        _queueWaitTimeHistogram = meter.CreateHistogram<double>(
            "queue_wait_time_seconds", 
            description: "Time workflows spend waiting in queue");
    }
}
```

### 🧪 Load Testing Strategy

**Testing Requirements:**
- Simulate 25+ concurrent SignalR connections
- Start 10+ concurrent workflows
- Verify queueing triggers at 20 users (80%)
- Verify degradation triggers at 23 users (90%)
- Test provider failover under load

**Load Testing Tools:**
```bash
# Use existing tools from architecture.md load testing baseline
# Consider: k6, JMeter, or custom C# console app with SignalR client

# Example k6 script for SignalR load testing:
# test-scripts/load-test-capacity.js
```

### 🔗 Integration with Existing Systems

**ChatHub Integration (Epic 3):**
```csharp
// src/bmadServer.ApiService/Hubs/ChatHub.cs
public override async Task OnConnectedAsync()
{
    _capacityMonitor.IncrementUsers();
    
    if (_capacityMonitor.IsNearCapacity())
    {
        await Clients.All.SendAsync("CAPACITY_WARNING", new {
            Message = "High demand - new workflows may be delayed",
            ActiveUsers = _capacityMonitor.GetActiveUsers(),
            MaxUsers = 25
        });
    }
    
    // Existing session recovery logic from Story 10.2...
}

public override async Task OnDisconnectedAsync(Exception exception)
{
    _capacityMonitor.DecrementUsers();
    
    // Check if we can start queued workflows
    await _queueProcessor.ProcessQueueAsync();
}
```

**WorkflowExecutionService Integration (Epic 4):**
```csharp
// src/bmadServer.ApiService/Services/WorkflowExecutionService.cs
public async Task<WorkflowInstance> StartWorkflowAsync(...)
{
    // Check capacity before starting
    if (_capacityMonitor.IsAtCapacity())
    {
        // Queue the workflow instead
        var queueEntry = await _queueService.EnqueueAsync(userId, workflowName, request);
        
        await _hubContext.Clients.User(userId).SendAsync("WORKFLOW_QUEUED", new {
            WorkflowName = workflowName,
            QueuePosition = queueEntry.Position,
            Message = "High demand - new workflows may be delayed"
        });
        
        return null; // Workflow not started yet
    }
    
    // Start workflow normally
    _capacityMonitor.IncrementWorkflows();
    var instance = await CreateWorkflowInstanceAsync(...);
    // ... existing workflow start logic
}
```

### 📝 Database Migrations

**Create WorkflowQueue Table:**
```csharp
// Add migration for workflow queue storage
// src/bmadServer.ApiService/Data/Migrations/AddWorkflowQueueTable.cs

public partial class AddWorkflowQueueTable : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "WorkflowQueues",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                UserId = table.Column<Guid>(nullable: false),
                WorkflowName = table.Column<string>(maxLength: 200, nullable: false),
                RequestPayload = table.Column<string>(type: "jsonb", nullable: false),
                QueuedAt = table.Column<DateTime>(nullable: false),
                Position = table.Column<int>(nullable: false),
                Status = table.Column<int>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_WorkflowQueues", x => x.Id);
                table.ForeignKey(
                    name: "FK_WorkflowQueues_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });
            
        migrationBuilder.CreateIndex(
            name: "IX_WorkflowQueues_Position",
            table: "WorkflowQueues",
            column: "Position");
            
        migrationBuilder.CreateIndex(
            name: "IX_WorkflowQueues_Status",
            table: "WorkflowQueues",
            column: "Status");
    }
}
```

### 🎯 Files to Create/Modify

**New Files to Create:**
1. `src/bmadServer.ApiService/Services/CapacityMonitoringService.cs` - Concurrent user/workflow tracking
2. `src/bmadServer.ApiService/Services/WorkflowQueueService.cs` - Queue management (enqueue, dequeue)
3. `src/bmadServer.ApiService/Services/WorkflowQueueProcessorService.cs` - Background queue processor
4. `src/bmadServer.ApiService/Services/FeatureFlagService.cs` - Degradation control
5. `src/bmadServer.ApiService/Services/LLM/ILlmProvider.cs` - Provider abstraction
6. `src/bmadServer.ApiService/Services/LLM/OpenAiLlmProvider.cs` - Primary provider implementation
7. `src/bmadServer.ApiService/Services/LLM/LlmProviderService.cs` - Failover orchestration with circuit breaker
8. `src/bmadServer.ApiService/Data/Entities/WorkflowQueue.cs` - Queue entity
9. `src/bmadServer.ApiService/Controllers/SystemController.cs` - Status endpoint
10. `src/bmadServer.ApiService/Data/Migrations/AddWorkflowQueueTable.cs` - Migration
11. `src/bmadServer.ApiService.Tests/Unit/CapacityMonitoringServiceTests.cs` - Unit tests
12. `src/bmadServer.ApiService.Tests/Unit/WorkflowQueueServiceTests.cs` - Unit tests
13. `src/bmadServer.ApiService.Tests/Unit/FeatureFlagServiceTests.cs` - Unit tests
14. `src/bmadServer.ApiService.Tests/Unit/LlmProviderServiceTests.cs` - Unit tests
15. `src/bmadServer.ApiService.Tests/Integration/CapacityIntegrationTests.cs` - Integration tests
16. `src/frontend/src/components/SystemStatusBanner.tsx` - Status UI component

**Existing Files to Modify:**
1. `src/bmadServer.ApiService/Hubs/ChatHub.cs` - Add capacity tracking on connect/disconnect
2. `src/bmadServer.ApiService/Services/WorkflowExecutionService.cs` - Add capacity checks and queueing
3. `src/bmadServer.ApiService/Program.cs` - Register new services (CapacityMonitor, QueueProcessor, LlmProvider, FeatureFlags)
4. `src/bmadServer.ApiService/Data/ApplicationDbContext.cs` - Add WorkflowQueue DbSet
5. `src/frontend/src/components/ResponsiveChat.tsx` - Handle CAPACITY_WARNING, WORKFLOW_QUEUED, WORKFLOW_STARTING, PROVIDER_SWITCHED events

### ⚠️ Common Pitfalls to Avoid

1. **Thread Safety:** Use Interlocked operations for counters, not simple increment/decrement
2. **Race Conditions:** Queue processing must handle concurrent dequeue attempts
3. **Memory Leaks:** Queue entries must have timeout and cleanup logic
4. **Cascading Failures:** Circuit breaker prevents provider failures from cascading
5. **Testing:** Load testing is CRITICAL - don't skip it!
6. **Metrics Overhead:** Keep capacity monitoring lightweight (< 1ms overhead)

### 🎓 Key Learnings from Previous Stories

**From Story 10.1 (Error Handling):**
- Use structured logging with correlation IDs for all capacity events
- Follow ProblemDetails RFC 7807 for error responses (e.g., queue timeout errors)
- Add Prometheus metrics for monitoring

**From Story 10.2 (Connection Recovery):**
- SignalR notifications pattern for queue/capacity alerts
- Use Polly for resilience policies (apply to provider failover)

**From Story 10.3 (Workflow Recovery):**
- Checkpoint pattern for queue state persistence
- Background service pattern for queue processor
- Use Polly for transient failure retry

**From Story 10.4 (Stall Recovery):**
- Timeout detection patterns (apply to queue timeouts)
- User notification patterns for stalled operations

### 🚀 Success Criteria

Story is complete when:
- ✅ System queues workflows at 80% capacity (20 of 25 users)
- ✅ Queue processes workflows when capacity becomes available
- ✅ Non-essential features degrade at 90% capacity
- ✅ Provider failover works with circuit breaker
- ✅ Status page shows accurate system health
- ✅ All unit and integration tests pass
- ✅ Load testing confirms capacity handling up to 30 concurrent users
- ✅ Prometheus metrics show capacity and queue data
- ✅ Epic 10 is COMPLETE!


---

## Aspire Development Standards

### PostgreSQL Connection Pattern

This story uses PostgreSQL configured in Story 1.2 via Aspire:
- Connection string automatically injected from Aspire AppHost
- Pattern: `builder.AddServiceDefaults();` (inherits PostgreSQL reference)
- See Story 1.2 for AppHost configuration pattern

### Project-Wide Standards

This story follows the Aspire-first development pattern:
- **Reference:** [PROJECT-WIDE-RULES.md](../../../PROJECT-WIDE-RULES.md)
- **Primary Documentation:** https://aspire.dev
- **GitHub:** https://github.com/microsoft/aspire

---


### Future: Redis Caching Pattern

When caching layer needed in Phase 2:
- Command: `aspire add Redis.Distributed`
- Pattern: DI injection via IConnectionMultiplexer
- Also available: Redis backplane for SignalR scaling
- Reference: https://aspire.dev Redis integration

## References
- **Aspire Rules:** [PROJECT-WIDE-RULES.md](../../../PROJECT-WIDE-RULES.md)
- **Aspire Docs:** https://aspire.dev

- Source: [epics.md - Story 10.5](../planning-artifacts/epics.md)
- Architecture: [architecture.md](../planning-artifacts/architecture.md)
- PRD: [prd.md](../planning-artifacts/prd.md)
