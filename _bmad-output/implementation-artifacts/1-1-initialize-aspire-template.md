# Story 1.1: Initialize bmadServer from .NET Aspire Starter Template

**Status:** ready-for-dev

## Story

As a developer,
I want to bootstrap bmadServer from the .NET Aspire Starter template,
so that I have a cloud-native project structure with service orchestration built-in.

## Acceptance Criteria

**Given** I have .NET 10 SDK installed  
**When** I run `aspire new aspire-starter --name bmadServer`  
**Then** the project structure is created with:
  - bmadServer.AppHost (service orchestration)
  - bmadServer.ApiService (REST API + SignalR)
  - bmadServer.ServiceDefaults (shared resilience patterns)
  - Directory.Build.props (solution-wide settings)  
**And** when I run `dotnet build`, the solution compiles without errors

**Given** the project is built  
**When** I run `aspire run`  
**Then** the Aspire dashboard appears at https://localhost:17360  
**And** the dashboard shows bmadServer.ApiService as "running"  
**And** the API responds to GET /health with 200 OK status

**Given** the AppHost is running  
**When** I check the AppHost logs  
**Then** I see structured JSON logs with trace IDs  
**And** distributed tracing infrastructure is configured  
**And** health checks are registered and operational

**Given** development environment is complete  
**When** I read `/bmadServer.ApiService/Program.cs`  
**Then** I can identify clear integration points for:
  - Service registration (builders.Services.Add*)
  - Middleware configuration (app.Use*)
  - SignalR hub mapping (app.MapHub)  
**And** documentation explains how to add new services

## Tasks / Subtasks

- [ ] **Task 1: Create project from Aspire Starter template** (AC: All AC #1)
  - [ ] Install .NET 10 SDK or verify installation
  - [ ] Run aspire new aspire-starter --name bmadServer
  - [ ] Verify project structure matches expected layout
  - [ ] Verify Directory.Build.props contains solution-wide settings
  - [ ] Run dotnet restore to ensure all dependencies install

- [ ] **Task 2: Build and verify baseline project** (AC: All AC #1-2)
  - [ ] **CRITICAL**: Use `aspire run` (NOT `dotnet run`) per PROJECT-WIDE-RULES.md
  - [ ] Run dotnet build in solution root (build only, don't run)
  - [ ] Verify no compilation errors
  - [ ] Verify no warnings (if any appear, document them)
  - [ ] Run `aspire run` to start the development environment
  - [ ] Verify Aspire dashboard appears at https://localhost:17360
  - [ ] Verify bmadServer.ApiService shows as "running"

- [ ] **Task 3: Configure health check endpoint** (AC: All AC #2-3)
  - [ ] Add health check middleware to ApiService Program.cs
  - [ ] Map /health endpoint returning 200 OK
  - [ ] Include database connection status in health check response
  - [ ] Include dependency status (basic info)
  - [ ] Test GET /health returns expected JSON structure

- [ ] **Task 4: Configure structured logging and tracing** (AC: All AC #3)
  - [ ] Add structured logging provider to AppHost
  - [ ] Configure JSON logging output with trace IDs
  - [ ] Verify logs appear as structured JSON in console
  - [ ] Verify trace IDs are generated and included in logs
  - [ ] Confirm distributed tracing infrastructure is operational

- [ ] **Task 5: Document service registration patterns** (AC: All AC #4)
  - [ ] Review Program.cs for service registration patterns
  - [ ] Document how builders.Services.Add* works
  - [ ] Identify middleware configuration points (app.Use*)
  - [ ] Document SignalR hub mapping patterns
  - [ ] Create inline code comments explaining integration points
  - [ ] Update project README with setup instructions

## Dev Notes

### Project Structure Notes

The .NET Aspire Starter template provides a cloud-native baseline with:
- **bmadServer.AppHost**: Orchestration project that defines services and their relationships
- **bmadServer.ApiService**: Main REST API service (port 8080 by default)
- **bmadServer.ServiceDefaults**: Shared resilience, telemetry, and health check patterns
- **Directory.Build.props**: Solution-wide MSBuild properties (e.g., language features, analyzers)

This story establishes the foundation for all downstream work (Auth, Chat, Workflows, etc.).

### Architecture Alignment

Per architecture.md requirements:
- Framework: .NET 10 with ASP.NET Core and Aspire orchestration ✅
- Real-time: SignalR WebSocket (NuGet: Microsoft.AspNetCore.SignalR) - added in Epic 3
- State: PostgreSQL with Event Log - configured in Epic 1 Story 2
- Agents: In-process (MVP), Queue-ready interface - implemented in Epic 4+

### Known Integration Points

**Program.cs Service Registration:**
- `builder.Services.AddSignalR()` - will be added in Epic 3
- `builder.Services.AddDbContext<ApplicationDbContext>()` - will be added in Epic 1 Story 2
- `builder.Services.AddAuthentication()` - will be added in Epic 2 Story 1
- `builder.Services.AddAuthorization()` - will be added in Epic 2 Story 5

**Middleware Pipeline:**
- `app.UseAuthentication()` - Epic 2
- `app.UseAuthorization()` - Epic 2
- `app.MapHub<ChatHub>("/hubs/chat")` - Epic 3
- `app.MapControllers()` - existing
- Health checks already mapped to `/health`

### References

- **🥇 PRIMARY**: [aspire.dev](https://aspire.dev) - Official Microsoft documentation
- **🥈 SECONDARY**: [GitHub: microsoft/aspire](https://github.com/microsoft/aspire) - Source and samples
- [PROJECT-WIDE-RULES.md](../../PROJECT-WIDE-RULES.md) - Universal Aspire-first development rules
- [ASPIRE_SETUP_GUIDE.md](../../ASPIRE_SETUP_GUIDE.md) - Development environment setup
- [Epic 1: Aspire Foundation & Project Setup](../../planning-artifacts/epics.md#epic-1-aspire-foundation--project-setup)
- [Architecture: Starter Template & Project Setup](../../planning-artifacts/architecture.md#starter-template--project-setup)
- [Architecture: Aspire Orchestration](../../planning-artifacts/architecture.md#aspire-orchestration)

## Dev Agent Record

### Agent Model Used

Claude 3.5 Sonnet

### Debug Log References

None yet (first story)

### Completion Notes List

- Story created and marked ready-for-dev
- All acceptance criteria extracted from epics.md
- Integration points documented for future stories
- Architecture alignment verified

### File List

- /Users/cris/bmadServer/bmadServer.AppHost/Program.cs
- /Users/cris/bmadServer/bmadServer.ApiService/Program.cs
- /Users/cris/bmadServer/Directory.Build.props
- /Users/cris/bmadServer/README.md (to be created with setup instructions)

## Story Context & Developer Intelligence

### Epic 1 Objectives

This story is the **first story** in Epic 1: Aspire Foundation & Project Setup. It enables all downstream work by establishing the baseline .NET Aspire project structure with:
- Service orchestration infrastructure
- Baseline health checks and monitoring
- Structured logging and distributed tracing
- Clear integration points for service registration

### Why This Matters

The .NET Aspire Starter template is a curated, best-practice baseline from Microsoft. Using it ensures:
- ✅ Correct project structure from the start (prevents restructuring later)
- ✅ Built-in resilience patterns (retries, timeouts, circuit breakers)
- ✅ Built-in health check infrastructure
- ✅ Built-in telemetry (structured logging, distributed tracing)
- ✅ Correct service orchestration for AppHost pattern

**Skipping this step or using `dotnet new aspnet` instead would:**
- ❌ Require manual setup of AppHost for orchestration
- ❌ Require manual setup of resilience patterns
- ❌ Require manual setup of telemetry
- ❌ Prevent using Aspire for development dashboard

### Previous Story Context

**This is the first story** - no previous stories to learn from. This establishes the baseline for all future stories.

### Related Stories in Epic 1

- **1-2**: Configure PostgreSQL (depends on 1-1 project structure)
- **1-3**: Docker Compose orchestration (uses 1-1 API structure + 1-2 database)
- **1-4**: GitHub Actions CI/CD (uses 1-1 project structure)
- **1-5**: Prometheus + Grafana (uses 1-1 health checks + structured logging)
- **1-6**: Documentation (documents 1-1 through 1-5)

### Implementation Strategy

1. **Create the project** using Aspire CLI - this is the "golden path" recommended by Microsoft
2. **Verify structure** matches expected layout from Aspire Starter
3. **Run baseline health checks** to ensure development environment works
4. **Document integration points** so future stories know where to add services

### Critical Success Factors

- ✅ Use `aspire new aspire-starter --name bmadServer` (NOT manual template setup)
- ✅ Verify Aspire dashboard at https://localhost:17360 works
- ✅ Verify API responds to /health endpoint
- ✅ Document service registration patterns for team
- ✅ All acceptance criteria pass (build, run, health checks, structured logs)

### Common Pitfalls to Avoid

- ❌ Using `dotnet new aspnet` instead of Aspire template (missing orchestration)
- ❌ Running `aspire run` without understanding AppHost concept
- ❌ Not verifying health check endpoint works
- ❌ Not documenting service registration patterns for future stories
- ❌ Assuming Aspire dashboard is optional (it's critical for local development)

### Testing Approach

**Manual verification steps:**
1. Run `aspire new aspire-starter --name bmadServer` - check output
2. Run `dotnet build` - verify no errors
3. Run `aspire run` - verify dashboard appears
4. Check `https://localhost:17360` - verify API shows "running"
5. Check `http://localhost:8080/health` - verify 200 OK response
6. Check console logs - verify JSON structured format with trace IDs

**No automated tests needed** - this is infrastructure setup story.

### Estimated Effort

- **Time**: 2-3 hours (including baseline verification and documentation)
- **Complexity**: Low (mostly following Aspire CLI wizard)
- **Risk**: Very low (using Microsoft's official template reduces custom configuration)

### Development Environment

**Required:**
- .NET 10 SDK installed
- Git repo initialized
- Terminal/command prompt access

**Recommended Tools:**
- Visual Studio Code with C# extension
- Visual Studio 2024 (optional but helpful for .NET development)
- Docker (for later stories, not needed for 1-1)

---

**Story created and ready for development. Developer should proceed with running the Aspire CLI command and following the wizard.**
