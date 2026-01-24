# Architecture Decision Records: ADR-004 through ADR-025

**Generated:** 2026-01-23  
**Category:** Consolidated reference for decisions 4-25  
**Status:** ACCEPTED (all locked decisions)

---

## ADR-004: Real-Time Communication with SignalR

**Category:** 3 - API & Communication  
**Decision ID:** 3.3

### Context
bmadServer requires real-time updates when workflow state changes. Polling creates latency; WebSocket provides true real-time without overhead.

### Decision
**Use SignalR 8.0+ for WebSocket-based real-time communication**

- Automatic fallback to Server-Sent Events if WebSocket unavailable
- Built-in reconnection + acknowledgment handling
- Works in .NET Aspire stack natively
- Scales to thousands of concurrent connections per instance

### Implementation
```csharp
// Hub definition
public class WorkflowHub : Hub
{
    public async Task SubscribeToWorkflow(string workflowId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"workflow-{workflowId}");
    }

    public async Task DecisionUpdated(string workflowId, Decision decision)
    {
        await Clients.Group($"workflow-{workflowId}")
            .SendAsync("decision-updated", decision);
    }
}

// Client connection
const connection = new HubConnectionBuilder()
    .withUrl("/workflowhub", {
        accessTokenFactory: () => getAuthToken()
    })
    .withAutomaticReconnect([0, 2000, 10000])
    .build();
```

### Rationale
- ✅ Battle-tested (used by enterprise .NET systems)
- ✅ Native ASP.NET Core integration
- ✅ Handles mobile/unreliable networks gracefully
- ✅ Built-in security (JWT + SignalR auth)

---

## ADR-005: Event Log + JSONB Dual Write Atomicity

**Category:** 1 - Data Architecture  
**Decision ID:** 1.1

### Decision
**All state mutations written atomically to both workflow_state (JSONB) and event_log (append-only)**

### Implementation
```csharp
using var transaction = await _context.Database.BeginTransactionAsync();
try
{
    // Update JSONB state
    workflow.State = newState;
    workflow.Version++;
    
    // Log event
    _context.AuditLogs.Add(new AuditLog
    {
        WorkflowId = workflow.Id,
        EventType = "decision_approved",
        Payload = JsonSerializer.Serialize(decision),
        Actor = userId,
        Timestamp = DateTime.UtcNow
    });

    await _context.SaveChangesAsync();
    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;
}
```

### Rationale
- ✅ Atomic consistency (both succeed or both rollback)
- ✅ Audit trail never out of sync with state
- ✅ Replay capability (reconstruct state from events)

---

## ADR-006: JWT + Refresh Token Authentication

**Category:** 2 - Authentication & Security  
**Decision ID:** 2.5

### Decision
**15-minute JWT access tokens + 7-day HttpOnly refresh cookies**

### Token Structure
- **Access Token (JWT):** Embedded in Authorization header, short-lived
- **Refresh Token:** HttpOnly cookie, long-lived, automatically sent by browser
- **Idle Timeout:** 30 minutes of inactivity forces re-login

### Rationale
- ✅ XSS-safe (refresh token in secure, HttpOnly cookie)
- ✅ CSRF-safe (SameSite=Strict)
- ✅ Works across tabs (cookie shared)
- ✅ Transparent token refresh (no UX disruption)

---

## ADR-007: API Versioning via URL Path

**Category:** 3 - API & Communication  
**Decision ID:** 3.5

### Decision
**Version in URL: `/api/v1/workflows`, `/api/v2/workflows` (future)**

### Rationale
- ✅ Explicit and discoverable (version in URL)
- ✅ Backward compatible (old clients use `/api/v1/`)
- ✅ Clear deprecation path (sunset v1 after 6 months notice)

---

## ADR-008: ProblemDetails Error Response Format

**Category:** 3 - API & Communication  
**Decision ID:** 3.2

### Decision
**RFC 7807 ProblemDetails for all errors (REST + WebSocket)**

### Format
```json
{
  "type": "https://bmadserver.api/errors/workflow-conflict",
  "title": "Workflow State Conflict",
  "status": 409,
  "detail": "State modified by another user",
  "instance": "/api/v1/workflows/wf-123",
  "traceId": "0HMVF7GIJF6AS:00000001"
}
```

### Rationale
- ✅ Industry standard (recognized by all tools)
- ✅ Machine-readable (clients parse programmatically)
- ✅ Consistent across REST + WebSocket

---

## ADR-009: Optimistic Concurrency Control

**Category:** 1 - Data Architecture  
**Decision ID:** 1.1

### Decision
**Version field (`_version`) on all JSONB documents for optimistic locking**

### Pattern
1. Client reads workflow with version=5
2. Client modifies offline
3. Client submits with expectedVersion=5
4. Server checks: actualVersion=5? Yes → accept, increment to 6
5. Server checks: actualVersion=6? No → reject with conflict

### Rationale
- ✅ Prevents silent overwrites (Sarah's decision overwriting Marcus's)
- ✅ No database locks (scales well)
- ✅ Detected conflicts surfaced to user ("refresh and try again")

---

## ADR-010: PostgreSQL JSONB with GIN Indexes

**Category:** 1 - Data Architecture  
**Decision ID:** 1.1

### Decision
**PostgreSQL JSONB columns with GIN indexes for performance**

### Performance Impact
- Without index: JSONB queries = 200-500ms (sequential scan)
- With GIN index: JSONB queries = 5-20ms (indexed lookup)
- **40x improvement at scale**

### Example
```sql
CREATE INDEX idx_workflows_status ON workflows 
USING GIN(state jsonb_ops);

SELECT * FROM workflows 
WHERE state @> '{"status": "in_progress"}'  -- Uses GIN index
```

---

## ADR-011: In-Memory IMemoryCache (MVP)

**Category:** 1 - Data Architecture  
**Decision ID:** 1.4

### Decision
**Cache session metadata, templates, agent registry in-memory (IMemoryCache)**

### Scope
- Session metadata: 5-minute TTL
- Workflow templates: 1-hour TTL (refresh on config change)
- Agent registry: 2-minute TTL
- Decision history: Session-lifetime TTL

### Rationale
- ✅ Zero external dependencies (simplifies MVP)
- ✅ Sub-millisecond access times
- ✅ Redis-ready interface for Phase 2 upgrade

---

## ADR-012: FluentValidation for Business Rules

**Category:** 1 - Data Architecture  
**Decision ID:** 1.2

### Decision
**EF Core Data Annotations + FluentValidation for comprehensive validation**

### Strategy
1. Database constraints (NOT NULL, UNIQUE): EF Core annotations
2. Business rules (workflow state transitions): FluentValidation
3. JSONB schema: Custom application-layer validators

### Rationale
- ✅ Separates concerns (DB constraints vs. business logic)
- ✅ Easy to test (validators run without database)
- ✅ Reusable across controllers/hubs

---

## ADR-013: Hybrid RBAC + Claims Authorization

**Category:** 2 - Authentication & Security  
**Decision ID:** 2.2

### Decision
**Roles (Admin, Participant, Viewer) + Claims (workflow:create, decision:approve)**

### Policy Example
```csharp
options.AddPolicy("CanApproveDecision",
    policy => policy
        .RequireRole("Participant", "Admin")
        .RequireClaim("decision:approve"));
```

### Rationale
- ✅ Supports current 3-person team structure
- ✅ Flexible for team growth (add claims without role changes)
- ✅ Workflow-specific permissions possible

---

## ADR-014: HTTPS + TLS 1.3 Transport Security

**Category:** 2 - Authentication & Security  
**Decision ID:** 2.3

### Decision
**TLS 1.3+ enforced for all traffic (MVP); at-rest encryption Phase 2**

### Configuration
```csharp
app.UseHttpsRedirection();
app.UseHsts(options => {
    options.MaxAge = TimeSpan.FromDays(365);
    options.Preload = true;
});
```

### Rationale
- ✅ MVP protection (data encrypted in transit)
- ✅ Industry standard for self-hosted
- ✅ At-rest encryption deferred to Phase 2 (no need for MVP)

---

## ADR-015: Per-User Rate Limiting

**Category:** 2 - Authentication & Security  
**Decision ID:** 2.4

### Decision
**60 requests/minute per user (API), 5 concurrent WebSocket connections**

### Implementation
```csharp
options.AddFixedWindowLimiter("api-default", options => {
    options.PermitLimit = 60;
    options.Window = TimeSpan.FromMinutes(1);
});
```

### Rationale
- ✅ Protects from accidental DOS
- ✅ Fair resource allocation
- ✅ Easy to adjust limits later

---

## ADR-016: Zustand + TanStack Query State Management

**Category:** 4 - Frontend Architecture  
**Decision ID:** 4.1

### Decision
**Zustand (2KB) for client state + TanStack Query (5.x) for server state**

### Layer Model
| Layer | Tool | Examples |
|-------|------|----------|
| Local UI | `useState` | Modal open, form input |
| Client State | Zustand | Auth user, theme, sidebar |
| Server State | TanStack Query | Workflows, decisions, API data |

### Rationale
- ✅ Minimal bundle size (vs Redux 20KB)
- ✅ Clear responsibility split
- ✅ Scales from MVP to enterprise

---

## ADR-017: Feature-Based Component Architecture

**Category:** 4 - Frontend Architecture  
**Decision ID:** 4.2

### Folder Structure
```
src/
├── features/
│   ├── auth/
│   │   ├── components/
│   │   ├── hooks/
│   │   ├── services/
│   │   └── types.ts
│   ├── workflows/
│   │   ├── components/
│   │   ├── hooks/
│   │   ├── services/
│   │   └── types.ts
│   ├── decisions/
│   └── collaborators/
├── shared/
│   ├── components/
│   ├── hooks/
│   ├── utils/
│   └── types.ts
└── App.tsx
```

### Rationale
- ✅ Scalable (parallel team development)
- ✅ Isolated concerns (features don't interfere)
- ✅ Easy to tree-shake unused features

---

## ADR-018: React Router v7 with Code Splitting

**Category:** 4 - Frontend Architecture  
**Decision ID:** 4.3

### Routing Pattern
```typescript
const routes = [
  { path: "/", element: <Layout /> },
  { 
    path: "/workflows", 
    element: <Workflows />, 
    lazy: () => import("./features/workflows")
  },
  { 
    path: "/decisions/:id", 
    element: <DecisionDetail />,
    lazy: () => import("./features/decisions")
  }
];
```

### Rationale
- ✅ Lazy loading reduces initial bundle (50-70% faster)
- ✅ Route-based code splitting optimizes load times
- ✅ Native to React Router v7

---

## ADR-019: Tailwind CSS for Styling

**Category:** 4 - Frontend Architecture  
**Decision ID:** 4.4

### Rationale
- ✅ Utility-first design system (fast prototyping)
- ✅ 20KB production bundle (tiny for MVP)
- ✅ Dark mode support built-in
- ✅ Mobile-first responsive design

---

## ADR-020: Aspire-Based Self-Hosted Deployment

**Category:** 5 - Infrastructure & Deployment  
**Decision ID:** 5.1

### Decision
**Docker Compose (MVP) → Kubernetes (Phase 2)**

### MVP Docker Setup
```yaml
version: '3.8'
services:
  app:
    build: .
    ports:
      - "5000:5000"
    environment:
      ConnectionStrings__DefaultConnection: "Host=postgres;..."
    depends_on:
      - postgres
  
  postgres:
    image: postgres:17
    environment:
      POSTGRES_PASSWORD: bmadserver
    volumes:
      - postgres_data:/var/lib/postgresql/data
```

### Rationale
- ✅ Self-hosted (no cloud vendor lock-in)
- ✅ Aspire-native (designed for this use case)
- ✅ Clear upgrade path to Kubernetes

---

## ADR-021: Health Checks + Readiness Probes

**Category:** 5 - Infrastructure & Deployment  
**Decision ID:** 5.2

### Implementation
```csharp
builder.Services.AddHealthChecks()
    .AddDbContextCheck<BmadServerContext>()
    .AddCheck<AgentRouterHealthCheck>("agent-router");

app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready")
});
```

### Rationale
- ✅ Kubernetes readiness probes route traffic to healthy instances
- ✅ Monitoring alerts on health check failures
- ✅ Zero-downtime deployments

---

## ADR-022: OpenTelemetry + Aspire Dashboard

**Category:** 5 - Infrastructure & Deployment  
**Decision ID:** 5.3

### Implementation
```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddSqlClientInstrumentation())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddRuntimeInstrumentation());
```

### Dashboard Access
- **Local:** `https://localhost:17360`
- **Live metrics:** Request rates, latency, errors
- **Traces:** Full distributed tracing of requests
- **Logs:** Structured logging with correlation IDs

---

## ADR-023: 99.5% Uptime Target + Monitoring Baselines

**Category:** 5 - Infrastructure & Deployment  
**Decision ID:** 5.2

### SLO (Service Level Objective)
- **Availability:** 99.5% monthly uptime (3.6 hours downtime allowed)
- **Latency:** p95 < 1 second, p99 < 5 seconds
- **Error Rate:** < 0.1% of requests fail

### Load Test Baseline
- **500 requests/second** sustained
- **100 concurrent WebSocket connections**
- **10 concurrent workflow operations**
- **1 minute recovery time on failure**

### Monitoring & Alerting
```
- CPU > 80% → alert "High CPU usage"
- Memory > 85% → alert "Memory pressure"
- Database connection pool > 90% → alert "DB connection exhaustion"
- Error rate > 1% → alert "Error rate spike"
- Request latency p95 > 2s → alert "Slow requests"
```

---

## ADR-024: Prometheus + Grafana Monitoring Stack

**Category:** 5 - Infrastructure & Deployment  
**Decision ID:** 5.3

### Metrics
- **Application:** Request latency, error rate, throughput
- **Database:** Query latency, connection count, JSONB query performance
- **Infrastructure:** CPU, memory, disk, network I/O
- **Business:** Workflow completion rate, decision approval time

### Dashboards
1. **System Overview:** Green/red status of all components
2. **Performance:** Latency percentiles, throughput trends
3. **Errors:** Error rate, top error types, recent incidents
4. **Database:** Slow queries, connection pool health, JSONB performance

---

## ADR-025: Quarterly Security Credential Rotation

**Category:** 5 - Infrastructure & Deployment  
**Decision ID:** 5.4

### Rotation Schedule
- **Database credentials:** Quarterly (Jan, Apr, Jul, Oct)
- **JWT signing key:** Annually (January)
- **API keys:** Quarterly
- **TLS certificates:** 90-day renewal (automated with Let's Encrypt)

### Process
1. Generate new credentials
2. Update in production secrets store
3. Restart service (pick low-traffic window)
4. Verify all logins working
5. Document in audit trail

### Rationale
- ✅ Limits blast radius of credential compromise
- ✅ Industry standard practice
- ✅ Minimal impact on MVP (3-person team, no external integrations)

---

## Summary Table: All 25 Decisions

| ID | Title | Category | Status | Tech Stack |
|---|---|---|---|---|
| 1.1 | Data Modeling | Data | LOCKED | EF Core 9.0 + JSONB |
| 1.2 | Validation | Data | LOCKED | FluentValidation 11.9.2 |
| 1.3 | Migrations | Data | LOCKED | EF Core CLI |
| 1.4 | Caching | Data | LOCKED | IMemoryCache |
| 1.5 | Concurrency | Data | LOCKED | Version field (_version) |
| 2.1 | Authentication | Security | LOCKED | Local DB + JWT |
| 2.2 | Authorization | Security | LOCKED | RBAC + Claims |
| 2.3 | Encryption | Security | LOCKED | HTTPS + TLS 1.3 |
| 2.4 | Rate Limiting | Security | LOCKED | System.Threading.RateLimiting |
| 2.5 | Token Expiration | Security | LOCKED | JWT 15min + Refresh 7day |
| 3.1 | REST Design | API | LOCKED | Hybrid REST + RPC |
| 3.2 | Error Handling | API | LOCKED | RFC 7807 ProblemDetails |
| 3.3 | Documentation | API | LOCKED | OpenAPI 3.1 + Swagger |
| 3.4 | WebSocket Errors | API | LOCKED | Explicit SignalR messages |
| 3.5 | API Versioning | API | LOCKED | URL path (/api/v1/) |
| 4.1 | State Management | Frontend | LOCKED | Zustand + TanStack Query |
| 4.2 | Components | Frontend | LOCKED | Feature-based architecture |
| 4.3 | Routing | Frontend | LOCKED | React Router v7 |
| 4.4 | Styling | Frontend | LOCKED | Tailwind CSS |
| 4.5 | Build Tool | Frontend | LOCKED | Vite |
| 5.1 | Deployment | Infrastructure | LOCKED | Docker Compose → K8s |
| 5.2 | Health Checks | Infrastructure | LOCKED | Readiness + Liveness probes |
| 5.3 | Monitoring | Infrastructure | LOCKED | OpenTelemetry + Grafana |
| 5.4 | Credential Rotation | Infrastructure | LOCKED | Quarterly + Annual |
| 5.5 | Scaling Strategy | Infrastructure | LOCKED | Single → Docker Swarm → K8s |

---

## Cross-Reference Index

### By Priority
**P0 (MVP Critical):**
- 1.1, 1.3, 2.1, 2.5, 3.1, 4.1, 5.1

**P1 (Pre-Launch):**
- 1.2, 1.4, 2.2, 2.3, 2.4, 3.2, 3.3, 4.2, 5.2, 5.3

**P2 (Monitor Post-MVP):**
- 1.5, 3.4, 3.5, 4.3, 4.4, 5.4, 5.5

### By Component
**Database Layer:** 1.1, 1.2, 1.3, 1.4, 1.5  
**Authentication:** 2.1, 2.2, 2.3, 2.4, 2.5  
**API Layer:** 3.1, 3.2, 3.3, 3.4, 3.5  
**Frontend:** 4.1, 4.2, 4.3, 4.4, 4.5  
**Infrastructure:** 5.1, 5.2, 5.3, 5.4, 5.5  

---

## References & Related Materials

- Complete architecture document: `/Users/cris/bmadServer/_bmad-output/planning-artifacts/architecture.md`
- Implementation patterns: `implementation-patterns.md`
- Developer onboarding: `developer-onboarding.md`
- Project context: `project-context-ai.md`
- 8-week roadmap: `8-week-roadmap.md`
