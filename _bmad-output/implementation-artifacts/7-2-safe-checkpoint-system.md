# Story 7.2: Safe Checkpoint System

Status: ready-for-dev

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a user (Marcus), I want inputs to be applied at safe checkpoints, so that workflow integrity is maintained even with concurrent contributions.

## Acceptance Criteria

### AC1: Input Queuing at Checkpoints

**Given** a workflow step is in progress  
**When** I submit an input  
**Then** the input is queued until the current step reaches a checkpoint  
**And** I see "Input queued - will be applied at next checkpoint"

### AC2: Checkpoint Processing (FIFO)

**Given** a checkpoint is reached  
**When** queued inputs are processed  
**Then** inputs are applied in order received (FIFO)  
**And** each input is validated before application  
**And** invalid inputs are rejected with feedback

### AC3: Checkpoint Definition

**Given** I check the checkpoint definition  
**When** I examine the workflow step  
**Then** checkpoints are defined at: step completion, decision confirmation, agent handoff, explicit save points

### AC4: Rollback on Failure

**Given** a step fails after accepting inputs  
**When** rollback occurs  
**Then** the state reverts to the last successful checkpoint  
**And** queued inputs are preserved for retry  
**And** users are notified of the rollback

### AC5: Checkpoint History Query

**Given** I query checkpoint history  
**When** I send GET `/api/v1/workflows/{id}/checkpoints`  
**Then** I see all checkpoints with: timestamp, stepId, state snapshot reference, triggeredBy

## Tasks / Subtasks

- [ ] Database schema for checkpoints and input queue (AC: #1, #2, #4, #5)
  - [ ] Create `workflow_checkpoints` table with workflow_id, step_id, state_snapshot, version, created_at, triggered_by
  - [ ] Create `queued_inputs` table with workflow_id, user_id, input_type, content, queued_at, processed_at, status
  - [ ] Add indexes on (workflow_id, created_at) for checkpoint retrieval
  - [ ] Add index on (workflow_id, status) for queued input processing
  - [ ] Create EF Core migration
  
- [ ] Domain models and DTOs (AC: #1, #2, #5)
  - [ ] Create `WorkflowCheckpoint` entity model
  - [ ] Create `QueuedInput` entity model
  - [ ] Create `InputStatus` enum (Queued, Processed, Rejected, Failed)
  - [ ] Create `CheckpointType` enum (StepCompletion, DecisionConfirmation, AgentHandoff, ExplicitSave)
  - [ ] Create `QueueInputRequest` and `CheckpointResponse` DTOs
  - [ ] Add FluentValidation rules for input queuing

- [ ] Checkpoint service (AC: #1, #2, #3, #4, #5)
  - [ ] Create `ICheckpointService` interface
  - [ ] Implement `CheckpointService` with create/restore/list methods
  - [ ] Implement checkpoint creation at defined trigger points
  - [ ] Implement state snapshot capture using JSONB
  - [ ] Implement rollback logic to restore from checkpoint
  - [ ] Add transaction handling for checkpoint + state updates
  
- [ ] Input queue service (AC: #1, #2, #4)
  - [ ] Create `IInputQueueService` interface
  - [ ] Implement `InputQueueService` with enqueue/process/reject methods
  - [ ] Implement FIFO processing logic
  - [ ] Implement input validation before application
  - [ ] Handle invalid inputs with appropriate error responses
  - [ ] Preserve queued inputs on rollback
  
- [ ] Checkpoint API endpoints (AC: #5)
  - [ ] GET `/api/v1/workflows/{id}/checkpoints` - List checkpoints with pagination
  - [ ] GET `/api/v1/workflows/{id}/checkpoints/{checkpointId}` - Get checkpoint details
  - [ ] POST `/api/v1/workflows/{id}/checkpoints` - Create explicit checkpoint (manual save point)
  - [ ] POST `/api/v1/workflows/{id}/restore/{checkpointId}` - Restore from checkpoint
  - [ ] Add `[Authorize]` and participant validation
  - [ ] Return RFC 7807 ProblemDetails on errors

- [ ] Workflow orchestrator integration (AC: #1, #2, #3)
  - [ ] Update WorkflowOrchestrator to detect checkpoint triggers
  - [ ] Implement automatic checkpoint creation at step completion
  - [ ] Implement automatic checkpoint creation at decision confirmation
  - [ ] Implement automatic checkpoint creation at agent handoff
  - [ ] Process queued inputs when checkpoint is reached
  - [ ] Update workflow state machine to handle checkpoint lifecycle

- [ ] SignalR event broadcasting (AC: #1, #4)
  - [ ] Broadcast INPUT_QUEUED event when input is queued
  - [ ] Broadcast CHECKPOINT_REACHED event when checkpoint created
  - [ ] Broadcast INPUT_PROCESSED event when queued input is applied
  - [ ] Broadcast ROLLBACK_OCCURRED event when state reverts
  - [ ] Update ChatHub to send real-time notifications to all participants

- [ ] Transaction and rollback management (AC: #4)
  - [ ] Implement database transaction wrapper for checkpoint operations
  - [ ] Implement rollback logic to revert to last checkpoint
  - [ ] Ensure queued inputs are preserved during rollback
  - [ ] Add logging for all rollback events
  - [ ] Send notifications to affected users

- [ ] Unit tests (AC: All)
  - [ ] CheckpointService tests (create, restore, list)
  - [ ] InputQueueService tests (enqueue, process FIFO, validation, rejection)
  - [ ] Rollback logic tests
  - [ ] Checkpoint trigger detection tests
  
- [ ] Integration tests (AC: All)
  - [ ] Checkpoint API endpoint tests
  - [ ] Input queuing workflow tests
  - [ ] FIFO processing verification tests
  - [ ] Rollback and recovery tests
  - [ ] Concurrent input queuing tests
  - [ ] Checkpoint history pagination tests

## Dev Notes

### Critical Architecture Patterns

This story implements the **Safe Checkpoint System** that ensures workflow integrity during multi-user collaboration. It builds upon:
- ✅ Story 7.1 (Multi-User Workflow Participation) - Multiple users can now safely contribute inputs
- ✅ Epic 4 (Workflow Orchestration Engine) - Workflow state machine foundation
- ✅ ADR-001 (Hybrid Document Store + Event Log) - State persistence with audit trail

#### 🔒 Core Checkpoint Principles

**WHY CHECKPOINTS ARE CRITICAL:**

1. **Workflow Integrity** - Prevents partial state corruption from concurrent inputs
2. **Rollback Safety** - Known-good states to revert to on failure
3. **Input Buffering** - Queues inputs during critical operations
4. **Audit Trail** - Complete history of all state changes
5. **Collaborative Safety** - Multiple users can contribute without conflicts

**Checkpoint Strategy:**

```
Normal Flow:
  User submits input → Input queued → Step continues processing → 
  Checkpoint reached → Process all queued inputs (FIFO) → 
  Validate each → Apply to state → Save checkpoint → Continue

Failure Flow:
  User submits input → Input queued → Step processing → 
  ERROR OCCURS → Rollback to last checkpoint → 
  Preserve queued inputs → Notify users → Retry or escalate
```

For complete implementation details, database schemas, code examples, and testing requirements, see the full story document.

### Project Structure Notes

This story follows the **established Aspire + Clean Architecture pattern**.

### Dependencies and Integration Points

**This story depends on:**
- ✅ Story 7.1 (Multi-User Workflow Participation)
- ✅ Epic 4 (Workflow Orchestration Engine)
- ✅ Epic 2 (Authentication)
- ✅ ADR-001 (Hybrid Document Store + Event Log)

**This story enables:**
- 🔜 Story 7.3 (Input Attribution & History)
- 🔜 Story 7.4 (Conflict Detection & Buffering)
- 🔜 Story 9.5 (Checkpoint Restoration)

### References

- **Epic 7 Context:** [epics.md Lines 2097-2131](../planning-artifacts/epics.md)
- **Architecture - ADR-001:** [architecture.md Lines 317-351](../planning-artifacts/architecture.md)
- **Project Context - Concurrency Control:** [project-context-ai.md Lines 30-49](../planning-artifacts/project-context-ai.md)
- **Story 7.1:** [7-1-multi-user-workflow-participation.md](./7-1-multi-user-workflow-participation.md)

## Dev Agent Record

### Agent Model Used

claude-3-7-sonnet-20250219

### Debug Log References

_(To be filled during implementation)_

### Completion Notes List

_(To be filled during implementation)_

### File List

_(To be filled during implementation)_


### Detailed Implementation Guidance

#### Database Schema Details

**Table: workflow_checkpoints**
```sql
CREATE TABLE workflow_checkpoints (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    workflow_id UUID NOT NULL,
    step_id VARCHAR(100) NOT NULL,
    checkpoint_type VARCHAR(50) NOT NULL CHECK (checkpoint_type IN ('StepCompletion', 'DecisionConfirmation', 'AgentHandoff', 'ExplicitSave')),
    state_snapshot JSONB NOT NULL,
    version BIGINT NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    triggered_by UUID NOT NULL,
    metadata JSONB,
    FOREIGN KEY (workflow_id) REFERENCES workflow_instances(id) ON DELETE CASCADE,
    FOREIGN KEY (triggered_by) REFERENCES users(id)
);

CREATE INDEX idx_checkpoints_workflow_time ON workflow_checkpoints(workflow_id, created_at DESC);
CREATE INDEX idx_checkpoints_version ON workflow_checkpoints(workflow_id, version);
```

**Table: queued_inputs**
```sql
CREATE TABLE queued_inputs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    workflow_id UUID NOT NULL,
    user_id UUID NOT NULL,
    input_type VARCHAR(50) NOT NULL,
    content JSONB NOT NULL,
    queued_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    processed_at TIMESTAMPTZ,
    status VARCHAR(20) NOT NULL DEFAULT 'Queued' CHECK (status IN ('Queued', 'Processed', 'Rejected', 'Failed')),
    rejection_reason TEXT,
    sequence_number BIGSERIAL,
    FOREIGN KEY (workflow_id) REFERENCES workflow_instances(id) ON DELETE CASCADE,
    FOREIGN KEY (user_id) REFERENCES users(id)
);

CREATE INDEX idx_queued_inputs_workflow_status ON queued_inputs(workflow_id, status, sequence_number);
CREATE INDEX idx_queued_inputs_user ON queued_inputs(user_id);
```

#### EF Core Entity Models

```csharp
public class WorkflowCheckpoint
{
    public Guid Id { get; set; }
    public Guid WorkflowId { get; set; }
    public string StepId { get; set; } = string.Empty;
    public CheckpointType CheckpointType { get; set; }
    public JsonDocument StateSnapshot { get; set; } = null!;
    public long Version { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid TriggeredBy { get; set; }
    public JsonDocument? Metadata { get; set; }
    
    // Navigation properties
    public WorkflowInstance Workflow { get; set; } = null!;
    public User TriggeredByUser { get; set; } = null!;
}

public enum CheckpointType
{
    StepCompletion,       // Automatic: when workflow step completes
    DecisionConfirmation, // Automatic: when decision is confirmed
    AgentHandoff,         // Automatic: when agent hands off to another agent
    ExplicitSave          // Manual: user-initiated checkpoint
}

public class QueuedInput
{
    public Guid Id { get; set; }
    public Guid WorkflowId { get; set; }
    public Guid UserId { get; set; }
    public string InputType { get; set; } = string.Empty;
    public JsonDocument Content { get; set; } = null!;
    public DateTime QueuedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public InputStatus Status { get; set; } = InputStatus.Queued;
    public string? RejectionReason { get; set; }
    public long SequenceNumber { get; set; }
    
    // Navigation properties
    public WorkflowInstance Workflow { get; set; } = null!;
    public User User { get; set; } = null!;
}

public enum InputStatus
{
    Queued,    // Waiting to be processed
    Processed, // Successfully applied
    Rejected,  // Failed validation
    Failed     // Processing error
}
```

#### Service Interface Definitions

```csharp
public interface ICheckpointService
{
    Task<WorkflowCheckpoint> CreateCheckpointAsync(
        Guid workflowId, string stepId, CheckpointType type, 
        Guid triggeredBy, CancellationToken cancellationToken = default);
    
    Task RestoreCheckpointAsync(
        Guid workflowId, Guid checkpointId, 
        CancellationToken cancellationToken = default);
    
    Task<PagedResult<WorkflowCheckpoint>> GetCheckpointsAsync(
        Guid workflowId, int page = 1, int pageSize = 20, 
        CancellationToken cancellationToken = default);
    
    Task<WorkflowCheckpoint?> GetLatestCheckpointAsync(
        Guid workflowId, CancellationToken cancellationToken = default);
}

public interface IInputQueueService
{
    Task<QueuedInput> EnqueueInputAsync(
        Guid workflowId, Guid userId, string inputType, 
        JsonDocument content, CancellationToken cancellationToken = default);
    
    Task<InputProcessingResult> ProcessQueuedInputsAsync(
        Guid workflowId, CancellationToken cancellationToken = default);
    
    Task<List<QueuedInput>> GetQueuedInputsAsync(
        Guid workflowId, CancellationToken cancellationToken = default);
}
```

#### SignalR Real-Time Events

**Events to implement in ChatHub:**
- `INPUT_QUEUED` - Broadcast when user submits input during active step
- `CHECKPOINT_REACHED` - Broadcast when checkpoint is created
- `INPUT_PROCESSED` - Broadcast when queued input is successfully applied
- `INPUT_REJECTED` - Send to user when input fails validation
- `ROLLBACK_OCCURRED` - Broadcast when workflow reverts to checkpoint

#### Critical Implementation Rules

**MUST-FOLLOW:**
1. Always use database transactions for checkpoint + state updates
2. Never auto-merge inputs - all must be explicitly validated
3. Preserve queued inputs on rollback (they may be valid after retry)
4. FIFO ordering is critical - use sequence_number with BIGSERIAL
5. Increment workflow version after each checkpoint
6. Log all checkpoint and input processing events for audit trail

**Error Handling with RFC 7807 ProblemDetails:**
```csharp
// Example: Checkpoint not found
return Problem(
    statusCode: 404,
    title: "Checkpoint Not Found",
    detail: $"Checkpoint {checkpointId} does not exist",
    type: "https://bmadserver.api/errors/checkpoint-not-found"
);
```

#### Performance Considerations

- Balance checkpoint frequency vs performance (don't checkpoint every input)
- JSONB state snapshots can grow large (consider compression in Phase 2)
- Process inputs async if queue exceeds 50 items
- Ensure proper indexes on (workflow_id, status, sequence_number)

### Testing Strategy

**Unit Tests Must Cover:**
- Checkpoint creation with state snapshot
- Rollback to checkpoint
- FIFO input processing
- Input validation and rejection
- Concurrent input queuing

**Integration Tests Must Cover:**
- End-to-end checkpoint API workflows
- Input queuing during active workflow
- Multi-user concurrent input scenarios
- Rollback and recovery workflows

### Known Limitations

1. Checkpoint storage size can grow (Phase 2: compression)
2. Large queues may slow processing (Phase 2: async processing)
3. Rollback only to last checkpoint (not arbitrary point in time)
4. Optimistic locking for MVP (pessimistic may be needed in Phase 2)
