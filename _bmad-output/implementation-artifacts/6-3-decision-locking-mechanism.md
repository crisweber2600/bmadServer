# Story 6.3: Decision Locking Mechanism

**Status:** ready-for-dev

## Story

As a user (Sarah), I want to lock important decisions, so that they cannot be accidentally changed.

## Acceptance Criteria

**Given** a decision is unlocked  
**When** I send POST `/api/v1/decisions/{id}/lock`  
**Then** the decision status changes to Locked  
**And** lockedBy and lockedAt are recorded  
**And** I receive 200 OK with updated decision

**Given** a decision is locked  
**When** I try to modify it via PUT `/api/v1/decisions/{id}`  
**Then** I receive 403 Forbidden with "Decision is locked. Unlock to modify."

**Given** I want to unlock a decision  
**When** I send POST `/api/v1/decisions/{id}/unlock` with reason  
**Then** the decision is unlocked  
**And** the unlock action is logged with reason

**Given** I am a Viewer role  
**When** I try to lock/unlock decisions  
**Then** I receive 403 Forbidden (only Participant/Admin can lock)

**Given** a decision is locked  
**When** I view it in the UI  
**Then** I see a lock icon and "Locked by [name] on [date]"  
**And** edit controls are disabled

## Tasks / Subtasks

- [ ] Extend Decision entity model with locking fields (AC: 1, 3, 4)
  - [ ] Add LockedBy (Guid?) - UserId who locked the decision
  - [ ] Add LockedAt (DateTime?) - Timestamp when locked
  - [ ] Add UnlockReason (string) - Reason for unlocking
  - [ ] Ensure Status field exists with enum values (Draft, UnderReview, Locked)
  - [ ] Update DecisionStatus enum if needed
- [ ] Create EF Core migration for locking fields (AC: 1)
  - [ ] Generate migration with `dotnet ef migrations add AddDecisionLockingFields`
  - [ ] Verify nullable fields for LockedBy and LockedAt (nullable when unlocked)
  - [ ] Add index on LockedBy for query performance
  - [ ] Test migration locally before committing
- [ ] Implement lock/unlock logic in DecisionService (AC: 1, 2, 3)
  - [ ] Create LockDecisionAsync(Guid decisionId, Guid userId) method
  - [ ] Create UnlockDecisionAsync(Guid decisionId, Guid userId, string reason) method
  - [ ] Validate decision exists before locking
  - [ ] Validate decision is not already locked before locking
  - [ ] Validate decision is locked before unlocking
  - [ ] Update Status to Locked/Draft appropriately
  - [ ] Record LockedBy, LockedAt on lock
  - [ ] Clear LockedBy, LockedAt on unlock, save UnlockReason
  - [ ] Include audit logging for all lock/unlock operations
- [ ] Add lock validation to UpdateDecisionAsync (AC: 2)
  - [ ] Check if decision.Status == DecisionStatus.Locked before update
  - [ ] Throw LockedDecisionException with "Decision is locked. Unlock to modify."
  - [ ] Ensure exception maps to 403 Forbidden in controller
- [ ] Add lock/unlock endpoints to DecisionController (AC: 1, 3, 4)
  - [ ] POST `/api/v1/decisions/{id}/lock` - Lock decision
  - [ ] POST `/api/v1/decisions/{id}/unlock` - Unlock decision with reason in body
  - [ ] Add [Authorize] with role check (Participant/Admin only)
  - [ ] Return 403 for Viewer role attempting lock/unlock
  - [ ] Return 200 OK with updated decision DTO on success
  - [ ] Add proper error handling and ProblemDetails responses
- [ ] Create DTOs for lock/unlock operations (AC: 1, 3)
  - [ ] LockDecisionResponse - includes decision with lock metadata
  - [ ] UnlockDecisionRequest - { reason: string }
  - [ ] Update DecisionDto to include: isLocked, lockedBy, lockedAt, lockedByName
- [ ] Write unit tests for locking logic (AC: 1, 2, 3, 4)
  - [ ] Test LockDecisionAsync sets Status, LockedBy, LockedAt correctly
  - [ ] Test LockDecisionAsync fails if decision already locked
  - [ ] Test UnlockDecisionAsync clears lock fields and logs reason
  - [ ] Test UpdateDecisionAsync throws LockedDecisionException when locked
  - [ ] Test authorization rules for Viewer vs Participant/Admin roles
  - [ ] Test unlock reason is persisted
- [ ] Write integration tests for lock/unlock API (AC: 1, 2, 3, 4)
  - [ ] Test POST lock endpoint changes status and records metadata
  - [ ] Test PUT update endpoint returns 403 when decision locked
  - [ ] Test POST unlock endpoint clears lock and logs reason
  - [ ] Test authorization rules with different roles
  - [ ] Test with real PostgreSQL database
- [ ] Update OpenAPI documentation (AC: 1, 3)
  - [ ] Document lock/unlock endpoints with examples
  - [ ] Document UnlockDecisionRequest schema
  - [ ] Document LockedDecisionException error response
  - [ ] Update DecisionDto schema with lock fields
- [ ] UI Integration guidance for frontend (AC: 5)
  - [ ] Document lock icon display logic (isLocked field)
  - [ ] Document edit control disabling when locked
  - [ ] Document "Locked by [name] on [date]" display format
  - [ ] Provide example API calls for UI components

## Dev Notes

### Architecture Context

This story implements the **locking mechanism** for the Decision Tracker component (Architecture.md Component #4 - lines 276-281), enabling decision governance and preventing accidental changes to critical decisions. This is foundational for Epic 6's decision management capabilities and directly supports the decision review workflow (Story 6.4) and conflict resolution (Story 6.5).

**Key Architectural Requirements:**
- **Optimistic Concurrency Control:** Decision entity already has Version field from Story 6.1 (line 99 in 6-1 story)
- **Event Log Pattern:** All lock/unlock operations must be logged for audit trail (Architecture.md Component #4 - line 281)
- **State Persistence Layer:** PostgreSQL with EF Core for Decision entity management (Architecture.md lines 297-300)
- **Authorization:** RBAC implementation from Epic 2 (Story 2.5) enforces Participant/Admin only for lock operations

### Database Schema Extensions

**Decision Entity Locking Fields (extends Story 6.1 schema):**
```csharp
public class Decision
{
    // ... existing fields from Story 6.1 ...
    
    // Locking fields (Story 6.3)
    public Guid? LockedBy { get; set; } // Nullable - null when unlocked
    public DateTime? LockedAt { get; set; } // Nullable - null when unlocked
    public string? UnlockReason { get; set; } // Nullable - reason for last unlock
    
    // Navigation property
    public User LockedByUser { get; set; }
}
```

**EF Core Migration Pattern:**
```csharp
migrationBuilder.AddColumn<Guid>(
    name: "LockedBy",
    table: "Decisions",
    type: "uuid",
    nullable: true);

migrationBuilder.AddColumn<DateTime>(
    name: "LockedAt",
    table: "Decisions",
    type: "timestamp with time zone",
    nullable: true);

migrationBuilder.AddColumn<string>(
    name: "UnlockReason",
    table: "Decisions",
    type: "text",
    nullable: true);

migrationBuilder.CreateIndex(
    name: "IX_Decisions_LockedBy",
    table: "Decisions",
    column: "LockedBy");
```

### Service Layer Implementation

**DecisionService Lock Methods:**
```csharp
public async Task<Decision> LockDecisionAsync(Guid decisionId, Guid userId)
{
    var decision = await _context.Decisions
        .Include(d => d.LockedByUser)
        .FirstOrDefaultAsync(d => d.Id == decisionId);
    
    if (decision == null)
        throw new NotFoundException($"Decision {decisionId} not found");
    
    if (decision.Status == DecisionStatus.Locked)
        throw new InvalidOperationException($"Decision is already locked by {decision.LockedByUser.Name}");
    
    decision.Status = DecisionStatus.Locked;
    decision.LockedBy = userId;
    decision.LockedAt = DateTime.UtcNow;
    decision.UpdatedAt = DateTime.UtcNow;
    
    await _context.SaveChangesAsync();
    
    _logger.LogInformation("Decision {DecisionId} locked by user {UserId}", decisionId, userId);
    
    return decision;
}

public async Task<Decision> UnlockDecisionAsync(Guid decisionId, Guid userId, string reason)
{
    var decision = await _context.Decisions.FindAsync(decisionId);
    
    if (decision == null)
        throw new NotFoundException($"Decision {decisionId} not found");
    
    if (decision.Status != DecisionStatus.Locked)
        throw new InvalidOperationException("Decision is not locked");
    
    decision.Status = DecisionStatus.Draft;
    decision.LockedBy = null;
    decision.LockedAt = null;
    decision.UnlockReason = reason;
    decision.UpdatedAt = DateTime.UtcNow;
    
    await _context.SaveChangesAsync();
    
    _logger.LogInformation("Decision {DecisionId} unlocked by user {UserId} with reason: {Reason}", 
        decisionId, userId, reason);
    
    return decision;
}

// Update existing UpdateDecisionAsync to check lock status
public async Task<Decision> UpdateDecisionAsync(Guid decisionId, UpdateDecisionRequest request)
{
    var decision = await _context.Decisions.FindAsync(decisionId);
    
    if (decision == null)
        throw new NotFoundException($"Decision {decisionId} not found");
    
    // NEW: Lock validation
    if (decision.Status == DecisionStatus.Locked)
        throw new LockedDecisionException("Decision is locked. Unlock to modify.");
    
    // ... rest of update logic ...
}
```

### Controller Layer Implementation

**DecisionController Lock Endpoints:**
```csharp
[ApiController]
[Route("api/v1/decisions")]
public class DecisionController : ControllerBase
{
    private readonly IDecisionService _decisionService;
    private readonly ILogger<DecisionController> _logger;
    
    [HttpPost("{id}/lock")]
    [Authorize(Roles = "Participant,Admin")]
    [ProducesResponseType(typeof(DecisionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DecisionDto>> LockDecision(Guid id)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
        
        try
        {
            var decision = await _decisionService.LockDecisionAsync(id, userId);
            var dto = MapToDto(decision);
            return Ok(dto);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new ProblemDetails { Detail = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ProblemDetails { Detail = ex.Message });
        }
    }
    
    [HttpPost("{id}/unlock")]
    [Authorize(Roles = "Participant,Admin")]
    [ProducesResponseType(typeof(DecisionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DecisionDto>> UnlockDecision(
        Guid id, 
        [FromBody] UnlockDecisionRequest request)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
        
        try
        {
            var decision = await _decisionService.UnlockDecisionAsync(id, userId, request.Reason);
            var dto = MapToDto(decision);
            return Ok(dto);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new ProblemDetails { Detail = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ProblemDetails { Detail = ex.Message });
        }
    }
    
    [HttpPut("{id}")]
    [Authorize(Roles = "Participant,Admin")]
    public async Task<ActionResult<DecisionDto>> UpdateDecision(
        Guid id, 
        [FromBody] UpdateDecisionRequest request)
    {
        try
        {
            var decision = await _decisionService.UpdateDecisionAsync(id, request);
            return Ok(MapToDto(decision));
        }
        catch (LockedDecisionException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, 
                new ProblemDetails { Detail = ex.Message });
        }
        // ... other exception handlers ...
    }
}
```

### DTOs and Request/Response Models

**New DTOs for Story 6.3:**
```csharp
public class UnlockDecisionRequest
{
    [Required]
    [StringLength(500, MinimumLength = 5)]
    public string Reason { get; set; }
}

public class DecisionDto
{
    public Guid Id { get; set; }
    public Guid WorkflowInstanceId { get; set; }
    public Guid StepId { get; set; }
    public string DecisionType { get; set; }
    public object Value { get; set; }
    public object Context { get; set; }
    public Guid DecidedBy { get; set; }
    public string DecidedByName { get; set; }
    public DateTime DecidedAt { get; set; }
    public int Version { get; set; }
    public string Status { get; set; }
    
    // Story 6.3 fields
    public bool IsLocked => Status == "Locked";
    public Guid? LockedBy { get; set; }
    public string LockedByName { get; set; }
    public DateTime? LockedAt { get; set; }
    public string UnlockReason { get; set; }
}

public class LockedDecisionException : Exception
{
    public LockedDecisionException(string message) : base(message) { }
}
```


### Project Structure

**Files to Create/Modify:**
- `src/bmadServer.ApiService/Models/Decision.cs` - **MODIFY** - Add locking fields (LockedBy, LockedAt, UnlockReason)
- `src/bmadServer.ApiService/Models/DecisionStatus.cs` - **VERIFY** - Ensure Locked status exists in enum
- `src/bmadServer.ApiService/Data/Migrations/AddDecisionLockingFields.cs` - **CREATE** - Migration for new fields
- `src/bmadServer.ApiService/Services/DecisionService.cs` - **MODIFY** - Add LockDecisionAsync, UnlockDecisionAsync, update UpdateDecisionAsync
- `src/bmadServer.ApiService/Services/IDecisionService.cs` - **MODIFY** - Add lock/unlock method signatures
- `src/bmadServer.ApiService/Controllers/DecisionController.cs` - **MODIFY** - Add lock/unlock endpoints
- `src/bmadServer.ApiService/DTOs/UnlockDecisionRequest.cs` - **CREATE** - Request DTO for unlock reason
- `src/bmadServer.ApiService/DTOs/DecisionDto.cs` - **MODIFY** - Add lock metadata fields (IsLocked, LockedBy, LockedByName, LockedAt)
- `src/bmadServer.ApiService/Exceptions/LockedDecisionException.cs` - **CREATE** - Custom exception for locked decision updates
- `src/bmadServer.Tests/Unit/Services/DecisionServiceTests.cs` - **MODIFY** - Add lock/unlock test cases
- `src/bmadServer.Tests/Integration/Controllers/DecisionControllerTests.cs` - **MODIFY** - Add lock/unlock API tests

### Testing Strategy

**Unit Tests (DecisionServiceTests.cs):**
- `LockDecision_ShouldSetStatusAndMetadata_WhenDecisionUnlocked`
- `LockDecision_ShouldThrowException_WhenDecisionAlreadyLocked`
- `UnlockDecision_ShouldClearLockFields_AndLogReason`
- `UnlockDecision_ShouldThrowException_WhenDecisionNotLocked`
- `UpdateDecision_ShouldThrowLockedDecisionException_WhenLocked`
- `UpdateDecision_ShouldSucceed_WhenUnlocked`

**Integration Tests (DecisionControllerTests.cs):**
- `POST_Lock_ReturnsOk_WithLockedDecision`
- `POST_Lock_Returns403_WhenViewerRole`
- `POST_Lock_Returns409_WhenAlreadyLocked`
- `POST_Unlock_ReturnsOk_WithUnlockedDecision`
- `POST_Unlock_PersistsReason_InDatabase`
- `PUT_Update_Returns403_WhenDecisionLocked`
- `PUT_Update_ReturnsOk_WhenDecisionUnlocked`

### Previous Story Learnings (Story 6.2)

**Key Insights from Decision Version History Implementation:**
- Decision entity uses EF Core with PostgreSQL and JSONB columns
- DecisionService follows async/await patterns with proper logging
- Authorization is enforced via [Authorize(Roles = "...")] attributes
- ProblemDetails RFC 7807 is used for standardized error responses
- Migrations are generated with `dotnet ef migrations add` command
- Unit tests use in-memory database for service layer testing
- Integration tests use real PostgreSQL with test containers

**Code Patterns to Follow:**
- Use ILogger<T> for structured logging with correlation IDs
- Throw domain-specific exceptions (e.g., NotFoundException, LockedDecisionException)
- Map exceptions to appropriate HTTP status codes in controller
- Include navigation properties with `.Include()` when fetching related entities
- Use DateTime.UtcNow for all timestamps
- Return DTOs from controllers, not entity models directly

### Architecture Alignment

**Component Mapping:**
- **Decision Tracker Component** (Architecture.md lines 276-281): This story extends the Decision Tracker with lock/unlock lifecycle management
- **State Persistence Layer** (Architecture.md lines 297-300): PostgreSQL with EF Core for atomic lock operations
- **Authorization** (Architecture.md lines 29-37): RBAC from Epic 2 enforces Participant/Admin roles for lock operations
- **Audit Trail** (Architecture.md line 281): All lock/unlock operations logged for compliance

**Technology Stack Alignment:**
- **Backend:** .NET 10 with ASP.NET Core (Architecture.md line 13)
- **ORM:** EF Core 9.0 with PostgreSQL JSONB (Architecture.md line 24)
- **Database:** PostgreSQL 17.x LTS (Architecture.md line 28)
- **Authentication:** ASP.NET Core Identity with JWT (Architecture.md lines 30-36)
- **API Design:** REST with ProblemDetails RFC 7807 (Architecture.md line 40)
- **Documentation:** OpenAPI 3.1 + Swagger UI (Architecture.md line 41)

### Security Considerations

**Authorization Rules:**
- Only Participant and Admin roles can lock/unlock decisions (Viewer role forbidden)
- Use `[Authorize(Roles = "Participant,Admin")]` on lock/unlock endpoints
- Verify userId from JWT claims matches authenticated user
- Log all lock/unlock operations with userId for audit trail

**Input Validation:**
- Unlock reason required (minimum 5 characters, maximum 500)
- Decision ID must be valid Guid
- Decision must exist in database before lock/unlock
- Cannot lock already locked decision (409 Conflict)
- Cannot unlock unlocked decision (409 Conflict)

**Concurrency Handling:**
- Decision entity has Version field for optimistic concurrency (Story 6.1)
- Lock operation updates Version to detect concurrent modifications
- Use `SaveChangesAsync()` to atomically update lock status and metadata

### API Documentation

**OpenAPI Examples:**

```yaml
/api/v1/decisions/{id}/lock:
  post:
    summary: Lock a decision to prevent modifications
    operationId: lockDecision
    security:
      - bearerAuth: []
    parameters:
      - name: id
        in: path
        required: true
        schema:
          type: string
          format: uuid
    responses:
      200:
        description: Decision locked successfully
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/DecisionDto'
            example:
              id: "3fa85f64-5717-4562-b3fc-2c963f66afa6"
              status: "Locked"
              lockedBy: "7c9e6679-7425-40de-944b-e07fc1f90ae7"
              lockedByName: "Sarah Connor"
              lockedAt: "2026-01-25T12:00:00Z"
      403:
        description: Forbidden - Viewer role cannot lock decisions
      404:
        description: Decision not found
      409:
        description: Decision is already locked

/api/v1/decisions/{id}/unlock:
  post:
    summary: Unlock a decision to allow modifications
    operationId: unlockDecision
    security:
      - bearerAuth: []
    parameters:
      - name: id
        in: path
        required: true
        schema:
          type: string
          format: uuid
    requestBody:
      required: true
      content:
        application/json:
          schema:
            $ref: '#/components/schemas/UnlockDecisionRequest'
          example:
            reason: "Decision needs revision based on new requirements"
    responses:
      200:
        description: Decision unlocked successfully
      403:
        description: Forbidden - Viewer role cannot unlock decisions
      404:
        description: Decision not found
      409:
        description: Decision is not locked
```

### Frontend Integration Guidance (AC 5)

**UI Implementation Notes for Story 6.3:**

The frontend should integrate with the lock/unlock API as follows:

1. **Decision Display Component:**
   - Check `decisionDto.isLocked` field to determine lock status
   - Display lock icon (🔒) when `isLocked === true`
   - Show "Locked by {lockedByName} on {lockedAt}" text
   - Format lockedAt as relative time (e.g., "2 hours ago")

2. **Edit Controls:**
   - Disable all edit buttons when `isLocked === true`
   - Add tooltip: "This decision is locked. Unlock to make changes."
   - Show "Unlock" button to Participant/Admin roles when locked
   - Show "Lock" button to Participant/Admin roles when unlocked

3. **API Integration:**
   ```typescript
   // Lock decision
   const lockDecision = async (decisionId: string) => {
     const response = await fetch(`/api/v1/decisions/${decisionId}/lock`, {
       method: 'POST',
       headers: {
         'Authorization': `Bearer ${token}`,
       },
     });
     if (response.status === 403) {
       showError('You do not have permission to lock this decision');
     }
     return await response.json();
   };

   // Unlock decision
   const unlockDecision = async (decisionId: string, reason: string) => {
     const response = await fetch(`/api/v1/decisions/${decisionId}/unlock`, {
       method: 'POST',
       headers: {
         'Authorization': `Bearer ${token}`,
         'Content-Type': 'application/json',
       },
       body: JSON.stringify({ reason }),
     });
     return await response.json();
   };
   ```

4. **User Experience:**
   - Prompt for unlock reason in modal dialog
   - Validate reason is 5-500 characters
   - Show confirmation message after lock/unlock
   - Refresh decision data after lock/unlock to show updated status
   - Display lock status in decision list view (small lock icon)

---

## Latest Technical Specifications

### EF Core 9.0 Best Practices (2026)

**Migration Generation:**
- Use `dotnet ef migrations add <MigrationName>` from ApiService project directory
- Always review generated migration before committing
- Test migration locally with `dotnet ef database update`
- Use nullable types (Guid?, DateTime?) for optional fields
- Add indexes on foreign keys and frequently queried columns

**JSONB Support in PostgreSQL:**
- Use `.HasColumnType("jsonb")` in entity configuration
- GIN indexes improve JSONB query performance
- Use `.HasMethod("gin")` when creating JSONB indexes
- EF Core 9.0 has native support for PostgreSQL JSONB operators

**Async Patterns:**
- Always use async/await for database operations
- Use `FirstOrDefaultAsync()` instead of `FirstOrDefault()`
- Use `SaveChangesAsync()` instead of `SaveChanges()`
- Include `.ConfigureAwait(false)` is no longer required in ASP.NET Core

### ASP.NET Core 10 Authorization (2026)

**Role-Based Authorization:**
- Use `[Authorize(Roles = "Role1,Role2")]` for multiple roles
- Roles are stored as claims in JWT token
- Access roles via `User.IsInRole("RoleName")` or `User.FindFirst(ClaimTypes.Role)`
- Use policy-based authorization for complex rules

**Middleware Order:**
```csharp
app.UseAuthentication(); // Must come before UseAuthorization
app.UseAuthorization();
app.MapControllers();
```

### ProblemDetails RFC 7807 (Current Standard)

**Error Response Format:**
```csharp
return StatusCode(403, new ProblemDetails
{
    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.3",
    Title = "Forbidden",
    Status = 403,
    Detail = "Decision is locked. Unlock to modify.",
    Instance = $"/api/v1/decisions/{id}"
});
```

**Custom Exception Mapping:**
- Create custom exception classes for domain errors
- Map to appropriate HTTP status codes in controller
- Use middleware for global exception handling

### PostgreSQL 17.x Features (2026)

**Performance Optimizations:**
- Use GIN indexes for JSONB columns (faster than GiST)
- Incremental VACUUM for large tables
- Partial indexes for filtered queries: `CREATE INDEX ... WHERE Status = 'Locked'`
- Use JSONB operators for efficient querying: `@>`, `?`, `?&`, `?|`

**Connection Pooling:**
- Aspire manages connection pooling automatically
- Default pool size: 100 connections
- Connection lifetime: 30 minutes
- Use `builder.AddServiceDefaults()` to inherit pool configuration

---

## Git Intelligence & Codebase Patterns

### Recent Development Patterns (from Epic 4 & Early Epic 6)

**Service Layer Pattern:**
- Services inherit from base service class (if exists)
- Constructor injection for dependencies: `IDbContext`, `ILogger<T>`
- Public async methods for business operations
- Private helper methods for internal logic
- Throw domain exceptions, catch in controller

**Controller Pattern:**
- Inherit from `ControllerBase` (API controllers)
- Use `[ApiController]` attribute for automatic model validation
- Use `[Route("api/v1/[controller]")]` for versioned routing
- Return `ActionResult<T>` for typed responses
- Use `ProducesResponseType` attributes for OpenAPI documentation

**Testing Pattern:**
- Unit tests use in-memory database or mocks
- Integration tests use Testcontainers for PostgreSQL
- Arrange-Act-Assert pattern for test structure
- Use `FluentAssertions` for readable assertions
- Test file naming: `{ClassName}Tests.cs`

### Existing Codebase Structure (from src/ analysis)

**Project Organization:**
- `bmadServer.ApiService` - Main API project with controllers, services, models
- `bmadServer.ServiceDefaults` - Shared services and extensions (Aspire defaults)
- `bmadServer.AppHost` - Aspire orchestration and service configuration
- `bmadServer.Tests` - Unit and integration tests
- `bmadServer.Web` - Frontend React application

**Established Conventions:**
- Use `ILogger<T>` for structured logging
- Use `IMemoryCache` for in-process caching (Redis-ready interface)
- Authentication via JWT with refresh tokens (Epic 2)
- Session management with idle timeout (Epic 2, Story 2.6)
- Workflow orchestration engine (Epic 4)

---

## Critical Developer Guardrails

### MUST DO ✅

1. **Add locking validation to UpdateDecisionAsync** - This is critical to prevent modifications to locked decisions
2. **Use proper authorization** - Only Participant/Admin can lock/unlock, enforce via [Authorize] attribute
3. **Log all lock/unlock operations** - Required for audit trail and compliance
4. **Include navigation properties** - Use `.Include(d => d.LockedByUser)` to load user names for display
5. **Test lock validation** - Verify 403 Forbidden is returned when updating locked decision
6. **Use nullable types** - LockedBy and LockedAt must be nullable (null when unlocked)
7. **Persist unlock reason** - Store reason in UnlockReason field for audit trail
8. **Update OpenAPI docs** - Document new endpoints and DTOs in Swagger

### MUST NOT DO ❌

1. **Do NOT allow Viewer role to lock/unlock** - This violates RBAC security requirements
2. **Do NOT skip lock validation in UpdateDecisionAsync** - This creates security vulnerability
3. **Do NOT use DateTime.Now** - Always use DateTime.UtcNow for consistency across timezones
4. **Do NOT forget migration** - Database schema changes require EF Core migration
5. **Do NOT expose Decision entity directly** - Always return DecisionDto from API endpoints
6. **Do NOT hardcode userId** - Extract from JWT claims via User.FindFirst(ClaimTypes.NameIdentifier)
7. **Do NOT skip authorization tests** - Verify role-based access control in integration tests
8. **Do NOT forget error handling** - Map exceptions to ProblemDetails with appropriate status codes

### Edge Cases to Handle

1. **Decision not found** - Return 404 NotFound with clear error message
2. **Already locked** - Return 409 Conflict when trying to lock locked decision
3. **Not locked** - Return 409 Conflict when trying to unlock unlocked decision
4. **Concurrent updates** - EF Core Version field handles optimistic concurrency
5. **Missing unlock reason** - Validate reason is provided and meets length requirements
6. **Invalid decision ID** - Validate Guid format, return 400 BadRequest for invalid format
7. **Unauthorized user** - Return 403 Forbidden with clear message about role requirements

### Performance Considerations

- Add index on LockedBy field for query performance (included in migration)
- Use `.AsNoTracking()` for read-only queries to improve performance
- Consider caching locked decision IDs if performance becomes issue
- Lock/unlock operations are infrequent, no need for aggressive optimization

---

## Implementation Checklist

### Phase 1: Database Schema (1-2 hours)
- [ ] Add locking fields to Decision.cs entity model
- [ ] Update DecisionStatus enum if Locked status missing
- [ ] Create EF Core migration with `dotnet ef migrations add AddDecisionLockingFields`
- [ ] Review generated migration SQL
- [ ] Test migration locally with `dotnet ef database update`
- [ ] Commit migration files

### Phase 2: Service Layer (2-3 hours)
- [ ] Add LockDecisionAsync method to IDecisionService interface
- [ ] Add UnlockDecisionAsync method to IDecisionService interface
- [ ] Implement LockDecisionAsync in DecisionService with validation
- [ ] Implement UnlockDecisionAsync in DecisionService with reason logging
- [ ] Update UpdateDecisionAsync with lock validation (throw LockedDecisionException)
- [ ] Add structured logging for all lock/unlock operations
- [ ] Create LockedDecisionException custom exception class
- [ ] Test service layer methods manually or with unit tests

### Phase 3: API Layer (2-3 hours)
- [ ] Create UnlockDecisionRequest DTO with validation attributes
- [ ] Update DecisionDto with lock metadata fields (IsLocked, LockedBy, etc.)
- [ ] Add POST /lock endpoint to DecisionController with authorization
- [ ] Add POST /unlock endpoint to DecisionController with authorization
- [ ] Update PUT endpoint to catch LockedDecisionException and return 403
- [ ] Add ProducesResponseType attributes for OpenAPI documentation
- [ ] Test endpoints manually with Swagger UI or Postman

### Phase 4: Testing (3-4 hours)
- [ ] Write unit tests for LockDecisionAsync (success and failure cases)
- [ ] Write unit tests for UnlockDecisionAsync (success and failure cases)
- [ ] Write unit tests for UpdateDecisionAsync lock validation
- [ ] Write integration tests for POST /lock endpoint
- [ ] Write integration tests for POST /unlock endpoint
- [ ] Write integration tests for PUT endpoint with locked decision
- [ ] Write integration tests for role-based authorization
- [ ] Run all tests and verify 100% pass rate

### Phase 5: Documentation (1 hour)
- [ ] Update OpenAPI/Swagger documentation with new endpoints
- [ ] Add XML documentation comments to controller methods
- [ ] Document request/response examples in OpenAPI
- [ ] Update DecisionDto schema in OpenAPI with lock fields
- [ ] Verify Swagger UI displays new endpoints correctly

### Phase 6: Code Review & QA (1-2 hours)
- [ ] Self-review all code changes
- [ ] Verify all acceptance criteria are met
- [ ] Run full test suite (unit + integration)
- [ ] Test with real PostgreSQL database
- [ ] Verify authorization rules work correctly
- [ ] Check logging output for lock/unlock operations
- [ ] Test error scenarios (404, 403, 409)
- [ ] Prepare for code review

**Estimated Total Time:** 10-15 hours

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

- Source: [epics.md - Story 6.3](../planning-artifacts/epics.md)
- Architecture: [architecture.md](../planning-artifacts/architecture.md)
- PRD: [prd.md](../planning-artifacts/prd.md)

---

## Dev Agent Record

### Agent Model Used

_To be filled by dev agent during implementation_

### Debug Log References

_To be filled by dev agent during implementation_

### Completion Notes List

_To be filled by dev agent during implementation_

### File List

_To be filled by dev agent during implementation_

---

## Story Completion Status

**Status:** ready-for-dev  
**Created:** 2026-01-25  
**Context Analysis:** Complete  
**Developer Readiness:** ✅ Comprehensive

**Ultimate BMad Method Story Context Created!**

This story file has been enhanced with:
- ✅ Complete architecture context from Architecture.md
- ✅ Detailed implementation guidance with code examples
- ✅ Previous story learnings from Story 6.2
- ✅ Latest technical specifications for EF Core 9.0 and ASP.NET Core 10
- ✅ Security considerations and authorization rules
- ✅ Comprehensive testing strategy with specific test cases
- ✅ Frontend integration guidance for UI components
- ✅ Critical developer guardrails (MUST DO / MUST NOT DO)
- ✅ Edge case handling and performance considerations
- ✅ Step-by-step implementation checklist with time estimates

**Developer has everything needed for flawless implementation!**
