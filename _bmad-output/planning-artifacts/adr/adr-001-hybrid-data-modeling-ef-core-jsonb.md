# ADR-001: Hybrid Data Modeling with Entity Framework Core + PostgreSQL JSONB

**Date:** 2026-01-23  
**Status:** ACCEPTED  
**Category:** 1 - Data Architecture  
**Decision ID:** 1.1

---

## Context

bmadServer requires storing two distinct types of data:
1. **Stable relational data:** Users, sessions, workflow metadata (schema is fixed)
2. **Dynamic workflow state:** BMAD workflows, decisions, context (schema evolves frequently)

Traditional fully-normalized relational databases force schema migrations for each new workflow type or state structure. This creates friction during rapid MVP development. Conversely, pure document stores sacrifice type safety and relational integrity.

**Architectural Drivers:**
- Multiple concurrent workflows with evolving schemas (PRD, Architecture, Epics + Stories)
- Type safety required for critical entities (Users, Sessions)
- Multi-user collaboration with pessimistic locking requirements
- BMAD workflow parity (must handle any workflow type CLI supports)

---

## Decision

**Adopt Hybrid Data Modeling:**
- **Relational Models (EF Core):** Users, Sessions, WorkflowMetadata, AuditLog entities
- **Document Models (JSONB):** Workflow state, decisions, context (stored as PostgreSQL JSONB columns)
- **Technology Stack:**
  - Entity Framework Core 9.0 (strongly-typed models, migrations, relationships)
  - PostgreSQL 17.x LTS (JSONB support, full-text search, GIN indexes)
  - System.Text.Json (serialization/deserialization with custom converters)

---

## Rationale

### Why Not Pure Relational?
- ❌ **Inflexible schemas:** Each new workflow type requires new tables + migration
- ❌ **Migration overhead:** Every team sprint needs DBA review (friction)
- ❌ **Parity burden:** Modeling n-ary tree (workflow steps) in relational model is complex

### Why Not Pure Document (NoSQL)?
- ❌ **No type safety:** Easy to introduce data corruption bugs
- ❌ **No relationships:** Multi-table joins require application code
- ❌ **Audit trail:** Event sourcing adds complexity not needed for MVP

### Why Hybrid?
- ✅ **Type safety** where it matters (entities with fixed schema)
- ✅ **Schema flexibility** for evolving workflows (JSONB documents)
- ✅ **Best of both worlds:** Relational integrity + document flexibility
- ✅ **PostgreSQL advantage:** JSONB operators for querying nested data
- ✅ **Future-proof:** Easy to extract JSONB -> relational tables if patterns emerge

---

## Implementation Pattern

### EF Core Models (Relational)

```csharp
// DbContext configuration
public class BmadServerContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Session> Sessions { get; set; }
    public DbSet<WorkflowMetadata> Workflows { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // User entity - standard relational model
        modelBuilder.Entity<User>()
            .HasKey(u => u.Id);
        modelBuilder.Entity<User>()
            .Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(255);
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        // Workflow metadata - relational header with JSONB state
        modelBuilder.Entity<WorkflowMetadata>()
            .HasKey(w => w.Id);
        modelBuilder.Entity<WorkflowMetadata>()
            .Property(w => w.State)
            .HasColumnType("jsonb")  // PostgreSQL JSONB type
            .HasDefaultValue("{}");
        
        // GIN index for JSONB queries (critical for performance)
        modelBuilder.Entity<WorkflowMetadata>()
            .HasIndex(w => w.State)
            .HasMethod("gin");
    }
}

// User entity
public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string[] Roles { get; set; } = ["Participant"];
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    
    // Navigation
    public ICollection<Session> Sessions { get; set; } = [];
}

// Session entity
public class Session
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? LastActivityAt { get; set; }
    
    // Navigation
    public User User { get; set; } = null!;
}

// Workflow metadata (relational header)
public class WorkflowMetadata
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = "default";
    public string WorkflowType { get; set; } = string.Empty; // "prd", "architecture", etc.
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // JSONB document storage
    public JsonDocument State { get; set; } = JsonDocument.Parse("{}");
    
    // Concurrency control (required for multi-user collaboration)
    public int Version { get; set; } = 1;
    public string? LastModifiedBy { get; set; }
    public DateTime? LastModifiedAt { get; set; }
}
```

### JSONB State Structure

```json
{
  "_schemaVersion": "1.0.0",
  "_version": 5,
  "_lastModifiedBy": "marcus-user-id",
  "_lastModifiedAt": "2026-01-23T10:30:00Z",
  "status": "in_progress",
  "currentStep": 5,
  "totalSteps": 12,
  "workflowType": "prd",
  "participants": [
    {
      "userId": "cris-user-id",
      "role": "admin",
      "lastActive": "2026-01-23T10:30:00Z"
    }
  ],
  "decisions": [
    {
      "id": "dec-001",
      "proposal": "Use JWT for authentication",
      "proposedBy": "marcus",
      "status": "locked",
      "confidence": 0.95,
      "lockedAt": "2026-01-23T10:15:00Z",
      "lockedBy": "cris"
    }
  ],
  "context": {
    "bmadVersion": "6.0.0",
    "workflowParity": "full_match",
    "checkpoints": {
      "step_1_complete": "2026-01-23T09:00:00Z",
      "step_4_complete": "2026-01-23T10:00:00Z"
    }
  }
}
```

### Updating JSONB with Optimistic Concurrency

```csharp
// Service layer
public class WorkflowService
{
    private readonly BmadServerContext _context;
    private readonly ILogger<WorkflowService> _logger;

    public async Task<WorkflowMetadata> UpdateDecisionAsync(
        Guid workflowId, 
        DecisionUpdateRequest request,
        CancellationToken ct = default)
    {
        var workflow = await _context.Workflows
            .FirstOrDefaultAsync(w => w.Id == workflowId, ct)
            ?? throw new WorkflowNotFoundException(workflowId);

        // Check version for optimistic concurrency
        if (workflow.Version != request.ExpectedVersion)
        {
            throw new WorkflowConflictException(
                expectedVersion: request.ExpectedVersion,
                actualVersion: workflow.Version,
                lastModifiedBy: workflow.LastModifiedBy,
                lastModifiedAt: workflow.LastModifiedAt);
        }

        // Parse JSONB state
        var state = JsonDocument.Parse(workflow.State.RootElement.GetRawText());
        var stateDict = JsonSerializer.Deserialize<Dictionary<string, object>>(state.RootElement.GetRawText())
            ?? new();

        // Update decision within JSONB
        var decisions = stateDict.GetValueOrDefault("decisions") as IList<object> ?? new List<object>();
        var decision = decisions.OfType<Dictionary<string, object>>()
            .FirstOrDefault(d => d["id"]?.ToString() == request.DecisionId)
            ?? throw new DecisionNotFoundException(request.DecisionId);

        decision["status"] = "locked";
        decision["lockedAt"] = DateTime.UtcNow;
        decision["lockedBy"] = request.UserId;

        stateDict["decisions"] = decisions;
        stateDict["_version"] = workflow.Version + 1;  // Increment version
        stateDict["_lastModifiedBy"] = request.UserId;
        stateDict["_lastModifiedAt"] = DateTime.UtcNow;

        // Serialize back to JSONB
        var newStateJson = JsonSerializer.Serialize(stateDict);
        workflow.State = JsonDocument.Parse(newStateJson);
        workflow.Version++;
        workflow.LastModifiedBy = request.UserId;
        workflow.UpdatedAt = DateTime.UtcNow;

        // Save with concurrency check
        try
        {
            await _context.SaveChangesAsync(ct);
            return workflow;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict updating workflow {WorkflowId}", workflowId);
            throw new WorkflowConflictException("Workflow was modified by another user");
        }
    }
}
```

### Querying JSONB Data

```csharp
// Query workflows by status (JSONB operator)
var activeWorkflows = await _context.Workflows
    .Where(w => EF.Functions.JsonContains(w.State, "{\"status\": \"in_progress\"}"))
    .ToListAsync();

// Query workflows with specific decision
var workflowsWithApprovals = await _context.Workflows
    .Where(w => w.State.ToString().Contains("\"status\": \"approved\""))
    .ToListAsync();

// Raw SQL for complex JSONB queries
var recentUpdates = await _context.Workflows
    .FromSqlRaw(@"
        SELECT * FROM workflows
        WHERE state->>'_lastModifiedAt' > NOW() - INTERVAL '1 hour'
        ORDER BY (state->>_lastModifiedAt') DESC
    ")
    .ToListAsync();
```

---

## Consequences

### Positive ✅
- **Type safety** for critical entities prevents data corruption
- **Schema flexibility** supports rapid workflow evolution
- **PostgreSQL leverage** - JSONB operators enable complex queries
- **Clear separation** - relational vs. document concerns are distinct
- **Migration efficiency** - new workflow types don't require schema changes

### Negative ⚠️
- **Application-level serialization** - must manually handle JSONB <-> object conversion
- **Query complexity** - JSONB queries use raw SQL or EF.Functions (less type-safe)
- **Index maintenance** - GIN indexes must be created for frequently-queried JSONB paths
- **Version management** - must track `_version` field manually for concurrency control
- **No database-level validation** - JSONB schema validation happens in application code

### Mitigations
- **Custom EF Core value converters** for automatic serialization/deserialization
- **Helper methods** for common JSONB queries (encapsulate complexity)
- **Migration tests** verify JSONB structure and schema versions
- **Concurrency tests** verify optimistic locking prevents conflicts

---

## Related Decisions

- **ADR-002:** Entity Framework Core Migrations (schema safety)
- **ADR-003:** Concurrency Control Strategy (optimistic locking with version fields)
- **ADR-004:** Validation Strategy (JSONB schema validation)
- **Category 2 - D2.3:** Encryption Strategy (how to encrypt JSONB at-rest in Phase 2)

---

## Alternatives Considered

### Alternative A: Fully Normalized Relational
- **Pros:** Type safety, clear schema, mature tools
- **Cons:** Inflexible, migration-heavy, breaks MVP velocity
- **Rejected:** Friction incompatible with rapid workflow evolution

### Alternative B: Pure Document Store (MongoDB)
- **Pros:** Schema flexibility, simple to scale horizontally
- **Cons:** No type safety, relational queries require app code, operational burden
- **Rejected:** Type safety and relational integrity needed for audit trail

---

## Implementation Checklist

- [ ] Configure DbContext with PostgreSQL JSONB columns (GIN indexes)
- [ ] Create System.Text.Json converters for JSONB serialization
- [ ] Implement optimistic concurrency pattern (`_version` field)
- [ ] Write migration tests (validate JSONB schema versions)
- [ ] Create helper methods for common JSONB queries
- [ ] Document JSONB schema structure and versioning strategy
- [ ] Add audit logging for all JSONB mutations
- [ ] Performance test JSONB queries with 1000+ workflows

---

## References

- [PostgreSQL JSONB Documentation](https://www.postgresql.org/docs/current/datatype-json.html)
- [Entity Framework Core Value Converters](https://learn.microsoft.com/en-us/ef/core/modeling/value-converters)
- [System.Text.Json Documentation](https://learn.microsoft.com/en-us/dotnet/api/system.text.json)
