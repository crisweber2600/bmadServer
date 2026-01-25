# System Architecture

## Project Vision

**bmadServer** is a real-time workflow orchestration platform built on .NET Aspire. It enables teams to define, execute, and collaborate on complex multi-agent workflows with built-in observability, resilience, and state management.

---

## High-Level Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    User Interfaces                           │
│  ┌──────────────┐  ┌──────────────┐  ┌─────────────────┐   │
│  │  Web Browser │  │  Mobile App  │  │  CLI / Webhooks │   │
│  └──────┬───────┘  └──────┬───────┘  └────────┬────────┘   │
└─────────┼───────────────────┼──────────────────┼─────────────┘
          │ HTTP/WebSocket    │ gRPC             │ REST
          └───────────────────┴──────────────────┴─────────────┐
                                                                │
┌───────────────────────────────────────────────────────────────┤
│                 .NET Aspire Orchestration (AppHost)           │
│  ┌────────────────────────────────────────────────────────┐  │
│  │              Service Discovery                         │  │
│  │  ┌──────────────┐  ┌──────────────┐  ┌────────────┐   │  │
│  │  │ API Service  │  │ Web Frontend │  │ Auth Svc   │   │  │
│  │  │  (Port 8080) │  │  (Port 5173) │  │ (Future)   │   │  │
│  │  └──────┬───────┘  └──────┬───────┘  └────┬───────┘   │  │
│  │         │                 │               │            │  │
│  │  ┌──────▼─────────────────▼───────────────▼────────┐   │  │
│  │  │        Health Checks & Service Defaults         │   │  │
│  │  │  - OpenTelemetry logging & tracing             │   │  │
│  │  │  - HTTP resilience (retries, circuit breaker)  │   │  │
│  │  │  - Connection pooling                          │   │  │
│  │  └──────┬─────────────────────────────────────────┘   │  │
│  └─────────┼────────────────────────────────────────────┘  │
│            │                                                 │
└────────────┼──────────────────────────────────────────────┐  │
             │ Container Network                           │  │
┌────────────▼──────────────────────────────────────────┐  │  │
│              PostgreSQL 17                            │  │  │
│  ┌────────────────────────────────────────────────┐  │  │  │
│  │  Database (Managed by Aspire)                  │  │  │  │
│  │  - Users table                                 │  │  │  │
│  │  - Sessions table                              │  │  │  │
│  │  - Workflows table                             │  │  │  │
│  │  - Event logs (JSONB)                          │  │  │  │
│  │  - Decision audit trail                        │  │  │  │
│  └────────────────────────────────────────────────┘  │  │  │
│                                                       │  │  │
│  pgAdmin (Optional Database UI)                      │  │  │
│  https://localhost:5050                             │  │  │
└───────────────────────────────────────────────────────┤  │  │
                                                        │  │  │
┌───────────────────────────────────────────────────────┘  │  │
│                                                           │  │
│  📊 Aspire Dashboard: https://localhost:17360           │  │
│  - Service status and health                            │  │
│  - Real-time logs with trace IDs                        │  │
│  - Performance metrics                                  │  │
│                                                           │  │
│  🔍 OpenTelemetry Backend (Future):                      │  │
│  - Jaeger / Zipkin for distributed tracing             │  │
│  - Prometheus + Grafana for metrics                     │  │
└───────────────────────────────────────────────────────────┘
```

---

## Core Components

### 1. **bmadServer.AppHost** - Orchestration Engine
**Purpose:** Defines all services, dependencies, and health checks  
**Key Responsibility:** Service startup order and discovery

**Key Code (AppHost.cs):**
```csharp
// PostgreSQL database orchestrated by Aspire
var db = builder.AddPostgres("pgsql")
    .WithPgAdmin()
    .AddDatabase("bmadserver", "bmadserver_dev");

// API Service with health checks
var apiService = builder.AddProject<Projects.bmadServer_ApiService>("apiservice")
    .WithHttpHealthCheck("/health")
    .WithReference(db)
    .WaitFor(db);
```

**Startup Order:**
1. PostgreSQL container starts
2. API service starts (waits for database health check)
3. Web frontend starts (waits for API health check)
4. Aspire dashboard available at https://localhost:17360

---

### 2. **bmadServer.ApiService** - REST API & SignalR Hub
**Purpose:** Primary service handling business logic  
**Technologies:** ASP.NET Core, Entity Framework Core, SignalR

**Key Endpoints (Current):**
- `GET /` - Health status message
- `GET /health` - Full health check (database, services)
- `GET /weatherforecast` - Sample endpoint (remove in production)

**Future Endpoints (Epic 2-5):**
- `POST /api/auth/login` - User authentication
- `POST /api/workflows` - Create workflow
- `POST /api/chat` - Send chat message (SignalR)
- `GET /api/sessions` - User sessions

**Database Context (ApplicationDbContext):**
```csharp
public DbSet<User> Users { get; set; }
public DbSet<Session> Sessions { get; set; }
public DbSet<Workflow> Workflows { get; set; }
```

---

### 3. **bmadServer.ServiceDefaults** - Shared Infrastructure
**Purpose:** Reusable patterns for all services  
**Key Features:**

- **OpenTelemetry:** Structured JSON logging with trace IDs
- **Health Checks:** Automatic /health and /alive endpoints
- **Resilience:** HTTP retry policies, circuit breakers, timeouts
- **Service Discovery:** Automatic service-to-service communication

**Key Code (Extensions.cs):**
```csharp
public static IServiceCollection AddServiceDefaults(
    this IServiceCollection services) =>
    services
        .ConfigureOpenTelemetry()      // Logging + tracing
        .AddDefaultHealthChecks()       // /health endpoint
        .AddServiceDiscovery()          // Service-to-service
        .ConfigureHttpClientDefaults(); // Resilience patterns
```

---

### 4. **bmadServer.Web** - Frontend
**Purpose:** User interface for workflow management  
**Technologies:** React (or Blazor - TBD)

**Current:** Sample frontend  
**Future:** Workflow designer, chat interface, admin dashboard

---

### 5. **Database Layer** - PostgreSQL with EF Core

#### Schema (Current - Epic 1)
```sql
-- Users (basic fields, expanded in Epic 2)
CREATE TABLE users (
    id UUID PRIMARY KEY,
    email VARCHAR(255) UNIQUE NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    created_at TIMESTAMP DEFAULT NOW()
);

-- Sessions (basic fields, expanded in Epic 2)
CREATE TABLE sessions (
    id UUID PRIMARY KEY,
    user_id UUID REFERENCES users(id),
    connection_id VARCHAR(255),
    created_at TIMESTAMP DEFAULT NOW()
);

-- Workflows (basic fields, expanded in Epic 4)
CREATE TABLE workflows (
    id UUID PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    status VARCHAR(50) DEFAULT 'active',
    created_at TIMESTAMP DEFAULT NOW()
);

-- Event Log (JSONB for flexible event structure)
CREATE TABLE event_logs (
    id UUID PRIMARY KEY,
    aggregate_id UUID NOT NULL,
    event_type VARCHAR(255) NOT NULL,
    event_data JSONB NOT NULL,
    created_at TIMESTAMP DEFAULT NOW()
);

-- EF Core migrations table
CREATE TABLE __EFMigrationsHistory (
    MigrationId VARCHAR(150) PRIMARY KEY,
    ProductVersion VARCHAR(32) NOT NULL
);
```

#### JSONB Event Log
Stores flexible event data for workflows:
```json
{
  "workflow_id": "uuid",
  "event_type": "WorkflowStarted",
  "timestamp": "2026-01-25T14:30:00Z",
  "actor": "user-123",
  "payload": {
    "workflow_name": "Approval Process",
    "parameters": { "approval_levels": 3 }
  }
}
```

---

## Data Flow

### 1. Request Flow (HTTP)
```
User Browser
    ↓
GET /api/workflows
    ↓
API Service (Program.cs)
    ↓
Controller/Handler
    ↓
DbContext.Workflows.ToListAsync()
    ↓
PostgreSQL
    ↓
Return JSON
    ↓
User Browser (rendered)
```

### 2. Real-Time Chat Flow (SignalR - Future)
```
User1 Browser
    ↓
Send message via WebSocket
    ↓
SignalR Hub (/hubs/chat)
    ↓
Save to database
    ↓
Broadcast to other users via WebSocket
    ↓
User2 Browser (receives update in real-time)
```

### 3. Workflow Execution (Future - Epic 4)
```
User creates workflow
    ↓
POST /api/workflows
    ↓
Store in database
    ↓
Workflow Engine processes steps
    ↓
Update workflow status
    ↓
Log events to event_logs table
    ↓
Broadcast status to connected clients (SignalR)
```

---

## Technology Choices

### Why .NET 10?
- Modern, performant runtime
- Cloud-native with Aspire out-of-box
- Strong async/await support for real-time features
- Excellent Entity Framework Core for data access

### Why .NET Aspire?
- Unified orchestration for local & cloud deployment
- Built-in service discovery
- Out-of-box health checks and resilience
- Dashboard for development visibility
- No Docker expertise required for local development

### Why PostgreSQL?
- Open-source, reliable, proven
- JSONB support for flexible event storage
- Excellent for complex queries and migrations
- Managed via Aspire (no manual setup)

### Why SignalR?
- Real-time bidirectional communication
- Built into ASP.NET Core
- Automatic fallback (WebSocket → SSE → polling)
- Scales with Azure SignalR Service in production

### Why EF Core Migrations?
- Version-controlled schema changes
- Easy rollback and replay
- Team collaboration on database changes
- LINQ queries instead of raw SQL (mostly)

---

## Deployment Architecture

### Local Development
```
Developer Machine
├── .NET SDK
├── Aspire Runtime
└── Docker Desktop (Aspire runs containers)
    ├── PostgreSQL 17
    ├── API Service (ASP.NET Core)
    └── Web Frontend
```

### CI/CD Pipeline (GitHub Actions)
```
Developer Push
    ↓
GitHub Actions Workflow
    ├─ Build (dotnet build --configuration Release)
    ├─ Test (dotnet test)
    └─ (Future) Deploy to staging/production
```

### Production (Self-Hosted)
```
Server Machine
├── .NET Runtime 10
├── Docker Engine
└── Docker Compose (orchestration)
    ├── PostgreSQL 17 (volume-backed)
    ├── API Service (health-checked)
    ├── Web Frontend
    └── Reverse Proxy (nginx)
```

### Production (Cloud - Future)
```
Azure Container Instances / AKS
├── API Service container
├── Web Frontend container
├── PostgreSQL (Azure Database)
└── SignalR Service (Azure SignalR Service)
```

---

## Health & Observability

### Health Checks
```
GET /health
{
  "status": "Healthy",
  "checks": {
    "database": "Healthy",
    "services": "Healthy"
  }
}
```

### Structured Logging (OpenTelemetry)
```json
{
  "timestamp": "2026-01-25T14:30:45.123Z",
  "level": "Information",
  "message": "API request completed",
  "trace_id": "550e8400-e29b-41d4-a716-446655440000",
  "span_id": "b9c7c3f5e8a1d2b4",
  "service": "ApiService",
  "duration_ms": 145
}
```

### Distributed Tracing
- Trace ID: Unique per request across all services
- Span ID: Individual operation within trace
- Visible in Aspire Dashboard
- Future: Jaeger/Zipkin for complex multi-service flows

---

## Scalability Considerations

### Horizontal Scaling (Future)
```
Load Balancer
├── API Instance 1
├── API Instance 2
└── API Instance 3
    └── PostgreSQL (shared)
```

### Connection Pooling
- Min: 5 connections
- Max: 100 connections
- Managed by Npgsql provider

### Database Optimization
- Connection pooling enabled
- Query result caching (Future)
- Read replicas for reporting (Future)

### Event Sourcing (Future - Epic 9)
- Event log as source of truth
- Current state computed from events
- Perfect for workflow audit trails

---

## Security Considerations

### Current (Epic 1)
- Health endpoints public (by design)
- No authentication yet
- HTTPS on production only

### Planned (Epic 2-11)
- JWT authentication
- Role-based access control (RBAC)
- Rate limiting per user
- Input validation & sanitization
- Encryption at rest
- Security headers (HSTS, CSP)
- Audit logging for compliance

---

## Future Enhancements

### Near Term (Epic 2-3)
- User authentication & JWT
- Real-time chat with SignalR
- WebSocket error handling

### Medium Term (Epic 4-6)
- Workflow orchestration engine
- Multi-agent collaboration
- Decision management & locking

### Long Term (Epic 7-13)
- Multi-user collaboration
- Persona translation & language adaptation
- Comprehensive error recovery
- Admin dashboard
- Webhook integrations
- Advanced monitoring (Prometheus + Grafana)

---

## References

- **Microsoft Aspire:** https://learn.microsoft.com/en-us/dotnet/aspire
- **EF Core Migrations:** https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations
- **PostgreSQL:** https://www.postgresql.org/docs
- **SignalR:** https://learn.microsoft.com/en-us/aspnet/core/signalr/introduction
- **OpenTelemetry:** https://opentelemetry.io/docs

---

**Last Updated:** 2026-01-25  
**Version:** 1.0 (Epic 1 baseline)
