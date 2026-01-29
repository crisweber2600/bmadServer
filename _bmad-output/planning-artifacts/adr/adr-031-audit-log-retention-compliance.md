# ADR-031: Audit Log Retention & Compliance

**Status:** Accepted  
**Date:** 2026-01-28  
**Context:** Epic 9 Story 9-6  
**Deciders:** Winston (Architect), John (PM), Mary (Analyst)

## Context and Problem Statement

bmadServer must retain audit logs for compliance, debugging, and forensic analysis. We need to define retention policies, archival strategies, and compliance requirements while balancing storage costs and query performance.

## Decision Drivers

- Compliance requirements (GDPR, SOC 2, industry-specific regulations)
- Storage costs for long-term retention
- Query performance for recent vs. historical logs
- Data privacy and anonymization requirements
- Right to deletion (GDPR Article 17)

## Considered Options

1. **Infinite Retention** - Keep all logs forever
2. **Fixed Retention Period** - Single retention policy for all logs
3. **Tiered Retention** - Hot/warm/cold storage tiers
4. **Compliance-Driven** - Retention based on data classification

## Decision Outcome

**Chosen: Option 3 - Tiered Retention with Compliance Classification**

Implement hot/warm/cold storage tiers with retention periods based on log sensitivity and compliance requirements.

### Retention Tiers

```yaml
Hot Storage (Primary Database):
  Duration: 90 days
  Location: PostgreSQL primary
  Performance: Real-time queries
  Contains: All recent audit events

Warm Storage (Archive Table):
  Duration: 1 year (after hot period)
  Location: PostgreSQL partitioned archive table
  Performance: Slower queries acceptable
  Contains: Historical audit events

Cold Storage (Object Storage):
  Duration: 6 years (7 year total retention)
  Location: S3-compatible storage (compressed)
  Performance: Manual export/analysis only
  Contains: Compressed audit exports
```

### Event Classification

```csharp
public enum AuditEventClass
{
    Security,      // Authentication, authorization - 7 years
    Compliance,    // Decisions, approvals, locks - 7 years
    Operational,   // Step transitions, agent routing - 1 year
    Diagnostic,    // Performance metrics, traces - 90 days
    PII            // Contains personal data - subject to deletion
}

public class AuditEvent
{
    // ... existing fields ...
    public AuditEventClass Classification { get; set; }
    public bool ContainsPII { get; set; }
    public DateTime? DeleteAfter { get; set; }  // For PII deletion requests
}
```

### Retention Policies

```csharp
public class RetentionPolicy
{
    public Dictionary<AuditEventClass, int> RetentionDays { get; } = new()
    {
        { AuditEventClass.Security, 2555 },      // 7 years
        { AuditEventClass.Compliance, 2555 },    // 7 years
        { AuditEventClass.Operational, 365 },    // 1 year
        { AuditEventClass.Diagnostic, 90 },      // 90 days
        { AuditEventClass.PII, 365 }             // 1 year (unless deleted)
    };
    
    public int HotStorageDays { get; set; } = 90;
    public int WarmStorageDays { get; set; } = 365;
}
```

### Archival Process

```csharp
public class AuditArchivalService
{
    // Daily job to move old events to warm storage
    public async Task ArchiveToWarmStorageAsync()
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-90);
        
        // Move events from hot to warm
        await _context.Database.ExecuteSqlRawAsync(@"
            INSERT INTO audit_events_archive 
            SELECT * FROM audit_events 
            WHERE timestamp < @cutoffDate
        ", new { cutoffDate });
        
        await _context.Database.ExecuteSqlRawAsync(@"
            DELETE FROM audit_events 
            WHERE timestamp < @cutoffDate
        ", new { cutoffDate });
    }
    
    // Monthly job to export to cold storage
    public async Task ArchiveToColdStorageAsync()
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-365);
        
        // Export to compressed JSON
        var events = await _context.AuditEventsArchive
            .Where(e => e.Timestamp < cutoffDate)
            .ToListAsync();
        
        var exportData = JsonSerializer.Serialize(events);
        var compressed = await CompressAsync(exportData);
        
        // Upload to S3-compatible storage
        var filename = $"audit-archive-{cutoffDate:yyyy-MM}.json.gz";
        await _blobStorage.UploadAsync(
            $"audit-archives/{cutoffDate:yyyy}/{filename}", 
            compressed
        );
        
        // Delete from warm storage after successful upload
        await _context.Database.ExecuteSqlRawAsync(@"
            DELETE FROM audit_events_archive 
            WHERE timestamp < @cutoffDate
        ", new { cutoffDate });
    }
}
```

### GDPR Compliance - Right to Deletion

```csharp
public async Task ProcessDeletionRequestAsync(Guid userId)
{
    // 1. Identify all events containing user PII
    var piiEvents = await _context.AuditEvents
        .Where(e => e.UserId == userId || e.ContainsPII)
        .ToListAsync();
    
    // 2. Anonymize instead of delete (preserve audit trail)
    foreach (var evt in piiEvents)
    {
        evt.UserId = Guid.Empty;  // Anonymize user reference
        evt.EventData = AnonymizeJson(evt.EventData);  // Redact PII fields
        evt.ContainsPII = false;
        evt.DeleteAfter = null;
    }
    
    await _context.SaveChangesAsync();
    
    // 3. Log the anonymization action
    await _auditService.LogEventAsync(new AuditEvent
    {
        EventType = "PIIAnonymized",
        EntityId = userId,
        Classification = AuditEventClass.Compliance,
        EventData = JsonSerializer.SerializeToDocument(new {
            Reason = "GDPR Article 17 - Right to Deletion",
            EventsAffected = piiEvents.Count
        })
    });
}
```

### Query Optimization

```sql
-- Partition hot storage by month
CREATE TABLE audit_events (
    id UUID PRIMARY KEY,
    timestamp TIMESTAMPTZ NOT NULL,
    event_type VARCHAR(100),
    classification VARCHAR(50),
    -- ... other fields
) PARTITION BY RANGE (timestamp);

-- Create monthly partitions
CREATE TABLE audit_events_2026_01 PARTITION OF audit_events
    FOR VALUES FROM ('2026-01-01') TO ('2026-02-01');

-- Index for common queries
CREATE INDEX idx_audit_user_time ON audit_events (user_id, timestamp DESC);
CREATE INDEX idx_audit_entity ON audit_events (entity_id, entity_type);
CREATE INDEX idx_audit_classification ON audit_events (classification, timestamp);
```

### Admin Dashboard Queries

```csharp
// Recent activity for user
var userActivity = await _context.AuditEvents
    .Where(e => e.UserId == userId && e.Timestamp > DateTime.UtcNow.AddDays(-30))
    .OrderByDescending(e => e.Timestamp)
    .Take(100)
    .ToListAsync();

// Compliance audit trail for workflow
var workflowAudit = await _context.AuditEvents
    .Where(e => e.EntityId == workflowId 
             && e.Classification == AuditEventClass.Compliance)
    .OrderBy(e => e.Timestamp)
    .ToListAsync();

// Security events last 24 hours
var securityEvents = await _context.AuditEvents
    .Where(e => e.Classification == AuditEventClass.Security 
             && e.Timestamp > DateTime.UtcNow.AddHours(-24))
    .ToListAsync();
```

## Implementation Notes

- Implement `IAuditArchivalService` for tiered storage
- Background jobs: Daily (hot→warm), Monthly (warm→cold)
- Admin API for compliance queries and deletion requests
- Monitoring dashboard for storage metrics
- Alerting for archival job failures
- Document retention policies in user-facing privacy policy

## Consequences

### Positive
- Compliance with GDPR and SOC 2 requirements
- Optimized query performance (hot storage small)
- Cost-effective long-term retention (cold storage cheap)
- Right to deletion support without losing audit trail

### Negative
- Complex archival pipeline to maintain
- Historical queries require accessing multiple tiers
- Storage infrastructure dependencies (S3-compatible)

### Neutral
- Anonymization strategy may need legal review
- Retention periods configurable per deployment

## Related Decisions

- ADR-026: Event Log Architecture
- ADR-028: Artifact Storage Management
- Infrastructure decision: S3-compatible storage

## References

- Epic 9 Story 9-6: audit-log-retention-compliance
- GDPR Article 17 (Right to Erasure)
- SOC 2 Type II audit requirements
- PostgreSQL table partitioning documentation
