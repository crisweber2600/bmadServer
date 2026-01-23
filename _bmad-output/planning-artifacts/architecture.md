---
stepsCompleted: [1, 2, 3, 4]
categoriesCompleted: [1, 2, 3, 4, 5]
partyModeInsights: true
inputDocuments:
  - /Users/cris/bmadServer/_bmad-output/planning-artifacts/product-brief-bmadServer-2026-01-20.md
  - /Users/cris/bmadServer/_bmad-output/planning-artifacts/prd.md
  - /Users/cris/bmadServer/_bmad-output/planning-artifacts/ux-design-specification.md
workflowState: step_4_complete_ready_for_step_5
date: 2026-01-23
author: Cris
architecturalConstraints:
  - "Backend: .NET 10 with Aspire"
  - "Template: .NET Aspire Starter App (via Aspire CLI)"
  - "Framework: ASP.NET Core with Aspire orchestration"
  - "Integration: OpenCode server for BMAD interactions"
  - "Real-time: SignalR WebSocket (NuGet: Microsoft.AspNetCore.SignalR)"
  - "State: PostgreSQL with Event Log (+ JSONB concurrency control)"
  - "Agents: In-process (MVP), Queue-ready interface"
  - "NuGet Preference: Aspire packages (v13.1.0+)"
adrsIdentified: 4
starterSelected: "aspire-starter"
dataArchitectureDecisions:
  - "Data Modeling: Hybrid (EF Core 9.0 + PostgreSQL JSONB)"
  - "Validation: EF Core Annotations + FluentValidation 11.9.2"
  - "Migrations: EF Core Migrations with local testing gate"
  - "Caching: In-Process IMemoryCache (Redis-ready interface)"
  - "Database: PostgreSQL 17.x LTS (incremental VACUUM + GIN indexes)"
authenticationSecurityDecisions:
  - "Authentication: Hybrid (Local DB MVP + OpenID Connect Ready Phase 2)"
  - "Authorization: Hybrid RBAC + Claims-Based (flexible scaling)"
  - "Encryption (Transit): HTTPS + TLS 1.3+ (Transport Layer)"
  - "Encryption (At-Rest): MVP not required (Phase 2 enhancement)"
  - "API Security: Security Headers + Per-User Rate Limiting"
  - "Access Token: JWT, 15-minute expiry (short-lived)"
  - "Refresh Token: HttpOnly Cookie, 7-day expiry (XSS-safe)"
  - "Idle Timeout: 30 minutes of inactivity forces re-login"
apiCommunicationDecisions:
  - "REST Design: Hybrid REST + RPC (resource + action endpoints)"
  - "Error Handling: ProblemDetails RFC 7807 (standardized)"
   - "Documentation: OpenAPI 3.1 + Swagger UI (auto-generated)"
   - "WebSocket Errors: Explicit Error Messages (structured, consistent)"
   - "API Versioning: URL Path /api/v1/ (explicit, backward compatible)"
infrastructureDeploymentDecisions:
   - "Hosting: Docker Compose (MVP) → Kubernetes (Phase 2/3)"
   - "Deployment: Self-hosted Linux servers (Ubuntu 22.04 LTS)"
   - "Container Registry: Docker Hub (public) + Self-hosted (private)"
   - "CI/CD: GitHub Actions + Docker Build + Push"
   - "Environment Config: .env ConfigMaps + Secrets"
   - "Monitoring: Prometheus 2.45+ + Grafana 10+"
   - "Logging (Phase 2): Structured JSON + Loki aggregation"
   - "Scaling Strategy: Single Server → Docker Swarm → Kubernetes"
   - "Load Balancing: Nginx reverse proxy (MVP) + Ingress (K8s)"
   - "Database Replication: PostgreSQL primary + 2 read replicas (Phase 2)"
   - "Health Checks: Built-in endpoint + Kubernetes liveness/readiness"
   - "Backup Strategy: Hourly PostgreSQL WAL + daily full backups"
   - "Secrets Rotation: Quarterly credentials, annual JWT secret"
   - "Load Testing Baseline: 500 req/sec, 100 WebSocket, 10 concurrent ops"
tools:
   - "Aspire CLI (primary)"
   - "dotnet new (fallback)"
   - "dotnet ef (migrations)"
   - "System.Threading.RateLimiting (built-in)"
   - "ASP.NET Core Identity (built-in)"
   - "Swashbuckle 6.5+ (OpenAPI/Swagger)"
   - "SignalR 8.0+ (WebSocket)"
   - "Vite (React build tool)"
   - "Zustand 4.5+ (state management)"
   - "TanStack Query 5.x (server state)"
   - "React Router v7 (routing)"
   - "TypeScript 5.x (strict mode)"
   - "Tailwind CSS (styling)"
   - "React Hook Form (form handling)"
partyModePanel:
  - "Winston (Architect)"
  - "Mary (Business Analyst)"
  - "Amelia (Developer)"
  - "Murat (Test Architect)"
---

# Architecture Document - bmadServer

**Author:** Cris  
**Date:** 2026-01-21  
**Status:** In Progress

---

## Architectural Context

### Project Overview
bmadServer transforms BMAD's CLI-dependent product formation workflows into a conversational, multi-user web application. The system orchestrates AI agents to guide users through structured product formation processes while maintaining full BMAD capability and enabling cross-device collaboration.

### Key Constraints
- **Backend Technology**: .NET 10 with ASP.NET Core
- **Framework**: Microsoft Aspire for cloud-native orchestration
- **Integration**: OpenCode server for BMAD agent interactions
- **Deployment**: Self-hosted as primary deployment model
- **Client**: Web-based chat interface with mobile responsiveness

---

## System Context

### Project Scope and Complexity

**Functional Requirements Overview:** 36 FRs organized into 8 capability areas:
1. **Workflow Orchestration (FR1-FR5):** Start, resume, track, and control BMAD workflows via chat
2. **Collaboration & Flow Preservation (FR6-FR11):** Multi-user contributions with safe checkpoints and decision locking
3. **Personas & Communication (FR12-FR15):** Business ↔ Technical language translation, adaptive responses
4. **Session & State Management (FR16-FR20):** Persistence across disconnects, history, recovery, exports
5. **Agent Collaboration (FR21-FR24):** Agent-to-agent messaging with human approval gates
6. **Parity & Compatibility (FR25-FR29):** Full BMAD workflow support, CLI-free operation
7. **Admin & Ops (FR30-FR34):** Health, access, provider routing, audit trails
8. **Integrations (FR35-FR36):** Webhooks, external tool notifications

**Non-Functional Requirements Summary:**

| Category | Requirement | Target |
|----------|-------------|--------|
| **Performance** | Chat UI ack / Agent start / Workflow step | 2s / 5s / 30s |
| **Reliability** | Uptime / Workflow success / Recovery | 99.5% / < 5% failures / 60s |
| **Security** | TLS / Encryption at rest / Audit logs | Yes / Yes / 90-day retention |
| **Scalability** | Concurrent users / workflows (MVP) | 25 users / 10 workflows |

**Project Scale Assessment:**

| Dimension | Assessment |
|-----------|------------|
| **Complexity Level** | **HIGH** - Multi-agent orchestration, real-time collaboration, state management |
| **Primary Domain** | **Full-stack** - Backend orchestration + real-time frontend + workflow engine |
| **Cross-Cutting Concerns** | Session management, authentication/RBAC, audit trails, error recovery, agent routing |
| **Real-time Features** | **Critical** - WebSocket streaming, live agent updates, collaborative buffers |
| **Data Complexity** | **High** - Workflow state graphs, decision trees, audit trails, multi-user inputs |
| **Integration Surface** | **Broad** - BMAD agents, webhooks, event streams, external tools |

### Critical Architectural Drivers

1. **Flow-Preserving Collaboration:** Multiple users in same workflow without breaking BMAD step sequence
2. **WebSocket-first Model:** Real-time chat + agent orchestration (not request-response polling)
3. **State Persistence Requirement:** Long-running workflows (30+ min) with mid-session recovery
4. **Language Parity:** No language exclusions; match BMAD's language support matrix
5. **Agent Orchestration:** Direct agent-to-agent messaging through system (not via user translation)
6. **Self-hosted Deployment:** Primary model with no hard external service dependencies
7. **BMAD Workflow Parity:** 100% capability match with existing BMAD workflows

---

## System Architecture Overview

### Conceptual Architecture Graph

The system can be understood as an interconnected network of six critical clusters:

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              bmadServer System                              │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Gateway Cluster               Orchestration Cluster      Collaboration     │
│  ────────────────               ──────────────────────     Cluster          │
│  • WebSocket Mgmt      ◄─────► • Workflow Engine   ◄─────► • Collab Buffer │
│  • Session Binding              • Agent Router              • Decision Track │
│  • Message Routing              • Step Tracking            • Conflict Detect│
│                                                                             │
│           │                              │                      │          │
│           │ Bidirectional               │ Command/Resp           │          │
│           │ Streaming                  │ + Events               │          │
│           │                              │                      │          │
│           └──────────────────────────────┼──────────────────────┘          │
│                                          ▼                                  │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                 Persona Translation Engine                           │   │
│  │            • Business ↔ Technical language                          │   │
│  │            • Context-aware responses                                │   │
│  │            • Role-based view filtering                              │   │
│  └──────────────────────────────────────┬─────────────────────────────┘   │
│                                         ▼                                   │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                    State Persistence Layer                           │   │
│  │   • PostgreSQL JSONB (hot state)                                   │   │
│  │   • Event log (decision audit trail)                               │   │
│  │   • Tiered storage (active → warm → cold)                          │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │              Agent Execution Layer (via OpenCode)                   │   │
│  │     PM Agent • Architect Agent • Dev Agent • Other Agents           │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Critical Dependency Analysis

Every system interaction follows this critical path:

```
User Action → WebSocket → Session Validation → State Lookup → Workflow Context 
→ Agent Routing → Agent Execution → State Update → Response
```

This 8-hop path represents both the system's power and its vulnerability. Each hop is a potential failure point. The system's blast radius from component failure must be carefully managed.

---

## Project Context Analysis (Advanced Elicitation Findings)

### Requirements and Constraints Synthesis

Through comprehensive multi-perspective analysis (Graph of Thoughts, Red Team-Blue Team, Architecture Decision Records, First Principles Analysis, and Pre-Mortem Analysis), the following architectural patterns and principles have been validated:

#### Pattern 1: Graph Representation

The system's architecture reveals six interconnected clusters with specific failure characteristics:

| Cluster | Components | Data Flow | Risk Level | Mitigation |
|---------|-----------|-----------|------------|-----------|
| **Gateway** | WebSocket ↔ Session ↔ Client | Bidirectional streaming | HIGH | Server-authoritative state, SignalR reconnection |
| **Orchestration** | Workflow ↔ Agent Router ↔ Agents | Command/Response + Events | HIGH | Shared workflow context, deadlock detection |
| **Collaboration** | Collab Buffer ↔ Decision Tracker | Buffered with checkpoints | MEDIUM | Never auto-merge, explicit conflict surfacing |
| **State** | All services ↔ Persistence Layer | Read/Write with consistency | HIGH | Optimistic concurrency, event log |
| **Translation** | Persona ↔ Workflow ↔ UI | Context-aware transformation | MEDIUM | Content classification, view filtering |
| **Agent** | Agents ↔ OpenCode ↔ Each other | Peer-to-peer via router | HIGH | Signed messages, message ordering |

#### Pattern 2: Failure Mode Prevention

Six critical failure modes have been identified and designed-against:

| Failure Mode | Architectural Prevention | Implementation Priority |
|--------------|-------------------------|------------------------|
| WebSocket reliability loss | SignalR + server-authoritative state | P0 - Core to MVP |
| Agent coordination breakdown | Shared workflow context + reasoning traces | P0 - Core to MVP |
| Collaboration conflicts | Optimistic concurrency + explicit conflicts | P0 - Core to MVP |
| Performance degradation at scale | Load testing + query optimization + streaming | P1 - Pre-launch |
| BMAD version drift | Workflow contract interface + parity testing | P1 - Pre-launch |
| Security incident | Audit logging + session validation | P0 - Core to MVP |

#### Pattern 3: First Principles Validation

Core architectural assumptions have been validated against fundamental truths:

1. **Users want decisions, not interfaces** — Build decision orchestration engine that could have multiple interfaces (chat is initial one)
2. **Coherent answers matter more than agent choreography** — User→Answer is primary flow; agent collaboration is optimization
3. **Only interactive feedback needs real-time** — Use WebSocket for chat/streaming, HTTP polling for status (reduces complexity)
4. **Equivalent outcomes trump identical execution** — Abstract workflow contracts from implementation (allows optimization)
5. **State persists while workflows are "active"** — Implement tiered storage: Hot (active) → Warm (30d) → Cold (90d)

---

## High-Level Components

### Component Architecture

Based on advanced architectural analysis, bmadServer consists of the following primary components:

#### 1. **Gateway & Session Management**
- SignalR WebSocket hub for real-time communication
- Session manager for user identity, authentication, and binding
- Connection heartbeat monitoring (60s interval)
- Graceful reconnection and session recovery

#### 2. **Workflow Orchestrator**
- BMAD workflow engine executing step-by-step processes
- Checkpoint management for pause/resume capability
- Step router directing to correct handler
- Execution context management with full history

#### 3. **Agent Router** (IAgentRouter Interface)
- Routes requests to appropriate BMAD agents
- Enforces message signing and authentication
- Deadlock detection for circular agent requests
- Timeout management (30s per request)
- MVP: In-process implementation; Future: Message queue compatible

#### 4. **Decision Tracker**
- Tracks all workflow decisions through lifecycle
- Implements optimistic concurrency control with version vectors
- Records decision lock/unlock events
- Maintains confidence levels and approval chain
- Audit trail of all decision mutations

#### 5. **Collaboration Manager**
- Buffers multi-user inputs at safe checkpoints
- Detects conflicting decision proposals
- Never auto-merges; surfaces conflicts explicitly
- Real-time WebSocket push for decision updates
- Clear ownership indicators per input

#### 6. **Persona Translation Engine**
- Business ↔ Technical language translation
- Context-aware response adaptation
- Content classification and view filtering
- Role-based access to technical depth
- Maintains semantic equivalence across translations

#### 7. **State Persistence Layer**
- **Hot Storage (PostgreSQL):** Active workflow state as JSONB documents
- **Event Log (PostgreSQL):** Append-only decision events for audit
- **Tiered Archive:** Warm storage for 30d inactive, cold storage for 90d+
- Transactional consistency for critical operations
- Event replay capability for state reconstruction

#### 8. **Reasoning Trace Logger**
- Captures agent decision-making context
- Logs full prompt, response, and confidence
- Records agent contradiction detection
- Enables debugging and user transparency
- Supports audit trail for compliance

---

## Architectural Decision Records (ADRs)

### ADR-001: State Persistence Strategy

**Decision: Hybrid Document Store + Event Log**

**Rationale:**
- Hot state (PostgreSQL JSONB): Fast queries, simple current-state access
- Event log (append-only): Complete audit trail, decision replay capability
- Tiered storage: Cost optimization for long-term retention

**Tradeoffs:**
- Simpler than full event sourcing (MVP-friendly)
- More auditable than document-only approach
- Requires discipline to keep history and state consistent

**Implementation:**
```sql
-- Primary state table
CREATE TABLE workflows (
    id UUID PRIMARY KEY,
    tenant_id VARCHAR(50) DEFAULT 'default',
    state JSONB NOT NULL,
    version BIGINT NOT NULL,
    updated_at TIMESTAMP NOT NULL,
    UNIQUE(id, version)
);

-- Event log for audit trail
CREATE TABLE workflow_events (
    id BIGSERIAL PRIMARY KEY,
    workflow_id UUID NOT NULL,
    event_type VARCHAR(100) NOT NULL,
    actor VARCHAR(100) NOT NULL,
    payload JSONB NOT NULL,
    timestamp TIMESTAMP NOT NULL,
    FOREIGN KEY (workflow_id) REFERENCES workflows(id)
);
```

---

### ADR-002: Agent Communication Pattern

**Decision: In-Process Mediator with Queue-Ready Interface**

**Rationale:**
- MVP needs fast time-to-market
- In-process provides sub-millisecond latency
- Interface abstraction enables future distribution
- Aligns with self-hosted, single-deployment model

**Tradeoffs:**
- Limited to single-process scaling initially
- Not suitable for multi-region deployment (pre-Phase 3)
- Requires careful memory management for concurrent agents

**Implementation:**
```csharp
// Abstraction layer for future flexibility
public interface IAgentRouter {
    Task<AgentResponse> RouteAsync(string agentId, AgentRequest request, TimeSpan timeout = default);
    Task BroadcastAsync(AgentEvent @event);
}

// MVP Implementation: In-process mediator
public class InProcessAgentRouter : IAgentRouter {
    private readonly IAgentRegistry _registry;
    private readonly IDeadlockDetector _deadlockDetector;
    private readonly IReasoningTraceLogger _traceLogger;
    
    public async Task<AgentResponse> RouteAsync(string agentId, AgentRequest request, TimeSpan timeout = default) {
        // Deadlock detection
        if (_deadlockDetector.WouldCreateCycle(agentId))
            throw new DeadlockDetectedException();
        
        // Message signing
        request.Signature = SignMessage(request);
        
        // Execution with timeout
        using var cts = new CancellationTokenSource(timeout);
        var response = await _registry.ExecuteAsync(agentId, request, cts.Token);
        
        // Reasoning trace logging
        _traceLogger.LogResponse(agentId, request, response);
        
        return response;
    }
}

// Future Implementation: Message queue based (Phase 2+)
public class ServiceBusAgentRouter : IAgentRouter {
    // Same interface, different backend
}
```

---

### ADR-003: Real-Time Communication Protocol

**Decision: SignalR**

**Rationale:**
- Battle-tested WebSocket + fallback implementation
- Automatic reconnection handling (critical for mobile users)
- Native .NET integration with ASP.NET Core
- Handles edge cases like connection drop + quick rejoin

**Tradeoffs:**
- Less control than raw WebSocket
- Slight performance overhead vs. custom protocol
- Dependency on Microsoft library

**Implementation Strategy:**
- SignalR Hub for interactive real-time (chat, streaming)
- HTTP polling for non-interactive updates (reduces WebSocket load)
- Explicit "Connection Quality" UI indicator
- Offline message queuing on client (send when reconnected)

---

### ADR-004: Multi-Tenancy Model

**Decision: Single-Tenant MVP, Tenant-Ready Schema**

**Rationale:**
- MVP is self-hosted dogfooding (only 1 tenant)
- Phase 3 multi-tenancy requires design but not implementation
- Preparing schema now prevents major refactoring later

**Tradeoffs:**
- Extra column everywhere (minor overhead)
- Not fully isolated (single schema for all tenants)
- Must add isolation machinery for multi-tenant phase

**Implementation:**
```sql
-- All tables include tenant_id, defaulting to 'default' for MVP
CREATE TABLE workflows (
    id UUID PRIMARY KEY,
    tenant_id VARCHAR(50) DEFAULT 'default' NOT NULL,
    -- ... other columns
);

CREATE INDEX idx_workflows_tenant ON workflows(tenant_id);

-- Future: Add row-level security policies for multi-tenant
-- CREATE POLICY workflows_isolation ON workflows 
--   USING (tenant_id = current_setting('app.current_tenant'));
```

---

## Starter Template Evaluation

### Primary Technology Domain

**Identified:** Full-Stack Microservices Architecture with Real-Time Communication

Project classification:
- Backend: ASP.NET Core (.NET 10) with SignalR for real-time chat orchestration
- Framework: Microsoft Aspire for cloud-native service orchestration
- Integration: OpenCode server for BMAD agent interactions  
- Real-time: WebSocket-based chat + event streaming
- Deployment: Self-hosted Docker containers with unified orchestration

### Starter Template Options Evaluated

#### Option 1: ASP.NET Core Web API (Simple Foundation)
```bash
dotnet new webapi -o bmadServer -f net10.0
```

**Pros:** Minimal, clean foundation; full control; fast start

**Cons:** No orchestration; requires manual Aspire integration; missing patterns

#### Option 2: .NET Aspire Starter App (RECOMMENDED) ⭐
```bash
# Via Aspire CLI (PREFERRED METHOD)
aspire new aspire-starter --name bmadServer --output ./

# Or via dotnet new (requires manual template installation)
dotnet new install Aspire.ProjectTemplates
dotnet new aspire-starter -o bmadServer -f net10.0
```

**Pros:**
- ✅ Out-of-the-box service orchestration (core requirement)
- ✅ Multi-project structure for distributed components
- ✅ Docker support and cloud-native patterns built-in
- ✅ Distributed tracing and health checks
- ✅ Service communication patterns pre-established
- ✅ Perfect for Phase 2+ scaling

**Cons:** Aspire is newer (but stable); fewer StackOverflow examples

#### Option 3: ASP.NET Core Empty (Minimal Approach)
```bash
dotnet new web -o bmadServer -f net10.0
```

**Pros:** Maximum flexibility; smallest surface area

**Cons:** Build everything from scratch; most time investment

### Selected Starter: .NET Aspire Starter App

**Rationale for Selection:**

The **.NET Aspire Starter** template is optimal for bmadServer because:

1. **Aspire Orchestration is Your Foundation** — Your ADR-002 decision (in-process agent routing) benefits from Aspire's service discovery and orchestration patterns, which would otherwise require significant custom development.

2. **Multi-Service Architecture Ready** — Template provides:
   - AppHost project (unified orchestration)
   - Main API service (extensible)
   - ServiceDefaults project (shared resilience/telemetry)
   - This structure aligns perfectly with multi-agent orchestration requirements

3. **Cloud-Native from Day 1** — Aspire is designed for resilience, self-hosted deployments, and graceful scaling—exactly your deployment model.

4. **SignalR Integration Path** — Adding SignalR is a single NuGet package. The architecture supports real-time communication seamlessly.

5. **Reduces Architectural Boilerplate** — You've done the hard thinking. The template handles scaffolding so the team focuses on unique business logic.

6. **Future-Proof** — When transitioning to message queues or distributed agents (Phase 2+), Aspire's foundation supports it without major refactoring.

### Installation & Project Creation

**RECOMMENDED: Use Aspire CLI**

```bash
# Install Aspire CLI (one-time setup)
# macOS/Linux:
curl -sSL https://aspire.dev/install.sh | bash

# Windows (PowerShell):
irm https://aspire.dev/install.ps1 | iex

# Verify installation
aspire --version

# Create new Aspire Starter project
aspire new aspire-starter --name bmadServer --output ./projects
cd bmadServer
```

**Alternative: Using dotnet new**

```bash
# Install Aspire templates once
dotnet new install Aspire.ProjectTemplates::13.1.0

# Create project
dotnet new aspire-starter -o bmadServer -f net10.0
cd bmadServer
```

### Project Structure Established by Starter

```
bmadServer/
├── bmadServer.AppHost/                    # Service orchestration & startup
│   ├── Program.cs                         # Configures all services + resources
│   ├── appsettings.json                   # AppHost configuration
│   └── Properties/launchSettings.json     # Debug profiles
│
├── bmadServer.ApiService/                 # Main API (add SignalR hub here)
│   ├── Program.cs                         # Service configuration + middleware
│   ├── appsettings.json                   # Service-specific config
│   └── Properties/launchSettings.json     # Debug profiles
│
├── bmadServer.ServiceDefaults/            # Shared conventions & defaults
│   ├── Extensions.cs                      # Resilience, telemetry setup
│   └── (Shared configuration patterns)
│
└── Directory.Build.props                   # Solution-wide MSBuild settings
```

### Architectural Decisions Established by Starter

#### Language & Runtime
- **Language:** C# 13 with nullable reference types enabled
- **Runtime:** .NET 10 (latest standard term support)
- **Target Framework:** net10.0
- **Web Server:** ASP.NET Core Kestrel (built-in)

#### Service Orchestration (Aspire Specific)
- **AppHost Pattern:** Unified service configuration and startup
- **Service Discovery:** Built-in DNS resolution between services
- **Health Checks:** Pre-configured per service
- **Telemetry:** Structured logging + distributed tracing ready
- **Container Support:** Docker orchestration out-of-the-box

#### Development Experience
- **Hot Reload:** Enabled for rapid iteration
- **Debugging:** Full .NET debugger integration
- **Telemetry Dashboard:** Aspire dashboard (https://localhost:17360 by default)
- **VS Code Integration:** launch.json and tasks.json included

#### Build & Package Management
- **Build System:** MSBuild (dotnet build)
- **Package Manager:** NuGet (integrated)
- **Dependency Management:** Centralized via .csproj files

#### Project Organization
```
bmadServer.ApiService/
├── Program.cs                             # All service configuration
├── appsettings.json                       # Configuration values
├── appsettings.Development.json           # Dev overrides
├── Properties/
│   └── launchSettings.json                # Launch profiles
└── (Your business logic code)
```

### Essential Aspire NuGet Packages

**PREFERENCE: Use Aspire-specific packages for consistency:**

| Package | Version | Purpose |
|---------|---------|---------|
| `Aspire.Hosting` | 13.1.0 | AppHost project hosting |
| `Aspire.Hosting.AppHost` | 13.1.0 | AppHost SDK |
| `Aspire.AppHost.Sdk` | 13.1.0 | AppHost build support |
| `Aspire.Hosting.Testing` | 13.1.0 | Integration testing support |
| `Microsoft.AspNetCore.SignalR` | (latest) | Real-time communication |

**Adding SignalR to the API Service:**

```bash
cd bmadServer/bmadServer.ApiService
dotnet add package Microsoft.AspNetCore.SignalR
```

Then in Program.cs:
```csharp
builder.Services.AddSignalR();
app.MapHub<WorkflowHub>("/workflowhub");
```

### Integration with Your Architecture

| Your Decision | How Starter Supports It |
|---------------|------------------------|
| **In-Process Agent Router** | AppHost + single service = natural container for MediatR mediator |
| **SignalR Real-Time** | ApiService ready for SignalR hub integration |
| **PostgreSQL State Persistence** | ServiceDefaults handles connection pooling patterns |
| **Session Management** | Middleware patterns established in Program.cs |
| **Self-Hosted Deployment** | AppHost designed specifically for self-hosted scenarios |
| **Future Phase 2 Scaling** | AppHost architecture supports multi-service without refactoring |

### First Implementation Story

**Story Title:** Initialize bmadServer from Aspire Starter template

**Acceptance Criteria:**
- ✅ Project created from aspire-starter template using Aspire CLI
- ✅ Solution compiles without errors (dotnet build)
- ✅ AppHost starts successfully (aspire run)
- ✅ AppHost dashboard accessible at https://localhost:17360
- ✅ ApiService starts and responds to HTTP requests
- ✅ SignalR package installed and ready to import
- ✅ Team can understand project structure and conventions
- ✅ All Aspire NuGet packages at version 13.1.0 or later

**Implementation Steps:**
```bash
# 1. Install Aspire CLI
curl -sSL https://aspire.dev/install.sh | bash

# 2. Create project from template
aspire new aspire-starter --name bmadServer --output ./

# 3. Add SignalR for real-time communication
cd bmadServer/bmadServer.ApiService
dotnet add package Microsoft.AspNetCore.SignalR

# 4. Verify everything compiles
cd ../../
dotnet build

# 5. Run and verify AppHost dashboard
aspire run

# Expected: Dashboard appears at https://localhost:17360
```

---

## Technology Stack

### Backend (.NET 10 + ASP.NET Core)

| Layer | Technology | Rationale |
|-------|-----------|-----------|
| **Web Server** | ASP.NET Core Kestrel | Native, performant, integrated |
| **Real-time** | SignalR | Reliable WebSocket + fallback |
| **API** | REST (HTTP) + SignalR | REST for stateless ops, SignalR for real-time |
| **Workflow Engine** | Custom (BMAD adapter layer) | Full BMAD parity required |
| **Messaging** | In-process MediatR (MVP) | Fast, simple for single deployment |
| **State Management** | PostgreSQL (JSONB) + Event Log | Persistent, queryable, auditable |
| **Caching** | Redis (optional, Phase 2) | Performance optimization if needed |
| **Logging** | Serilog + ELK Stack | Structured logging for debugging |

### Frontend (Web, Mobile-Responsive)

| Layer | Technology | Rationale |
|-------|-----------|-----------|
| **Framework** | React or Vue.js | Component-based, responsive |
| **Real-time** | SignalR client library | Matches backend, automatic reconnect |
| **UI Framework** | Tailwind CSS or Material-UI | Rapid styling, accessibility |
| **State** | Redux or Pinia | Manage session, workflow state |
| **Markdown Rendering** | React Markdown | Display agent responses |
| **Code Blocks** | Prism.js or Highlight.js | Syntax highlighting |

### Infrastructure (Self-hosted)

| Component | Technology | Rationale |
|-----------|-----------|-----------|
| **Container** | Docker | Self-contained deployment |
| **Orchestration** | Docker Compose (MVP) / Kubernetes (Phase 2) | Simple to complex progression |
| **Database** | PostgreSQL 14+ | JSONB support, reliability |
| **Persistence** | Volume mounts | Data survives container restart |
| **Backup** | PostgreSQL backup tools | Daily snapshots recommended |

---

## Infrastructure Architecture

### Deployment Architecture

**MVP Single-Host Deployment:**

```
┌─────────────────────────────────────────────────────────────┐
│                    Single Server (Self-Hosted)               │
│                                                              │
│  ┌──────────────────────────────────────────────────────┐   │
│  │           Docker/Container Runtime                   │   │
│  │                                                      │   │
│  │  ┌────────────────┐      ┌────────────────┐        │   │
│  │  │ bmadServer App │◄────►│  PostgreSQL    │        │   │
│  │  │ (ASP.NET Core) │      │  (State + Log) │        │   │
│  │  │                │      │                │        │   │
│  │  │ • SignalR Hub  │      │ • Workflows    │        │   │
│  │  │ • API Layer    │      │ • Decisions    │        │   │
│  │  │ • Orchestrator │      │ • Audit Log    │        │   │
│  │  │ • Agent Router │      │ • Archives     │        │   │
│  │  └────────────────┘      └────────────────┘        │   │
│  │            │                      │                 │   │
│  │            └──────────┬───────────┘                 │   │
│  │                       │                             │   │
│  │            ┌──────────▼──────────┐                 │   │
│  │            │  Shared Volume      │                 │   │
│  │            │  • Workflow files   │                 │   │
│  │            │  • Artifacts        │                 │   │
│  │            │  • Logs             │                 │   │
│  │            └─────────────────────┘                 │   │
│  └──────────────────────────────────────────────────────┘   │
│                          │                                   │
│                          ▼                                   │
│  ┌──────────────────────────────────────────────────────┐   │
│  │      Host Network (localhost:3000, 5432, etc.)      │   │
│  └──────────────────────────────────────────────────────┘   │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

---

## Security Architecture

### Security Layers

#### Layer 1: Network Transport
- **TLS/SSL encryption** for all traffic in transit
- Self-signed certificates acceptable for self-hosted
- HTTPS enforcement at reverse proxy (nginx)

#### Layer 2: Authentication & Authorization
- **Session-based authentication** on WebSocket connect
- **60-second heartbeat validation** (re-auth checkpoint)
- **Graceful session refresh** without disconnection
- **Role-based access control (RBAC)** for content visibility

#### Layer 3: Message Security
- **HMAC message signing** for agent-originated messages
- **Timestamp validation** (prevent replay attacks)
- **Nonce tracking** (prevent duplicate requests)

#### Layer 4: Data Access Control
- **Row-level security** by tenant_id (schema prepared, not enforced in MVP)
- **User context injection** into all database queries
- **Persona-based content filtering** (business vs technical views)

#### Layer 5: Audit & Compliance
- **Comprehensive audit logging:** Who, What, When, Where, Why
- **90-day log retention** (configurable)
- **Full decision provenance** with actor attribution
- **Incident response capability** (logs support forensics)

### Security Checklist (Pre-Launch)

- [ ] TLS certificates configured
- [ ] Session validation on every WebSocket message
- [ ] Agent message signing implemented and validated
- [ ] Audit logging captures all state mutations
- [ ] Rate limiting configured at gateway
- [ ] Secrets management (API keys, DB credentials) secured
- [ ] SQL injection prevention via parameterized queries
- [ ] CORS properly configured
- [ ] Sensitive data not logged (passwords, tokens)
- [ ] Penetration testing completed

---

## Data Architecture

### State Management Strategy

**Principle:** Server is the single source of truth; client is a view layer.

#### Workflow State Structure

```json
{
  "workflow_id": "uuid",
  "tenant_id": "default",
  "status": "in_progress",
  "current_step": 5,
  "total_steps": 12,
  "version": 42,
  "workflow_type": "prd",
  "participants": [
    { "user_id": "sarah", "role": "business", "last_active": "2026-01-23T10:30:00Z" },
    { "user_id": "marcus", "role": "technical", "last_active": "2026-01-23T10:25:00Z" }
  ],
  "decisions": [
    {
      "id": "dec-001",
      "proposal": "Use JWT for authentication",
      "proposed_by": "marcus",
      "status": "locked",
      "confidence": 0.95,
      "locked_at": "2026-01-23T10:15:00Z",
      "locked_by": "sarah"
    }
  ],
  "context": {
    "bmad_version": "6.0.0",
    "workflow_parity_check": "full_match",
    "last_checkpoint": "step_4_complete"
  },
  "metadata": {
    "created_at": "2026-01-20T14:00:00Z",
    "updated_at": "2026-01-23T10:30:00Z",
    "session_resume_count": 3
  }
}
```

#### Event Log Structure

```json
{
  "id": 12345,
  "workflow_id": "uuid",
  "event_type": "decision_locked",
  "actor": "sarah",
  "timestamp": "2026-01-23T10:15:00Z",
  "payload": {
    "decision_id": "dec-001",
    "decision_text": "Use JWT for authentication",
    "confidence": 0.95,
    "approval_rationale": "Aligns with security requirements"
  }
}
```

### Data Flow Patterns

#### Pattern 1: Decision Proposal → Collaboration → Lock

```
User A proposes decision
    ↓
System creates decision object, version=1
    ↓
Broadcast to all participants via WebSocket
    ↓
User B reviews in collaboration buffer
    ↓
Conflict detection (if User A modifies meanwhile)
    ↓
User B approves (version check: still v1?)
    ↓
Decision marked "locked"
    ↓
Event logged with full provenance
    ↓
Broadcast lock confirmation
```

#### Pattern 2: Workflow Checkpoint → Session Resume

```
User completes step 5 of 12
    ↓
System writes checkpoint to workflow state
    ↓
Event logged: "checkpoint_created"
    ↓
User closes browser, session expires
    ↓
Days later: User logs back in
    ↓
System loads workflow state from checkpoint
    ↓
Displays: "You were at Step 5. Continue?" 
    ↓
User resumes, system increments step counter
```

#### Pattern 3: Agent Response → Reasoning Trace

```
Workflow requests response from Architect Agent
    ↓
Agent receives full context (prior decisions, user intent)
    ↓
Agent generates response
    ↓
Reasoning trace captured: prompt + response + confidence
    ↓
Response checked for contradictions with prior decisions
    ↓
Confidence score applied
    ↓
Response streamed to user via WebSocket
    ↓
Reasoning trace stored in event log for audit
```

---

## Risk Assessment

### Identified Risks and Mitigations

#### P0 Risks (Must Address for MVP)

| Risk | Impact | Likelihood | Mitigation | Owner |
|------|--------|-----------|-----------|-------|
| **WebSocket Connection Loss** | User loses context, workflow interrupted | MEDIUM | SignalR reconnection + server-authoritative state + session recovery | Backend |
| **Agent Coordination Breakdown** | Agents give contradictory guidance, user loses trust | MEDIUM | Shared workflow context + contradiction detection + reasoning traces | Agent Router |
| **State Corruption via Race Condition** | Decision integrity destroyed, audit trail corrupted | LOW | Optimistic concurrency control (version vectors) + real-time sync | State Mgmt |
| **Security Breach** | Sensitive product decisions leaked | LOW-MEDIUM | Audit logging + session validation + message signing | Security |

#### P1 Risks (Address Pre-Launch)

| Risk | Impact | Likelihood | Mitigation | Owner |
|------|--------|-----------|-----------|-------|
| **Performance Degradation at Scale** | System becomes unusable with concurrent users | MEDIUM | Load testing at 2x scale + query optimization + connection pooling | DevOps |
| **BMAD Workflow Parity Drift** | Users discover features in CLI that don't work in bmadServer | MEDIUM | Workflow contract interface + parity testing in CI + version matrix | Architect |
| **Agent Deadlock** | Two agents wait for each other, workflow hangs | LOW | Timeout (30s) + deadlock detection + circular dependency tracking | Agent Router |
| **Database Connection Pool Exhaustion** | Cannot serve new requests | LOW | Connection limits + idle timeout + monitoring + alerts | DevOps |

#### P2 Risks (Monitor Post-Launch)

| Risk | Impact | Likelihood | Mitigation | Owner |
|------|--------|-----------|-----------|-------|
| **Storage Growth** | Event log and audit trail consume unlimited disk | MEDIUM | Tiered storage strategy (warm/cold) + retention policies | DevOps |
| **Operator Fatigue** | Repeated manual interventions required | MEDIUM | Automated recovery procedures + incident playbooks | DevOps |

---

## Implementation Strategy

### MVP Phase (Weeks 1-8)

**Definition:** First complete BMAD workflow through web interface with all critical P0 risks mitigated.

**Success Criteria:**
1. End-to-end workflow (PRD, Architecture, or Epics) completes without terminal
2. Multi-user collaboration works (Sarah + Marcus can collaborate without conflicts)
3. Workflow state persists across browser refreshes and reconnects
4. 95% workflow completion rate under normal conditions

**Priority Implementation Order:**
1. **Weeks 1-2:** Backend skeleton (ASP.NET Core + SignalR + PostgreSQL)
2. **Weeks 2-3:** Session & workflow state management
3. **Weeks 3-4:** Agent router & BMAD integration
4. **Weeks 4-5:** Frontend (React chat interface)
5. **Weeks 5-6:** Collaboration & decision tracking
6. **Weeks 6-7:** Error handling & recovery
7. **Week 7-8:** Testing, hardening, deployment

### Phase 2 (Post-MVP, Weeks 9-16)

- Performance optimization (caching, query optimization)
- Workflow visualization & progress map
- Integrations (GitHub, Slack, Jira webhooks)
- Event stream for external tools
- Audit trail UI

### Phase 3+ (Expansion)

- Multi-tenancy implementation
- BMAD-as-a-Service for external teams
- Workflow marketplace
- AI-powered optimization suggestions

---

## Decision Log

### D-001: Architecture Pattern for Multi-Agent Orchestration
**Decision:** Graph-based component architecture with six interconnected clusters (Gateway, Orchestration, Collaboration, State, Translation, Agent)
**Rationale:** Allows analysis of dependencies, failure modes, and recovery paths
**Date:** 2026-01-23

### D-002: State Persistence
**Decision:** Hybrid Document Store + Event Log (ADR-001)
**Rationale:** Balances MVP simplicity with audit trail requirements
**Date:** 2026-01-23

### D-003: Agent Communication
**Decision:** In-Process Mediator with Queue-Ready Interface (ADR-002)
**Rationale:** Fast MVP development with future distribution capability
**Date:** 2026-01-23

### D-004: Real-Time Protocol
**Decision:** SignalR (ADR-003)
**Rationale:** Battle-tested, native .NET integration, excellent reconnection handling
**Date:** 2026-01-23

### D-005: Multi-Tenancy
**Decision:** Single-Tenant MVP, Tenant-Ready Schema (ADR-004)
**Rationale:** MVP doesn't need multi-tenancy, but schema prepared for Phase 3
**Date:** 2026-01-23

---

## Technology Stack

*[Detailed technology decisions to be made through architectural discussion...]*

---

## Infrastructure Architecture

*[Infrastructure and deployment architecture to be designed...]*

---

## Security Architecture

*[Security considerations and implementation to be defined...]*

---

## Core Architectural Decisions (Step 4)

### Category 1: Data Architecture

#### Decision 1.1: Data Modeling Approach

**Decision:** Hybrid Model (EF Core + JSONB)

**Details:**
- Core entities (Users, Sessions, Workflow metadata) managed as strongly-typed EF Core models
- Workflow state and dynamic content stored as PostgreSQL JSONB documents
- EF Core handles relationships; application code manages JSONB serialization/validation

**Version/Technology:**
- Entity Framework Core 9.0 (STS release, supported until November 2026)
- PostgreSQL JSONB data type with binary format optimization

**Rationale:**
1. ✅ Type safety for stable entities (Users, Sessions) prevents common bugs
2. ✅ Schema flexibility for evolving BMAD workflows (no migration required for new workflow types)
3. ✅ Aligns with existing "Hybrid Document Store + Event Log" architecture decision
4. ✅ Reduces friction for rapidly changing product formation workflows
5. ✅ Intermediate complexity – not over-engineering with full CQRS yet

**Implications:**
- Entity relationships must be managed via EF Core foreign keys
- Workflow state queries require JSONB operators (`->`, `@>`, etc.)
- Serialization/deserialization responsibility falls to application layer
- Requires careful JSONB validation before persistence

**Affected Components:**
- Session Manager (session data stored as JSONB)
- Workflow Orchestrator (workflow state stored as JSONB)
- Collaboration Manager (decision tracking with JSONB payloads)
- Data Access Layer (DbContext configuration)

---

#### Decision 1.2: Data Validation Strategy

**Decision:** Hybrid Validation (EF Core Annotations + FluentValidation)

**Details:**
- **EF Core Data Annotations:** `[Required]`, `[StringLength]`, `[MaxLength]` for database-level constraints
- **FluentValidation:** Business rule validation (workflow-specific rules, complex conditions)
- **JSONB Validation:** Custom application-layer validation before persisting document state

**Version/Technology:**
- FluentValidation 11.9.2 (stable, .NET 8/9 compatible)
- Built-in EF Core Data Annotations
- Custom validators for JSONB schemas

**Rationale:**
1. ✅ Database integrity enforced at schema level (fast, reliable)
2. ✅ Complex BMAD workflow rules live in FluentValidation (flexible, testable, reusable)
3. ✅ JSONB state validated before touching database (prevent invalid state storage)
4. ✅ Separates concerns: DB constraints vs. business logic
5. ✅ Easy to test; validators can run without database

**Validation Sequence:**
1. Input data arrives at API endpoint
2. FluentValidation runs business rules
3. Valid data passes to EF Core models
4. EF Core constraints checked before Save
5. JSONB documents validated by custom validators
6. Database writes only fully validated state

**Affected Components:**
- Session Manager (validate session data)
- Workflow Orchestrator (validate workflow state transitions)
- Collaboration Manager (validate decision payloads)
- API Controllers (request validation)

---

#### Decision 1.3: Database Migration Strategy

**Decision:** EF Core Migrations with Local Testing Gate

**Details:**
- Use `dotnet ef migrations add {MigrationName}` to generate versioned migration files
- All migrations version-controlled in `/Data/Migrations` directory
- Require local testing before applying to shared environments
- Aspire AppHost can conditionally run migrations on startup (with safeguards)
- Manual `dotnet ef database update` in production after review

**Version/Technology:**
- Entity Framework Core 9.0 CLI tools (`dotnet ef`)
- PostgreSQL-specific migrations (generated by EF Core provider)
- Version control (Git) for all migration files

**Rationale:**
1. ✅ Integrated with .NET Aspire stack (no external migration tools)
2. ✅ Reversible (rollback via `dotnet ef migrations remove`)
3. ✅ Version-controlled for audit trail
4. ✅ Balances MVP velocity with safety gates
5. ✅ Prevents accidental schema changes to production

**Migration Process:**
1. Developer makes model changes locally
2. Generate migration: `dotnet ef migrations add AddWorkflowStatusColumn`
3. Review generated SQL in migration file
4. Test locally: `dotnet ef database update`
5. Commit migration to Git
6. CI/CD picks up, runs tests
7. Production: Manual review + `dotnet ef database update` with backup

**Affected Components:**
- Data Access Layer (DbContext)
- All entities requiring schema changes
- Deployment pipeline (migration execution step)

---

#### Decision 1.4: Caching Strategy

**Decision:** In-Process Memory Cache (MVP), Redis-Ready Interface

**Details:**
- Use IMemoryCache for session metadata, workflow templates, agent response cache
- No external Redis instance for MVP deployment
- Design cache abstraction layer (IDistributedCache) to support future Redis upgrade
- Single-instance self-hosted deployment (no cross-instance cache sharing needed initially)

**Version/Technology:**
- Microsoft.Extensions.Caching.Memory (built-in, latest)
- IDistributedCache interface (prepared for Aspire.StackExchange.Redis.DistributedCaching v13.1.0 later)
- Cache expiration: TTL-based (configurable per entity type)

**Rationale:**
1. ✅ Zero external dependencies (simplifies MVP deployment)
2. ✅ Fast access times for frequently requested data (metadata, templates)
3. ✅ Self-hosted primary deployment (single instance)
4. ✅ Easy upgrade path to Redis when scaling (interface-based abstraction)
5. ✅ Reduces database load for read-heavy operations (session lookup, workflow metadata)

**Caching Scope (MVP):**
- **Session metadata:** Last active time, user preferences, current workflow context (TTL: 5 min)
- **Workflow templates:** BMAD workflow definitions, step templates (TTL: 1 hour, refresh on config change)
- **Agent registry:** Available agents, capabilities, current status (TTL: 2 min)
- **Decision history:** Recent decisions in current session (TTL: session lifetime)

**Cache Invalidation:**
- Time-based expiration (TTL)
- Event-based invalidation (when workflow state changes)
- Manual flush endpoint for admin operations

**Future Redis Upgrade (Phase 2):**
- Replace IMemoryCache with IDistributedCache implementation
- Add Aspire Redis resource to AppHost
- No application code changes (interface-based abstraction)

**Affected Components:**
- Session Manager (cache session metadata)
- Workflow Orchestrator (cache workflow templates and state)
- Agent Router (cache agent registry and capabilities)
- Performance optimization layer (new cache service)

---

#### Decision 1.5: Cascading Implications

**Impacts on Other Architectural Decisions:**

| Decision | Impact | Effect |
|----------|--------|--------|
| **Authentication** | User identity required in Session cache | See Category 2 |
| **API Design** | Cache headers in HTTP responses | See Category 3 |
| **Frontend State** | Workflow state synced from JSONB cache | See Category 4 |
| **Monitoring** | Cache hit/miss metrics, DB connection pool monitoring | See Category 5 |
| **Data Retention** | Event log growth + JSONB size = storage planning | See Category 5 |

---

#### Panel Review & Recommendations (Party Mode)

**Review Date:** 2026-01-23  
**Panel:** Winston (Architect), Mary (Business Analyst), Amelia (Developer), Murat (Test Architect)  
**Panel Verdict:** ✅ APPROVED with yellow flag enhancements

**Critical Enhancements from Panel:**

##### P0 Priority Recommendations

**1. JSONB Concurrency Control (Mary + Winston)**
- **Issue:** Multi-user collaboration without conflict detection = data loss risk
- **Recommendation:** Add three fields to ALL workflow JSONB state documents:
  - `_version` (integer) - Increment on every update
  - `_lastModifiedBy` (string) - User ID who made the change
  - `_lastModifiedAt` (ISO 8601 timestamp) - When the change occurred
- **Implementation:** Use optimistic concurrency pattern (check version on update, reject if changed)
- **Impact:** Prevents Sarah's decision from overwriting Marcus's simultaneous change
- **Status:** **LOCKED - Implement before first implementation**

**2. JSON Serializer Selection (Amelia)**
- **Decision:** Use **System.Text.Json** (default .NET 9 serializer)
- **Rationale:** Faster performance, native to .NET ecosystem, built-in configuration
- **Implementation:** Configure custom converters for workflow state JSONB documents
- **Fallback:** Newtonsoft.Json only if non-ASCII language support requires it (Mary flagged for testing)
- **Status:** **LOCKED - Configure in Program.cs before first migration**

**3. FluentValidation Middleware Setup (Amelia)**
- **Issue:** If using ASP.NET Core Minimal APIs (typical for Aspire), FluentValidation needs explicit middleware
- **Recommendation:** Add `.AddFluentValidation()` + custom validation filter to all endpoints
- **Not Automatic:** Unlike Controller-based APIs, Minimal APIs require manual setup
- **Status:** **LOCKED - Part of API layer configuration**

##### P1 Priority Recommendations

**4. JSONB Schema Versioning (Murat + Amelia)**
- **Issue:** Workflow state evolves. Workflows from sprint 1 have different schemas than sprint 10
- **Recommendation:** Add `_schemaVersion` (e.g., "1.0.0") to every JSONB document
- **Migration Strategy:** Write upgrade logic for schema version transitions
- **Testing:** Schema migration tests in CI (migrate up + down + verify)
- **Status:** **First Migration** - Implement in migration 001
- **Note:** Option A (explicit schema version). Option B (Event Sourcing) deferred to Phase 2.

**5. GIN Indexes on JSONB (Winston + Murat)**
- **Issue:** At 100+ concurrent workflows, JSONB queries without indexes hit 200-500ms latency
- **Recommendation:** Add PostgreSQL GIN (Generalized Inverted Index) to frequently-queried JSONB columns
- **Performance Impact:** Queries drop from 200-500ms to 5-20ms (40x improvement)
- **Cost:** Negligible index creation time on empty database (do it first)
- **SQL Pattern:** `CREATE INDEX idx_workflow_state ON workflows USING GIN(state jsonb_ops);`
- **Status:** **First Migration** - Add indexes in migration 001

**6. EF Core Migration Execution Strategy (Amelia)**
- **Development:** Aspire AppHost runs migrations automatically on startup
- **Production:** Manual `dotnet ef database update` after review + backup verification
- **Implementation:** Conditional migration execution in Program.cs (if Development environment)
- **Rollback Plan:** Every migration must have reversible logic (test with `dotnet ef migrations remove`)
- **Status:** **Deployment Architecture** - Lock this in Category 5

**7. Testing Strategy for Data Layer (Murat)**
- **Unit Tests:** 100% coverage for FluentValidation rules (easy, fast)
- **Integration Tests:** JSONB schema validation tests in PostgreSQL (slower, critical)
- **Migration Tests:** Every migration has up + down test (CI verifies reversibility)
- **Concurrency Tests:** Simulate simultaneous JSONB updates, verify version field prevents conflicts
- **Status:** **Quality Gate** - Add to CI/CD pipeline design

##### P2 Recommendations (Monitor Post-MVP)

**8. Non-ASCII Character Support (Mary)**
- **Concern:** BMAD supports multiple languages (Arabic, Chinese, Russian, etc.)
- **Action:** Verify System.Text.Json handles non-ASCII characters correctly in workflow state
- **Fallback:** If issues arise, evaluate Newtonsoft.Json (more forgiving with Unicode)
- **Timeline:** Test during sprint 2, before shipping to international users

**9. Event Log + JSONB Dual Write Atomicity (Winston)**
- **Concern:** If event log write succeeds but JSONB update fails (or vice versa), state is inconsistent
- **Recommendation:** Use PostgreSQL transactions to ensure atomicity (both succeed or both rollback)
- **Implementation:** Wrap both operations in `BeginTransaction()` / `CommitAsync()`
- **Timeline:** Implement in first backend sprint, before shipping to multiple users

---

### Data Architecture Summary Table

| Aspect | Decision | Version | Rationale |
|--------|----------|---------|-----------|
| **Data Modeling** | Hybrid (EF Core + JSONB) | EF Core 9.0 | Type safety + flexibility |
| **Validation** | EF Annotations + FluentValidation | FluentValidation 11.9.2 | Separation of concerns |
| **Migrations** | EF Core Migrations with testing gate | EF Core 9.0 CLI | Safety + reversibility |
| **Caching** | In-Process (IMemoryCache) | Built-in, Redis-ready | MVP simplicity + scalability path |
| **Database** | PostgreSQL + Event Log | 17.x LTS (incremental VACUUM) | Performance + audit trail |

---

## Data Architecture

*[Data flow, storage, and persistence patterns established in Category 1 decisions above]*

---

### Category 2: Authentication & Security

#### Decision 2.1: Authentication Method

**Decision:** Hybrid Approach (Local Database + OpenID Connect Ready)

**Details:**
- **MVP Phase:** Local user database with session-based authentication
- Users/passwords stored in PostgreSQL (bcrypt hashing)
- Short-lived access tokens + long-lived refresh tokens
- OpenID Connect integration point prepared but not implemented (Phase 2)

**Version/Technology:**
- ASP.NET Core Identity (built-in, no NuGet required)
- Microsoft.AspNetCore.Authentication.JwtBearer (latest 2025)
- Prepared for: Microsoft.AspNetCore.Authentication.OpenIdConnect (Phase 2)
- Password hashing: bcrypt via Identity framework
- Token generation: System.IdentityModel.Tokens.Jwt

**Rationale:**
1. ✅ Zero external dependencies (ship without IdP setup)
2. ✅ Complete control over session management (critical for real-time collaboration)
3. ✅ Fast MVP iteration (no IdP configuration friction)
4. ✅ Can upgrade to OpenID Connect without code restructuring (interface-based)
5. ✅ Self-hosted deployment model (no cloud vendor lock-in)

**MVP Implementation:**
- Login endpoint accepts username/password
- Validates against bcrypt hash in PostgreSQL Users table
- Issues JWT access token (15-min expiry) + refresh token (7-day expiry)
- Refresh token stored in HttpOnly cookie (XSS protection)
- SignalR connections validated via JWT in Authorization header

**Phase 2 Upgrade Path:**
- Extract authentication logic to `IAuthenticationService` interface
- Implement `OpenIdConnectAuthenticationService` alongside `LocalDatabaseAuthenticationService`
- Users choose auth method at deployment time (config-driven)

**Affected Components:**
- Session Manager (manages session → user mapping)
- API Controller (login endpoint, token validation)
- SignalR Hub (WebSocket authentication via JWT)
- Middleware (JWT bearer token validation)

---

#### Decision 2.2: Authorization Pattern

**Decision:** Hybrid RBAC + Claims-Based Authorization

**Details:**
- **Roles:** `Admin` (Cris), `Participant` (Sarah/Marcus), `Viewer` (observers)
- **Claims:** Fine-grained permissions (e.g., `workflow:create`, `decision:approve`)
- Both roles and claims evaluated via ASP.NET Core authorization policies
- Claims stored in JWT payload (no database lookup on each request)
- Roles stored in PostgreSQL Users table (loaded at login, embedded in JWT)

**Version/Technology:**
- ASP.NET Core Authorization (built-in)
- System.Security.Claims namespace
- JSON Web Token Claims as standard claims format

**Authorization Hierarchy:**

| Role | Capabilities | Claims |
|------|--------------|--------|
| **Admin** (Cris) | Full system control | `workflow:create`, `workflow:delete`, `agent:route`, `config:manage`, `audit:read` |
| **Participant** (Sarah/Marcus) | Participate in workflows, make decisions | `workflow:join`, `decision:comment`, `decision:approve`, `workflow:export` |
| **Viewer** | Read-only access | `workflow:view`, `decision:view` |

**MVP Implementation:**
```csharp
// Policy-based authorization
services.AddAuthorization(options =>
{
    options.AddPolicy("CanStartWorkflow", policy =>
        policy.RequireRole("Admin")
              .RequireClaim("workflow:create"));
    
    options.AddPolicy("CanApproveDecision", policy =>
        policy.RequireRole("Participant", "Admin")
              .RequireClaim("decision:approve"));
});

// Usage in controllers/hubs
[Authorize(Policy = "CanApproveDecision")]
public async Task ApproveDecision(DecisionApprovalRequest request)
{
    // Implementation
}
```

**Rationale:**
1. ✅ Supports current 3-person team structure (Admin + Participants)
2. ✅ Flexible for team growth (add claims-based permissions without role changes)
3. ✅ Workflow-specific permissions (Sarah can approve PRD, Marcus can approve Architecture)
4. ✅ Performance: Claims in JWT = no database lookup per request
5. ✅ ASP.NET Core native support (no external libraries needed)

**Future Scaling (Phase 2+):**
- Add granular workflow-type claims (e.g., `prd:approve`, `architecture:approve`)
- Implement team-based permissions (teams can have different roles/claims)
- Add time-limited claims (e.g., approval valid for 24 hours)

**Affected Components:**
- Session Manager (load claims from PostgreSQL at login)
- JWT Token generation (embed claims in token payload)
- All API endpoints (decorated with `[Authorize(Policy = "...")]`)
- SignalR Hubs (validate claims before accepting WebSocket messages)
- Audit logging (log authorization decisions)

---

#### Decision 2.3: Data Encryption Strategy

**Decision:** Transport-Layer Encryption (TLS/HTTPS) for MVP, Application-Level Encryption Planned Phase 2

**Details:**
- **MVP Phase:** All data encrypted in transit only
  - WebSocket connections over WSS (secure WebSocket)
  - HTTP redirected to HTTPS
  - Database connections use SSL (Npgsql supports SSL)
- **At-Rest:** PostgreSQL data unencrypted on disk (trust founding team access)
- **Phase 2+:** Application-level encryption for sensitive fields (workflow_state, decisions)

**Version/Technology:**
- ASP.NET Core: `UseHttpsRedirection()` + `UseHsts()`
- TLS 1.3+ (enforced via configuration)
- Npgsql SSL connection string: `ssl mode=require`
- Future: System.Security.Cryptography for application-level encryption

**TLS Configuration (Required for MVP):**

```csharp
// Program.cs
builder.Services.AddHsts(options =>
{
    options.Preload = true;
    options.IncludeSubDomains = true;
    options.MaxAge = TimeSpan.FromDays(365);
});

app.UseHttpsRedirection();
app.UseHsts();

// Force minimum TLS version
builder.Services.Configure<HttpsConnectionAdapterOptions>(options =>
{
    options.ServerCertificate = /* your certificate */;
});
```

**Security Headers (MVP):**
- `Strict-Transport-Security: max-age=31536000; includeSubDomains; preload`
- `X-Content-Type-Options: nosniff`
- `X-Frame-Options: DENY` (prevent clickjacking)
- `X-XSS-Protection: 1; mode=block`
- `Content-Security-Policy` (defined per endpoint if needed)

**Rationale:**
1. ✅ TLS sufficient for MVP (data protected in transit)
2. ✅ Founding team has trusted database access (no need for at-rest encryption now)
3. ✅ Simpler initial implementation (focus on shipping)
4. ✅ Clear upgrade path to application-level encryption (Phase 2)
5. ✅ Industry standard for SaaS (meets MVP security expectations)

**Important Caveat:**
- If system processes payment data, PHI, or PII → upgrade to application-level encryption immediately
- Current use case (product decisions, workflows) doesn't require Phase 2 encryption for MVP
- Document this as a Phase 2 enhancement in project backlog

**Phase 2 Application-Level Encryption (Planned):**
- Encrypt `workflow_state` JSONB field using EF Core value converters
- Use Data Protection API (DPAPI) or Azure Key Vault
- Maintain encryption key rotation policy
- Zero impact on existing code (transparent via EF Core converters)

**Affected Components:**
- Kestrel HTTPS configuration
- Database connection SSL settings
- SignalR WSS configuration
- Security header middleware
- (Phase 2: EF Core value converters for sensitive fields)

---

#### Decision 2.4: API Security & Rate Limiting

**Decision:** HTTPS Security Headers + Per-User Rate Limiting

**Details:**
- **Part A: Security Headers** (mandatory for all endpoints)
  - HTTPS redirection + HSTS
  - XSS protection headers
  - Content Security Policy (optional per endpoint)
  - CORS policy (if frontend separate domain)

- **Part B: Rate Limiting** (per authenticated user)
  - API endpoints: 60 requests/minute per user
  - WebSocket connections: 5 concurrent connections per user
  - Agent interactions: 10 concurrent agent calls per session
  - Enforced via custom middleware/filter

**Version/Technology:**
- ASP.NET Core: built-in HTTPS middleware
- Rate limiting NuGet: `System.Threading.RateLimiting` (built-in .NET 8+)
- Custom middleware for per-user enforcement

**Rate Limiting Implementation:**

```csharp
// Program.cs
services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter(policyName: "api-default", configure: options =>
    {
        options.PermitLimit = 60;
        options.Window = TimeSpan.FromMinutes(1);
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 0;
    });
    
    options.AddFixedWindowLimiter(policyName: "websocket", configure: options =>
    {
        options.PermitLimit = 5; // 5 concurrent connections
        options.Window = TimeSpan.FromSeconds(1);
    });
});

app.UseRateLimiter();

// Usage
[HttpGet("workflow/{id}")]
[RequireRateLimiting("api-default")]
public async Task<IActionResult> GetWorkflow(string id)
{
    // Implementation
}
```

**Rationale:**
1. ✅ Protects MVP from accidental DOS (bot crawlers, runaway scripts)
2. ✅ Fair resource allocation (no single user monopolizes system)
3. ✅ Easy to implement (built-in middleware, no external dependencies)
4. ✅ Simple to adjust (increase limits for scaling later)
5. ✅ Per-user enforcement (fair for team collaboration)

**MVP Limits (Conservative):**
- General API: 60 req/min (reasonable for web UI + background tasks)
- WebSocket: 5 concurrent (sufficient for single workflow + a few side tabs)
- Agent calls: 10 concurrent (prevents runaway agent loops)

**Phase 2 Adjustments:**
- Monitor actual usage patterns
- Increase limits if team hitting them (unlikely for 3-person MVP)
- Add admin override for special cases (Cris might need unlimited during testing)

**Affected Components:**
- Kestrel middleware (security headers)
- All API controllers (rate limiting filter)
- SignalR Hub (WebSocket connection limiting)
- Custom authentication middleware (track per-user usage)

---

#### Decision 2.5: Session Security & Token Expiration

**Decision:** Long-Lived Tokens with Refresh Pattern

**Details:**
- **Access Token:** 15 minutes
  - Short-lived JWT for API requests
  - Embedded in Authorization header
  - Validated on every API call
  
- **Refresh Token:** 7 days
  - Long-lived token for obtaining new access tokens
  - Stored in HttpOnly, Secure, SameSite cookie (XSS/CSRF protection)
  - Never exposed to JavaScript
  - Can be revoked immediately on security incident
  
- **Idle Timeout:** 30 minutes
  - If no activity for 30 min, force refresh token re-entry
  - Prevents infinite session from unattended browser

**Version/Technology:**
- JWT (JSON Web Token) for access tokens
- HttpOnly cookies for refresh tokens
- ASP.NET Core Identity for token management

**Token Issuance Flow:**

```
1. User login (POST /auth/login)
   ↓
2. Validate password
   ↓
3. Issue Access Token (JWT, 15 min expiry)
   ↓
4. Issue Refresh Token (HttpOnly cookie, 7 day expiry)
   ↓
5. Return access token in response body (for JavaScript use)
   ↓
6. Return refresh token in Set-Cookie header (browser handles automatically)
```

**Token Refresh Flow:**

```
1. Access token expires (15 min)
   ↓
2. Client requests new access token (POST /auth/refresh)
   ↓
3. Refresh token sent automatically (browser cookie)
   ↓
4. Server validates refresh token
   ↓
5. Issue new access token + optionally rotate refresh token
   ↓
6. Return new access token
```

**Token Content (JWT Claims):**

```json
{
  "sub": "user-id-123",
  "email": "cris@bmad.io",
  "name": "Cris",
  "roles": ["Admin"],
  "claims": ["workflow:create", "agent:route"],
  "exp": 1705857600,
  "iat": 1705857300,
  "jti": "token-id-for-revocation"
}
```

**Rationale:**
1. ✅ Users can work for 7 days without re-entering password (good UX)
2. ✅ Access token expiry limits damage from token theft (15 min window)
3. ✅ Refresh tokens can be revoked instantly (security incident response)
4. ✅ HttpOnly cookies prevent JavaScript access (XSS mitigation)
5. ✅ Idle timeout prevents unattended session hijacking
6. ✅ JWT is stateless (no server-side session storage needed)

**Security Features:**

| Feature | Protection |
|---------|-----------|
| **HttpOnly Cookie** | Prevents JavaScript access (XSS attack mitigation) |
| **Secure Flag** | Only transmitted over HTTPS |
| **SameSite=Strict** | Prevents CSRF attacks (cookie not sent to cross-origin requests) |
| **JTI (JWT ID)** | Token revocation via blacklist if needed |
| **Idle Timeout** | Unattended session automatically invalidated |
| **Refresh Token Rotation** | Optional: Issue new refresh token on each use |

**Idle Timeout Implementation:**

```csharp
// SignalR Hub
private async Task ValidateSessionExpiry(HubCallerContext context)
{
    var lastActivityClaim = context.User?.FindFirst("last_activity");
    if (lastActivityClaim != null && DateTime.TryParse(lastActivityClaim.Value, out var lastActivity))
    {
        if (DateTime.UtcNow - lastActivity > TimeSpan.FromMinutes(30))
        {
            await context.Clients.Caller.SendAsync("SessionExpired");
            context.Abort();
        }
    }
}
```

**Affected Components:**
- Login endpoint (issue tokens)
- Refresh endpoint (validate refresh token, issue new access token)
- Middleware (validate access token on each request)
- SignalR Hub (validate token expiry, enforce idle timeout)
- Frontend (handle token refresh before expiry, re-authenticate on expiry)

**Frontend Token Refresh Strategy (React):**
- Store access token in memory (not localStorage for security)
- Use `setInterval()` to refresh 1 minute before expiry (proactive)
- Automatically refresh on 401 Unauthorized response (reactive fallback)
- Clear tokens on logout

---

#### Decision 2.6: Cascading Implications

**Impacts on Other Architectural Decisions:**

| Decision | Impact | Effect |
|----------|--------|--------|
| **Authentication** | JWT tokens need signing key | See Infrastructure (key management) |
| **Authorization** | Claims loaded at login | Performance: Claims embedded in token (no DB lookup) |
| **Rate Limiting** | Per-user tracking | Cache user ID from JWT claim |
| **Audit Logging** | Log all auth events | See Infrastructure (audit trail) |
| **Session State** | User ID from JWT | Consistent user identity across WebSocket/HTTP |
| **Frontend** | Token refresh logic | React component for token management |

---

### Authentication & Security Summary Table

| Aspect | Decision | Version | Rationale |
|--------|----------|---------|-----------|
| **Authentication** | Hybrid (Local DB + OIDC Ready) | ASP.NET Core Identity | MVP control + Phase 2 extensibility |
| **Authorization** | Hybrid RBAC + Claims | Built-in | Simple + flexible for growth |
| **Encryption (Transit)** | HTTPS + TLS 1.3+ | Built-in | MVP protection, transparent to app |
| **Encryption (At-Rest)** | Not required MVP | — | Phase 2 enhancement if needed |
| **API Security** | Security Headers + Headers | NetEscapades | Standard OWASP recommendations |
| **Rate Limiting** | Per-User Fixed Window | System.Threading.RateLimiting | Fair resource allocation |
| **Access Token** | JWT, 15 min | System.IdentityModel.Tokens.Jwt | Short-lived for security |
| **Refresh Token** | HttpOnly Cookie, 7 day | Built-in cookies | XSS-safe, long-lived convenience |
| **Idle Timeout** | 30 minutes | Custom middleware | Unattended session protection |

---

### Category 3: API & Communication Patterns

#### Decision 3.1: REST API Design Pattern

**Decision:** Hybrid REST + RPC (Resource-Based + Action-Based)

**Details:**
- **Standard CRUD Operations:** Resource-oriented REST endpoints
  - GET `/api/v1/workflows` - List workflows
  - POST `/api/v1/workflows` - Create workflow
  - GET `/api/v1/workflows/{id}` - Get workflow details
  - PUT `/api/v1/workflows/{id}` - Update workflow metadata

- **Complex Workflow Operations:** RPC-style action endpoints
  - POST `/api/v1/workflows/{id}/start` - Start workflow (complex operation)
  - POST `/api/v1/workflows/{id}/pause` - Pause workflow
  - POST `/api/v1/decisions/{id}/approve` - Approve decision with rationale
  - POST `/api/v1/decisions/{id}/reject` - Reject decision with reason

**Version/Technology:**
- ASP.NET Core Minimal APIs (Aspire template default)
- Standard HTTP methods (GET, POST, PUT, DELETE)
- Consistent URL structure: `/api/v{version}/{resource}/{id}/{action}`

**HTTP Semantics:**

| Method | Resource | Action | Purpose |
|--------|----------|--------|---------|
| **GET** | `/workflows` | — | List/retrieve workflows |
| **POST** | `/workflows` | — | Create workflow |
| **PUT** | `/workflows/{id}` | — | Update workflow metadata |
| **DELETE** | `/workflows/{id}` | — | Delete workflow |
| **POST** | `/workflows/{id}/start` | start | Initiate workflow |
| **POST** | `/workflows/{id}/approve` | approve | Complex business logic |

**Rationale:**
1. ✅ Familiar to REST developers (standard CRUD operations)
2. ✅ Clear action names for complex operations (what does POST to `/workflows/{id}` mean? Clearer with `/start` or `/approve`)
3. ✅ Scalable (add new actions without changing resource structure)
4. ✅ Works well with OpenAPI documentation
5. ✅ Supports both stateless (PUT) and operation-focused (POST /action) patterns

**Request/Response Examples:**

```csharp
// Start Workflow (RPC-style action)
POST /api/v1/workflows
{
  "workflowType": "product-brief",
  "initiatedBy": "cris",
  "context": { "productName": "bmadServer" }
}
Response: 201 Created
{
  "id": "wf-123",
  "status": "in-progress",
  "createdAt": "2026-01-23T10:00:00Z"
}

// Approve Decision (RPC-style action with business logic)
POST /api/v1/decisions/dec-456/approve
{
  "rationale": "Architecture review passed QA standards",
  "approvedBy": "marcus"
}
Response: 200 OK
{
  "id": "dec-456",
  "status": "approved",
  "approvedAt": "2026-01-23T10:15:00Z",
  "nextStep": "proceed-to-implementation"
}
```

**Affected Components:**
- API Controllers/Minimal APIs (endpoint definitions)
- OpenAPI documentation (Swagger)
- Frontend API client (fetch patterns)
- Client SDK generation (if generating clients)

---

#### Decision 3.2: Error Handling & Status Codes

**Decision:** ProblemDetails (RFC 7807 Standard)

**Details:**
- **Standardized Error Format:** RFC 7807 "Problem Details for HTTP APIs"
- **Built-in ASP.NET Core Support:** No NuGet packages required
- **Consistent Across All Errors:** Single error response shape for clients
- **Machine-Readable:** Clients can parse errors programmatically

**Version/Technology:**
- Microsoft.AspNetCore.Mvc.ProblemDetails (built-in)
- IExceptionHandler for custom exception mapping
- ValidationProblemDetails for validation errors

**ProblemDetails Response Format:**

```json
{
  "type": "https://bmadserver.api/errors/workflow-not-found",
  "title": "Workflow Not Found",
  "status": 404,
  "detail": "Workflow with ID 'wf-invalid-123' does not exist in the system",
  "instance": "/api/v1/workflows/wf-invalid-123",
  "traceId": "0HMVF7GIJF6AS:00000001"
}
```

**Conflict Error Example (JSONB Version Conflict):**

```json
{
  "type": "https://bmadserver.api/errors/workflow-conflict",
  "title": "Workflow State Conflict",
  "status": 409,
  "detail": "Workflow state was modified by another user. Please refresh and try again.",
  "instance": "/api/v1/workflows/wf-123",
  "conflictingUserId": "marcus-123",
  "expectedVersion": 5,
  "actualVersion": 6,
  "lastModifiedAt": "2026-01-23T10:14:30Z",
  "lastModifiedBy": "marcus"
}
```

**Validation Error Example:**

```json
{
  "type": "https://bmadserver.api/errors/validation-failed",
  "title": "Validation Failed",
  "status": 400,
  "detail": "Request validation failed. See errors for details.",
  "instance": "/api/v1/workflows",
  "errors": {
    "workflowType": ["'workflowType' is required"],
    "context.productName": ["'productName' must be between 1 and 100 characters"]
  }
}
```

**HTTP Status Codes (Consistent):**

| Status | Meaning | When to Use | Example |
|--------|---------|------------|---------|
| **200** | OK | Successful GET, POST (non-creation) | GET `/workflows/{id}` |
| **201** | Created | Successfully created resource | POST `/workflows` |
| **204** | No Content | Successful but no response body | DELETE `/workflows/{id}` |
| **400** | Bad Request | Invalid input, validation failure | Malformed JSON, missing required fields |
| **401** | Unauthorized | Missing/invalid JWT token | No Authorization header |
| **403** | Forbidden | User lacks permission | User not in Admin role |
| **404** | Not Found | Resource doesn't exist | Workflow ID doesn't exist |
| **409** | Conflict | Version conflict, duplicate key | JSONB concurrent update |
| **429** | Too Many Requests | Rate limit exceeded | Exceeded 60 req/min |
| **500** | Internal Error | Unhandled server exception | Database connection lost |
| **503** | Service Unavailable | External dependency down | OpenCode API unreachable |

**Implementation (Program.cs):**

```csharp
// Enable ProblemDetails for all endpoints
services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        // Add custom fields (e.g., traceId for logging correlation)
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
        context.ProblemDetails.Extensions["timestamp"] = DateTime.UtcNow;
    };
});

// Map endpoints with exception handling
app.UseExceptionHandler();
app.UseStatusCodePages();
```

**Custom Exception Mapping:**

```csharp
// Custom exception types
public class WorkflowConflictException : Exception
{
    public string? ConflictingUserId { get; set; }
    public int ExpectedVersion { get; set; }
    public int ActualVersion { get; set; }
}

// IExceptionHandler implementation
public class WorkflowExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        if (exception is WorkflowConflictException conflictEx)
        {
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            var problemDetails = new ProblemDetails
            {
                Type = "https://bmadserver.api/errors/workflow-conflict",
                Title = "Workflow State Conflict",
                Status = StatusCodes.Status409Conflict,
                Detail = "Workflow state was modified by another user",
                Instance = context.Request.Path,
                Extensions = new Dictionary<string, object?>
                {
                    ["conflictingUserId"] = conflictEx.ConflictingUserId,
                    ["expectedVersion"] = conflictEx.ExpectedVersion,
                    ["actualVersion"] = conflictEx.ActualVersion
                }
            };
            
            await context.Response.WriteAsJsonAsync(problemDetails, cancellationToken: ct);
            return true;
        }
        return false;
    }
}
```

**Rationale:**
1. ✅ Industry standard (RFC 7807 recognized across APIs)
2. ✅ Built-in to ASP.NET Core (no extra code)
3. ✅ Machine-readable (clients parse errors programmatically)
4. ✅ Consistent response shape (reduces client-side error handling complexity)
5. ✅ Supports custom fields (extend for domain-specific errors)

**Affected Components:**
- Global exception handler middleware
- All API endpoints (inherit error handling automatically)
- Frontend error parsing (receives consistent format)
- Logging/monitoring (traceId in every error)

---

#### Decision 3.3: API Documentation

**Decision:** OpenAPI 3.1 with Swagger UI

**Details:**
- **Specification Format:** OpenAPI 3.1 (latest standard)
- **Interactive UI:** Swagger UI for testing endpoints
- **Auto-Generated:** Derived from code annotations
- **Built-In:** NuGet package Swashbuckle 6.5+
- **No Manual Sync:** Always reflects current API state

**Version/Technology:**
- Swashbuckle.AspNetCore 6.5+ (OpenAPI 3.1)
- Swagger UI 4.x (interactive testing)
- Code annotations: `[Produces]`, `[ProduceResponseType]`, `[Authorize]`

**OpenAPI Specification Example (Auto-Generated):**

```yaml
openapi: 3.1.0
info:
  title: bmadServer API
  version: v1
  description: BMAD Workflow Orchestration Platform
servers:
  - url: https://bmadserver.local/api/v1
paths:
  /workflows:
    get:
      summary: List workflows
      tags:
        - Workflows
      security:
        - BearerAuth: []
      parameters:
        - name: status
          in: query
          schema:
            type: string
            enum: [pending, in-progress, completed]
      responses:
        '200':
          description: List of workflows
          content:
            application/json:
              schema:
                type: array
                items:
                  $ref: '#/components/schemas/Workflow'
        '401':
          description: Unauthorized
        '429':
          description: Too Many Requests
    post:
      summary: Create new workflow
      requestBody:
        required: true
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/CreateWorkflowRequest'
      responses:
        '201':
          description: Workflow created
        '400':
          description: Bad Request
  /workflows/{id}/approve:
    post:
      summary: Approve workflow decision
      parameters:
        - name: id
          in: path
          required: true
          schema:
            type: string
      requestBody:
        required: true
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/ApprovalRequest'
      responses:
        '200':
          description: Decision approved
        '409':
          description: Conflict (version mismatch)
```

**Implementation (Program.cs):**

```csharp
// Add Swagger/OpenAPI services
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "bmadServer API",
        Version = "v1",
        Description = "BMAD Workflow Orchestration Platform",
        Contact = new OpenApiContact
        {
            Name = "bmadServer Support",
            Email = "support@bmadserver.local"
        }
    });
    
    // Add JWT bearer token support in Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "JWT Authorization header using Bearer scheme"
    });
    
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] { }
        }
    });
});

// Use Swagger middleware
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "bmadServer API v1");
    options.RoutePrefix = "docs";  // Available at /docs
});
```

**Endpoint Annotations (Code-Driven Generation):**

```csharp
[HttpPost("/workflows/{id}/approve")]
[Authorize(Policy = "CanApproveDecisions")]
[Produces("application/json")]
[ProduceResponseType(typeof(ApprovalResponse), StatusCodes.Status200OK)]
[ProduceResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
[ProduceResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
public async Task<IActionResult> ApproveDecision(
    [FromRoute] string id,
    [FromBody] ApprovalRequest request)
{
    // Implementation
}
```

**Rationale:**
1. ✅ Zero maintenance (generated from code)
2. ✅ Always current (reflects latest code state)
3. ✅ Interactive testing (Swagger UI for manual testing)
4. ✅ Machine-readable (can generate client libraries)
5. ✅ Industry standard (recognized by all developer tools)

**Access Points:**
- Swagger UI: `https://bmadserver.local/docs`
- OpenAPI JSON: `https://bmadserver.local/swagger/v1/swagger.json`
- Can be embedded in GitHub/documentation wikis

**Affected Components:**
- Swashbuckle middleware
- All API endpoint definitions (annotations)
- Frontend API client generation (optional)

---

#### Decision 3.4: WebSocket Error Handling (SignalR)

**Decision:** Explicit Error Messages (Server-to-Client Messages)

**Details:**
- **No Exception Propagation:** Hub methods never throw directly
- **Explicit Handling:** Try-catch in every Hub method
- **Structured Error Messages:** Send error as SignalR message with consistent format
- **Aligned with ProblemDetails:** Same error structure as REST APIs

**Error Message Format (Matches ProblemDetails):**

```json
{
  "type": "error",
  "code": "WORKFLOW_CONFLICT",
  "title": "Workflow State Conflict",
  "message": "Workflow state was modified by another user",
  "status": 409,
  "details": {
    "conflictingUserId": "marcus-123",
    "expectedVersion": 5,
    "actualVersion": 6,
    "lastModifiedAt": "2026-01-23T10:14:30Z"
  },
  "traceId": "0HMVF7GIJF6AS:00000001"
}
```

**Implementation Pattern:**

```csharp
public class WorkflowHub : Hub
{
    private readonly ILogger<WorkflowHub> _logger;
    private readonly IWorkflowService _workflowService;
    
    public WorkflowHub(ILogger<WorkflowHub> logger, IWorkflowService workflowService)
    {
        _logger = logger;
        _workflowService = workflowService;
    }
    
    // Explicit error handling pattern
    public async Task ApproveDecision(string decisionId, string rationale)
    {
        try
        {
            var result = await _workflowService.ApproveDecisionAsync(decisionId, rationale);
            
            // Broadcast approval to all connected clients
            await Clients.All.SendAsync("decision-approved", new
            {
                decisionId = result.Id,
                approvedAt = result.ApprovedAt,
                approvedBy = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            });
        }
        catch (WorkflowConflictException ex)
        {
            // Send structured error to caller
            await Clients.Caller.SendAsync("error", new
            {
                type = "error",
                code = "WORKFLOW_CONFLICT",
                title = "Workflow State Conflict",
                message = ex.Message,
                status = 409,
                details = new
                {
                    conflictingUserId = ex.ConflictingUserId,
                    expectedVersion = ex.ExpectedVersion,
                    actualVersion = ex.ActualVersion,
                    lastModifiedAt = ex.LastModifiedAt
                },
                traceId = Context.GetHttpContext()?.TraceIdentifier
            });
            
            _logger.LogWarning(ex, "Workflow conflict in ApproveDecision: {DecisionId}", decisionId);
        }
        catch (UnauthorizedAccessException ex)
        {
            await Clients.Caller.SendAsync("error", new
            {
                type = "error",
                code = "UNAUTHORIZED",
                title = "Unauthorized",
                message = "You do not have permission to approve this decision",
                status = 403
            });
            
            _logger.LogWarning(ex, "Unauthorized attempt to approve decision: {DecisionId}", decisionId);
        }
        catch (Exception ex)
        {
            // Log unexpected errors, send generic message to client
            await Clients.Caller.SendAsync("error", new
            {
                type = "error",
                code = "INTERNAL_ERROR",
                title = "Internal Server Error",
                message = "An unexpected error occurred. Please contact support.",
                status = 500,
                traceId = Context.GetHttpContext()?.TraceIdentifier
            });
            
            _logger.LogError(ex, "Unexpected error in ApproveDecision: {DecisionId}", decisionId);
        }
    }
}
```

**Client-Side Handling (React):**

```typescript
// SignalR connection listener
connection.on("error", (error) => {
  console.error("Server error:", error);
  
  // Show user-friendly error message
  switch (error.code) {
    case "WORKFLOW_CONFLICT":
      setError(`Conflict: ${error.details.conflictingUserId} modified this simultaneously`);
      break;
    case "UNAUTHORIZED":
      setError(error.message);
      break;
    default:
      setError("An unexpected error occurred");
  }
});
```

**Rationale:**
1. ✅ Full control over error format (consistent with REST ProblemDetails)
2. ✅ Rich context in errors (can include version numbers, conflicting users)
3. ✅ Graceful degradation (send safe messages to clients, log full details server-side)
4. ✅ No exception stack traces exposed to client
5. ✅ Works well with React error boundaries
6. ✅ Audit trail of all errors (logged server-side)

**WebSocket Connection Error Scenarios:**

| Scenario | Handling | Client Impact |
|----------|----------|---------------|
| **User loses auth token** | Reject message + reconnection prompt | "Please log in again" |
| **Rate limit exceeded** | Send 429 error + cooldown delay | "Too many requests, retry in 60s" |
| **Agent timeout (30s)** | Send TIMEOUT error + suggested action | "Agent didn't respond, try again" |
| **Database constraint violation** | Send CONFLICT error + retry hint | "Change detected, refresh" |
| **Internal exception** | Generic error + traceId for support | "Error occurred, contact support" |

**Affected Components:**
- All SignalR Hub methods (error handling pattern)
- Frontend WebSocket listeners (error message handling)
- Logging system (capture all errors with context)
- Monitoring/alerts (trace errors by code)

---

#### Decision 3.5: API Versioning Strategy

**Decision:** URL Path Versioning (`/api/v1/`, `/api/v2/`, etc.)

**Details:**
- **Version in URL Path:** `/api/v1/workflows`, `/api/v2/workflows`
- **Explicit and Clear:** Version obvious from endpoint URL
- **Multiple Versions:** Can run v1 and v2 simultaneously
- **Deprecation Support:** Mark old versions as deprecated in OpenAPI

**Version Management Strategy:**

```
Phase 1 (MVP): /api/v1/*
├── All endpoints behind v1
├── Clear API contract
└── No deprecations yet

Phase 2 (If breaking changes needed):
├── /api/v1/* (maintained, marked deprecated)
├── /api/v2/* (new features, breaking changes)
├── 3-month deprecation notice
└── Sunset v1 after 6 months

Phase 3+:
├── Remove v1 completely
├── /api/v2/* becomes main version
├── /api/v3/* if needed
└── Follow same deprecation cycle
```

**Implementation (Program.cs):**

```csharp
// Map v1 endpoints
var v1Routes = app.MapGroup("/api/v1")
    .WithOpenApi()
    .WithName("API v1")
    .WithDescription("Version 1 - Current stable API");

v1Routes.MapGet("/workflows", GetWorkflows)
    .WithName("ListWorkflows")
    .WithDescription("List all workflows");

v1Routes.MapPost("/workflows", CreateWorkflow)
    .WithName("CreateWorkflow");

v1Routes.MapPost("/workflows/{id}/approve", ApproveWorkflow)
    .WithName("ApproveWorkflow");

// If v2 needed in future:
// var v2Routes = app.MapGroup("/api/v2")
//     .WithOpenApi()
//     .WithName("API v2")
//     .WithDescription("Version 2 - New features with breaking changes");
```

**Deprecation Markers in OpenAPI:**

```csharp
[Obsolete("Use /api/v2/workflows instead", false)]
[HttpGet("/workflows/{id}")]
[Produces("application/json")]
[ProduceResponseType(typeof(WorkflowDto), StatusCodes.Status200OK)]
public async Task<IActionResult> GetWorkflow(string id)
{
    // Implementation with deprecation header
    Response.Headers.Add("Deprecated", "true");
    Response.Headers.Add("Sunset", "Sun, 23 Jul 2026 23:59:59 GMT");
    Response.Headers.Add("Link", "</api/v2/workflows/{id}>; rel=\"successor-version\"");
    
    // Implementation
}
```

**Client Migration Path:**
1. v1 released (MVP)
2. v2 announced 6 months before sunset
3. v1 endpoints return `Deprecated: true` header + sunset date
4. Clients given 3-month window to migrate
5. v1 sunset after 6 months total notice

**Rationale:**
1. ✅ Explicit and discoverable (version in URL)
2. ✅ Works well with OpenAPI (each version documented separately)
3. ✅ Backward compatibility (can run multiple versions)
4. ✅ Clear deprecation path (clients know when to upgrade)
5. ✅ Supports gradual migration (not forced immediate upgrade)

**Alternatives (Not Selected):**
- **Header Versioning** - Clean URLs but less discoverable
- **No Versioning** - Simpler initially, breaks later

**Affected Components:**
- Route definitions (grouped by version)
- OpenAPI documentation (per-version specs)
- Client SDK generation (one SDK per version)
- Frontend API configuration (base URL includes version)
- Deprecation notifications (headers, docs)

---

#### Decision 3.6: Cascading Implications

**Impacts on Other Architectural Decisions:**

| Decision | Impact | Effect |
|----------|--------|--------|
| **REST Design** | OpenAPI must document both CRUD + RPC patterns | See Documentation (auto-generated) |
| **Error Handling** | SignalR errors must match ProblemDetails format | Consistent client error parsing |
| **WebSocket Errors** | Must be versioned with REST API | v1 errors include v1 fields |
| **API Versioning** | Frontend must handle multiple API versions | Client SDK generation per version |
| **Rate Limiting** | Rate limit errors follow ProblemDetails | 429 status with `Retry-After` header |
| **Authentication** | JWT validation errors use ProblemDetails | 401 errors follow standard format |

---

### API & Communication Patterns Summary Table

| Aspect | Decision | Version | Rationale |
|--------|----------|---------|-----------|
| **REST Design** | Hybrid REST + RPC | ASP.NET Core Minimal APIs | Simple CRUD + clear action names |
| **Error Handling** | ProblemDetails (RFC 7807) | Built-in to ASP.NET Core 10 | Industry standard, machine-readable |
| **Documentation** | OpenAPI 3.1 + Swagger UI | Swashbuckle 6.5+ | Auto-generated, always current |
| **WebSocket Errors** | Explicit Error Messages | SignalR 8.0+ | Full control, consistent format |
| **API Versioning** | URL Path (`/api/v1/`) | Standard pattern | Explicit, backward compatible |

---

## Frontend Architecture

### Overview: Client-Side Composition

The frontend is a **conversational web interface** that translates BMAD's powerful CLI into an intuitive, real-time collaboration platform. It integrates with the backend via **REST APIs + WebSocket (SignalR)**, maintains application state via **Zustand + TanStack Query**, and renders components using **feature-based architecture** with **React Router v7**.

**Key Principles:**
- ✅ **Lightweight:** Minimal bundle size (React Router v7 + Zustand + TanStack Query = ~150KB gzipped)
- ✅ **Responsive:** Real-time updates via WebSocket push (no polling)
- ✅ **Type-Safe:** Strict TypeScript configuration catches errors at compile-time
- ✅ **Scalable:** Feature-based folder structure supports parallel team development
- ✅ **Performant:** Code splitting by route + bundle optimization (50-70% faster initial load)

---

### Decision 4.1: State Management Architecture

**Selected Option: C - Zustand + TanStack Query (Recommended)**

**Selected Technologies:**
- **Global Client State:** Zustand 4.5+ (2KB gzipped)
- **Server State:** TanStack Query 5.x (React Query)
- **Local Component State:** React.useState / useReducer

**Rationale:**

After evaluating Redux Toolkit, Jotai, and context-only approaches, we selected the Zustand + TanStack Query combination because:

1. **Zustand Advantages:**
   - 2KB gzipped (vs Redux ~20KB)
   - No Provider wrapper required (simpler setup)
   - Minimal boilerplate (no actions/reducers/dispatch)
   - Perfect for auth state, UI toggles, theme, user preferences
   - Easy to test (simple functions, no complex middleware)

2. **TanStack Query Advantages:**
   - Server state ≠ client state (critical separation)
   - Automatic caching, background refetching, stale data handling
   - Handles API synchronization across multiple components
   - Built-in loading/error states
   - Reduces "component soup" (no prop drilling for API data)

3. **Combined Benefits:**
   - Clear responsibility split: TanStack Query (async) + Zustand (sync)
   - Prevents state duplication (single source of truth)
   - Scales from MVP to enterprise (can add Redux later if team grows 10+)
   - 2026 industry standard (confirmed in recent articles)

**Mental Model (4-Layer State):**

| Layer | Examples | Tool | Implementation |
|-------|----------|------|-----------------|
| **Local UI State** | Modal open, form input value | `useState` / `useReducer` | Component-level hooks |
| **Derived State** | Filtered lists, computed values | `useMemo` / selectors | Within components |
| **Global Client State** | Auth user, theme, sidebar collapsed | Zustand | Centralized store |
| **Server State** | Workflows, decisions, API data | TanStack Query | Query client + hooks |

**Zustand Store Structure:**

```typescript
// src/stores/authStore.ts
import { create } from 'zustand';

interface AuthState {
  user: User | null;
  isLoggedIn: boolean;
  setUser: (user: User) => void;
  logout: () => void;
}

export const useAuthStore = create<AuthState>((set) => ({
  user: null,
  isLoggedIn: false,
  setUser: (user) => set({ user, isLoggedIn: true }),
  logout: () => set({ user: null, isLoggedIn: false }),
}));
```

**TanStack Query Implementation:**

```typescript
// src/hooks/useWorkflows.ts
import { useQuery } from '@tanstack/react-query';
import { fetchWorkflows } from '../api/workflows';

export const useWorkflows = () => {
  return useQuery({
    queryKey: ['workflows'],
    queryFn: fetchWorkflows,
    staleTime: 5 * 60 * 1000, // 5 min cache
    refetchOnWindowFocus: true, // Sync on tab focus
  });
};
```

**Affected Components:**
- Authentication flow (Zustand)
- API data fetching (TanStack Query)
- Form state (useState)
- Real-time updates (SignalR + TanStack Query invalidation)
- Global UI state (theme, sidebar, notifications)

**Alternatives (Not Selected):**
- **Redux Toolkit** - Overkill for MVP, too much boilerplate for solo/small team
- **Jotai** - Great for atomic state but overkill for this use case
- **Context API Only** - Too slow for frequent state updates, prop drilling nightmare

---

### Decision 4.2: Component Architecture

**Selected Option: A - Feature-Based with Shared Layer (Recommended)**

**Folder Structure:**

```
src/
├── features/               # Feature modules (user-facing functionality)
│   ├── auth/              # Authentication feature
│   │   ├── components/    # Auth-specific components (Login, Register)
│   │   ├── hooks/         # useLogin, useRegister hooks
│   │   ├── services/      # Auth API calls
│   │   └── types.ts       # Auth domain types
│   ├── workflows/         # Workflow management feature
│   │   ├── components/    # WorkflowList, WorkflowDetail, WorkflowEditor
│   │   ├── hooks/         # useWorkflows, useCreateWorkflow
│   │   ├── services/      # Workflow API calls
│   │   └── types.ts       # Workflow domain types
│   ├── decisions/         # Decision management feature
│   │   ├── components/    # DecisionTree, DecisionDetail, DecisionForm
│   │   ├── hooks/         # useDecisions, useApproveDecision
│   │   ├── services/      # Decision API calls
│   │   └── types.ts       # Decision domain types
│   ├── settings/          # User settings feature
│   │   ├── components/    # SettingsPanel, PreferenceForm
│   │   ├── hooks/         # useSettings
│   │   ├── services/      # Settings API calls
│   │   └── types.ts       # Settings domain types
│   └── collaboration/     # Real-time collaboration feature
│       ├── components/    # ActiveUsers, ConflictResolver
│       ├── hooks/         # useActiveUsers, useConflictDetection
│       ├── services/      # SignalR integration
│       └── types.ts       # Collaboration domain types
├── shared/                # Shared across features
│   ├── components/        # Button, Modal, Layout, Navbar, Sidebar
│   ├── hooks/             # useAsync, useLocalStorage, useDebounce
│   ├── utils/             # formatDate, apiClient, validators
│   ├── types/             # GlobalError, ApiResponse, User
│   ├── constants/         # API URLs, error messages, config
│   └── styles/            # Global CSS, Tailwind config
├── stores/                # Zustand stores (auth, ui state)
├── api/                   # Centralized API client
│   ├── client.ts          # Axios/Fetch instance with interceptors
│   └── endpoints.ts       # API route definitions
├── lib/                   # Third-party integrations
│   ├── queryClient.ts     # TanStack Query configuration
│   ├── signalr.ts         # SignalR connection setup
│   └── icons.ts           # Icon library configuration
├── App.tsx                # Main app component
├── main.tsx               # Vite entry point
└── index.css              # Global styles
```

**Rationale:**

1. **Feature-Based Advantages:**
   - **Team Scaling:** Developers can work on separate features without conflicts
   - **Code Cohesion:** All related code (components, hooks, services, types) in one folder
   - **Discoverability:** New team members find code faster
   - **Refactoring:** Moving/removing a feature is isolated

2. **Shared Layer Benefits:**
   - **DRY:** Reusable components, utilities, types
   - **Consistency:** Shared design system, API client, configuration
   - **Maintenance:** Updates in one place

3. **Scalability:**
   - **MVP:** Easy to add features quickly
   - **Growth:** Team can work on features in parallel
   - **Monorepo Ready:** Can extract features to separate packages later

**Component Examples:**

```typescript
// src/features/workflows/components/WorkflowList.tsx
import { useWorkflows } from '../hooks/useWorkflows';
import { WorkflowCard } from './WorkflowCard';

export const WorkflowList: React.FC = () => {
  const { data: workflows, isLoading, error } = useWorkflows();

  if (isLoading) return <div>Loading...</div>;
  if (error) return <div>Error: {error.message}</div>;

  return (
    <div className="workflow-list">
      {workflows?.map(workflow => (
        <WorkflowCard key={workflow.id} workflow={workflow} />
      ))}
    </div>
  );
};

// src/shared/components/Button.tsx
import React from 'react';
import './Button.css';

interface ButtonProps {
  variant: 'primary' | 'secondary' | 'danger';
  size: 'small' | 'medium' | 'large';
  onClick: () => void;
  disabled?: boolean;
  children: React.ReactNode;
}

export const Button: React.FC<ButtonProps> = ({
  variant, size, onClick, disabled, children
}) => (
  <button 
    className={`btn btn-${variant} btn-${size}`}
    onClick={onClick}
    disabled={disabled}
  >
    {children}
  </button>
);
```

**Alternatives (Not Selected):**
- **Type-Based** (`/components`, `/containers`, `/pages`) - Simple but doesn't scale
- **Hybrid** (mix of feature + type) - Confusing, lacks clear ownership

---

### Decision 4.3: Routing Strategy

**Selected Option: A - React Router v7 Declarative (Recommended)**

**Selected Technology:**
- **React Router:** v7 (released 2025, latest stable)
- **Pattern:** Declarative JSX-based routing (not data-driven)
- **Code Splitting:** React.lazy() + Suspense boundaries

**Route Structure:**

```typescript
// src/App.tsx
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { MainLayout } from './shared/components/MainLayout';
import { AuthLayout } from './shared/components/AuthLayout';

// Lazy load features for code splitting
const LoginPage = React.lazy(() => import('./features/auth/pages/Login'));
const WorkflowsPage = React.lazy(() => import('./features/workflows/pages/List'));
const WorkflowDetailPage = React.lazy(() => import('./features/workflows/pages/Detail'));
const DecisionsPage = React.lazy(() => import('./features/decisions/pages/List'));
const SettingsPage = React.lazy(() => import('./features/settings/pages/Settings'));
const DocumentationPage = React.lazy(() => import('./features/docs/pages/Index'));

export const App: React.FC = () => {
  return (
    <BrowserRouter>
      <Routes>
        {/* Auth Routes */}
        <Route element={<AuthLayout />}>
          <Route path="/login" element={<React.Suspense fallback={<div>Loading...</div>}><LoginPage /></React.Suspense>} />
        </Route>

        {/* Protected Routes */}
        <Route element={<ProtectedRoute><MainLayout /></ProtectedRoute>}>
          <Route path="/" element={<Navigate to="/workflows" replace />} />
          <Route path="/workflows" element={<React.Suspense fallback={<div>Loading...</div>}><WorkflowsPage /></React.Suspense>} />
          <Route path="/workflows/:id" element={<React.Suspense fallback={<div>Loading...</div>}><WorkflowDetailPage /></React.Suspense>} />
          <Route path="/workflows/:id/decisions" element={<React.Suspense fallback={<div>Loading...</div>}><DecisionsPage /></React.Suspense>} />
          <Route path="/settings" element={<React.Suspense fallback={<div>Loading...</div>}><SettingsPage /></React.Suspense>} />
          <Route path="/docs" element={<React.Suspense fallback={<div>Loading...</div>}><DocumentationPage /></React.Suspense>} />
        </Route>
      </Routes>
    </BrowserRouter>
  );
};
```

**Route Definitions (v7 recommended):**

```typescript
// src/routes.tsx
import { RouteObject } from 'react-router-dom';

export const routes: RouteObject[] = [
  {
    path: '/login',
    element: <LoginPage />,
    errorElement: <ErrorPage />,
  },
  {
    path: '/',
    element: <MainLayout />,
    children: [
      {
        path: 'workflows',
        element: <WorkflowsPage />,
      },
      {
        path: 'workflows/:id',
        element: <WorkflowDetailPage />,
        loader: workflowDetailLoader,
      },
    ],
  },
];
```

**URL Mapping:**

| Route | Purpose | Component | Data Loading |
|-------|---------|-----------|---------------|
| `/` | Redirect to workflows | MainLayout | N/A |
| `/login` | Authentication | LoginPage | Form-based |
| `/workflows` | List all workflows | WorkflowList | TanStack Query |
| `/workflows/:id` | Workflow details | WorkflowDetail | TanStack Query |
| `/workflows/:id/decisions` | Workflow decisions | DecisionTree | TanStack Query + WebSocket |
| `/settings` | User preferences | SettingsPanel | Zustand + API |
| `/docs` | Help/Documentation | DocumentationIndex | Static or CMS |

**Rationale:**

1. **Declarative Routing Benefits:**
   - Easy to read and understand
   - No complex configuration objects
   - Natural React component structure
   - Fast to implement

2. **Code Splitting Strategy:**
   - Routes lazy-loaded with React.lazy()
   - Reduces initial bundle size by ~50-70%
   - Each route loads only when visited
   - Suspense boundaries for smooth loading UX

3. **Scalability:**
   - React Router v7 supports data-driven routing (future upgrade)
   - Framework mode available (SSR, file-based routing if needed)
   - Proven in production apps (Remix, Next.js competitors)

**Alternatives (Not Selected):**
- **Data-Driven Routing** - Too complex for MVP, easier to upgrade later
- **TanStack Router** - Overkill, more complex syntax

---

### Decision 4.4: Performance Optimization Strategy

**Selected Option: C - Code Splitting + Bundle Optimization (Recommended)**

**Code Splitting Implementation:**

1. **Route-Based Code Splitting (Primary):**
   ```typescript
   const WorkflowsPage = React.lazy(() => 
     import('./features/workflows/pages/List')
   );
   
   // In routes:
   <Route path="/workflows" element={
     <Suspense fallback={<LoadingSpinner />}>
       <WorkflowsPage />
     </Suspense>
   } />
   ```

2. **Component-Based Code Splitting (Heavy Components):**
   ```typescript
   const AdvancedEditor = React.lazy(() => 
     import('./shared/components/AdvancedEditor')
   );
   
   // Use sparingly for large, infrequently-used components
   ```

**Bundle Optimization:**

1. **Build Tool:** Vite (default with Create React App 2025+)
   - Tree-shaking: Removes unused code automatically
   - Minification: Reduces bundle size
   - Module federation: Prepare for micro-frontend scaling

2. **Configuration (vite.config.ts):**
   ```typescript
   import { defineConfig } from 'vite';
   import react from '@vitejs/plugin-react';
   
   export default defineConfig({
     plugins: [react()],
     build: {
       chunkSizeWarningLimit: 500,
       rollupOptions: {
         output: {
           manualChunks: {
             // Vendor chunks
             'vendor-react': ['react', 'react-dom'],
             'vendor-query': ['@tanstack/react-query'],
             'vendor-router': ['react-router-dom'],
             'vendor-ui': ['@headlessui/react', 'lucide-react'],
           },
         },
       },
     },
   });
   ```

3. **Performance Results:**
   - Initial Bundle: ~120-150KB (gzipped)
   - Route chunks: 20-40KB each (lazy loaded)
   - TTI (Time to Interactive): 1.2-1.8 seconds (3G/4G)
   - FCP (First Contentful Paint): 600-800ms

4. **Monitoring:**
   - Use Lighthouse CI in CI/CD pipeline
   - Alert if bundle size increases >10%
   - Track Core Web Vitals in production (LCP, FID, CLS)

**Image Optimization:**

```typescript
// Use native lazy loading for images
<img src="..." alt="..." loading="lazy" />

// Or use Picture element for responsive images
<picture>
  <source srcSet="image.webp" type="image/webp" />
  <img src="image.jpg" alt="..." loading="lazy" />
</picture>
```

**Rationale:**

1. **Performance Impact:**
   - Route-based splitting: 50-70% faster initial load
   - Bundle optimization: 20-30% smaller bundles
   - Combined: ~2x performance improvement

2. **UX Benefit:**
   - Faster Time to Interactive (TTI)
   - Smooth transitions between routes (Suspense)
   - Better Core Web Vitals for SEO

3. **Scalability:**
   - Grows linearly with features (not exponentially)
   - Easy to add new routes without bloating initial bundle

**Alternatives (Not Selected):**
- **Code Splitting Only** - Misses bundling optimization benefits
- **Bundle Optimization Only** - Doesn't help initial load times

---

### Decision 4.5: TypeScript Configuration

**Selected Option: A - Strict Mode with Escape Hatch (Recommended)**

**TypeScript Configuration (tsconfig.json):**

```json
{
  "compilerOptions": {
    "target": "ES2020",
    "lib": ["ES2020", "DOM", "DOM.Iterable"],
    "jsx": "react-jsx",
    "module": "ESNext",
    "moduleResolution": "bundler",
    
    "strict": true,
    "noImplicitAny": true,
    "strictNullChecks": true,
    "strictFunctionTypes": true,
    "strictBindCallApply": true,
    "strictPropertyInitialization": true,
    "noImplicitThis": true,
    "alwaysStrict": true,
    
    "noUnusedLocals": true,
    "noUnusedParameters": true,
    "noImplicitReturns": true,
    "noFallthroughCasesInSwitch": true,
    
    "esModuleInterop": true,
    "allowSyntheticDefaultImports": true,
    "forceConsistentCasingInFileNames": true,
    "resolveJsonModule": true,
    "isolatedModules": true,
    
    "baseUrl": "./src",
    "paths": {
      "@/*": ["./*"],
      "@features/*": ["./features/*"],
      "@shared/*": ["./shared/*"],
      "@stores/*": ["./stores/*"],
      "@api/*": ["./api/*"],
      "@lib/*": ["./lib/*"],
      "@types/*": ["./types/*"],
    }
  },
  "include": ["src"],
  "exclude": ["node_modules", "dist"]
}
```

**Strict Mode Enforcement:**

1. **No Implicit `any`:**
   ```typescript
   // ❌ Error: parameter 'x' implicitly has 'any' type
   function add(x, y) { return x + y; }
   
   // ✅ Correct: explicit types
   function add(x: number, y: number): number { return x + y; }
   ```

2. **Null/Undefined Checks:**
   ```typescript
   // ❌ Error: object is possibly 'undefined'
   function getName(user: User) { return user.name.toUpperCase(); }
   
   // ✅ Correct: null check
   function getName(user: User | null) {
     if (!user) return 'Unknown';
     return user.name.toUpperCase();
   }
   ```

3. **No Unused Variables:**
   ```typescript
   // ❌ Error: unused variable 'foo'
   const foo = 'bar';
   
   // ✅ Correct: used or prefixed with _
   const foo = 'bar';
   console.log(foo);
   ```

**Escape Hatch (When Necessary):**

Use `as const` for complex type patterns:

```typescript
// Narrow type at runtime
const workflowStates = ['draft', 'active', 'completed'] as const;
type WorkflowState = typeof workflowStates[number];

// Type assertion for third-party libraries (last resort)
const data = JSON.parse(jsonString) as WorkflowData;
```

**Component Type Definitions:**

```typescript
// src/types/domain.ts
export interface User {
  id: string;
  email: string;
  name: string;
  role: 'admin' | 'participant' | 'viewer';
}

export interface Workflow {
  id: string;
  name: string;
  status: 'draft' | 'active' | 'completed';
  ownerId: string;
  createdAt: Date;
  updatedAt: Date;
}

// src/features/workflows/types.ts
export interface WorkflowListProps {
  workflows: Workflow[];
  onSelect: (workflow: Workflow) => void;
  isLoading: boolean;
}

// src/features/workflows/components/WorkflowList.tsx
export const WorkflowList: React.FC<WorkflowListProps> = ({ 
  workflows, onSelect, isLoading 
}) => {
  // TypeScript catches errors here - all props typed
  return (...);
};
```

**Rationale:**

1. **Catch Bugs Early:**
   - Compile-time errors instead of runtime crashes
   - Null/undefined handling enforced
   - Unused code detected automatically

2. **Better IDE Support:**
   - IntelliSense works better with strict types
   - Refactoring is safer (IDE knows all usages)
   - Auto-completion is more accurate

3. **Maintainability:**
   - Self-documenting code (types are documentation)
   - Onboarding new developers easier
   - Refactoring later is safer

4. **Team Scaling:**
   - Strict mode prevents common mistakes
   - Enforces consistency across developers
   - Reduces code review time

**Alternatives (Not Selected):**
- **Standard Mode** - Too permissive, misses bugs
- **Gradual Typing** - Lacks consistency, creates chaos

---

### Frontend Architecture Summary Table

| Aspect | Decision | Technology | Version | Rationale |
|--------|----------|-----------|---------|-----------|
| **State Management** | Zustand + TanStack Query | Zustand 4.5+ / React Query 5.x | 2KB / Dynamic | Lightweight, clear separation (client ↔ server state) |
| **Component Structure** | Feature-Based + Shared | React 18+ | Modular | Team scalability, code cohesion |
| **Routing** | React Router Declarative | React Router v7 | Latest | Fast to implement, lazy code splitting |
| **Performance** | Code Splitting + Bundle Opt | Vite | Latest | 50-70% faster initial load |
| **Type Safety** | Strict Mode + Escape Hatch | TypeScript 5.x | Strict | Compile-time error detection |

**Frontend Stack Summary:**

```
┌─────────────────────────────────────────────────────┐
│              React Frontend (SPA)                   │
├─────────────────────────────────────────────────────┤
│ • UI Framework: React 18+ (JSX)                     │
│ • Build Tool: Vite (fast refresh, tree-shaking)    │
│ • Language: TypeScript (strict mode)                │
│ • State: Zustand (global) + TanStack Query (server) │
│ • Routing: React Router v7 (lazy code split)       │
│ • Styling: Tailwind CSS (utility-first)            │
│ • UI Components: Headless UI + custom design       │
│ • Icons: Lucide React (lightweight, customizable)  │
│ • Form Handling: React Hook Form + Zod validation  │
│ • Real-Time: SignalR (WebSocket integration)       │
├─────────────────────────────────────────────────────┤
│ Bundle Size: 120-150KB gzipped (initial)            │
│ TTI (Time to Interactive): 1.2-1.8s (3G/4G)        │
│ Code Splitting: Per-route (~20-40KB each)          │
│ Performance: Grade A (Lighthouse)                   │
└─────────────────────────────────────────────────────┘
```

---

### Frontend Architecture Cascading Implications

**Impacts on Other Architectural Decisions:**

| Decision | Impact | Effect |
|----------|--------|--------|
| **API Design (REST + RPC)** | Frontend uses both CRUD + action endpoints | Client SDK generation per endpoint type |
| **Authentication (JWT Tokens)** | Frontend stores tokens in HttpOnly cookies | No XSS vulnerability, automatic token refresh |
| **Error Handling (ProblemDetails)** | Frontend intercepts and displays standardized errors | Consistent error UX across app |
| **WebSocket Errors (SignalR)** | Frontend receives structured conflict/error messages | Real-time UI updates for collaboration |
| **Rate Limiting (Per-User)** | Frontend respects 429 errors + Retry-After headers | Exponential backoff + user notification |
| **API Versioning (/api/v1/)** | Frontend hardcodes version in requests | Multiple API versions require separate clients |
| **Data Architecture (JSONB State)** | Frontend caches workflow decisions as objects | Optimistic updates possible, conflict detection |

---

### Frontend Implementation Roadmap (Timeline)

**Week 1-2: Project Setup + Core Infrastructure**
- ✅ Vite + React 18 + TypeScript (strict mode)
- ✅ Route structure (React Router v7)
- ✅ Zustand store initialization (auth + ui state)
- ✅ TanStack Query client configuration
- ✅ API client (axios with interceptors)

**Week 3: Authentication + Layout**
- ✅ Login/Register pages
- ✅ JWT token handling + refresh logic
- ✅ Protected route wrapper
- ✅ Main layout (Navbar, Sidebar, Main content area)

**Week 4: Core Features (Workflows)**
- ✅ Workflow list + filtering
- ✅ Workflow detail view
- ✅ Workflow create form
- ✅ Real-time status updates (WebSocket)

**Week 5: Decision Management**
- ✅ Decision tree visualization
- ✅ Decision details + approval form
- ✅ Conflict resolution UI
- ✅ Real-time decision updates

**Week 6: Collaboration Features**
- ✅ Active user badges
- ✅ Real-time conflict detection
- ✅ Notification system
- ✅ Session persistence

**Week 7: Settings + Documentation**
- ✅ User preferences
- ✅ API documentation viewer
- ✅ Help/Getting started guide

**Week 8: Testing + Optimization**
- ✅ Performance testing + bundle optimization
- ✅ Accessibility testing (WCAG 2.1 AA)
- ✅ Cross-browser testing
- ✅ User acceptance testing

---

## Infrastructure & Deployment

### Overview: Self-Hosted Cloud-Native Architecture

bmadServer is designed as a **self-hosted, cloud-agnostic** application using **containerized microservices** orchestrated by .NET Aspire. The deployment strategy prioritizes simplicity for MVP (Docker Compose) with a clear upgrade path to Kubernetes for scaling.

**Core Principles:**
- ✅ **Self-Hosted:** Deploy to any Linux server; no cloud vendor lock-in
- ✅ **Cloud-Native:** Containerized services with standardized deployment artifacts
- ✅ **Progressive:** Start with Docker Compose, upgrade to Kubernetes without code changes
- ✅ **Observable:** Structured logging, metrics, and health checks built-in
- ✅ **Resilient:** Graceful degradation, auto-restart, circuit breakers

---

### Decision 5.1: Hosting & Deployment Strategy

**Selected Option: A - Self-Hosted Docker Compose (MVP) → Kubernetes (Phase 2) (Recommended)**

**Selected Technologies:**
- **MVP Deployment:** Docker Compose (single server or Docker Swarm)
- **Phase 2 Deployment:** Kubernetes manifests (generated by Aspire)
- **Container Registry:** Docker Hub (public) or Self-Hosted Registry (private)
- **Server OS:** Linux (Ubuntu 22.04 LTS recommended)

**Deployment Topology (MVP):**

```
┌────────────────────────────────────────────────────────┐
│         Single Linux Server (Digital Ocean / Hetzner)  │
├────────────────────────────────────────────────────────┤
│                                                        │
│  Docker Compose Stack:                                │
│  ┌──────────────────────────────────────────────────┐ │
│  │ Nginx Reverse Proxy (Port 80 → 443)             │ │
│  │ Let's Encrypt SSL/TLS Certificates              │ │
│  └──────────────────────────────────────────────────┘ │
│                          ↓                             │
│  ┌──────────────────────────────────────────────────┐ │
│  │ ASP.NET Core API Service (Kestrel)              │ │
│  │ - REST endpoints (/api/v1/*)                    │ │
│  │ - WebSocket (SignalR)                           │ │
│  │ - Rate limiting, auth middleware                │ │
│  └──────────────────────────────────────────────────┘ │
│                          ↓                             │
│  ┌──────────────────────────────────────────────────┐ │
│  │ PostgreSQL 17 Container                         │ │
│  │ - Persistent volume: /data/postgres/            │ │
│  │ - Automated backups: hourly + daily             │ │
│  │ - Connection pooling: PgBouncer                 │ │
│  └──────────────────────────────────────────────────┘ │
│                                                        │
│  ┌──────────────────────────────────────────────────┐ │
│  │ Redis 7 Container (optional, Phase 2)           │ │
│  │ - Session caching                               │ │
│  │ - Rate limit counters                           │ │
│  │ - Distributed tasks                             │ │
│  └──────────────────────────────────────────────────┘ │
│                                                        │
│  Monitoring Stack:                                    │
│  ┌──────────────────────────────────────────────────┐ │
│  │ Prometheus (metrics collection)                 │ │
│  │ Grafana (dashboards + alerts)                   │ │
│  │ Application Insights (optional)                 │ │
│  └──────────────────────────────────────────────────┘ │
│                                                        │
│  Storage:                                             │
│  ├─ /data/postgres/ (database)                        │
│  ├─ /data/backups/ (daily backups)                    │
│  └─ /data/logs/ (application logs)                    │
│                                                        │
└────────────────────────────────────────────────────────┘
```

**Docker Compose Configuration (docker-compose.yml):**

```yaml
version: '3.9'

services:
  # Nginx Reverse Proxy with SSL
  nginx:
    image: nginx:alpine
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./nginx.conf:/etc/nginx/nginx.conf
      - ./ssl:/etc/nginx/ssl
    depends_on:
      - api
    restart: always

  # ASP.NET Core API Service
  api:
    build: ./src/BmadServer.ApiService
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=Server=postgres;Port=5432;Database=bmadserver;User Id=bmad;Password=${PG_PASSWORD}
      - Jwt__Secret=${JWT_SECRET}
      - Jwt__Issuer=https://bmadserver.local
      - SignalR__ConnectionString=Server=postgres;Port=5432;Database=bmadserver;User Id=bmad;Password=${PG_PASSWORD}
    depends_on:
      - postgres
    restart: always
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:5000/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s

  # PostgreSQL Database
  postgres:
    image: postgres:17-alpine
    environment:
      POSTGRES_DB: bmadserver
      POSTGRES_USER: bmad
      POSTGRES_PASSWORD: ${PG_PASSWORD}
    volumes:
      - postgres_data:/var/lib/postgresql/data
      - ./backups:/var/backups
    ports:
      - "5432:5432"  # For local admin access
    restart: always
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U bmad -d bmadserver"]
      interval: 10s
      timeout: 5s
      retries: 5

  # pgAdmin (Database Admin UI - optional)
  pgadmin:
    image: dpage/pgadmin4
    environment:
      PGADMIN_DEFAULT_EMAIL: admin@bmadserver.local
      PGADMIN_DEFAULT_PASSWORD: ${PGADMIN_PASSWORD}
    ports:
      - "5050:80"  # For internal admin access only
    depends_on:
      - postgres
    restart: always

  # Prometheus (Metrics)
  prometheus:
    image: prom/prometheus
    volumes:
      - ./prometheus.yml:/etc/prometheus/prometheus.yml
      - prometheus_data:/prometheus
    ports:
      - "9090:9090"
    restart: always

  # Grafana (Dashboards)
  grafana:
    image: grafana/grafana
    ports:
      - "3000:3000"
    environment:
      - GF_SECURITY_ADMIN_PASSWORD=${GRAFANA_PASSWORD}
    volumes:
      - grafana_data:/var/lib/grafana
      - ./grafana-dashboards:/etc/grafana/provisioning/dashboards
    depends_on:
      - prometheus
    restart: always

volumes:
  postgres_data:
  prometheus_data:
  grafana_data:
```

**Deployment Process:**

1. **Generate Artifacts:**
   ```bash
   aspire publish -o docker-compose-artifacts
   ```

2. **Deploy to Server:**
   ```bash
   scp docker-compose.yml .env username@server:/opt/bmadserver/
   ssh username@server
   cd /opt/bmadserver
   docker compose up -d
   ```

3. **Verify Deployment:**
   ```bash
   docker compose ps
   curl https://bmadserver.local/health
   ```

**Rationale:**

1. **MVP Simplicity:**
   - Single server = easier management
   - No Kubernetes complexity (learning curve, operational overhead)
   - Cost-effective (shared hosting or small VPS)

2. **Scalability Path:**
   - Aspire generates Kubernetes manifests automatically
   - No code/config changes needed for upgrade
   - Clear migration path as user base grows

3. **Self-Hosted Benefits:**
   - Full data control (self-hosted PostgreSQL)
   - No vendor lock-in
   - Cost predictable (pay for servers, not per-request)

**Alternatives (Not Selected):**
- **Azure App Service** - Cloud vendor lock-in, complicates self-hosting
- **Kubernetes from Day 1** - Overkill for MVP, requires DevOps expertise
- **Serverless (Lambda/Functions)** - Doesn't fit persistent database + WebSocket requirements

---

### Decision 5.2: CI/CD Pipeline Strategy

**Selected Option: A - GitHub Actions + Docker Build + Docker Hub Push (Recommended)**

**Selected Technologies:**
- **CI/CD Tool:** GitHub Actions (built-in to GitHub, no separate account)
- **Container Registry:** Docker Hub (free for public images)
- **Build Automation:** GitHub Actions Docker build action
- **Deployment:** Manual SSH or webhook-triggered

**GitHub Actions Workflow (.github/workflows/deploy.yml):**

```yaml
name: Build, Test, and Deploy

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main]

env:
  REGISTRY: docker.io
  IMAGE_NAME: crissbiad/bmadserver

jobs:
  build-and-test:
    runs-on: ubuntu-latest
    
    steps:
      # Checkout code
      - uses: actions/checkout@v4

      # Setup .NET environment
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      # Run tests
      - name: Run Tests
        run: dotnet test --configuration Release --no-build

      # Build solution
      - name: Build Solution
        run: dotnet build --configuration Release

  build-and-push-docker:
    runs-on: ubuntu-latest
    needs: build-and-test
    if: github.ref == 'refs/heads/main'  # Only on main branch
    
    permissions:
      contents: read
      packages: write

    steps:
      - uses: actions/checkout@v4

      # Login to Docker Hub
      - name: Log in to Docker Hub
        uses: docker/login-action@v3
        with:
          username: ${{ secrets.DOCKERHUB_USERNAME }}
          password: ${{ secrets.DOCKERHUB_TOKEN }}

      # Extract metadata (version, tags)
      - name: Extract metadata
        id: meta
        uses: docker/metadata-action@v5
        with:
          images: ${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}
          tags: |
            type=sha,prefix={{branch}}-
            type=semver,pattern={{version}}
            type=semver,pattern={{major}}.{{minor}}
            type=raw,value=latest,enable={{is_default_branch}}

      # Build and push Docker image
      - name: Build and push Docker image
        uses: docker/build-push-action@v5
        with:
          context: .
          push: true
          tags: ${{ steps.meta.outputs.tags }}
          labels: ${{ steps.meta.outputs.labels }}

  deploy-to-production:
    runs-on: ubuntu-latest
    needs: build-and-push-docker
    if: github.ref == 'refs/heads/main'
    
    steps:
      # Deploy via SSH
      - name: Deploy to Production Server
        uses: appleboy/ssh-action@master
        with:
          host: ${{ secrets.DEPLOY_HOST }}
          username: ${{ secrets.DEPLOY_USER }}
          key: ${{ secrets.DEPLOY_KEY }}
          script: |
            cd /opt/bmadserver
            docker compose pull
            docker compose up -d
            docker compose logs api
            # Run migrations
            docker compose exec -T api dotnet ef database update
```

**Deployment Pipeline:**

```
┌──────────────────────────────┐
│ Git Commit to Main Branch    │
└─────────────┬────────────────┘
              ↓
┌──────────────────────────────┐
│ GitHub Actions Triggered     │
│ - Checkout code              │
│ - Setup .NET 10              │
│ - Run unit tests             │
│ - Build solution             │
└─────────────┬────────────────┘
              ↓
         Tests Pass?
        /            \
      Yes            No → Notify dev (stop pipeline)
      ↓
┌──────────────────────────────┐
│ Build Docker Image           │
│ - Multi-stage build          │
│ - Optimization               │
└─────────────┬────────────────┘
              ↓
┌──────────────────────────────┐
│ Push to Docker Hub           │
│ - Tag: branch-SHA / latest   │
│ - Sign image (optional)      │
└─────────────┬────────────────┘
              ↓
┌──────────────────────────────┐
│ Deploy to Production         │
│ - SSH to server              │
│ - Docker compose pull        │
│ - Docker compose up -d       │
│ - Run migrations             │
│ - Health check               │
└──────────────────────────────┘
```

**Rationale:**

1. **GitHub Actions Benefits:**
   - Free with GitHub (2000 free minutes/month)
   - No separate CI/CD platform account
   - Tight integration with repositories
   - Secret management built-in

2. **Docker Hub Benefits:**
   - Public images free (perfect for open-source culture)
   - Private images available (paid tier)
   - Automated builds optional
   - Image scanning for vulnerabilities

3. **Quality Gates:**
   - Unit tests must pass before build
   - Docker image built only on main branch
   - Health checks verify deployment success

**Alternatives (Not Selected):**
- **Azure DevOps** - More complex, overkill for MVP
- **GitLab CI/CD** - Requires separate GitLab account
- **Manual SSH Deploy** - Error-prone, doesn't scale

---

### Decision 5.3: Environment Configuration Strategy

**Selected Option: A - Kubernetes-Style ConfigMaps + Secrets (Recommended)**

**Selected Technologies:**
- **Configuration:** Environment variables + docker-compose .env file
- **Secrets:** Encrypted .env file (gitignore'd, backed up securely)
- **Multi-Environment:** dev/staging/prod with separate docker-compose files

**Environment Structure:**

```
/opt/bmadserver/
├── docker-compose.yml           # Base configuration
├── docker-compose.prod.yml      # Production overrides
├── .env.example                 # Template for secrets (committed)
├── .env                         # Production secrets (gitignore'd)
├── .env.dev                     # Dev secrets
├── nginx.conf                   # Reverse proxy config
├── prometheus.yml               # Metrics scraper config
├── grafana-dashboards/          # Custom dashboards
└── backups/                     # Database backups
```

**Configuration Management:**

1. **Environment Variables (docker-compose.yml):**
   ```yaml
   environment:
     - ASPNETCORE_ENVIRONMENT=${ASPNETCORE_ENVIRONMENT:-Production}
     - LOG_LEVEL=${LOG_LEVEL:-Information}
     - ConnectionStrings__DefaultConnection=Server=postgres;Port=5432;Database=${DB_NAME};User Id=${DB_USER};Password=${DB_PASSWORD}
     - Jwt__Secret=${JWT_SECRET}
     - Jwt__ExpirationMinutes=${JWT_EXPIRATION_MINUTES:-15}
     - SignalR__MaxConcurrentUsers=${SIGNALR_MAX_USERS:-100}
     - RateLimit__RequestsPerMinute=${RATE_LIMIT_RPM:-60}
   ```

2. **Secrets Management (.env file - never committed):**
   ```env
   # Database
   DB_NAME=bmadserver
   DB_USER=bmad
   DB_PASSWORD=<generate-strong-password>
   
   # Security
   JWT_SECRET=<generate-base64-encoded-256bit-key>
   PGADMIN_PASSWORD=<secure-password>
   GRAFANA_PASSWORD=<secure-password>
   
   # Docker Hub (for automated pulls)
   DOCKERHUB_USERNAME=<your-username>
   DOCKERHUB_TOKEN=<your-token>
   
   # Deployment
   ASPNETCORE_ENVIRONMENT=Production
   LOG_LEVEL=Information
   
   # Scaling
   SIGNALR_MAX_USERS=100
   RATE_LIMIT_RPM=60
   JWT_EXPIRATION_MINUTES=15
   ```

3. **Secret Rotation Strategy:**
   - JWT_SECRET: Rotate annually (no impact - validates bearer tokens)
   - DB_PASSWORD: Rotate quarterly (managed via automated script)
   - Backup secrets separately from application

**Multi-Environment Deployment:**

```bash
# Development (local)
docker compose -f docker-compose.yml -f docker-compose.dev.yml up -d

# Staging (test server)
docker compose -f docker-compose.yml -f docker-compose.staging.yml up -d

# Production (live server)
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d
```

**Rationale:**

1. **Kubernetes-Ready:**
   - Easy migration from ConfigMaps to Kubernetes Secrets
   - Same concepts, same syntax

2. **Security:**
   - Secrets never in git (encrypted, backed up separately)
   - Environment variables prevent hardcoding
   - Clear separation of concerns

3. **Operational Clarity:**
   - All configuration in one place
   - Easy to rotate secrets
   - Audit trail via backups

**Alternatives (Not Selected):**
- **Hardcoded configuration** - Security risk
- **Complex config servers** - Overkill for MVP

---

### Decision 5.4: Monitoring & Logging Strategy

**Selected Option: A - Prometheus + Grafana (MVP) → ELK/Loki (Phase 2) (Recommended)**

**Selected Technologies:**
- **Metrics:** Prometheus (time-series database)
- **Visualization:** Grafana (dashboards + alerts)
- **Application Logging:** Structured logging to stdout/file
- **Log Aggregation (Phase 2):** ELK Stack or Loki

**Monitoring Architecture:**

```
┌────────────────────────────────────────────────┐
│        Application & Infrastructure             │
├────────────────────────────────────────────────┤
│                                                │
│  ASP.NET Core API                             │
│  ├─ Prometheus metrics endpoint                │
│  │  (.net runtime, request latency, errors)   │
│  ├─ Structured logging (Serilog)             │
│  │  (to stdout, JSON format)                  │
│  └─ Health checks                             │
│                                                │
│  PostgreSQL                                   │
│  ├─ pg_stat_statements (slow query log)      │
│  ├─ Connection pool metrics                   │
│  └─ Replication lag                           │
│                                                │
│  Nginx                                        │
│  ├─ Request counts                            │
│  ├─ Response times                            │
│  └─ Error rates                               │
│                                                │
└────────────────────────────────────────────────┘
              ↓
┌────────────────────────────────────────────────┐
│  Prometheus (Metrics Collection)               │
│  ├─ Scrapes /metrics endpoint every 15s       │
│  ├─ Stores 15 days of data (configurable)     │
│  └─ Alerts on threshold violations            │
└────────────────────────────────────────────────┘
              ↓
┌────────────────────────────────────────────────┐
│  Grafana (Visualization)                       │
│  ├─ Dashboard: System Health                  │
│  ├─ Dashboard: API Performance                │
│  ├─ Dashboard: Database Metrics               │
│  └─ Alerts: Email/Slack notifications         │
└────────────────────────────────────────────────┘
```

**ASP.NET Core Metrics Configuration:**

```csharp
// Program.cs
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Sinks.ApplicationInsights;

var builder = WebApplication.CreateBuilder(args);

// Structured Logging (Serilog)
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(new RenderedCompactJsonFormatter()) // JSON to stdout
    .WriteTo.File(
        "logs/app-.txt",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
    )
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "bmadserver-api")
    .CreateLogger();

builder.Host.UseSerilog();

// Prometheus Metrics
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddPrometheusExporter());

// Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>()
    .AddCheck("memory", () => GC.GetTotalMemory(false) < 1_000_000_000 
        ? HealthCheckResult.Healthy() 
        : HealthCheckResult.Unhealthy());

var app = builder.Build();

// Metrics endpoint
app.MapPrometheusScrapingEndpoint();

// Health endpoint
app.MapHealthChecks("/health");

app.Run();
```

**Grafana Dashboards (Pre-configured):**

1. **System Health Dashboard:**
   - CPU usage
   - Memory usage
   - Disk I/O
   - Network I/O
   - Container restarts

2. **API Performance Dashboard:**
   - Request rate (requests/sec)
   - Response time (p50, p95, p99 latency)
   - Error rate (by status code)
   - WebSocket connections
   - JWT token validations

3. **Database Dashboard:**
   - Query execution time
   - Slow queries (>1s)
   - Connection pool usage
   - Transaction rate
   - Replication lag (if applicable)

**Alert Rules:**

```yaml
# prometheus-alerts.yml
groups:
  - name: api_alerts
    interval: 30s
    rules:
      - alert: HighErrorRate
        expr: rate(http_requests_total{status=~"5.."}[5m]) > 0.05
        for: 5m
        annotations:
          summary: "API returning 5xx errors"
          
      - alert: HighLatency
        expr: http_request_duration_seconds{le="1"} < 0.8
        for: 10m
        annotations:
          summary: "API response time >1s"
          
      - alert: DatabaseConnectionPoolExhausted
        expr: pg_stat_activity_count / pg_setting_max_connections > 0.8
        for: 5m
        annotations:
          summary: "Database connection pool >80% utilization"
          
      - alert: DiskSpaceRunningOut
        expr: node_filesystem_free_bytes{mountpoint="/"} < 5e9
        annotations:
          summary: "Disk space <5GB remaining"
```

**Log Aggregation Phase 2 (Loki):**

```yaml
# loki-config.yml (future)
auth_enabled: false
ingester:
  chunk_idle_period: 3m
  max_chunk_age: 1h
  max_streams_limit_per_user: 33600
  lifecycler:
    ring:
      kvstore:
        store: inmemory
      replication_factor: 1

schema_config:
  configs:
    - from: 2020-05-15
      store: boltdb
      object_store: filesystem
      schema:
        version: v11
        index:
          prefix: index_
          period: 24h

server:
  http_listen_port: 3100
```

**Rationale:**

1. **Prometheus Benefits:**
   - Time-series database perfect for metrics
   - Built-in for containerized apps
   - Lightweight (fits on single server)

2. **Grafana Benefits:**
   - Beautiful dashboards
   - Alert management
   - Multi-source support (Prometheus + logs future)

3. **Structured Logging:**
   - Machine-readable JSON format
   - Searchable (future Loki integration)
   - Rich context (user, request ID, operation)

**Alternatives (Not Selected):**
- **Application Insights** - Vendor lock-in (Azure), expensive
- **Datadog** - Expensive for small team
- **No monitoring** - Risk for production issues

---

### Decision 5.5: Scaling & High Availability Strategy

**Selected Option: A - Horizontal Scaling via Docker Swarm (MVP) → Kubernetes (Phase 2) (Recommended)**

**Selected Technologies:**
- **MVP Scaling:** Docker Compose on multiple servers + Nginx load balancing
- **Phase 2:** Kubernetes with auto-scaling policies

**Scaling Architecture (Phase 2):**

```
┌──────────────────────────────────────────────────────┐
│  Kubernetes Cluster (3+ nodes)                       │
├──────────────────────────────────────────────────────┤
│                                                      │
│  Ingress Controller (Nginx)                          │
│  ├─ TLS termination                                 │
│  ├─ Request routing                                 │
│  └─ Rate limiting (IP-based)                        │
│          ↓                                            │
│  ┌──────────────────────────────────────────────┐   │
│  │  API Deployment (HPA enabled)                │   │
│  │  Min: 2 replicas                             │   │
│  │  Max: 10 replicas                            │   │
│  │  Scale on: CPU >70%, Memory >80%, RPS >500   │   │
│  │  ├─ Pod 1: API Service                       │   │
│  │  ├─ Pod 2: API Service                       │   │
│  │  ├─ Pod 3: API Service (when scaled)         │   │
│  │  └─ ...                                       │   │
│  └──────────────────────────────────────────────┘   │
│          ↓                                            │
│  ┌──────────────────────────────────────────────┐   │
│  │  PostgreSQL StatefulSet                      │   │
│  │  ├─ Master: Read/Write                       │   │
│  │  ├─ Replica 1: Read-only (streaming replication) │
│  │  └─ Replica 2: Read-only (backup replica)   │   │
│  │  Persistent Volume (distributed storage)     │   │
│  └──────────────────────────────────────────────┘   │
│          ↓                                            │
│  ┌──────────────────────────────────────────────┐   │
│  │  Redis Cache (optional, for Phase 2)         │   │
│  │  ├─ Master: Write operations                 │   │
│  │  └─ Replica: Read operations                 │   │
│  └──────────────────────────────────────────────┘   │
│                                                      │
│  Monitoring:                                         │
│  ├─ Prometheus (multi-server)                       │
│  ├─ Grafana (multi-source)                          │
│  ├─ Alertmanager (notification routing)             │
│  └─ Loki (log aggregation)                          │
│                                                      │
└──────────────────────────────────────────────────────┘
```

**Horizontal Pod Autoscaling (Phase 2):**

```yaml
# hpa.yml
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: api-hpa
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: api
  minReplicas: 2
  maxReplicas: 10
  metrics:
    - type: Resource
      resource:
        name: cpu
        target:
          type: Utilization
          averageUtilization: 70
    - type: Resource
      resource:
        name: memory
        target:
          type: Utilization
          averageUtilization: 80
  behavior:
    scaleUp:
      stabilizationWindowSeconds: 300
      policies:
        - type: Percent
          value: 100
          periodSeconds: 60
    scaleDown:
      stabilizationWindowSeconds: 300
      policies:
        - type: Percent
          value: 50
          periodSeconds: 60
```

**Database Scaling (Phase 2):**

```sql
-- PostgreSQL Read Replica Configuration
-- Primary server: pg-primary.local
-- Replica 1: pg-replica-1.local
-- Replica 2: pg-replica-2.local

-- On Primary:
-- WAL archiving enabled for streaming replication
-- max_wal_senders = 5 (allow 5 replica connections)
-- wal_keep_segments = 1000 (retain WAL for replicas)

-- On Replicas:
-- Recovery configuration for streaming standby
-- standby_mode = 'on'
-- primary_conninfo = 'host=pg-primary.local user=replication password=XXX'
-- trigger_file = '/tmp/promote_to_primary'

-- Application uses pg_partman for table partitioning:
-- Workflow table partitioned by date
-- Decision table partitioned by status
-- Query performance improves with auto-pruned partitions
```

**MVP Scaling Strategy (Single Server → Multiple Servers):**

1. **Phase 1 (Current - Single Server):**
   - All services on one server
   - Vertical scaling: increase CPU/RAM
   - Max ~100 concurrent users

2. **Phase 2 (Multi-Server):**
   - Docker Swarm (easier than Kubernetes)
   - API layer: load-balanced across 3 servers
   - Database: primary + 2 replicas
   - Cache layer: Redis cluster
   - Max ~1000 concurrent users

3. **Phase 3 (Full Kubernetes):**
   - Kubernetes cluster (3+ master, 5+ worker nodes)
   - Auto-scaling based on metrics
   - Multi-availability zone deployment (if cloud)
   - Max ~10,000+ concurrent users

**Load Testing Baseline:**

```bash
# Use Apache Bench or k6 for load testing
# Single server capacity:
# - 500 req/sec (API layer)
# - 100 WebSocket connections
# - 10 concurrent workflow operations
# - <200ms p95 latency

# Test script (k6):
import http from 'k6/http';
import { check } from 'k6';

export let options = {
  stages: [
    { duration: '2m', target: 100 },  // Ramp up
    { duration: '5m', target: 100 },  // Stay
    { duration: '2m', target: 0 },    // Ramp down
  ],
};

export default function() {
  let res = http.get('https://bmadserver.local/api/v1/workflows');
  check(res, { 'status is 200': (r) => r.status === 200 });
}
```

**Rationale:**

1. **Progressive Scaling:**
   - Start simple (single server)
   - Upgrade path is clear (Docker Swarm → Kubernetes)
   - No over-engineering for MVP

2. **Resilience:**
   - Horizontal scaling (add more servers)
   - Database replication (read scaling)
   - Cache layer (request optimization)

3. **Cost-Effective:**
   - MVP: Single server ($10-20/month)
   - Phase 2: 3 servers ($50-100/month)
   - Phase 3: Kubernetes ($200-500/month, scales with usage)

**Alternatives (Not Selected):**
- **Single Server Only** - No redundancy, hits limits quickly
- **Kubernetes from Day 1** - Operational overhead outweighs benefits for MVP

---

### Infrastructure & Deployment Summary Table

| Aspect | Decision | Technology | Target | Rationale |
|--------|----------|-----------|--------|-----------|
| **Hosting** | Docker Compose (MVP) | Docker 25.x + Linux | Single server + upgrade path | Self-hosted, progressive scaling |
| **CI/CD** | GitHub Actions | Actions + Docker Hub | Automated builds/deployments | Free, integrated, secure |
| **Environment Config** | .env + ConfigMaps | Environment variables | All stages (dev/staging/prod) | Kubernetes-ready, secure secrets |
| **Monitoring** | Prometheus + Grafana | Prom 2.45+ / Grafana 10+ | Real-time visibility | Cloud-agnostic, customizable |
| **Scaling** | Docker Swarm (Phase 2) | Kubernetes (Phase 3) | 100→1000→10K users | Progressive, proven patterns |

**Deployment Timeline:**

| Timeline | Milestone | Deliverables |
|----------|-----------|--------------|
| **Week 1** | Local Development | Docker Compose stack, environment setup |
| **Week 2-4** | Staging Deployment | GitHub Actions pipeline, staging server |
| **Week 5-7** | Production Deployment | Production secrets, Prometheus/Grafana alerts |
| **Week 8** | Load Testing | Baseline metrics, scaling procedures documented |
| **Phase 2** | Multi-Server Upgrade | Docker Swarm manifests, replication setup |
| **Phase 3** | Kubernetes Migration | K8s manifests, HPA policies, rolling updates |

---

### Implementation Checklist

**Infrastructure Prerequisites:**
- [ ] Linux server provisioned (Ubuntu 22.04 LTS, 2GB+ RAM)
- [ ] Domain DNS configured (bmadserver.local or custom)
- [ ] SSH key-based access configured
- [ ] Docker & Docker Compose installed
- [ ] GitHub Actions secrets configured (DOCKER_HUB_TOKEN, DEPLOY_KEY, etc.)
- [ ] PostgreSQL backups location secured
- [ ] SSL certificates provisioned (Let's Encrypt via Nginx)

**Deployment Checklist:**
- [ ] docker-compose.yml tested locally
- [ ] Environment variables documented in .env.example
- [ ] Database migrations tested
- [ ] Health checks operational (curl /health)
- [ ] Logging streams to stdout (for Docker inspection)
- [ ] Prometheus scraping configured
- [ ] Grafana dashboards imported
- [ ] Alert thresholds calibrated
- [ ] Rollback procedures documented

**Security Checklist:**
- [ ] SSL/TLS certificates valid and auto-renewed
- [ ] Database credentials rotated (strong random)
- [ ] JWT secret securely generated
- [ ] API keys/tokens stored in .env (never in code)
- [ ] Network policies restrict database access to API only
- [ ] Firewall rules: 80/443 public, 5432/5050 private
- [ ] Regular security updates applied (Docker images scanned)

---

## Next Steps: Completion & Handoff

### Step 5 Completion (Implementation Patterns)

Once all 5 categories are approved, we proceed to **Step 5: Implementation Patterns**, which will:

1. **Create Implementation Guide:**
   - Code generation patterns (scaffolding)
   - Folder structure templates
   - API endpoint patterns
   - Database model patterns
   - Component patterns (React)

2. **Define Developer Onboarding:**
   - Local setup script
   - Architecture overview walkthrough
   - First-story execution guide

3. **Create Architecture Decision Record (ADR):**
   - Rationale for each architectural decision
   - Trade-offs considered
   - Future upgrade paths

### Final Deliverables

**Comprehensive Architecture Document (This file)**
- 5 Categories × 5 Decisions each = 25 decisions documented
- Rationale, trade-offs, alternatives for each
- Implementation examples and code templates
- Cascading implications mapped

**Deployment Package**
- docker-compose.yml ready for deployment
- GitHub Actions workflow for CI/CD
- Prometheus/Grafana configuration
- Environment setup script

**Implementation Roadmap**
- 8-week sprint breakdown
- Team responsibilities (Cris, Sarah, Marcus)
- Risk mitigation strategies
- Dependency mapping

---