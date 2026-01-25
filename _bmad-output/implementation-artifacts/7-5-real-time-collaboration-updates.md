# Story 7.5: Real-Time Collaboration Updates

Status: ready-for-dev

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a user (Sarah),
I want to see changes from other users in real-time,
so that I stay in sync with my team.

## Acceptance Criteria

**Given** multiple users are in a workflow  
**When** one user sends a message  
**Then** all other connected users see the message within 500ms via SignalR

**Given** a user makes a decision  
**When** the decision is confirmed  
**Then** all users receive a DECISION_MADE event  
**And** their UIs update to reflect the decision

**Given** a workflow step advances  
**When** the step changes  
**Then** all users receive a STEP_CHANGED event  
**And** everyone sees the new current step

**Given** a user goes offline  
**When** their connection drops  
**Then** other users see their presence indicator change  
**And** when they reconnect, they receive all missed updates

**Given** high-frequency updates occur  
**When** many users are active  
**Then** updates are batched (50ms window) to prevent UI thrashing  
**And** the system remains responsive under load

## Tasks / Subtasks

- [ ] **Task 1: Implement real-time message broadcasting** (AC: #1)
  - [ ] Add SignalR group management for workflow participants
  - [ ] Implement message broadcast method in ChatHub
  - [ ] Add message event models (MESSAGE_RECEIVED)
  - [ ] Test message delivery latency < 500ms
  - [ ] Add message attribution (userId, displayName, timestamp)

- [ ] **Task 2: Implement decision event broadcasting** (AC: #2)
  - [ ] Create DECISION_MADE event model with decision details
  - [ ] Add broadcast method for decision events
  - [ ] Update decision service to trigger broadcasts
  - [ ] Test all participants receive decision updates
  - [ ] Include decision metadata (alternatives, confidence)

- [ ] **Task 3: Implement workflow step change events** (AC: #3)
  - [ ] Create STEP_CHANGED event model
  - [ ] Add broadcast method in workflow step executor
  - [ ] Update StepExecutor to broadcast on step transitions
  - [ ] Test synchronization across multiple clients
  - [ ] Include step metadata (stepId, stepName, status)

- [ ] **Task 4: Implement presence tracking and offline handling** (AC: #4)
  - [ ] Add presence tracking in SessionService
  - [ ] Create USER_ONLINE and USER_OFFLINE events
  - [ ] Implement connection/disconnection broadcast
  - [ ] Build missed updates queue/replay mechanism
  - [ ] Test reconnection with update replay
  - [ ] Add presence indicator data to session state

- [ ] **Task 5: Implement update batching for high-frequency scenarios** (AC: #5)
  - [ ] Create update batching service with 50ms window
  - [ ] Add update queue and batch processor
  - [ ] Implement debouncing for rapid state changes
  - [ ] Test system responsiveness under load (25+ concurrent users)
  - [ ] Monitor and log batch statistics

- [ ] **Task 6: Integration testing and performance validation**
  - [ ] Create multi-client SignalR integration tests
  - [ ] Test message broadcast latency < 500ms
  - [ ] Test presence tracking with multiple connections
  - [ ] Test update batching under high load
  - [ ] Validate NFR10 (25 concurrent users)

## Dev Notes

### Architecture Patterns

**SignalR Hub Management:**
- Use existing `ChatHub` in `src/bmadServer.ApiService/Hubs/ChatHub.cs` as foundation
- Extend with group management for workflow-based broadcasting
- Follow authorization patterns: `[Authorize]` attribute on hub
- Use ILogger<ChatHub> for structured logging with trace IDs

**Real-Time Communication Stack:**
- SignalR 8.0+ (Microsoft.AspNetCore.SignalR from NuGet)
- WebSocket transport (primary), with fallback to Server-Sent Events
- Hub methods must be async (async Task pattern)
- Use `Clients.Group(workflowId).SendAsync()` for workflow-scoped broadcasts

**State Management:**
- Presence tracking in `ISessionService` (existing service pattern)
- Event models in `src/bmadServer.ApiService/Models/` directory
- Leverage existing `WorkflowState` model for state synchronization
- Use PostgreSQL JSONB for storing missed updates queue

**Performance Considerations:**
- NFR10 requirement: Support 25 concurrent users
- Message latency target: < 500ms (AC #1)
- Update batching: 50ms window to prevent UI thrashing (AC #5)
- Use System.Threading.RateLimiting for update throttling if needed

### Critical Implementation Details

**1. Group Management (Task 1)**
```csharp
// Add user to workflow group on workflow start
await Groups.AddToGroupAsync(Context.ConnectionId, $"workflow-{workflowId}");

// Broadcast to workflow participants
await Clients.Group($"workflow-{workflowId}").SendAsync("MESSAGE_RECEIVED", messageData);
```

**2. Event Model Pattern**
All real-time events should follow consistent structure:
```csharp
public class WorkflowEvent
{
    public string EventType { get; set; } // MESSAGE_RECEIVED, DECISION_MADE, STEP_CHANGED
    public Guid WorkflowId { get; set; }
    public Guid UserId { get; set; }
    public string DisplayName { get; set; }
    public DateTime Timestamp { get; set; }
    public object Data { get; set; } // Event-specific payload
}
```

**3. Presence Tracking (Task 4)**
- Update SessionService to track connection state
- Broadcast USER_ONLINE when OnConnectedAsync fires
- Broadcast USER_OFFLINE when OnDisconnectedAsync fires
- Store last seen timestamp in session for reconnection logic

**4. Update Batching (Task 5)**
Implement batching service to aggregate rapid state changes:
```csharp
// Pseudo-code pattern
private readonly Dictionary<Guid, List<WorkflowEvent>> _pendingUpdates = new();
private readonly Timer _batchTimer; // 50ms interval

private void QueueUpdate(Guid workflowId, WorkflowEvent evt)
{
    if (!_pendingUpdates.TryGetValue(workflowId, out var events))
    {
        events = new List<WorkflowEvent>();
        _pendingUpdates[workflowId] = events;
    }

    events.Add(evt);
}

private async Task FlushBatch()
{
    foreach (var (workflowId, events) in _pendingUpdates)
    {
        await Clients.Group($"workflow-{workflowId}")
            .SendAsync("BATCH_UPDATE", events);
    }
    _pendingUpdates.Clear();
}
```

**5. Missed Updates Replay (Task 4)**
When user reconnects:
1. Load missed events from PostgreSQL (stored as JSONB array)
2. Send all missed events in order
3. Mark events as delivered
4. Resume normal real-time updates

### Source Tree Components

**Files to Create:**
- `src/bmadServer.ApiService/Models/Events/WorkflowEvent.cs` - Base event model
- `src/bmadServer.ApiService/Models/Events/MessageReceivedEvent.cs`
- `src/bmadServer.ApiService/Models/Events/DecisionMadeEvent.cs`
- `src/bmadServer.ApiService/Models/Events/StepChangedEvent.cs`
- `src/bmadServer.ApiService/Models/Events/PresenceEvent.cs`
- `src/bmadServer.ApiService/Services/UpdateBatchingService.cs`
- `src/bmadServer.ApiService/Services/PresenceTrackingService.cs`

**Files to Modify:**
- `src/bmadServer.ApiService/Hubs/ChatHub.cs` - Add group management and broadcasting
- `src/bmadServer.ApiService/Services/SessionService.cs` - Add presence tracking
- `src/bmadServer.ApiService/Services/Workflows/StepExecutor.cs` - Add STEP_CHANGED broadcasts
- `src/bmadServer.ApiService/Program.cs` - Register new services

**Test Files to Create:**
- `src/bmadServer.Tests/Integration/RealTimeCollaborationTests.cs`
- `src/bmadServer.Tests/Integration/PresenceTrackingTests.cs`
- `src/bmadServer.Tests/Integration/UpdateBatchingTests.cs`
- `src/bmadServer.BDD.Tests/Features/RealTimeCollaboration.feature`

### Project Structure Notes

**Alignment with Unified Project Structure:**
- Event models follow established pattern in `Models/` directory
- Services registered in Program.cs using AddScoped/AddSingleton pattern
- Hub follows existing ChatHub authorization and logging patterns
- Integration tests follow TestWebApplicationFactory pattern
- BDD tests use SpecFlow with Gherkin syntax

**Naming Conventions:**
- Services: Interface prefix `I` (IPresenceTrackingService)
- Events: Suffix `Event` (MessageReceivedEvent)
- Hub methods: PascalCase, async Task pattern
- SignalR client methods: UPPER_SNAKE_CASE (MESSAGE_RECEIVED)

**Database Schema:**
```sql
-- Add to WorkflowParticipants table (assumed to exist from Story 7.1)
ALTER TABLE WorkflowParticipants 
ADD COLUMN IsOnline BOOLEAN DEFAULT FALSE,
ADD COLUMN LastSeenAt TIMESTAMP,
ADD COLUMN MissedUpdates JSONB DEFAULT '[]';
```

### Testing Standards

**Integration Tests:**
- Use `TestWebApplicationFactory` for full server setup
- Use `HubConnectionBuilder` to create test clients
- Test latency with `Stopwatch` measurements
- Test multi-client scenarios (minimum 3 concurrent clients)
- Validate event payloads match expected structure

**BDD Tests (SpecFlow):**
```gherkin
Feature: Real-Time Collaboration Updates
  As a user collaborating on a workflow
  I want to see changes from other users in real-time
  So that I stay synchronized with my team

Scenario: User receives message from collaborator within 500ms
  Given user "Sarah" is connected to workflow "test-workflow-123"
  And user "Marcus" is connected to the same workflow
  When Marcus sends a message "Let's review the architecture"
  Then Sarah receives the message within 500ms
  And the message includes Marcus's display name and timestamp
```

**Performance Tests:**
- Test with 25 concurrent users (NFR10 requirement)
- Measure P50, P95, P99 latency for message delivery
- Verify update batching reduces message count under high load
- Monitor memory usage with presence tracking

### References

**Architecture:**
- [Source: _bmad-output/planning-artifacts/architecture.md#Real-time Communication]
  - SignalR 8.0+ for WebSocket communication
  - Aspire orchestration patterns
  - Structured logging with trace IDs

**PRD:**
- [Source: _bmad-output/planning-artifacts/prd.md#Success Criteria]
  - WebSocket connections stable for 30+ minutes
  - System handles concurrent workflows without cross-contamination

**Epic Context:**
- [Source: _bmad-output/planning-artifacts/epics.md#Epic 7: Collaboration & Multi-User Support]
  - Story 7.1: Multi-User Workflow Participation (presence indicators prerequisite)
  - Story 7.2: Safe Checkpoint System (decision events integration)
  - Story 7.3: Input Attribution & History (message attribution)
  - Story 7.4: Conflict Detection & Buffering (conflict event broadcasting)

**Existing Implementation:**
- [Source: src/bmadServer.ApiService/Hubs/ChatHub.cs]
  - Existing hub with authentication and session management
  - OnConnectedAsync/OnDisconnectedAsync lifecycle methods
  - SESSION_RESTORED event pattern to follow

**Technology Stack:**
- SignalR Hub: `Microsoft.AspNetCore.SignalR` (version 8.0+, included in Aspire)
- Real-time groups: Built-in SignalR groups API
- Batching: System.Threading.Timer or System.Threading.Channels
- Presence tracking: In-memory + PostgreSQL persistence

## Dev Agent Record

### Agent Model Used

claude-3-7-sonnet-20250219

### Debug Log References

(To be populated during implementation)

### Completion Notes List

(To be populated during implementation)

### File List

(To be populated during implementation)
