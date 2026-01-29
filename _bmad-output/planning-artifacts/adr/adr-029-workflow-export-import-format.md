# ADR-029: Workflow Export/Import Format

**Status:** Accepted  
**Date:** 2026-01-28  
**Context:** Epic 9 Story 9-4  
**Deciders:** Winston (Architect), John (PM), Amelia (Dev)

## Context and Problem Statement

Users need ability to export workflow state for backup, migration, or sharing purposes, and import workflows to restore or clone them. We need a portable, versionable format that captures complete workflow context.

## Decision Drivers

- Must be human-readable for debugging and auditing
- Version tolerance for format evolution
- Must include workflow state, artifacts, history, decisions
- Size efficiency for network transfer
- Cross-platform compatibility

## Considered Options

1. **JSON Format** - Human-readable, widely supported
2. **Protocol Buffers** - Efficient binary format
3. **ZIP Archive with JSON Manifest** - Multiple files in container
4. **Custom Binary Format** - Maximum efficiency

## Decision Outcome

**Chosen: Option 3 - ZIP Archive with JSON Manifest**

Export workflow as a ZIP archive containing JSON manifest plus artifact files, providing best balance of readability, efficiency, and structure.

### Export Package Structure

```
workflow-export-{workflow-id}-{timestamp}.zip
├── manifest.json          # Workflow metadata and structure
├── state.json            # Current workflow state (JSONB)
├── history.json          # Workflow events and step history
├── decisions.json        # All decisions with versions
├── participants.json     # Collaboration participants
├── artifacts/
│   ├── prd.md
│   ├── architecture.md
│   └── diagram.png
└── metadata/
    ├── export-info.json  # Export metadata
    └── schema-version.txt
```

### Manifest Schema

```json
{
  "exportVersion": "1.0",
  "exportedAt": "2026-01-28T10:00:00Z",
  "exportedBy": "user-guid",
  "schemaVersion": "1.0",
  "workflow": {
    "id": "workflow-guid",
    "definitionId": "prd-creation",
    "status": "InProgress",
    "createdAt": "2026-01-20T10:00:00Z",
    "updatedAt": "2026-01-28T09:55:00Z"
  },
  "contents": {
    "state": "state.json",
    "history": "history.json",
    "decisions": "decisions.json",
    "participants": "participants.json",
    "artifacts": [
      {
        "id": "artifact-guid",
        "name": "prd.md",
        "path": "artifacts/prd.md",
        "contentType": "text/markdown",
        "sizeBytes": 45678,
        "checksum": "sha256:abc123..."
      }
    ]
  },
  "references": {
    "userId": "user-guid",
    "userEmail": "user@example.com",
    "participantIds": ["user-guid-2", "user-guid-3"]
  }
}
```

### Export API

```csharp
public class WorkflowExportService
{
    public async Task<byte[]> ExportWorkflowAsync(
        Guid workflowId, 
        WorkflowExportOptions options)
    {
        // Load workflow data
        var workflow = await LoadWorkflowWithRelatedDataAsync(workflowId);
        
        // Create in-memory ZIP
        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            // Add manifest
            await AddManifestAsync(archive, workflow);
            
            // Add state
            await AddStateAsync(archive, workflow.State);
            
            // Add history
            if (options.IncludeHistory)
                await AddHistoryAsync(archive, workflow.Id);
            
            // Add decisions
            if (options.IncludeDecisions)
                await AddDecisionsAsync(archive, workflow.Id);
            
            // Add artifacts
            if (options.IncludeArtifacts)
                await AddArtifactsAsync(archive, workflow.Id);
        }
        
        return memoryStream.ToArray();
    }
}
```

### Import Strategy

```csharp
public class WorkflowImportService
{
    public async Task<Guid> ImportWorkflowAsync(
        byte[] exportData, 
        WorkflowImportOptions options)
    {
        using var zipStream = new MemoryStream(exportData);
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);
        
        // 1. Validate manifest and schema version
        var manifest = await ReadManifestAsync(archive);
        ValidateSchemaCompatibility(manifest.SchemaVersion);
        
        // 2. Create new workflow instance
        var newWorkflowId = Guid.NewGuid();
        
        // 3. Import state
        var state = await ReadStateAsync(archive);
        await CreateWorkflowInstanceAsync(newWorkflowId, manifest, state);
        
        // 4. Import decisions (optional)
        if (options.ImportDecisions)
            await ImportDecisionsAsync(archive, newWorkflowId);
        
        // 5. Import artifacts
        if (options.ImportArtifacts)
            await ImportArtifactsAsync(archive, newWorkflowId);
        
        // 6. Remap user references
        await RemapUserReferencesAsync(newWorkflowId, manifest.References);
        
        return newWorkflowId;
    }
}
```

### Versioning & Compatibility

**Schema Versioning:**
- Major version change = breaking format change (requires migration)
- Minor version change = backward compatible additions
- Export includes schema version for validation on import

**Migration Strategy:**
```csharp
public interface IExportFormatMigrator
{
    bool CanMigrate(string fromVersion, string toVersion);
    Task<ZipArchive> MigrateAsync(ZipArchive oldFormat);
}
```

## Implementation Notes

- Use `System.IO.Compression.ZipArchive` (.NET built-in)
- Implement `IWorkflowExportService` and `IWorkflowImportService`
- Export endpoint: `GET /api/v1/workflows/{id}/export`
- Import endpoint: `POST /api/v1/workflows/import`
- Add validation for export size limits (100MB default)
- Support streaming for large exports
- Audit log all export/import operations

## Consequences

### Positive
- Human-readable JSON for debugging
- Artifacts included directly (no external dependencies)
- Version tolerance through schema versioning
- Cross-platform ZIP format
- Easy to inspect with standard tools

### Negative
- Larger than pure binary format
- ZIP compression overhead for small workflows
- Must handle user ID remapping on import

### Neutral
- Schema evolution requires migration code
- File size proportional to artifact count

## Related Decisions

- ADR-027: JSONB State Storage Strategy
- ADR-028: Artifact Storage Management
- ADR-030: Checkpoint & Restoration Guarantees

## References

- Epic 9 Story 9-4: workflow-export-import
- JSON Schema specification
- ZIP file format specification (RFC 1950, RFC 1951)
