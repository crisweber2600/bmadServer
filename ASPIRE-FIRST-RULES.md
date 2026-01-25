# UNIVERSAL ASPIRE-FIRST DEVELOPMENT RULES
## bmadServer Project - Story 1-2 Implementation

**Effective Date:** January 24, 2026  
**Project:** bmadServer (Cloud-Native Workflow Orchestration Platform)  
**Framework:** .NET 10.0+ with .NET Aspire 13.1.0+

---

## 🎯 CORE PRINCIPLE: ASPIRE FIRST

Every development decision follows this hierarchy:

```
1️⃣  Aspire CLI (aspire new, aspire add, aspire run, aspire mcp)
2️⃣  Aspire.Hosting.* NuGet packages (from aspire_list_integrations)
3️⃣  Aspire-compatible third-party packages
4️⃣  Generic .NET packages (ONLY as fallback)
```

**NEVER** use `dotnet new`, `dotnet add`, or `dotnet run` for Aspire projects.  
**ALWAYS** verify Aspire package exists before adding ANY NuGet package.

---

## 📋 MANDATORY RULES

### Rule 1: Package Resolution
- **Before adding any NuGet package**: Call `aspire_list_integrations`
- **Search for**: Aspire.Hosting.* packages matching your need
- **If found**: Use `aspire_get_integration_docs` to get official docs
- **If NOT found**: Only then consider third-party packages
- **Example**: Need PostgreSQL?
  - ✅ Check for `Aspire.Hosting.PostgreSQL` (FOUND → use it)
  - ❌ Don't add `Npgsql.EntityFrameworkCore.PostgreSQL` directly

### Rule 2: Documentation Authority
- **Primary Source**: `aspire_get_integration_docs` via Aspire MCP
- **Secondary Source**: Microsoft Docs MCP (for .NET framework docs)
- **Tertiary Source**: Official GitHub repos
- **NEVER**: Trust cached or outdated web searches

### Rule 3: Project Setup
- **ALWAYS use Aspire CLI**: `aspire new aspire-starter --output {path}`
- **Add components via**: `aspire add {component-name}` (or manual .csproj editing with verified versions)
- **Never use**: `dotnet new aspnet`, `dotnet new web`, etc.

### Rule 4: Service Registration
- **AppHost.cs**: Define all resources (databases, services, etc.)
- **Program.cs**: Register with `builder.AddServiceDefaults()` and inject from Aspire
- **All configuration**: Comes from Aspire resource definitions, never hardcoded

### Rule 5: Testing & Validation
- Use `aspire_list_resources` to verify services are running
- Use `aspire_list_structured_logs` to debug issues
- Use `aspire_list_traces` for distributed tracing analysis
- **Never**: Open browser, check logs manually, or guess at connectivity

### Rule 6: File Paths & URLs
- **Always absolute paths**: `/Users/cris/bmadServer/...`
- **Never relative**: Don't use `../` or `./`
- **Connection strings**: Always injected by Aspire via environment variables

---

## 🔧 MANDATORY TOOLS & MCPs

### Aspire MCP (aspire_* functions)
- `aspire_list_integrations` - Find available packages
- `aspire_get_integration_docs` - Get official docs
- `aspire_list_resources` - Verify running services
- `aspire_list_console_logs` - Debug output
- `aspire_list_structured_logs` - Structured telemetry
- `aspire_list_traces` - Distributed tracing

### Microsoft Docs MCP (microsoftdocs/mcp)
- **Setup Required**: https://github.com/microsoftdocs/mcp
- **Use for**: .NET framework docs, EF Core, ASP.NET Core patterns
- **When**: Before implementing any framework feature

### Bash + Aspire CLI
- `export PATH="/opt/homebrew/Cellar/dotnet/10.0.102/bin:$PATH"` - Ensure .NET 10
- `aspire new aspire-starter` - Create projects
- `aspire add {package}` - Add integrations
- `aspire run` - Start orchestration

---

## 📊 STORY 1-2 IMPLEMENTATION CHECKLIST

Using **ONLY** Aspire CLI and approved packages:

- [ ] Task 1: Aspire PostgreSQL resource (via aspire add or verified .csproj)
- [ ] Task 2: ApiService configured to receive database reference
- [ ] Task 3: Verify dashboard shows PostgreSQL running
- [ ] Task 4: Add EF Core packages (via Aspire-approved versions)
- [ ] Task 5: Create DbContext with entities
- [ ] Task 6: Register DbContext in Program.cs via Aspire
- [ ] Task 7: Create migrations (via aspire CLI context, not dotnet CLI)
- [ ] Task 8: Apply migrations against Aspire-managed database
- [ ] Task 9: Verify all Aspire resources healthy
- [ ] Task 10: Update sprint-status.yaml to "review"

---

## ⚠️ ANTI-PATTERNS (NEVER DO THESE)

```csharp
// ❌ WRONG: Using dotnet CLI
dotnet new aspnet
dotnet add package Npgsql
dotnet run

// ❌ WRONG: Hardcoded connection strings
var conn = "Server=localhost;Database=mydb;";

// ❌ WRONG: Mixing Aspire with manual setup
var postgres = builder.AddPostgres("db");
// Then separately configuring connection in appsettings.json

// ❌ WRONG: Ignoring Aspire packages in favor of generic ones
// (Adding EF Core without verifying Aspire version first)
```

```csharp
// ✅ CORRECT: Using Aspire CLI
aspire new aspire-starter
aspire add PostgreSQL.Server

// ✅ CORRECT: Aspire-injected connection strings
// In AppHost:
var db = builder.AddPostgres("pgsql").AddDatabase("mydb");
var api = builder.AddProject<Projects.ApiService>("api")
    .WithReference(db);

// In ApiService Program.cs:
builder.AddServiceDefaults();  // Aspire handles connection injection
builder.Services.AddDbContext<MyDbContext>();
```

---

## 📝 FILE STRUCTURE (Story 1-2)

```
/Users/cris/bmadServer/
├── src/
│   ├── bmadServer.AppHost/
│   │   ├── AppHost.cs                          ← PostgreSQL resource definition
│   │   └── bmadServer.AppHost.csproj          ← Aspire.Hosting.PostgreSQL package
│   ├── bmadServer.ApiService/
│   │   ├── Program.cs                          ← DbContext registration
│   │   ├── Data/
│   │   │   ├── ApplicationDbContext.cs         ← EF Core context
│   │   │   ├── Entities/
│   │   │   │   ├── User.cs
│   │   │   │   ├── Session.cs
│   │   │   │   └── Workflow.cs
│   │   │   └── Migrations/
│   │   │       ├── 20260124000001_InitialCreate.cs
│   │   │       └── ApplicationDbContextModelSnapshot.cs
│   │   └── bmadServer.ApiService.csproj       ← EF Core packages (Aspire-verified)
│   ├── bmadServer.ServiceDefaults/
│   ├── bmadServer.Web/
│   └── bmadServer.sln
├── _bmad-output/
│   └── implementation-artifacts/
│       ├── sprint-status.yaml                  ← Update after completion
│       └── 1-2-configure-postgresql.md         ← This story
└── docs/                                        ← Project knowledge base
```

---

## 🚀 NEXT IMMEDIATE ACTIONS

1. **Verify .NET 10**: `export PATH="/opt/homebrew/Cellar/dotnet/10.0.102/bin:$PATH"`
2. **List Aspire integrations**: `aspire_list_integrations` → Search for PostgreSQL
3. **Get official docs**: `aspire_get_integration_docs` for Aspire.Hosting.PostgreSQL
4. **Check AppHost setup**: Verify PostgreSQL resource defined correctly
5. **Verify ApiService**: Confirm DbContext registration and entities
6. **Use Aspire MCP**: `aspire_list_resources` to verify PostgreSQL running
7. **Create migrations**: Via Aspire context runner (NOT `dotnet ef`)
8. **Mark tasks complete**: Update todo list progressively
9. **Run code review**: Before marking story "done"

---

**Created by:** Amelia (Dev Agent)  
**Last Updated:** 2026-01-24 15:07 UTC  
**Status:** ENFORCED for all future development
