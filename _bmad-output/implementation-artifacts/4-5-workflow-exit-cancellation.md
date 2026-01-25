# Story 4.5: Workflow Exit & Cancellation

**Status:** done

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

- [x] Extend WorkflowInstanceService with cancel method (AC: 1)
  - [x] Implement CancelWorkflow(workflowId, userId) method
  - [x] Validate workflow is in cancellable state (Running, Paused, WaitingForInput)
  - [x] Transition to Cancelled state
  - [x] Terminate any pending operations (coordinate with StepExecutor from Story 4.3)
  - [x] Log cancellation event to WorkflowEvents
- [x] Add validation to prevent cancelling completed workflows (AC: 2, 3)
  - [x] Check if status is Completed, Failed, or already Cancelled
  - [x] Return 400 Bad Request with appropriate message
- [x] Ensure workflow history preservation (AC: 2)
  - [x] Cancelled workflows remain in database (soft delete pattern)
  - [x] Add CancelledAt timestamp to WorkflowInstance
  - [x] All WorkflowStepHistory records remain accessible
- [x] Prevent resuming cancelled workflows (AC: 2)
  - [x] Update ResumeWorkflow validation from Story 4.4
  - [x] Return 400 Bad Request if workflow is Cancelled
- [x] Add UI support for cancelled workflows (AC: 4)
  - [x] Update workflow list endpoint to include cancelled status
  - [x] Add filter query parameter: ?showCancelled=true/false
  - [x] Return display metadata for strikethrough/badge styling
- [x] Create API endpoint: POST /api/v1/workflows/{id}/cancel
  - [x] Require authentication and workflow ownership
  - [x] Return 200 OK with updated workflow state
- [x] Add unit tests
  - [x] Test cancel from each valid state (Running, Paused, WaitingForInput)
  - [x] Test cannot cancel Completed workflow
  - [x] Test cannot resume Cancelled workflow
  - [x] Test unauthorized cancel attempts
- [x] Add integration tests
  - [x] End-to-end cancel via API
  - [x] Verify workflow history preservation
  - [x] Test filter functionality for cancelled workflows

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

Claude 3.5 Sonnet (GitHub Copilot CLI)

### Debug Log References

N/A - All tests passed on first run after fixing state transition definitions.

### Completion Notes List

1. **Database Migration**: Created migration `20260125130309_AddCancelledAtToWorkflowInstance` to add the `CancelledAt` nullable timestamp column to `workflow_instances` table with index.

2. **State Transitions**: Updated `WorkflowStatus.cs` to allow Running → Cancelled transitions in addition to the existing Paused, WaitingForInput, and WaitingForApproval → Cancelled transitions.

3. **Service Implementation**: 
   - Implemented `CancelWorkflowAsync` in `WorkflowInstanceService` with proper validation
   - Prevents cancellation of Completed, Failed, or already Cancelled workflows
   - Logs WorkflowCancelled events
   - Updated `ResumeWorkflowAsync` to reject cancelled workflows
   - Added `GetWorkflowInstancesAsync` with showCancelled filter

4. **API Endpoint**:
   - Added POST `/api/v1/workflows/{id}/cancel` endpoint
   - Added GET `/api/v1/workflows?showCancelled=true/false` endpoint
   - Returns WorkflowInstanceListItem with display metadata (IsCancelled, IsTerminal)
   - SignalR notification for WORKFLOW_CANCELLED event

5. **Testing**:
   - Unit tests: 23 tests in WorkflowInstanceServiceTests (all passing)
   - Integration tests: 8 tests in WorkflowCancellationIntegrationTests (all passing)
   - Total workflow tests: 214 passing
   - Coverage includes all valid/invalid state transitions, history preservation, filter functionality

### File List

**Modified:**
- `src/bmadServer.ApiService/Controllers/WorkflowsController.cs` - Added Cancel and List endpoints
- `src/bmadServer.ApiService/Data/ApplicationDbContext.cs` - Added CancelledAt index
- `src/bmadServer.ApiService/Models/Workflows/WorkflowInstance.cs` - Added CancelledAt property
- `src/bmadServer.ApiService/Models/Workflows/WorkflowStatus.cs` - Updated valid transitions
- `src/bmadServer.ApiService/Services/Workflows/IWorkflowInstanceService.cs` - Added interface methods
- `src/bmadServer.ApiService/Services/Workflows/WorkflowInstanceService.cs` - Implemented cancel and list logic
- `src/bmadServer.Tests/Unit/Services/Workflows/WorkflowInstanceServiceTests.cs` - Added cancel tests

**Created:**
- `src/bmadServer.ApiService/Migrations/20260125130309_AddCancelledAtToWorkflowInstance.cs` - Migration
- `src/bmadServer.ApiService/Migrations/20260125130309_AddCancelledAtToWorkflowInstance.Designer.cs` - Migration designer
- `src/bmadServer.Tests/Integration/Workflows/WorkflowCancellationIntegrationTests.cs` - Integration tests

### File List

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
