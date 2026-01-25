# Story 7.1: Multi-User Workflow Participation

Status: ready-for-dev

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a user (Sarah), I want to invite team members to my workflow, so that we can collaborate on product development together.

## Acceptance Criteria

### AC1: Add Participant to Workflow

**Given** I own a workflow  
**When** I send POST `/api/v1/workflows/{id}/participants` with userId and role (Contributor/Observer)  
**Then** the user is added to the workflow  
**And** they receive an invitation notification  
**And** they appear in the participants list

### AC2: Contributor Access and Attribution

**Given** a user is added as Contributor  
**When** they access the workflow  
**Then** they can send messages, make decisions, and advance steps  
**And** their actions are attributed to them

### AC3: Observer Read-Only Access

**Given** a user is added as Observer  
**When** they access the workflow  
**Then** they can view messages and decisions  
**And** they cannot make changes or send messages  
**And** the UI shows read-only mode

### AC4: Real-Time Presence Indicators

**Given** multiple users are connected  
**When** I view the workflow  
**Then** I see presence indicators showing who is online  
**And** I see typing indicators when others are composing messages

### AC5: Remove Participant

**Given** I want to remove a participant  
**When** I send DELETE `/api/v1/workflows/{id}/participants/{userId}`  
**Then** the user loses access immediately  
**And** they receive a notification  
**And** their future access attempts are denied

## Tasks / Subtasks

- [ ] Database schema for workflow participants (AC: #1, #2, #3, #5)
  - [ ] Create `workflow_participants` table with userId, workflowId, role, addedAt, addedBy
  - [ ] Add index on (workflow_id, user_id) for lookup performance
  - [ ] Create EF Core migration
  
- [ ] Domain models and DTOs (AC: #1, #2, #3)
  - [ ] Create `WorkflowParticipant` entity model
  - [ ] Create `ParticipantRole` enum (Owner, Contributor, Observer)
  - [ ] Create `AddParticipantRequest` and `ParticipantResponse` DTOs
  - [ ] Add FluentValidation rules for role and userId validation

- [ ] Participant management service (AC: #1, #5)
  - [ ] Create `IParticipantService` interface
  - [ ] Implement `ParticipantService` with add/remove/list methods
  - [ ] Add authorization checks (only owner can add/remove participants)
  - [ ] Implement notification logic for invitations
  
- [ ] API endpoints in WorkflowsController (AC: #1, #5)
  - [ ] POST `/api/v1/workflows/{id}/participants` - Add participant
  - [ ] GET `/api/v1/workflows/{id}/participants` - List participants
  - [ ] DELETE `/api/v1/workflows/{id}/participants/{userId}` - Remove participant
  - [ ] Add `[Authorize]` and role validation middleware
  - [ ] Return RFC 7807 ProblemDetails on errors

- [ ] Authorization policy updates (AC: #2, #3, #5)
  - [ ] Update workflow access checks to include participants
  - [ ] Implement role-based action filtering (Contributor vs Observer)
  - [ ] Add authorization handler for workflow operations
  - [ ] Update ChatHub to check participant role before accepting messages

- [ ] Real-time presence system (AC: #4)
  - [ ] Add presence tracking in ChatHub (OnConnected/OnDisconnected)
  - [ ] Broadcast USER_JOINED and USER_LEFT events to workflow group
  - [ ] Implement typing indicator broadcast (USER_TYPING event)
  - [ ] Add SignalR group management per workflow
  - [ ] Store active connections in memory (or Redis for future scaling)

- [ ] Notification system integration (AC: #1, #5)
  - [ ] Create notification event for participant invitation
  - [ ] Create notification event for participant removal
  - [ ] Send notifications via SignalR to online users
  - [ ] Store notifications for offline users (future retrieval)

- [ ] Unit tests (AC: All)
  - [ ] ParticipantService tests (add, remove, validation)
  - [ ] Authorization policy tests
  - [ ] Role enum and validation tests
  
- [ ] Integration tests (AC: All)
  - [ ] WorkflowsController participant endpoints tests
  - [ ] ChatHub presence and typing indicator tests
  - [ ] Authorization tests for Contributor vs Observer
  - [ ] Concurrent user scenario tests

- [ ] Frontend UI components (AC: #4)
  - [ ] Participant list component with avatars and roles
  - [ ] Presence indicator badges (online/offline status)
  - [ ] Typing indicator UI component
  - [ ] Add/remove participant modal dialogs

## Dev Notes

### Critical Architecture Patterns

This story introduces **multi-user collaboration** to the workflow orchestration engine. It builds on the foundation established in Epic 4 (Workflow Orchestration) and Epic 3 (SignalR Real-Time Chat).

#### 🔒 Security and Authorization Requirements

**MANDATORY: Every participant operation requires authorization**

1. **Workflow Owner Rights** - Only workflow owner (creator) can add/remove participants
2. **Participant Role Enforcement** - Contributor vs Observer permissions enforced at API and SignalR level
3. **JWT Authentication** - All endpoints and SignalR connections require valid JWT token
4. **Authorization Policy** - Use policy-based authorization with custom handlers

**Implementation Pattern:**
```csharp
// In Program.cs
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("WorkflowOwner", policy => 
        policy.Requirements.Add(new WorkflowOwnerRequirement()));
    
builder.Services.AddSingleton<IAuthorizationHandler, WorkflowOwnerHandler>();

// In controller
[HttpPost("{id}/participants")]
[Authorize(Policy = "WorkflowOwner")]
public async Task<ActionResult> AddParticipant(Guid id, AddParticipantRequest request) { }
```

#### 🗄️ Database Schema Design

**New Table: `workflow_participants`**

```sql
CREATE TABLE workflow_participants (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    workflow_id UUID NOT NULL,
    user_id UUID NOT NULL,
    role VARCHAR(20) NOT NULL CHECK (role IN ('Owner', 'Contributor', 'Observer')),
    added_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    added_by UUID NOT NULL,
    FOREIGN KEY (workflow_id) REFERENCES workflow_instances(id) ON DELETE CASCADE,
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    FOREIGN KEY (added_by) REFERENCES users(id),
    UNIQUE (workflow_id, user_id)
);

CREATE INDEX idx_workflow_participants_workflow ON workflow_participants(workflow_id);
CREATE INDEX idx_workflow_participants_user ON workflow_participants(user_id);
```

**EF Core Model:**
```csharp
public class WorkflowParticipant
{
    public Guid Id { get; set; }
    public Guid WorkflowId { get; set; }
    public Guid UserId { get; set; }
    public ParticipantRole Role { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    public Guid AddedBy { get; set; }
    
    // Navigation properties
    public WorkflowInstance Workflow { get; set; } = null!;
    public User User { get; set; } = null!;
}

public enum ParticipantRole
{
    Owner,       // Can add/remove participants, full control
    Contributor, // Can send messages, make decisions, advance steps
    Observer     // Read-only access, cannot make changes
}
```

#### 📡 SignalR Real-Time Presence System

**Group Management Pattern:**

Each workflow has a SignalR group. When users connect:
1. Join the workflow group: `Groups.AddToGroupAsync(Context.ConnectionId, $"workflow-{workflowId}")`
2. Broadcast presence to group members
3. Track active connections in-memory or Redis

**Required SignalR Events:**
- `USER_JOINED` - Broadcast when user connects to workflow
- `USER_LEFT` - Broadcast when user disconnects
- `USER_TYPING` - Broadcast when user is composing a message
- `PARTICIPANT_ADDED` - Notify when new participant added
- `PARTICIPANT_REMOVED` - Notify when participant removed

**Implementation in ChatHub:**
```csharp
public async Task JoinWorkflow(Guid workflowId)
{
    var userId = GetUserIdFromClaims();
    
    // Verify user is a participant
    var isParticipant = await _participantService.IsParticipantAsync(workflowId, userId);
    if (!isParticipant)
        throw new UnauthorizedAccessException("Not a participant of this workflow");
    
    // Join SignalR group
    await Groups.AddToGroupAsync(Context.ConnectionId, $"workflow-{workflowId}");
    
    // Broadcast presence
    await Clients.Group($"workflow-{workflowId}").SendAsync("USER_JOINED", new {
        userId,
        userName = Context.User?.Identity?.Name,
        timestamp = DateTime.UtcNow
    });
}

public async Task SendTypingIndicator(Guid workflowId)
{
    var userId = GetUserIdFromClaims();
    await Clients.OthersInGroup($"workflow-{workflowId}").SendAsync("USER_TYPING", new {
        userId,
        userName = Context.User?.Identity?.Name
    });
}
```

#### 🔄 Participant Authorization Flow

**For Every Workflow Operation:**

1. **Extract user ID** from JWT claims (ClaimTypes.NameIdentifier)
2. **Check participant role** via ParticipantService
3. **Enforce role permissions:**
   - Observer: Can only read, cannot send messages or make decisions
   - Contributor: Can send messages, make decisions, advance workflow steps
   - Owner: Full control including add/remove participants

**Example Authorization Handler:**
```csharp
public class WorkflowParticipantHandler : AuthorizationHandler<WorkflowParticipantRequirement>
{
    private readonly IParticipantService _participantService;
    
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        WorkflowParticipantRequirement requirement)
    {
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var workflowId = GetWorkflowIdFromHttpContext(); // Extract from route
        
        var participant = await _participantService.GetParticipantAsync(workflowId, userId);
        
        if (participant != null && 
            (requirement.AllowedRoles.Contains(participant.Role)))
        {
            context.Succeed(requirement);
        }
    }
}
```

#### ⚠️ Error Handling Patterns

**Use RFC 7807 ProblemDetails for all errors:**

```csharp
// User not found
return Problem(
    statusCode: 404,
    title: "User Not Found",
    detail: $"User {userId} does not exist",
    type: "https://bmadserver.api/errors/user-not-found"
);

// Not authorized (not workflow owner)
return Problem(
    statusCode: 403,
    title: "Forbidden",
    detail: "Only workflow owner can add participants",
    type: "https://bmadserver.api/errors/forbidden"
);

// Participant already exists
return Problem(
    statusCode: 409,
    title: "Participant Already Exists",
    detail: $"User {userId} is already a participant",
    type: "https://bmadserver.api/errors/participant-exists"
);
```

### Project Structure Notes

This story follows the **established Aspire + Clean Architecture pattern**:

```
src/bmadServer.ApiService/
├── Controllers/
│   └── WorkflowsController.cs          # Add participants endpoints
├── Services/
│   └── ParticipantService.cs           # NEW: Participant management
├── Models/
│   └── WorkflowParticipant.cs          # NEW: Participant entity
├── DTOs/
│   ├── AddParticipantRequest.cs        # NEW: Request DTO
│   └── ParticipantResponse.cs          # NEW: Response DTO
├── Validators/
│   └── AddParticipantValidator.cs      # NEW: FluentValidation rules
├── Hubs/
│   └── ChatHub.cs                      # UPDATE: Add presence tracking
├── Migrations/
│   └── YYYYMMDDHHMMSS_AddParticipants.cs  # NEW: EF Core migration
└── Data/
    └── ApplicationDbContext.cs         # UPDATE: Add DbSet<WorkflowParticipant>

src/bmadServer.Tests/
├── Unit/
│   └── ParticipantServiceTests.cs      # NEW: Unit tests
└── Integration/
    └── WorkflowParticipantsTests.cs    # NEW: Integration tests
```

### Testing Requirements

#### Unit Tests (Minimum Coverage)

1. **ParticipantService Tests:**
   - Add participant (valid role, duplicate detection)
   - Remove participant (owner check, participant exists)
   - List participants (filter by workflow, include user details)
   - Role validation (invalid roles rejected)

2. **Authorization Tests:**
   - Owner can add/remove participants
   - Contributor cannot add/remove participants
   - Observer has read-only access

#### Integration Tests (End-to-End Scenarios)

1. **API Endpoint Tests:**
   - POST /participants returns 201 with participant details
   - GET /participants returns paginated list
   - DELETE /participants returns 204 and user loses access
   - Authorization failures return 403 ProblemDetails

2. **SignalR Hub Tests:**
   - User joins workflow and receives USER_JOINED event
   - Typing indicator broadcasts to other group members
   - Observer cannot send messages through hub

3. **Concurrent User Tests:**
   - Multiple users can join same workflow
   - Presence indicators update correctly
   - Messages attributed to correct user

### Dependencies and Integration Points

**This story depends on:**
- ✅ Epic 2 (Authentication) - JWT token validation
- ✅ Epic 3 (SignalR Chat) - Real-time communication infrastructure
- ✅ Epic 4 (Workflow Orchestration) - WorkflowInstance and WorkflowInstanceService
- ✅ Story 2.1 (User Registration) - User entity exists

**This story enables:**
- 🔜 Story 7.2 (Safe Checkpoint System) - Multi-user input buffering
- 🔜 Story 7.3 (Input Attribution & History) - Track who made each contribution
- 🔜 Story 7.4 (Conflict Detection & Buffering) - Detect conflicting inputs from multiple users
- 🔜 Story 7.5 (Real-Time Collaboration Updates) - All users see changes in real-time

### Known Constraints and Limitations

1. **Concurrency Model:** Single-tenant MVP means no cross-tenant isolation needed yet
2. **Scalability:** In-memory presence tracking sufficient for MVP (25 concurrent users). Use Redis for Phase 2.
3. **Notification Delivery:** SignalR for online users only. Offline notification queue is Phase 2.
4. **Presence Timeout:** 60-second disconnect grace period (matches session recovery window from Story 2.4)

### References

- **Epic 7 Context:** [epics.md Lines 2048-2095](../planning-artifacts/epics.md)
- **Architecture - ADR-002:** [architecture.md Lines 355-407](../planning-artifacts/architecture.md) (Agent Router pattern applicable to participant routing)
- **Architecture - Security Layers:** [architecture.md Lines 786-799](../planning-artifacts/architecture.md)
- **PRD - Multi-User Collaboration:** [prd.md](../planning-artifacts/prd.md) (FR6-FR8)
- **Project Context - Rule 7:** [project-context-ai.md Lines 110-122](../planning-artifacts/project-context-ai.md) (Authentication mandatory)
- **Story 3.1:** [3-1-signalr-hub-setup-websocket-connection.md](./3-1-signalr-hub-setup-websocket-connection.md) (SignalR foundation)
- **Story 4.2:** [4-2-workflow-instance-creation-state-machine.md](./4-2-workflow-instance-creation-state-machine.md) (Workflow instance model)

## Dev Agent Record

### Agent Model Used

claude-3-7-sonnet-20250219

### Debug Log References

_(To be filled during implementation)_

### Completion Notes List

_(To be filled during implementation)_

### File List

_(To be filled during implementation)_
