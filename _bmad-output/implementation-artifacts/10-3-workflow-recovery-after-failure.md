# Story 10.3: Workflow Recovery After Failure

**Status:** ready-for-dev

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a user (Sarah), I want workflows to recover automatically after failures, so that I don't lose progress.

## Acceptance Criteria

**Given** a workflow step fails  
**When** the failure is transient (timeout, temporary service issue)  
**Then** the system automatically retries up to 3 times  
**And** each retry is logged

**Given** retries are exhausted  
**When** the step still fails  
**Then** the workflow transitions to Failed state  
**And** I receive notification: "Step failed after multiple attempts"  
**And** I can manually retry or skip (if optional)

**Given** a workflow is in Failed state  
**When** I send POST `/api/v1/workflows/{id}/recover`  
**Then** the system attempts recovery from last checkpoint  
**And** if successful, workflow resumes from safe state

**Given** the server restarts mid-workflow  
**When** the server comes back online  
**Then** incomplete workflows are detected  
**And** recovery is attempted automatically  
**And** users are notified of recovery status

**Given** recovery fails completely  
**When** manual intervention is needed  
**Then** the admin dashboard shows workflows needing attention  
**And** support can manually restore from checkpoint

## Tasks / Subtasks

- [ ] Task 1: Implement Workflow Step Retry Logic with Transient Failure Detection (AC: 1)
  - [ ] Add retry counter and state tracking to WorkflowInstance entity
  - [ ] Create transient failure detection logic (timeouts, network errors, 5xx responses)
  - [ ] Implement exponential backoff for retries (0s, 2s, 5s)
  - [ ] Add structured logging for each retry attempt with correlationId
  - [ ] Update WorkflowExecutionService to handle retries automatically
  - [ ] Add Polly resilience policies for transient failures
  - [ ] Test retry logic with simulated transient failures
- [ ] Task 2: Failed State Transition and User Notification (AC: 2)
  - [ ] Add "Failed" state to WorkflowStatus enum
  - [ ] Implement transition to Failed state after exhausted retries
  - [ ] Create notification system for workflow failures via SignalR
  - [ ] Add "WORKFLOW_STEP_FAILED" event with failure details
  - [ ] Implement manual retry and skip functionality for failed steps
  - [ ] Add skip validation (only for optional steps)
  - [ ] Update UI to show manual retry/skip options
- [ ] Task 3: Workflow Recovery API Endpoint (AC: 3)
  - [ ] Create POST `/api/v1/workflows/{id}/recover` endpoint
  - [ ] Implement checkpoint restoration logic
  - [ ] Add recovery validation (can only recover from Failed state)
  - [ ] Load last successful checkpoint from database
  - [ ] Reset workflow state to last safe point
  - [ ] Resume workflow execution from recovered state
  - [ ] Send recovery success/failure notification to client
  - [ ] Add authorization check (only workflow owner or admin)
- [ ] Task 4: Server Restart Recovery and Incomplete Workflow Detection (AC: 4)
  - [ ] Create background service for startup recovery checks
  - [ ] Query for workflows in In-Progress or Executing state on startup
  - [ ] Implement automatic recovery attempts for detected workflows
  - [ ] Add recovery status notifications via SignalR (if users connected)
  - [ ] Store recovery attempt results in audit log
  - [ ] Handle cases where users are offline during recovery
  - [ ] Add circuit breaker to prevent infinite recovery loops
- [ ] Task 5: Admin Dashboard Integration for Failed Workflow Management (AC: 5)
  - [ ] Add query for workflows requiring manual intervention
  - [ ] Create admin API endpoint: GET `/api/v1/admin/workflows/failed`
  - [ ] Add manual checkpoint restoration capability for admins
  - [ ] Implement workflow state override (admin only)
  - [ ] Add detailed failure reason display
  - [ ] Create admin action logging for audit trail
  - [ ] Test admin recovery flows end-to-end
- [ ] Task 6: Testing and Validation
  - [ ] Unit tests for retry logic and exponential backoff
  - [ ] Unit tests for state transitions (Running → Failed → Recovered)
  - [ ] Integration tests for recovery API endpoint
  - [ ] Integration tests for server restart recovery
  - [ ] BDD tests for all acceptance criteria
  - [ ] Test checkpoint restoration accuracy
  - [ ] Manual testing with network failures and timeouts
  - [ ] Load testing with concurrent workflow failures

## Dev Notes

### 🎯 CRITICAL IMPLEMENTATION REQUIREMENTS

#### Epic 10 Context: Error Handling & Recovery Foundation

This story is **Story 3 of 5** in Epic 10, building on:
- **Story 10.1 (Graceful Error Handling):** ProblemDetails RFC 7807, structured logging, correlation IDs
- **Story 10.2 (Connection Recovery):** SignalR reconnection with exponential backoff, session restoration

**Key Dependencies:**
- Use existing ProblemDetails infrastructure from Story 10.1
- Leverage SignalR connection recovery patterns from Story 10.2
- Build on existing WorkflowExecutionService from Epic 4

#### Existing Workflow Infrastructure

**CRITICAL:** bmadServer already has comprehensive workflow orchestration from Epic 4!

**Workflow Execution Service (Epic 4, Story 4.3):**
```csharp
// src/bmadServer.ApiService/Services/WorkflowExecutionService.cs
// Existing step execution and state management
public async Task<WorkflowStepResult> ExecuteStepAsync(
    Guid workflowId, 
    string stepDefinitionId, 
    Dictionary<string, object> inputs)
```

**Workflow State Machine (Epic 4, Story 4.2):**
```csharp
// src/bmadServer.ApiService/Data/Entities/WorkflowInstance.cs
public enum WorkflowStatus
{
    NotStarted,
    InProgress,
    Paused,
    Completed,
    Cancelled
    // ADD: Failed (for this story)
}
```

**Checkpoint System (Epic 4):**
- Workflow checkpoints already stored in WorkflowInstance.StateJson (JSONB)
- Session recovery already implemented via SessionService
- Event log architecture provides audit trail

#### Architecture Patterns for Recovery

**Transient Failure Detection Pattern:**
```csharp
// Use Polly for resilience policies
// NuGet: Polly 8.0+ (already used in Story 10.2 for API retries)

// Transient failures to detect:
// 1. Network timeouts (HttpClient timeout)
// 2. HTTP 5xx errors (server errors)
// 3. Database connection timeouts
// 4. Agent non-response (no activity within timeout)
// 5. SignalR disconnection during workflow step

// Pattern from Story 10.2:
builder.Services.AddHttpClient("WorkflowClient")
    .AddPolicyHandler(GetRetryPolicy())
    .AddPolicyHandler(GetCircuitBreakerPolicy());

static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError() // Handles 5xx and network errors
        .Or<TimeoutException>()
        .WaitAndRetryAsync(3, retryAttempt => 
            TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))); // 2s, 4s, 8s
}
```

**Retry Strategy:**
- Max retries: 3 attempts (as per AC)
- Backoff: Exponential (consistent with Story 10.2)
- Suggested intervals: 2s, 5s, 10s (slightly more aggressive than connection retry)
- Each retry must be logged with correlation ID

**Checkpoint Restoration:**
```csharp
// WorkflowInstance already stores state in JSONB
public class WorkflowInstance
{
    public Guid Id { get; set; }
    public WorkflowStatus Status { get; set; }
    public string StateJson { get; set; } // JSONB - stores workflow progress
    public int CurrentStepIndex { get; set; }
    public DateTime? LastCheckpointAt { get; set; }
    
    // ADD for this story:
    public int RetryCount { get; set; } = 0;
    public string? FailureReason { get; set; }
    public DateTime? FailedAt { get; set; }
    public bool RequiresManualIntervention { get; set; } = false;
}
```

**Recovery API Pattern:**
```csharp
// POST /api/v1/workflows/{id}/recover
[HttpPost("{id}/recover")]
[Authorize]
public async Task<ActionResult<WorkflowRecoveryResult>> RecoverWorkflow(Guid id)
{
    var userId = GetUserIdFromClaims();
    
    // Load workflow and validate ownership
    var workflow = await _workflowService.GetWorkflowAsync(id);
    if (workflow.UserId != userId && !User.IsInRole("Admin"))
        return Forbid();
    
    // Validate can recover (must be in Failed state)
    if (workflow.Status != WorkflowStatus.Failed)
        return BadRequest(new ProblemDetails { ... });
    
    // Attempt recovery from last checkpoint
    var result = await _workflowRecoveryService.RecoverFromCheckpointAsync(workflow);
    
    if (result.Success)
    {
        // Notify user via SignalR
        await _hubContext.Clients.User(userId.ToString())
            .SendAsync("WORKFLOW_RECOVERED", new { workflowId = id, ... });
        return Ok(result);
    }
    
    return StatusCode(500, new ProblemDetails { ... });
}
```

#### Server Restart Recovery Pattern

**Background Service for Startup Recovery:**
```csharp
// Create new: src/bmadServer.ApiService/Services/WorkflowRecoveryBackgroundService.cs
public class WorkflowRecoveryBackgroundService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait for application to fully start
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        
        // Find incomplete workflows
        var incompleteWorkflows = await _dbContext.WorkflowInstances
            .Where(w => w.Status == WorkflowStatus.InProgress 
                     || w.Status == WorkflowStatus.Executing)
            .ToListAsync(stoppingToken);
        
        _logger.LogInformation(
            "Server restart detected. Found {Count} incomplete workflows", 
            incompleteWorkflows.Count);
        
        // Attempt recovery for each
        foreach (var workflow in incompleteWorkflows)
        {
            await AttemptAutomaticRecovery(workflow, stoppingToken);
        }
    }
}

// Register in Program.cs:
builder.Services.AddHostedService<WorkflowRecoveryBackgroundService>();
```

**Recovery Logic:**
1. Check last activity timestamp (if > 5 minutes, likely interrupted)
2. Validate checkpoint integrity
3. Attempt to resume from last safe state
4. If recovery succeeds: transition to InProgress, notify user
5. If recovery fails: transition to Failed, mark for manual intervention
6. Log all recovery attempts with outcome

#### SignalR Notification Events

**New Events for Story 10.3:**
```csharp
// Client events to implement in ChatHub:
public async Task NotifyWorkflowStepFailed(Guid workflowId, string stepName, string reason)
{
    await Clients.User(userId.ToString()).SendAsync("WORKFLOW_STEP_FAILED", new
    {
        WorkflowId = workflowId,
        StepName = stepName,
        Reason = reason,
        CanRetry = true,
        CanSkip = IsStepOptional(stepName),
        Timestamp = DateTime.UtcNow
    });
}

public async Task NotifyWorkflowRecovered(Guid workflowId)
{
    await Clients.User(userId.ToString()).SendAsync("WORKFLOW_RECOVERED", new
    {
        WorkflowId = workflowId,
        ResumedFrom = workflow.LastCheckpointAt,
        Timestamp = DateTime.UtcNow
    });
}
```

#### Database Schema Changes

**Required Migration:**
```csharp
// Migration: Add recovery fields to WorkflowInstance
migrationBuilder.AddColumn<int>(
    name: "RetryCount",
    table: "WorkflowInstances",
    nullable: false,
    defaultValue: 0);

migrationBuilder.AddColumn<string>(
    name: "FailureReason",
    table: "WorkflowInstances",
    nullable: true,
    maxLength: 500);

migrationBuilder.AddColumn<DateTime>(
    name: "FailedAt",
    table: "WorkflowInstances",
    nullable: true);

migrationBuilder.AddColumn<bool>(
    name: "RequiresManualIntervention",
    table: "WorkflowInstances",
    nullable: false,
    defaultValue: false);

// Update WorkflowStatus enum to include 'Failed'
migrationBuilder.Sql(@"
    ALTER TYPE workflow_status ADD VALUE IF NOT EXISTS 'Failed';
");
```

#### Learnings from Story 10.1 & 10.2

**From Story 10.1 (Error Handling):**
✅ ProblemDetails infrastructure already configured
✅ Correlation IDs already in use for request tracking
✅ Structured logging pattern established
✅ Use existing ExceptionHandlingMiddleware pattern

**From Story 10.2 (Connection Recovery):**
✅ Exponential backoff pattern established (0s, 2s, 10s, 30s)
✅ Polly retry policies already configured
✅ SignalR reconnection logic provides template
✅ Session restoration pattern can be adapted for workflows

**Apply These Patterns:**
- Use same exponential backoff timing for workflow retries
- Leverage Polly policies for transient failure detection
- Follow ProblemDetails format for recovery API errors
- Use correlation IDs to trace recovery attempts

#### Testing Strategy

**Unit Tests:**
```csharp
// Test: WorkflowRecoveryService.RetryLogic
[Fact]
public async Task ExecuteStep_TransientFailure_RetriesThreeTimes()
{
    // Arrange: Mock step that fails twice, succeeds third time
    // Assert: Verify 3 attempts made with correct backoff
}

[Fact]
public async Task ExecuteStep_PermanentFailure_TransitionsToFailed()
{
    // Arrange: Mock step that always fails
    // Assert: After 3 retries, status = Failed
}

[Fact]
public async Task RecoverWorkflow_ValidCheckpoint_RestoresState()
{
    // Arrange: Workflow in Failed state with valid checkpoint
    // Assert: State restored, status = InProgress
}
```

**Integration Tests:**
```csharp
[Fact]
public async Task POST_WorkflowRecover_FromFailedState_Returns200()
{
    // Test: Recovery API endpoint
    // Verify: Workflow resumes from checkpoint
}

[Fact]
public async Task ServerRestart_IncompleteWorkflows_AutoRecoveryAttempted()
{
    // Test: Background service recovery
    // Verify: Incomplete workflows detected and recovered
}
```

**BDD Tests (SpecFlow):**
```gherkin
Scenario: Transient failure triggers automatic retry
  Given a workflow step encounters a timeout
  When the step execution fails
  Then the system retries up to 3 times
  And each retry is logged with correlation ID

Scenario: Failed workflow recovered via API
  Given a workflow is in Failed state
  When I POST to /api/v1/workflows/{id}/recover
  Then the workflow resumes from last checkpoint
  And I receive a success notification
```

#### Files to Create/Modify

**New Files:**
```
src/bmadServer.ApiService/
  Services/
    WorkflowRecoveryService.cs          # Core recovery logic
    WorkflowRecoveryBackgroundService.cs # Startup recovery
  Controllers/
    WorkflowRecoveryController.cs       # Recovery API endpoint
  Models/DTOs/
    WorkflowRecoveryResult.cs          # Recovery response model
    WorkflowFailureNotification.cs      # SignalR notification model
```

**Modified Files:**
```
src/bmadServer.ApiService/
  Data/Entities/
    WorkflowInstance.cs                 # Add: RetryCount, FailureReason, FailedAt, RequiresManualIntervention
  Services/
    WorkflowExecutionService.cs         # Add: Retry logic, transient failure detection
  Hubs/
    ChatHub.cs                          # Add: WORKFLOW_STEP_FAILED, WORKFLOW_RECOVERED events
  Program.cs                            # Register: WorkflowRecoveryBackgroundService
```

**Database Migrations:**
```
src/bmadServer.ApiService/Data/Migrations/
  AddWorkflowRecoveryFields.cs          # Migration for new fields
```

**Test Files:**
```
src/bmadServer.Tests/
  Services/
    WorkflowRecoveryServiceTests.cs
  Controllers/
    WorkflowRecoveryControllerTests.cs
src/bmadServer.ApiService.IntegrationTests/
  WorkflowRecoveryTests.cs
src/bmadServer.BDD.Tests/
  Features/
    WorkflowRecovery.feature            # BDD scenarios
  StepDefinitions/
    WorkflowRecoverySteps.cs
```

#### Performance Considerations

**Retry Strategy Impact:**
- Retries add latency: Max 17 seconds (2s + 5s + 10s)
- Use async/await to avoid blocking
- Consider timeout per retry attempt (e.g., 30s per attempt)

**Background Recovery:**
- Limit concurrent recovery attempts (e.g., 5 at a time)
- Use semaphore or task limiting to prevent resource exhaustion
- Monitor recovery performance with metrics

**Database Performance:**
- Index on WorkflowStatus for fast incomplete workflow queries
- Use JSONB efficiently for checkpoint storage
- Consider archiving old failed workflows

#### Security Considerations

**Authorization:**
- Recovery API: Only workflow owner or admin can recover
- Admin dashboard: Requires Admin role
- Validate workflow ownership before recovery

**Audit Trail:**
- Log all recovery attempts with user ID
- Track manual interventions by admins
- Store recovery outcomes in event log

**Data Validation:**
- Validate checkpoint integrity before restoration
- Sanitize failure reasons before storing
- Prevent injection attacks in recovery parameters

---

## Aspire Development Standards

### PostgreSQL Connection Pattern

This story uses PostgreSQL configured in Story 1.2 via Aspire:
- Connection string automatically injected from Aspire AppHost
- Pattern: `builder.AddServiceDefaults();` (inherits PostgreSQL reference)
- See Story 1.2 for AppHost configuration pattern

### Aspire Observability for Recovery Tracking

Use built-in Aspire telemetry for recovery monitoring:
```csharp
// Recovery metrics automatically tracked via OpenTelemetry
// View in Aspire Dashboard: http://localhost:15888
```

### Project-Wide Standards

This story follows the Aspire-first development pattern:
- **Reference:** [PROJECT-WIDE-RULES.md](../../../PROJECT-WIDE-RULES.md)
- **Primary Documentation:** https://aspire.dev
- **GitHub:** https://github.com/microsoft/aspire

---

## References

- **Epic Context:** [epics.md - Epic 10: Error Handling & Recovery](../planning-artifacts/epics.md#epic-10-error-handling--recovery)
- **Story Source:** [epics.md - Story 10.3](../planning-artifacts/epics.md)
- **Architecture:** [architecture.md](../planning-artifacts/architecture.md)
- **PRD:** [prd.md](../planning-artifacts/prd.md)
- **Previous Stories:** 
  - [10.1: Graceful Error Handling](./10-1-graceful-error-handling.md)
  - [10.2: Connection Recovery & Retry](./10-2-connection-recovery-retry.md)
- **Epic 4 Foundation:** Workflow Orchestration Engine (Stories 4.1-4.7)
- **Aspire Rules:** [PROJECT-WIDE-RULES.md](../../../PROJECT-WIDE-RULES.md)

---

## Dev Agent Record

### Agent Model Used

_To be filled during implementation_

### Debug Log References

_To be added during implementation_

### Completion Notes List

_To be added during implementation_

### File List

_To be added during implementation_
