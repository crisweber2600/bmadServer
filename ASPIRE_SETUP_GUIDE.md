# .NET Aspire Setup Guide for bmadServer

## ⚠️ UNIVERSAL PROJECT RULES (READ FIRST)

**These rules apply to ALL development on bmadServer:**

1. **Aspire CLI BEFORE dotnet Commands**: Always use `aspire` commands before `dotnet` commands
2. **Aspire Add-ons FIRST**: Use Aspire components before manual configuration
3. **Documentation Source**: Reference https://aspire.dev as primary documentation source

📋 **See PROJECT-WIDE-RULES.md for complete details and decision trees.**

---

## Overview

bmadServer uses **.NET Aspire** for cloud-native application orchestration and local development. Aspire provides:

- 🎯 **Unified Service Orchestration**: Start all services (API, Database, etc.) with a single command
- 🎨 **Developer Dashboard**: Real-time monitoring, logs, and traces at https://localhost:17360
- 🔌 **Automatic Service Discovery**: Services automatically discover and connect to each other
- 📊 **Built-in Observability**: Structured logging, distributed tracing, and metrics out of the box
- 🏥 **Health Checks**: Integrated health monitoring for all services
- 🐳 **Container-Ready**: Aspire configurations translate to production Dockerfiles and Kubernetes manifests

## Project Structure

```
bmadServer/
├── src/                              # Source code
│   ├── bmadServer.sln               # Solution file
│   ├── bmadServer.AppHost/          # Orchestration project (defines services)
│   │   ├── Program.cs               # Service definitions and relationships
│   │   └── bmadServer.AppHost.csproj
│   ├── bmadServer.ApiService/       # Main REST API service
│   │   ├── Program.cs               # ASP.NET Core app setup
│   │   ├── Data/                    # Database entities and migrations
│   │   └── bmadServer.ApiService.csproj
│   ├── bmadServer.Web/              # Frontend (if applicable)
│   │   └── bmadServer.Web.csproj
│   └── bmadServer.ServiceDefaults/  # Shared resilience patterns
│       ├── Extensions.cs            # Service registration helpers
│       └── bmadServer.ServiceDefaults.csproj
├── _bmad-output/                    # Generated artifacts
│   ├── planning-artifacts/          # PRD, architecture, epics
│   └── implementation-artifacts/    # Story files, sprint status
└── ASPIRE_SETUP_GUIDE.md           # This file
```

## Getting Started

### Prerequisites

1. **Install .NET 9.0+ SDK** (or higher when .NET 10 is available)
   ```bash
   dotnet --version  # Verify installation
   ```

2. **Verify Aspire Workload**
   ```bash
   dotnet workload list | grep aspire
   ```

3. **Verify Docker** (used by Aspire for running services)
   ```bash
   docker --version
   docker run hello-world
   ```

### Starting Development Environment

**Command:**
```bash
cd /Users/cris/bmadServer/src
aspire run                            # Aspire builds and starts all services
```

**Important:** Always use `aspire run` first. Aspire CLI handles building and orchestration automatically.

**Expected Output:**
```
[13:45:22.123] Aspire version: 8.2.2
[13:45:22.456] Aspire Dashboard: https://localhost:17360
[13:45:23.789] Starting bmadServer.ApiService...
[13:45:24.123] Starting PostgreSQL...
```

### Accessing Services

| Service | URL | Purpose |
|---------|-----|---------|
| **Aspire Dashboard** | https://localhost:17360 | Monitor services, logs, traces |
| **API Health Check** | http://localhost:8080/health | Verify API is running |
| **PostgreSQL** | localhost:5432 | Database connection |
| **pgAdmin** (optional) | https://localhost:5050 | Database UI |

## Epic 1: Aspire Foundation & Project Setup

The current implementation covers:

### ✅ Story 1.1: Initialize Aspire Template
- **Status**: Ready for development
- **Scope**: Create project from Aspire Starter template
- **Integration Points**: AppHost, ApiService, ServiceDefaults
- **Deliverables**: Baseline project structure with service orchestration

### ✅ Story 1.2: Configure PostgreSQL via Aspire
- **Status**: Ready for development  
- **Scope**: Add PostgreSQL to AppHost orchestration
- **Scope**: Configure EF Core for database access
- **Deliverables**: Database resource in Aspire, initial migrations

### ❌ Story 1.3: Docker Compose Orchestration
- **Status**: CANCELLED (superseded by Aspire)
- **Reason**: Aspire provides superior orchestration with better DX
- **Alternative**: Use Aspire for local development; separate Docker/K8s configs for production

### ⏳ Story 1.4: GitHub Actions CI/CD
- **Status**: Backlog
- **Scope**: Set up automated builds and tests
- **Dependencies**: Completes after 1.1-1.2 baseline

### ❌ Story 1.5: Prometheus + Grafana
- **Status**: CANCELLED (superseded by Aspire Dashboard)
- **Reason**: Aspire built-in monitoring meets MVP needs
- **Alternative**: Add Prometheus for production long-term retention (post-MVP)

### ⏳ Story 1.6: Project Documentation
- **Status**: Backlog
- **Scope**: README, setup guides, architecture diagrams
- **Dependencies**: Completes after 1.1-1.2 implementation

## .NET Version Strategy

### Current: .NET 9.0.305
- Used for current development
- Fully supported for MVP implementation
- All tooling and libraries available

### Future: .NET 10 Migration
- Planned for future upgrade (when .NET 10 is released)
- No blocking issues identified
- Migration path documented in Story 1.1

## Working with Aspire

### Adding a New Service - ASPIRE-FIRST WORKFLOW

**ALWAYS follow this pattern:**

1. **Check https://aspire.dev for the Aspire component**
   - Search: "Component Name" (e.g., "PostgreSQL", "Redis")
   - Find the official `Aspire.Hosting.*` package

2. **Use Aspire add command**
   ```bash
   cd /Users/cris/bmadServer/src
   aspire add PostgreSQL.Server
   ```

3. **Configure in AppHost**
   ```csharp
   // In bmadServer.AppHost/Program.cs
   var postgres = builder.AddPostgres("postgres");
   var db = postgres.AddDatabase("bmadserver", "bmadserver_dev");
   ```

4. **Wire service into your project**
   ```csharp
   // In bmadServer.ApiService/Program.cs
   var api = builder.AddProject<Projects.bmadServer_ApiService>("api")
       .WithReference(postgres);
   ```

5. **Run with Aspire**
   ```bash
   aspire run
   ```

---

### Adding a NuGet Package

**Check for Aspire component FIRST:**

```bash
# Step 1: Visit https://aspire.dev and search for component
# Step 2: If found as Aspire component, use:
aspire add ComponentName

# Step 3: If NOT found as Aspire component, then use dotnet:
cd src/bmadServer.ApiService
dotnet add package PackageName
```

### Debugging Aspire Issues

**Check Service Logs:**
1. Open https://localhost:17360
2. Click on service name (e.g., "bmadServer.ApiService")
3. View structured logs in dashboard

**Rebuild After Changes:**
```bash
cd src
dotnet build
aspire run                    # Rebuild automatically detected
```

## Environment Configuration

### Development (Local)

All settings are managed by Aspire. Key services:
- **PostgreSQL**: localhost:5432 (managed by Aspire container)
- **API**: http://localhost:8080
- **Dashboard**: https://localhost:17360

### Testing

Use separate `appsettings.Testing.json` for test-specific settings (if needed).

### Production

Aspire configurations **will be converted** to:
- **Docker**: Dockerfile generation from Aspire projects
- **Kubernetes**: Manifest generation for cloud deployment
- **Cloud**: Azure Container Apps, AWS ECS, etc.

## Troubleshooting

### "Aspire Dashboard not accessible"
```bash
# Verify port 17360 is not in use
lsof -i :17360

# Try restarting Aspire
aspire run
```

### "PostgreSQL connection failed"
```bash
# Check PostgreSQL service in dashboard
# Verify connection string in Program.cs
# Ensure Docker is running
docker ps | grep postgres
```

### "Build errors after .csproj changes"
```bash
# Clean and rebuild
dotnet clean
dotnet build
aspire run
```

### "Port already in use"
```bash
# Find and kill process
lsof -i :8080
kill -9 <PID>

# Or change AppHost port configuration
```

## Next Steps

1. **Complete Story 1.1**: Run Aspire template initialization
   - Verify dashboard loads at https://localhost:17360
   - Verify API health check returns 200 OK

2. **Complete Story 1.2**: Configure PostgreSQL
   - Add PostgreSQL to AppHost
   - Run migrations with EF Core
   - Test database connectivity

3. **Proceed to Epic 2**: User Authentication
   - Build on top of Aspire foundation
   - Add authentication services

## References

**🥇 PRIMARY DOCUMENTATION SOURCES (Official Microsoft):**
- **Aspire Official Docs**: https://aspire.dev
- **Aspire GitHub**: https://github.com/microsoft/aspire
- **Aspire Samples**: https://github.com/microsoft/aspire-samples
- **Aspire Components**: https://github.com/microsoft/aspire#components

**📚 SECONDARY REFERENCES:**
- [Microsoft Learn: .NET Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/)
- [Aspire Dashboard Docs](https://aspire.dev/dashboard/)
- [Aspire Components Reference](https://aspire.dev/components/)

**📋 PROJECT DOCUMENTATION:**
- [PROJECT-WIDE-RULES.md](./PROJECT-WIDE-RULES.md) - Universal Aspire-first rules
- [bmadServer Epic 1: Aspire Foundation](./_bmad-output/planning-artifacts/epics.md#epic-1-aspire-foundation--project-setup)
- [bmadServer Architecture](./_bmad-output/planning-artifacts/architecture.md)

**⚠️ CRITICAL: Always check aspire.dev FIRST before searching elsewhere**

---

**Last Updated**: 2026-01-24  
**Status**: Ready for development  
**Maintainer**: Development Team
