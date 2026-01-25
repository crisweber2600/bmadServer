# Story 4.5: Workflow Exit & Cancellation

**Status:** ready-for-dev

## Story

As a user (Marcus), I want to safely exit or cancel a workflow, so that I can abandon work that's no longer needed.

## Acceptance Criteria

**Given** a workflow is in any active state (Running, Paused, WaitingForInput)  
**When** I send POST `/api/v1/workflows/{id}/cancel`  
**Then** the workflow transitions to Cancelled state  
**And** all pending operations are terminated  
**And** a cancellation event is logged

**Given** I cancel a workflow  
**When** the cancellation completes  
**Then** the workflow state is preserved for audit purposes (not deleted)  
**And** I can still view the workflow history  
**And** I cannot resume a cancelled workflow

**Given** I try to cancel a completed workflow  
**When** the request is processed  
**Then** I receive 400 Bad Request with "Cannot cancel a completed workflow"

**Given** a workflow is cancelled  
**When** I view the workflow list  
**Then** cancelled workflows are clearly marked with strikethrough or badge  
**And** I can filter to show/hide cancelled workflows

## Tasks / Subtasks

- [ ] Extend WorkflowInstanceService with cancel method (AC: 1)
  - [ ] Implement CancelWorkflow(workflowId, userId) method
  - [ ] Validate workflow is in cancellable state (Running, Paused, WaitingForInput)
  - [ ] Transition to Cancelled state
  - [ ] Terminate any pending operations (coordinate with StepExecutor from Story 4.3)
  - [ ] Log cancellation event to WorkflowEvents
- [ ] Add validation to prevent cancelling completed workflows (AC: 2, 3)
  - [ ] Check if status is Completed, Failed, or already Cancelled
  - [ ] Return 400 Bad Request with appropriate message
- [ ] Ensure workflow history preservation (AC: 2)
  - [ ] Cancelled workflows remain in database (soft delete pattern)
  - [ ] Add CancelledAt timestamp to WorkflowInstance
  - [ ] All WorkflowStepHistory records remain accessible
- [ ] Prevent resuming cancelled workflows (AC: 2)
  - [ ] Update ResumeWorkflow validation from Story 4.4
  - [ ] Return 400 Bad Request if workflow is Cancelled
- [ ] Add UI support for cancelled workflows (AC: 4)
  - [ ] Update workflow list endpoint to include cancelled status
  - [ ] Add filter query parameter: ?showCancelled=true/false
  - [ ] Return display metadata for strikethrough/badge styling
- [ ] Create API endpoint: POST /api/v1/workflows/{id}/cancel
  - [ ] Require authentication and workflow ownership
  - [ ] Return 200 OK with updated workflow state
- [ ] Add unit tests
  - [ ] Test cancel from each valid state (Running, Paused, WaitingForInput)
  - [ ] Test cannot cancel Completed workflow
  - [ ] Test cannot resume Cancelled workflow
  - [ ] Test unauthorized cancel attempts
- [ ] Add integration tests
  - [ ] End-to-end cancel via API
  - [ ] Verify workflow history preservation
  - [ ] Test filter functionality for cancelled workflows

## Dev Notes

### Architecture Alignment

**Source:** [ARCHITECTURE.MD - Workflow State Management, Soft Delete Patterns]

- Extend: `src/bmadServer.ApiService/Services/Workflows/WorkflowInstanceService.cs`
- Extend: `src/bmadServer.ApiService/Endpoints/WorkflowsEndpoint.cs`
- Add CancelledAt column to WorkflowInstance (migration)
- Use soft delete pattern: do not physically delete cancelled workflows

### Technical Requirements

**State Transition Rules:**
- Running → Cancelled (valid)
- Paused → Cancelled (valid)
- WaitingForInput → Cancelled (valid)
- Completed → Cancelled (INVALID - return 400)
- Failed → Cancelled (INVALID - already terminal)
- Cancelled → any state (INVALID - terminal state)

**Termination Logic:**
```csharp
// Coordinate with StepExecutor to stop current step
if (workflowInstance.Status == WorkflowStatus.Running)
{
    await _stepExecutor.CancelCurrentStep(workflowInstance.Id);
}
workflowInstance.Status = WorkflowStatus.Cancelled;
workflowInstance.CancelledAt = DateTime.UtcNow;
```

**Filter Implementation:**
```csharp
// GET /api/v1/workflows?showCancelled=false (default)
query = query.Where(w => w.Status != WorkflowStatus.Cancelled);
```

### File Structure Requirements

```
src/bmadServer.ApiService/
├── Services/
│   └── Workflows/
│       └── WorkflowInstanceService.cs (extend with CancelWorkflow)
├── Endpoints/
│   └── WorkflowsEndpoint.cs (add cancel endpoint, update list filter)
└── Data/
    └── Migrations/
        └── XXX_AddCancelledAtToWorkflowInstance.cs
```

### Dependencies

**From Previous Stories:**
- Story 4.2: WorkflowInstance and state machine
- Story 4.3: StepExecutor needs CancelCurrentStep method
- Story 4.4: ResumeWorkflow needs validation update

**Authorization:**
- User must own workflow or have admin role
- Reuse authorization from Stories 2.1-2.2

### Testing Requirements

**Unit Tests:** `test/bmadServer.Tests/Services/Workflows/WorkflowInstanceServiceTests.cs`

**Test Coverage:**
- Cancel from all valid states
- Prevent cancel from invalid states
- Verify CancelledAt timestamp set
- Test filter functionality
- Resume validation for cancelled workflows

### Integration Notes

**Connection to Other Stories:**
- Story 4.3: Step execution must be cancellable
- Story 4.4: Resume should reject cancelled workflows
- Story 4.7: Status API includes cancelled workflows with filter

**UI Considerations:**
- Frontend should display cancelled workflows with visual indication
- Provide clear messaging that cancelled workflows cannot be resumed
- Consider archive or cleanup job for old cancelled workflows (future epic)

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 4.5]
- [Source: ARCHITECTURE.md - State Management, Audit Patterns]
- [Soft Delete Pattern: Industry best practice for audit trails]

## Dev Agent Record

### Agent Model Used

_To be filled by dev agent_

### Debug Log References

_To be filled by dev agent_

### Completion Notes List

_To be filled by dev agent_

### File List

_To be filled by dev agent_

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

- Source: [epics.md - Story 4.5](../planning-artifacts/epics.md)
- Architecture: [architecture.md](../planning-artifacts/architecture.md)
- PRD: [prd.md](../planning-artifacts/prd.md)
