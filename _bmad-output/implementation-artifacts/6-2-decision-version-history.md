# Story 6.2: Decision Version History

**Status:** ready-for-dev

## Story

As a user (Marcus), I want to see the history of changes to a decision, so that I can understand how it evolved.

## Acceptance Criteria

**Given** a decision exists  
**When** I modify the decision  
**Then** a new DecisionVersion record is created  
**And** the previous version is preserved  
**And** version number increments

**Given** I query decision history  
**When** I send GET `/api/v1/decisions/{id}/history`  
**Then** I receive all versions with: versionNumber, value, modifiedBy, modifiedAt, changeReason

**Given** I compare versions  
**When** I request a diff between two versions  
**Then** the system shows what changed (added, removed, modified fields)

**Given** I want to revert a decision  
**When** I send POST `/api/v1/decisions/{id}/revert?version=2`  
**Then** a new version is created with the content of version 2  
**And** the revert action is logged

**Given** version history exists  
**When** I view the decision in the UI  
**Then** I see a version indicator and can expand to view history timeline

## Tasks / Subtasks

- [ ] Design DecisionVersion entity model (AC: 1, 2)
  - [ ] Create DecisionVersion entity class with Id, DecisionId, VersionNumber, Value (JSONB), ModifiedBy, ModifiedAt, ChangeReason, CreatedAt
  - [ ] Add EF Core entity configuration for JSONB columns with GIN indexes
  - [ ] Add foreign key relationship to Decision entity
  - [ ] Add composite unique index on (DecisionId, VersionNumber)
  - [ ] Configure cascade behavior for version history when decision is deleted
- [ ] Create EF Core migration for DecisionVersions table (AC: 1)
  - [ ] Generate migration with `dotnet ef migrations add AddDecisionVersionsTable`
  - [ ] Verify GIN indexes on Value JSONB column
  - [ ] Verify composite unique constraint on (DecisionId, VersionNumber)
  - [ ] Test migration locally before committing
- [ ] Implement version tracking in DecisionService (AC: 1, 2)
  - [ ] Modify UpdateDecisionAsync to create DecisionVersion before update
  - [ ] Implement GetDecisionHistoryAsync to retrieve all versions chronologically
  - [ ] Add logic to increment version numbers automatically
  - [ ] Ensure atomic version creation with decision update (database transaction)
  - [ ] Include audit logging for version creation
- [ ] Implement version comparison logic (AC: 3)
  - [ ] Create CompareDecisionVersionsAsync method in DecisionService
  - [ ] Implement JSON diff algorithm for JSONB comparison
  - [ ] Return structured diff with added, removed, modified fields
  - [ ] Handle nested JSON objects and arrays in diff
  - [ ] Consider using JsonDiffPatch or similar library
- [ ] Implement decision revert functionality (AC: 4)
  - [ ] Create RevertDecisionAsync method in DecisionService
  - [ ] Load specified version and create new version with its content
  - [ ] Increment version number and set changeReason = "Reverted to version {versionNumber}"
  - [ ] Validate that target version exists before reverting
  - [ ] Include audit logging for revert operations
- [ ] Add version history endpoints to DecisionController (AC: 2, 3, 4)
  - [ ] GET `/api/v1/decisions/{id}/history` - List all versions chronologically
  - [ ] GET `/api/v1/decisions/{id}/versions/{versionNumber}` - Get specific version
  - [ ] GET `/api/v1/decisions/{id}/compare?from={v1}&to={v2}` - Compare versions
  - [ ] POST `/api/v1/decisions/{id}/revert?version={versionNumber}` - Revert decision
  - [ ] Add proper authorization (Participant/Admin only for POST)
- [ ] Write unit tests for version tracking (AC: 1, 2, 3, 4)
  - [ ] Test DecisionVersion creation on decision update
  - [ ] Test version number increments correctly
  - [ ] Test GetDecisionHistoryAsync returns versions in order
  - [ ] Test CompareDecisionVersionsAsync with various JSON structures
  - [ ] Test RevertDecisionAsync creates new version with old content
  - [ ] Test error handling for non-existent versions
- [ ] Write integration tests for version history API (AC: 2, 3, 4, 5)
  - [ ] Test GET history returns all versions with correct data
  - [ ] Test GET specific version returns correct content
  - [ ] Test compare endpoint with different version combinations
  - [ ] Test revert endpoint creates new version correctly
  - [ ] Test authorization rules on all endpoints
  - [ ] Test with real PostgreSQL database
- [ ] Update OpenAPI documentation (AC: 2, 3, 4)
  - [ ] Document DecisionVersion DTO and schema
  - [ ] Document version history endpoints with examples
  - [ ] Document compare endpoint response structure
  - [ ] Document revert endpoint behavior

## Dev Notes

### Architecture Context

This story implements **version tracking** for the Decision Tracker component (Architecture.md Component #4), enabling full audit trails and decision evolution tracking. This is critical for Epic 6's decision management capabilities and provides the foundation for decision review workflows (Story 6.4) and conflict resolution (Story 6.5).

**Key Architectural Requirements:**
- **State Persistence Layer:** PostgreSQL with JSONB for flexible version storage (Architecture.md lines 297-300)
- **Event Log Pattern:** All version operations must be logged for audit trail (Architecture.md Component #4 - line 281)
- **Optimistic Concurrency:** Version field on Decision entity enables conflict detection (Story 6.1 foundation)
- **Database Transactions:** Version creation must be atomic with decision updates to prevent inconsistency

### Database Schema Design

**DecisionVersion Entity Structure:**
```csharp
public class DecisionVersion
{
    public Guid Id { get; set; }
    public Guid DecisionId { get; set; }
    public int VersionNumber { get; set; } // Sequential: 1, 2, 3...
    public string Value { get; set; } // JSONB - snapshot of decision value at this version
    public Guid ModifiedBy { get; set; }
    public DateTime ModifiedAt { get; set; }
    public string? ChangeReason { get; set; } // Optional: "Updated by user", "Reverted to version 2", etc.
    public DateTime CreatedAt { get; set; }
    
    // Navigation properties
    public Decision Decision { get; set; }
    public User User { get; set; }
}
```

**EF Core Configuration (Fluent API):**
```csharp
builder.Entity<DecisionVersion>(entity =>
{
    entity.HasKey(e => e.Id);
    entity.Property(e => e.Value).HasColumnType("jsonb"); // PostgreSQL JSONB
    entity.HasIndex(e => e.Value).HasMethod("gin"); // GIN index for JSONB queries
    
    // Composite unique constraint: one version number per decision
    entity.HasIndex(e => new { e.DecisionId, e.VersionNumber })
          .IsUnique();
    
    // Foreign key with cascade delete (optional: keep history even if decision deleted)
    entity.HasOne(e => e.Decision)
          .WithMany()
          .HasForeignKey(e => e.DecisionId)
          .OnDelete(DeleteBehavior.Cascade); // Or Restrict to preserve history
    
    // Index for chronological retrieval
    entity.HasIndex(e => new { e.DecisionId, e.ModifiedAt });
    
    entity.Property(e => e.ChangeReason).HasMaxLength(500);
});
```

**Decision Entity Update (from Story 6.1):**
```csharp
// Add navigation property to Decision entity
public class Decision
{
    // ... existing properties from Story 6.1 ...
    
    // New navigation property for version history
    public ICollection<DecisionVersion> Versions { get; set; }
}
```

### Project Structure

**New Files to Create:**
- `src/bmadServer.ApiService/Models/DecisionVersion.cs` - Entity model
- `src/bmadServer.ApiService/Data/Configurations/DecisionVersionConfiguration.cs` - EF Core fluent configuration
- `src/bmadServer.ApiService/DTOs/DecisionVersionResponse.cs` - Version response DTO
- `src/bmadServer.ApiService/DTOs/VersionDiffResponse.cs` - Diff response DTO
- `src/bmadServer.ApiService/DTOs/RevertDecisionRequest.cs` - Revert request DTO
- `src/bmadServer.ApiService/Services/VersionComparer.cs` - JSON diff logic (helper class)
- `tests/bmadServer.ApiService.Tests/Services/DecisionServiceVersionTests.cs` - Unit tests for version logic
- `tests/bmadServer.ApiService.Tests/Services/VersionComparerTests.cs` - Unit tests for diff logic
- `tests/bmadServer.ApiService.IntegrationTests/DecisionVersionApiTests.cs` - Integration tests

**Existing Files to Modify:**
- `src/bmadServer.ApiService/Data/ApplicationDbContext.cs` - Add DbSet<DecisionVersion>
- `src/bmadServer.ApiService/Services/DecisionService.cs` - Add version tracking to UpdateDecisionAsync, add new methods
- `src/bmadServer.ApiService/Services/IDecisionService.cs` - Add version-related method signatures
- `src/bmadServer.ApiService/Controllers/DecisionController.cs` - Add version history endpoints
- `src/bmadServer.ApiService/Models/Decision.cs` - Add Versions navigation property

### Technical Requirements

**1. Version Tracking Implementation:**
When a decision is updated, create a version record before applying the update:
```csharp
// In DecisionService.UpdateDecisionAsync
using var transaction = await _context.Database.BeginTransactionAsync();
try
{
    // Load current decision with version tracking
    var decision = await _context.Decisions
        .Include(d => d.Versions)
        .FirstOrDefaultAsync(d => d.Id == decisionId);
    
    if (decision == null)
        throw new NotFoundException("Decision not found");
    
    // Create version snapshot BEFORE update
    var version = new DecisionVersion
    {
        Id = Guid.NewGuid(),
        DecisionId = decision.Id,
        VersionNumber = (decision.Versions?.Max(v => v.VersionNumber) ?? 0) + 1,
        Value = decision.Value, // Snapshot of current value
        ModifiedBy = userId,
        ModifiedAt = DateTime.UtcNow,
        ChangeReason = request.ChangeReason ?? "Updated",
        CreatedAt = DateTime.UtcNow
    };
    
    _context.DecisionVersions.Add(version);
    
    // Now update decision
    decision.Value = request.NewValue;
    decision.UpdatedAt = DateTime.UtcNow;
    decision.Version++; // Optimistic concurrency token from Story 6.1
    
    await _context.SaveChangesAsync();
    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;
}
```

**2. JSON Diff Algorithm:**
For version comparison, implement a JSON diff algorithm that identifies:
- **Added fields:** Present in new version but not in old
- **Removed fields:** Present in old version but not in new
- **Modified fields:** Present in both but with different values

Consider using **JsonDiffPatch.NET** library (NuGet package) or implement custom logic:
```csharp
public class VersionComparer
{
    public VersionDiffResponse Compare(string oldValue, string newValue)
    {
        var oldJson = JsonDocument.Parse(oldValue);
        var newJson = JsonDocument.Parse(newValue);
        
        var diff = new VersionDiffResponse
        {
            Added = new List<JsonProperty>(),
            Removed = new List<JsonProperty>(),
            Modified = new List<JsonPropertyChange>()
        };
        
        // Recursive comparison logic
        CompareObjects(oldJson.RootElement, newJson.RootElement, "", diff);
        
        return diff;
    }
    
    private void CompareObjects(JsonElement oldElement, JsonElement newElement, string path, VersionDiffResponse diff)
    {
        // Implementation details: recursive JSON comparison
        // Handle nested objects, arrays, primitives
    }
}
```

**3. Revert Functionality:**
Revert creates a NEW version with content from an old version (not a true rollback):
```csharp
// In DecisionService.RevertDecisionAsync
public async Task<Decision> RevertDecisionAsync(Guid decisionId, int targetVersionNumber, Guid userId)
{
    using var transaction = await _context.Database.BeginTransactionAsync();
    try
    {
        var decision = await _context.Decisions.FindAsync(decisionId);
        var targetVersion = await _context.DecisionVersions
            .FirstOrDefaultAsync(v => v.DecisionId == decisionId && v.VersionNumber == targetVersionNumber);
        
        if (targetVersion == null)
            throw new NotFoundException($"Version {targetVersionNumber} not found");
        
        // Create new version with reverted content
        var newVersionNumber = await _context.DecisionVersions
            .Where(v => v.DecisionId == decisionId)
            .MaxAsync(v => v.VersionNumber) + 1;
        
        var revertVersion = new DecisionVersion
        {
            Id = Guid.NewGuid(),
            DecisionId = decision.Id,
            VersionNumber = newVersionNumber,
            Value = targetVersion.Value, // Use old version's value
            ModifiedBy = userId,
            ModifiedAt = DateTime.UtcNow,
            ChangeReason = $"Reverted to version {targetVersionNumber}",
            CreatedAt = DateTime.UtcNow
        };
        
        _context.DecisionVersions.Add(revertVersion);
        
        // Update decision with reverted value
        decision.Value = targetVersion.Value;
        decision.UpdatedAt = DateTime.UtcNow;
        decision.Version++;
        
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        
        return decision;
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}
```

**4. Authorization:**
- Only authenticated users can view version history (GET endpoints)
- Only Participants and Admins can revert decisions (POST endpoint)
- Use `[Authorize]` attribute on all endpoints
- Check roles in controller: `User.IsInRole("Participant") || User.IsInRole("Admin")`

### Integration Points

**DecisionService Integration:**
- **UpdateDecisionAsync:** Modified to create DecisionVersion before updating Decision
- **DeleteDecisionAsync:** Consider preserving version history even after decision deletion (use DeleteBehavior.Restrict)
- **CaptureDecisionAsync:** Initial decision capture should create version 1 (optional, or start versioning from first update)

**API Endpoint Pattern:**
```csharp
// In DecisionController
[HttpGet("{id}/history")]
[Authorize]
public async Task<ActionResult<IEnumerable<DecisionVersionResponse>>> GetHistory(Guid id)
{
    var versions = await _decisionService.GetDecisionHistoryAsync(id);
    return Ok(versions);
}

[HttpGet("{id}/compare")]
[Authorize]
public async Task<ActionResult<VersionDiffResponse>> CompareVersions(
    Guid id, 
    [FromQuery] int from, 
    [FromQuery] int to)
{
    var diff = await _decisionService.CompareDecisionVersionsAsync(id, from, to);
    return Ok(diff);
}

[HttpPost("{id}/revert")]
[Authorize(Roles = "Participant,Admin")]
public async Task<ActionResult<DecisionResponse>> RevertDecision(
    Guid id, 
    [FromQuery] int version)
{
    var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    var decision = await _decisionService.RevertDecisionAsync(id, version, userId);
    return Ok(MapToResponse(decision));
}
```

### Testing Strategy

**Unit Tests (DecisionServiceVersionTests.cs):**
- Test version creation on decision update
- Test version number increments sequentially (1, 2, 3...)
- Test GetDecisionHistoryAsync returns versions in chronological order
- Test CompareDecisionVersionsAsync identifies added, removed, modified fields
- Test RevertDecisionAsync creates new version with old content
- Test error handling: non-existent decision, non-existent version
- Test transaction rollback on error
- Mock ApplicationDbContext and verify SaveChangesAsync calls

**Unit Tests (VersionComparerTests.cs):**
- Test JSON diff with added fields
- Test JSON diff with removed fields
- Test JSON diff with modified primitive values
- Test JSON diff with nested objects
- Test JSON diff with arrays
- Test JSON diff with complex structures (mixed types)
- Test edge cases: empty objects, null values

**Integration Tests (DecisionVersionApiTests.cs):**
- Use WebApplicationFactory<Program> pattern (established in Story 1.4)
- Test POST decision update creates version record in database
- Test GET `/api/v1/decisions/{id}/history` returns all versions
- Test GET `/api/v1/decisions/{id}/versions/{versionNumber}` returns specific version
- Test GET compare endpoint with different version combinations
- Test POST revert endpoint creates new version and updates decision
- Test authorization: unauthenticated returns 401, Viewer role on revert returns 403
- Verify version data persists correctly in PostgreSQL with JSONB
- Test with real database (use TestContainers or local PostgreSQL)

### Performance Considerations

**Query Optimization:**
- Index on (DecisionId, VersionNumber) for unique constraint and fast lookups
- Index on (DecisionId, ModifiedAt) for chronological retrieval
- GIN index on Value JSONB column for schema-based queries
- Consider pagination for large version histories (> 100 versions)
- Use `.AsNoTracking()` for read-only history queries to reduce memory overhead

**JSONB Best Practices:**
- Version snapshots stored as JSONB (same as Decision.Value)
- Keep JSONB documents reasonably sized (< 1 MB recommended)
- Consider compressing old versions after 90 days (Phase 2 optimization)
- Avoid deep nesting in JSONB structures

**Transaction Management:**
- Use database transactions for atomic version creation + decision update
- Keep transaction scope minimal (only version + decision update)
- Timeout transactions after 30 seconds to prevent locks
- Use `IsolationLevel.ReadCommitted` (default) for most cases

### Security Considerations

**Input Validation:**
- Validate decisionId exists before querying versions
- Validate version numbers are positive integers
- Validate from/to version numbers in compare endpoint (from < to)
- Limit version history query results (max 1000 versions per request)
- Sanitize JSON content to prevent injection attacks

**Audit Logging:**
- Log all version creation events with userId, timestamp, decisionId
- Use structured logging (ILogger<DecisionService>)
- Include before/after state in audit log for version updates
- Log revert operations with target version number
- Follow audit log pattern from Story 2.5 (RBAC implementation)

**Authorization:**
- All version history endpoints require authentication
- Revert endpoint requires Participant or Admin role
- Consider decision ownership: only decision creator or admins can revert
- Log authorization failures for security monitoring

### Dependencies

**Required Stories (Completed):**
- Story 1.2: Configure PostgreSQL (✅ done) - Provides database foundation
- Story 6.1: Decision Capture & Storage (⏳ ready-for-dev) - Provides Decision entity and Version field

**Enables Future Stories:**
- Story 6.3: Decision Locking Mechanism - Version history enables audit of locked decisions
- Story 6.4: Decision Review Workflow - Version comparison used in review process
- Story 6.5: Conflict Detection & Resolution - Version history used to detect conflicting changes

### Previous Story Learnings

**From Story 6.1 (Decision Capture & Storage):**
- Decision entity with JSONB Value and Context fields
- Version field on Decision entity for optimistic concurrency (foundation for version tracking)
- DecisionType enum and DecisionStatus enum patterns
- EF Core configuration with GIN indexes on JSONB columns
- Service layer pattern with interface injection
- ProblemDetails for error responses (RFC 7807)
- Integration tests with WebApplicationFactory
- Always include CreatedAt/UpdatedAt timestamps
- Use async/await consistently for database operations
- Entity configurations in separate files under Data/Configurations/

**From Epic 4 Stories (Workflow Orchestration):**
- Consistent use of Guid for entity IDs
- DTOs separate from entity models for API responses
- Include related entities in responses (e.g., user names, not just IDs)
- Pagination support for list endpoints
- Chronological retrieval with proper indexing

**From Story 4.7 (Workflow Status & Progress API):**
- Pattern for chronological retrieval with (EntityId, Timestamp) index
- DTO mapping from entities to API responses
- Include navigation properties in responses when needed

### Error Handling Patterns

**Common Error Scenarios:**
- DecisionId not found → 404 Not Found
- VersionNumber not found → 404 Not Found with detail "Version {versionNumber} not found for decision {decisionId}"
- Invalid version range in compare (from >= to) → 400 Bad Request
- Revert to non-existent version → 404 Not Found
- Unauthorized user → 401 Unauthorized
- Insufficient permissions (Viewer trying to revert) → 403 Forbidden
- Database transaction failure → 500 Internal Server Error with retry guidance
- Optimistic concurrency failure → 409 Conflict

**Response Format (ProblemDetails):**
```csharp
// Example: Version not found
return Problem(
    statusCode: 404,
    title: "Version not found",
    detail: $"Version {versionNumber} not found for decision {decisionId}",
    instance: HttpContext.Request.Path
);

// Example: Invalid version range
return Problem(
    statusCode: 400,
    title: "Invalid version range",
    detail: "The 'from' version must be less than the 'to' version",
    instance: HttpContext.Request.Path,
    extensions: new Dictionary<string, object>
    {
        ["from"] = fromVersion,
        ["to"] = toVersion
    }
);
```

### Latest Technical Information

**NuGet Packages:**
- **JsonDiffPatch.NET** (v2.3.0+): For JSON diff functionality
  - Package: `JsonDiffPatchDotNet`
  - Usage: Compare two JSON documents and generate structured diffs
  - Alternative: Implement custom diff logic using System.Text.Json

**EF Core 9.0 Features:**
- **JSONB Support in Npgsql.EntityFrameworkCore.PostgreSQL 9.0+**: Native JSONB mapping with GIN indexes
- **Database Transactions**: Use `IDbContextTransaction` for atomic operations
- **Navigation Properties**: Configure one-to-many relationships with `HasMany().WithOne()`

**Best Practices for Version History:**
- Keep version snapshots immutable (never modify a DecisionVersion record)
- Use sequential version numbers (easier to reason about than timestamps)
- Store version metadata (modifiedBy, modifiedAt, changeReason) for audit trail
- Consider soft delete pattern: keep deleted decisions' version history for compliance

## Project Structure Notes

### Alignment with Unified Project Structure

This story aligns with the established bmadServer project structure:
- **API Layer:** `src/bmadServer.ApiService/` - Controllers, DTOs, Services
- **Data Layer:** `src/bmadServer.ApiService/Data/` - EF Core configurations, migrations
- **Domain Models:** `src/bmadServer.ApiService/Models/` - Entity classes
- **Tests:** `tests/bmadServer.ApiService.Tests/` and `tests/bmadServer.ApiService.IntegrationTests/`

### Aspire Integration

**PostgreSQL Connection:**
- Connection string automatically injected from Aspire AppHost (Story 1.2)
- Pattern: `builder.AddServiceDefaults();` inherits PostgreSQL reference
- AppHost configuration in `src/bmadServer.AppHost/Program.cs`

**Service Registration:**
```csharp
// In Program.cs
builder.Services.AddScoped<IDecisionService, DecisionService>();
builder.Services.AddScoped<VersionComparer>(); // Helper class for JSON diff
```

### Naming Conventions

- **Entities:** PascalCase, singular (DecisionVersion, not DecisionVersions)
- **DTOs:** PascalCase with suffix (DecisionVersionResponse, VersionDiffResponse)
- **Services:** PascalCase with suffix (DecisionService, VersionComparer)
- **API Routes:** kebab-case, plural resources (`/api/v1/decisions/{id}/history`)
- **Database Tables:** PascalCase, plural (DecisionVersions)
- **JSONB Columns:** PascalCase (Value)

## References

- **Epic Source:** [epics.md - Epic 6, Story 6.2](../planning-artifacts/epics.md)
- **Architecture:** [architecture.md - Decision Tracker Component](../planning-artifacts/architecture.md)
- **PRD:** [prd.md - FR9, FR10, FR22, FR23](../planning-artifacts/prd.md)
- **Project Context:** [project-context-ai.md - Critical Architecture Rules](../planning-artifacts/project-context-ai.md)
- **Previous Story:** [6-1-decision-capture-storage.md](./6-1-decision-capture-storage.md)
- **Aspire Rules:** [PROJECT-WIDE-RULES.md](../../PROJECT-WIDE-RULES.md)
- **PostgreSQL JSONB:** https://www.npgsql.org/efcore/mapping/json.html
- **EF Core 9.0 Docs:** https://learn.microsoft.com/en-us/ef/core/
- **JsonDiffPatch.NET:** https://github.com/wbish/jsondiffpatch.net
- **Aspire Documentation:** https://aspire.dev

---

## Dev Agent Record

### Agent Model Used

_To be filled by dev agent_

### Debug Log References

_To be filled by dev agent_

### Completion Notes List

_To be filled by dev agent_

### File List

_To be filled by dev agent during implementation_
