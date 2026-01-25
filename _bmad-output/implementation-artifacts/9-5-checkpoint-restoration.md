# Story 9.5: Checkpoint Restoration

**Status:** ready-for-dev

## Story

As a user (Sarah), I want to restore previous checkpoints, so that I can recover from mistakes or explore alternative paths.

## Acceptance Criteria

**Given** a workflow has checkpoints  
**When** I send GET `/api/v1/workflows/{id}/checkpoints`  
**Then** I see all checkpoints with: id, timestamp, stepId, description, canRestore

**Given** I want to restore a checkpoint  
**When** I send POST `/api/v1/workflows/{id}/checkpoints/{checkpointId}/restore`  
**Then** a new workflow branch is created from that checkpoint  
**And** the original workflow is preserved  
**And** I'm redirected to the new branch

**Given** I restore a checkpoint  
**When** the restoration completes  
**Then** workflow state matches the checkpoint exactly  
**And** subsequent events/decisions are cleared in the branch  
**And** I can proceed from that point

**Given** automatic checkpoints exist  
**When** I examine checkpoint frequency  
**Then** checkpoints are created at: each step completion, hourly during active sessions, before risky operations

**Given** checkpoint storage grows  
**When** checkpoints are older than 90 days  
**Then** they are archived (moved to cold storage)  
**And** restoration requires archive retrieval (may be slower)

## Tasks / Subtasks

- [ ] Create WorkflowCheckpoint entity model (AC: 1, 3)
  - [ ] Add properties: Id (Guid), WorkflowInstanceId (Guid), StepId (string), Description (string)
  - [ ] Add Snapshot (JsonDocument) for complete state capture
  - [ ] Add Version (int), CreatedAt (DateTime), CreatedBy (Guid)
  - [ ] Add CheckpointType (enum): Manual, Automatic, BeforeRiskyOperation
  - [ ] Add IsArchived (bool), CanRestore (bool)
- [ ] Create CheckpointType enum (AC: 4)
  - [ ] Define types: Manual, AutomaticStepCompletion, AutomaticHourly, BeforeRiskyOperation
  - [ ] Add XML documentation for each type
- [ ] Create database migration for Checkpoints table (AC: 1)
  - [ ] Use EF Core migrations to create table
  - [ ] Add indexes: workflowInstanceId, createdAt, stepId
  - [ ] Add FK constraint to workflow_instances table
  - [ ] Add JSONB column for snapshot with GIN index
- [ ] Implement CheckpointService (AC: 1, 2, 3, 4)
  - [ ] CreateCheckpointAsync: Capture current workflow state
  - [ ] GetCheckpointsAsync: List all checkpoints for workflow
  - [ ] RestoreCheckpointAsync: Create workflow branch from checkpoint
  - [ ] CanRestoreCheckpoint: Validate if checkpoint is restorable
  - [ ] Capture complete state: WorkflowInstance, artifacts, decisions, current step
- [ ] Implement automatic checkpoint creation (AC: 4)
  - [ ] Hook into WorkflowInstanceService step completion
  - [ ] Create checkpoint after each step completes
  - [ ] Implement background service for hourly checkpoints during active sessions
  - [ ] Add configuration for checkpoint frequency (appsettings.json)
  - [ ] Skip checkpoint if last checkpoint is recent (< 5 minutes)
- [ ] Implement checkpoint restoration with branching (AC: 2, 3)
  - [ ] Create new WorkflowInstance with "(Restored)" suffix
  - [ ] Copy checkpoint snapshot to new workflow state
  - [ ] Link to parent workflow and checkpoint in metadata
  - [ ] Clear events/decisions after checkpoint timestamp
  - [ ] Set new workflow to appropriate status (usually Paused)
  - [ ] Log CheckpointRestored event in both workflows
- [ ] Implement checkpoint archival (AC: 5)
  - [ ] Create background service for checkpoint cleanup
  - [ ] Archive checkpoints older than 90 days (configurable)
  - [ ] Move snapshot to cold storage (filesystem or S3)
  - [ ] Update IsArchived flag in database
  - [ ] Keep metadata for querying
  - [ ] Implement retrieval from archive for restoration
- [ ] Add API endpoints (AC: 1, 2)
  - [ ] GET /api/v1/workflows/{id}/checkpoints (list checkpoints)
  - [ ] POST /api/v1/workflows/{id}/checkpoints (create manual checkpoint)
  - [ ] POST /api/v1/workflows/{id}/checkpoints/{checkpointId}/restore (restore checkpoint)
  - [ ] GET /api/v1/workflows/{id}/branches (list workflow branches)
  - [ ] DELETE /api/v1/checkpoints/{id} (soft delete checkpoint)
- [ ] Add authorization checks
  - [ ] User must own workflow to create/restore checkpoints
  - [ ] Admin can manage all checkpoints
  - [ ] Log all checkpoint operations for audit
- [ ] Write unit tests
  - [ ] Test checkpoint creation with state snapshot
  - [ ] Test checkpoint restoration creates new branch
  - [ ] Test automatic checkpoint triggers
  - [ ] Test checkpoint archival logic
  - [ ] Test CanRestore validation
  - [ ] Test authorization checks
- [ ] Write integration tests
  - [ ] End-to-end checkpoint creation and restoration
  - [ ] Verify restored workflow state matches checkpoint
  - [ ] Test automatic checkpoint creation during workflow execution
  - [ ] Test checkpoint archival and retrieval
  - [ ] Test workflow branching from checkpoint

## Dev Notes

### Architecture Alignment

**Source:** [ARCHITECTURE.MD - State Management, Branching]

- Create entity: `src/bmadServer.ApiService/Models/Checkpoints/WorkflowCheckpoint.cs`
- Create enum: `src/bmadServer.ApiService/Models/Checkpoints/CheckpointType.cs`
- Create service: `src/bmadServer.ApiService/Services/Checkpoints/CheckpointService.cs`
- Create background service: `src/bmadServer.ApiService/Services/Checkpoints/AutomaticCheckpointService.cs`
- Create background service: `src/bmadServer.ApiService/Services/Checkpoints/CheckpointArchivalService.cs`
- Create controller: `src/bmadServer.ApiService/Controllers/CheckpointsController.cs`
- Migration: `src/bmadServer.ApiService/Data/Migrations/XXX_CreateWorkflowCheckpointsTable.cs`

### Technical Requirements

**WorkflowCheckpoint Entity:**
```csharp
public class WorkflowCheckpoint
{
    public Guid Id { get; set; }
    public Guid WorkflowInstanceId { get; set; }
    public WorkflowInstance WorkflowInstance { get; set; }
    
    public string StepId { get; set; }
    public string Description { get; set; }
    public CheckpointType CheckpointType { get; set; }
    
    // Complete state snapshot
    public JsonDocument Snapshot { get; set; }
    public int Version { get; set; }  // Workflow version at checkpoint time
    
    public DateTime CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public User CreatedByUser { get; set; }
    
    // Archival
    public bool IsArchived { get; set; }
    public string? ArchiveLocation { get; set; }
    
    // Computed property
    public bool CanRestore => !IsArchived || ArchiveLocation != null;
}
```

**Checkpoint Snapshot Structure:**
```json
{
  "workflowState": {
    "status": "Running",
    "currentStep": "review-prd",
    "version": 5,
    "state": { /* workflow JSONB state */ }
  },
  "artifacts": [
    { "id": "...", "name": "prd.md", "version": 2 }
  ],
  "decisions": [
    { "id": "...", "decision": "approved", "timestamp": "..." }
  ],
  "context": {
    "lastEventSequence": 42,
    "participantIds": ["user-1", "user-2"]
  }
}
```

**Checkpoint Creation:**
```csharp
public async Task<WorkflowCheckpoint> CreateCheckpointAsync(
    Guid workflowId,
    string description,
    CheckpointType type,
    Guid userId,
    CancellationToken cancellationToken = default)
{
    var workflow = await _workflowService.GetWorkflowInstanceAsync(workflowId, cancellationToken);
    
    // Capture complete state
    var snapshot = await CaptureWorkflowSnapshotAsync(workflow, cancellationToken);
    
    var checkpoint = new WorkflowCheckpoint
    {
        Id = Guid.NewGuid(),
        WorkflowInstanceId = workflowId,
        StepId = workflow.CurrentStepId ?? "initial",
        Description = description,
        CheckpointType = type,
        Snapshot = snapshot,
        Version = workflow.Version,
        CreatedAt = DateTime.UtcNow,
        CreatedBy = userId,
        IsArchived = false
    };
    
    _context.WorkflowCheckpoints.Add(checkpoint);
    await _context.SaveChangesAsync(cancellationToken);
    
    // Log checkpoint creation
    await _eventStore.AppendAsync(new WorkflowEvent
    {
        WorkflowInstanceId = workflowId,
        EventType = WorkflowEventType.CheckpointCreated,
        Payload = JsonSerializer.SerializeToDocument(new
        {
            checkpointId = checkpoint.Id,
            description,
            type
        }),
        UserId = userId
    }, cancellationToken);
    
    _logger.LogInformation(
        "Checkpoint {CheckpointId} created for workflow {WorkflowId} at step {StepId}",
        checkpoint.Id, workflowId, checkpoint.StepId);
    
    return checkpoint;
}

private async Task<JsonDocument> CaptureWorkflowSnapshotAsync(
    WorkflowInstance workflow,
    CancellationToken cancellationToken)
{
    var artifacts = await _artifactService.GetArtifactsForWorkflowAsync(
        workflow.Id, 
        includeVersions: false, 
        cancellationToken);
        
    var lastEvent = await _eventStore.GetLastEventAsync(workflow.Id, cancellationToken);
    
    var snapshot = new
    {
        workflowState = new
        {
            status = workflow.Status.ToString(),
            currentStep = workflow.CurrentStepId,
            version = workflow.Version,
            state = workflow.State
        },
        artifacts = artifacts.Select(a => new
        {
            id = a.Id,
            name = a.Name,
            version = a.Version,
            artifactType = a.ArtifactType.ToString()
        }),
        context = new
        {
            lastEventSequence = lastEvent?.SequenceNumber ?? 0,
            capturedAt = DateTime.UtcNow
        }
    };
    
    return JsonSerializer.SerializeToDocument(snapshot);
}
```

**Checkpoint Restoration with Branching:**
```csharp
public async Task<Guid> RestoreCheckpointAsync(
    Guid checkpointId,
    Guid userId,
    CancellationToken cancellationToken = default)
{
    var checkpoint = await _context.WorkflowCheckpoints
        .Include(c => c.WorkflowInstance)
        .FirstOrDefaultAsync(c => c.Id == checkpointId, cancellationToken);
        
    if (checkpoint == null)
        throw new NotFoundException($"Checkpoint {checkpointId} not found");
        
    if (!checkpoint.CanRestore)
        throw new InvalidOperationException("Checkpoint cannot be restored");
    
    // Retrieve snapshot (from archive if needed)
    var snapshot = checkpoint.IsArchived
        ? await RetrieveFromArchiveAsync(checkpoint.ArchiveLocation!, cancellationToken)
        : checkpoint.Snapshot;
    
    // Create new workflow branch
    var originalWorkflow = checkpoint.WorkflowInstance;
    var branchedWorkflow = new WorkflowInstance
    {
        Id = Guid.NewGuid(),
        Name = $"{originalWorkflow.Name} (Restored from {checkpoint.CreatedAt:yyyy-MM-dd HH:mm})",
        Status = WorkflowStatus.Paused, // Start paused for user to review
        CreatedAt = DateTime.UtcNow,
        CreatedBy = userId,
        Metadata = JsonSerializer.SerializeToDocument(new
        {
            restoredFrom = checkpointId,
            originalWorkflow = originalWorkflow.Id,
            restorePoint = checkpoint.CreatedAt,
            isBranch = true
        })
    };
    
    // Restore state from snapshot
    var snapshotData = snapshot.RootElement.GetProperty("workflowState");
    branchedWorkflow.State = JsonDocument.Parse(snapshotData.GetProperty("state").ToString());
    branchedWorkflow.Version = 1; // New workflow starts at version 1
    branchedWorkflow.CurrentStepId = snapshotData.GetProperty("currentStep").GetString();
    
    _context.WorkflowInstances.Add(branchedWorkflow);
    await _context.SaveChangesAsync(cancellationToken);
    
    // Restore artifacts to new workflow
    var artifactsData = snapshot.RootElement.GetProperty("artifacts");
    foreach (var artifactData in artifactsData.EnumerateArray())
    {
        var originalArtifactId = Guid.Parse(artifactData.GetProperty("id").GetString()!);
        var originalArtifact = await _artifactService.GetArtifactAsync(originalArtifactId, cancellationToken);
        
        // Copy artifact to new workflow
        var content = await _artifactService.DownloadArtifactAsync(originalArtifactId, cancellationToken);
        await _artifactService.CreateArtifactAsync(
            branchedWorkflow.Id,
            originalArtifact.ArtifactType,
            originalArtifact.Name,
            originalArtifact.Format,
            content,
            userId,
            cancellationToken);
    }
    
    // Log restoration events
    await _eventStore.AppendAsync(new WorkflowEvent
    {
        WorkflowInstanceId = originalWorkflow.Id,
        EventType = WorkflowEventType.CheckpointRestored,
        Payload = JsonSerializer.SerializeToDocument(new
        {
            checkpointId,
            newWorkflowId = branchedWorkflow.Id
        }),
        UserId = userId
    }, cancellationToken);
    
    await _eventStore.AppendAsync(new WorkflowEvent
    {
        WorkflowInstanceId = branchedWorkflow.Id,
        EventType = WorkflowEventType.WorkflowBranchedFromCheckpoint,
        Payload = JsonSerializer.SerializeToDocument(new
        {
            checkpointId,
            originalWorkflowId = originalWorkflow.Id
        }),
        UserId = userId
    }, cancellationToken);
    
    _logger.LogInformation(
        "Checkpoint {CheckpointId} restored as new workflow {NewWorkflowId}",
        checkpointId, branchedWorkflow.Id);
    
    return branchedWorkflow.Id;
}
```

**Automatic Checkpoint Background Service:**
```csharp
public class AutomaticCheckpointService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<AutomaticCheckpointService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(60);
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CreateHourlyCheckpointsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating automatic checkpoints");
            }
            
            await Task.Delay(_checkInterval, stoppingToken);
        }
    }
    
    private async Task CreateHourlyCheckpointsAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var checkpointService = scope.ServiceProvider.GetRequiredService<ICheckpointService>();
        
        // Find active workflows without recent checkpoint
        var activeWorkflows = await context.WorkflowInstances
            .Where(w => w.Status == WorkflowStatus.Running || w.Status == WorkflowStatus.WaitingForInput)
            .Where(w => !context.WorkflowCheckpoints
                .Any(c => c.WorkflowInstanceId == w.Id 
                       && c.CreatedAt > DateTime.UtcNow.AddMinutes(-55)))
            .ToListAsync(cancellationToken);
        
        foreach (var workflow in activeWorkflows)
        {
            try
            {
                await checkpointService.CreateCheckpointAsync(
                    workflow.Id,
                    $"Automatic hourly checkpoint",
                    CheckpointType.AutomaticHourly,
                    workflow.CreatedBy,
                    cancellationToken);
                    
                _logger.LogInformation(
                    "Created automatic checkpoint for workflow {WorkflowId}",
                    workflow.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, 
                    "Failed to create automatic checkpoint for workflow {WorkflowId}",
                    workflow.Id);
            }
        }
    }
}
```

**Checkpoint Archival Service:**
```csharp
public class CheckpointArchivalService : BackgroundService
{
    private readonly TimeSpan _checkInterval = TimeSpan.FromDays(1);
    private readonly int _retentionDays = 90;
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ArchiveOldCheckpointsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error archiving checkpoints");
            }
            
            await Task.Delay(_checkInterval, stoppingToken);
        }
    }
    
    private async Task ArchiveOldCheckpointsAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var fileStorage = scope.ServiceProvider.GetRequiredService<IFileStorage>();
        
        var cutoffDate = DateTime.UtcNow.AddDays(-_retentionDays);
        
        var checkpointsToArchive = await context.WorkflowCheckpoints
            .Where(c => !c.IsArchived && c.CreatedAt < cutoffDate)
            .ToListAsync(cancellationToken);
        
        foreach (var checkpoint in checkpointsToArchive)
        {
            try
            {
                // Store snapshot in cold storage
                var snapshotJson = checkpoint.Snapshot.RootElement.ToString();
                using var stream = new MemoryStream(Encoding.UTF8.GetBytes(snapshotJson));
                
                var archiveLocation = await fileStorage.StoreAsync(
                    stream,
                    $"checkpoint_{checkpoint.Id}.json",
                    encrypt: true,
                    cancellationToken);
                
                // Update checkpoint record
                checkpoint.IsArchived = true;
                checkpoint.ArchiveLocation = archiveLocation;
                checkpoint.Snapshot = null!; // Remove from database
                
                _logger.LogInformation(
                    "Archived checkpoint {CheckpointId} to {Location}",
                    checkpoint.Id, archiveLocation);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to archive checkpoint {CheckpointId}",
                    checkpoint.Id);
            }
        }
        
        await context.SaveChangesAsync(cancellationToken);
    }
}
```

### File Structure Requirements

```
src/bmadServer.ApiService/
├── Models/
│   └── Checkpoints/
│       ├── WorkflowCheckpoint.cs (new)
│       └── CheckpointType.cs (new)
├── Services/
│   └── Checkpoints/
│       ├── ICheckpointService.cs (new)
│       ├── CheckpointService.cs (new)
│       ├── AutomaticCheckpointService.cs (new - background service)
│       └── CheckpointArchivalService.cs (new - background service)
├── Controllers/
│   └── CheckpointsController.cs (new)
└── Data/
    └── Migrations/
        └── XXX_CreateWorkflowCheckpointsTable.cs (new)
```

### Dependencies

**From Previous Stories:**
- Story 4.2: WorkflowInstance entity (FK reference)
- Story 9.1: EventStore (log checkpoint events)
- Story 9.2: State storage with versioning
- Story 9.3: ArtifactService (copy artifacts to branch)

**NuGet Packages:**
- System.Text.Json (already in project) - for snapshot serialization
- No additional packages required

### Testing Requirements

**Unit Tests:** `test/bmadServer.Tests/Services/Checkpoints/CheckpointServiceTests.cs`

**Test Coverage:**
- Create checkpoint with state snapshot
- Restore checkpoint creates new branch
- Automatic checkpoint triggers
- Checkpoint archival logic
- CanRestore validation
- Authorization checks

**Integration Tests:** `test/bmadServer.Tests/Integration/Checkpoints/CheckpointRestorationTests.cs`

**Test Coverage:**
- Full checkpoint creation and restoration flow
- Verify restored workflow matches checkpoint
- Automatic checkpoint during workflow execution
- Checkpoint archival and retrieval from archive
- Multiple branch restoration from same checkpoint

### Integration Notes

**Connection to Other Stories:**
- Story 9.2: Checkpoint captures state with version
- Story 9.3: Artifacts restored to branched workflow
- Story 9.4: Checkpoints included in exports
- Story 4.4: Pause/Resume uses checkpoint pattern

**Future Enhancements:**
- Checkpoint comparison (diff between checkpoints)
- Merge branches back to main workflow
- Checkpoint labels and tagging
- Smart checkpoint frequency (more frequent during critical steps)

### Previous Story Intelligence

**From Epic 4 & 9 Stories:**
- Workflows already have state management
- Versioning implemented in Story 9.2
- Branching pattern similar to git branches
- Event sourcing enables point-in-time restoration

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 9.5]
- [Source: ARCHITECTURE.md - State Management, Branching Patterns]
- [Git Branching Pattern: Inspiration for workflow branching]
- [Event Sourcing: Martin Fowler]
