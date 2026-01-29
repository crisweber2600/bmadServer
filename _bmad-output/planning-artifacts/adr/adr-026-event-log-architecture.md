# ADR-026: Event Log Architecture for Audit Trail

**Status:** Accepted  
**Date:** 2026-01-28  
**Context:** Epic 9 Story 9-1  
**Deciders:** Winston (Architect), Amelia (Dev)

## Context and Problem Statement

bmadServer requires a comprehensive audit trail for workflow operations, decision changes, and user actions to support compliance, debugging, and state reconstruction. We need to decide between implementing full event sourcing vs. a simpler event log approach.

## Decision Drivers

- Existing entities already use traditional CRUD (WorkflowInstance, Decision, etc.)
- Need audit trail without rewriting entire data layer
- Must support querying event history efficiently
- Compliance requirements for tracking all decision changes
- Performance impact on write operations must be minimal

## Considered Options

1. **Full Event Sourcing** - All state derived from events
2. **Hybrid Event Log** - Traditional CRUD + append-only event log
3. **Audit Triggers** - PostgreSQL triggers for change tracking

## Decision Outcome

**Chosen: Option 2 - Hybrid Event Log**

We will implement an append-only event log table alongside existing CRUD entities, capturing significant business events without converting to full event sourcing.

### Architecture

```csharp
public class AuditEvent
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string EventType { get; set; }  // WorkflowStarted, DecisionLocked, etc.
    public Guid? EntityId { get; set; }
    public string EntityType { get; set; }
    public Guid UserId { get; set; }
    public JsonDocument EventData { get; set; }  // JSONB payload
    public string CorrelationId { get; set; }
    public string? ParentEventId { get; set; }
}
```

### Event Types to Log

**Workflow Events:**
- WorkflowStarted, WorkflowCompleted, WorkflowFailed, WorkflowPaused, WorkflowResumed
- StepStarted, StepCompleted, AgentHandoffRequested

**Decision Events:**
- DecisionCreated, DecisionUpdated, DecisionLocked, DecisionUnlocked
- DecisionReviewRequested, DecisionReviewApproved, DecisionReviewRejected

**Session Events:**
- SessionCreated, SessionRecovered, SessionExpired
- UserAuthenticated, UserLoggedOut

**Collaboration Events:**
- ParticipantJoined, ParticipantLeft, CheckpointCreated

### Storage Strategy

- **Partition Key:** Timestamp (monthly partitions for efficient querying)
- **Indexes:** 
  - (EntityId, EntityType) for entity history
  - (UserId, Timestamp) for user activity
  - (CorrelationId) for distributed tracing
  - GIN index on EventData for JSONB queries
- **Retention:** 7 years (configurable via appsettings)
- **Archival:** Move events older than 1 year to cold storage table

## Implementation Notes

- Use EF Core with PostgreSQL JSONB for EventData
- Implement `IAuditService` for consistent event capture
- Events are write-only (immutable after creation)
- Use background job for partition management
- Expose event query API for admin dashboard

## Consequences

### Positive
- Simple to implement - no domain rewrite needed
- Clear audit trail for compliance
- Can replay events for debugging
- Minimal performance impact (async writes)

### Negative
- State duplication (events + entity tables)
- Not true event sourcing (can't rebuild state from events alone)
- Storage overhead for event payloads

### Neutral
- Future migration to full event sourcing is possible
- Event log can be used to build read models later

## Related Decisions

- ADR-001: Hybrid Data Modeling (EF Core + JSONB)
- ADR-027: JSONB State Storage Strategy
- ADR-031: Audit Log Retention & Compliance

## References

- Epic 9 Story 9-1: event-log-architecture
- PostgreSQL JSONB documentation
- Event Sourcing pattern (Martin Fowler)
