# Story 9.1: Event Log Architecture

**Status:** ready-for-dev

## Story

As a developer, I want all workflow events logged immutably, so that we have a complete audit trail.

## Acceptance Criteria

**Given** any workflow action occurs  
**When** the action completes  
**Then** an event is appended to the WorkflowEvents table with: id, workflowInstanceId, eventType, payload, userId, timestamp, correlationId

**Given** the event log schema exists  
**When** I examine the table  
**Then** it uses append-only semantics (no UPDATE/DELETE in application code)  
**And** partitioning is configured by month for performance

**Given** events are logged  
**When** I query by workflowInstanceId  
**Then** I can reconstruct the complete workflow history in order

**Given** event types are defined  
**When** I check the enum  
**Then** I see: WorkflowStarted, StepCompleted, DecisionMade, UserInput, AgentResponse, StateChanged, Error, etc.

**Given** I need to replay events  
**When** I call EventStore.Replay(workflowId, fromSequence)  
**Then** events are returned in sequence order  
**And** I can rebuild state from any point in history

## Tasks / Subtasks

- [ ] Create WorkflowEvent entity model (AC: 1, 3)
  - [ ] Add properties: Id (Guid), WorkflowInstanceId (Guid), EventType (enum), Payload (JSONB), UserId (Guid), Timestamp (DateTime), CorrelationId (string), SequenceNumber (long)
  - [ ] Add indexes: workflowInstanceId, timestamp, eventType
  - [ ] Ensure model is immutable (readonly properties after construction)
- [ ] Create WorkflowEventType enum (AC: 4)
  - [ ] Define event types: WorkflowStarted, StepCompleted, DecisionMade, UserInput, AgentResponse, StateChanged, Error, WorkflowPaused, WorkflowResumed, WorkflowCancelled, WorkflowCompleted
  - [ ] Add XML documentation for each event type
- [ ] Create database migration for WorkflowEvents table (AC: 2)
  - [ ] Use EF Core migrations to create table
  - [ ] Add monthly partitioning configuration (PostgreSQL native partitioning)
  - [ ] Create indexes for performance: (workflowInstanceId, sequenceNumber), (timestamp)
  - [ ] Set up append-only constraints (no UPDATE/DELETE triggers)
- [ ] Implement EventStore service (AC: 5)
  - [ ] Create IEventStore interface with methods: AppendAsync, GetEventsAsync, ReplayAsync
  - [ ] Implement EventStore service with PostgreSQL backend
  - [ ] AppendAsync: Insert event with auto-incremented sequence number
  - [ ] GetEventsAsync: Query by workflowInstanceId with ordering
  - [ ] ReplayAsync: Get events from specific sequence number
  - [ ] Add logging for all operations
- [ ] Integrate event logging into existing workflow services (AC: 1)
  - [ ] Update WorkflowInstanceService to log WorkflowStarted, WorkflowCompleted, WorkflowCancelled events
  - [ ] Update StepExecutor (from Story 4.3) to log StepCompleted events
  - [ ] Ensure all state changes trigger StateChanged events
  - [ ] Add error logging for exceptions
- [ ] Add partition management (AC: 2)
  - [ ] Create monthly partition creation job
  - [ ] Add background service to auto-create partitions for next month
  - [ ] Add partition cleanup for old data (retention policy)
- [ ] Write unit tests
  - [ ] Test EventStore.AppendAsync inserts events correctly
  - [ ] Test EventStore.GetEventsAsync returns events in order
  - [ ] Test EventStore.ReplayAsync filters by sequence number
  - [ ] Test event immutability (cannot update/delete)
  - [ ] Test all event types are logged correctly
- [ ] Write integration tests
  - [ ] End-to-end workflow event logging
  - [ ] Verify event replay reconstructs workflow state
  - [ ] Test partition creation and querying
  - [ ] Verify append-only enforcement

## Dev Notes

### Architecture Alignment

**Source:** [ARCHITECTURE.MD - Event Log, JSONB Storage]

- Create entity: `src/bmadServer.ApiService/Models/Events/WorkflowAuditEvent.cs` (Note: Renamed to avoid conflict with existing WorkflowEvent.cs)
- Create enum: `src/bmadServer.ApiService/Models/Events/WorkflowEventType.cs`
- Create service: `src/bmadServer.ApiService/Services/Events/EventStore.cs`
- Create interface: `src/bmadServer.ApiService/Services/Events/IEventStore.cs`
- Migration: `src/bmadServer.ApiService/Migrations/XXX_CreateWorkflowEventsTable.cs`
- Follow PostgreSQL JSONB pattern from existing event_logs table structure

### Technical Requirements

**Event Log Schema:**
```csharp
// Note: Named WorkflowAuditEvent to avoid conflict with existing WorkflowEvent.cs
// The existing WorkflowEvent is used for simple status change tracking.
// This new entity is for comprehensive event sourcing with JSONB payloads.
public class WorkflowAuditEvent
{
    public Guid Id { get; init; }
    public Guid WorkflowInstanceId { get; init; }
    public WorkflowEventType EventType { get; init; }
    public JsonDocument Payload { get; init; }  // System.Text.Json JSONB
    public Guid? UserId { get; init; }
    public DateTime Timestamp { get; init; }
    public string? CorrelationId { get; init; }
    public long SequenceNumber { get; init; }  // Auto-increment per workflow
}
```

**PostgreSQL Partitioning:**
```sql
-- Create partitioned table by month
-- Note: Using workflow_audit_events to avoid conflict with existing workflow_events table
-- sequence_number is application-managed per workflow (not BIGSERIAL for global sequence)
CREATE TABLE workflow_audit_events (
    id UUID PRIMARY KEY,
    workflow_instance_id UUID NOT NULL,
    event_type VARCHAR(100) NOT NULL,
    payload JSONB NOT NULL,
    user_id UUID,
    timestamp TIMESTAMP NOT NULL,
    correlation_id VARCHAR(255),
    sequence_number BIGINT NOT NULL  -- Application manages per-workflow sequencing
) PARTITION BY RANGE (timestamp);

-- Create unique index to enforce per-workflow sequential ordering
CREATE UNIQUE INDEX idx_workflow_audit_events_workflow_sequence 
    ON workflow_audit_events (workflow_instance_id, sequence_number);

-- Create index on JSONB payload for querying
CREATE INDEX idx_workflow_audit_events_payload ON workflow_audit_events USING GIN (payload);

-- Example partition for January 2026
CREATE TABLE workflow_audit_events_2026_01 PARTITION OF workflow_audit_events
    FOR VALUES FROM ('2026-01-01') TO ('2026-02-01');
```

**Append-Only Enforcement:**
```csharp
// In EventStore, configure entity with proper key
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<WorkflowAuditEvent>(builder =>
    {
        builder.ToTable("workflow_audit_events");
        builder.HasKey(e => e.Id);
        
        // Append-only is enforced by:
        // 1. Service layer only exposes AppendAsync (no Update/Delete methods)
        // 2. Database trigger prevents UPDATE/DELETE operations
        // 3. All properties use 'init' accessors (immutable after construction)
    });
}
```

**Event Replay Pattern:**
```csharp
public async Task<IEnumerable<WorkflowAuditEvent>> ReplayAsync(
    Guid workflowId, 
    long fromSequence = 0, 
    CancellationToken cancellationToken = default)
{
    return await _context.WorkflowEvents
        .Where(e => e.WorkflowInstanceId == workflowId 
                 && e.SequenceNumber >= fromSequence)
        .OrderBy(e => e.SequenceNumber)
        .AsNoTracking()
        .ToListAsync(cancellationToken);
}
```

### File Structure Requirements

```
src/bmadServer.ApiService/
├── Models/
│   └── Events/
│       ├── WorkflowAuditEvent.cs (new - renamed to avoid conflict with existing WorkflowEvent)
│       └── WorkflowEventType.cs (new)
├── Services/
│   └── Events/
│       ├── IEventStore.cs (new)
│       └── EventStore.cs (new)
└── Migrations/
    └── XXX_CreateWorkflowAuditEventsTable.cs (new)
```

**EventStore Implementation with Per-Workflow Sequencing:**
```csharp
public class EventStore : IEventStore
{
    private readonly ApplicationDbContext _context;
    
    public async Task AppendEventAsync(WorkflowAuditEvent @event)
    {
        // Get next sequence number for this workflow
        var nextSequence = await _context.WorkflowAuditEvents
            .Where(e => e.WorkflowInstanceId == @event.WorkflowInstanceId)
            .MaxAsync(e => (long?)e.SequenceNumber) ?? 0;
        
        var eventWithSequence = @event with 
        { 
            SequenceNumber = nextSequence + 1,
            Id = @event.Id == Guid.Empty ? Guid.NewGuid() : @event.Id,
            Timestamp = @event.Timestamp == default ? DateTime.UtcNow : @event.Timestamp
        };
        
        _context.WorkflowAuditEvents.Add(eventWithSequence);
        await _context.SaveChangesAsync();
    }
}
```
│       ├── EventStore.cs (new)
│       └── PartitionManagementService.cs (new - background service)
└── Data/
    └── Migrations/
        └── XXX_CreateWorkflowEventsTable.cs (new)
```

### Dependencies

**From Previous Stories:**
- Story 4.2: WorkflowInstance entity (FK reference)
- Story 4.3: StepExecutor service (needs event logging integration)
- Story 2.1: User entity (FK reference for userId)

**NuGet Packages:**
- System.Text.Json (already in project) - for JSONB payload
- Npgsql.EntityFrameworkCore.PostgreSQL (already in project) - for partitioning

### Testing Requirements

**Unit Tests:** `test/bmadServer.Tests/Services/Events/EventStoreTests.cs`

**Test Coverage:**
- Append events with all required fields
- Query events by workflow ID
- Replay from specific sequence number
- Verify sequence number auto-increment
- Test partition creation logic
- Verify no UPDATE/DELETE operations allowed

**Integration Tests:** `test/bmadServer.Tests/Integration/Events/EventLogIntegrationTests.cs`

**Test Coverage:**
- Full workflow execution logs all expected events
- Event replay successfully reconstructs workflow state
- Partition switching works correctly at month boundaries
- Concurrent event appends maintain sequence integrity

### Integration Notes

**Connection to Other Stories:**
- Story 9.2: State changes trigger StateChanged events
- Story 9.3: Artifact creation triggers events
- Story 9.5: Checkpoint creation logs snapshot events
- Story 9.6: Audit log queries use event log data

**Performance Considerations:**
- Partition by month to keep index sizes manageable
- Use GIN index on JSONB for complex payload queries
- Batch event insertion for bulk operations
- Consider read replicas for heavy audit queries

### Previous Story Intelligence

**From Epic 4 Stories:**
- WorkflowInstance already has event logging placeholder (event_logs table exists)
- Current event_logs table uses JSONB with flexible schema - leverage this pattern
- Partition management can be handled via background service pattern from Story 4.4

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 9.1]
- [Source: ARCHITECTURE.md - Event Log, JSONB Storage, Database Layer]
- [PostgreSQL Partitioning: https://www.postgresql.org/docs/17/ddl-partitioning.html]
- [Event Sourcing Pattern: Martin Fowler - https://martinfowler.com/eaaDev/EventSourcing.html]


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

- Source: [epics.md - Story 9.1](../planning-artifacts/epics.md)
- Architecture: [architecture.md](../planning-artifacts/architecture.md)
- PRD: [prd.md](../planning-artifacts/prd.md)
