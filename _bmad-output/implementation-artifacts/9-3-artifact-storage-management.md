# Story 9.3: Artifact Storage & Management

**Status:** ready-for-dev

## Story

As a user (Sarah), I want workflow artifacts stored securely, so that I can access generated documents and outputs.

## Acceptance Criteria

**Given** a workflow generates an artifact (PRD, architecture doc, etc.)  
**When** the artifact is created  
**Then** it is stored in the Artifacts table with: id, workflowInstanceId, artifactType, content, format, createdAt, createdBy

**Given** artifacts may be large  
**When** content exceeds 1MB  
**Then** content is stored in object storage (filesystem MVP, S3-compatible later)  
**And** the Artifacts table stores a reference/path

**Given** I query artifacts  
**When** I send GET `/api/v1/workflows/{id}/artifacts`  
**Then** I receive artifact metadata (not content) for listing  
**And** I can download specific artifacts via GET `/api/v1/artifacts/{id}/download`

**Given** I want artifact history  
**When** I query with includeVersions=true  
**Then** I see all versions of each artifact  
**And** I can download any previous version

**Given** artifacts contain sensitive data  
**When** stored at rest  
**Then** encryption is applied (AES-256)  
**And** decryption happens transparently on retrieval

## Tasks / Subtasks

- [ ] Create Artifact entity model (AC: 1, 2)
  - [ ] Add properties: Id (Guid), WorkflowInstanceId (Guid), ArtifactType (enum), Name (string), Format (string)
  - [ ] Add ContentSize (long), StorageLocation (string), IsExternal (bool)
  - [ ] Add Version (int), CreatedAt (DateTime), CreatedBy (Guid)
  - [ ] Add ContentHash (string - SHA256) for integrity verification
  - [ ] Add EncryptionKeyId (string) for encrypted artifacts
- [ ] Create ArtifactType enum (AC: 1)
  - [ ] Define types: PRD, Architecture, TechnicalSpec, UserStory, TestPlan, Decision, GeneratedCode, Report, Other
  - [ ] Add XML documentation for each type
- [ ] Create database migration for Artifacts table (AC: 1)
  - [ ] Use EF Core migrations to create table
  - [ ] Add indexes: workflowInstanceId, artifactType, createdAt
  - [ ] Add FK constraint to workflow_instances table
- [ ] Implement IArtifactStorage abstraction (AC: 2, 5)
  - [ ] Create IFileStorage interface with: StoreAsync, RetrieveAsync, DeleteAsync
  - [ ] Create FileSystemStorage implementation for MVP
  - [ ] Add configuration for storage path (appsettings.json)
  - [ ] Implement AES-256 encryption/decryption in storage layer
  - [ ] Use IDataProtectionProvider for key management
- [ ] Implement ArtifactService (AC: 1, 2, 3)
  - [ ] CreateArtifactAsync: Store metadata in DB, content in storage
  - [ ] GetArtifactMetadataAsync: Return metadata without content
  - [ ] GetArtifactsForWorkflowAsync: List all artifacts for workflow
  - [ ] DownloadArtifactAsync: Retrieve and decrypt content
  - [ ] Add threshold logic: < 1MB in DB (JSONB), >= 1MB in external storage
  - [ ] Calculate and verify SHA256 hash on upload/download
- [ ] Add artifact versioning (AC: 4)
  - [ ] Track version number per artifact name + workflow
  - [ ] GetArtifactVersionsAsync: Return all versions of artifact
  - [ ] DownloadArtifactVersionAsync: Download specific version
  - [ ] Add IsLatestVersion boolean to metadata
- [ ] Add encryption support (AC: 5)
  - [ ] Use ASP.NET Core Data Protection API
  - [ ] Generate unique encryption key per artifact
  - [ ] Store EncryptionKeyId in database
  - [ ] Implement transparent decryption on download
  - [ ] Add configuration for encryption: Enabled/Disabled
- [ ] Create API endpoints (AC: 3)
  - [ ] POST /api/v1/workflows/{workflowId}/artifacts (create artifact)
  - [ ] GET /api/v1/workflows/{workflowId}/artifacts (list metadata)
  - [ ] GET /api/v1/artifacts/{id}/download (download content)
  - [ ] GET /api/v1/artifacts/{id}/versions (list versions)
  - [ ] GET /api/v1/artifacts/{id}/versions/{version}/download (download specific version)
  - [ ] DELETE /api/v1/artifacts/{id} (soft delete)
- [ ] Add authorization checks (AC: 5)
  - [ ] User must own workflow or have read access
  - [ ] Admin can access all artifacts
  - [ ] Log all artifact access for audit trail
- [ ] Write unit tests
  - [ ] Test artifact creation with small content (< 1MB)
  - [ ] Test artifact creation with large content (>= 1MB)
  - [ ] Test artifact versioning
  - [ ] Test encryption/decryption
  - [ ] Test hash verification
  - [ ] Test authorization rules
- [ ] Write integration tests
  - [ ] End-to-end artifact upload and download
  - [ ] Verify content integrity with hash
  - [ ] Test version listing and retrieval
  - [ ] Test encryption at rest
  - [ ] Test unauthorized access prevention

## Dev Notes

### Architecture Alignment

**Source:** [ARCHITECTURE.MD - File Storage, Security]

- Create entity: `src/bmadServer.ApiService/Models/Artifacts/Artifact.cs`
- Create enum: `src/bmadServer.ApiService/Models/Artifacts/ArtifactType.cs`
- Create interface: `src/bmadServer.ApiService/Services/Storage/IFileStorage.cs`
- Create service: `src/bmadServer.ApiService/Services/Storage/FileSystemStorage.cs`
- Create service: `src/bmadServer.ApiService/Services/Artifacts/ArtifactService.cs`
- Create controller: `src/bmadServer.ApiService/Controllers/ArtifactsController.cs`
- Migration: `src/bmadServer.ApiService/Migrations/XXX_CreateArtifactsTable.cs`

### Technical Requirements

**Artifact Entity:**
```csharp
public class Artifact
{
    public Guid Id { get; set; }
    public Guid WorkflowInstanceId { get; set; }
    public WorkflowInstance WorkflowInstance { get; set; }
    
    public ArtifactType ArtifactType { get; set; }
    public string Name { get; set; }
    public string Format { get; set; }  // e.g., "markdown", "json", "pdf"
    
    public long ContentSize { get; set; }
    public string StorageLocation { get; set; }  // DB or file path
    public bool IsExternal { get; set; }  // true if stored outside DB
    
    public int Version { get; set; }
    public bool IsLatestVersion { get; set; }
    
    public string ContentHash { get; set; }  // SHA256
    public string? EncryptionKeyId { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    
    // Navigation property - User is in bmadServer.ApiService.Data.Entities namespace
    public bmadServer.ApiService.Data.Entities.User? CreatedByUser { get; set; }
    
    // For small artifacts (< 1MB) - stored inline
    public byte[]? ContentData { get; set; }
}
```

**Storage Abstraction:**
```csharp
public interface IFileStorage
{
    Task<string> StoreAsync(
        Stream content, 
        string fileName, 
        bool encrypt = true, 
        CancellationToken cancellationToken = default);
        
    Task<Stream> RetrieveAsync(
        string location, 
        bool decrypt = true, 
        CancellationToken cancellationToken = default);
        
    Task DeleteAsync(
        string location, 
        CancellationToken cancellationToken = default);
        
    Task<bool> ExistsAsync(
        string location, 
        CancellationToken cancellationToken = default);
}

public class FileSystemStorage : IFileStorage
{
    private readonly IDataProtectionProvider _dataProtection;
    private readonly string _basePath;
    
    public async Task<string> StoreAsync(
        Stream content, 
        string fileName, 
        bool encrypt = true, 
        CancellationToken cancellationToken = default)
    {
        var relativePath = Path.Combine(
            DateTime.UtcNow.ToString("yyyy/MM/dd"),
            $"{Guid.NewGuid()}_{fileName}");
            
        var fullPath = Path.Combine(_basePath, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        
        await using var fileStream = File.Create(fullPath);
        
        if (encrypt)
        {
            // Encrypt stream using Data Protection API
            // Note: ASP.NET Core Data Protection doesn't have built-in stream encryption
            // We need to read, encrypt chunks, and write
            var protector = _dataProtection.CreateProtector("ArtifactStorage");
            
            using var ms = new MemoryStream();
            await content.CopyToAsync(ms, cancellationToken);
            var plainBytes = ms.ToArray();
            var encryptedBytes = protector.Protect(plainBytes);
            await fileStream.WriteAsync(encryptedBytes, cancellationToken);
        }
        else
        {
            await content.CopyToAsync(fileStream, cancellationToken);
        }
        
        return relativePath;
    }
}
```

**ArtifactService Pattern:**
```csharp
public class ArtifactService : IArtifactService
{
    private const long MaxInlineSize = 1_048_576; // 1MB
    
    public async Task<Artifact> CreateArtifactAsync(
        Guid workflowId,
        ArtifactType type,
        string name,
        string format,
        Stream content,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        // Verify stream is seekable for length and position operations
        if (!content.CanSeek)
        {
            throw new NotSupportedException("Artifact content stream must be seekable.");
        }
        
        // Calculate hash (this will read through the stream)
        var hash = await ComputeHashAsync(content);
        
        // Reset stream position after hash computation
        content.Position = 0;
        
        var size = content.Length;
        
        // Determine storage location
        string? storageLocation = null;
        byte[]? inlineData = null;
        bool isExternal = false;
        
        if (size < MaxInlineSize)
        {
            // Store inline in database
            using var ms = new MemoryStream();
            await content.CopyToAsync(ms, cancellationToken);
            inlineData = ms.ToArray();
            storageLocation = "inline";
        }
        else
        {
            // Store in external storage
            storageLocation = await _fileStorage.StoreAsync(
                content, 
                $"{name}.{format}", 
                encrypt: true, 
                cancellationToken);
            isExternal = true;
        }
        
        // Get current version for this artifact name
        var currentVersion = await GetLatestVersionAsync(workflowId, name, cancellationToken);
        
        // Mark previous version as not latest
        if (currentVersion != null)
        {
            currentVersion.IsLatestVersion = false;
            _context.Update(currentVersion);
        }
        
        // Create artifact record
        var artifact = new Artifact
        {
            Id = Guid.NewGuid(),
            WorkflowInstanceId = workflowId,
            ArtifactType = type,
            Name = name,
            Format = format,
            ContentSize = size,
            StorageLocation = storageLocation,
            IsExternal = isExternal,
            ContentData = inlineData,
            ContentHash = hash,
            Version = (currentVersion?.Version ?? 0) + 1,
            IsLatestVersion = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId
        };
        
        _context.Artifacts.Add(artifact);
        await _context.SaveChangesAsync(cancellationToken);
        
        // Log artifact creation event
        await _eventStore.AppendAsync(new WorkflowEvent
        {
            WorkflowInstanceId = workflowId,
            EventType = WorkflowEventType.ArtifactCreated,
            Payload = JsonSerializer.SerializeToDocument(new { 
                artifactId = artifact.Id, 
                name, 
                type, 
                version = artifact.Version 
            }),
            UserId = userId
        }, cancellationToken);
        
        return artifact;
    }
    
    private async Task<string> ComputeHashAsync(Stream stream)
    {
        // Save current position to restore later
        var originalPosition = stream.Position;
        
        using var sha256 = SHA256.Create();
        var hashBytes = await sha256.ComputeHashAsync(stream);
        
        // Restore stream position after hashing
        stream.Position = originalPosition;
        
        return Convert.ToBase64String(hashBytes);
    }
}
```

**API Controller:**
```csharp
[HttpGet("{workflowId}/artifacts")]
public async Task<IActionResult> GetArtifacts(
    Guid workflowId,
    [FromQuery] bool includeVersions = false)
{
    var artifacts = await _artifactService.GetArtifactsForWorkflowAsync(
        workflowId, 
        includeVersions);
        
    return Ok(artifacts.Select(a => new
    {
        a.Id,
        a.Name,
        a.ArtifactType,
        a.Format,
        a.ContentSize,
        a.Version,
        a.IsLatestVersion,
        a.CreatedAt,
        DownloadUrl = Url.Action("Download", new { id = a.Id })
    }));
}

[HttpGet("artifacts/{id}/download")]
public async Task<IActionResult> Download(Guid id)
{
    var artifact = await _artifactService.GetArtifactAsync(id);
    
    if (artifact == null)
        return NotFound();
        
    // Authorization check
    if (!await _authService.CanAccessWorkflowAsync(
        artifact.WorkflowInstanceId, 
        User.GetUserId()))
        return Forbid();
    
    // Log access
    await _eventStore.AppendAsync(new WorkflowEvent
    {
        WorkflowInstanceId = artifact.WorkflowInstanceId,
        EventType = WorkflowEventType.ArtifactAccessed,
        Payload = JsonSerializer.SerializeToDocument(new { artifactId = id }),
        UserId = User.GetUserId()
    });
    
    // Retrieve content
    Stream content;
    if (!artifact.IsExternal)
    {
        content = new MemoryStream(artifact.ContentData!);
    }
    else
    {
        content = await _fileStorage.RetrieveAsync(
            artifact.StorageLocation, 
            decrypt: true);
    }
    
    // Verify hash
    var hash = await ComputeHashAsync(content);
    if (hash != artifact.ContentHash)
    {
        _logger.LogError("Hash mismatch for artifact {Id}", id);
        return StatusCode(500, "Artifact integrity check failed");
    }
    
    content.Position = 0;
    return File(content, GetMimeType(artifact.Format), artifact.Name);
}
```

### File Structure Requirements

```
src/bmadServer.ApiService/
├── Models/
│   └── Artifacts/
│       ├── Artifact.cs (new)
│       └── ArtifactType.cs (new)
├── Services/
│   ├── Storage/
│   │   ├── IFileStorage.cs (new)
│   │   └── FileSystemStorage.cs (new)
│   └── Artifacts/
│       ├── IArtifactService.cs (new)
│       └── ArtifactService.cs (new)
├── Controllers/
│   └── ArtifactsController.cs (new)
└── Data/
    └── Migrations/
        └── XXX_CreateArtifactsTable.cs (new)
```

### Dependencies

**From Previous Stories:**
- Story 4.2: WorkflowInstance entity (FK reference)
- Story 9.1: EventStore (log artifact events)
- Story 2.1: User entity (FK reference for createdBy)

**NuGet Packages:**
- Microsoft.AspNetCore.DataProtection (already in project) - for encryption
- System.Security.Cryptography (already in .NET) - for SHA256

### Testing Requirements

**Unit Tests:** `test/bmadServer.Tests/Services/Artifacts/ArtifactServiceTests.cs`

**Test Coverage:**
- Create artifact with small content (inline storage)
- Create artifact with large content (external storage)
- Version increment on duplicate name
- Hash computation and verification
- Encryption/decryption
- Authorization checks

**Integration Tests:** `test/bmadServer.Tests/Integration/Artifacts/ArtifactManagementTests.cs`

**Test Coverage:**
- Upload and download artifact
- Verify content integrity with hash
- List artifacts with versions
- Download specific version
- Unauthorized access prevention
- Encryption at rest verification

### Integration Notes

**Connection to Other Stories:**
- Story 9.1: All artifact operations logged as events
- Story 9.4: Export uses artifact listing and download
- Story 4.x: Workflow execution generates artifacts

**Future Enhancements:**
- Replace FileSystemStorage with S3-compatible storage (MinIO, AWS S3)
- Add thumbnail generation for documents
- Add full-text search on artifact content
- Add artifact sharing with external users

### Previous Story Intelligence

**From Epic 4 Stories:**
- Workflows already generate outputs (PRD, architecture, etc.)
- No existing artifact storage - critical gap this fills
- Event logging pattern established - integrate here

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 9.3]
- [Source: ARCHITECTURE.md - Security, File Storage]
- [ASP.NET Core Data Protection: https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/]
- [SHA256 Hashing: https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.sha256]


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

## References
- **Aspire Rules:** [PROJECT-WIDE-RULES.md](../../../PROJECT-WIDE-RULES.md)
- **Aspire Docs:** https://aspire.dev

- Source: [epics.md - Story 9.3](../planning-artifacts/epics.md)
- Architecture: [architecture.md](../planning-artifacts/architecture.md)
- PRD: [prd.md](../planning-artifacts/prd.md)
