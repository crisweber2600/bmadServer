# Distributed Messaging Strategy for bmadServer

**Document Type:** Pre-Planning Architecture  
**Status:** PHASE 2 Pre-Planning (Not MVP)  
**Target Phase:** Phase 2 - Post-MVP Distribution  
**Last Updated:** 2026-01-24  
**Owner:** Development Team  

---

## 📋 EXECUTIVE SUMMARY

This document outlines the distributed messaging strategy for bmadServer, planned for **Phase 2** implementation after MVP completion. Messaging will be introduced via Aspire component when multi-instance deployment or high-volume async processing becomes necessary.

### When to Implement Messaging

**Phase 1 (MVP - Current):** ❌ NOT NEEDED
- Single-process, in-process messaging sufficient
- All agents run within same application domain
- No cross-server communication required
- Synchronous workflows adequate for single instance

**Phase 2 (Post-MVP - When Needed):**
- ✅ Multiple microservice instances required
- ✅ Agent-to-agent communication across processes
- ✅ High-volume webhook/event processing
- ✅ Decoupled components for independent scaling
- ✅ Failure isolation between services

### Decision: MVP does NOT include Message Queue

**Rationale:**
- Added operational complexity (new service to manage)
- Aspire PostgreSQL with event sourcing sufficient for Phase 1
- ServiceCollection dependency injection handles in-process messaging
- Can be added non-disruptively in Phase 2
- Aspire component (`aspire add RabbitMq.Aspire`) makes future migration straightforward

---

## 🎯 PHASE 1 (MVP): IN-PROCESS MESSAGING

### Pattern: ServiceCollection-Based Message Pub/Sub

For MVP, implement lightweight in-process event bus using `IHostApplicationLifetime`:

```csharp
// In bmadServer.ApiService/Services/EventBus.cs
public interface IEventBus
{
    void Publish<T>(T evt) where T : DomainEvent;
    void Subscribe<T>(IEventHandler<T> handler) where T : DomainEvent;
}

public class InProcessEventBus : IEventBus
{
    private readonly Dictionary<Type, List<Delegate>> _handlers = new();
    
    public void Publish<T>(T evt) where T : DomainEvent
    {
        var type = typeof(T);
        if (_handlers.TryGetValue(type, out var handlers))
        {
            foreach (var handler in handlers.Cast<Action<T>>())
            {
                handler(evt);
            }
        }
    }
    
    public void Subscribe<T>(IEventHandler<T> handler) where T : DomainEvent
    {
        var type = typeof(T);
        if (!_handlers.ContainsKey(type))
            _handlers[type] = new();
        
        _handlers[type].Add(new Action<T>(handler.Handle));
    }
}

// In Program.cs
builder.Services.AddSingleton<IEventBus, InProcessEventBus>();
```

### MVP Use Cases with In-Process Messaging

#### Epic 5: Multi-Agent Collaboration
**Scenario:** One agent completes analysis, triggers another agent

```csharp
// In MarketResearchAgent
public class MarketResearchCompletedEvent : DomainEvent
{
    public string WorkflowId { get; set; }
    public MarketAnalysis Analysis { get; set; }
}

// Agent publishes event
await _eventBus.PublishAsync(new MarketResearchCompletedEvent 
{
    WorkflowId = workflow.Id,
    Analysis = analysis
});

// Another agent subscribes
_eventBus.Subscribe<MarketResearchCompletedEvent>(
    async evt => await ProcessMarketAnalysisAsync(evt.Analysis)
);
```

**Why This Works for MVP:**
- All agents in same process
- Events delivered synchronously (reliable)
- No network latency
- Simple to debug and test

#### Epic 7: Collaboration & Multi-User Checkpoints
**Scenario:** User saves checkpoint, other connected users notified in real-time

```csharp
// In CheckpointService
public class CheckpointSavedEvent : DomainEvent
{
    public string WorkflowId { get; set; }
    public string UserId { get; set; }
    public CheckpointData Data { get; set; }
}

// Publish to event bus
await _eventBus.PublishAsync(new CheckpointSavedEvent 
{
    WorkflowId = workflowId,
    UserId = currentUser.Id,
    Data = checkpoint
});

// SignalR Hub subscribes via constructor injection
public class CollaborationHub : Hub
{
    private readonly IEventBus _eventBus;
    
    public CollaborationHub(IEventBus eventBus)
    {
        _eventBus = eventBus;
        _eventBus.Subscribe<CheckpointSavedEvent>(OnCheckpointSaved);
    }
    
    private async Task OnCheckpointSaved(CheckpointSavedEvent evt)
    {
        // Broadcast to clients
        await Clients.Group(evt.WorkflowId)
            .SendAsync("CheckpointUpdated", evt.Data);
    }
}
```

**Why This Works for MVP:**
- SignalR and event bus in same process
- Real-time updates without message queue
- Transactional consistency (database + event = atomic)

#### Epic 13: Webhooks (Initial Approach)
**Scenario:** Event triggers webhook delivery

```csharp
// In DatabaseQueue approach (not RabbitMQ yet)
public class WebhookQueueItem
{
    public int Id { get; set; }
    public string Url { get; set; }
    public string Payload { get; set; }
    public int RetryCount { get; set; }
    public WebhookStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

// In WebhookService
public async Task EnqueueWebhookAsync(string url, object payload)
{
    var item = new WebhookQueueItem
    {
        Url = url,
        Payload = JsonSerializer.Serialize(payload),
        Status = WebhookStatus.Pending,
        CreatedAt = DateTime.UtcNow
    };
    
    _db.WebhookQueue.Add(item);
    await _db.SaveChangesAsync();
}

// BackgroundService polls database
public class WebhookDeliveryService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var pending = await _db.WebhookQueue
                .Where(x => x.Status == WebhookStatus.Pending)
                .Take(10)
                .ToListAsync();
            
            foreach (var item in pending)
            {
                await DeliverWebhookAsync(item, stoppingToken);
            }
            
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
```

**Why This Works for MVP:**
- Persisted in PostgreSQL (reliable)
- Simple background service pulls and delivers
- Retry logic built-in
- No external dependency

---

## 🚀 PHASE 2: DISTRIBUTED MESSAGING

### When to Add RabbitMQ/Kafka

**Trigger Points:**
1. Agents deployed to separate microservices
2. Webhook throughput exceeds database queue capacity (>1000/min)
3. Cross-service communication becomes bottleneck
4. Event replay/audit trail becomes critical requirement

### Adding RabbitMQ to bmadServer (Phase 2)

**Step 1: Add RabbitMQ via Aspire CLI**
```bash
cd /Users/cris/bmadServer/src
aspire add RabbitMq.Aspire
```

This command:
- Installs `Aspire.Hosting.RabbitMq` NuGet package
- Installs `RabbitMQ.Client` (client library)
- Creates RabbitMQ container orchestration in AppHost

**Step 2: Configure in AppHost**
```csharp
// In bmadServer.AppHost/Program.cs
var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .AddDatabase("bmadserver", "bmadserver_dev");

var rabbitmq = builder.AddRabbitMq("rabbitmq")
    .WithManagementPlugin();  // Enable management UI (http://localhost:15672)

var api = builder.AddProject<Projects.bmadServer_ApiService>("api")
    .WithReference(postgres)
    .WithReference(rabbitmq);

builder.Build().Run();
```

**Step 3: Configure in ApiService**
```csharp
// In bmadServer.ApiService/Program.cs
builder.AddServiceDefaults();

// Add RabbitMQ messaging
builder.Services.AddRabbitMq("rabbitmq");

// Register message handlers
builder.Services.AddScoped<IMessageHandler<MarketResearchCompletedEvent>, 
    MarketResearchCompletedHandler>();
```

**Step 4: Replace In-Process EventBus**
```csharp
// Create RabbitMQ-based event bus
public class DistributedEventBus : IEventBus
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    
    public DistributedEventBus(IConnection connection)
    {
        _connection = connection;
        _channel = connection.CreateModel();
    }
    
    public async Task PublishAsync<T>(T evt) where T : DomainEvent
    {
        var exchangeName = typeof(T).Name;
        _channel.ExchangeDeclare(exchangeName, ExchangeType.Fanout);
        
        var json = JsonSerializer.Serialize(evt);
        var body = Encoding.UTF8.GetBytes(json);
        
        _channel.BasicPublish(exchangeName, "", null, body);
        await Task.CompletedTask;
    }
    
    public async Task SubscribeAsync<T>(IAsyncEventHandler<T> handler) 
        where T : DomainEvent
    {
        var exchangeName = typeof(T).Name;
        var queueName = $"{typeof(T).Name}.{handler.GetType().Name}";
        
        _channel.ExchangeDeclare(exchangeName, ExchangeType.Fanout);
        _channel.QueueDeclare(queueName, durable: true);
        _channel.QueueBind(queueName, exchangeName, "");
        
        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (model, ea) =>
        {
            var json = Encoding.UTF8.GetString(ea.Body.ToArray());
            var evt = JsonSerializer.Deserialize<T>(json);
            await handler.HandleAsync(evt);
            _channel.BasicAck(ea.DeliveryTag, false);
        };
        
        _channel.BasicConsume(queueName, false, consumer);
        await Task.CompletedTask;
    }
}
```

---

## 📊 PHASE 2 DISTRIBUTED MESSAGING PATTERNS

### Epic 5: Multi-Agent Collaboration (Distributed)

**Scenario:** Agents deployed as separate microservices

```csharp
// MarketResearchAgent (separate service/process)
public class MarketResearchAgent
{
    private readonly IEventBus _eventBus;
    
    public async Task AnalyzeMarketAsync(string workflowId)
    {
        var analysis = await PerformAnalysis();
        
        // Publish to distributed message bus
        await _eventBus.PublishAsync(new MarketResearchCompletedEvent
        {
            WorkflowId = workflowId,
            Analysis = analysis
        });
    }
}

// CompetitiveAnalysisAgent (different service/process)
public class CompetitiveAnalysisAgent
{
    private readonly IEventBus _eventBus;
    
    public CompetitiveAnalysisAgent(IEventBus eventBus)
    {
        _eventBus = eventBus;
        // Subscribe to market research completion
        _eventBus.SubscribeAsync<MarketResearchCompletedEvent>(HandleMarketResearch);
    }
    
    private async Task HandleMarketResearch(MarketResearchCompletedEvent evt)
    {
        // Start competitive analysis when market research done
        var analysis = await AnalyzeCompetitors(evt.Analysis);
        
        await _eventBus.PublishAsync(new CompetitiveAnalysisCompletedEvent
        {
            WorkflowId = evt.WorkflowId,
            Analysis = analysis
        });
    }
}
```

**Architecture:**
```
┌─────────────────────┐         ┌──────────────────────┐
│ Market Research     │         │ Competitive Analysis │
│ Agent (Service 1)   │ ──RabbitMQ──> Agent (Service 2) │
└─────────────────────┘         └──────────────────────┘
         │                                │
         └────────────┬───────────────────┘
                      │
                ┌─────▼────────┐
                │ Workflow     │
                │ Engine       │
                │ Orchestrator │
                └──────────────┘
```

### Epic 7: Collaboration Checkpoints (Distributed)

**Scenario:** Multiple users on different servers, need real-time sync

```csharp
// On Server 1: User saves checkpoint
public class CheckpointService
{
    private readonly IEventBus _eventBus;
    
    public async Task SaveCheckpointAsync(string workflowId, CheckpointData data)
    {
        await _db.Checkpoints.AddAsync(new Checkpoint { /* ... */ });
        await _db.SaveChangesAsync();
        
        // Broadcast to all servers
        await _eventBus.PublishAsync(new CheckpointSavedEvent
        {
            WorkflowId = workflowId,
            UserId = currentUser.Id,
            Data = data
        });
    }
}

// On All Servers: SignalR hubs receive event
public class CollaborationHub : Hub
{
    private readonly IEventBus _eventBus;
    
    public CollaborationHub(IEventBus eventBus)
    {
        _eventBus = eventBus;
        _eventBus.SubscribeAsync<CheckpointSavedEvent>(async evt =>
        {
            // Event received on all servers
            await Clients.Group(evt.WorkflowId)
                .SendAsync("CheckpointUpdated", evt.Data);
        });
    }
}
```

**Benefits Over MVP:**
- Checkpoints synced across all servers
- No server affinity required
- Users can reconnect to different server

### Epic 13: High-Volume Webhooks (Distributed)

**Scenario:** Replace database queue with RabbitMQ for high-volume webhooks

```csharp
// Publisher: Enqueue webhook to RabbitMQ
public class WebhookService
{
    private readonly IEventBus _eventBus;
    
    public async Task TriggerWebhookAsync(string url, object payload)
    {
        await _eventBus.PublishAsync(new WebhookTriggeredEvent
        {
            Url = url,
            Payload = JsonSerializer.Serialize(payload),
            Timestamp = DateTime.UtcNow
        });
    }
}

// Subscriber: Dedicated webhook delivery service(s)
public class WebhookDeliveryService
{
    private readonly IEventBus _eventBus;
    
    public WebhookDeliveryService(IEventBus eventBus)
    {
        _eventBus = eventBus;
        _eventBus.SubscribeAsync<WebhookTriggeredEvent>(DeliverWebhook);
    }
    
    private async Task DeliverWebhook(WebhookTriggeredEvent evt)
    {
        try
        {
            using var client = new HttpClient();
            var response = await client.PostAsJsonAsync(evt.Url, 
                JsonDocument.Parse(evt.Payload));
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            // Retry logic with exponential backoff
            await RetryWebhookAsync(evt, retryCount: 0);
        }
    }
}
```

**Benefits:**
- Separate webhook service can scale independently
- Failed webhooks don't block main workflow
- Multiple instances for parallel delivery
- Configurable retry policies

---

## 🔄 KAFKA VS. RABBITMQ

### When to Choose RabbitMQ (Phase 2 Default)

```bash
aspire add RabbitMq.Aspire
```

**Best for:**
- Epic 5: Agent-to-agent messaging (request/reply pattern)
- Epic 7: Real-time collaboration
- Episode 13: Webhook delivery

**Characteristics:**
- Message queue (not event log)
- Fire-and-forget semantics
- Simpler to get started with
- Good for transient, non-critical messages

### When to Choose Kafka (Advanced)

```bash
# Future phase if audit trail becomes critical
aspire add Kafka.Aspire  # (if available) or use Container pattern
```

**Best for:**
- When you need event replay
- Immutable event log (audit trail)
- High throughput (>100K events/sec)
- Complex stream processing

**Characteristics:**
- Event log (append-only)
- Retained message history
- Multiple consumer groups
- Better for critical business events

**Decision for bmadServer:**
- **Phase 2:** Use RabbitMQ (simpler, sufficient for use cases)
- **Phase 3+:** Consider Kafka if audit requirements emerge

---

## 🛠️ ASPIRE MESSAGING MONITORING

### Via Aspire Dashboard

Once RabbitMQ is added:

1. **Start development:** `aspire run`
2. **Open Dashboard:** https://localhost:17360
3. **View RabbitMQ:** In Resources panel
   - Connection status
   - Queue depth
   - Message throughput
   - Active consumers

### Manual Health Check

```csharp
public void ConfigureHealthChecks(IHealthChecksBuilder healthChecks)
{
    healthChecks.AddRabbitMQ(
        builder.Configuration["RABBITMQ_CONNECTION_STRING"] ?? 
        "amqp://guest:guest@localhost:5672"
    );
}

app.MapHealthChecks("/health/rabbitmq");
```

### RabbitMQ Management UI

When using `.WithManagementPlugin()` in AppHost:

1. Open: http://localhost:15672
2. Login: guest / guest
3. View:
   - Exchanges created by agents
   - Queues and message counts
   - Bindings between components
   - Performance metrics

---

## 🚀 MIGRATION PATH: MVP → PHASE 2

### Before Phase 2 (Current MVP)
- ❌ RabbitMQ not installed
- ✅ In-process event bus via ServiceCollection
- ✅ All agents in single process
- ✅ Database queue for webhooks
- ✅ Single server deployment

### Starting Phase 2 (When Needed)
1. Run: `aspire add RabbitMq.Aspire`
2. Update AppHost (3-line change to add RabbitMQ)
3. Update ApiService Program.cs (register RabbitMQ)
4. Create DistributedEventBus implementation
5. Register message handlers as scoped services
6. Update agents to use IEventBus interface (already exist!)

### After Phase 2
- ✅ RabbitMQ running via Aspire
- ✅ Agents can be deployed separately
- ✅ Event-driven architecture fully distributed
- ✅ High-volume async processing supported
- ✅ Automatic dashboarding via Aspire

---

## 📋 IMPLEMENTATION ROADMAP

### MVP Phase (Now)
- [x] Design IEventBus interface
- [x] Implement in-process event bus
- [x] Register in ServiceCollection
- [x] Use in agents (in-process)
- [x] Database queue for webhooks
- [x] Document MVP pattern

### Phase 2 Planning (This Document)
- [x] Document RabbitMQ integration
- [x] Define migration path
- [x] Specify Aspire command
- [x] Show handler patterns

### Phase 2 Implementation (When Triggered)
- [ ] Run `aspire add RabbitMq.Aspire`
- [ ] Implement DistributedEventBus
- [ ] Update AppHost
- [ ] Refactor agents to use DI-injected IEventBus
- [ ] Deploy agents as separate services
- [ ] Monitor via Aspire Dashboard

### Phase 3 (Optional)
- [ ] Consider Kafka for audit trail
- [ ] Implement event sourcing pattern
- [ ] Advanced stream processing

---

## 🔗 REFERENCE LINKS

### Official Aspire Documentation
- **Main Docs:** https://aspire.dev
- **RabbitMQ Component:** Search "RabbitMQ" on aspire.dev
- **GitHub:** https://github.com/microsoft/aspire

### RabbitMQ (Client Library)
- **GitHub:** https://github.com/rabbitmq/rabbitmq-dotnet-client
- **Docs:** https://www.rabbitmq.com/tutorials/tutorial-one-dotnet.html

### Related Stories
- **Epic 5:** Multi-Agent Collaboration (primary messaging use case)
- **Epic 7:** Collaboration & Multi-User Support (checkpoint sync)
- **Epic 13:** Integrations & Webhooks (webhook delivery)

### Design Patterns
- **Event Sourcing:** https://martinfowler.com/eaaDev/EventSourcing.html
- **Message Bus Pattern:** https://www.enterpriseintegrationpatterns.com/MessageBus.html
- **Pub/Sub Pattern:** https://www.enterpriseintegrationpatterns.com/PublisherSubscriber.html

---

## ✅ VALIDATION CHECKLIST FOR PHASE 2

When implementing distributed messaging in Phase 2:

- [ ] Run `aspire add RabbitMq.Aspire` successfully
- [ ] Update AppHost with RabbitMQ resource
- [ ] Implement DistributedEventBus (or use library)
- [ ] Register message handlers in ServiceCollection
- [ ] Update agents to use IEventBus (no code changes needed!)
- [ ] Create integration tests for message flow
- [ ] Verify Aspire Dashboard shows RabbitMQ metrics
- [ ] Test retry logic for failed messages
- [ ] Load test with high message volume
- [ ] Document event naming conventions
- [ ] Update API documentation for async patterns

---

## 🎓 KEY CONCEPTS

### Pub/Sub (What We Use)
- **Many publishers** → **Many subscribers**
- One agent publishes event
- Multiple agents subscribe independently
- Example: MarketAnalysis → CompetitiveAnalysis, StrategicPlanning

### Request/Reply (Alternative)
- **One requester** → **One responder**
- Service A requests service B
- Service B replies directly
- RabbitMQ supports this via Reply-To headers

### Event Sourcing (Advanced)
- Persist all events as immutable log
- Rebuild state from event replay
- Full audit trail
- Consider for Phase 3 if requirements emerge

---

## 📞 DECISION LOG

| Date | Decision | Rationale |
|------|----------|-----------|
| 2026-01-24 | MVP does NOT include RabbitMQ | In-process messaging sufficient; added complexity not justified |
| 2026-01-24 | Phase 2 will add RabbitMQ via Aspire | Non-disruptive; enables distributed architecture |
| 2026-01-24 | Choose RabbitMQ over Kafka for Phase 2 | Simpler to operate; sufficient for use cases |
| 2026-01-24 | Event bus interface designed for migration | Agents use IEventBus; implementation swappable |
| 2026-01-24 | Database queue adequate for MVP webhooks | Simpler than external queue; can upgrade to RabbitMQ later |

---

**Status:** PHASE 2 PRE-PLANNING COMPLETE  
**Next Step:** When Phase 2 begins, follow the implementation steps in this document  
**Maintainer:** Development Team
