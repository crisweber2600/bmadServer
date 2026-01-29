# Story 9.2: JSONB State Storage with Concurrency Control

**Story ID:** E9-S2  
**Epic:** Epic 9 - Data Persistence & State Management  
**Points:** 8  
**Priority:** HIGH  
**ADR Reference:** [ADR-027: JSONB State Storage Strategy](../planning-artifacts/adr/adr-027-jsonb-state-storage-strategy.md)

## User Story

As a user, I want my workflow progress automatically saved and synchronized, so that I can seamlessly switch between devices or resume after disconnections without losing work.

> **Technical Implementation Note:** Workflow instances store dynamic state in JSONB columns with optimistic concurrency control, enabling workflows to evolve without schema migrations and handle concurrent updates safely.

## Acceptance Criteria

**Given** a workflow instance exists  
**When** I update the workflow state  
**Then** the state is persisted as JSONB  
**And** the Version field is incremented automatically

**Given** two users update the same workflow simultaneously  
**When** the second update attempts to save  
**Then** a DbUpdateConcurrencyException is thrown  
**And** the conflict is handled appropriately (merge or reject)

**Given** I need to query workflows by state property  
**When** I use JSONB query syntax  
**Then** the query executes efficiently using GIN indexes  
**And** results are returned in under 100ms for typical queries

## Tasks

- [ ] Add `State` JsonDocument property to WorkflowInstance
- [ ] Add `Version` int property with ConcurrencyToken attribute
- [ ] Add `StateChecksum` string property for integrity validation
- [ ] Configure EF Core for JSONB column type
- [ ] Configure Version as concurrency token
- [ ] Create migration to add State, Version, StateChecksum columns
- [ ] Create GIN index on State column
- [ ] Create expression indexes for common query paths
- [ ] Implement `IWorkflowStateService` interface
- [ ] Implement state validation before persistence
- [ ] Implement checksum calculation (SHA256)
- [ ] Implement concurrency exception handling with retry logic
- [ ] Add state query helpers for common patterns
- [ ] Create WorkflowStateDto for API responses
- [ ] Add unit tests for state serialization/deserialization
- [ ] Add unit tests for concurrency control
- [ ] Add integration tests for JSONB queries
- [ ] Add performance tests for state updates

## Files to Create

- `src/bmadServer.ApiService/Services/WorkflowStateService.cs`
- `src/bmadServer.ApiService/Models/WorkflowStateDto.cs`
- `src/bmadServer.ApiService/Data/Migrations/YYYYMMDDHHMMSS_AddWorkflowState.cs`

## Files to Modify

- `src/bmadServer.ApiService/Data/Entities/WorkflowInstance.cs` - Add State, Version, StateChecksum
- `src/bmadServer.ApiService/Data/ApplicationDbContext.cs` - Configure JSONB column
- `src/bmadServer.ApiService/Services/Workflows/WorkflowOrchestrator.cs` - Use IWorkflowStateService
- `src/bmadServer.ApiService/Controllers/WorkflowsController.cs` - Handle concurrency exceptions
- `src/bmadServer.ApiService/Program.cs` - Register IWorkflowStateService

## Testing Checklist

- [ ] Unit test: State serialization preserves complex objects
- [ ] Unit test: Version increments on each update
- [ ] Unit test: Checksum validation detects corruption
- [ ] Integration test: Concurrent updates trigger DbUpdateConcurrencyException
- [ ] Integration test: Retry logic resolves transient conflicts
- [ ] Integration test: JSONB query by currentStep returns correct workflows
- [ ] Integration test: JSONB query by nested property works
- [ ] Performance test: State update completes in under 50ms
- [ ] Performance test: JSONB query with GIN index under 100ms
- [ ] Load test: 10 concurrent state updates handled correctly

## Definition of Done

- [ ] All acceptance criteria met
- [ ] All tasks completed
- [ ] All tests passing (unit + integration + performance)
- [ ] Code review completed
- [ ] Documentation updated
- [ ] ADR-027 implementation verified
- [ ] Migration tested on staging database
- [ ] Story demonstrated to stakeholders
- [ ] Merged to main branch

## Notes

- State schema should be documented in workflow definitions
- Consider using JSON Schema for runtime validation
- Concurrency conflicts should be rare with proper UI design
- GIN indexes significantly improve JSONB query performance
- Monitor state size - consider splitting if exceeding 1MB
