# Story 9.6: Audit Log Retention & Compliance

**Story ID:** E9-S6  
**Epic:** Epic 9 - Data Persistence & State Management  
**Points:** 5  
**Priority:** MEDIUM  
**ADR Reference:** [ADR-031: Audit Log Retention & Compliance](../planning-artifacts/adr/adr-031-audit-log-retention-compliance.md)

## User Story

As a compliance officer, I want audit logs retained according to regulations with proper archival and deletion policies, so that we meet GDPR, SOC 2, and industry compliance requirements.

## Acceptance Criteria

**Given** audit events exist for 90+ days  
**When** the daily archival job runs  
**Then** events are moved from hot storage to warm storage  
**And** hot storage query performance remains optimal

**Given** audit events exist for 1+ year  
**When** the monthly archival job runs  
**Then** events are compressed and exported to cold storage (S3-compatible)  
**And** events are deleted from warm storage after successful export

**Given** a user requests data deletion (GDPR Article 17)  
**When** the deletion request is processed  
**Then** user PII is anonymized in audit events  
**And** the audit trail structure is preserved  
**And** an anonymization event is logged

**Given** I query recent audit events  
**When** I filter by classification (Security, Compliance, etc.)  
**Then** results are returned efficiently from hot storage  
**And** classification-based retention policies are enforced

## Tasks

- [ ] Add `Classification` and `ContainsPII` fields to AuditEvent
- [ ] Create migration for new audit event fields
- [ ] Create `audit_events_archive` partitioned table
- [ ] Implement `IAuditArchivalService` interface
- [ ] Implement hot-to-warm archival (90 days)
- [ ] Implement warm-to-cold archival (1 year)
- [ ] Implement S3-compatible storage integration
- [ ] Implement compression for cold storage exports
- [ ] Implement GDPR anonymization logic
- [ ] Implement PII redaction from JSON event data
- [ ] Create daily archival background job
- [ ] Create monthly cold storage background job
- [ ] Add retention policy configuration
- [ ] Add data deletion API `POST /api/v1/admin/data-deletion/{userId}`
- [ ] Add archival status API `GET /api/v1/admin/audit/archival-status`
- [ ] Create archival monitoring dashboard
- [ ] Add unit tests for archival logic
- [ ] Add unit tests for anonymization
- [ ] Add integration tests for hot-to-warm migration
- [ ] Add integration tests for GDPR deletion
- [ ] Add integration tests for classification-based retention

## Files to Create

- `src/bmadServer.ApiService/Services/AuditArchivalService.cs`
- `src/bmadServer.ApiService/Services/DataDeletionService.cs`
- `src/bmadServer.ApiService/Jobs/DailyAuditArchivalJob.cs`
- `src/bmadServer.ApiService/Jobs/MonthlyColdStorageJob.cs`
- `src/bmadServer.ApiService/Controllers/DataComplianceController.cs`
- `src/bmadServer.ApiService/Data/Migrations/YYYYMMDDHHMMSS_AddAuditClassification.cs`
- `src/bmadServer.ApiService/Data/Migrations/YYYYMMDDHHMMSS_CreateAuditArchiveTable.cs`

## Files to Modify

- `src/bmadServer.ApiService/Data/Entities/AuditEvent.cs` - Add Classification, ContainsPII
- `src/bmadServer.ApiService/Services/AuditService.cs` - Set classification on event creation
- `src/bmadServer.ApiService/Program.cs` - Register archival services
- `appsettings.json` - Add retention policy configuration

## Testing Checklist

- [ ] Unit test: Classification assigned correctly for event types
- [ ] Unit test: ContainsPII flag set for user events
- [ ] Unit test: Anonymization redacts PII fields
- [ ] Unit test: Anonymization preserves audit structure
- [ ] Integration test: Daily job moves 90-day events to archive
- [ ] Integration test: Monthly job exports to cold storage
- [ ] Integration test: Cold storage upload succeeds
- [ ] Integration test: GDPR deletion anonymizes user events
- [ ] Integration test: Anonymization event logged
- [ ] Integration test: Query hot storage performs under 100ms
- [ ] Integration test: Classification-based retention enforced
- [ ] Performance test: Archival of 100K events completes under 5min
- [ ] Compliance test: 7-year retention verified for Security events
- [ ] Compliance test: PII anonymization verified

## Definition of Done

- [ ] All acceptance criteria met
- [ ] All tasks completed
- [ ] All tests passing
- [ ] Code review completed
- [ ] Documentation updated
- [ ] ADR-031 implementation verified
- [ ] Privacy policy updated with retention details
- [ ] Compliance documentation created
- [ ] Story demonstrated to stakeholders
- [ ] Merged to main branch

## Notes

- Retention tiers: Hot (90 days), Warm (1 year), Cold (7 years)
- Classification: Security (7y), Compliance (7y), Operational (1y), Diagnostic (90d)
- GDPR anonymization preferred over hard deletion
- S3-compatible storage required for cold archival
- Consider legal review of anonymization strategy
- Monitor storage costs for cold archival
- Archival job failures should trigger alerts
