# Epic 10-13 Implementation Readiness Stories

**Date:** January 27, 2026  
**Purpose:** Hardening stories for Epic 10-11 and implementation plan for Epic 12-13  
**Team:** Development Team via Party Mode

---

## 📋 EPIC 10: Error Handling & Recovery - HARDENING

### Current State
- ✅ ProblemDetails RFC 7807 implemented
- ✅ Session recovery (60-second window) working
- ⚠️ Missing: Retry policies for agent calls
- ⚠️ Missing: Circuit breaker pattern
- ⚠️ Missing: Graceful degradation under load

### Ready-for-Dev Stories

---

#### Story 10.1-H: Add Polly Retry Policies to Agent Calls

**Story ID:** E10-S1-HARDENING  
**Points:** 5  
**Priority:** HIGH  

As a developer, I want retry policies on agent calls, so that transient failures don't cause workflow failures.

**Acceptance Criteria:**

**Given** an agent call fails with a transient error (timeout, 503, network error)  
**When** the retry policy is triggered  
**Then** the call is retried up to 3 times with exponential backoff (1s, 2s, 4s)  
**And** each retry is logged with correlationId

**Given** all retries are exhausted  
**When** the final retry fails  
**Then** the error is logged as CRITICAL  
**And** the workflow transitions to Failed state  
**And** the user receives notification with actionable guidance

**Tasks:**
- [ ] Add Polly NuGet package to ApiService
- [ ] Create `AgentCallPolicy` with retry and timeout policies
- [ ] Wrap `AgentMessaging.RequestFromAgentAsync()` with policy
- [ ] Add policy metrics to OpenTelemetry
- [ ] Add unit tests for retry scenarios
- [ ] Add integration test for transient failure recovery

**Files to Modify:**
- `src/bmadServer.ApiService/bmadServer.ApiService.csproj` - Add Polly
- `src/bmadServer.ApiService/Services/Workflows/AgentMessaging.cs` - Apply policy
- `src/bmadServer.ApiService/Infrastructure/Policies/AgentCallPolicy.cs` - Create

---

#### Story 10.2-H: Implement Circuit Breaker for External Services

**Story ID:** E10-S2-HARDENING  
**Points:** 5  
**Priority:** HIGH  

As an operator, I want circuit breakers on external service calls, so that cascading failures are prevented.

**Acceptance Criteria:**

**Given** 5 consecutive failures occur to an external service  
**When** the circuit breaker trips  
**Then** subsequent calls fail fast for 30 seconds  
**And** the circuit state is logged and exposed via metrics

**Given** the circuit is open  
**When** the timeout expires  
**Then** the circuit moves to half-open state  
**And** a single test request is allowed  
**And** success closes the circuit, failure reopens it

**Tasks:**
- [ ] Configure Polly CircuitBreaker policy
- [ ] Apply to: LLM provider calls, external HTTP clients
- [ ] Expose circuit state via `/health` endpoint
- [ ] Add Grafana alert for circuit breaker trips
- [ ] Add tests for circuit breaker behavior

**Files to Modify:**
- `src/bmadServer.ApiService/Infrastructure/Policies/CircuitBreakerPolicy.cs` - Create
- `src/bmadServer.ApiService/Program.cs` - Register policies
- `src/bmadServer.ServiceDefaults/Extensions.cs` - Add circuit breaker health check

---

#### Story 10.3-H: Add Graceful Degradation Under Load

**Story ID:** E10-S3-HARDENING  
**Points:** 3  
**Priority:** MEDIUM  

As an operator, I want graceful degradation, so that core functionality remains available under heavy load.

**Acceptance Criteria:**

**Given** concurrent users reach 80% of capacity (20/25)  
**When** a new workflow start is requested  
**Then** it is queued with estimated wait time  
**And** user sees: "High demand - your request is queued"

**Given** the system is under load  
**When** non-essential features are identified  
**Then** typing indicators and presence updates are disabled first  
**And** core workflow execution continues

**Tasks:**
- [ ] Add request queuing for workflow starts at capacity
- [ ] Implement feature flags for non-essential features
- [ ] Add degradation middleware
- [ ] Add capacity metrics to Grafana dashboard
- [ ] Add load test to verify degradation behavior

**Files to Modify:**
- `src/bmadServer.ApiService/Middleware/CapacityMiddleware.cs` - Create
- `src/bmadServer.ApiService/Controllers/WorkflowsController.cs` - Add queueing
- `src/bmadServer.ApiService/Hubs/ChatHub.cs` - Add feature flag checks

---

## 📋 EPIC 11: Security & Access Control - HARDENING

### Current State
- ✅ JWT authentication implemented
- ✅ BCrypt password hashing working
- ✅ FluentValidation for inputs
- ⚠️ Missing: Per-user rate limiting
- ⚠️ Missing: Security headers middleware
- ⚠️ Missing: Encryption at rest

### Ready-for-Dev Stories

---

#### Story 11.1-H: Implement Per-User Rate Limiting

**Story ID:** E11-S1-HARDENING  
**Points:** 5  
**Priority:** HIGH  

As an operator, I want per-user rate limiting, so that no single user can overwhelm the system.

**Acceptance Criteria:**

**Given** rate limiting configuration exists  
**When** I check appsettings.json  
**Then** I see: `RateLimiting: { RequestsPerMinute: 60, BurstLimit: 10 }`

**Given** a user exceeds RequestsPerMinute  
**When** they make another request  
**Then** they receive 429 Too Many Requests  
**And** response includes Retry-After header with seconds to wait

**Given** User A is rate limited  
**When** User B makes requests  
**Then** User B is not affected (limits are per-user from JWT)

**Tasks:**
- [ ] Add AspNetCoreRateLimit NuGet package
- [ ] Configure per-user rate limiting in Program.cs
- [ ] Add rate limit headers to responses
- [ ] Add rate limit metrics to OpenTelemetry
- [ ] Add tests for rate limiting behavior
- [ ] Document rate limit configuration

**Files to Modify:**
- `src/bmadServer.ApiService/bmadServer.ApiService.csproj` - Add package
- `src/bmadServer.ApiService/Program.cs` - Configure middleware
- `appsettings.json` - Add RateLimiting section
- `src/bmadServer.ApiService/Middleware/RateLimitMiddleware.cs` - Create custom if needed

---

#### Story 11.2-H: Add Security Headers Middleware

**Story ID:** E11-S2-HARDENING  
**Points:** 3  
**Priority:** HIGH  

As an operator, I want proper security headers, so that the application is protected from common attacks.

**Acceptance Criteria:**

**Given** any HTTPS response is sent  
**When** I inspect the headers  
**Then** I see:
- `Strict-Transport-Security: max-age=31536000; includeSubDomains`
- `X-Content-Type-Options: nosniff`
- `X-Frame-Options: DENY`
- `X-XSS-Protection: 1; mode=block`
- `Content-Security-Policy: default-src 'self'; script-src 'self' 'unsafe-inline'`

**Given** HTTP request in production  
**When** request is received  
**Then** 301 redirect to HTTPS

**Tasks:**
- [ ] Add NWebsec or custom security headers middleware
- [ ] Configure CSP policy appropriate for SPA
- [ ] Enable HTTPS redirection in production
- [ ] Run security scanner to verify headers
- [ ] Add tests for header presence

**Files to Modify:**
- `src/bmadServer.ApiService/Program.cs` - Add middleware
- `src/bmadServer.ApiService/Middleware/SecurityHeadersMiddleware.cs` - Create

---

#### Story 11.3-H: Implement Encryption at Rest for Sensitive Data

**Story ID:** E11-S3-HARDENING  
**Points:** 5  
**Priority:** MEDIUM  

As an operator, I want sensitive data encrypted at rest, so that data breaches don't expose plaintext.

**Acceptance Criteria:**

**Given** sensitive data columns are identified (RefreshToken.TokenHash is already hashed)  
**When** additional sensitive data is stored  
**Then** application-level AES-256 encryption is applied

**Given** encryption keys exist  
**When** I check configuration  
**Then** keys are loaded from environment variables  
**And** key rotation is supported without downtime

**Tasks:**
- [ ] Identify all sensitive data columns
- [ ] Create DataProtection service for encryption/decryption
- [ ] Apply to: workflow state containing PII, artifact content
- [ ] Add key rotation capability
- [ ] Document encryption configuration
- [ ] Add tests for encryption/decryption

**Files to Modify:**
- `src/bmadServer.ApiService/Services/DataProtectionService.cs` - Create
- `src/bmadServer.ApiService/Data/ApplicationDbContext.cs` - Add value converters
- `appsettings.json` - Add encryption configuration

---

## 📋 EPIC 12: Admin Dashboard & Operations - IMPLEMENTATION PLAN

### Current State
- ✅ User management (RolesController) exists
- ✅ Health endpoints via Aspire
- ❌ No dedicated admin dashboard
- ❌ No metrics aggregation
- ❌ No provider configuration UI

### Implementation Plan

**Approach:** API-first development. Build admin API endpoints first, then UI.

**Phase 1: Admin API (2 weeks)**
1. Story 12.1: System Health API - GET `/api/v1/admin/health/detailed`
2. Story 12.2: Session Monitoring API - GET `/api/v1/admin/sessions`
3. Story 12.3: User Management API - CRUD at `/api/v1/admin/users`
4. Story 12.5: Audit Logs API - GET `/api/v1/admin/audit-logs`

**Phase 2: Admin UI (1-2 weeks)**
5. Story 12.1-UI: Health Dashboard component
6. Story 12.2-UI: Session Monitor component
7. Story 12.3-UI: User Management component

**Phase 3: Provider Configuration (1 week)**
8. Story 12.4: Provider Configuration API and UI

---

## 📋 EPIC 13: Integrations & Webhooks - IMPLEMENTATION PLAN

### Current State
- ✅ SignalR real-time notifications
- ❌ No webhook infrastructure
- ❌ No external event delivery
- ❌ No notification integrations

### Implementation Plan

**Approach:** Event-driven architecture. Leverage existing event logging.

**Phase 1: Webhook Infrastructure (1.5 weeks)**
1. Story 13.1: Webhook entity and registration API
2. Story 13.2: Webhook delivery service with retry
3. Story 13.3: Event ordering and idempotency

**Phase 2: Notification Integrations (1 week)**
4. Story 13.4: Slack/Teams webhook templates
5. Story 13.5: Webhook management UI

### Ready-for-Dev Stories

---

#### Story 13.1-R: Webhook Registration Infrastructure

**Story ID:** E13-S1-READY  
**Points:** 5  
**Priority:** HIGH (if integrations needed)

As an administrator, I want to register webhooks, so that external systems receive workflow events.

**Acceptance Criteria:**

**Given** I access webhook configuration  
**When** I POST to `/api/v1/admin/webhooks`  
**Then** I can create: name, URL, events[], secret, active

**Given** a webhook exists  
**When** a matching event occurs  
**Then** the webhook delivery service queues the event  
**And** delivery is attempted with retry

**Tasks:**
- [ ] Create Webhook entity in ApplicationDbContext
- [ ] Add WebhooksController with CRUD operations
- [ ] Create WebhookDeliveryService
- [ ] Add migration for Webhooks table
- [ ] Add tests for webhook registration

**Files to Create:**
- `src/bmadServer.ApiService/Data/Entities/Webhook.cs`
- `src/bmadServer.ApiService/Controllers/WebhooksController.cs`
- `src/bmadServer.ApiService/Services/WebhookDeliveryService.cs`

---

#### Story 13.2-R: Webhook Event Delivery with Retry

**Story ID:** E13-S2-READY  
**Points:** 8  
**Priority:** HIGH (if integrations needed)

As an operator, I want reliable webhook delivery, so that events reach external systems.

**Acceptance Criteria:**

**Given** a webhook event is triggered  
**When** delivery is attempted  
**Then** the payload includes: event type, timestamp, data, signature

**Given** delivery fails  
**When** retry policy is triggered  
**Then** retries occur at: 1min, 5min, 30min, 2hr, 24hr  
**And** after 24 hours, event is marked failed

**Given** I need to verify authenticity  
**When** I receive a webhook  
**Then** I can verify HMAC-SHA256 signature using the secret

**Tasks:**
- [ ] Create WebhookEvent entity for delivery tracking
- [ ] Implement signature generation (HMAC-SHA256)
- [ ] Add background job for webhook delivery
- [ ] Implement retry with exponential backoff
- [ ] Add delivery status tracking
- [ ] Add tests for delivery and retry

**Files to Modify:**
- `src/bmadServer.ApiService/Data/Entities/WebhookEvent.cs` - Create
- `src/bmadServer.ApiService/Services/WebhookDeliveryService.cs` - Add delivery logic
- `src/bmadServer.ApiService/Jobs/WebhookDeliveryJob.cs` - Create background job

---

## 📊 SUMMARY

### Epic 10-11 Hardening (Ready for Dev)

| Story | Points | Priority | Status |
|-------|--------|----------|--------|
| 10.1-H: Retry Policies | 5 | HIGH | Ready |
| 10.2-H: Circuit Breaker | 5 | HIGH | Ready |
| 10.3-H: Graceful Degradation | 3 | MEDIUM | Ready |
| 11.1-H: Rate Limiting | 5 | HIGH | Ready |
| 11.2-H: Security Headers | 3 | HIGH | Ready |
| 11.3-H: Encryption at Rest | 5 | MEDIUM | Ready |
| **TOTAL** | **26** | | |

### Epic 12-13 Planning

| Epic | Stories | Points | Timeline |
|------|---------|--------|----------|
| Epic 12: Admin Dashboard | 6 | 34 | 3-4 weeks |
| Epic 13: Webhooks | 5 | 26 | 2-3 weeks |
| **TOTAL** | **11** | **60** | **5-7 weeks** |

---

**Document Created:** January 27, 2026  
**Next Action:** Start Epic 10-11 hardening stories
