# bmadServer: Universal Project-Wide Rules

## 🎯 CRITICAL ASPIRE-FIRST RULES

These rules apply to **ALL** development work across bmadServer and supersede any other guidance.

### Rule 1: Aspire CLI BEFORE dotnet Commands

**ALWAYS use `aspire` CLI commands before using `dotnet` commands.**

```bash
# ✅ CORRECT: Use aspire CLI first
aspire new aspire-starter --name MyProject
aspire add [component-name]
aspire run

# ❌ WRONG: Using dotnet directly for Aspire work
dotnet new aspnet
dotnet add package
dotnet run
```

**Why?**
- Aspire CLI ensures correct project structure and configuration
- Aspire CLI automatically wires up orchestration in AppHost
- `dotnet` commands may work but skip critical Aspire setup
- Aspire CLI maintains consistency across the team

**When to use `dotnet` commands:**
- After Aspire has created/configured the project
- For project-specific tasks: `dotnet build`, `dotnet publish`
- For EF Core migrations: `dotnet ef migrations add`
- For tool installation: `dotnet tool install --global`

---

### Rule 2: Aspire Add-ons FIRST, Then Manual Configuration

**ALWAYS use Aspire add-ons before manually configuring components.**

Use official Aspire add-ons from:
- 📚 https://github.com/microsoft/aspire
- 📚 https://github.com/microsoft/aspire-samples
- 🔗 https://aspire.dev (official documentation)

```bash
# ✅ CORRECT: Use Aspire add-ons
aspire add PostgreSQL.Server       # Adds PostgreSQL component
aspire add Redis.Distributed       # Adds Redis cache
aspire add Seq.Aspire              # Adds Seq logging
aspire add Dapr.Aspire             # Adds Dapr runtime
aspire add MySql.Server            # Adds MySQL

# ❌ WRONG: Manual Docker setup
docker run -d postgres:17 -e POSTGRES_PASSWORD=...
# (This bypasses Aspire orchestration and service discovery)
```

**What "Aspire Add-ons" includes:**
- Official `Aspire.Hosting.*` NuGet packages (Microsoft)
- Official `Aspire.Components.*` packages for integrations
- Community add-ons from https://github.com/microsoft/aspire#components
- Database components: PostgreSQL, MySQL, MongoDB, etc.
- Cache components: Redis, Memcached
- Messaging: RabbitMQ, Kafka
- Observability: Seq, Jaeger, Grafana
- Cloud: Azure Container Apps, AWS

---

### Rule 3: Documentation Source Priority

**Use this priority order for documentation:**

1. **🥇 PRIMARY**: https://aspire.dev (official Microsoft documentation)
2. **🥈 SECONDARY**: https://github.com/microsoft/aspire (source code + samples)
3. **🥉 TERTIARY**: Microsoft Learn (filtered to Aspire content only)
4. ❌ **NOT ACCEPTABLE**: Generic Docker/Docker Compose tutorials
5. ❌ **NOT ACCEPTABLE**: Manual environment setup guides

**Why?**
- aspire.dev is maintained by Microsoft and always current
- github.com/microsoft/aspire has working code examples
- Aspire-specific docs prevent anti-patterns
- Generic Docker tutorials don't understand Aspire orchestration

---

## 🔬 OBSERVABILITY: TRACING & LOGGING AS DEFAULT

### Rule 4: OpenTelemetry Tracing from Day 1

**EVERY story must include OpenTelemetry tracing and structured logging. This is NOT optional.**

**Why?**
- Distributed tracing is required for multi-service debugging
- Structured logging (JSON) enables log aggregation
- Aspire Dashboard displays traces automatically
- Finding bugs in production requires observability
- Early implementation is far easier than retrofitting later

**Implementation (Standard Pattern):**

```csharp
// ServiceDefaults/Extensions.cs - Shared pattern for ALL services
public static IServiceCollection AddServiceDefaults(
    this IServiceCollection services) =>
    services
        .AddOpenTelemetry()
        .WithMetrics(metrics => metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation())
        .WithTracing(tracing => tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddEntityFrameworkCoreInstrumentation())
        .Build();
```

**What this provides:**
- ✅ Automatic request tracing (W3C Trace Context)
- ✅ Structured JSON logging
- ✅ HTTP client timing
- ✅ Database query tracking
- ✅ Visible in Aspire Dashboard

### Rule 5: Health Checks for Every Service

**Every service MUST have a /health endpoint configured in AppHost and responding properly.**

```csharp
// AppHost.cs
var api = builder.AddProject<Projects.ApiService>("api")
    .WithHttpHealthCheck("/health")        // ← REQUIRED
    .WithReference(db)
    .WaitFor(db);
```

```csharp
// ApiService/Program.cs
builder.MapDefaultEndpoints();  // Maps /health and /alive

// Or manually:
[HttpGet("/health")]
public IActionResult Health()
{
    return Ok(new { status = "healthy" });
}
```

**Requirements:**
- [ ] /health endpoint configured
- [ ] Returns 200 OK when healthy
- [ ] Includes database connectivity check
- [ ] Visible in Aspire Dashboard
- [ ] Used in startup ordering (WaitFor)

### Rule 6: Research Before Implementing Observability Features

**If you need observability features, check Aspire packages FIRST.**

Available Aspire observability packages:
- `Aspire.Hosting.OpenTelemetry` - Distributed tracing
- `Aspire.Hosting.Prometheus` - Metrics collection
- `Aspire.Hosting.Grafana` - Metrics visualization
- `Aspire.Hosting.Seq` - Log aggregation
- `Aspire.Hosting.Jaeger` - Trace visualization

**Pattern:**
```bash
# ✅ CORRECT: Use Aspire components
aspire add Seq.Aspire      # Add logging aggregation
aspire add Prometheus.Aspire  # Add metrics collection

# ❌ WRONG: Manual implementation
# Trying to implement your own structured logging library
# when Aspire components already exist
```

---

## 📋 PRACTICAL WORKFLOW

### When Adding a New Service

**Step 1: Find the Aspire component** on https://aspire.dev
```
Example: Need PostgreSQL?
→ Visit: https://aspire.dev
→ Search: "PostgreSQL"
→ Find: "Aspire.Hosting.PostgreSQL"
```

**Step 2: Use Aspire add-on command**
```bash
cd /Users/cris/bmadServer/src
aspire add PostgreSQL.Server
```

**Step 3: Configure in AppHost/Program.cs**
```csharp
// In bmadServer.AppHost/Program.cs
var postgres = builder.AddPostgres("postgres");
var db = postgres.AddDatabase("bmadserver", "bmadserver_dev");

var api = builder.AddProject<Projects.bmadServer_ApiService>("api")
    .WithReference(db);  // Automatic service discovery
```

**Step 4: Use connection in ApiService**
```csharp
// In bmadServer.ApiService/Program.cs
builder.AddServiceDefaults();  // Aspire service configuration
builder.Services.AddDbContext<ApplicationDbContext>();

// Connection string auto-injected from Aspire
```

**Step 5: Run with Aspire**
```bash
aspire run
```

### When Adding a Package

**✅ CORRECT WORKFLOW:**

1. Check if it's available as an Aspire component first
   ```bash
   # Visit https://aspire.dev and search for the component
   ```

2. If Aspire component exists, use `aspire add`:
   ```bash
   aspire add [ComponentName]
   ```

3. If NO Aspire component exists, then use `dotnet add package`:
   ```bash
   cd src/bmadServer.ApiService
   dotnet add package SomeNugetPackage
   ```

4. Configure in AppHost and ApiService as needed

---

## 🛑 ANTI-PATTERNS: NEVER DO THIS

### ❌ Anti-Pattern 1: Docker Compose for Local Development
```bash
# WRONG: Creating docker-compose.yml manually
version: '3.8'
services:
  postgres:
    image: postgres:17

# CORRECT: Use Aspire add-on instead
aspire add PostgreSQL.Server
```

**Why?**
- Aspire provides automatic service discovery
- Aspire dashboard shows all services
- Aspire handles health checks
- One `aspire run` command starts everything

### ❌ Anti-Pattern 2: Manual Configuration Before Aspire
```bash
# WRONG: Setting up database manually
docker run -d postgres:17 ...
psql -h localhost -U postgres ...

# CORRECT: Let Aspire manage it
aspire run  # PostgreSQL starts automatically
```

### ❌ Anti-Pattern 3: Using dotnet Commands for Aspire Setup
```bash
# WRONG: Using dotnet new for Aspire projects
dotnet new aspnet --name bmadServer

# CORRECT: Use Aspire CLI
aspire new aspire-starter --name bmadServer
```

### ❌ Anti-Pattern 4: Mixing Multiple Orchestration Tools
```bash
# WRONG: Using Docker Compose AND Aspire
aspire run
docker-compose up  # Don't do this!

# CORRECT: Aspire only
aspire run
```

### ❌ Anti-Pattern 5: Ignoring Aspire Components
```bash
# WRONG: Installing tools separately
npm install redis-cli
brew install postgresql-client

# CORRECT: Let Aspire provide them
aspire add Redis.Distributed
aspire add PostgreSQL.Server
# Run with aspire run - no installation needed
```

---

## 🔍 DECISION TREE: Which Command to Use?

```
┌─ Need to work with the project?
│
├─ YES, setting up NEW service/component
│  ├─ Check https://aspire.dev for component
│  ├─ Component exists?
│  │  ├─ YES → aspire add [ComponentName]
│  │  └─ NO → dotnet add package [PackageName]
│  └─ Configure in AppHost and ApiService
│
├─ YES, building the project
│  └─ aspire run  (from src directory)
│
├─ YES, running tests
│  └─ dotnet test  (from test project directory)
│
├─ YES, creating migrations
│  ├─ Start: aspire run (in another terminal)
│  └─ Run: dotnet ef migrations add [MigrationName]
│
├─ YES, publishing for deployment
│  └─ dotnet publish -c Release
│
└─ YES, daily development
   └─ aspire run  (everything starts automatically)
```

---

## 📚 REFERENCE LINKS

### Official Aspire Documentation
- **Main Docs**: https://aspire.dev
- **GitHub Repo**: https://github.com/microsoft/aspire
- **Samples**: https://github.com/microsoft/aspire-samples
- **Components**: https://github.com/microsoft/aspire#components

### Quick Component Reference
| Component | Command | Docs |
|-----------|---------|------|
| PostgreSQL | `aspire add PostgreSQL.Server` | [Link](https://aspire.dev) |
| Redis | `aspire add Redis.Distributed` | [Link](https://aspire.dev) |
| MySQL | `aspire add MySql.Server` | [Link](https://aspire.dev) |
| MongoDB | `aspire add MongoDB.Aspire` | [Link](https://aspire.dev) |
| RabbitMQ | `aspire add RabbitMq.Aspire` | [Link](https://aspire.dev) |
| Seq | `aspire add Seq.Aspire` | [Link](https://aspire.dev) |
| Dapr | `aspire add Dapr.Aspire` | [Link](https://aspire.dev) |

### EF Core + Aspire Pattern
```bash
# Start Aspire with database
aspire run

# In another terminal, add migration
dotnet ef migrations add InitialCreate --project src/bmadServer.ApiService

# Apply migration (while aspire run is still going)
dotnet ef database update --project src/bmadServer.ApiService
```

---

## ✅ VERIFICATION CHECKLIST

When starting a new story, verify:

- [ ] Checked https://aspire.dev for available components
- [ ] Used `aspire add` for any new services/components
- [ ] Updated AppHost/Program.cs with new resources
- [ ] Services are discoverable via Aspire (not hardcoded connection strings)
- [ ] Can start everything with single `aspire run` command
- [ ] All services visible in Aspire Dashboard (https://localhost:17360)
- [ ] No Docker Compose files created (use Aspire instead)
- [ ] No manual environment setup needed (all in AppHost)
- [ ] Documentation references point to aspire.dev

---

## 🚀 QUICK START REMINDER

```bash
# Navigate to src
cd /Users/cris/bmadServer/src

# Build and run with Aspire
aspire run

# Everything starts automatically!
# Dashboard: https://localhost:17360
# API: http://localhost:8080/health
```

---

## 📚 BEST PRACTICES & ADDITIONAL RESOURCES

### Complete Aspire Best Practices Guide
See: [ASPIRE-BEST-PRACTICES.md](./ASPIRE-BEST-PRACTICES.md)

This comprehensive guide covers:
- Research-first approach (check official docs)
- All available Aspire components
- Observability patterns
- Testing strategies
- Story checklist
- Anti-patterns to avoid

### Official Documentation Priority

1. **🥇 PRIMARY**: https://aspire.dev (official Microsoft docs)
2. **🥈 SECONDARY**: https://github.com/microsoft/aspire (source code + samples)
3. **🥉 TERTIARY**: https://learn.microsoft.com/en-us/dotnet/aspire
4. **NO**: Generic Docker tutorials (don't apply to Aspire)

### Key Learning from Epic 1

**Don't assume framework capabilities—always research:**
- .NET 10 exists (it's the current stable LTS)
- Aspire has packages for most patterns
- Official docs are always authoritative
- When in doubt, look it up!

---

## 📞 REFERENCES

When in doubt:
1. Check https://aspire.dev for official docs
2. Look at https://github.com/microsoft/aspire-samples for examples
3. Review the rule: "Aspire CLI first, then dotnet"
4. Remember: If it's not an `aspire` command, ask if it should be

---

**Last Updated**: 2026-01-24  
**Status**: Active - Enforce on all stories  
**Maintainer**: Development Team
