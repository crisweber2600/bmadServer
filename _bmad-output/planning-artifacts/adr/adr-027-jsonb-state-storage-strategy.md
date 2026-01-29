# ADR-027: JSONB State Storage Strategy with Concurrency Control

**Status:** Accepted  
**Date:** 2026-01-28  
**Context:** Epic 9 Story 9-2  
**Deciders:** Winston (Architect), Amelia (Dev)

## Context and Problem Statement

Workflow instances require flexible state storage that can evolve without schema migrations. We need to store complex, hierarchical workflow state while providing optimistic concurrency control for multi-user collaboration.

## Decision Drivers

- Workflow state structure varies by workflow type
- Need schema flexibility without frequent migrations
- Must support concurrent updates from multiple users
- Query capability for workflow state (filtering, searching)
- PostgreSQL JSONB provides native indexing and querying

## Considered Options

1. **Pure EF Core Entities** - Strongly typed C# classes
2. **PostgreSQL JSONB Column** - Flexible JSON storage
3. **Hybrid Approach** - Core fields as columns + JSONB for dynamic state

## Decision Outcome

**Chosen: Option 3 - Hybrid Approach**

Store core workflow metadata as typed columns with dynamic state in a JSONB column, using PostgreSQL's optimistic locking capabilities.

### Schema Design

```csharp
public class WorkflowInstance
{
    // Typed columns for core metadata (existing)
    public Guid Id { get; set; }
    public string WorkflowDefinitionId { get; set; }
    public WorkflowStatus Status { get; set; }
    public Guid UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // NEW: JSONB state storage
    public JsonDocument State { get; set; }  // Workflow-specific state
    public int Version { get; set; }          // Optimistic concurrency
    public string StateChecksum { get; set; } // Additional integrity check
}
```

### State Structure Example

```json
{
  "currentStep": "gather-requirements",
  "variables": {
    "projectName": "MyApp",
    "targetPlatform": "web"
  },
  "agentContext": {
    "currentAgent": "pm",
    "conversationHistory": [...]
  },
  "userInputs": {
    "step1": { "answer": "..." }
  },
  "metadata": {
    "lastCheckpoint": "2026-01-28T10:00:00Z"
  }
}
```

### Concurrency Control Strategy

**Optimistic Locking with Version Field:**

```csharp
// Before update
var instance = await context.WorkflowInstances
    .FirstOrDefaultAsync(w => w.Id == id);

instance.State = newState;
instance.Version++;  // Increment version
instance.UpdatedAt = DateTime.UtcNow;

try 
{
    await context.SaveChangesAsync();
}
catch (DbUpdateConcurrencyException)
{
    // Handle conflict: merge or reject
    throw new WorkflowConcurrencyException();
}
```

**EF Core Configuration:**

```csharp
modelBuilder.Entity<WorkflowInstance>()
    .Property(w => w.Version)
    .IsConcurrencyToken();

modelBuilder.Entity<WorkflowInstance>()
    .Property(w => w.State)
    .HasColumnType("jsonb");
```

### Indexing Strategy

```sql
-- GIN index for JSONB queries
CREATE INDEX idx_workflow_state_gin ON workflow_instances USING GIN (state);

-- Partial indexes for common queries
CREATE INDEX idx_workflow_current_step 
    ON workflow_instances ((state->>'currentStep'))
    WHERE status = 'InProgress';

-- Expression index for nested paths
CREATE INDEX idx_workflow_agent 
    ON workflow_instances ((state->'agentContext'->>'currentAgent'));
```

### Query Patterns

```csharp
// Find workflows at specific step
var workflows = await context.WorkflowInstances
    .Where(w => EF.Functions.JsonContains(
        w.State, 
        JsonDocument.Parse("{\"currentStep\":\"gather-requirements\"}")
    ))
    .ToListAsync();

// Query nested properties
var pmWorkflows = await context.WorkflowInstances
    .FromSqlRaw(@"
        SELECT * FROM workflow_instances 
        WHERE state->'agentContext'->>'currentAgent' = 'pm'
        AND status = 'InProgress'
    ")
    .ToListAsync();
```

## Implementation Notes

- Use `System.Text.Json.JsonDocument` for JSONB mapping
- Implement `IWorkflowStateService` for state operations
- Add state validation before persistence
- Checksum validation for critical state changes
- Migration scripts to add JSONB columns to existing tables

## Consequences

### Positive
- Schema flexibility for different workflow types
- Strong concurrency control with version tracking
- Efficient JSONB queries with PostgreSQL indexes
- Can store arbitrarily complex state without migrations

### Negative
- Type safety reduced for dynamic state
- Requires JSON serialization/deserialization overhead
- GIN indexes consume more disk space
- Query syntax more complex for nested properties

### Neutral
- State schema must be documented externally
- Validation logic lives in application layer

## Related Decisions

- ADR-001: Hybrid Data Modeling
- ADR-026: Event Log Architecture
- ADR-030: Checkpoint & Restoration Guarantees

## References

- Epic 9 Story 9-2: jsonb-state-storage-with-concurrency-control
- PostgreSQL JSONB documentation
- EF Core JSON columns support
