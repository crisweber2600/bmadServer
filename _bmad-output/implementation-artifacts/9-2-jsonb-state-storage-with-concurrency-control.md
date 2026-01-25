# Story 9.2: JSONB State Storage with Concurrency Control

**Status:** ready-for-dev

## Story

As a developer, I want JSONB state storage with proper concurrency control, so that concurrent updates don't corrupt workflow state.

## Acceptance Criteria

**Given** workflow state is stored as JSONB  
**When** I examine the schema  
**Then** state columns include: _version (int), _lastModifiedBy (uuid), _lastModifiedAt (timestamp)

**Given** I update workflow state  
**When** I send the update  
**Then** the system checks _version matches expected value  
**And** if mismatch, returns 409 Conflict with current state

**Given** optimistic concurrency fails  
**When** the client receives 409  
**Then** the client can fetch current state, merge changes, and retry  
**And** the conflict is logged for monitoring

**Given** I need atomic state updates  
**When** multiple fields must change together  
**Then** the update is wrapped in a database transaction  
**And** partial updates are impossible

**Given** GIN indexes exist on JSONB columns  
**When** I query by JSONB path (e.g., state->'currentStep')  
**Then** the query uses the index  
**And** performance is acceptable (< 100ms for typical queries)

## Tasks / Subtasks

- [ ] Add versioning fields to WorkflowInstance model (AC: 1)
  - [ ] Add Version property (int) with ConcurrencyCheck attribute
  - [ ] Add LastModifiedBy property (Guid?)
  - [ ] Add LastModifiedAt property (DateTime)
  - [ ] Create migration to add columns to workflow_instances table
- [ ] Enhance WorkflowInstance with JSONB state column (AC: 1, 5)
  - [ ] Add State property (JsonDocument) for flexible workflow state
  - [ ] Add StateMetadata property (JsonDocument) for additional metadata
  - [ ] Update migration to add JSONB columns with GIN indexes
- [ ] Implement optimistic concurrency checking (AC: 2, 3)
  - [ ] Update WorkflowInstanceService.UpdateWorkflowAsync to check version
  - [ ] Detect DbUpdateConcurrencyException from EF Core
  - [ ] Return 409 Conflict with current state when version mismatch occurs
  - [ ] Log concurrency conflicts for monitoring
  - [ ] Create custom ConcurrencyException with current state details
- [ ] Add atomic state update methods (AC: 4)
  - [ ] Create UpdateStateAtomicallyAsync method with transaction scope
  - [ ] Ensure all state changes within single database transaction
  - [ ] Add rollback on any failure
  - [ ] Add integration with event logging (Story 9.1)
- [ ] Configure GIN indexes for JSONB queries (AC: 5)
  - [ ] Add GIN index on State column in migration
  - [ ] Add GIN index on StateMetadata column
  - [ ] Create test queries to verify index usage (EXPLAIN ANALYZE)
  - [ ] Document common query patterns for developers
- [ ] Add API endpoint for state updates (AC: 2, 3)
  - [ ] PATCH /api/v1/workflows/{id}/state
  - [ ] Accept version in request header (If-Match: "version")
  - [ ] Return 409 with current state on version mismatch
  - [ ] Return 200 with updated state on success
- [ ] Create conflict resolution helpers (AC: 3)
  - [ ] Add StateConflictResolver utility class
  - [ ] Implement merge strategies: ClientWins, ServerWins, CustomMerge
  - [ ] Provide retry helper with exponential backoff
  - [ ] Document conflict resolution patterns for frontend
- [ ] Write unit tests
  - [ ] Test optimistic concurrency detection
  - [ ] Test conflict exception handling
  - [ ] Test atomic transaction rollback on failure
  - [ ] Test version increment on successful update
  - [ ] Test LastModifiedBy and LastModifiedAt tracking
- [ ] Write integration tests
  - [ ] Concurrent update test (simulate race condition)
  - [ ] Verify 409 Conflict response with current state
  - [ ] Test client retry with merged state
  - [ ] Verify GIN index usage with EXPLAIN ANALYZE
  - [ ] Test complex JSONB queries performance

## Dev Notes

### Architecture Alignment

**Source:** [ARCHITECTURE.MD - Database Layer, JSONB Storage]

- Modify entity: `src/bmadServer.ApiService/Models/Workflows/WorkflowInstance.cs`
- Modify service: `src/bmadServer.ApiService/Services/Workflows/WorkflowInstanceService.cs`
- Create utilities: `src/bmadServer.ApiService/Services/Workflows/StateConflictResolver.cs`
- Create exception: `src/bmadServer.ApiService/Exceptions/ConcurrencyException.cs`
- Migration: `src/bmadServer.ApiService/Data/Migrations/XXX_AddVersioningAndStateToWorkflowInstance.cs`

### Technical Requirements

**Versioning Pattern:**
```csharp
public class WorkflowInstance
{
    public Guid Id { get; set; }
    // ... existing properties ...
    
    [ConcurrencyCheck]
    public int Version { get; set; }
    
    public Guid? LastModifiedBy { get; set; }
    public DateTime LastModifiedAt { get; set; }
    
    // JSONB state storage
    public JsonDocument? State { get; set; }
    public JsonDocument? StateMetadata { get; set; }
}
```

**Migration - Add Versioning & JSONB:**
```sql
ALTER TABLE workflow_instances 
    ADD COLUMN version INT NOT NULL DEFAULT 0,
    ADD COLUMN last_modified_by UUID,
    ADD COLUMN last_modified_at TIMESTAMP NOT NULL DEFAULT NOW(),
    ADD COLUMN state JSONB,
    ADD COLUMN state_metadata JSONB;

-- Create GIN indexes for JSONB queries
CREATE INDEX idx_workflow_instances_state ON workflow_instances USING GIN (state);
CREATE INDEX idx_workflow_instances_state_metadata ON workflow_instances USING GIN (state_metadata);

-- Create index on specific JSONB paths (example)
CREATE INDEX idx_workflow_instances_current_step ON workflow_instances 
    USING btree ((state->>'currentStep'));
```

**Optimistic Concurrency Pattern:**
```csharp
public async Task<WorkflowInstance> UpdateStateAsync(
    Guid workflowId, 
    JsonDocument newState, 
    int expectedVersion,
    Guid userId,
    CancellationToken cancellationToken = default)
{
    using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    
    try
    {
        var workflow = await _context.WorkflowInstances
            .FindAsync(new object[] { workflowId }, cancellationToken);
            
        if (workflow == null)
            throw new NotFoundException($"Workflow {workflowId} not found");
            
        // Check version for optimistic concurrency
        if (workflow.Version != expectedVersion)
        {
            _logger.LogWarning(
                "Concurrency conflict: expected version {Expected}, actual {Actual}",
                expectedVersion, workflow.Version);
                
            throw new ConcurrencyException(
                "Workflow state has been modified by another user",
                workflow.Version,
                workflow.State);
        }
        
        // Update state atomically
        workflow.State = newState;
        workflow.Version++;
        workflow.LastModifiedBy = userId;
        workflow.LastModifiedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        
        // Log state change event
        await _eventStore.AppendAsync(new WorkflowEvent
        {
            WorkflowInstanceId = workflowId,
            EventType = WorkflowEventType.StateChanged,
            Payload = newState,
            UserId = userId
        }, cancellationToken);
        
        return workflow;
    }
    catch
    {
        await transaction.RollbackAsync(cancellationToken);
        throw;
    }
}
```

**API Endpoint Pattern:**
```csharp
[HttpPatch("{id}/state")]
public async Task<IActionResult> UpdateState(
    Guid id,
    [FromBody] UpdateStateRequest request,
    [FromHeader(Name = "If-Match")] int? ifMatch)
{
    if (!ifMatch.HasValue)
        return BadRequest("If-Match header required for versioning");
        
    try
    {
        var updated = await _workflowService.UpdateStateAsync(
            id, 
            request.State, 
            ifMatch.Value,
            User.GetUserId());
            
        return Ok(new
        {
            Version = updated.Version,
            State = updated.State,
            LastModifiedAt = updated.LastModifiedAt
        });
    }
    catch (ConcurrencyException ex)
    {
        return Conflict(new
        {
            Error = "State has been modified by another user",
            CurrentVersion = ex.CurrentVersion,
            CurrentState = ex.CurrentState,
            Message = "Please refresh and try again"
        });
    }
}
```

**GIN Index Usage Query:**
```csharp
// This query will use the GIN index
var workflows = await _context.WorkflowInstances
    .Where(w => EF.Functions.JsonContains(
        w.State, 
        JsonDocument.Parse(@"{""currentStep"": ""review""}")))
    .ToListAsync();
    
// JSONB path query
var workflows = await _context.WorkflowInstances
    .FromSqlRaw(@"
        SELECT * FROM workflow_instances 
        WHERE state @> '{""status"": ""active""}'
        AND state->>'currentStep' = 'review'
    ")
    .ToListAsync();
```

### File Structure Requirements

```
src/bmadServer.ApiService/
├── Models/
│   └── Workflows/
│       └── WorkflowInstance.cs (modify - add versioning fields)
├── Services/
│   └── Workflows/
│       ├── WorkflowInstanceService.cs (modify - add concurrency logic)
│       └── StateConflictResolver.cs (new)
├── Exceptions/
│   └── ConcurrencyException.cs (new)
├── Controllers/
│   └── WorkflowsController.cs (modify - add PATCH /state endpoint)
└── Data/
    └── Migrations/
        └── XXX_AddVersioningAndStateToWorkflowInstance.cs (new)
```

### Dependencies

**From Previous Stories:**
- Story 4.2: WorkflowInstance entity (modify)
- Story 4.2: WorkflowInstanceService (modify)
- Story 9.1: EventStore (log StateChanged events)

**NuGet Packages:**
- System.Text.Json (already in project) - for JsonDocument
- Npgsql.EntityFrameworkCore.PostgreSQL (already in project) - for JSONB support

### Testing Requirements

**Unit Tests:** `test/bmadServer.Tests/Services/Workflows/ConcurrencyControlTests.cs`

**Test Coverage:**
- Successful state update increments version
- Version mismatch throws ConcurrencyException
- Transaction rollback on any failure
- LastModifiedBy and LastModifiedAt are set correctly
- StateConflictResolver merge strategies

**Integration Tests:** `test/bmadServer.Tests/Integration/Workflows/ConcurrencyScenariosTests.cs`

**Test Coverage:**
- Concurrent updates (parallel requests)
- Client receives 409 with current state
- Client successfully retries after merging
- GIN index performance validation
- Complex JSONB queries performance

### Performance Considerations

**Index Strategy:**
- GIN index on full JSONB column for flexible queries
- B-tree indexes on frequently accessed JSONB paths (e.g., currentStep)
- Monitor query performance with pg_stat_statements

**Query Optimization:**
```sql
-- Verify index usage
EXPLAIN ANALYZE 
SELECT * FROM workflow_instances 
WHERE state @> '{"status": "active"}';

-- Should show: Index Scan using idx_workflow_instances_state
```

### Integration Notes

**Connection to Other Stories:**
- Story 9.1: All state changes log StateChanged events
- Story 9.5: Checkpoints capture full state including version
- Story 4.4: Resume workflow uses state with version checking

**Conflict Resolution Strategies:**
1. **Client Wins:** Overwrite server state (dangerous)
2. **Server Wins:** Discard client changes (safe but loses work)
3. **Custom Merge:** Merge non-conflicting fields (recommended)

### Previous Story Intelligence

**From Epic 4 Stories:**
- WorkflowInstance already exists with basic fields
- Current implementation uses simple status enum
- No existing concurrency control - this is critical addition
- State transitions already logged in event_logs

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 9.2]
- [Source: ARCHITECTURE.md - JSONB Storage, Database Layer]
- [EF Core Concurrency: https://learn.microsoft.com/en-us/ef/core/saving/concurrency]
- [PostgreSQL JSONB: https://www.postgresql.org/docs/17/datatype-json.html]
- [Optimistic Concurrency Pattern: Microsoft Docs]


---

## Aspire Development Standards

### PostgreSQL Connection Pattern

This story uses PostgreSQL configured in Story 1.2 via Aspire:
- Connection string automatically injected from Aspire AppHost
- Pattern: `builder.AddServiceDefaults();` (inherits PostgreSQL reference)
- See Story 1.2 for AppHost configuration pattern

### Project-Wide Standards

This story follows the Aspire-first development pattern:
- **Reference:** [PROJECT-WIDE-RULES.md](../../../PROJECT-WIDE-RULES.md)
- **Primary Documentation:** https://aspire.dev
- **GitHub:** https://github.com/microsoft/aspire

---

## References
- **Aspire Rules:** [PROJECT-WIDE-RULES.md](../../../PROJECT-WIDE-RULES.md)
- **Aspire Docs:** https://aspire.dev

- Source: [epics.md - Story 9.2](../planning-artifacts/epics.md)
- Architecture: [architecture.md](../planning-artifacts/architecture.md)
- PRD: [prd.md](../planning-artifacts/prd.md)
