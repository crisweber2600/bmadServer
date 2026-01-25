# Story 4.2: Workflow Instance Creation & State Machine

**Status:** ready-for-dev

## Story

As a user (Marcus), I want to start a new workflow instance, so that I can begin a BMAD process like creating a PRD.

## Acceptance Criteria

**Given** I am authenticated with Participant role  
**When** I send POST `/api/v1/workflows` with workflowId and initial parameters  
**Then** the system creates a WorkflowInstance record with: Id, WorkflowDefinitionId, UserId, CurrentStep, Status (Created), CreatedAt

**Given** a workflow instance is created  
**When** I examine the state machine  
**Then** valid states include: Created, Running, Paused, WaitingForInput, WaitingForApproval, Completed, Failed, Cancelled

**Given** a workflow instance exists  
**When** state transitions occur  
**Then** only valid transitions are allowed (e.g., Created->Running, Running->Paused, not Created->Completed)  
**And** invalid transitions return 400 Bad Request with explanation

**Given** a workflow starts  
**When** the first step executes  
**Then** Status changes from Created to Running  
**And** CurrentStep is set to step 1  
**And** an event is logged to the WorkflowEvents table

**Given** I check the database schema  
**When** I run the migration for WorkflowInstances  
**Then** the table includes JSONB columns for StepData and Context with proper indexes

## Tasks / Subtasks

- [ ] Create WorkflowInstance entity model (AC: 1, 5)
  - [ ] Add properties: Id (Guid), WorkflowDefinitionId (string), UserId (string)
  - [ ] Add CurrentStep (int), Status (enum), CreatedAt (DateTime)
  - [ ] Add StepData (JSONB), Context (JSONB)
  - [ ] Configure EF Core entity with proper JSONB mapping
- [ ] Create WorkflowStatus enum with state machine (AC: 2, 3)
  - [ ] Define states: Created, Running, Paused, WaitingForInput, WaitingForApproval, Completed, Failed, Cancelled
  - [ ] Implement ValidateTransition method to enforce state machine rules
  - [ ] Document valid transitions in code comments
- [ ] Create WorkflowEvents entity for audit log (AC: 4)
  - [ ] Properties: Id, WorkflowInstanceId, EventType, OldStatus, NewStatus, Timestamp, UserId
- [ ] Create database migration for WorkflowInstances and WorkflowEvents tables (AC: 5)
  - [ ] Include JSONB columns with GIN indexes for performance
  - [ ] Add foreign key to Users table
  - [ ] Add indexes on Status and CreatedAt
- [ ] Create WorkflowInstanceService (AC: 1, 3, 4)
  - [ ] Implement CreateWorkflowInstance method
  - [ ] Implement StartWorkflow method (Created -> Running transition)
  - [ ] Add state transition validation
  - [ ] Log all state changes to WorkflowEvents
- [ ] Create POST /api/v1/workflows endpoint (AC: 1)
  - [ ] Validate workflowId exists in WorkflowRegistry (from Story 4.1)
  - [ ] Require authentication and Participant role
  - [ ] Return created WorkflowInstance with 201 status
- [ ] Add unit tests for state machine logic
  - [ ] Test all valid state transitions
  - [ ] Test invalid transitions return proper errors
  - [ ] Test event logging occurs for each transition
- [ ] Add integration tests
  - [ ] Test workflow creation via API endpoint
  - [ ] Verify database persistence of JSONB data
  - [ ] Test authorization (requires Participant role)

## Dev Notes

### Architecture Alignment

**Source:** [ARCHITECTURE.md - Workflow Orchestration Engine, Database Schema]

- Entity models: `src/bmadServer.ApiService/Models/Workflows/WorkflowInstance.cs`
- Service layer: `src/bmadServer.ApiService/Services/Workflows/WorkflowInstanceService.cs`
- API endpoint: `src/bmadServer.ApiService/Endpoints/WorkflowsEndpoint.cs`
- Use Entity Framework Core with Npgsql for PostgreSQL JSONB support
- Follow RESTful API conventions established in Stories 2.1-2.2

### Technical Requirements

**Framework Stack:**
- .NET 8 with Aspire
- Entity Framework Core 8 with Npgsql.EntityFrameworkCore.PostgreSQL
- PostgreSQL JSONB for flexible StepData and Context storage

**Database Schema:**
```sql
CREATE TABLE workflow_instances (
    id UUID PRIMARY KEY,
    workflow_definition_id VARCHAR(100) NOT NULL,
    user_id UUID NOT NULL REFERENCES users(id),
    current_step INT NOT NULL DEFAULT 0,
    status VARCHAR(50) NOT NULL,
    step_data JSONB,
    context JSONB,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_workflow_instances_status ON workflow_instances(status);
CREATE INDEX idx_workflow_instances_user ON workflow_instances(user_id);
CREATE INDEX idx_workflow_instances_step_data ON workflow_instances USING GIN(step_data);
```

**State Machine Rules:**
- Created → Running (first step starts)
- Running → Paused, WaitingForInput, WaitingForApproval, Completed, Failed
- Paused → Running, Cancelled
- WaitingForInput → Running, Cancelled
- WaitingForApproval → Running, Cancelled
- Completed/Failed/Cancelled → Terminal (no transitions)

### File Structure Requirements

```
src/bmadServer.ApiService/
├── Models/
│   └── Workflows/
│       ├── WorkflowInstance.cs
│       ├── WorkflowStatus.cs (enum)
│       └── WorkflowEvent.cs
├── Services/
│   └── Workflows/
│       ├── IWorkflowInstanceService.cs
│       └── WorkflowInstanceService.cs
├── Endpoints/
│   └── WorkflowsEndpoint.cs
└── Data/
    └── Migrations/
        └── XXX_CreateWorkflowTables.cs

test/bmadServer.Tests/
└── Services/
    └── Workflows/
        ├── WorkflowInstanceServiceTests.cs
        └── WorkflowStateMachineTests.cs
```

### Dependencies

**From Story 4.1:**
- WorkflowRegistry to validate workflowId
- WorkflowDefinition model

**NuGet Packages:**
- Npgsql.EntityFrameworkCore.PostgreSQL (already in project from Story 1.2)

### Testing Requirements

**Unit Tests Location:** `test/bmadServer.Tests/Services/Workflows/`

**Test Coverage:**
- State machine transition validation (all valid and invalid paths)
- Event logging on state changes
- JSONB serialization/deserialization
- Error cases: invalid workflowId, unauthorized access

**Integration Tests:**
- End-to-end workflow creation via API
- Database persistence verification
- Authorization checks

### Integration Notes

**Depends On:**
- Story 4.1: WorkflowRegistry for workflow validation
- Story 2.1: User authentication and authorization

**Used By:**
- Story 4.3: Step execution will update WorkflowInstance.CurrentStep
- Story 4.4: Pause/resume will transition states
- Story 4.5: Cancellation will transition to Cancelled state
- Story 4.7: Status API will query WorkflowInstance

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 4.2]
- [Source: ARCHITECTURE.md - Database Schema, State Management]
- [Source: Stories 2.1-2.2 - Authentication patterns]

## Dev Agent Record

### Agent Model Used

_To be filled by dev agent_

### Debug Log References

_To be filled by dev agent_

### Completion Notes List

_To be filled by dev agent_

### File List

_To be filled by dev agent_
