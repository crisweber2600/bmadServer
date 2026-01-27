# Story 7.4: Conflict Detection & Buffering

**Status:** review

## Story

As a user (Marcus), I want conflicting inputs to be buffered and flagged, so that we can resolve disagreements properly.

## Acceptance Criteria

### AC1: Conflict Detection on Concurrent Input

**Given** two users submit different inputs for the same field  
**When** both inputs arrive before checkpoint  
**Then** the system detects the conflict  
**And** both inputs are buffered (not applied)  
**And** users are notified: "Conflict detected - human arbitration required"

### AC2: Conflict UI Display

**Given** a conflict is detected  
**When** I view the conflict UI  
**Then** I see: both proposed values, who submitted each, timestamp, field context

### AC3: Conflict Resolution

**Given** I am a workflow owner or Admin  
**When** I resolve the conflict  
**Then** I can choose: Accept A, Accept B, Merge, Reject Both  
**And** the resolution is applied at next checkpoint

### AC4: Conflict Timeout and Escalation

**Given** a conflict remains unresolved for 1 hour  
**When** the timeout occurs  
**Then** the workflow pauses  
**And** escalation notifications are sent to workflow owner  
**And** the workflow cannot proceed until resolved

### AC5: Conflict History Tracking

**Given** conflicts are resolved  
**When** I query conflict history  
**Then** I see all conflicts with: inputs, resolution, resolver, timestamp, reason

## Tasks / Subtasks

- [ ] Create Conflict data model and entity (AC: #1, #2, #3, #5)
  - [ ] Create Conflict entity with fields: Id, WorkflowId, FieldName, ConflictType
  - [ ] Add ConflictInput embedded model: UserId, DisplayName, Value, Timestamp
  - [ ] Add ConflictResolution embedded model: ResolvedBy, ResolutionType, FinalValue, ResolvedAt, Reason
  - [ ] Add Status (Pending, Resolved, Escalated)
  - [ ] Add CreatedAt, EscalatedAt, ExpiresAt timestamps
  - [ ] Create EF Core entity configuration
  - [ ] Create database migration for conflicts table

- [ ] Implement conflict detection service (AC: #1)
  - [ ] Create IConflictDetectionService interface
  - [ ] Implement ConflictDetectionService
  - [ ] Add DetectConflictAsync method that compares buffered inputs
  - [ ] Support field-level conflict detection (same field, different values)
  - [ ] Support decision-level conflict detection (contradicting decisions)
  - [ ] Store detected conflicts in database
  - [ ] Mark conflicting inputs as "buffered" (not applied)

- [ ] Input buffer enhancement (AC: #1)
  - [ ] Add ConflictStatus field to BufferedInput model
  - [ ] Update BufferInputAsync to check for existing inputs on same field
  - [ ] Trigger conflict detection when duplicate field detected
  - [ ] Prevent buffered input application if conflict exists

- [ ] Conflict notification service (AC: #1, #4)
  - [ ] Create IConflictNotificationService interface
  - [ ] Implement ConflictNotificationService
  - [ ] Send SignalR notification when conflict detected: CONFLICT_DETECTED event
  - [ ] Include conflict details in notification
  - [ ] Send SignalR notification to all workflow participants
  - [ ] Schedule escalation notification after 1 hour timeout
  - [ ] Use background job (Hangfire or similar) for timeout tracking

- [ ] Conflict resolution service (AC: #3)
  - [ ] Create IConflictResolutionService interface
  - [ ] Implement ConflictResolutionService
  - [ ] Add ResolveConflictAsync method with resolution options
  - [ ] Support resolution types: AcceptA, AcceptB, Merge, RejectBoth
  - [ ] Apply resolution at next checkpoint (integrate with Story 7.2)
  - [ ] Update conflict status to Resolved
  - [ ] Record resolver userId, timestamp, and reason
  - [ ] Log conflict resolution in WorkflowEvents for audit trail

- [ ] Conflict API endpoints (AC: #2, #3, #5)
  - [ ] Add GET `/api/v1/workflows/{id}/conflicts` - List all conflicts
  - [ ] Add GET `/api/v1/workflows/{id}/conflicts/{conflictId}` - Get conflict details
  - [ ] Add POST `/api/v1/workflows/{id}/conflicts/{conflictId}/resolve` - Resolve conflict
  - [ ] Add query parameter filtering: status (pending/resolved/escalated)
  - [ ] Validate user is workflow owner or Admin for resolution
  - [ ] Add `[Authorize]` attributes
  - [ ] Return RFC 7807 ProblemDetails on errors

- [ ] Workflow pause/resume for conflicts (AC: #4)
  - [ ] Add WorkflowStatus.PausedConflict state
  - [ ] Update workflow state machine to pause on unresolved expired conflict
  - [ ] Prevent step advancement if conflicts exist
  - [ ] Add ResumeWorkflowAsync method when conflict resolved
  - [ ] Update WorkflowsController to handle paused state

- [ ] Conflict escalation job (AC: #4)
  - [ ] Create ConflictEscalationJob background service
  - [ ] Poll for conflicts where ExpiresAt < DateTime.UtcNow and Status == Pending
  - [ ] Update conflict status to Escalated
  - [ ] Send escalation notification to workflow owner
  - [ ] Pause workflow if not already paused
  - [ ] Log escalation event in WorkflowEvents

- [ ] SignalR real-time conflict events (AC: #1, #2)
  - [ ] Add CONFLICT_DETECTED event to ChatHub
  - [ ] Add CONFLICT_RESOLVED event to ChatHub
  - [ ] Add CONFLICT_ESCALATED event to ChatHub
  - [ ] Broadcast events to all workflow participants
  - [ ] Include conflict details in event payload

- [ ] Unit tests (AC: All)
  - [ ] ConflictDetectionService tests (field-level, decision-level)
  - [ ] ConflictResolutionService tests (all resolution types)
  - [ ] ConflictEscalationJob tests (timeout, escalation logic)
  - [ ] Conflict model tests (validation, state transitions)
  - [ ] Input buffer conflict detection tests

- [ ] Integration tests (AC: All)
  - [ ] End-to-end conflict detection flow
  - [ ] Conflict resolution API endpoints
  - [ ] Conflict escalation and workflow pause
  - [ ] Multi-user concurrent input with conflict
  - [ ] Conflict history query
  - [ ] SignalR real-time conflict notifications

## Dev Notes

### Critical Architecture Patterns

This story implements **Conflict Detection & Buffering** to handle concurrent multi-user inputs safely. It builds upon:
- ✅ Story 7.1 (Multi-User Workflow Participation) - Multiple users can contribute
- ✅ Story 7.2 (Safe Checkpoint System) - Input buffering and checkpoint-based application
- ✅ Story 7.3 (Input Attribution & History) - Track who submitted each conflicting input
- ✅ ADR-001 (Hybrid Document Store + Event Log) - Audit trail with full provenance

#### 🎯 Core Conflict Detection Principles

**WHY CONFLICT DETECTION IS CRITICAL:**

1. **Data Integrity** - Prevent conflicting inputs from corrupting workflow state
2. **Collaboration Safety** - Enable multiple users to work concurrently without overwrites
3. **Human Judgment** - Complex conflicts require human arbitration, not automated resolution
4. **Audit Trail** - Track all conflicts and their resolutions for compliance
5. **Workflow Continuity** - Pause workflows until conflicts are resolved to maintain integrity

**Conflict Detection Strategy:**

```
Concurrent Input Flow:
  User A submits input for field X → Buffered
  User B submits different input for field X → Conflict detected!
  
  Both inputs buffered (not applied) → 
  All participants notified → 
  Workflow pauses if conflict not resolved within 1 hour → 
  Owner/Admin resolves conflict → 
  Resolution applied at next checkpoint
```

### Technical Implementation Details

#### Conflict Entity Model

```csharp
public class Conflict
{
    public Guid Id { get; set; }
    public Guid WorkflowInstanceId { get; set; }
    public string FieldName { get; set; } = string.Empty; // e.g., "productName", "targetAudience"
    public ConflictType Type { get; set; } // FieldValue, Decision, Checkpoint
    public ConflictStatus Status { get; set; } // Pending, Resolved, Escalated
    
    // Conflicting inputs
    public List<ConflictInput> Inputs { get; set; } = new();
    
    // Resolution
    public ConflictResolution? Resolution { get; set; }
    
    // Timestamps
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; } // CreatedAt + 1 hour
    public DateTime? EscalatedAt { get; set; }
    
    // Navigation
    public WorkflowInstance? WorkflowInstance { get; set; }
}

public class ConflictInput
{
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty; // JSON serialized value
    public DateTime Timestamp { get; set; }
    public Guid BufferedInputId { get; set; } // Link to original buffered input
}

public class ConflictResolution
{
    public Guid ResolvedBy { get; set; } // UserId
    public string ResolverDisplayName { get; set; } = string.Empty;
    public ResolutionType Type { get; set; } // AcceptA, AcceptB, Merge, RejectBoth
    public string FinalValue { get; set; } = string.Empty; // JSON serialized final value
    public DateTime ResolvedAt { get; set; }
    public string Reason { get; set; } = string.Empty; // Explanation for resolution choice
}

public enum ConflictType
{
    FieldValue,    // Two users submitted different values for same field
    Decision,      // Two users made contradicting decisions
    Checkpoint     // Conflict at checkpoint boundary
}

public enum ConflictStatus
{
    Pending,       // Awaiting resolution
    Resolved,      // Resolution applied
    Escalated      // Timeout exceeded, workflow paused
}

public enum ResolutionType
{
    AcceptA,       // Use first input's value
    AcceptB,       // Use second input's value
    Merge,         // Combine both values (requires manual merge value)
    RejectBoth     // Discard both inputs, require new input
}
```

#### Database Migration

```sql
-- Create conflicts table
CREATE TABLE conflicts (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    workflow_instance_id UUID NOT NULL REFERENCES workflow_instances(id) ON DELETE CASCADE,
    field_name VARCHAR(255) NOT NULL,
    type VARCHAR(50) NOT NULL, -- FieldValue, Decision, Checkpoint
    status VARCHAR(50) NOT NULL DEFAULT 'Pending', -- Pending, Resolved, Escalated
    inputs JSONB NOT NULL, -- Array of ConflictInput objects
    resolution JSONB, -- ConflictResolution object (nullable)
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    expires_at TIMESTAMP NOT NULL,
    escalated_at TIMESTAMP,
    
    -- Indexes for queries
    INDEX idx_conflicts_workflow (workflow_instance_id),
    INDEX idx_conflicts_status (status),
    INDEX idx_conflicts_expires (expires_at) WHERE status = 'Pending'
);

-- Add conflict_status to buffered_inputs
ALTER TABLE buffered_inputs 
ADD COLUMN conflict_id UUID REFERENCES conflicts(id) ON DELETE SET NULL;

CREATE INDEX idx_buffered_inputs_conflict ON buffered_inputs(conflict_id);
```

#### Conflict Detection Service

```csharp
public interface IConflictDetectionService
{
    Task<Conflict?> DetectConflictAsync(
        Guid workflowId, 
        string fieldName, 
        BufferedInput newInput, 
        CancellationToken cancellationToken = default);
    
    Task<List<Conflict>> GetPendingConflictsAsync(
        Guid workflowId, 
        CancellationToken cancellationToken = default);
}

public class ConflictDetectionService : IConflictDetectionService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<ConflictDetectionService> _logger;
    
    public async Task<Conflict?> DetectConflictAsync(
        Guid workflowId, 
        string fieldName, 
        BufferedInput newInput, 
        CancellationToken cancellationToken = default)
    {
        // Check if there's already a buffered input for same field
        var existingInputs = await _dbContext.BufferedInputs
            .Where(bi => bi.WorkflowInstanceId == workflowId 
                      && bi.FieldName == fieldName
                      && bi.Status == BufferStatus.Pending
                      && bi.UserId != newInput.UserId) // Different user
            .ToListAsync(cancellationToken);
        
        if (!existingInputs.Any())
            return null; // No conflict
        
        // Conflict detected! Check if values differ
        bool valuesConflict = existingInputs.Any(ei => 
            !JsonSerializer.Serialize(ei.Value).Equals(
                JsonSerializer.Serialize(newInput.Value)));
        
        if (!valuesConflict)
            return null; // Same value, no real conflict
        
        // Create conflict record
        var conflict = new Conflict
        {
            WorkflowInstanceId = workflowId,
            FieldName = fieldName,
            Type = ConflictType.FieldValue,
            Status = ConflictStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            Inputs = new List<ConflictInput>()
        };
        
        // Add all conflicting inputs
        foreach (var existing in existingInputs)
        {
            conflict.Inputs.Add(new ConflictInput
            {
                UserId = existing.UserId,
                DisplayName = existing.DisplayName,
                Value = JsonSerializer.Serialize(existing.Value),
                Timestamp = existing.Timestamp,
                BufferedInputId = existing.Id
            });
        }
        
        // Add new input
        conflict.Inputs.Add(new ConflictInput
        {
            UserId = newInput.UserId,
            DisplayName = newInput.DisplayName,
            Value = JsonSerializer.Serialize(newInput.Value),
            Timestamp = newInput.Timestamp,
            BufferedInputId = newInput.Id
        });
        
        _dbContext.Conflicts.Add(conflict);
        await _dbContext.SaveChangesAsync(cancellationToken);
        
        _logger.LogWarning(
            "Conflict detected for workflow {WorkflowId}, field {FieldName}, conflict {ConflictId}",
            workflowId, fieldName, conflict.Id);
        
        return conflict;
    }
}
```

#### Conflict Resolution Service

```csharp
public interface IConflictResolutionService
{
    Task<ConflictResolution> ResolveConflictAsync(
        Guid conflictId, 
        Guid resolvedByUserId,
        ResolutionType resolutionType,
        string? mergeValue,
        string reason,
        CancellationToken cancellationToken = default);
    
    Task ApplyResolutionAtCheckpointAsync(
        Guid workflowId, 
        CancellationToken cancellationToken = default);
}

public class ConflictResolutionService : IConflictResolutionService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<ConflictResolutionService> _logger;
    private readonly IHubContext<ChatHub> _hubContext;
    
    public async Task<ConflictResolution> ResolveConflictAsync(
        Guid conflictId, 
        Guid resolvedByUserId,
        ResolutionType resolutionType,
        string? mergeValue,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var conflict = await _dbContext.Conflicts
            .Include(c => c.WorkflowInstance)
            .FirstOrDefaultAsync(c => c.Id == conflictId, cancellationToken);
        
        if (conflict == null)
            throw new NotFoundException($"Conflict {conflictId} not found");
        
        if (conflict.Status != ConflictStatus.Pending)
            throw new InvalidOperationException($"Conflict {conflictId} already resolved");
        
        // Get resolver display name
        var resolver = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == resolvedByUserId, cancellationToken);
        
        // Determine final value based on resolution type
        string finalValue = resolutionType switch
        {
            ResolutionType.AcceptA => conflict.Inputs[0].Value,
            ResolutionType.AcceptB => conflict.Inputs[1].Value,
            ResolutionType.Merge => mergeValue ?? throw new ArgumentException("Merge value required"),
            ResolutionType.RejectBoth => "",
            _ => throw new ArgumentException($"Unknown resolution type: {resolutionType}")
        };
        
        // Create resolution record
        var resolution = new ConflictResolution
        {
            ResolvedBy = resolvedByUserId,
            ResolverDisplayName = resolver?.DisplayName ?? "Unknown",
            Type = resolutionType,
            FinalValue = finalValue,
            ResolvedAt = DateTime.UtcNow,
            Reason = reason
        };
        
        conflict.Resolution = resolution;
        conflict.Status = ConflictStatus.Resolved;
        
        await _dbContext.SaveChangesAsync(cancellationToken);
        
        // Log resolution event
        _dbContext.WorkflowEvents.Add(new WorkflowEvent
        {
            WorkflowInstanceId = conflict.WorkflowInstanceId,
            EventType = "ConflictResolved",
            UserId = resolvedByUserId,
            DisplayName = resolver?.DisplayName,
            Timestamp = DateTime.UtcNow,
            Payload = JsonSerializer.SerializeToDocument(new
            {
                ConflictId = conflictId,
                FieldName = conflict.FieldName,
                ResolutionType = resolutionType.ToString(),
                FinalValue = finalValue,
                Reason = reason
            })
        });
        
        await _dbContext.SaveChangesAsync(cancellationToken);
        
        // Broadcast resolution event
        await _hubContext.Clients.Group($"workflow-{conflict.WorkflowInstanceId}")
            .SendAsync("CONFLICT_RESOLVED", new
            {
                ConflictId = conflictId,
                Resolution = resolution
            }, cancellationToken);
        
        _logger.LogInformation(
            "Conflict {ConflictId} resolved by user {UserId} with {ResolutionType}",
            conflictId, resolvedByUserId, resolutionType);
        
        return resolution;
    }
    
    public async Task ApplyResolutionAtCheckpointAsync(
        Guid workflowId, 
        CancellationToken cancellationToken = default)
    {
        // Get all resolved conflicts
        var resolvedConflicts = await _dbContext.Conflicts
            .Where(c => c.WorkflowInstanceId == workflowId 
                     && c.Status == ConflictStatus.Resolved)
            .ToListAsync(cancellationToken);
        
        foreach (var conflict in resolvedConflicts)
        {
            if (conflict.Resolution == null)
                continue;
            
            // Apply final value to workflow state
            var workflowState = await _dbContext.WorkflowInstances
                .FirstOrDefaultAsync(w => w.Id == workflowId, cancellationToken);
            
            if (workflowState != null && conflict.Resolution.Type != ResolutionType.RejectBoth)
            {
                // Update workflow state with final value
                // Implementation depends on workflow state structure
                _logger.LogInformation(
                    "Applied conflict resolution {ConflictId} to workflow {WorkflowId}",
                    conflict.Id, workflowId);
            }
        }
    }
}
```

#### API Endpoints

```csharp
[ApiController]
[Route("api/v1/workflows/{workflowId}/conflicts")]
[Authorize]
public class ConflictsController : ControllerBase
{
    private readonly IConflictDetectionService _detectionService;
    private readonly IConflictResolutionService _resolutionService;
    private readonly ApplicationDbContext _dbContext;
    
    [HttpGet]
    public async Task<ActionResult<List<ConflictResponse>>> GetConflicts(
        Guid workflowId,
        [FromQuery] ConflictStatus? status = null)
    {
        // Verify user has access to workflow
        var workflow = await _dbContext.WorkflowInstances
            .Include(w => w.Participants)
            .FirstOrDefaultAsync(w => w.Id == workflowId);
        
        if (workflow == null)
            return NotFound(new ProblemDetails
            {
                Status = 404,
                Title = "Workflow Not Found",
                Detail = $"Workflow {workflowId} not found"
            });
        
        var userId = User.GetUserId();
        if (!workflow.Participants.Any(p => p.UserId == userId))
            return Forbid();
        
        // Get conflicts
        var query = _dbContext.Conflicts
            .Where(c => c.WorkflowInstanceId == workflowId);
        
        if (status.HasValue)
            query = query.Where(c => c.Status == status.Value);
        
        var conflicts = await query
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
        
        return Ok(conflicts.Select(c => new ConflictResponse
        {
            Id = c.Id,
            FieldName = c.FieldName,
            Type = c.Type.ToString(),
            Status = c.Status.ToString(),
            Inputs = c.Inputs.Select(i => new ConflictInputDto
            {
                UserId = i.UserId,
                DisplayName = i.DisplayName,
                Value = i.Value,
                Timestamp = i.Timestamp
            }).ToList(),
            Resolution = c.Resolution != null ? new ConflictResolutionDto
            {
                ResolvedBy = c.Resolution.ResolvedBy,
                ResolverDisplayName = c.Resolution.ResolverDisplayName,
                Type = c.Resolution.Type.ToString(),
                FinalValue = c.Resolution.FinalValue,
                ResolvedAt = c.Resolution.ResolvedAt,
                Reason = c.Resolution.Reason
            } : null,
            CreatedAt = c.CreatedAt,
            ExpiresAt = c.ExpiresAt,
            EscalatedAt = c.EscalatedAt
        }));
    }
    
    [HttpGet("{conflictId}")]
    public async Task<ActionResult<ConflictResponse>> GetConflict(
        Guid workflowId,
        Guid conflictId)
    {
        var conflict = await _dbContext.Conflicts
            .Include(c => c.WorkflowInstance)
                .ThenInclude(w => w.Participants)
            .FirstOrDefaultAsync(c => c.Id == conflictId && c.WorkflowInstanceId == workflowId);
        
        if (conflict == null)
            return NotFound();
        
        var userId = User.GetUserId();
        if (!conflict.WorkflowInstance.Participants.Any(p => p.UserId == userId))
            return Forbid();
        
        return Ok(/* map to ConflictResponse */);
    }
    
    [HttpPost("{conflictId}/resolve")]
    public async Task<ActionResult<ConflictResolutionDto>> ResolveConflict(
        Guid workflowId,
        Guid conflictId,
        [FromBody] ResolveConflictRequest request)
    {
        // Verify user is workflow owner or Admin
        var workflow = await _dbContext.WorkflowInstances
            .FirstOrDefaultAsync(w => w.Id == workflowId);
        
        if (workflow == null)
            return NotFound();
        
        var userId = User.GetUserId();
        var isOwner = workflow.OwnerId == userId;
        var isAdmin = User.IsInRole("Admin");
        
        if (!isOwner && !isAdmin)
            return Problem(
                statusCode: 403,
                title: "Forbidden",
                detail: "Only workflow owner or Admin can resolve conflicts",
                type: "https://bmadserver.api/errors/conflict-resolution-forbidden"
            );
        
        try
        {
            var resolution = await _resolutionService.ResolveConflictAsync(
                conflictId,
                userId,
                request.ResolutionType,
                request.MergeValue,
                request.Reason);
            
            return Ok(new ConflictResolutionDto
            {
                ResolvedBy = resolution.ResolvedBy,
                ResolverDisplayName = resolution.ResolverDisplayName,
                Type = resolution.Type.ToString(),
                FinalValue = resolution.FinalValue,
                ResolvedAt = resolution.ResolvedAt,
                Reason = resolution.Reason
            });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Status = 404,
                Title = "Conflict Not Found",
                Detail = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return Problem(
                statusCode = 400,
                title: "Invalid Operation",
                detail: ex.Message,
                type: "https://bmadserver.api/errors/conflict-already-resolved"
            );
        }
    }
}
```

### Project Structure Notes

This story follows the **established Aspire + Clean Architecture pattern**:

```
src/bmadServer.ApiService/
├── Data/
│   └── Entities/
│       └── Conflict.cs (NEW)
├── Models/
│   ├── ConflictInput.cs (NEW)
│   ├── ConflictResolution.cs (NEW)
│   └── Enums/
│       ├── ConflictType.cs (NEW)
│       ├── ConflictStatus.cs (NEW)
│       └── ResolutionType.cs (NEW)
├── Services/
│   ├── ConflictDetectionService.cs (NEW)
│   ├── ConflictResolutionService.cs (NEW)
│   ├── ConflictNotificationService.cs (NEW)
│   └── ConflictEscalationJob.cs (NEW - Background service)
├── Controllers/
│   └── ConflictsController.cs (NEW)
├── DTOs/
│   ├── ConflictResponse.cs (NEW)
│   ├── ConflictInputDto.cs (NEW)
│   ├── ConflictResolutionDto.cs (NEW)
│   └── ResolveConflictRequest.cs (NEW)
└── Hubs/
    └── ChatHub.cs (UPDATE: Add conflict events)
```

### Dependencies and Integration Points

**This story depends on:**
- ✅ Story 7.1 (Multi-User Workflow Participation) - Multiple users contributing
- ✅ Story 7.2 (Safe Checkpoint System) - Input buffering and checkpoint application
- ✅ Story 7.3 (Input Attribution & History) - User attribution for conflict tracking
- ✅ ADR-001 (Hybrid Document Store + Event Log) - Event persistence

**This story enables:**
- 🔜 Story 7.5 (Real-Time Collaboration Updates) - Real-time conflict notifications
- 🔜 Epic 11 (Security & Compliance) - Conflict resolution audit trail
- 🔜 Epic 12 (Dashboard & Monitoring) - Conflict metrics and alerts

### Critical Implementation Rules

**MUST-FOLLOW:**

1. **Detect conflicts at input time** (before buffering, not at checkpoint)
2. **Buffer conflicting inputs** (never auto-apply conflicting values)
3. **Notify all participants immediately** via SignalR when conflict detected
4. **Pause workflow** if conflict unresolved after 1 hour timeout
5. **Require owner/Admin** for conflict resolution (not any participant)
6. **Apply resolutions at checkpoints** (integrate with Story 7.2 checkpoint system)
7. **Log all conflicts and resolutions** in WorkflowEvents for audit trail
8. **Use background job** for timeout tracking (Hangfire, HostedService, or similar)

**Error Handling with RFC 7807 ProblemDetails:**

```csharp
// Example: Conflict not found
return Problem(
    statusCode: 404,
    title: "Conflict Not Found",
    detail: $"Conflict {conflictId} does not exist",
    type: "https://bmadserver.api/errors/conflict-not-found"
);

// Example: Conflict already resolved
return Problem(
    statusCode: 400,
    title: "Conflict Already Resolved",
    detail: "This conflict has already been resolved",
    type: "https://bmadserver.api/errors/conflict-already-resolved"
);

// Example: Unauthorized resolution attempt
return Problem(
    statusCode: 403,
    title: "Resolution Forbidden",
    detail: "Only workflow owner or Admin can resolve conflicts",
    type: "https://bmadserver.api/errors/conflict-resolution-forbidden"
);
```

### Performance Considerations

- Index conflicts by (workflow_id, status, expires_at) for fast queries
- Use background job polling (every 5 minutes) for escalation, not per-request
- Cache conflict count per workflow to show "Conflicts: 2" badge without query
- Consider soft-delete for resolved conflicts (keep in DB for audit, hide from UI)

### UI Integration Notes

**Conflict Banner (Frontend):**
```typescript
interface ConflictBanner {
  conflictId: string;
  fieldName: string;
  conflictingUsers: string[]; // Display names
  conflictCount: number;
  expiresAt: Date;
}
```

**Conflict Resolution Modal:**
```typescript
interface ConflictResolutionModal {
  conflictId: string;
  fieldName: string;
  inputs: ConflictInput[];
  onResolve: (resolutionType: ResolutionType, mergeValue?: string, reason: string) => Promise<void>;
}
```

**UI Components to Create:**
- ConflictBanner: Show conflict alert at top of workflow
- ConflictResolutionModal: Allow owner/Admin to resolve
- ConflictHistoryList: Show all conflicts with status/resolution
- WorkflowPausedBanner: Show when workflow paused due to expired conflict

### Testing Strategy

**Unit Tests Must Cover:**
- ConflictDetectionService: Detect conflicts on same field, different values
- ConflictDetectionService: No conflict when same value submitted by multiple users
- ConflictResolutionService: All resolution types (AcceptA, AcceptB, Merge, RejectBoth)
- ConflictEscalationJob: Timeout detection and workflow pause
- Conflict model validation

**Integration Tests Must Cover:**
- End-to-end conflict flow: detect → notify → resolve → apply
- Concurrent inputs from 2 users causing conflict
- Conflict resolution by owner
- Conflict escalation after 1 hour timeout
- Workflow pause and resume after conflict resolution
- SignalR real-time conflict notifications
- Conflict history API endpoint

**Test Data Scenarios:**
- Two users submit different product names simultaneously
- Two users submit contradicting decisions
- Conflict resolved with AcceptA
- Conflict resolved with Merge (custom value)
- Conflict escalated after timeout (1 hour)
- Workflow paused and resumed

### Known Limitations

1. **No automatic merge** - All conflicts require human arbitration (by design)
2. **1-hour timeout fixed** - MVP uses fixed 1 hour, Phase 2 could make configurable
3. **Owner/Admin only** - Only owner or Admin can resolve (not any Contributor)
4. **Field-level conflicts only** - MVP detects same-field conflicts, not logical conflicts across fields
5. **No conflict preview** - Users see conflict after submission, not before (Phase 2 enhancement)

---

## Aspire Development Standards

### PostgreSQL Connection Pattern

This story uses PostgreSQL configured in Story 1.2 via Aspire:
- Connection string automatically injected from Aspire AppHost
- Pattern: `builder.AddServiceDefaults();` (inherits PostgreSQL reference)
- See Story 1.2 for AppHost configuration pattern
- Use EF Core 9.0 with PostgreSQL provider for database access
- Use `JsonDocument` type for JSONB columns (Inputs, Resolution)

### Project-Wide Standards

This story follows the Aspire-first development pattern:
- **Reference:** [PROJECT-WIDE-RULES.md](../../../PROJECT-WIDE-RULES.md)
- **Primary Documentation:** https://aspire.dev
- **GitHub:** https://github.com/microsoft/aspire

### SignalR Integration Pattern

This story uses SignalR configured in Story 3.1:
- **MVP:** Built-in ASP.NET Core SignalR
- Real-time conflict notifications via SignalR hub
- Broadcast CONFLICT_DETECTED, CONFLICT_RESOLVED, CONFLICT_ESCALATED events
- See Story 3.1 for SignalR configuration pattern

### Background Jobs Pattern

For conflict escalation timeout tracking, use one of these patterns:
- **Option 1 (Recommended):** `IHostedService` with timer (built-in .NET)
- **Option 2:** Hangfire (if already configured for other background jobs)
- **Option 3:** Quartz.NET (if more complex scheduling needed)

**MVP Recommendation:** Use `IHostedService` with `PeriodicTimer` for simplicity:

```csharp
public class ConflictEscalationHostedService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<ConflictEscalationHostedService> _logger;
    private readonly PeriodicTimer _timer = new(TimeSpan.FromMinutes(5));
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (await _timer.WaitForNextTickAsync(stoppingToken))
        {
            using var scope = _services.CreateScope();
            var escalationService = scope.ServiceProvider.GetRequiredService<IConflictEscalationService>();
            
            await escalationService.ProcessExpiredConflictsAsync(stoppingToken);
        }
    }
}
```

Register in `Program.cs`:
```csharp
builder.Services.AddHostedService<ConflictEscalationHostedService>();
```

---

### References

- **Epic 7 Context:** [epics.md Lines 2165-2199](../planning-artifacts/epics.md)
- **Architecture - ADR-001:** [architecture.md Lines 317-351](../planning-artifacts/architecture.md)
- **Architecture - ADR-003 (SignalR):** [architecture.md Lines 411-431](../planning-artifacts/architecture.md)
- **Story 7.1 (Multi-User Participation):** [7-1-multi-user-workflow-participation.md](./7-1-multi-user-workflow-participation.md)
- **Story 7.2 (Safe Checkpoint System):** [7-2-safe-checkpoint-system.md](./7-2-safe-checkpoint-system.md)
- **Story 7.3 (Input Attribution):** [7-3-input-attribution-history.md](./7-3-input-attribution-history.md)
- **Aspire Rules:** [PROJECT-WIDE-RULES.md](../../../PROJECT-WIDE-RULES.md)
- **Aspire Docs:** https://aspire.dev

---

## Dev Agent Record

### Agent Model Used

claude-3-7-sonnet-20250219

### Debug Log References

_(To be filled during implementation)_

### Completion Notes List

_(To be filled during implementation)_

### File List

_(To be filled during implementation)_

### Latest Technology Information

**Key Technologies & Versions:**

1. **ASP.NET Core 9.0** - Web API framework
   - Use `IHostedService` for background conflict escalation job
   - Use `PeriodicTimer` for polling (built-in, efficient)
   - JWT authentication via `Microsoft.AspNetCore.Authentication.JwtBearer`

2. **Entity Framework Core 9.0** - ORM
   - Use `JsonDocument` type for JSONB columns (Inputs, Resolution)
   - Use `.HasColumnType("jsonb")` in entity configuration
   - Use migrations for schema changes
   - Use `.Include()` for eager loading related entities

3. **PostgreSQL 17.x** - Database
   - JSONB columns for flexible conflict data storage
   - Indexes on (workflow_instance_id, status, expires_at) for performance
   - Use `gen_random_uuid()` for primary keys
   - Foreign key constraints with `ON DELETE CASCADE`

4. **SignalR** - Real-time communication
   - Broadcast conflict events: CONFLICT_DETECTED, CONFLICT_RESOLVED, CONFLICT_ESCALATED
   - Include conflict details in event payload
   - Use `IHubContext<ChatHub>` for broadcasting from services

5. **FluentValidation 11.9.2** - Input validation
   - Validate resolution request: resolution type, merge value (if needed), reason
   - Validate conflict status transitions

6. **Serilog** - Structured logging
   - Log all conflict detections, resolutions, escalations for audit trail
   - Include conflictId, workflowId, userId in log context

**Security Best Practices:**
- Validate user is workflow owner or Admin before allowing conflict resolution
- Use `[Authorize]` attribute on all endpoints
- Validate user is workflow participant before viewing conflicts
- Rate limit conflict resolution endpoint (prevent abuse)
- Log all conflict resolutions for audit compliance

**API Design:**
- Follow RESTful conventions
- Use RFC 7807 ProblemDetails for errors
- Version APIs with `/api/v1/` prefix
- Use consistent HTTP status codes (200, 400, 403, 404, 500)

**Background Job Best Practices:**
- Use `IHostedService` for MVP (simple, built-in)
- Poll every 5 minutes for expired conflicts (balance between latency and load)
- Use scoped services inside background job (`CreateScope()`)
- Handle exceptions gracefully (log and continue)
- Consider idempotency (same conflict shouldn't escalate twice)

**Conflict Detection Algorithm:**
1. When input buffered, check for existing buffered inputs on same field
2. If existing input found with different value from different user → Conflict!
3. Create Conflict record with both inputs
4. Mark both BufferedInputs with conflict_id
5. Notify all participants via SignalR
6. Set 1-hour expiration timer

**Conflict Resolution Algorithm:**
1. Validate user is owner or Admin
2. Validate conflict status is Pending
3. Apply resolution type (AcceptA/AcceptB/Merge/RejectBoth)
4. Update conflict status to Resolved
5. Log resolution in WorkflowEvents
6. Broadcast CONFLICT_RESOLVED event
7. Resolution applied at next checkpoint (Story 7.2 integration)


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


### SignalR Integration Pattern

This story uses SignalR configured in Story 3.1:
- **MVP:** Built-in ASP.NET Core SignalR
- Real-time collaboration updates via SignalR hub
- See Story 3.1 for SignalR configuration pattern

## References
- **Aspire Rules:** [PROJECT-WIDE-RULES.md](../../../PROJECT-WIDE-RULES.md)
- **Aspire Docs:** https://aspire.dev

- Source: [epics.md - Story 7.4](../planning-artifacts/epics.md)
- Architecture: [architecture.md](../planning-artifacts/architecture.md)
- PRD: [prd.md](../planning-artifacts/prd.md)
