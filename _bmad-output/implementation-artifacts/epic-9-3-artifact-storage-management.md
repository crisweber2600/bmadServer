# Story 9.3: Artifact Storage Management

**Story ID:** E9-S3  
**Epic:** Epic 9 - Data Persistence & State Management  
**Points:** 5  
**Priority:** MEDIUM  
**ADR Reference:** [ADR-028: Artifact Storage Management](../planning-artifacts/adr/adr-028-artifact-storage-management.md)

## User Story

As a user, I want workflow artifacts (documents, diagrams) stored and versioned efficiently, so that I can retrieve and reference them throughout the workflow lifecycle.

## Acceptance Criteria

**Given** a workflow generates an artifact under 1MB  
**When** the artifact is stored  
**Then** it is persisted directly in PostgreSQL  
**And** a checksum is calculated for integrity verification

**Given** a workflow generates an artifact over 1MB  
**When** the artifact is stored  
**Then** it is saved to the file system  
**And** metadata is stored in the database with storage key

**Given** I update an existing artifact  
**When** the new version is saved  
**Then** a new Artifact record is created with incremented Version  
**And** the previous version is retained for history

**Given** I query artifacts for a workflow  
**When** I request the latest version  
**Then** I receive the most recent artifact by Version number

## Tasks

- [ ] Create `Artifact` entity with hybrid storage fields
- [ ] Add DbSet to ApplicationDbContext
- [ ] Create migration for artifacts table
- [ ] Implement `IArtifactStorageService` interface
- [ ] Implement `IFileStorageProvider` abstraction
- [ ] Implement FileSystemStorageProvider
- [ ] Implement storage decision logic (1MB threshold)
- [ ] Implement SHA256 checksum calculation
- [ ] Implement artifact versioning logic
- [ ] Create artifact storage directory structure
- [ ] Add artifact upload API endpoint `POST /api/v1/workflows/{id}/artifacts`
- [ ] Add artifact download API endpoint `GET /api/v1/artifacts/{id}`
- [ ] Add artifact version history endpoint `GET /api/v1/artifacts/{id}/versions`
- [ ] Implement streaming for large artifact downloads
- [ ] Create lifecycle cleanup background job
- [ ] Add unit tests for storage decision logic
- [ ] Add unit tests for versioning
- [ ] Add integration tests for upload/download
- [ ] Add performance tests for large artifacts

## Files to Create

- `src/bmadServer.ApiService/Data/Entities/Artifact.cs`
- `src/bmadServer.ApiService/Services/ArtifactStorageService.cs`
- `src/bmadServer.ApiService/Services/Storage/IFileStorageProvider.cs`
- `src/bmadServer.ApiService/Services/Storage/FileSystemStorageProvider.cs`
- `src/bmadServer.ApiService/Controllers/ArtifactsController.cs`
- `src/bmadServer.ApiService/Data/Migrations/YYYYMMDDHHMMSS_AddArtifacts.cs`
- `src/bmadServer.ApiService/Jobs/ArtifactCleanupJob.cs`

## Files to Modify

- `src/bmadServer.ApiService/Data/ApplicationDbContext.cs` - Add Artifacts DbSet
- `src/bmadServer.ApiService/Program.cs` - Register artifact services
- `appsettings.json` - Add artifact storage configuration

## Testing Checklist

- [ ] Unit test: Small artifact stored in database
- [ ] Unit test: Large artifact stored in file system
- [ ] Unit test: Checksum calculation correct
- [ ] Unit test: Version increment logic
- [ ] Integration test: Upload artifact under 1MB
- [ ] Integration test: Upload artifact over 1MB
- [ ] Integration test: Download artifact returns correct content
- [ ] Integration test: Upload new version increments Version field
- [ ] Integration test: Get versions returns all artifact versions
- [ ] Performance test: Upload 5MB artifact completes under 2s
- [ ] Performance test: Download streams large artifacts efficiently

## Definition of Done

- [ ] All acceptance criteria met
- [ ] All tasks completed
- [ ] All tests passing
- [ ] Code review completed
- [ ] Documentation updated
- [ ] ADR-028 implementation verified
- [ ] Storage directory structure created
- [ ] Story demonstrated to stakeholders
- [ ] Merged to main branch

## Notes

- 1MB threshold is configurable via appsettings
- Future S3 migration path preserved with IFileStorageProvider abstraction
- Consider implementing Content-Disposition headers for friendly filenames
- Monitor disk space usage in production
- Lifecycle cleanup should respect retention policies
