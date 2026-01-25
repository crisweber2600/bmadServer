# Story 4.4: Workflow Pause & Resume

**Status:** ready-for-dev

## Story

As a user (Sarah), I want to pause and resume a workflow, so that I can take breaks without losing progress.

## Acceptance Criteria

**Given** a workflow is in Running state  
**When** I send POST `/api/v1/workflows/{id}/pause`  
**Then** the workflow transitions to Paused state  
**And** a pause event is logged with timestamp and userId  
**And** I receive 200 OK with updated workflow state

**Given** a workflow is in Paused state  
**When** I send POST `/api/v1/workflows/{id}/resume`  
**Then** the workflow transitions back to Running state  
**And** execution continues from the last completed step  
**And** context is fully restored

**Given** I try to pause an already paused workflow  
**When** the request is processed  
**Then** I receive 400 Bad Request with "Workflow is already paused"

**Given** a workflow has been paused for 24+ hours  
**When** I resume the workflow  
**Then** a context refresh occurs to reload any stale data  
**And** I see a notification: "Workflow resumed. Context has been refreshed."

**Given** multiple users are in a collaborative workflow  
**When** one user pauses the workflow  
**Then** all connected users receive a SignalR notification  
**And** their UIs update to show paused state

## Tasks / Subtasks

- [ ] Extend WorkflowInstanceService with pause/resume methods (AC: 1, 2)
  - [ ] Implement PauseWorkflow(workflowId, userId) method
  - [ ] Implement ResumeWorkflow(workflowId, userId) method
  - [ ] Validate state transitions (Running→Paused, Paused→Running)
  - [ ] Log pause/resume events to WorkflowEvents table
- [ ] Add validation for duplicate pause attempts (AC: 3)
  - [ ] Check current state before transition
  - [ ] Return 400 Bad Request with clear message for invalid transitions
- [ ] Implement context refresh for long-paused workflows (AC: 4)
  - [ ] Check pause duration (CreatedAt vs current time)
  - [ ] If >24 hours, trigger context refresh
  - [ ] Add PausedAt timestamp to WorkflowInstance model (migration)
  - [ ] Return refresh notification in resume response
- [ ] Add SignalR notifications for pause/resume (AC: 5)
  - [ ] Broadcast WORKFLOW_PAUSED event to all participants
  - [ ] Broadcast WORKFLOW_RESUMED event to all participants
  - [ ] Include workflow id and updated status in event payload
- [ ] Create API endpoints
  - [ ] POST /api/v1/workflows/{id}/pause
  - [ ] POST /api/v1/workflows/{id}/resume
  - [ ] Both require authentication and workflow ownership validation
- [ ] Add unit tests
  - [ ] Test pause transition from Running state
  - [ ] Test resume transition from Paused state
  - [ ] Test duplicate pause returns 400
  - [ ] Test context refresh for 24+ hour pause
  - [ ] Test unauthorized pause/resume attempts
- [ ] Add integration tests
  - [ ] Test pause/resume via API endpoints
  - [ ] Verify SignalR notifications are sent
  - [ ] Test multi-user workflow pause scenario

## Dev Notes

### Architecture Alignment

**Source:** [ARCHITECTURE.md - Workflow State Management]

- Extend: `src/bmadServer.ApiService/Services/Workflows/WorkflowInstanceService.cs`
- Extend: `src/bmadServer.ApiService/Endpoints/WorkflowsEndpoint.cs`
- Use: `src/bmadServer.ApiService/Hubs/ChatHub.cs` for SignalR notifications
- Add PausedAt column to WorkflowInstance table (migration)

### Technical Requirements

**State Transitions:**
- Running → Paused (valid)
- Paused → Running (valid)
- Paused → Paused (invalid, return 400)
- Other states → Cannot pause (invalid)

**Context Refresh Logic:**
```csharp
var pauseDuration = DateTime.UtcNow - workflowInstance.PausedAt;
if (pauseDuration > TimeSpan.FromHours(24))
{
    await RefreshWorkflowContext(workflowInstance);
    notification = "Workflow resumed. Context has been refreshed.";
}
```

**SignalR Event Structure:**
```json
{
  "eventType": "WORKFLOW_PAUSED",
  "workflowId": "guid",
  "status": "Paused",
  "userId": "guid",
  "timestamp": "ISO8601"
}
```

### File Structure Requirements

```
src/bmadServer.ApiService/
├── Services/
│   └── Workflows/
│       └── WorkflowInstanceService.cs (extend)
├── Endpoints/
│   └── WorkflowsEndpoint.cs (add pause/resume endpoints)
└── Data/
    └── Migrations/
        └── XXX_AddPausedAtToWorkflowInstance.cs
```

### Dependencies

**From Previous Stories:**
- Story 4.2: WorkflowInstance and state machine
- Story 3.1: SignalR ChatHub (from Epic 3) - if not available, use simple notification mechanism

**Authorization:**
- User must own the workflow or be a collaborator
- Reuse authorization patterns from Stories 2.1-2.2

### Testing Requirements

**Unit Tests:** `test/bmadServer.Tests/Services/Workflows/WorkflowInstanceServiceTests.cs`

**Test Coverage:**
- Valid pause/resume transitions
- Invalid state transition errors
- Context refresh for long pauses
- Event logging

**Integration Tests:**
- API endpoint authorization
- SignalR notification delivery
- Database persistence of PausedAt timestamp

### Integration Notes

**Connection to Other Stories:**
- Story 4.3: Pause should interrupt current step execution
- Story 4.7: Status API should show Paused state
- Epic 7: Multi-user workflows receive pause notifications

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 4.4]
- [Source: ARCHITECTURE.md - State Machine, Real-time Updates]
- [Story 3.1: SignalR implementation patterns]

## Dev Agent Record

### Agent Model Used

_To be filled by dev agent_

### Debug Log References

_To be filled by dev agent_

### Completion Notes List

_To be filled by dev agent_

### File List

_To be filled by dev agent_
- Test files


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

- Source: [epics.md - Story 4.4](../planning-artifacts/epics.md)
- Architecture: [architecture.md](../planning-artifacts/architecture.md)
- PRD: [prd.md](../planning-artifacts/prd.md)
