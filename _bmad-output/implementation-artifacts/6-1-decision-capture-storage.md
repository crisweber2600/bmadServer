# Story 6.1: Decision Capture & Storage

**Status:** ready-for-dev

## Story

As a user (Sarah), I want my decisions to be captured and stored, so that I have a record of what was decided and when.

## Acceptance Criteria

**Given** I make a decision in a workflow  
**When** I confirm my choice  
**Then** a Decision record is created with: id, workflowInstanceId, stepId, decisionType, value, decidedBy, decidedAt

**Given** a decision is stored  
**When** I query GET `/api/v1/workflows/{id}/decisions`  
**Then** I receive all decisions for that workflow in chronological order

**Given** I examine a decision  
**When** I view the decision details  
**Then** I see: the question asked, options presented, selected option, reasoning (if provided), context at time of decision

**Given** the Decisions table migration runs  
**When** I check the schema  
**Then** it includes JSONB for value and context, with GIN indexes for querying

**Given** a decision involves structured data  
**When** the decision is captured  
**Then** the value is stored as validated JSON matching the expected schema

## Tasks / Subtasks

- [ ] Design Decision entity model with JSONB support (AC: 1, 4)
  - [ ] Create Decision entity class with Id, WorkflowInstanceId, StepId, DecisionType, Value (JSONB), Context (JSONB), DecidedBy, DecidedAt, CreatedAt, UpdatedAt
  - [ ] Add EF Core entity configuration for JSONB columns with GIN indexes
  - [ ] Define DecisionType enum (TextInput, SingleChoice, MultipleChoice, NumericInput, DateInput, Structured)
  - [ ] Add foreign key relationships to WorkflowInstance and WorkflowStep
- [ ] Create EF Core migration for Decisions table (AC: 4)
  - [ ] Generate migration with `dotnet ef migrations add AddDecisionsTable`
  - [ ] Verify GIN indexes on Value and Context JSONB columns
  - [ ] Test migration locally before committing
- [ ] Implement DecisionService for decision capture logic (AC: 1, 5)
  - [ ] Create CaptureDecisionAsync method with validation
  - [ ] Add JSON schema validation for structured decisions
  - [ ] Implement GetWorkflowDecisionsAsync for retrieval
  - [ ] Add GetDecisionByIdAsync for single decision details
  - [ ] Include audit logging for all decision operations
- [ ] Create DecisionController with REST endpoints (AC: 2, 3)
  - [ ] POST `/api/v1/workflows/{id}/decisions` - Capture decision
  - [ ] GET `/api/v1/workflows/{id}/decisions` - List decisions chronologically
  - [ ] GET `/api/v1/decisions/{id}` - Get decision details
  - [ ] Add proper authorization (Participant/Admin only for POST)
- [ ] Add decision capture to workflow execution flow (AC: 1)
  - [ ] Integrate CaptureDecisionAsync calls in WorkflowExecutionService
  - [ ] Ensure decisions are captured when workflow steps complete
  - [ ] Link decisions to specific workflow steps
- [ ] Write unit tests for DecisionService (AC: 1, 5)
  - [ ] Test decision capture with all DecisionType variants
  - [ ] Test JSONB value storage and retrieval
  - [ ] Test schema validation for structured decisions
  - [ ] Test error handling for invalid decision data
- [ ] Write integration tests for Decision API endpoints (AC: 2, 3)
  - [ ] Test POST decision capture flow
  - [ ] Test GET decisions list with ordering
  - [ ] Test GET decision details with all fields
  - [ ] Test authorization rules
- [ ] Update OpenAPI documentation (AC: 2, 3)
  - [ ] Document decision DTOs and schemas
  - [ ] Add API endpoint examples
  - [ ] Document DecisionType enum values

## Dev Notes

### Architecture Context

This story implements the **Decision Tracker** component (see Architecture.md Component #4) which is critical for Epic 6's decision management capabilities. The Decision Tracker maintains the complete lifecycle and audit trail of all workflow decisions.

**Key Architectural Requirements:**
- **State Persistence Layer:** PostgreSQL with JSONB for flexible decision value storage (see Architecture.md lines 297-300)
- **Event Log Pattern:** All decision operations must be logged for audit trail (Architecture.md Component #4 - line 281)
- **Optimistic Concurrency:** Foundation for future locking mechanism in Story 6.3 (use EF Core version tokens)

### Database Schema Design

**Decision Entity Structure:**
```csharp
public class Decision
{
    public Guid Id { get; set; }
    public Guid WorkflowInstanceId { get; set; }
    public Guid StepId { get; set; }
    public DecisionType DecisionType { get; set; }
    public string Value { get; set; } // JSONB - stores the actual decision value
    public string Context { get; set; } // JSONB - stores question, options, reasoning
    public Guid DecidedBy { get; set; }
    public DateTime DecidedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int Version { get; set; } // For optimistic concurrency (Story 6.2)
    public DecisionStatus Status { get; set; } // Draft, UnderReview, Locked, etc. (Stories 6.3-6.4)
    
    // Navigation properties
    public WorkflowInstance WorkflowInstance { get; set; }
    public WorkflowStep WorkflowStep { get; set; }
    public User User { get; set; }
}

public enum DecisionType
{
    TextInput,
    SingleChoice,
    MultipleChoice,
    NumericInput,
    DateInput,
    Structured // For complex JSON schemas
}

public enum DecisionStatus
{
    Draft,
    UnderReview,
    Locked
}
```

**EF Core Configuration (Fluent API):**
```csharp
builder.Entity<Decision>(entity =>
{
    entity.HasKey(e => e.Id);
    entity.Property(e => e.Value).HasColumnType("jsonb"); // PostgreSQL JSONB
    entity.Property(e => e.Context).HasColumnType("jsonb");
    entity.HasIndex(e => e.Value).HasMethod("gin"); // GIN index for JSONB queries
    entity.HasIndex(e => e.Context).HasMethod("gin");
    entity.HasIndex(e => new { e.WorkflowInstanceId, e.DecidedAt }); // For chronological retrieval
    entity.Property(e => e.Version).IsConcurrencyToken(); // Optimistic concurrency
});
```

### Project Structure

**New Files to Create:**
- `src/bmadServer.ApiService/Models/Decision.cs` - Entity model
- `src/bmadServer.ApiService/Models/DecisionType.cs` - Enum
- `src/bmadServer.ApiService/Models/DecisionStatus.cs` - Enum (for future stories)
- `src/bmadServer.ApiService/Data/Configurations/DecisionConfiguration.cs` - EF Core fluent configuration
- `src/bmadServer.ApiService/Services/DecisionService.cs` - Business logic
- `src/bmadServer.ApiService/Services/IDecisionService.cs` - Interface
- `src/bmadServer.ApiService/Controllers/DecisionController.cs` - REST API
- `src/bmadServer.ApiService/DTOs/CaptureDecisionRequest.cs` - Request DTO
- `src/bmadServer.ApiService/DTOs/DecisionResponse.cs` - Response DTO
- `tests/bmadServer.ApiService.Tests/Services/DecisionServiceTests.cs` - Unit tests
- `tests/bmadServer.ApiService.IntegrationTests/DecisionApiTests.cs` - Integration tests

**Existing Files to Modify:**
- `src/bmadServer.ApiService/Data/ApplicationDbContext.cs` - Add DbSet<Decision>
- `src/bmadServer.ApiService/Program.cs` - Register IDecisionService
- `src/bmadServer.ApiService/Services/WorkflowExecutionService.cs` - Integrate decision capture

### Technical Requirements

**1. PostgreSQL JSONB Usage:**
- Use Npgsql.EntityFrameworkCore.PostgreSQL package (already configured in Story 1.2)
- JSONB allows flexible schema while maintaining queryability
- GIN indexes enable efficient JSONB queries
- Reference: https://www.npgsql.org/efcore/mapping/json.html

**2. EF Core 9.0 Migrations:**
- Migration pattern established in Stories 1.2, 2.1, 4.1-4.7
- Always test migrations locally before committing
- Use `dotnet ef migrations add AddDecisionsTable --project src/bmadServer.ApiService`
- Run migration: `dotnet ef database update --project src/bmadServer.ApiService`

**3. JSON Schema Validation:**
- For structured decisions (DecisionType.Structured), validate against JSON schemas
- Use System.Text.Json for parsing and validation
- Consider using Json.Schema.Net package for advanced validation
- Store validation errors in ProblemDetails response (RFC 7807 pattern)

**4. Authorization:**
- Only authenticated users can capture decisions
- Use `[Authorize]` attribute on POST endpoints
- Extract userId from JWT claims: `User.FindFirst(ClaimTypes.NameIdentifier)?.Value`
- Viewers can GET decisions but not create (check role in controller)

### Integration Points

**WorkflowExecutionService Integration:**
When a workflow step completes with user input, capture the decision:
```csharp
// In WorkflowExecutionService.CompleteStepAsync
var decision = new CaptureDecisionRequest
{
    WorkflowInstanceId = instance.Id,
    StepId = currentStep.Id,
    DecisionType = DetermineDecisionType(stepInput),
    Value = JsonSerializer.Serialize(stepInput.Value),
    Context = JsonSerializer.Serialize(new
    {
        Question = currentStep.Question,
        Options = currentStep.Options,
        Reasoning = stepInput.Reasoning
    }),
    DecidedBy = userId,
    DecidedAt = DateTime.UtcNow
};

await _decisionService.CaptureDecisionAsync(decision);
```

### Testing Strategy

**Unit Tests (DecisionServiceTests.cs):**
- Test all DecisionType variants (TextInput, SingleChoice, MultipleChoice, etc.)
- Test JSONB serialization/deserialization
- Test schema validation for structured decisions
- Test error handling for malformed JSON
- Test concurrency token behavior (Version field)
- Mock ApplicationDbContext and verify SaveChangesAsync calls

**Integration Tests (DecisionApiTests.cs):**
- Use WebApplicationFactory<Program> pattern (established in Story 1.4)
- Test POST `/api/v1/workflows/{id}/decisions` with valid data
- Test GET `/api/v1/workflows/{id}/decisions` returns chronological list
- Test GET `/api/v1/decisions/{id}` returns complete decision details
- Test authorization: unauthenticated returns 401, Viewer role on POST returns 403
- Verify JSONB data persists correctly in PostgreSQL
- Test with real database (use TestContainers or local PostgreSQL)

### Performance Considerations

**Query Optimization:**
- Index on (WorkflowInstanceId, DecidedAt) for chronological retrieval
- GIN indexes on JSONB columns for schema-based queries
- Consider pagination for large decision lists (Story 6.2 will expand on this)
- Use `.AsNoTracking()` for read-only queries to reduce memory overhead

**JSONB Best Practices:**
- Keep JSONB documents reasonably sized (< 1 MB recommended)
- Use specific JSONB operators for queries: `@>`, `?`, `?&`, etc.
- Avoid deep nesting in JSONB structures
- Consider extracting frequently-queried fields as regular columns

### Security Considerations

**Input Validation:**
- Validate DecisionType matches expected values
- Sanitize JSON content to prevent injection attacks
- Limit JSONB document size (use `[MaxLength]` on Value and Context)
- Validate WorkflowInstanceId and StepId exist before capturing decision

**Audit Logging:**
- Log all decision capture events with userId, timestamp, workflowId
- Use structured logging (ILogger<DecisionService>)
- Include before/after state for decision updates (future Story 6.2)
- Follow audit log pattern from Story 2.5 (RBAC implementation)

### Dependencies

**Required Stories (Completed):**
- Story 1.2: Configure PostgreSQL (✅ done) - Provides database foundation
- Story 4.1: Workflow Definition Registry (✅ done) - WorkflowInstance entity
- Story 4.2: Workflow Instance Creation (✅ done) - WorkflowInstance and WorkflowStep entities
- Story 2.1: User Registration & Auth (✅ done) - User entity and JWT claims

**Enables Future Stories:**
- Story 6.2: Decision Version History - Builds on Version field
- Story 6.3: Decision Locking Mechanism - Uses Status field
- Story 6.4: Decision Review Workflow - Uses Status transitions
- Story 6.5: Conflict Detection & Resolution - Queries decision history

### Previous Story Learnings

**From Epic 4 Stories (Workflow Orchestration):**
- Consistent use of Guid for entity IDs
- Service layer pattern with interface injection
- ProblemDetails for error responses (RFC 7807)
- Integration tests with WebApplicationFactory
- Always include CreatedAt/UpdatedAt timestamps
- Use async/await consistently for database operations
- Entity configurations in separate files under Data/Configurations/

**From Story 4.7 (Workflow Status & Progress API):**
- Pattern for chronological retrieval with proper indexing
- DTOs separate from entity models for API responses
- Include related entities in responses (e.g., user names, not just IDs)
- Pagination support for list endpoints

### Error Handling Patterns

**Common Error Scenarios:**
- WorkflowInstanceId not found → 404 Not Found
- StepId not found → 404 Not Found
- Invalid JSON in Value/Context → 400 Bad Request with validation details
- Unauthorized user → 401 Unauthorized
- Insufficient permissions → 403 Forbidden
- Database constraint violation → 409 Conflict
- Optimistic concurrency failure → 409 Conflict

**Response Format (ProblemDetails):**
```csharp
return Problem(
    statusCode: 400,
    title: "Invalid decision data",
    detail: "The decision value does not match the expected JSON schema",
    instance: HttpContext.Request.Path
);
```

## Project Structure Notes

### Alignment with Unified Project Structure

This story aligns with the established bmadServer project structure:
- **API Layer:** `src/bmadServer.ApiService/` - Controllers, DTOs, Services
- **Data Layer:** `src/bmadServer.ApiService/Data/` - EF Core configurations, migrations
- **Domain Models:** `src/bmadServer.ApiService/Models/` - Entity classes, enums
- **Tests:** `tests/bmadServer.ApiService.Tests/` and `tests/bmadServer.ApiService.IntegrationTests/`

### Aspire Integration

**PostgreSQL Connection:**
- Connection string automatically injected from Aspire AppHost (Story 1.2)
- Pattern: `builder.AddServiceDefaults();` inherits PostgreSQL reference
- AppHost configuration in `src/bmadServer.AppHost/Program.cs`

**Service Registration:**
```csharp
// In Program.cs
builder.Services.AddScoped<IDecisionService, DecisionService>();
```

### Naming Conventions

- **Entities:** PascalCase, singular (Decision, not Decisions)
- **DTOs:** PascalCase with suffix (CaptureDecisionRequest, DecisionResponse)
- **Services:** PascalCase with suffix (DecisionService, IDecisionService)
- **API Routes:** kebab-case, plural resources (`/api/v1/workflows/{id}/decisions`)
- **Database Tables:** PascalCase, plural (Decisions)
- **JSONB Columns:** PascalCase (Value, Context)

## References

- **Epic Source:** [epics.md - Epic 6, Story 6.1](../planning-artifacts/epics.md)
- **Architecture:** [architecture.md - Decision Tracker Component](../planning-artifacts/architecture.md)
- **PRD:** [prd.md - FR9, FR10, FR22, FR23](../planning-artifacts/prd.md)
- **Aspire Rules:** [PROJECT-WIDE-RULES.md](../../PROJECT-WIDE-RULES.md)
- **PostgreSQL JSONB:** https://www.npgsql.org/efcore/mapping/json.html
- **EF Core 9.0 Docs:** https://learn.microsoft.com/en-us/ef/core/
- **Aspire Documentation:** https://aspire.dev

---

## Dev Agent Record

### Agent Model Used

_To be filled by dev agent_

### Debug Log References

_To be filled by dev agent_

### Completion Notes List

_To be filled by dev agent_

### File List

_To be filled by dev agent during implementation_
