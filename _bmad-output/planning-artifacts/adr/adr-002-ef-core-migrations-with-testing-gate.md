# ADR-002: Entity Framework Core Migrations with Local Testing Gate

**Date:** 2026-01-23  
**Status:** ACCEPTED  
**Category:** 1 - Data Architecture  
**Decision ID:** 1.3

---

## Context

Database schema changes are among the riskiest operations in production systems. A failed migration can:
- Corrupt workflow state (data loss)
- Lock tables during high traffic (availability loss)
- Render application unable to start
- Leave the database in an inconsistent state

bmadServer requires a balance between **MVP velocity** (rapid iteration, quick schema changes) and **production safety** (verified migrations, rollback capability).

**Constraints:**
- Single-developer operations (Cris can review migrations)
- Local testing environment available for pre-flight checks
- Self-hosted deployment (no managed database service)
- 3-person team (conservative but not paranoid)

---

## Decision

**Use Entity Framework Core Migrations with Local Testing Gate:**

1. **Development:** Developers create EF Core migrations locally using `dotnet ef migrations add`
2. **Local Testing:** All migrations tested locally before commit (schema verification, data integrity)
3. **Git Versioning:** Migration files version-controlled in `/Data/Migrations/` directory
4. **Aspire AppHost:** Conditional automatic migration execution on startup (dev environment only)
5. **Production:** Manual review + `dotnet ef database update` after backup verification
6. **Rollback:** Every migration includes reversible logic (tested with `dotnet ef migrations remove`)

---

## Rationale

### Why Entity Framework Core?
- ✅ **Integrated with .NET Aspire** - no external tools, single deployment unit
- ✅ **Reversible migrations** - can rollback with `dotnet ef migrations remove`
- ✅ **Version-controlled** - full migration history in Git
- ✅ **Type-safe** - C# code, compile-time checking
- ✅ **PostgreSQL provider** - native JSONB support, GIN indexes

### Why Local Testing Gate?
- ✅ **Catch errors early** - developer tests before commit
- ✅ **Idempotent migrations** - verify rollback + re-apply works
- ✅ **Data integrity checks** - custom validation logic in migration
- ✅ **Audit trail** - all changes logged and reviewed
- ✅ **Production safety** - only tested migrations proceed to prod

### Why Not Automatic in Production?
- ❌ **Risk of downtime** - long-running migrations lock tables
- ❌ **No validation** - automatic execution skips business rule checks
- ❌ **Rollback complexity** - automatic rollback is unsafe
- ✅ Manual review allows: timing coordination, backup verification, team notification

---

## Implementation Pattern

### 1. Migration Creation (Developer Workflow)

```bash
# Developer modifies User.cs entity
# Add a new property: public string Department { get; set; }

# Generate migration
cd bmadServer/bmadServer.ApiService
dotnet ef migrations add AddUserDepartmentField

# Generated file: Data/Migrations/20260123_AddUserDepartmentField.cs
```

### 2. Migration File Structure

```csharp
// Generated migration with manual enhancements
public partial class AddUserDepartmentField : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Add column
        migrationBuilder.AddColumn<string>(
            name: "Department",
            table: "Users",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true);

        // Add index (if needed for queries)
        migrationBuilder.CreateIndex(
            name: "IX_Users_Department",
            table: "Users",
            column: "Department");

        // Data migration (populate existing records)
        migrationBuilder.Sql(@"
            UPDATE users
            SET department = 'Unassigned'
            WHERE department IS NULL;
        ");

        // Add constraint after data migration
        migrationBuilder.AlterColumn<string>(
            name: "Department",
            table: "Users",
            type: "character varying(100)",
            maxLength: 100,
            nullable: false);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Rollback (remove column)
        migrationBuilder.DropIndex(
            name: "IX_Users_Department",
            table: "Users");

        migrationBuilder.DropColumn(
            name: "Department",
            table: "Users");
    }
}
```

### 3. Local Testing Pattern

```csharp
// Integration test for migration
[TestFixture]
public class MigrationTests
{
    private IHost _host;
    private BmadServerContext _context;

    [OneTimeSetUp]
    public async Task SetUp()
    {
        // Start in-memory test database
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddDbContext<BmadServerContext>(options =>
                    options.UseNpgsql("Host=localhost;Database=bmadserver_test;..."));
            })
            .Build();

        _context = _host.Services.GetRequiredService<BmadServerContext>();
    }

    [Test]
    public async Task Migration_AddUserDepartmentField_AppliesSuccessfully()
    {
        // Apply migration
        await _context.Database.MigrateAsync();

        // Verify schema changes
        var table = _context.Model.FindEntityType(typeof(User));
        Assert.That(table?.FindProperty("Department"), Is.Not.Null);

        // Verify data integrity
        var users = await _context.Users.ToListAsync();
        Assert.That(users.All(u => u.Department != null), Is.True);
    }

    [Test]
    public async Task Migration_Rollback_ReversesChanges()
    {
        // Apply migration
        await _context.Database.MigrateAsync();

        // Verify schema exists
        Assert.That(_context.Model.FindEntityType(typeof(User))
            ?.FindProperty("Department"), Is.Not.Null);

        // Rollback (remove last migration)
        await _context.Database.ExecuteSqlAsync($@"
            DELETE FROM __efmigrationshistory
            WHERE migrationid = '20260123_AddUserDepartmentField'
        ");

        // Re-apply
        await _context.Database.MigrateAsync();

        // Verify schema restored
        Assert.That(_context.Model.FindEntityType(typeof(User))
            ?.FindProperty("Department"), Is.Not.Null);
    }
}
```

### 4. Aspire AppHost Automatic Migration (Dev Only)

```csharp
// Program.cs in bmadServer.ApiService
builder.Services.AddScoped<IStartupFilter>(sp =>
{
    return new MigrationStartupFilter(
        sp.GetRequiredService<ILogger<MigrationStartupFilter>>(),
        sp.GetRequiredService<BmadServerContext>());
});

// Custom startup filter
public class MigrationStartupFilter : IStartupFilter
{
    private readonly ILogger<MigrationStartupFilter> _logger;
    private readonly BmadServerContext _context;

    public MigrationStartupFilter(ILogger<MigrationStartupFilter> logger, BmadServerContext context)
    {
        _logger = logger;
        _context = context;
    }

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return async app =>
        {
            // Only run migrations in Development
            if (app.ApplicationServices.GetRequiredService<IHostEnvironment>().IsDevelopment())
            {
                try
                {
                    _logger.LogInformation("Running EF Core migrations on startup (Development only)");
                    await _context.Database.MigrateAsync();
                    _logger.LogInformation("Migrations completed successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Migration failed - application cannot start");
                    throw;
                }
            }
            else
            {
                _logger.LogInformation("Skipping automatic migrations in {Environment}", 
                    app.ApplicationServices.GetRequiredService<IHostEnvironment>().EnvironmentName);
            }

            next(app);
        };
    }
}
```

### 5. Production Migration Process

**Before Production Migration:**
1. Create database backup: `pg_dump bmadserver > backup-$(date +%s).sql`
2. Test migration on backup: Apply migration to backup, verify success
3. Schedule maintenance window (if long-running migration expected)
4. Notify team of planned downtime (if any)

**During Production Migration:**
```bash
# On production server
cd /opt/bmadserver
dotnet ef database update --connection "Host=prod-db;Database=bmadserver;..."

# Monitor progress
watch -n 1 "SELECT schemaversion, description FROM __efmigrationshistory ORDER BY installed_on DESC LIMIT 1"
```

**After Migration:**
1. Run health checks: `curl https://bmadserver.local/health`
2. Verify workflows can be loaded: `SELECT COUNT(*) FROM workflows`
3. Spot-check recent workflows for data integrity
4. Announce to team: "Migration complete, system operational"

---

## Consequences

### Positive ✅
- **Type-safe schema changes** - C# migration code is compiled, checked
- **Reversible migrations** - easy to rollback if issues discovered
- **Git history** - full audit trail of all schema changes
- **Integrated tooling** - no external migration tools, single deployment
- **Automatic dev environment** - developers don't manual manage local schemas

### Negative ⚠️
- **Manual production deployment** - requires Cris intervention for each migration
- **Testing burden** - each migration must be tested locally before commit
- **Long-running migrations** - potential downtime for large tables (mitigated by online DDL in PostgreSQL)
- **Coordination needed** - team must coordinate schema changes if working in parallel

### Mitigations
- **Online DDL** - PostgreSQL supports adding columns/indexes without table lock
- **Zero-downtime deployments** - deploy new code before running migrations
- **Rollback plan** - document rollback steps for each migration
- **Monitoring** - watch for slow queries after migration

---

## Data Integrity Checks

### Pre-Migration Validation

```csharp
// In migration Up() method
migrationBuilder.Sql(@"
    -- Verify no null values before adding NOT NULL constraint
    SELECT COUNT(*) as null_count
    FROM users
    WHERE email IS NULL;
    
    -- If count > 0, migration fails
    IF (SELECT COUNT(*) FROM users WHERE email IS NULL) > 0
    THEN
        RAISE EXCEPTION 'Cannot add NOT NULL constraint: email column has NULL values';
    END IF;
");
```

### Post-Migration Validation

```csharp
// In integration test
await _context.Database.MigrateAsync();

// Verify schema matches expectations
var userTable = _context.Model.FindEntityType(typeof(User));
var emailProperty = userTable?.FindProperty(nameof(User.Email));
Assert.That(emailProperty?.IsNullable, Is.False);

// Verify data integrity
var orphanedSessions = await _context.Sessions
    .Where(s => !_context.Users.Any(u => u.Id == s.UserId))
    .ToListAsync();
Assert.That(orphanedSessions, Is.Empty, "Orphaned sessions detected after migration");
```

---

## Sensitive Migrations

### Migration: Renaming Tables (High Risk)

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    // Step 1: Create new table
    migrationBuilder.CreateTable(
        name: "workflow_history",
        columns: table => new
        {
            id = table.Column<Guid>(type: "uuid", nullable: false),
            // ... columns
        },
        constraints: table => table.PrimaryKey("PK_workflow_history", x => x.id));

    // Step 2: Copy data
    migrationBuilder.Sql("INSERT INTO workflow_history SELECT * FROM workflows;");

    // Step 3: Update views/triggers if any
    migrationBuilder.Sql("DROP VIEW IF EXISTS active_workflows;");

    // Step 4: Drop old table (after verification period)
    migrationBuilder.DropTable(name: "workflows");
}

protected override void Down(MigrationBuilder migrationBuilder)
{
    // Rollback creates original table and restores data
    migrationBuilder.Sql("CREATE TABLE workflows AS SELECT * FROM workflow_history;");
    migrationBuilder.DropTable(name: "workflow_history");
}
```

### Migration: Adding Large Index (Performance Risk)

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    // Use CONCURRENTLY to avoid locking table
    // (EF doesn't support this directly, use raw SQL)
    migrationBuilder.Sql(@"
        CREATE INDEX CONCURRENTLY idx_workflows_tenant_status
        ON workflows (tenant_id, (state->>'status'))
        USING gin;
    ");
}
```

---

## Related Decisions

- **ADR-001:** Hybrid Data Modeling (what JSONB schema versions are validated)
- **ADR-003:** Concurrency Control (version field incremented in migrations)
- **ADR-004:** Validation Strategy (JSONB schema versions)

---

## Implementation Checklist

- [ ] Configure DbContext for PostgreSQL provider
- [ ] Create `/Data/Migrations/` directory for migration files
- [ ] Write MigrationStartupFilter for automatic dev environment migrations
- [ ] Create integration tests for each migration (Up + Down)
- [ ] Document production migration process (checklist, rollback steps)
- [ ] Set up backup + restore procedures for production
- [ ] Create monitoring dashboard for migration progress
- [ ] Add pre-migration health checks (backup verification, capacity checks)

---

## References

- [Entity Framework Core Migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
- [PostgreSQL Online DDL](https://www.postgresql.org/docs/current/sql-createindex.html#SQL-CREATEINDEX-CONCURRENTLY)
- [Production Deployment Patterns](https://learn.microsoft.com/en-us/aspnet/core/data/ef-rp/intro)
