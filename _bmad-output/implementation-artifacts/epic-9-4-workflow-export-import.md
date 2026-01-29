# Story 9.4: Workflow Export/Import

**Story ID:** E9-S4  
**Epic:** Epic 9 - Data Persistence & State Management  
**Points:** 8  
**Priority:** MEDIUM  
**ADR Reference:** [ADR-029: Workflow Export/Import Format](../planning-artifacts/adr/adr-029-workflow-export-import-format.md)

## User Story

As a user, I want to export and import complete workflows, so that I can backup my work, migrate between environments, or share workflows with others.

## Acceptance Criteria

**Given** I have a workflow in progress  
**When** I export the workflow  
**Then** I receive a ZIP archive containing manifest.json, state.json, and all artifacts  
**And** the export file is named `workflow-export-{id}-{timestamp}.zip`

**Given** I have an exported workflow ZIP  
**When** I import the workflow  
**Then** a new workflow instance is created with a new ID  
**And** all state, decisions, and artifacts are restored  
**And** user references are remapped to current user

**Given** the export schema version is older than current  
**When** I import the workflow  
**Then** schema migration is applied automatically  
**And** the import succeeds with migrated data

## Tasks

- [ ] Implement `IWorkflowExportService` interface
- [ ] Implement ZIP archive creation with System.IO.Compression
- [ ] Create manifest.json builder with schema versioning
- [ ] Implement state.json serialization
- [ ] Implement history.json serialization (optional)
- [ ] Implement decisions.json serialization (optional)
- [ ] Implement artifact packaging in ZIP
- [ ] Add checksum validation for artifacts
- [ ] Implement `IWorkflowImportService` interface
- [ ] Implement ZIP archive reading and validation
- [ ] Implement manifest schema version compatibility check
- [ ] Implement workflow instance creation from import
- [ ] Implement user ID remapping logic
- [ ] Implement decision import (optional based on options)
- [ ] Implement artifact extraction and storage
- [ ] Create `IExportFormatMigrator` interface for versioning
- [ ] Add export API endpoint `GET /api/v1/workflows/{id}/export`
- [ ] Add import API endpoint `POST /api/v1/workflows/import`
- [ ] Add export options query parameters
- [ ] Add size limit validation (100MB default)
- [ ] Add unit tests for export generation
- [ ] Add unit tests for import parsing
- [ ] Add integration tests for round-trip export/import
- [ ] Add tests for schema migration

## Files to Create

- `src/bmadServer.ApiService/Services/WorkflowExportService.cs`
- `src/bmadServer.ApiService/Services/WorkflowImportService.cs`
- `src/bmadServer.ApiService/Services/Export/IExportFormatMigrator.cs`
- `src/bmadServer.ApiService/Models/WorkflowExportOptions.cs`
- `src/bmadServer.ApiService/Models/WorkflowImportOptions.cs`
- `src/bmadServer.ApiService/Models/Export/ExportManifest.cs`

## Files to Modify

- `src/bmadServer.ApiService/Controllers/WorkflowsController.cs` - Add export/import endpoints
- `src/bmadServer.ApiService/Program.cs` - Register export/import services
- `appsettings.json` - Add export size limits configuration

## Testing Checklist

- [ ] Unit test: Export generates valid ZIP structure
- [ ] Unit test: Manifest includes all required fields
- [ ] Unit test: Artifact checksums calculated correctly
- [ ] Unit test: Import parses valid ZIP successfully
- [ ] Unit test: Import validates schema version
- [ ] Unit test: User ID remapping works correctly
- [ ] Integration test: Export workflow with artifacts
- [ ] Integration test: Import exported workflow creates new instance
- [ ] Integration test: Round-trip export/import preserves state
- [ ] Integration test: Import with older schema version migrates
- [ ] Integration test: Export with options excludes optional data
- [ ] Integration test: Import large workflow (50MB) succeeds
- [ ] Performance test: Export completes under 5s for typical workflow
- [ ] Performance test: Import completes under 10s for typical workflow

## Definition of Done

- [ ] All acceptance criteria met
- [ ] All tasks completed
- [ ] All tests passing
- [ ] Code review completed
- [ ] Documentation updated
- [ ] ADR-029 implementation verified
- [ ] Export/import user guide created
- [ ] Story demonstrated to stakeholders
- [ ] Merged to main branch

## Notes

- Export format version: 1.0
- Schema evolution requires migrator implementations
- Consider streaming for very large exports
- Audit log all export/import operations
- Consider encryption option for sensitive workflows
