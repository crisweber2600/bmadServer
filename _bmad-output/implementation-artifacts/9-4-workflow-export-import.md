# Story 9.4: Workflow Export & Import

**Status:** ready-for-dev

## Story

As a user (Marcus), I want to export workflow artifacts and data, so that I can use them outside bmadServer.

## Acceptance Criteria

**Given** I have a completed workflow  
**When** I send POST `/api/v1/workflows/{id}/export`  
**Then** the system generates an export package containing: all artifacts, decision history, event log summary

**Given** export options exist  
**When** I specify format in the request  
**Then** I can choose: ZIP (all files), JSON (structured data only), PDF (formatted report)

**Given** I export to ZIP  
**When** I download the package  
**Then** it contains: /artifacts/*, /decisions.json, /history.json, /metadata.json

**Given** I want to import a previous export  
**When** I send POST `/api/v1/workflows/import` with the package  
**Then** a new workflow instance is created with imported data  
**And** the import source is recorded in metadata

**Given** export contains sensitive data  
**When** I request export  
**Then** the system validates my access level  
**And** sensitive fields can be redacted based on role

## Tasks / Subtasks

- [ ] Create ExportFormat enum (AC: 2)
  - [ ] Define formats: Zip, Json, Pdf
  - [ ] Add XML documentation for each format
- [ ] Create WorkflowExportService (AC: 1, 2, 3)
  - [ ] Implement ExportWorkflowAsync with format parameter
  - [ ] Gather all artifacts from ArtifactService (Story 9.3)
  - [ ] Gather decision history (if Epic 6 implemented)
  - [ ] Gather event log summary from EventStore (Story 9.1)
  - [ ] Create export manifest (metadata.json)
  - [ ] Support ZIP format with directory structure
  - [ ] Support JSON format with structured data
  - [ ] Support PDF format with formatted report (use library)
- [ ] Implement ZIP export (AC: 3)
  - [ ] Use System.IO.Compression.ZipArchive
  - [ ] Create directory structure: /artifacts/, /decisions.json, /history.json, /metadata.json
  - [ ] Include workflow metadata: id, name, status, dates, participants
  - [ ] Include all artifact versions with versioning info
  - [ ] Compress efficiently for download
- [ ] Implement JSON export (AC: 2)
  - [ ] Create structured JSON with: workflow, artifacts, decisions, events
  - [ ] Use System.Text.Json for serialization
  - [ ] Include base64-encoded artifact content for small files
  - [ ] External files referenced by URL or path
- [ ] Implement PDF export (AC: 2)
  - [ ] Use QuestPDF or similar library
  - [ ] Create formatted report with sections
  - [ ] Include workflow summary, timeline, artifacts list, decisions
  - [ ] Professional formatting with branding
- [ ] Implement data redaction (AC: 5)
  - [ ] Create IDataRedactor interface
  - [ ] Implement RedactionService with role-based rules
  - [ ] Redact sensitive fields: passwords, tokens, PII
  - [ ] Add configuration for redaction rules
  - [ ] Log redaction actions for audit
- [ ] Create WorkflowImportService (AC: 4)
  - [ ] Implement ImportWorkflowAsync with file upload
  - [ ] Parse ZIP or JSON import packages
  - [ ] Validate import structure and schema
  - [ ] Create new WorkflowInstance with imported data
  - [ ] Restore artifacts to artifact storage
  - [ ] Record import source in metadata
  - [ ] Link to original workflow if available
  - [ ] Generate ImportCompleted event
- [ ] Add authorization checks (AC: 5)
  - [ ] User must own workflow or have export permission
  - [ ] Admin role can export any workflow
  - [ ] Export actions logged for audit trail
  - [ ] Check file size limits for uploads
- [ ] Create API endpoints
  - [ ] POST /api/v1/workflows/{id}/export (create export)
  - [ ] GET /api/v1/workflows/{id}/exports/{exportId} (download export)
  - [ ] POST /api/v1/workflows/import (upload import package)
  - [ ] GET /api/v1/workflows/{id}/import-history (view import source)
- [ ] Write unit tests
  - [ ] Test ZIP export structure
  - [ ] Test JSON export format
  - [ ] Test PDF generation
  - [ ] Test data redaction rules
  - [ ] Test import validation
  - [ ] Test authorization checks
- [ ] Write integration tests
  - [ ] Export workflow and verify package contents
  - [ ] Import workflow and verify data restoration
  - [ ] Test round-trip: export → import → export
  - [ ] Test unauthorized export/import attempts
  - [ ] Test large workflow exports (performance)

## Dev Notes

### Architecture Alignment

**Source:** [ARCHITECTURE.MD - File Storage, API Design]

- Create service: `src/bmadServer.ApiService/Services/Export/WorkflowExportService.cs`
- Create service: `src/bmadServer.ApiService/Services/Import/WorkflowImportService.cs`
- Create service: `src/bmadServer.ApiService/Services/Security/DataRedactionService.cs`
- Create models: `src/bmadServer.ApiService/Models/Export/ExportManifest.cs`
- Create controller: `src/bmadServer.ApiService/Controllers/WorkflowExportController.cs`

### Technical Requirements

**Export Manifest Structure:**
```csharp
public class ExportManifest
{
    public Guid ExportId { get; set; }
    public Guid WorkflowInstanceId { get; set; }
    public string WorkflowName { get; set; }
    public WorkflowStatus Status { get; set; }
    public DateTime ExportedAt { get; set; }
    public Guid ExportedBy { get; set; }
    public ExportFormat Format { get; set; }
    public string Version { get; set; } = "1.0";
    
    public WorkflowMetadata Workflow { get; set; }
    public List<ArtifactReference> Artifacts { get; set; }
    public List<EventSummary> Events { get; set; }
    public Dictionary<string, object> CustomMetadata { get; set; }
}
```

**ZIP Export Implementation:**
```csharp
public async Task<Stream> ExportAsZipAsync(
    Guid workflowId,
    Guid userId,
    CancellationToken cancellationToken = default)
{
    var workflow = await _workflowService.GetWorkflowInstanceAsync(workflowId, cancellationToken);
    var artifacts = await _artifactService.GetArtifactsForWorkflowAsync(workflowId, includeVersions: true, cancellationToken);
    var events = await _eventStore.GetEventsAsync(workflowId, cancellationToken);
    
    var memoryStream = new MemoryStream();
    using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
    {
        // Add metadata.json
        var metadataEntry = archive.CreateEntry("metadata.json");
        await using (var entryStream = metadataEntry.Open())
        {
            await JsonSerializer.SerializeAsync(entryStream, new ExportManifest
            {
                ExportId = Guid.NewGuid(),
                WorkflowInstanceId = workflowId,
                WorkflowName = workflow.Name,
                Status = workflow.Status,
                ExportedAt = DateTime.UtcNow,
                ExportedBy = userId,
                Format = ExportFormat.Zip,
                Version = "1.0"
            }, cancellationToken: cancellationToken);
        }
        
        // Add artifacts
        foreach (var artifact in artifacts)
        {
            var content = await _artifactService.DownloadArtifactAsync(artifact.Id, cancellationToken);
            var artifactEntry = archive.CreateEntry($"artifacts/{artifact.Name}_v{artifact.Version}.{artifact.Format}");
            await using (var entryStream = artifactEntry.Open())
            {
                await content.CopyToAsync(entryStream, cancellationToken);
            }
        }
        
        // Add history.json (event summary)
        var historyEntry = archive.CreateEntry("history.json");
        await using (var entryStream = historyEntry.Open())
        {
            var eventSummary = events.Select(e => new
            {
                e.EventType,
                e.Timestamp,
                Payload = e.Payload.RootElement.ToString()
            });
            await JsonSerializer.SerializeAsync(entryStream, eventSummary, cancellationToken: cancellationToken);
        }
    }
    
    memoryStream.Position = 0;
    return memoryStream;
}
```

**Import Implementation:**
```csharp
public async Task<Guid> ImportWorkflowAsync(
    Stream importPackage,
    Guid userId,
    CancellationToken cancellationToken = default)
{
    using var archive = new ZipArchive(importPackage, ZipArchiveMode.Read);
    
    // Read and validate manifest
    var metadataEntry = archive.GetEntry("metadata.json");
    if (metadataEntry == null)
        throw new InvalidOperationException("Import package missing metadata.json");
        
    await using var metadataStream = metadataEntry.Open();
    var manifest = await JsonSerializer.DeserializeAsync<ExportManifest>(metadataStream, cancellationToken: cancellationToken);
    
    // Create new workflow instance
    var newWorkflow = new WorkflowInstance
    {
        Id = Guid.NewGuid(),
        Name = $"{manifest.WorkflowName} (Imported)",
        Status = WorkflowStatus.Completed, // Imported workflows are read-only
        CreatedAt = DateTime.UtcNow,
        CreatedBy = userId,
        Metadata = JsonSerializer.SerializeToDocument(new
        {
            ImportedFrom = manifest.WorkflowInstanceId,
            ImportedAt = DateTime.UtcNow,
            OriginalExportDate = manifest.ExportedAt
        })
    };
    
    _context.WorkflowInstances.Add(newWorkflow);
    await _context.SaveChangesAsync(cancellationToken);
    
    // Restore artifacts
    var artifactEntries = archive.Entries.Where(e => e.FullName.StartsWith("artifacts/"));
    foreach (var entry in artifactEntries)
    {
        await using var content = entry.Open();
        var fileName = Path.GetFileNameWithoutExtension(entry.Name);
        var format = Path.GetExtension(entry.Name).TrimStart('.');
        
        await _artifactService.CreateArtifactAsync(
            newWorkflow.Id,
            ArtifactType.Other, // Infer from file extension if needed
            fileName,
            format,
            content,
            userId,
            cancellationToken);
    }
    
    // Log import event
    await _eventStore.AppendAsync(new WorkflowEvent
    {
        WorkflowInstanceId = newWorkflow.Id,
        EventType = WorkflowEventType.WorkflowImported,
        Payload = JsonSerializer.SerializeToDocument(new
        {
            originalWorkflowId = manifest.WorkflowInstanceId,
            artifactCount = artifactEntries.Count()
        }),
        UserId = userId
    }, cancellationToken);
    
    return newWorkflow.Id;
}
```

**PDF Export (using QuestPDF):**
```csharp
public async Task<byte[]> ExportAsPdfAsync(
    Guid workflowId,
    CancellationToken cancellationToken = default)
{
    var workflow = await _workflowService.GetWorkflowInstanceAsync(workflowId, cancellationToken);
    var artifacts = await _artifactService.GetArtifactsForWorkflowAsync(workflowId, cancellationToken: cancellationToken);
    var events = await _eventStore.GetEventsAsync(workflowId, cancellationToken);
    
    return Document.Create(container =>
    {
        container.Page(page =>
        {
            page.Margin(50);
            
            page.Header().Text($"Workflow Export: {workflow.Name}")
                .FontSize(20).Bold();
            
            page.Content().Column(column =>
            {
                column.Item().Text("Workflow Summary").FontSize(16).Bold();
                column.Item().Text($"ID: {workflow.Id}");
                column.Item().Text($"Status: {workflow.Status}");
                column.Item().Text($"Created: {workflow.CreatedAt:yyyy-MM-dd}");
                
                column.Item().PaddingTop(20).Text("Artifacts").FontSize(16).Bold();
                foreach (var artifact in artifacts)
                {
                    column.Item().Text($"• {artifact.Name} ({artifact.Format}) - v{artifact.Version}");
                }
                
                column.Item().PaddingTop(20).Text("Event Timeline").FontSize(16).Bold();
                foreach (var evt in events.OrderBy(e => e.Timestamp))
                {
                    column.Item().Text($"{evt.Timestamp:yyyy-MM-dd HH:mm} - {evt.EventType}");
                }
            });
            
            page.Footer().AlignCenter().Text(text =>
            {
                text.Span("Exported: ");
                text.Span(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm")).Bold();
            });
        });
    }).GeneratePdf();
}
```

**Data Redaction (Recursive for Nested JSON):**
```csharp
using System.Text.Json;

public class DataRedactionService : IDataRedactor
{
    private static readonly HashSet<string> SensitiveKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "password",
        "apiKey",
        "secret",
        "token",
        "credentials"
    };

    public JsonDocument RedactSensitiveData(JsonDocument data, UserRole role)
    {
        if (role == UserRole.Admin)
            return data; // Admins see everything

        // Recreate the JSON document while redacting sensitive fields at any depth
        var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            RedactElement(data.RootElement, writer);
        }

        stream.Position = 0;
        return JsonDocument.Parse(stream);
    }

    private static bool IsSensitiveKey(string propertyName)
    {
        return SensitiveKeys.Contains(propertyName);
    }

    private static void RedactElement(JsonElement element, Utf8JsonWriter writer)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in element.EnumerateObject())
                {
                    writer.WritePropertyName(property.Name);
                    if (IsSensitiveKey(property.Name))
                    {
                        writer.WriteStringValue("[REDACTED]");
                    }
                    else
                    {
                        // Recursively process nested objects/arrays
                        RedactElement(property.Value, writer);
                    }
                }
                writer.WriteEndObject();
                break;

            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                {
                    // Recursively process array items
                    RedactElement(item, writer);
                }
                writer.WriteEndArray();
                break;

            default:
                // Copy primitive values as-is
                element.WriteTo(writer);
                break;
        }
    }
}
```

### File Structure Requirements

```
src/bmadServer.ApiService/
├── Models/
│   └── Export/
│       ├── ExportManifest.cs (new)
│       ├── ExportFormat.cs (new)
│       └── ImportResult.cs (new)
├── Services/
│   ├── Export/
│   │   ├── IWorkflowExportService.cs (new)
│   │   └── WorkflowExportService.cs (new)
│   ├── Import/
│   │   ├── IWorkflowImportService.cs (new)
│   │   └── WorkflowImportService.cs (new)
│   └── Security/
│       ├── IDataRedactor.cs (new)
│       └── DataRedactionService.cs (new)
└── Controllers/
    └── WorkflowExportController.cs (new)
```

### Dependencies

**From Previous Stories:**
- Story 4.2: WorkflowInstance entity
- Story 9.1: EventStore for event logs
- Story 9.3: ArtifactService for artifact retrieval

**NuGet Packages:**
- System.IO.Compression (already in .NET) - for ZIP
- System.Text.Json (already in project) - for JSON
- QuestPDF (new) - for PDF generation: `dotnet add package QuestPDF`

### Testing Requirements

**Unit Tests:** `test/bmadServer.Tests/Services/Export/WorkflowExportServiceTests.cs`

**Test Coverage:**
- ZIP export structure validation
- JSON export format validation
- PDF generation
- Data redaction logic
- Import validation
- Round-trip integrity

**Integration Tests:** `test/bmadServer.Tests/Integration/Export/WorkflowExportImportTests.cs`

**Test Coverage:**
- Full export workflow
- Full import workflow
- Round-trip: export → import → verify
- Large workflow performance
- Authorization checks

### Integration Notes

**Connection to Other Stories:**
- Story 9.3: Artifact download for export
- Story 9.1: Event log for history
- Story 9.5: Checkpoint data in export

**Future Enhancements:**
- Scheduled exports
- Export to cloud storage (S3, Azure Blob)
- Incremental exports (only changes since last export)
- Export templates for different audiences

### Previous Story Intelligence

**From Epic 4 & 9 Stories:**
- Workflows contain multiple artifacts
- Event logs provide complete audit trail
- Artifacts already have encryption
- Export should maintain encryption for sensitive data

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 9.4]
- [Source: ARCHITECTURE.md - File Storage, Security]
- [QuestPDF Documentation: https://www.questpdf.com/]
- [System.IO.Compression: https://learn.microsoft.com/en-us/dotnet/api/system.io.compression]
