# ADR-030: Checkpoint & Restoration Guarantees

**Status:** Accepted  
**Date:** 2026-01-28  
**Context:** Epic 9 Story 9-5  
**Deciders:** Winston (Architect), Amelia (Dev)

## Context and Problem Statement

Users need ability to save workflow progress at specific points (checkpoints) and restore to those points if needed. We must define checkpoint semantics, storage strategy, and restoration guarantees while maintaining data integrity.

## Decision Drivers

- Users may want to "undo" workflow progress
- System crashes should not lose significant work
- Checkpoints must capture complete restorable state
- Performance impact of checkpoint creation must be minimal
- Storage costs for multiple checkpoints per workflow

## Considered Options

1. **Auto-Checkpoint Every Step** - Automatic checkpointing at each step boundary
2. **Manual Checkpoints Only** - User explicitly creates checkpoints
3. **Hybrid Approach** - Auto-save + user-triggered named checkpoints
4. **Git-like Branching** - Checkpoint creates branch, restore switches branches

## Decision Outcome

**Chosen: Option 3 - Hybrid Approach**

Automatically checkpoint at critical workflow boundaries, plus support user-triggered named checkpoints for intentional save points.

### Checkpoint Entity

```csharp
public class WorkflowCheckpoint
{
    public Guid Id { get; set; }
    public Guid WorkflowInstanceId { get; set; }
    public string Name { get; set; }              // "Before requirements review"
    public CheckpointType Type { get; set; }      // Auto | Manual | System
    public DateTime CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public string Description { get; set; }
    
    // Snapshot of complete state at checkpoint time
    public JsonDocument WorkflowState { get; set; }  // Full state snapshot
    public int StepIndex { get; set; }              // Which step
    public string CurrentAgent { get; set; }         // Active agent
    
    // References to related data at checkpoint time
    public List<Guid> DecisionIds { get; set; }     // Decisions at this point
    public List<Guid> ArtifactIds { get; set; }     // Artifacts at this point
    
    public bool IsRestorePoint { get; set; }        // Can restore to this?
    public DateTime? RestoredAt { get; set; }       // If restored, when?
}

public enum CheckpointType
{
    Auto,       // System-generated at step boundaries
    Manual,     // User-triggered save point
    System,     // Pre-restore safety checkpoint
    Conflict    // Created before merge/conflict resolution
}
```

### Auto-Checkpoint Triggers

**Checkpoint automatically created when:**
- Step completion (every workflow step)
- Decision locked (before locking critical decisions)
- Before conflict resolution
- Before workflow state changes (pause → resume)
- Agent handoff (switching between agents)

**Not checkpointed:**
- Typing indicators
- Presence updates
- Chat messages without state changes

### Checkpoint Creation Strategy

```csharp
public async Task<Guid> CreateCheckpointAsync(
    Guid workflowId,
    string name,
    CheckpointType type,
    Guid userId)
{
    // Load current workflow state
    var workflow = await _context.WorkflowInstances
        .Include(w => w.Decisions)
        .Include(w => w.Artifacts)
        .FirstOrDefaultAsync(w => w.Id == workflowId);
    
    // Create checkpoint snapshot
    var checkpoint = new WorkflowCheckpoint
    {
        Id = Guid.NewGuid(),
        WorkflowInstanceId = workflowId,
        Name = name ?? GenerateAutoCheckpointName(workflow),
        Type = type,
        CreatedAt = DateTime.UtcNow,
        CreatedBy = userId,
        WorkflowState = workflow.State,  // Deep copy
        StepIndex = workflow.CurrentStepIndex,
        CurrentAgent = workflow.CurrentAgent,
        DecisionIds = workflow.Decisions.Select(d => d.Id).ToList(),
        ArtifactIds = workflow.Artifacts.Select(a => a.Id).ToList(),
        IsRestorePoint = true
    };
    
    await _context.WorkflowCheckpoints.AddAsync(checkpoint);
    await _context.SaveChangesAsync();
    
    // Log checkpoint event
    await _auditService.LogEventAsync(new AuditEvent
    {
        EventType = "CheckpointCreated",
        EntityId = workflowId,
        EventData = JsonSerializer.SerializeToDocument(checkpoint)
    });
    
    return checkpoint.Id;
}
```

### Restoration Strategy

```csharp
public async Task<RestoreResult> RestoreCheckpointAsync(
    Guid workflowId,
    Guid checkpointId,
    RestoreOptions options)
{
    // 1. Create safety checkpoint of current state
    await CreateCheckpointAsync(
        workflowId, 
        "Before restore", 
        CheckpointType.System, 
        options.UserId
    );
    
    // 2. Load checkpoint
    var checkpoint = await _context.WorkflowCheckpoints
        .FirstOrDefaultAsync(c => c.Id == checkpointId);
    
    if (!checkpoint.IsRestorePoint)
        throw new InvalidOperationException("Checkpoint is not a restore point");
    
    // 3. Restore workflow state
    var workflow = await _context.WorkflowInstances
        .FirstOrDefaultAsync(w => w.Id == workflowId);
    
    workflow.State = checkpoint.WorkflowState;  // Restore state
    workflow.CurrentStepIndex = checkpoint.StepIndex;
    workflow.CurrentAgent = checkpoint.CurrentAgent;
    workflow.UpdatedAt = DateTime.UtcNow;
    workflow.Version++;  // Increment for concurrency
    
    // 4. Handle decisions
    if (options.RestoreDecisions)
    {
        // Mark decisions created after checkpoint as "superseded"
        var laterDecisions = await _context.Decisions
            .Where(d => d.WorkflowId == workflowId 
                     && d.CreatedAt > checkpoint.CreatedAt)
            .ToListAsync();
        
        foreach (var decision in laterDecisions)
        {
            decision.Status = DecisionStatus.Superseded;
            decision.SupersededAt = DateTime.UtcNow;
        }
    }
    
    // 5. Handle artifacts
    if (options.RestoreArtifacts)
    {
        // Revert artifacts to checkpoint versions
        await RevertArtifactsAsync(workflowId, checkpoint.ArtifactIds);
    }
    
    // 6. Save and log
    checkpoint.RestoredAt = DateTime.UtcNow;
    await _context.SaveChangesAsync();
    
    await _auditService.LogEventAsync(new AuditEvent
    {
        EventType = "CheckpointRestored",
        EntityId = workflowId,
        EventData = JsonSerializer.SerializeToDocument(new {
            CheckpointId = checkpointId,
            RestoredBy = options.UserId
        })
    });
    
    return new RestoreResult { Success = true };
}
```

### Restoration Guarantees

**What is restored:**
✅ Workflow state (current step, variables, agent context)
✅ Step history (reverted to checkpoint point)
✅ Current agent assignment

**What is optionally restored:**
⚙️ Decisions (can mark later decisions as superseded)
⚙️ Artifacts (can revert to checkpoint versions)

**What is NOT restored:**
❌ Audit events (immutable log)
❌ User messages/chat history (preserved for context)
❌ Collaboration participants (users remain in workflow)

### Storage & Retention

- **Retention:** Keep last 20 checkpoints per workflow (configurable)
- **Auto-cleanup:** Delete checkpoints older than 90 days (except manual)
- **Manual checkpoints:** Retained indefinitely unless explicitly deleted
- **Storage optimization:** Share identical state snapshots via deduplication

## Implementation Notes

- Implement `ICheckpointService` for checkpoint operations
- Background job for checkpoint cleanup
- UI indicator for available restore points
- Confirmation dialog before restoration
- Support "compare checkpoints" feature (diff viewer)
- Add checkpoint count to workflow metadata

## Consequences

### Positive
- Complete workflow state recovery
- Safety net for experimentation
- Debugging support (inspect state at any checkpoint)
- User confidence in making changes

### Negative
- Storage overhead for state snapshots
- Complexity in handling post-checkpoint decisions
- Potential confusion about "undo" semantics

### Neutral
- Restoration is not instant (requires page reload)
- Some data deliberately not restored (audit trail)

## Related Decisions

- ADR-027: JSONB State Storage Strategy
- ADR-029: Workflow Export/Import Format
- ADR-026: Event Log Architecture

## References

- Epic 9 Story 9-5: checkpoint-restoration
- Git checkpoint/revert semantics
- Event sourcing snapshot patterns
