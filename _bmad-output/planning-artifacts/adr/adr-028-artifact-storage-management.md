# ADR-028: Artifact Storage Management Strategy

**Status:** Accepted  
**Date:** 2026-01-28  
**Context:** Epic 9 Story 9-3  
**Deciders:** Winston (Architect), Amelia (Dev)

## Context and Problem Statement

Workflow execution produces artifacts (documents, diagrams, specifications) that must be stored, versioned, and retrieved efficiently. We need to decide between database storage vs. blob storage while maintaining referential integrity.

## Decision Drivers

- Artifacts range from 10KB (markdown) to 5MB (diagrams, exports)
- Need version control for artifact updates
- Must support metadata queries (find all artifacts for workflow X)
- Storage costs and performance considerations
- Integration with PostgreSQL for transactional consistency

## Considered Options

1. **PostgreSQL BYTEA Column** - Store blobs directly in database
2. **File System Storage** - Local filesystem with database metadata
3. **S3-Compatible Blob Storage** - External object storage (MinIO/S3)
4. **Hybrid Strategy** - Small artifacts in DB, large in blob storage

## Decision Outcome

**Chosen: Option 4 - Hybrid Strategy**

Store small artifacts (< 1MB) directly in PostgreSQL for simplicity, larger artifacts in file system with S3-compatible interface for future migration.

### Architecture

```csharp
public class Artifact
{
    public Guid Id { get; set; }
    public Guid WorkflowInstanceId { get; set; }
    public string Name { get; set; }           // "prd.md", "architecture.pdf"
    public string ContentType { get; set; }    // MIME type
    public long SizeBytes { get; set; }
    public string StorageType { get; set; }    // "database" | "filesystem" | "blob"
    public string StorageKey { get; set; }     // Path or blob ID
    public int Version { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Checksum { get; set; }       // SHA256 for integrity
    
    // For small artifacts (< 1MB)
    public byte[]? Content { get; set; }       // Null if stored externally
}
```

### Storage Decision Logic

```csharp
public async Task<Guid> StoreArtifactAsync(
    Guid workflowId, 
    string name, 
    Stream content, 
    string contentType)
{
    var sizeBytes = content.Length;
    var artifact = new Artifact
    {
        Id = Guid.NewGuid(),
        WorkflowInstanceId = workflowId,
        Name = name,
        ContentType = contentType,
        SizeBytes = sizeBytes,
        Checksum = await ComputeSha256Async(content),
        Version = 1,
        CreatedAt = DateTime.UtcNow
    };

    if (sizeBytes <= 1_048_576) // 1MB threshold
    {
        artifact.StorageType = "database";
        artifact.Content = await ReadAllBytesAsync(content);
    }
    else
    {
        artifact.StorageType = "filesystem";
        artifact.StorageKey = await _fileStorage.SaveAsync(
            $"artifacts/{workflowId}/{artifact.Id}", 
            content
        );
    }

    await _context.Artifacts.AddAsync(artifact);
    await _context.SaveChangesAsync();
    
    return artifact.Id;
}
```

### File System Storage Structure

```
{storage_root}/
  artifacts/
    {workflow-id}/
      {artifact-id}.bin
      {artifact-id}.meta.json
  exports/
    {export-id}/
      workflow-state.json
      artifacts.zip
```

### Versioning Strategy

- New version creates new Artifact record with incremented Version
- Previous versions retained (soft delete for space management)
- Query pattern: `WHERE Name = 'prd.md' ORDER BY Version DESC LIMIT 1`

### Lifecycle Management

```csharp
public class ArtifactLifecyclePolicy
{
    public int RetainVersions { get; set; } = 10;  // Keep last 10 versions
    public int ArchiveAfterDays { get; set; } = 90;  // Move old to cold storage
    public int DeleteAfterDays { get; set; } = 365;  // Permanent deletion
}
```

## Implementation Notes

- Implement `IArtifactStorageService` abstraction
- Use `IFileStorageProvider` interface (file system → S3 migration path)
- Background job for artifact cleanup (delete old versions)
- Checksum validation on retrieval
- Support Content-Disposition headers for downloads
- Implement streaming for large artifacts

## Consequences

### Positive
- Small artifacts benefit from ACID guarantees
- Large artifacts don't bloat database
- Clear migration path to S3/blob storage
- Version history maintained automatically

### Negative
- Two storage backends to manage
- Referential integrity only for database-stored artifacts
- File system cleanup required for orphaned files

### Neutral
- 1MB threshold is configurable
- May need periodic integrity checks (checksum validation)

## Related Decisions

- ADR-026: Event Log Architecture
- ADR-029: Workflow Export/Import Format
- ADR-030: Checkpoint & Restoration Guarantees

## References

- Epic 9 Story 9-3: artifact-storage-management
- PostgreSQL BYTEA vs. filesystem storage comparison
- S3-compatible storage APIs (MinIO, AWS S3)
