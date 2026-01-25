# COMPREHENSIVE ASPIRE ALIGNMENT ANALYSIS
## All Stories Cross-Referenced Against Aspire Documentation

**Analysis Date:** 2026-01-24  
**Status:** COMPLETE - Detailed findings with recommendations  
**Scope:** All 69 stories across 13 epics

---

## EXECUTIVE SUMMARY

### ✅ ALIGNMENT STATUS

| Category | Status | Details |
|----------|--------|---------|
| **Epic 1** | ✅ ALIGNED | Stories 1.1, 1.2 properly use Aspire-first approach |
| **Epic 2-13** | ⚠️ PARTIAL | Infrastructure patterns identified but no explicit Aspire integration yet |
| **Overall** | 🟡 REQUIRES UPDATE | 12/13 epics need Aspire component identification and integration patterns |

### 📊 KEY FINDINGS

**Total Stories:** 69  
**Stories with Aspire references:** 5 (Epic 1 only)  
**Stories needing Aspire integration:** 64  

**Infrastructure Components Found:**
- ✅ PostgreSQL (Epic 1, 2, 4, 6, 7, 8, 9, 11, 12, 13)
- ✅ SignalR - **VERIFIED**: `Aspire.Hosting.Azure.SignalR` available with local emulator support
- ⏳ Redis/Caching (Epic 10 - `Aspire.Hosting.Redis` available, implement when needed)
- ⏳ RabbitMQ/Messaging (Epics 5, 7, 13 - `Aspire.Hosting.RabbitMq` available, post-MVP)

---

## DETAILED FINDINGS BY EPIC

### EPIC 1: Aspire Foundation & Project Setup

#### Status: ✅ ALIGNED

**Stories Analyzed:**
1. 1.1 - Initialize Aspire Template ✅
2. 1.2 - Configure PostgreSQL via Aspire ✅
3. 1.3 - Docker Compose (CANCELLED - Superseded by Aspire) ✅
4. 1.4 - GitHub Actions CI/CD ⚠️ (No Aspire refs, but not needed for CI/CD)
5. 1.5 - Prometheus/Grafana (CANCELLED - Superseded by Aspire Dashboard) ✅
6. 1.6 - Project Documentation ⚠️ (References both Aspire and Docker Compose)

**Alignment Assessment:**
- ✅ Stories 1.1 & 1.2 correctly implement Aspire-first pattern
- ✅ Stories 1.3 & 1.5 properly cancelled with clear rationale (Aspire supersedes)
- ⚠️ Story 1.4 needs CI/CD patterns (not Aspire-specific, but should document Aspire project structure)
- ⚠️ Story 1.6 mixes Aspire and Docker Compose documentation (should prioritize Aspire)

**Recommendations:**
- [ ] Story 1.4: Add documentation about building Aspire projects in CI/CD
- [ ] Story 1.6: Update to prioritize Aspire patterns, note Docker Compose as post-MVP option
- [ ] Completed: No action needed for 1.1 & 1.2

---

### EPIC 2: User Authentication & Session Management

#### Status: ⚠️ REQUIRES UPDATE

**Stories:** 6 stories (2-1 through 2-6)

**Current State:**
- PostgreSQL mentioned (5 stories) - ✅ Will use Aspire from Story 1.2
- No infrastructure orchestration mentioned
- Uses standard ASP.NET Core DI patterns
- Database configuration inherited from Story 1.2

**Aspire Integration Analysis:**

| Story | Component | Current | Required | Status |
|-------|-----------|---------|----------|--------|
| 2-1 | PostgreSQL | ✓ Implicit | Uses Aspire from 1.2 | ✅ OK |
| 2-2 | PostgreSQL | ✓ Implicit | Uses Aspire from 1.2 | ✅ OK |
| 2-3 | PostgreSQL | ✓ Implicit | Uses Aspire from 1.2 | ✅ OK |
| 2-4 | PostgreSQL | ✓ Implicit | Uses Aspire from 1.2 | ✅ OK |
| 2-5 | PostgreSQL | ✓ Implicit | Uses Aspire from 1.2 | ✅ OK |
| 2-6 | PostgreSQL | ✓ Implicit | Uses Aspire from 1.2 | ✅ OK |

**Recommendations:**
- [ ] Add reference section to all stories linking to PROJECT-WIDE-RULES.md
- [ ] Clarify that PostgreSQL connections use Aspire's IConnectionStringProvider
- [ ] Document Aspire-managed secrets for auth tokens/credentials
- ✅ No new Aspire components needed (inherits from Epic 1)

---

### EPIC 3: Real-Time Chat Interface

#### Status: ✅ VERIFIED - Aspire SignalR Component Available

**Stories:** 6 stories (3-1 through 3-6)

**Current State:**
- Story 3.1 uses `dotnet add package Microsoft.AspNetCore.SignalR` (manual package)
- All stories reference PostgreSQL and message persistence
- No explicit service orchestration

**🎉 SIGNALR ASPIRE COMPONENT VERIFIED (2026-01-24):**

| Package | NuGet | Mode | Local Dev |
|---------|-------|------|-----------|
| `Aspire.Hosting.Azure.SignalR` | v13.1.0+ | Default/Serverless | ✅ Emulator available |

**Two Deployment Options:**

**Option A: Azure SignalR Service (Default Mode) - RECOMMENDED for Production**
```csharp
// AppHost/Program.cs
var signalR = builder.AddAzureSignalR("signalr");
var api = builder.AddProject<Projects.ApiService>("api")
    .WithReference(signalR)
    .WaitFor(signalR);
```
- Full hub support with `Microsoft.Azure.SignalR` package
- Uses `AddNamedAzureSignalR("signalr")` in consuming project
- Requires Azure subscription for production

**Option B: Azure SignalR Emulator (Serverless Mode) - For Local Development**
```csharp
// AppHost/Program.cs
using Aspire.Hosting.Azure;
var signalR = builder.AddAzureSignalR("signalr", AzureSignalRServiceMode.Serverless)
    .RunAsEmulator();
```
- Runs locally without Azure subscription
- Requires `Microsoft.Azure.SignalR.Management` package
- Uses `/negotiate` endpoint pattern instead of traditional hubs

**Option C: Self-Hosted SignalR (No Azure) - SIMPLEST for MVP**
```csharp
// No Aspire component needed - SignalR built into ASP.NET Core
// ApiService/Program.cs
builder.Services.AddSignalR();
app.MapHub<ChatHub>("/chat");
```
- Uses built-in ASP.NET Core SignalR
- No external dependency required
- Scales via sticky sessions or Redis backplane (future)

**RECOMMENDATION FOR bmadServer MVP:**
Use **Option C (Self-Hosted SignalR)** for MVP simplicity:
- No Azure dependency
- No additional Aspire configuration
- Built-in to ASP.NET Core
- Add Redis backplane later if horizontal scaling needed (Epic 10)

**Aspire Integration Analysis:**

| Component | Current | Aspire Option | Status |
|-----------|---------|---------------|--------|
| PostgreSQL | Implicit | ✅ Aspire.Hosting.PostgreSQL | ✅ Use from Epic 1 |
| SignalR | `dotnet add` | ✅ Self-hosted (ASP.NET Core) | ✅ **MVP: Use built-in** |
| Azure SignalR | Not needed | ✅ Aspire.Hosting.Azure.SignalR | ⏳ Future: Production scaling |
| Real-time Messaging | Not specified | Possible Redis backplane | ⏳ Defer to Epic 10 |

**Recommendations (UPDATED):**
- [x] ~~Verify if Aspire.Hosting.SignalR component exists on aspire.dev~~ **DONE: Yes, Azure.SignalR available**
- [x] Update Story 3.1 to document MVP approach:
  - **MVP:** Use built-in ASP.NET Core SignalR (no Aspire component needed)
  - **Future (Production scaling):** Use `Aspire.Hosting.Azure.SignalR` with emulator for local dev
- [ ] Add reference section to all stories linking to PROJECT-WIDE-RULES.md
- [ ] Clarify that database connections use Aspire from Epic 1

---

### EPIC 4: Workflow Orchestration Engine

#### Status: ⚠️ REQUIRES UPDATE

**Stories:** 7 stories (4-1 through 4-7)

**Current State:**
- All stories reference PostgreSQL for workflow state
- No infrastructure services mentioned
- Service-to-service communication may require messaging
- No explicit Aspire integration

**Aspire Integration Analysis:**

| Component | Mention | Aspire Pattern | Status |
|-----------|---------|----------------|--------|
| PostgreSQL | ✅ All stories | Aspire.Hosting.PostgreSQL | ✅ From Epic 1 |
| Service Communication | Implicit | May need messaging | ⚠️ See notes |
| Internal Agent Routing | ✅ Mentioned | In-process (MVP) | ✅ No Aspire needed |

**Key Finding:**
- Stories don't mention inter-service messaging requirements
- Agent routing is in-process (not distributed queue)
- Database persistence via Aspire PostgreSQL is sufficient

**Recommendations:**
- [ ] Add reference section to all stories linking to PROJECT-WIDE-RULES.md
- [ ] Clarify that workflow state persists via Aspire-managed PostgreSQL
- [ ] Document that agent routing is in-process (no external messaging required for MVP)
- [ ] Note: If distributed agents needed in future, will require Aspire messaging component (e.g., RabbitMQ)

---

### EPIC 5: Multi-Agent Collaboration

#### Status: ⚠️ REQUIRES UPDATE

**Stories:** 5 stories (5-1 through 5-5)

**Current State:**
- All reference PostgreSQL for state storage
- Inter-agent messaging mentioned (implicit)
- No explicit messaging infrastructure defined

**Aspire Integration Analysis:**

| Component | Mentioned | Current | Aspire Option | Status |
|-----------|-----------|---------|---------------|--------|
| PostgreSQL | ✅ Yes | Implicit | Aspire.Hosting.PostgreSQL | ✅ From Epic 1 |
| Messaging | ✅ Implicit | None | RabbitMQ / Kafka | ⚠️ Future epic |
| In-Process Communication | ✅ Yes | DI container | ServiceCollection | ✅ OK |

**Key Findings:**
- Agent collaboration uses in-process DI for MVP
- No explicit messaging infrastructure required yet
- Future: Distributed agents may need messaging (e.g., RabbitMQ via Aspire)

**Recommendations:**
- [ ] Add reference section to all stories linking to PROJECT-WIDE-RULES.md
- [ ] Clarify in-process agent collaboration pattern (no messaging needed for MVP)
- [ ] Document future messaging pattern: "When distributed agents needed, use `aspire add RabbitMq.Aspire`"
- [ ] Add planning note for future messaging architecture

---

### EPIC 6: Decision Management & Locking

#### Status: ⚠️ REQUIRES UPDATE

**Stories:** 5 stories (6-1 through 6-5)

**Current State:**
- All reference PostgreSQL for decision storage
- JSONB pattern mentioned for flexible schema
- No infrastructure orchestration

**Aspire Integration Analysis:**

| Component | Needed | Aspire Pattern | Status |
|-----------|--------|----------------|--------|
| PostgreSQL | ✅ Yes | Aspire.Hosting.PostgreSQL | ✅ From Epic 1 |
| JSONB Storage | ✅ Yes | Native PostgreSQL | ✅ OK |
| Locking Mechanism | ✅ Yes | PG Advisory Locks | ✅ Built-in |

**Recommendations:**
- [ ] Add reference section to all stories linking to PROJECT-WIDE-RULES.md
- [ ] Clarify JSONB schema management via EF Core migrations
- [ ] Document PostgreSQL advisory lock patterns for decision locking
- ✅ No new Aspire components needed

---

### EPIC 7: Collaboration & Multi-User Support

#### Status: ⚠️ REQUIRES UPDATE

**Stories:** 5 stories (7-1 through 7-5)

**Current State:**
- All reference PostgreSQL
- Real-time updates mentioned (via SignalR from Epic 3)
- Messaging for checkpoint buffers implied

**Aspire Integration Analysis:**

| Component | Mentioned | Aspire Pattern | Status |
|-----------|-----------|----------------|--------|
| PostgreSQL | ✅ Yes | Aspire.Hosting.PostgreSQL | ✅ From Epic 1 |
| SignalR | ✅ Implicit | From Epic 3 | ✅ Determined there |
| Conflict Resolution | ✅ Yes | In-process buffer | ✅ OK |

**Recommendations:**
- [ ] Add reference section to all stories linking to PROJECT-WIDE-RULES.md
- [ ] Clarify real-time updates use SignalR from Epic 3
- [ ] Document checkpoint buffering as in-process queue (no external messaging)
- [ ] Note: Large-scale deployments may need distributed messaging later

---

### EPIC 8: Persona Translation & Language Adaptation

#### Status: ⚠️ REQUIRES UPDATE

**Stories:** 5 stories (8-1 through 8-5)

**Current State:**
- All reference PostgreSQL for persona profiles
- No infrastructure mentioned
- Language/translation logic application-level

**Aspire Integration Analysis:**

| Component | Needed | Aspire | Status |
|-----------|--------|--------|--------|
| PostgreSQL | ✅ Yes | Aspire.Hosting.PostgreSQL | ✅ From Epic 1 |
| Translation API | ❓ Maybe | External service | ⚠️ See notes |

**Key Finding:**
- Persona configuration stored in PostgreSQL (Aspire-managed)
- Language translation logic is application-level (no external service in AC)
- If future: "Use Aspire.Hosting.Container for external translation API"

**Recommendations:**
- [ ] Add reference section to all stories linking to PROJECT-WIDE-RULES.md
- [ ] Clarify that persona profiles stored via Aspire-managed PostgreSQL
- [ ] If translation API added: "Use `aspire add Container` with translation service"

---

### EPIC 9: Data Persistence & State Management

#### Status: ⚠️ REQUIRES UPDATE

**Stories:** 6 stories (9-1 through 9-6)

**Current State:**
- All reference PostgreSQL
- Event log architecture mentioned
- JSONB state storage emphasized
- No explicit Aspire references

**Aspire Integration Analysis:**

| Component | Needed | Pattern | Status |
|-----------|--------|---------|--------|
| PostgreSQL | ✅ Yes | Aspire.Hosting.PostgreSQL | ✅ From Epic 1 |
| Event Log | ✅ Yes | PostgreSQL tables | ✅ EF Core migrations |
| JSONB State | ✅ Yes | PostgreSQL JSONB | ✅ Native |
| Audit Logging | ✅ Yes | PostgreSQL + Aspire logging | ✅ Built-in |

**Recommendations:**
- [ ] Add reference section to all stories linking to PROJECT-WIDE-RULES.md
- [ ] Clarify that event log persists via Aspire-managed PostgreSQL
- [ ] Document Aspire's structured logging integration for audit trails
- [ ] Reference PROJECT-WIDE-RULES.md for database migration pattern

---

### EPIC 10: Error Handling & Recovery

#### Status: ⚠️ REQUIRES UPDATE

**Stories:** 5 stories (10-1 through 10-5)

**Current State:**
- Error logging and metrics mentioned
- Graceful degradation discussed
- Monitoring via Grafana (but Story 1.5 cancelled Prometheus/Grafana for MVP)
- Redis caching mentioned in one story

**Aspire Integration Analysis:**

| Component | Mentioned | Current | Aspire | Status |
|-----------|-----------|---------|--------|--------|
| Error Logging | ✅ Yes | Implicit | Aspire structured logging | ✅ From Epic 1 |
| Metrics | ✅ Yes | Grafana (cancelled) | Aspire Dashboard | ✅ Superseded |
| Redis Cache | ✅ Yes (1 story) | Not specified | Check aspire.dev | ⚠️ Need component |
| Health Checks | ✅ Implicit | None | Aspire health checks | ✅ Built-in |

**Key Findings:**
- Error handling will use Aspire's built-in structured logging
- Metrics will be visible in Aspire Dashboard (not Prometheus/Grafana)
- Redis caching (if needed) should use Aspire component
- Health checks via Aspire

**Recommendations:**
- [ ] Update Story 10-X: Reference Aspire Dashboard for metrics visualization (not Grafana)
- [ ] Add reference section linking to PROJECT-WIDE-RULES.md
- [ ] If Redis caching needed: "Use `aspire add Redis.Distributed`"
- [ ] Clarify health checks use Aspire pattern from Story 1.1

---

### EPIC 11: Security & Access Control

#### Status: ⚠️ REQUIRES UPDATE

**Stories:** 5 stories (11-1 through 11-5)

**Current State:**
- All reference PostgreSQL for audit logging
- Input validation application-level
- Encryption at-rest mentioned

**Aspire Integration Analysis:**

| Component | Needed | Pattern | Status |
|-----------|--------|---------|--------|
| PostgreSQL | ✅ Yes | Aspire.Hosting.PostgreSQL | ✅ From Epic 1 |
| Audit Logging | ✅ Yes | PostgreSQL + Aspire logging | ✅ Built-in |
| Encryption | ✅ Yes | PostgreSQL native | ✅ OK |
| Secrets Management | ✅ Yes | Aspire secrets (dev) | ⚠️ See notes |

**Key Finding:**
- Secrets management for production should use Aspire environment variable pattern
- Development: Aspire handles secrets via launchSettings/environment

**Recommendations:**
- [ ] Add reference section to all stories linking to PROJECT-WIDE-RULES.md
- [ ] Document Aspire secrets management pattern for API keys
- [ ] Clarify PostgreSQL encryption via Aspire configuration
- [ ] Audit logging via Aspire-managed PostgreSQL

---

### EPIC 12: Admin Dashboard & Operations

#### Status: ⚠️ REQUIRES UPDATE

**Stories:** 6 stories (12-1 through 12-6)

**Current State:**
- All reference PostgreSQL
- System health monitoring mentioned
- Dashboard implementation not yet specified

**Aspire Integration Analysis:**

| Component | Needed | Pattern | Status |
|-----------|--------|---------|--------|
| PostgreSQL | ✅ Yes | Aspire.Hosting.PostgreSQL | ✅ From Epic 1 |
| System Health | ✅ Yes | Aspire health checks | ✅ Dashboard |
| Monitoring | ✅ Yes | Aspire Dashboard | ✅ Built-in (1.5 cancelled) |
| Provider Config | ✅ Yes | Secrets + DI | ✅ Aspire pattern |

**Recommendations:**
- [ ] Add reference section to all stories linking to PROJECT-WIDE-RULES.md
- [ ] Clarify admin dashboard integrates with Aspire Dashboard
- [ ] Health monitoring uses Aspire health check endpoints
- [ ] Provider configuration via Aspire environment/secrets

---

### EPIC 13: Integrations & Webhooks

#### Status: ⚠️ REQUIRES UPDATE

**Stories:** 5 stories (13-1 through 13-5)

**Current State:**
- All reference PostgreSQL for webhook storage
- Webhook retry logic mentioned
- External integrations referenced

**Aspire Integration Analysis:**

| Component | Needed | Pattern | Status |
|-----------|--------|---------|--------|
| PostgreSQL | ✅ Yes | Aspire.Hosting.PostgreSQL | ✅ From Epic 1 |
| External Services | ✅ Yes | HttpClient | ✅ Standard .NET |
| Webhook Queue | ⚠️ Maybe | Aspire messaging | Future epic |
| Notification APIs | ❓ Maybe | Aspire Container | Future |

**Recommendations:**
- [ ] Add reference section to all stories linking to PROJECT-WIDE-RULES.md
- [ ] Clarify webhook data persists via Aspire-managed PostgreSQL
- [ ] If webhook queue needed: "Use `aspire add RabbitMq.Aspire`"
- [ ] If notification APIs: "Use `aspire add Container` for external service"

---

## ASPIRE COMPONENT INVENTORY

### Components Used (Already Configured)

| Component | Aspire Pattern | Story | Status |
|-----------|----------------|-------|--------|
| PostgreSQL | `Aspire.Hosting.PostgreSQL` | 1.2 | ✅ Configured |
| App Host | `DistributedApplication` | 1.1 | ✅ Configured |
| Service Defaults | `Aspire.ServiceDefaults` | 1.1 | ✅ Configured |

### Components Recommended (Not Yet Integrated)

| Component | Aspire Pattern | Would be used in | Status | Priority |
|-----------|----------------|-----------------|--------|----------|
| SignalR | `ASP.NET Core built-in` | Epic 3 (MVP) | ✅ **VERIFIED** | MVP |
| SignalR (Azure) | `Aspire.Hosting.Azure.SignalR` | Epic 3 (Production) | ✅ **Available** | Post-MVP |
| Redis | `Aspire.Hosting.Redis` | Epic 10 (caching/backplane) | ✅ Available | Post-MVP |
| RabbitMQ | `Aspire.Hosting.RabbitMq` | Epics 5, 7, 13 (messaging) | ✅ Available | Post-MVP |
| Health Checks | `Aspire built-in` | All epics (inherited) | ✅ OK | - |
| Structured Logging | `OpenTelemetry` | All epics (inherited) | ✅ OK | - |
| Observability | `Aspire Dashboard` | All epics (inherited) | ✅ OK | - |

---

## CROSS-EPIC DEPENDENCY MAP

```
Epic 1 (Foundation)
├─ Aspire App Host ✅
├─ PostgreSQL ✅
├─ Health Checks ✅
└─ Structured Logging ✅
    │
    ├─→ Epic 2 (Auth) - Uses PostgreSQL ✅
    ├─→ Epic 3 (Chat) - Uses PostgreSQL + SignalR ⚠️
    ├─→ Epic 4 (Workflows) - Uses PostgreSQL ✅
    ├─→ Epic 5 (Multi-Agent) - Uses PostgreSQL ✅
    ├─→ Epic 6 (Decisions) - Uses PostgreSQL ✅
    ├─→ Epic 7 (Collaboration) - Uses PostgreSQL + SignalR ⚠️
    ├─→ Epic 8 (Personas) - Uses PostgreSQL ✅
    ├─→ Epic 9 (Persistence) - Uses PostgreSQL ✅
    ├─→ Epic 10 (Error Handling) - Uses PostgreSQL + Redis (future) ⚠️
    ├─→ Epic 11 (Security) - Uses PostgreSQL ✅
    ├─→ Epic 12 (Admin) - Uses PostgreSQL ✅
    └─→ Epic 13 (Webhooks) - Uses PostgreSQL ✅
```

---

## RECOMMENDATIONS BY PRIORITY

### 🔴 HIGH PRIORITY (Must do before any coding)

1. **~~Verify SignalR Aspire Component (Epic 3)~~** ✅ **COMPLETED 2026-01-24**
   - [x] Check https://aspire.dev for `Aspire.Hosting.SignalR` → **FOUND: `Aspire.Hosting.Azure.SignalR`**
   - [x] Document MVP approach: Use built-in ASP.NET Core SignalR (no Azure dependency)
   - [x] Document production approach: Use Azure SignalR with Aspire emulator for local dev
   - **Decision:** MVP uses self-hosted SignalR; Azure SignalR available for production scaling

2. **Add PROJECT-WIDE-RULES.md References (All Epics)** ⏳ IN PROGRESS
   - [ ] Add reference section to Stories 2.1-13.5
   - [ ] Link to aspire.dev for any Aspire components
   - [ ] Document Aspire-first decision pattern

3. **Clarify PostgreSQL Connection Pattern (All Epics)** ⏳ IN PROGRESS
   - [ ] Document that all PostgreSQL uses Aspire.Hosting.PostgreSQL
   - [ ] Show IConnectionStringProvider injection pattern
   - [ ] Reference Story 1.2 as template

### 🟡 MEDIUM PRIORITY (Before implementation of each epic)

4. **Redis Component Documentation (Epic 10)**
   - [ ] When Epic 10 caching is implemented: `aspire add Redis.Distributed`
   - [ ] Pre-plan Redis integration pattern

5. **Update Epic 1.4 & 1.6 Stories**
   - [ ] Story 1.4: Document CI/CD for Aspire projects
   - [ ] Story 1.6: Prioritize Aspire documentation

6. **Messaging Pattern Planning (Epics 5, 7, 13)**
   - [ ] Document future RabbitMQ/Kafka component pattern
   - [ ] When distributed architecture needed: `aspire add RabbitMq.Aspire`
   - [ ] Create separate epic for distributed messaging (if needed)

### 🟢 LOW PRIORITY (Future consideration)

7. **Production Deployment Patterns (All Epics)**
   - [ ] Document `aspire deploy` workflow
   - [ ] Plan cloud deployment targets (Azure, AWS, etc.)
   - [ ] Create deployment documentation

8. **Polyglot Support (Future)**
   - [ ] When adding Python/JavaScript components: Use Aspire `AddContainer`
   - [ ] Reference polyglot patterns on aspire.dev

---

## VALIDATION CHECKLIST

Each story implementation should verify:

- [ ] **Aspire-First**: Does story use `aspire add` for new components?
- [ ] **ProjectWideRules**: Does story reference PROJECT-WIDE-RULES.md?
- [ ] **Documentation**: Does story link to aspire.dev for patterns?
- [ ] **PostgreSQL**: If database used, does it use Aspire from Story 1.2?
- [ ] **Health Checks**: Are health checks implemented via Aspire pattern?
- [ ] **Logging**: Does error/monitoring use Aspire structured logging?
- [ ] **Secrets**: Are secrets managed via Aspire (dev) / environment (prod)?
- [ ] **No Docker Compose**: Are stories avoiding Docker Compose for local dev?
- [ ] **Service Discovery**: Are services discoverable via AppHost references?

---

## MIGRATION PATH: When to Add Components

**Phase 1 (MVP - Current)**
- ✅ PostgreSQL (Aspire)
- ✅ Structured Logging (Aspire)
- ✅ Health Checks (Aspire)
- ✅ Observability Dashboard (Aspire)

**Phase 2 (Post-MVP - When distributed agents needed)**
- ➕ RabbitMQ / Kafka (via `aspire add`)
- ➕ Distributed tracing (enhanced OpenTelemetry)

**Phase 3 (Production - When scaling)**
- ➕ Redis caching (via `aspire add Redis.Distributed`)
- ➕ Azure / AWS managed services (via Aspire cloud patterns)
- ➕ Kubernetes deployment (via Aspire manifests)

---

## CONCLUSION

### Overall Alignment: 🟡 YELLOW (Partial)

**Status Summary:**
- ✅ **Epic 1**: Fully aligned with Aspire-first pattern
- ⚠️ **Epics 2-13**: Infrastructure correctly identified; need explicit Aspire documentation and references

**Key Actions Required:**
1. ~~Verify SignalR Aspire component (Epic 3)~~ ✅ **DONE** - Use built-in ASP.NET Core SignalR for MVP
2. Add PROJECT-WIDE-RULES.md references to all stories ⏳ IN PROGRESS
3. Document Aspire connection string pattern for PostgreSQL (inherited) ⏳ IN PROGRESS
4. Plan Redis/RabbitMQ integration when needed ⏳ PENDING

**No Major Refactoring Needed:** All stories are designed with Aspire-compatible patterns. They inherit from Aspire foundation (Epic 1) and only need documentation updates to explicitly reference Aspire decisions.

**Next Step:** Begin updating stories 2.1-13.5 with PROJECT-WIDE-RULES.md references and explicit Aspire component documentation.

---

**Analysis completed by:** Analyst Agent  
**Reviewed:** Manual cross-reference against https://aspire.dev  
**Last Updated:** 2026-01-24
