# Story 9.6: Audit Log Retention & Compliance

**Status:** ready-for-dev

## Story

As an administrator, I want audit logs retained properly, so that we meet compliance requirements.

## Acceptance Criteria

**Given** audit log retention is configured  
**When** I check appsettings.json  
**Then** I see: AuditLogRetentionDays: 90 (configurable per NFR9)

**Given** logs reach retention limit  
**When** the cleanup job runs nightly  
**Then** logs older than retention period are archived/deleted  
**And** the cleanup is itself logged

**Given** I query audit logs  
**When** I send GET `/api/v1/admin/audit-logs` with filters  
**Then** I can filter by: dateRange, userId, eventType, workflowId  
**And** results are paginated

**Given** audit logs must be tamper-evident  
**When** logs are stored  
**Then** each log includes a hash of the previous log (chain)  
**And** tampering can be detected by verifying the chain

**Given** compliance export is needed  
**When** I send POST `/api/v1/admin/audit-logs/export`  
**Then** logs are exported in compliance-friendly format  
**And** the export includes integrity verification data

## Tasks / Subtasks

- [ ] Add retention configuration (AC: 1)
  - [ ] Add AuditLogRetentionDays to appsettings.json
  - [ ] Add AuditLogArchiveEnabled (true/false)
  - [ ] Add AuditLogArchiveLocation (path or S3 bucket)
  - [ ] Create AuditLogSettings configuration class
  - [ ] Register configuration in Program.cs
- [ ] Enhance WorkflowEvent with tamper-evident chain (AC: 4)
  - [ ] Add PreviousEventHash property to WorkflowEvent
  - [ ] Calculate hash: SHA256(id + eventType + timestamp + payload + previousHash)
  - [ ] Update EventStore.AppendAsync to chain events
  - [ ] Add migration to add previous_event_hash column
- [ ] Implement hash chain verification (AC: 4)
  - [ ] Create AuditLogVerificationService
  - [ ] Implement VerifyEventChainAsync method
  - [ ] Detect tampering by checking hash continuity
  - [ ] Log verification results
  - [ ] Add admin endpoint to trigger verification
- [ ] Create AuditLogRetentionService background service (AC: 2)
  - [ ] Run nightly at 2 AM (configurable)
  - [ ] Find events older than retention period
  - [ ] Archive events to cold storage if enabled
  - [ ] Delete events from database after successful archive
  - [ ] Log retention job execution
  - [ ] Track metrics: events archived, events deleted
- [ ] Implement audit log archival (AC: 2)
  - [ ] Export events to JSON format
  - [ ] Compress with gzip
  - [ ] Store in archive location (filesystem or S3)
  - [ ] Maintain hash chain in archive
  - [ ] Create archive index for querying
- [ ] Create audit log query service (AC: 3)
  - [ ] Implement GetAuditLogsAsync with filtering
  - [ ] Support filters: dateRange, userId, eventType, workflowId
  - [ ] Implement pagination with cursor-based approach
  - [ ] Add sorting options
  - [ ] Query both active and archived logs
- [ ] Create admin API endpoints (AC: 3, 5)
  - [ ] GET /api/v1/admin/audit-logs (query logs)
  - [ ] POST /api/v1/admin/audit-logs/export (export logs)
  - [ ] POST /api/v1/admin/audit-logs/verify (verify hash chain)
  - [ ] GET /api/v1/admin/audit-logs/retention-status (view retention stats)
  - [ ] Require Admin role for all endpoints
- [ ] Implement compliance export (AC: 5)
  - [ ] Export to CSV format with all fields
  - [ ] Include hash chain verification results
  - [ ] Add export metadata: date range, total events, verification status
  - [ ] Sign export with digital signature
  - [ ] Create compliance report PDF
- [ ] Add authorization and logging
  - [ ] Require Admin role for audit log access
  - [ ] Log all audit log queries (meta-audit)
  - [ ] Prevent deletion without archival
  - [ ] Rate limit export requests
- [ ] Write unit tests
  - [ ] Test hash chain calculation
  - [ ] Test hash chain verification
  - [ ] Test retention policy logic
  - [ ] Test archival process
  - [ ] Test query filters and pagination
  - [ ] Test compliance export format
- [ ] Write integration tests
  - [ ] End-to-end retention job execution
  - [ ] Verify tamper detection
  - [ ] Test audit log query with filters
  - [ ] Test compliance export generation
  - [ ] Test archive retrieval and querying

## Dev Notes

### Architecture Alignment

**Source:** [ARCHITECTURE.MD - Audit Logging, Compliance]

- Modify entity: `src/bmadServer.ApiService/Models/Events/WorkflowEvent.cs` (add PreviousEventHash)
- Create service: `src/bmadServer.ApiService/Services/Audit/AuditLogVerificationService.cs`
- Create service: `src/bmadServer.ApiService/Services/Audit/AuditLogRetentionService.cs` (background)
- Create service: `src/bmadServer.ApiService/Services/Audit/AuditLogQueryService.cs`
- Create service: `src/bmadServer.ApiService/Services/Audit/ComplianceExportService.cs`
- Create background service: `src/bmadServer.ApiService/Services/Background/AuditLogCleanupService.cs`
- Create controller: `src/bmadServer.ApiService/Controllers/Admin/AuditLogsController.cs`
- Migration: `src/bmadServer.ApiService/Migrations/XXX_AddHashFieldsToWorkflowAuditEvents.cs`

**Note:** This story extends Story 9.1's WorkflowAuditEvent entity (not the simple WorkflowEvent).
The migration adds EventHash and PreviousEventHash fields to the workflow_audit_events table.

### Technical Requirements

**Configuration (appsettings.json):**
```json
{
  "AuditLogSettings": {
    "RetentionDays": 90,
    "ArchiveEnabled": true,
    "ArchiveLocation": "/var/bmadServer/archives",
    "RetentionJobSchedule": "0 2 * * *",  // 2 AM daily (cron)
    "VerificationInterval": 7  // days
  }
}
```

**Configuration Class:**
```csharp
public class AuditLogSettings
{
    public int RetentionDays { get; set; } = 90;
    public bool ArchiveEnabled { get; set; } = true;
    public string ArchiveLocation { get; set; } = "/var/bmadServer/archives";
    public string RetentionJobSchedule { get; set; } = "0 2 * * *";
    public int VerificationInterval { get; set; } = 7;
}
```

**Enhanced WorkflowAuditEvent with Hash Chain:**
```csharp
// Note: Extends Story 9.1's WorkflowAuditEvent entity
// Migration adds these fields to existing workflow_audit_events table
public class WorkflowAuditEvent
{
    public Guid Id { get; init; }
    public Guid WorkflowInstanceId { get; init; }
    public WorkflowEventType EventType { get; init; }
    public JsonDocument Payload { get; init; }
    public Guid? UserId { get; init; }
    public DateTime Timestamp { get; init; }
    public string? CorrelationId { get; init; }
    public long SequenceNumber { get; init; }
    
    // Added by Story 9.6: Tamper-evident hash chain
    public string? EventHash { get; init; }
    public string? PreviousEventHash { get; init; }
    
    public string ComputeHash()
    {
        // Use GetRawText() for consistent, deterministic JSON serialization
        var data = $"{Id}|{EventType}|{Timestamp:O}|{Payload.RootElement.GetRawText()}|{PreviousEventHash ?? ""}";
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToBase64String(hashBytes);
    }
}
```

**EventStore with Hash Chaining:**
```csharp
public async Task AppendAsync(
    WorkflowEvent evt,
    CancellationToken cancellationToken = default)
{
    // Get previous event for this workflow
    var previousEvent = await _context.WorkflowEvents
        .Where(e => e.WorkflowInstanceId == evt.WorkflowInstanceId)
        .OrderByDescending(e => e.SequenceNumber)
        .FirstOrDefaultAsync(cancellationToken);
    
    // Get next sequence number
    var nextSequence = (previousEvent?.SequenceNumber ?? 0) + 1;
    
    // Create new event with hash chain - compute hash during construction
    var previousHash = previousEvent?.EventHash;
    var eventId = Guid.NewGuid();
    var timestamp = DateTime.UtcNow;
    
    // Compute hash with all values known
    var tempData = $"{eventId}|{evt.EventType}|{timestamp:O}|{evt.Payload.RootElement.GetRawText()}|{previousHash ?? ""}";
    using var sha256 = SHA256.Create();
    var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(tempData));
    var eventHash = Convert.ToBase64String(hashBytes);
    
    // Create event with all fields including hash
    var newEvent = new WorkflowAuditEvent
    {
        Id = eventId,
        WorkflowInstanceId = evt.WorkflowInstanceId,
        EventType = evt.EventType,
        Payload = evt.Payload,
        UserId = evt.UserId,
        Timestamp = timestamp,
        CorrelationId = evt.CorrelationId,
        SequenceNumber = nextSequence,
        PreviousEventHash = previousHash,
        EventHash = eventHash
    };
    
    _context.WorkflowAuditEvents.Add(newEvent);
    await _context.SaveChangesAsync(cancellationToken);
}
```

**Hash Chain Verification:**
```csharp
public class AuditLogVerificationService
{
    public async Task<VerificationResult> VerifyEventChainAsync(
        Guid workflowId,
        CancellationToken cancellationToken = default)
    {
        var events = await _context.WorkflowEvents
            .Where(e => e.WorkflowInstanceId == workflowId)
            .OrderBy(e => e.SequenceNumber)
            .ToListAsync(cancellationToken);
        
        var result = new VerificationResult
        {
            WorkflowId = workflowId,
            TotalEvents = events.Count,
            VerifiedAt = DateTime.UtcNow
        };
        
        string? expectedPreviousHash = null;
        foreach (var evt in events)
        {
            // Verify previous hash link
            if (evt.PreviousEventHash != expectedPreviousHash)
            {
                result.IsTampered = true;
                result.TamperedEvents.Add(new TamperedEvent
                {
                    EventId = evt.Id,
                    SequenceNumber = evt.SequenceNumber,
                    Reason = "Previous hash mismatch"
                });
            }
            
            // Verify event hash
            var computedHash = evt.ComputeHash();
            if (evt.EventHash != computedHash)
            {
                result.IsTampered = true;
                result.TamperedEvents.Add(new TamperedEvent
                {
                    EventId = evt.Id,
                    SequenceNumber = evt.SequenceNumber,
                    Reason = "Event hash mismatch"
                });
            }
            
            expectedPreviousHash = evt.EventHash;
        }
        
        // Log verification result
        _logger.LogInformation(
            "Event chain verification for workflow {WorkflowId}: {Result}",
            workflowId,
            result.IsTampered ? "TAMPERED" : "VERIFIED");
        
        return result;
    }
}
```

**Retention Service:**
```csharp
public class AuditLogRetentionService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly AuditLogSettings _settings;
    private readonly ILogger<AuditLogRetentionService> _logger;
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(GetNextRunDelay(), stoppingToken);
                await ExecuteRetentionJobAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in audit log retention job");
            }
        }
    }
    
    private async Task ExecuteRetentionJobAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var archivalService = scope.ServiceProvider.GetRequiredService<IAuditLogArchivalService>();
        
        var cutoffDate = DateTime.UtcNow.AddDays(-_settings.RetentionDays);
        
        var eventsToRetire = await context.WorkflowEvents
            .Where(e => e.Timestamp < cutoffDate)
            .OrderBy(e => e.Timestamp)
            .Take(10000) // Process in batches
            .ToListAsync(cancellationToken);
        
        if (!eventsToRetire.Any())
        {
            _logger.LogInformation("No events to archive");
            return;
        }
        
        _logger.LogInformation(
            "Starting retention job: {Count} events to process",
            eventsToRetire.Count);
        
        // Archive if enabled
        if (_settings.ArchiveEnabled)
        {
            await archivalService.ArchiveEventsAsync(eventsToRetire, cancellationToken);
        }
        
        // Delete from active database
        context.WorkflowEvents.RemoveRange(eventsToRetire);
        await context.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation(
            "Retention job completed: {Count} events archived and removed",
            eventsToRetire.Count);
        
        // Log the retention job execution (meta-audit)
        await LogRetentionJobAsync(eventsToRetire.Count, cancellationToken);
    }
    
    private TimeSpan GetNextRunDelay()
    {
        // Parse cron schedule and calculate next run time
        // For simplicity, run daily at 2 AM
        var now = DateTime.UtcNow;
        var nextRun = now.Date.AddDays(1).AddHours(2);
        return nextRun - now;
    }
}
```

**Compliance Export:**
```csharp
public class ComplianceExportService
{
    public async Task<Stream> ExportComplianceReportAsync(
        DateTime startDate,
        DateTime endDate,
        Guid exportedBy,
        CancellationToken cancellationToken = default)
    {
        var events = await _context.WorkflowEvents
            .Where(e => e.Timestamp >= startDate && e.Timestamp <= endDate)
            .OrderBy(e => e.Timestamp)
            .ToListAsync(cancellationToken);
        
        // Verify hash chain integrity
        var verificationResults = new Dictionary<Guid, VerificationResult>();
        var workflowIds = events.Select(e => e.WorkflowInstanceId).Distinct();
        
        foreach (var workflowId in workflowIds)
        {
            var result = await _verificationService.VerifyEventChainAsync(
                workflowId, 
                cancellationToken);
            verificationResults[workflowId] = result;
        }
        
        // Create CSV export
        var csv = new StringBuilder();
        csv.AppendLine("EventId,WorkflowId,EventType,Timestamp,UserId,SequenceNumber,EventHash,PreviousHash,Verified");
        
        foreach (var evt in events)
        {
            var verified = verificationResults.TryGetValue(evt.WorkflowInstanceId, out var vr) 
                && !vr.IsTampered;
                
            csv.AppendLine($"{evt.Id},{evt.WorkflowInstanceId},{evt.EventType}," +
                          $"{evt.Timestamp:O},{evt.UserId},{evt.SequenceNumber}," +
                          $"{evt.EventHash},{evt.PreviousEventHash},{verified}");
        }
        
        // Create export metadata
        var metadata = new
        {
            ExportId = Guid.NewGuid(),
            ExportDate = DateTime.UtcNow,
            StartDate = startDate,
            EndDate = endDate,
            TotalEvents = events.Count,
            ExportedBy = exportedBy,
            IntegrityVerified = verificationResults.All(vr => !vr.Value.IsTampered),
            TamperedWorkflows = verificationResults
                .Where(vr => vr.Value.IsTampered)
                .Select(vr => vr.Key)
                .ToList()
        };
        
        // Create ZIP with CSV + metadata
        var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            // Add CSV
            var csvEntry = archive.CreateEntry("audit_logs.csv");
            await using (var entryStream = csvEntry.Open())
            await using (var writer = new StreamWriter(entryStream))
            {
                await writer.WriteAsync(csv.ToString());
            }
            
            // Add metadata JSON
            var metadataEntry = archive.CreateEntry("export_metadata.json");
            await using (var entryStream = metadataEntry.Open())
            {
                await JsonSerializer.SerializeAsync(entryStream, metadata, cancellationToken: cancellationToken);
            }
        }
        
        memoryStream.Position = 0;
        return memoryStream;
    }
}
```

**Query Service with Filtering:**
```csharp
public async Task<PagedResult<WorkflowEvent>> GetAuditLogsAsync(
    AuditLogQueryFilter filter,
    CancellationToken cancellationToken = default)
{
    var query = _context.WorkflowEvents.AsQueryable();
    
    // Apply filters
    if (filter.StartDate.HasValue)
        query = query.Where(e => e.Timestamp >= filter.StartDate.Value);
        
    if (filter.EndDate.HasValue)
        query = query.Where(e => e.Timestamp <= filter.EndDate.Value);
        
    if (filter.UserId.HasValue)
        query = query.Where(e => e.UserId == filter.UserId.Value);
        
    if (filter.WorkflowId.HasValue)
        query = query.Where(e => e.WorkflowInstanceId == filter.WorkflowId.Value);
        
    if (filter.EventTypes?.Any() == true)
        query = query.Where(e => filter.EventTypes.Contains(e.EventType));
    
    // Apply sorting
    query = filter.SortDirection == SortDirection.Descending
        ? query.OrderByDescending(e => e.Timestamp)
        : query.OrderBy(e => e.Timestamp);
    
    // Get total count
    var total = await query.CountAsync(cancellationToken);
    
    // Apply pagination
    var events = await query
        .Skip((filter.Page - 1) * filter.PageSize)
        .Take(filter.PageSize)
        .ToListAsync(cancellationToken);
    
    // Calculate total pages
    var totalPages = (int)Math.Ceiling(total / (double)filter.PageSize);
    
    return new PagedResult<WorkflowAuditEvent>
    {
        Items = events,
        TotalItems = total,
        Page = filter.Page,
        PageSize = filter.PageSize,
        TotalPages = totalPages,
        HasPrevious = filter.Page > 1,
        HasNext = filter.Page < totalPages
        PageSize = filter.PageSize
    };
}
```

### File Structure Requirements

```
src/bmadServer.ApiService/
├── Models/
│   ├── Events/
│   │   └── WorkflowEvent.cs (modify - add PreviousEventHash)
│   └── Audit/
│       ├── AuditLogQueryFilter.cs (new)
│       ├── VerificationResult.cs (new)
│       └── AuditLogSettings.cs (new)
├── Services/
│   └── Audit/
│       ├── IAuditLogVerificationService.cs (new)
│       ├── AuditLogVerificationService.cs (new)
│       ├── AuditLogRetentionService.cs (new - background service)
│       ├── IAuditLogArchivalService.cs (new)
│       ├── AuditLogArchivalService.cs (new)
│       ├── IAuditLogQueryService.cs (new)
│       ├── AuditLogQueryService.cs (new)
│       ├── IComplianceExportService.cs (new)
│       └── ComplianceExportService.cs (new)
├── Controllers/
│   └── Admin/
│       └── AuditLogsController.cs (new)
└── Data/
    └── Migrations/
        └── XXX_AddPreviousEventHashToWorkflowEvents.cs (new)
```

### Dependencies

**From Previous Stories:**
- Story 9.1: WorkflowEvent entity (modify for hash chain)
- Story 9.1: EventStore service (enhance with chaining)
- Story 9.3: FileStorage (for archival)

**NuGet Packages:**
- System.Security.Cryptography (already in .NET) - for SHA256
- System.IO.Compression (already in .NET) - for ZIP
- No additional packages required

### Testing Requirements

**Unit Tests:** `test/bmadServer.Tests/Services/Audit/AuditLogVerificationTests.cs`

**Test Coverage:**
- Hash calculation and verification
- Hash chain integrity check
- Tamper detection
- Retention policy logic
- Query filtering and pagination
- Compliance export format

**Integration Tests:** `test/bmadServer.Tests/Integration/Audit/AuditLogComplianceTests.cs`

**Test Coverage:**
- Full retention job execution
- Verify tamper detection on modified events
- Query audit logs with various filters
- Generate and validate compliance export
- Archive and retrieve archived events

### Integration Notes

**Connection to Other Stories:**
- Story 9.1: Event log foundation
- Story 9.3: Archive storage pattern
- Story 9.5: Checkpoint audit events
- All Stories: Every action generates audit events

**Compliance Standards:**
- SOC 2 Type II: Audit trail requirements
- GDPR: Right to access, retention limits
- HIPAA: Audit log requirements (if applicable)
- ISO 27001: Information security management

### Previous Story Intelligence

**From Epic 9 Stories:**
- Event log already exists (Story 9.1)
- Archival pattern established (Story 9.5)
- File storage available (Story 9.3)
- Hash chain adds tamper-evident layer

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 9.6]
- [Source: ARCHITECTURE.md - Audit Logging, Security]
- [Blockchain Hash Chain Concept: https://en.wikipedia.org/wiki/Blockchain]
- [SOC 2 Compliance: https://www.aicpa.org/]
- [GDPR Audit Requirements: https://gdpr.eu/]
