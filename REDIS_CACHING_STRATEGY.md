# Redis Caching Strategy for bmadServer

**Document Type:** Pre-Planning Architecture  
**Status:** PHASE 2 Pre-Planning (Not MVP)  
**Target Phase:** Phase 2 - Post-MVP Scaling  
**Last Updated:** 2026-01-24  
**Owner:** Development Team  

---

## 📋 EXECUTIVE SUMMARY

This document outlines the Redis caching strategy for bmadServer, planned for **Phase 2** implementation after MVP completion. Redis will be introduced via Aspire component when horizontal scaling or intensive caching requirements emerge.

### When to Implement Redis

**Phase 1 (MVP - Current):** ❌ NOT NEEDED
- Single-process in-memory caching sufficient
- PostgreSQL handles state management
- No horizontal scaling required

**Phase 2 (Post-MVP - When Needed):**
- ✅ Multiple server instances deployed
- ✅ SignalR backplane needed (distributed connections)
- ✅ Cache invalidation across instances required
- ✅ High-frequency queries causing database bottlenecks
- ✅ Session state needs to be server-independent

### Decision: MVP does NOT include Redis

**Rationale:**
- Added complexity not needed for single-instance MVP
- Aspire PostgreSQL handles all Phase 1 requirements
- Can be added non-disruptively in Phase 2
- Aspire component (`aspire add Redis.Distributed`) makes future migration trivial

---

## 🎯 PHASE 2 REDIS IMPLEMENTATION PLAN

### Adding Redis to bmadServer (When Needed)

**Step 1: Add Redis via Aspire CLI**
```bash
cd /Users/cris/bmadServer/src
aspire add Redis.Distributed
```

This command:
- Installs `Aspire.Hosting.Redis` NuGet package
- Installs `StackExchange.Redis` (client library)
- Creates Redis container orchestration in AppHost

**Step 2: Configure in AppHost**
```csharp
// In bmadServer.AppHost/Program.cs
var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .AddDatabase("bmadserver", "bmadserver_dev");

var redis = builder.AddRedis("redis")
    .WithDataVolume();  // Persist cache data

var api = builder.AddProject<Projects.bmadServer_ApiService>("api")
    .WithReference(postgres)
    .WithReference(redis)
    .WaitFor(redis);

builder.Build().Run();
```

**Step 3: Configure in ApiService**
```csharp
// In bmadServer.ApiService/Program.cs
builder.AddServiceDefaults();

// Add Redis caching
builder.AddStackExchangeRedisCache(options =>
{
    options.ConfigurationOptions = ConfigurationOptions.Parse(
        builder.Configuration["REDIS_CONNECTION_STRING"] ?? "localhost:6379"
    );
});
```

**Step 4: Use in Services**
```csharp
public class CachingService
{
    private readonly IDistributedCache _cache;
    
    public CachingService(IDistributedCache cache)
    {
        _cache = cache;
    }
    
    public async Task<T> GetOrSetAsync<T>(string key, 
        Func<Task<T>> factory, 
        TimeSpan? expiration = null)
    {
        var cached = await _cache.GetStringAsync(key);
        if (cached != null)
            return JsonSerializer.Deserialize<T>(cached)!;
        
        var value = await factory();
        await _cache.SetStringAsync(key, 
            JsonSerializer.Serialize(value),
            new DistributedCacheEntryOptions 
            { 
                AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(30)
            }
        );
        
        return value;
    }
}
```

---

## 🔄 USE CASES FOR REDIS IN BMADSERVER

### Epic 10: Error Handling & Recovery
**Pattern:** Response Caching

**What to Cache:**
- Workflow state snapshots (1-5 min TTL)
- Health check results (30 sec TTL)
- Frequently accessed agent configurations (1 hour TTL)

**Implementation Example:**
```csharp
// In ErrorHandlingService
public async Task<WorkflowState> GetWorkflowStateAsync(string workflowId)
{
    return await _cache.GetOrSetAsync(
        $"workflow:{workflowId}",
        async () => await _db.GetWorkflowAsync(workflowId),
        TimeSpan.FromMinutes(5)
    );
}
```

**Benefits:**
- Reduces database load during recovery scenarios
- Faster workflow state retrieval
- Smoother degradation under high load

---

### Epic 3: Real-Time Chat Interface (Horizontal Scaling)
**Pattern:** SignalR Backplane

**What to Cache:**
- Active connection metadata
- Message delivery confirmations
- User presence information

**Implementation Pattern:**
```csharp
// In bmadServer.ApiService/Program.cs
builder.Services.AddSignalR()
    .AddStackExchangeRedis(options =>
    {
        options.Configuration = builder.Configuration["REDIS_CONNECTION_STRING"];
    });

// ChatHub.cs
public class ChatHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        // Redis backplane automatically distributes connection info
        await base.OnConnectedAsync();
        await Clients.All.SendAsync("UserConnected", Context.User?.Identity?.Name);
    }
}
```

**Why This Matters:**
- SignalR can serve thousands per instance
- With multiple servers: connections distributed across instances
- Redis backplane routes messages to correct server
- User A on Server 1 can message User B on Server 2

**When You Need This:**
- Initial deployment: 1 server - NO Redis needed
- Scale to 2+ servers - Add Redis backplane

---

### Epic 9: Data Persistence & State Management
**Pattern:** Session State Cache

**What to Cache:**
- User session objects (tied to HttpContext.Session)
- Multi-turn conversation context (5-30 min TTL)
- Temporary workflow state during execution

**Implementation Pattern:**
```csharp
// In bmadServer.ApiService/Program.cs
builder.AddStackExchangeRedisCache(options =>
{
    options.ConfigurationOptions = ConfigurationOptions.Parse(
        builder.Configuration["REDIS_CONNECTION_STRING"] ?? "localhost:6379"
    );
});

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.IsEssential = true;
});

// Middleware
app.UseSession();  // Uses Redis backend automatically
```

**Benefits:**
- Multi-server deployment: Same session accessible from any server
- No server affinity needed (sticky sessions)
- Automatic expiration management

---

### Future: Epic 13 Webhook Queue (Alternative Approach)
**Note:** Epic 13 webhooks may initially use database queue (simpler). If webhook throughput becomes bottleneck, Redis can be alternative to RabbitMQ.

```csharp
// Alternative lightweight queue using Redis
public class WebhookQueueService
{
    private readonly IConnectionMultiplexer _redis;
    
    public async Task EnqueueWebhookAsync(WebhookEvent evt)
    {
        var db = _redis.GetDatabase();
        var json = JsonSerializer.Serialize(evt);
        await db.ListRightPushAsync("webhook:queue", json);
    }
    
    public async Task<WebhookEvent?> DequeueWebhookAsync()
    {
        var db = _redis.GetDatabase();
        var json = await db.ListLeftPopAsync("webhook:queue");
        return json.IsNullOrEmpty ? null 
            : JsonSerializer.Deserialize<WebhookEvent>(json.ToString());
    }
}
```

---

## 🛠️ ASPIRE REDIS MONITORING

### Via Aspire Dashboard

Once Redis is added, it automatically appears in the Aspire Dashboard:

1. **Start development:** `aspire run`
2. **Open Dashboard:** https://localhost:17360
3. **View Redis:** In Resources panel
   - Connection status
   - Memory usage
   - Key count
   - Commands executed

### Manual Health Check

```csharp
// HealthChecks for Redis
public void ConfigureHealthChecks(IHealthChecksBuilder healthChecks)
{
    healthChecks.AddRedis(
        builder.Configuration["REDIS_CONNECTION_STRING"] ?? "localhost:6379",
        "redis"
    );
}

// Endpoint for monitoring
app.MapHealthChecks("/health/redis");
```

---

## 🚀 MIGRATION PATH: MVP → PHASE 2

### Before Phase 2 (Current MVP)
- ❌ Redis not installed
- ✅ PostgreSQL handles all state
- ✅ In-process caching via IMemoryCache
- ✅ Single server deployment

### Starting Phase 2 (When Scaling)
1. Run: `aspire add Redis.Distributed`
2. Update AppHost (3-line change)
3. Update ApiService Program.cs (3-line change)
4. Refactor high-hit endpoints to use Redis
5. Configure SignalR backplane if multi-server

### After Phase 2
- ✅ Redis running via Aspire
- ✅ Automatic dashboarding
- ✅ Supports unlimited horizontal scaling
- ✅ SignalR works across servers
- ✅ Session state server-independent

---

## 📊 REDIS VS. ALTERNATIVES

| Approach | MVP | Phase 2 | Pro | Con |
|----------|-----|---------|-----|-----|
| **IMemoryCache** | ✅ | ❌ | Simple, no dependency | Not distributed |
| **PostgreSQL** | ✅ | ✅ | Persistent, reliable | Slower than Redis |
| **Redis** | ❌ | ✅ | Fast, distributed, built-in | Added complexity |
| **Memcached** | ❌ | ⚠️ | Fast like Redis | Simpler but no persistence |

**Recommendation:** Stick with PostgreSQL + IMemoryCache for MVP. Add Redis in Phase 2 if needed for backplane or hot-path caching.

---

## 🔗 REFERENCE LINKS

### Official Aspire Redis Documentation
- **Main Docs:** https://aspire.dev
- **Redis Component:** Search "Redis" on aspire.dev
- **GitHub:** https://github.com/microsoft/aspire

### StackExchange.Redis (Client Library)
- **GitHub:** https://github.com/StackExchange/StackExchange.Redis
- **Docs:** https://stackexchange.github.io/StackExchange.Redis/

### Related Stories
- **Epic 10:** Error Handling & Recovery (primary caching use case)
- **Epic 3:** Real-Time Chat Interface (SignalR backplane use case)
- **Epic 9:** Data Persistence & State Management (session caching use case)

---

## ✅ VALIDATION CHECKLIST FOR PHASE 2

When implementing Redis in Phase 2:

- [ ] Run `aspire add Redis.Distributed` successfully
- [ ] Update AppHost with Redis resource and references
- [ ] Update ApiService to inject IDistributedCache
- [ ] Implement caching service with TTL strategy
- [ ] Add health checks for Redis
- [ ] Test cache invalidation scenarios
- [ ] Configure SignalR backplane if multi-server
- [ ] Verify Aspire Dashboard shows Redis metrics
- [ ] Load test with caching enabled
- [ ] Document cache key naming conventions

---

## 🎓 LEARNING RESOURCES

### Quick Start
1. Read: https://aspire.dev (search "Redis")
2. Run: `aspire add Redis.Distributed`
3. Check: Updated AppHost demonstrates pattern

### Deep Dive
- StackExchange.Redis best practices
- Cache invalidation strategies
- SignalR backplane configuration
- Redis key expiration patterns
- Memory management in distributed cache

---

## 📞 DECISION LOG

| Date | Decision | Rationale |
|------|----------|-----------|
| 2026-01-24 | MVP does NOT include Redis | Aspire PostgreSQL sufficient for Phase 1; complexity not justified |
| 2026-01-24 | Phase 2 will add Redis via Aspire component | Non-disruptive addition; benefits horizontal scaling |
| 2026-01-24 | Primary use: SignalR backplane (Epic 3 scaling) | Enables multi-server deployments |
| 2026-01-24 | Secondary use: Response caching (Epic 10) | Performance optimization for error recovery |

---

**Status:** PHASE 2 PRE-PLANNING COMPLETE  
**Next Step:** When Phase 2 begins, follow the implementation steps in this document  
**Maintainer:** Development Team
