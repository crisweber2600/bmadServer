# Story 9.5: Checkpoint Restoration

**Story ID:** E9-S5  
**Epic:** Epic 9 - Data Persistence & State Management  
**Points:** 8  
**Priority:** MEDIUM  
**ADR Reference:** [ADR-030: Checkpoint & Restoration Guarantees](../planning-artifacts/adr/adr-030-checkpoint-restoration-guarantees.md)

## User Story

As a user, I want to create checkpoints of workflow progress and restore to them, so that I can safely experiment and undo changes if needed.

## Acceptance Criteria

**Given** I am working on a workflow  
**When** I complete a step  
**Then** an automatic checkpoint is created  
**And** the checkpoint captures complete workflow state

**Given** I want to save my progress intentionally  
**When** I create a manual checkpoint with a name  
**Then** a named checkpoint is created  
**And** it is marked as a restore point

**Given** I want to undo recent changes  
**When** I restore to a previous checkpoint  
**Then** workflow state is reverted to that point  
**And** a safety checkpoint of current state is created first  
**And** decisions created after that checkpoint are marked as superseded

**Given** I view checkpoint history  
**When** I access the checkpoints list  
**Then** I see all checkpoints with names, timestamps, and types  
**And** I can compare checkpoints (diff viewer)

## Tasks

- [ ] Verify WorkflowCheckpoint entity exists (already in codebase)
- [ ] Implement `ICheckpointService` interface
- [ ] Implement auto-checkpoint on step completion
- [ ] Implement auto-checkpoint on decision lock
- [ ] Implement auto-checkpoint before conflict resolution
- [ ] Implement manual checkpoint creation
- [ ] Implement checkpoint naming logic
- [ ] Implement restoration with safety checkpoint
- [ ] Implement decision superseding logic
- [ ] Implement artifact version reversion
- [ ] Implement user ID remapping on restore
- [ ] Add checkpoint creation API `POST /api/v1/workflows/{id}/checkpoints`
- [ ] Add checkpoint list API `GET /api/v1/workflows/{id}/checkpoints`
- [ ] Add restore API `POST /api/v1/workflows/{id}/checkpoints/{checkpointId}/restore`
- [ ] Add checkpoint comparison API `GET /api/v1/checkpoints/{id1}/compare/{id2}`
- [ ] Create checkpoint cleanup background job
- [ ] Add unit tests for checkpoint creation
- [ ] Add unit tests for restoration logic
- [ ] Add integration tests for auto-checkpoints
- [ ] Add integration tests for manual checkpoints
- [ ] Add integration tests for restoration
- [ ] Add integration tests for decision superseding

## Files to Create

- `src/bmadServer.ApiService/Services/CheckpointService.cs`
- `src/bmadServer.ApiService/Controllers/CheckpointsController.cs`
- `src/bmadServer.ApiService/Models/CheckpointDto.cs`
- `src/bmadServer.ApiService/Models/RestoreOptions.cs`
- `src/bmadServer.ApiService/Jobs/CheckpointCleanupJob.cs`

## Files to Modify

- `src/bmadServer.ApiService/Services/Workflows/WorkflowOrchestrator.cs` - Add auto-checkpoint triggers
- `src/bmadServer.ApiService/Services/DecisionService.cs` - Add checkpoint before lock
- `src/bmadServer.ApiService/Program.cs` - Register ICheckpointService
- `appsettings.json` - Add checkpoint retention configuration

## Testing Checklist

- [ ] Unit test: Auto-checkpoint created on step completion
- [ ] Unit test: Manual checkpoint with custom name
- [ ] Unit test: Safety checkpoint created before restore
- [ ] Unit test: Restoration reverts state correctly
- [ ] Unit test: Decision superseding marks later decisions
- [ ] Integration test: Checkpoint workflow at step 3
- [ ] Integration test: Restore to step 2 checkpoint
- [ ] Integration test: Verify state restored correctly
- [ ] Integration test: Verify decisions superseded after restore point
- [ ] Integration test: Multiple restores create safety checkpoints
- [ ] Integration test: Checkpoint cleanup removes old auto-checkpoints
- [ ] Integration test: Manual checkpoints retained indefinitely
- [ ] UI test: Checkpoint UI shows restore points
- [ ] UI test: Restore confirmation dialog appears

## Definition of Done

- [ ] All acceptance criteria met
- [ ] All tasks completed
- [ ] All tests passing
- [ ] Code review completed
- [ ] Documentation updated
- [ ] ADR-030 implementation verified
- [ ] User guide for checkpoints created
- [ ] Story demonstrated to stakeholders
- [ ] Merged to main branch

## Notes

- WorkflowCheckpoint entity already exists in codebase
- Retention: Keep last 20 checkpoints per workflow
- Manual checkpoints retained indefinitely
- Consider deduplication for identical state snapshots
- UI should clearly indicate restore points vs history checkpoints
- Restoration is not instant - requires confirmation and page reload
