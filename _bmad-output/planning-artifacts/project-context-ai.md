# Project Context for AI Agents - bmadServer

**Purpose:** Guide AI agents during code generation, testing, and review  
**Generated:** 2026-01-23  
**Status:** LOCKED - DO NOT MODIFY  
**Audience:** AI code generators, test agents, review agents

---

## CRITICAL ARCHITECTURE RULES (MUST-FOLLOW)

### Rule 1: Technology Stack is LOCKED

**PROHIBITED:**
- ❌ Suggesting Redux instead of Zustand
- ❌ Using MongoDB instead of PostgreSQL
- ❌ Replacing SignalR with Socket.IO
- ❌ Using alternative JWT libraries
- ❌ Suggesting Express.js instead of ASP.NET Core

**ENFORCED:**
- ✅ .NET 10 + ASP.NET Core 10
- ✅ PostgreSQL 17.x with JSONB
- ✅ Entity Framework Core 9.0
- ✅ SignalR 8.0+ for WebSocket
- ✅ React 18 + TypeScript strict mode
- ✅ Zustand 4.5 + TanStack Query 5.x
- ✅ Aspire 13.1.0+ for orchestration

### Rule 2: Concurrency Control is MANDATORY

**Every workflow state mutation requires:**
- `_version` field check (optimistic locking)
- `expectedVersion` in request payload
- Throw `WorkflowConflictException` if version mismatch
- Return HTTP 409 Conflict with current version details

**Example:**
```csharp
// ❌ WRONG
workflow.State = newState;
await _context.SaveChangesAsync();

// ✅ CORRECT
if (workflow.Version != expectedVersion)
    throw new WorkflowConflictException(...);
workflow.Version++;
await _context.SaveChangesAsync();
```

### Rule 3: All State Mutations are ASYNC

**PROHIBITED:**
- ❌ Synchronous database operations
- ❌ Blocking I/O in request handlers
- ❌ No `async/await` keywords

**ENFORCED:**
```csharp
// ✅ All methods must be async
public async Task UpdateWorkflowAsync(...)
public async Task ApproveDecisionAsync(...)
await _context.SaveChangesAsync();
```

### Rule 4: Error Handling Uses ProblemDetails (RFC 7807)

**Every API response must include:**
- HTTP status code (400, 401, 404, 409, 500, etc.)
- ProblemDetails with: type, title, status, detail, instance
- Custom fields for domain errors (e.g., expectedVersion, actualVersion)

**Example:**
```json
{
  "type": "https://bmadserver.api/errors/workflow-conflict",
  "title": "Workflow State Conflict",
  "status": 409,
  "detail": "Modified by another user",
  "instance": "/api/v1/workflows/wf-123",
  "expectedVersion": 5,
  "actualVersion": 6
}
```

### Rule 5: JSONB State Validation is MANDATORY

**Before persisting JSONB state:**
1. Validate structure (schema version check)
2. Validate business rules (FluentValidation)
3. Validate JSONB schema (custom validators)
4. Only then: call `_context.SaveChangesAsync()`

```csharp
// Validation sequence
var state = JsonDocument.Parse(userInput);
await _validator.ValidateAndThrowAsync(state);  // FluentValidation
ValidateJsonbSchema(state);                      // Custom validation
workflow.State = state;
await _context.SaveChangesAsync();
```

### Rule 6: Rate Limiting is PER-USER

- **API:** 60 requests/minute per user
- **WebSocket:** 5 concurrent connections per user
- **Agent calls:** 10 concurrent per session
- Enforced via `[RequireRateLimiting("api-default")]` attribute

### Rule 7: Authentication is MANDATORY on ALL APIs

**PROHIBITED:**
- ❌ Unauthenticated public endpoints
- ❌ Endpoints without `[Authorize]` attribute
- ❌ Skipping token validation

**ENFORCED:**
```csharp
app.MapPost("/api/v1/workflows", CreateWorkflow)
    .RequireAuthorization()  // Every endpoint
    .WithOpenApi();
```

### Rule 8: Database Transactions for Dual Writes

**When writing to both workflow_state (JSONB) and event_log (audit):**
```csharp
using var transaction = await _context.Database.BeginTransactionAsync();
try
{
    workflow.State = newState;
    _context.AuditLogs.Add(auditEntry);
    await _context.SaveChangesAsync();
    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;
}
```

### Rule 9: All Strings in URL/JSON are UTF-8 Encoded

- BMAD supports Arabic, Chinese, Russian, etc.
- Use `System.Text.Json` (handles UTF-8 natively)
- Test with multi-byte characters

### Rule 10: Code Splitting is REQUIRED for Frontend

- Lazy load features by route
- Keep initial bundle < 150KB gzipped
- Never load all features upfront

---

## CODING STANDARDS & PATTERNS

### C# Backend Standards

#### Naming Conventions
```csharp
// ✅ CORRECT
public class WorkflowService { }
public async Task CreateWorkflowAsync() { }
private readonly IWorkflowRepository _repository;
private const int MaxRetries = 3;

// ❌ WRONG
public class workflowservice { }
public void CreateWorkflow() { }  // Not async
public IWorkflowRepository repository;  // Public field
const int maxRetries = 3;  // Constant not UPPER_CASE
```

#### Error Handling
```csharp
// ✅ CORRECT
catch (WorkflowConflictException ex)
{
    _logger.LogWarning(ex, "Concurrency conflict: {WorkflowId}", workflowId);
    throw;  // Re-throw for handler
}

// ❌ WRONG
catch (Exception) { }  // Swallowing exceptions
catch (Exception ex) { /* do nothing */ }
```

#### Entity Models
```csharp
// ✅ CORRECT
public class Workflow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int Version { get; set; } = 1;  // For optimistic concurrency
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [Column(TypeName = "jsonb")]
    public JsonDocument State { get; set; } = JsonDocument.Parse("{}");
}

// ❌ WRONG
public class Workflow
{
    public int Id { get; set; }  // Use Guid, not int
    // Missing Version field (concurrency control)
    public object State { get; set; }  // Use JsonDocument
}
```

### TypeScript/React Frontend Standards

#### Naming Conventions
```typescript
// ✅ CORRECT
const useWorkflows = () => { };
const WorkflowList: React.FC = () => <div />;
const [isLoading, setIsLoading] = useState(false);
const { data: workflows } = useQuery(...);

// ❌ WRONG
const useworkflows = () => { };  // Lowercase
const workflowList = () => <div />;  // Not capitalized
const [loading, setLoading] = useState(false);  // Not boolean name
```

#### Component Structure
```typescript
// ✅ CORRECT
export function WorkflowList() {
  const { data, isLoading, error } = useWorkflows();
  
  if (isLoading) return <Loading />;
  if (error) return <Error error={error} />;
  
  return <div>{/* render */}</div>;
}

// ❌ WRONG
export default function workflowList() {  // Named export + lowercase
  // No loading/error states
  return <div>{data.map(...)}</div>;
}
```

#### State Management
```typescript
// ✅ CORRECT - Clear separation
const useAuthStore = create(...);  // Zustand for client state
const useWorkflows = () => useQuery(...);  // TanStack Query for server state
const [formInput, setFormInput] = useState("");  // useState for local UI state

// ❌ WRONG
const [workflows, setWorkflows] = useState([]);  // Duplicates server state
// No clear structure for different types of state
```

---

## PERFORMANCE BASELINES & ALERT THRESHOLDS

### API Latency (p95)

| Endpoint | Target | Alert |
|----------|--------|-------|
| GET /workflows | < 100ms | > 500ms |
| POST /workflows | < 200ms | > 1000ms |
| POST /decisions/{id}/approve | < 300ms | > 1500ms |
| Any JSONB query | < 150ms | > 800ms |

### Database Metrics

| Metric | Target | Alert |
|--------|--------|-------|
| Connection pool used | < 70% | > 90% |
| Slow query (> 1s) | 0 | Any |
| JSONB query time | < 50ms | > 200ms |
| GIN index size | < 100MB | > 500MB |

### Frontend Performance

| Metric | Target | Alert |
|--------|--------|-------|
| Initial bundle size | 150KB | > 200KB gzipped |
| First contentful paint | < 2s | > 5s |
| WebSocket connection time | < 500ms | > 2s |
| React render time | < 50ms | > 200ms |

### Infrastructure

| Metric | Target | Alert |
|--------|--------|-------|
| CPU utilization | < 70% | > 85% |
| Memory usage | < 75% | > 90% |
| Disk usage | < 70% | > 85% |
| Error rate | < 0.1% | > 1% |
| Uptime | 99.5% | < 99% |

---

## QUALITY GATES & TESTING REQUIREMENTS

### Code Coverage Minimums

- **Backend:** 80% overall coverage
- **Critical paths** (auth, concurrency, state): 100%
- **Frontend:** 70% overall coverage
- **API endpoints:** Must have integration tests

### Test Types Required

| Type | Backend | Frontend | Coverage |
|------|---------|----------|----------|
| Unit | ✅ Required | ✅ Required | Functions, utilities |
| Integration | ✅ Required | ✅ Recommended | API endpoints, DB |
| E2E | ✅ Recommended | ✅ Required | Full workflows |
| Load | ✅ Required (MVP) | — | 500 req/sec |
| Security | ✅ Required | ✅ Required | Auth, data access |

### Pre-Commit Checks

```bash
# Backend
dotnet format --verify-no-changes
dotnet build
dotnet test

# Frontend
npm run lint
npm run type-check
npm test
```

---

## SECURITY CHECKLIST (Validate in Every PR)

- [ ] **Authentication:** Endpoint has `[Authorize]` attribute
- [ ] **Authorization:** Role/claim checks match requirements
- [ ] **Input Validation:** All user input validated with FluentValidation
- [ ] **SQL Injection:** Using parameterized queries (EF Core)
- [ ] **XSS Prevention:** No `dangerouslySetInnerHTML` in React
- [ ] **CSRF Protection:** SameSite cookie policy enforced
- [ ] **Rate Limiting:** Endpoints respecting rate limits
- [ ] **Logging:** No sensitive data in logs (passwords, tokens)
- [ ] **Error Messages:** Generic error messages to users, detailed logs server-side
- [ ] **HTTPS:** All endpoints use HTTPS
- [ ] **Dependencies:** No known vulnerabilities (`dotnet list package --vulnerable`)

---

## COMMON PITFALLS & HOW TO AVOID

### Pitfall 1: Forgetting Version Check on State Updates

**❌ WRONG:**
```csharp
var workflow = await _context.Workflows.FirstAsync(w => w.Id == id);
workflow.State = newState;
await _context.SaveChangesAsync();
// Sarah's change overwrites Marcus's change silently!
```

**✅ CORRECT:**
```csharp
if (workflow.Version != expectedVersion)
    throw new WorkflowConflictException(...);
workflow.Version++;
await _context.SaveChangesAsync();
```

### Pitfall 2: Mixing Synchronous and Asynchronous Code

**❌ WRONG:**
```csharp
public async Task<Workflow> GetAsync() 
{
    return _context.Workflows.First();  // Blocking call!
}
```

**✅ CORRECT:**
```csharp
public async Task<Workflow> GetAsync() 
{
    return await _context.Workflows.FirstAsync();  // Async
}
```

### Pitfall 3: Not Handling WebSocket Reconnection

**❌ WRONG:**
```typescript
const hub = new HubConnectionBuilder()
    .withUrl("/workflowhub")
    .build();  // No reconnection strategy
```

**✅ CORRECT:**
```typescript
const hub = new HubConnectionBuilder()
    .withUrl("/workflowhub")
    .withAutomaticReconnect([0, 2000, 10000])  // Exponential backoff
    .build();
```

### Pitfall 4: Storing Sensitive Data in Frontend State

**❌ WRONG:**
```typescript
localStorage.setItem("token", accessToken);  // XSS vulnerability
const [password, setPassword] = useState("");  // Could be leaked
```

**✅ CORRECT:**
```typescript
// Token in memory only (lost on page refresh, but secure)
const token = useRef<string | null>(null);

// Use HttpOnly cookie (server handles token)
// React can't access it, so XSS can't steal it
```

### Pitfall 5: Ignoring JSONB Query Performance

**❌ WRONG:**
```csharp
var workflows = _context.Workflows.ToList();  // Load all to memory
var approved = workflows
    .Where(w => w.State.ToString().Contains("approved"))
    .ToList();  // Slow! O(n) search
```

**✅ CORRECT:**
```csharp
var approved = await _context.Workflows
    .Where(w => EF.Functions.JsonContains(
        w.State, 
        "{\"status\": \"approved\"}"))  // Uses GIN index
    .ToListAsync();
```

### Pitfall 6: Race Condition in SignalR Handlers

**❌ WRONG:**
```csharp
public async Task UpdateWorkflow(WorkflowUpdate update)
{
    var workflow = await _repository.GetAsync(update.Id);
    workflow.Data = update.Data;
    await _repository.SaveAsync(workflow);
    // Two concurrent calls = one update lost!
}
```

**✅ CORRECT:**
```csharp
public async Task UpdateWorkflow(WorkflowUpdate update)
{
    try 
    {
        var workflow = await _repository.GetAsync(update.Id);
        if (workflow.Version != update.ExpectedVersion)
            throw new ConflictException();
        
        workflow.Version++;
        await _repository.SaveAsync(workflow);
    }
    catch (ConflictException ex)
    {
        await Clients.Caller.SendAsync("conflict", 
            new { currentVersion = workflow.Version });
    }
}
```

---

## DEPENDENCY MANAGEMENT RULES

### Backend NuGet Packages

**LOCKED VERSIONS** (do not upgrade without approval):
- Entity Framework Core: 9.0.x
- SignalR: 8.0.x
- FluentValidation: 11.9.x
- System.Threading.RateLimiting: Latest

**How to update:**
1. Create feature branch
2. Update one package at a time
3. Run full test suite
4. Test integration with related packages
5. Get code review before merging

### Frontend npm Packages

**LOCKED VERSIONS:**
- React: 18.x
- Zustand: 4.5.x
- TanStack Query: 5.x
- React Router: 7.x
- TypeScript: 5.x

**Security Updates:**
- Run `npm audit` weekly
- Fix high/critical vulnerabilities immediately
- Test thoroughly before deploying

---

## DATABASE MIGRATION RULES

### Before Creating Migration

- [ ] Model change is documented in ADR
- [ ] Tested locally with `dotnet ef migrations add`
- [ ] Rollback scenario tested (`dotnet ef migrations remove`)

### Migration Checklist

- [ ] No data loss (backfill strategy documented)
- [ ] No production downtime (using PostgreSQL online DDL)
- [ ] Indexes created for frequently-queried columns
- [ ] Foreign keys properly configured
- [ ] Triggers/constraints verified

### Post-Migration Verification

```sql
-- Verify schema matches expectations
\dt workflows  -- List tables
\d workflows   -- Describe schema
SELECT * FROM __efmigrationshistory;  -- Verify migration applied
```

---

## FINAL VALIDATION CHECKLIST (Before Code Review)

- [ ] All tests passing (`dotnet test` + `npm test`)
- [ ] No compiler warnings (`dotnet build` clean)
- [ ] Code formatted (`dotnet format`)
- [ ] Linting passes (`npm run lint`)
- [ ] Type checking passes (`npm run type-check`)
- [ ] No performance regressions
- [ ] Security checklist items verified
- [ ] Architecture rules followed
- [ ] Documentation updated (ADRs, code comments)
- [ ] Git commit message is descriptive

---

**This document is LOCKED. Changes require architect approval.**

Last updated: 2026-01-23  
Next review: 2026-02-20
