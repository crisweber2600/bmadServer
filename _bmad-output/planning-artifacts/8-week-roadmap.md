# 8-Week Implementation Roadmap - bmadServer MVP

**Timeline:** Weeks 1-8 (Feb 3 - Mar 30, 2026)  
**Goal:** Ship first complete BMAD workflow via web interface  
**Team:** Cris (Architect/Lead Dev), Sarah (Product), Marcus (Backend Dev)  
**Status:** READY FOR EXECUTION

---

## Executive Summary

### Phase Structure
- **Phase 1 (Weeks 1-2):** Foundation - Backend skeleton + DB schema
- **Phase 2 (Weeks 3-4):** Core Features - Workflow engine + state management
- **Phase 3 (Weeks 5-6):** Real-Time - WebSocket + collaboration features
- **Phase 4 (Weeks 7-8):** Polish - Testing, hardening, deployment

### Success Criteria (MVP Definition)
✅ Complete one BMAD workflow (PRD) end-to-end via web UI  
✅ 2+ users can collaborate without conflicts  
✅ Workflow state persists across browser refresh  
✅ 95% workflow completion rate under normal load  
✅ All P0 security requirements met  
✅ API documented (Swagger)  
✅ Deployable to self-hosted Linux  

### Risk Mitigation Upfront
- **Mitigation 1:** WebSocket connectivity → SignalR + auto-reconnect
- **Mitigation 2:** State conflicts → optimistic concurrency with version fields
- **Mitigation 3:** Agent coordination → in-process router + deadlock detection
- **Mitigation 4:** Performance → connection pooling + JSONB GIN indexes

---

## Week-by-Week Sprint Breakdown

### **WEEK 1: Foundation (Feb 3-7) - "Get Running"**

**Sprint Goal:** Infrastructure working, first workflow state in database

#### Monday-Tuesday: Backend Skeleton
- [ ] **M1.1** Initialize Aspire starter template (`aspire new aspire-starter`)
- [ ] **M1.2** Configure PostgreSQL connection (connection pooling + SSL)
- [ ] **M1.3** Create DbContext with User, Session, Workflow entities
- [ ] **M1.4** First EF Core migration (`dotnet ef migrations add InitialCreate`)
- [ ] **M1.5** Add OpenAPI/Swagger documentation
- [ ] **M1.6** Create health check endpoint `/health`

**Owner:** Marcus (Backend)  
**Delivery:** Backend compiles, migrations run, Swagger accessible

```bash
# By end of day Tuesday
dotnet build  # ✅ No errors
dotnet ef database update  # ✅ Tables created
curl https://localhost:5001/swagger  # ✅ Swagger UI loads
```

#### Wednesday-Thursday: Authentication MVP
- [ ] **M2.1** Implement JWT token generation (15-min expiry)
- [ ] **M2.2** Create `POST /api/v1/auth/login` endpoint
- [ ] **M2.3** Create `POST /api/v1/auth/refresh` endpoint (refresh token handling)
- [ ] **M2.4** Add JWT bearer middleware to Program.cs
- [ ] **M2.5** Create test users in database (Cris, Sarah, Marcus)
- [ ] **M2.6** Implement session validation on WebSocket connect

**Owner:** Marcus  
**Delivery:** Can log in and get JWT token

```bash
# Test login
curl -X POST https://localhost:5001/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"cris@bmad.io","password":"password"}'
# Returns: { "accessToken": "eyJ...", "expiresIn": 900 }
```

#### Friday: React Frontend Shell
- [ ] **M3.1** Initialize React 18 + Vite project
- [ ] **M3.2** Setup Zustand + TanStack Query
- [ ] **M3.3** Create login form component
- [ ] **M3.4** Create API client with interceptor for JWT tokens
- [ ] **M3.5** Layout structure (Header, Sidebar, Main content area)
- [ ] **M3.6** React Router with lazy loading setup

**Owner:** Cris  
**Delivery:** Frontend runs locally, can log in

```bash
cd client
npm run dev  # ✅ Runs on localhost:5173
# Can navigate to /login and authenticate
```

#### End of Week 1 Checkpoint
- ✅ Backend: HTTP API responses working
- ✅ Frontend: UI framework + routing functional
- ✅ Database: Schema created, test data seeded
- ✅ No build errors or warnings
- **Sprint Velocity:** 2 features complete (Auth + Base Infrastructure)

---

### **WEEK 2: Workflow State (Feb 10-14) - "First Workflow State"**

**Sprint Goal:** Create workflow with JSONB state persistence

#### Monday-Tuesday: Workflow API Endpoints
- [ ] **M4.1** Create `POST /api/v1/workflows` endpoint (create workflow)
- [ ] **M4.2** Create `GET /api/v1/workflows` endpoint (list workflows, paginated)
- [ ] **M4.3** Create `GET /api/v1/workflows/{id}` endpoint (get details)
- [ ] **M4.4** Implement JSONB state serialization/deserialization
- [ ] **M4.5** Add FluentValidation for CreateWorkflowRequest
- [ ] **M4.6** Create workflow state DTO with all required fields

**Owner:** Marcus  
**Delivery:** Can create workflow via API

```bash
curl -X POST https://localhost:5001/api/v1/workflows \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"workflowType":"prd","context":{"productName":"bmadServer"}}'
# Returns: 201 Created with workflow ID
```

#### Wednesday: Workflow Frontend Components
- [ ] **M5.1** Create WorkflowList component with React Query
- [ ] **M5.2** Create WorkflowDetail component
- [ ] **M5.3** Create CreateWorkflowForm with validation
- [ ] **M5.4** Implement workflow navigation (list → detail)
- [ ] **M5.5** Add loading/error states
- [ ] **M5.6** Display workflow state as JSON (temporary, for debugging)

**Owner:** Cris  
**Delivery:** Can create and view workflows in UI

#### Thursday: Concurrency Control + Error Handling
- [ ] **M6.1** Add `_version`, `_lastModifiedBy`, `_lastModifiedAt` to JSONB state
- [ ] **M6.2** Implement optimistic concurrency check in update handler
- [ ] **M6.3** Throw WorkflowConflictException with version mismatch details
- [ ] **M6.4** Create exception handler middleware
- [ ] **M6.5** Return ProblemDetails (RFC 7807) format
- [ ] **M6.6** Add conflict error handling in frontend

**Owner:** Marcus + Cris  
**Delivery:** Concurrent updates detected and handled gracefully

```csharp
// Version conflict scenario
if (workflow.Version != expectedVersion)
    throw new WorkflowConflictException(
        expectedVersion: expectedVersion,
        actualVersion: workflow.Version,
        lastModifiedBy: workflow.LastModifiedBy);
// Returns: 409 Conflict with version details
```

#### Friday: Testing + Verification
- [ ] **M7.1** Write integration tests for workflow CRUD
- [ ] **M7.2** Test concurrency with simultaneous updates
- [ ] **M7.3** Verify JSONB schema validation
- [ ] **M7.4** Test API error responses
- [ ] **M7.5** Load test: 100 concurrent workflow creations
- [ ] **M7.6** Document API via Swagger

**Owner:** Cris  
**Delivery:** 80%+ test coverage on API layer

```bash
dotnet test --filter "Workflow"  # All workflow tests pass
npm test -- WorkflowList  # React component tests pass
```

#### End of Week 2 Checkpoint
- ✅ Can create, read, update workflows via API
- ✅ JSONB state persisting correctly
- ✅ Concurrent updates detected (version conflicts)
- ✅ Error responses follow ProblemDetails format
- ✅ Basic tests passing (70%+ coverage)
- **Sprint Velocity:** 5 features complete

---

### **WEEK 3: Workflow Orchestration (Feb 17-21) - "Steps & Decisions"**

**Sprint Goal:** Workflows have steps; decisions can be proposed

#### Monday: Workflow Steps
- [ ] **M8.1** Add step tracking to workflow state (currentStep, totalSteps)
- [ ] **M8.2** Create `POST /api/v1/workflows/{id}/next-step` endpoint
- [ ] **M8.3** Implement step validation (can only advance valid states)
- [ ] **M8.4** Track step completion in event log
- [ ] **M8.5** Frontend: Display workflow progress (1 of 12 steps)
- [ ] **M8.6** Add "Next Step" button (updates state optimistically)

**Owner:** Marcus + Cris  
**Delivery:** Can advance through workflow steps

#### Tuesday-Wednesday: Decision Management
- [ ] **M9.1** Add decisions array to workflow state
- [ ] **M9.2** Create `POST /api/v1/workflows/{id}/decisions` (propose decision)
- [ ] **M9.3** Create `POST /api/v1/decisions/{id}/approve` endpoint
- [ ] **M9.4** Create `POST /api/v1/decisions/{id}/reject` endpoint
- [ ] **M9.5** Track decision metadata (proposedBy, approvedBy, confidence)
- [ ] **M9.6** Implement decision locking (approved decisions can't be changed)

**Owner:** Marcus  
**Delivery:** Can propose, approve, reject decisions

```csharp
// Decision structure
{
  "id": "dec-001",
  "proposal": "Use JWT for authentication",
  "proposedBy": "marcus",
  "status": "locked",
  "confidence": 0.95,
  "approvedBy": "cris",
  "approvedAt": "2026-02-19T10:00:00Z"
}
```

#### Thursday: Decision UI Components
- [ ] **M10.1** Create DecisionList component
- [ ] **M10.2** Create DecisionDetail component (modal)
- [ ] **M10.3** Create DecisionForm (propose/approve/reject)
- [ ] **M10.4** Add decision voting/approval UI
- [ ] **M10.5** Show confidence scores visually
- [ ] **M10.6** Real-time decision updates (via polling for now)

**Owner:** Cris  
**Delivery:** Can propose and vote on decisions in UI

#### Friday: Event Audit Trail
- [ ] **M11.1** Implement audit log table (WorkflowEvent)
- [ ] **M11.2** Log all state mutations (step change, decision proposal/approval)
- [ ] **M11.3** Create `GET /api/v1/workflows/{id}/events` endpoint
- [ ] **M11.4** Display audit trail in UI
- [ ] **M11.5** Test atomicity (state + event log consistency)
- [ ] **M11.6** Document event schema

**Owner:** Marcus  
**Delivery:** Full audit trail visible to users

#### End of Week 3 Checkpoint
- ✅ Workflows have steps + progression
- ✅ Decisions can be proposed, approved, rejected
- ✅ Full audit trail of all changes
- ✅ Concurrency control working for decisions too
- ✅ Can complete first workflow ~50%
- **Sprint Velocity:** 4 features complete

---

### **WEEK 4: Real-Time Communication (Feb 24-28) - "WebSocket Live"**

**Sprint Goal:** SignalR WebSocket streaming (no more polling)

#### Monday-Tuesday: SignalR Hub Setup
- [ ] **M12.1** Create WorkflowHub (SignalR hub)
- [ ] **M12.2** Implement `SubscribeToWorkflow` method
- [ ] **M12.3** Implement `ApproveDecision` method (broadcasts to group)
- [ ] **M12.4** Add JWT authentication for WebSocket
- [ ] **M12.5** Implement graceful disconnection handling
- [ ] **M12.6** Add connection heartbeat (60-second interval)

**Owner:** Marcus  
**Delivery:** WebSocket hub operational

```csharp
public class WorkflowHub : Hub
{
    public async Task SubscribeToWorkflow(string workflowId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"workflow-{workflowId}");
    }

    public async Task ApproveDecision(string decisionId, string rationale)
    {
        var result = await _workflowService.ApproveAsync(...);
        await Clients.Group($"workflow-{result.WorkflowId}")
            .SendAsync("decision-approved", result);
    }
}
```

#### Wednesday: Frontend SignalR Client
- [ ] **M13.1** Setup SignalR client connection
- [ ] **M13.2** Implement auto-reconnect with exponential backoff
- [ ] **M13.3** Subscribe to workflow on page load
- [ ] **M13.4** Listen for decision-approved event
- [ ] **M13.5** Update UI in real-time when decisions change
- [ ] **M13.6** Show connection status indicator

**Owner:** Cris  
**Delivery:** Real-time updates in UI

```typescript
const connection = new HubConnectionBuilder()
    .withUrl("/workflowhub", { accessTokenFactory: () => token })
    .withAutomaticReconnect([0, 2000, 10000])
    .build();

await connection.start();
await connection.invoke("SubscribeToWorkflow", workflowId);

connection.on("decision-approved", (decision) => {
    setDecisions(prev => prev.map(d => d.id === decision.id ? decision : d));
});
```

#### Thursday: Multi-User Collaboration
- [ ] **M14.1** Display participant list (who's connected)
- [ ] **M14.2** Show last active time per user
- [ ] **M14.3** Broadcast user activity (typing, approving, etc.)
- [ ] **M14.4** Handle concurrent decision proposals
- [ ] **M14.5** Conflict detection + user notification
- [ ] **M14.6** Test 2-user simultaneous workflow

**Owner:** Marcus + Cris  
**Delivery:** 2+ users can collaborate in real-time

#### Friday: Error Handling + Resilience
- [ ] **M15.1** Handle WebSocket disconnection gracefully
- [ ] **M15.2** Queue offline messages, send on reconnect
- [ ] **M15.3** Retry failed API calls with exponential backoff
- [ ] **M15.4** Show user-friendly error messages
- [ ] **M15.5** Test network interruption scenarios
- [ ] **M15.6** Document error recovery patterns

**Owner:** Cris  
**Delivery:** Resilient to network issues

#### End of Week 4 Checkpoint
- ✅ WebSocket real-time updates working
- ✅ 2+ users can collaborate simultaneously
- ✅ Conflicts detected and handled
- ✅ Disconnection + reconnection working
- ✅ ~70% of first workflow completable
- **Sprint Velocity:** 5 features complete

---

### **WEEK 5: Agent Integration (Mar 3-7) - "Ask BMAD Agents"**

**Sprint Goal:** Route requests to BMAD agents + stream responses

#### Monday-Tuesday: Agent Router
- [ ] **M16.1** Implement IAgentRouter interface
- [ ] **M16.2** Create InProcessAgentRouter (MVP implementation)
- [ ] **M16.3** Add deadlock detection (call stack tracking)
- [ ] **M16.4** Implement HMAC message signing
- [ ] **M16.5** Add 30-second timeout per agent call
- [ ] **M16.6** Logging + reasoning trace capture

**Owner:** Marcus  
**Delivery:** Agent routing operational

```csharp
public class InProcessAgentRouter : IAgentRouter
{
    public async Task<AgentResponse> RouteAsync(string agentId, AgentRequest request, TimeSpan? timeout = null)
    {
        // Deadlock detection
        if (_deadlockDetector.WouldCreateCycle(agentId))
            throw new DeadlockDetectedException();

        // Message signing
        request.Signature = SignMessage(request);

        // Execution with timeout
        var response = await agent.ExecuteAsync(request, timeout ?? TimeSpan.FromSeconds(30));

        // Logging
        await _traceLogger.LogResponseAsync(agentId, response);

        return response;
    }
}
```

#### Wednesday: SignalR Streaming Agent Response
- [ ] **M17.1** Create `RequestAgentResponse` SignalR method
- [ ] **M17.2** Stream response chunks (for long agent outputs)
- [ ] **M17.3** Send reasoning trace with response
- [ ] **M17.4** Confidence scoring
- [ ] **M17.5** Contradiction detection (vs prior decisions)
- [ ] **M17.6** Frontend: Display streamed agent response

**Owner:** Marcus + Cris  
**Delivery:** Can request agent response + see it stream

#### Thursday: Workflow Integration
- [ ] **M18.1** Call appropriate agent for each workflow step
- [ ] **M18.2** Pass workflow context to agent (prior decisions, etc.)
- [ ] **M18.3** Show agent recommendations in UI
- [ ] **M18.4** Allow user to accept/reject recommendations
- [ ] **M18.5** Log agent interactions in audit trail
- [ ] **M18.6** Handle agent errors gracefully

**Owner:** Marcus  
**Delivery:** Full agent-assisted workflow

#### Friday: Testing + Hardening
- [ ] **M19.1** Test agent router with mocked agents
- [ ] **M19.2** Test deadlock detection scenarios
- [ ] **M19.3** Test timeout scenarios (agent doesn't respond in 30s)
- [ ] **M19.4** Test with real BMAD agents (if available)
- [ ] **M19.5** Load test: 10 concurrent agent calls
- [ ] **M19.6** Document agent routing architecture

**Owner:** Cris  
**Delivery:** Robust agent integration

#### End of Week 5 Checkpoint
- ✅ Agents can be called from workflows
- ✅ Agent responses stream to UI
- ✅ Deadlock + timeout protection working
- ✅ Full workflow ~85% completable
- ✅ Multi-user + agent collaboration working
- **Sprint Velocity:** 4 features complete

---

### **WEEK 6: Polish & Hardening (Mar 10-14) - "Production Ready"**

**Sprint Goal:** Error handling, performance, security hardening

#### Monday: Performance Optimization
- [ ] **M20.1** Add database connection pooling tuning
- [ ] **M20.2** Create GIN indexes for JSONB queries
- [ ] **M20.3** Implement caching with IMemoryCache
- [ ] **M20.4** Lazy load React components
- [ ] **M20.5** Minify frontend bundle
- [ ] **M20.6** Performance test: 500 req/sec sustained

**Owner:** Marcus + Cris  
**Delivery:** Sub-second response times

#### Tuesday: Security Hardening
- [ ] **M21.1** Implement rate limiting (60 req/min)
- [ ] **M21.2** Add security headers (HSTS, CSP, X-Frame-Options)
- [ ] **M21.3** Validate all JSONB state schemas
- [ ] **M21.4** Penetration test (basic)
- [ ] **M21.5** Verify no sensitive data in logs
- [ ] **M21.6** Security review checklist

**Owner:** Cris  
**Delivery:** Passes security audit

#### Wednesday: Error Handling + Edge Cases
- [ ] **M22.1** Graceful handling of concurrent workflow modifications
- [ ] **M22.2** Session expiration + token refresh scenarios
- [ ] **M22.3** Database connection pool exhaustion
- [ ] **M22.4** Large workflow state (10,000+ decisions)
- [ ] **M22.5** Network latency + timeout scenarios
- [ ] **M22.6** User guidance in error messages

**Owner:** Marcus + Cris  
**Delivery:** Robust error handling

#### Thursday: Documentation + Deployment
- [ ] **M23.1** Complete API documentation (Swagger)
- [ ] **M23.2** Create Docker image + docker-compose.yml
- [ ] **M23.3** Document environment variables + configuration
- [ ] **M23.4** Create deployment runbook
- [ ] **M23.5** Document rollback procedures
- [ ] **M23.6** Create monitoring dashboard (basic)

**Owner:** Cris  
**Delivery:** Ready to deploy

#### Friday: Full Workflow Test
- [ ] **M24.1** Complete full PRD workflow (1-12 steps)
- [ ] **M24.2** 2-user collaboration test
- [ ] **M24.3** Concurrent decision approval
- [ ] **M24.4** Performance under load
- [ ] **M24.5** Error scenario testing
- [ ] **M24.6** UAT with team

**Owner:** All  
**Delivery:** MVP feature complete + tested

#### End of Week 6 Checkpoint
- ✅ Full workflow completable end-to-end
- ✅ 2+ users collaborating without conflicts
- ✅ Performance meets baselines
- ✅ Security requirements met
- ✅ Deployable to self-hosted Linux
- ✅ Ready for UAT
- **Sprint Velocity:** 3 features complete

---

### **WEEK 7-8: Testing & Deployment (Mar 17-30) - "Launch"**

#### WEEK 7: Testing & Validation (Mar 17-21)

**Monday-Tuesday: Comprehensive Testing**
- [ ] **M25.1** Unit tests (80%+ coverage, backend + frontend)
- [ ] **M25.2** Integration tests (API endpoints, DB, SignalR)
- [ ] **M25.3** E2E tests (full workflow scenarios)
- [ ] **M25.4** Load test (500 req/sec, 100 WebSocket connections)
- [ ] **M25.5** Chaos testing (network failures, DB timeouts)
- [ ] **M25.6** UAT with Cris, Sarah, Marcus

**Owner:** All  
**Delivery:** 95%+ test pass rate

**Wednesday-Thursday: Monitoring Setup**
- [ ] **M26.1** Configure OpenTelemetry + Aspire Dashboard
- [ ] **M26.2** Setup Prometheus + Grafana dashboards
- [ ] **M26.3** Configure alerting (CPU, memory, errors, latency)
- [ ] **M26.4** Create incident response runbook
- [ ] **M26.5** Health check + readiness probe endpoints
- [ ] **M26.6** Logging aggregation setup (optional)

**Owner:** Cris  
**Delivery:** Monitoring + alerting operational

**Friday: Pre-Launch Checklist**
- [ ] **M27.1** All tests passing
- [ ] **M27.2** No compiler warnings or linting errors
- [ ] **M27.3** Documentation complete
- [ ] **M27.4** Security checklist passed
- [ ] **M27.5** Performance baseline met
- [ ] **M27.6** Team signoff ready

**Owner:** All  
**Delivery:** Ready to launch

#### WEEK 8: Deployment & Launch (Mar 24-30)

**Monday-Tuesday: Production Deployment**
- [ ] **M28.1** Provision Linux server (Ubuntu 22.04 LTS)
- [ ] **M28.2** Configure PostgreSQL production (backups, monitoring)
- [ ] **M28.3** Deploy Docker Compose stack
- [ ] **M28.4** Configure TLS certificates (Let's Encrypt)
- [ ] **M28.5** Setup nginx reverse proxy
- [ ] **M28.6** Smoke tests on production

**Owner:** Cris  
**Delivery:** Live on production

**Wednesday-Thursday: Launch & Monitoring**
- [ ] **M29.1** Internal team testing (Cris, Sarah, Marcus)
- [ ] **M29.2** Monitor system health (dashboards, alerts)
- [ ] **M29.3** Document any bugs found
- [ ] **M29.4** Performance baseline validation
- [ ] **M29.5** Security validation (pen test results)
- [ ] **M29.6** Create post-launch support runbook

**Owner:** All  
**Delivery:** System stable, 0 critical issues

**Friday: Official Launch**
- [ ] **M30.1** Team announcement (internal launch)
- [ ] **M30.2** Create launch blog post / documentation
- [ ] **M30.3** Gather initial feedback
- [ ] **M30.4** Document lessons learned
- [ ] **M30.5** Plan Phase 2 priorities
- [ ] **M30.6** Sprint retrospective

**Owner:** Cris (with team input)  
**Delivery:** MVP launched successfully

#### End of Week 8: LAUNCH COMPLETE ✅
- ✅ Complete workflow (PRD) working end-to-end
- ✅ 2+ users collaborating in real-time
- ✅ 95% workflow completion rate
- ✅ 99.5% uptime / <1s p95 latency
- ✅ All security requirements met
- ✅ Full monitoring + alerting operational
- ✅ Team trained and confident

---

## Critical Path Analysis

### Blockers & Dependencies

```
Week 1: Backend Foundation (BLOCKING everything)
  ├─ W2: Workflow State (needs W1)
  ├─ W2: Frontend Shell (needs W1)
  └─ W3+: All features (need W1)

W2: Workflow CRUD + Auth (BLOCKING real-time)
  └─ W3: Steps & Decisions (needs W2)
      └─ W4: Real-Time (needs W3)
          └─ W5: Agents (needs W4)
              └─ W6: Hardening (needs W5)

Parallel Tracks:
  - Backend (Marcus): W1→W2→W3→W4→W5→W6
  - Frontend (Cris):  W1→W2→W3→W4→W5→W6
  - Ops/Deployment (Cris starts W6): Monitoring, Docker, deployment
```

### Risk Mitigation Timeline

| Risk | Mitigation | When |
|------|-----------|------|
| **WebSocket failures** | SignalR with auto-reconnect | W1 (design), W4 (implement) |
| **Concurrency conflicts** | Version field + optimistic locking | W1 (design), W2 (implement) |
| **Agent deadlocks** | Call stack tracking + timeout | W1 (design), W5 (implement) |
| **Performance degradation** | Connection pooling + GIN indexes | W2 (implement), W6 (optimize) |
| **Deployment issues** | Docker + deployment runbook | W6 (prepare), W8 (execute) |

---

## Team Responsibilities

### Cris (Architect/Tech Lead)
- W1-8: Overall architecture decisions
- W1-2: React frontend setup
- W3-4: Frontend features
- W5-6: Integration testing + performance
- W6-8: Deployment + monitoring

### Marcus (Backend Developer)
- W1-2: Backend skeleton + authentication
- W2-3: Workflow API + concurrency
- W3-4: SignalR hub + collaboration
- W5-6: Agent router + optimization
- W7-8: Testing + monitoring

### Sarah (Product Manager)
- W1: Requirements refinement
- W2-3: Feature feedback
- W6-7: UAT + sign-off
- W8: Launch communication

---

## Resource Requirements

| Resource | Week 1-2 | Week 3-4 | Week 5-6 | Week 7-8 |
|----------|----------|----------|----------|----------|
| **Engineer-Hours** | 80 | 80 | 80 | 60 |
| **Code Review** | 10 | 15 | 20 | 10 |
| **Testing** | 5 | 10 | 30 | 20 |
| **Deployment** | 0 | 0 | 10 | 30 |

**Total: ~520 engineer-hours over 8 weeks**

---

## Success Metrics (Launch Day)

✅ **Functionality:**
- [ ] 100% of one complete workflow (PRD) working
- [ ] 2+ users can collaborate simultaneously
- [ ] All CRUD operations functional
- [ ] Decision approval workflow complete

✅ **Reliability:**
- [ ] 99.5% uptime during test period
- [ ] 95% workflow completion rate
- [ ] < 5 minutes recovery time on failure
- [ ] Zero data loss incidents

✅ **Performance:**
- [ ] p95 latency < 1 second
- [ ] p99 latency < 5 seconds
- [ ] 500 req/sec sustained
- [ ] 100 concurrent WebSocket connections

✅ **Security:**
- [ ] All authentication working
- [ ] No SQL injection vulnerabilities
- [ ] No XSS vulnerabilities
- [ ] All data encrypted in transit (HTTPS)

✅ **Quality:**
- [ ] 80%+ test coverage (backend)
- [ ] 70%+ test coverage (frontend)
- [ ] Zero compiler warnings
- [ ] Zero linting errors
- [ ] Full API documentation

---

## Phase 2 Planning (Post-MVP)

**Timeline:** Weeks 9-16 (Apr-May 2026)

### Phase 2 Features (Out of Scope for MVP)
1. **Performance Optimization:** Redis caching, query optimization
2. **Workflow Visualization:** Gantt charts, dependency graphs
3. **Integrations:** GitHub webhooks, Slack notifications, Jira
4. **Event Stream:** External tools can subscribe to workflow changes
5. **Audit Trail UI:** Historical view of all decisions + changes
6. **Multi-Tenancy:** Support multiple teams/organizations
7. **Analytics:** Usage metrics, decision success rates
8. **Mobile:** React Native or mobile-responsive improvements

### Phase 2 Resource Plan
- Same 3-person team
- 8 weeks sprint + 1 week planning = 9 weeks total
- Focus on high-impact features (TBD with Sarah)

---

**This roadmap is LOCKED. Changes require architecture team approval.**

Generated: 2026-01-23  
Last Updated: 2026-01-23  
Status: READY FOR EXECUTION
