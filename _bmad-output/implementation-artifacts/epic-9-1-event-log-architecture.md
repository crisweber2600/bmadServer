# Story 9.1: Event Log Architecture

**Story ID:** E9-S1  
**Epic:** Epic 9 - Data Persistence & State Management  
**Points:** 8  
**Priority:** HIGH  
**ADR Reference:** [ADR-026: Event Log Architecture](../planning-artifacts/adr/adr-026-event-log-architecture.md)

## User Story

As a system operator, I want a comprehensive audit trail of all workflow operations, so that I can debug issues, ensure compliance, and reconstruct system state.

## Acceptance Criteria

**Given** a workflow operation occurs (workflow started, decision locked, etc.)  
**When** the operation completes  
**Then** an immutable audit event is written to the event log  
**And** the event includes: timestamp, event type, entity ID, user ID, and JSONB payload

**Given** I query the audit event API  
**When** I filter by entity ID  
**Then** I receive all events for that entity in chronological order  
**And** each event contains the complete context at the time it occurred

**Given** audit events are 90 days old  
**When** the archival job runs  
**Then** events are moved to the archive table  
**And** query performance for recent events remains fast

## Tasks

- [ ] Create `AuditEvent` entity with JSONB EventData column
- [ ] Add DbSet to ApplicationDbContext
- [ ] Create migration for audit_events table with GIN index
- [ ] Implement `IAuditService` interface
- [ ] Implement event capture for workflow operations
- [ ] Implement event capture for decision operations
- [ ] Implement event capture for session operations
- [ ] Create monthly partition management background job
- [ ] Add audit event query API endpoint `GET /api/v1/admin/audit-events`
- [ ] Add filtering by entity ID, user ID, event type, date range
- [ ] Add unit tests for IAuditService
- [ ] Add integration tests for event capture
- [ ] Update admin dashboard to display audit events

## Files to Create

- `src/bmadServer.ApiService/Data/Entities/AuditEvent.cs`
- `src/bmadServer.ApiService/Services/AuditService.cs`
- `src/bmadServer.ApiService/Controllers/AuditEventsController.cs`
- `src/bmadServer.ApiService/Data/Migrations/YYYYMMDDHHMMSS_AddAuditEvents.cs`
- `src/bmadServer.ApiService/Jobs/AuditPartitionManagementJob.cs`

## Files to Modify

- `src/bmadServer.ApiService/Data/ApplicationDbContext.cs` - Add AuditEvents DbSet
- `src/bmadServer.ApiService/Program.cs` - Register IAuditService
- `src/bmadServer.ApiService/Services/Workflows/WorkflowOrchestrator.cs` - Add event logging
- `src/bmadServer.ApiService/Services/DecisionService.cs` - Add event logging
- `src/bmadServer.ApiService/Services/SessionService.cs` - Add event logging

## Testing Checklist

- [ ] Unit test: Event capture with correct EventData
- [ ] Unit test: CorrelationId linking for distributed tracing
- [ ] Integration test: Workflow start logs WorkflowStarted event
- [ ] Integration test: Decision lock logs DecisionLocked event
- [ ] Integration test: Query events by entity ID returns correct results
- [ ] Integration test: Partition management creates monthly partitions
- [ ] Performance test: Event writes don't block main operations
- [ ] Performance test: Query performance with 100K+ events

## Definition of Done

- [ ] All acceptance criteria met
- [ ] All tasks completed
- [ ] All tests passing (unit + integration)
- [ ] Code review completed
- [ ] Documentation updated
- [ ] ADR-026 implementation verified
- [ ] Story demonstrated to stakeholders
- [ ] Merged to main branch

## Notes

- Event log is append-only and immutable
- Use async event writing to avoid blocking
- Consider using background queue for event persistence
- GIN index on EventData enables fast JSONB queries
- Partition management prevents table bloat
