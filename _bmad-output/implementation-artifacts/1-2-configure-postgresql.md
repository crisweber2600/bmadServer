# Story 1.2: Configure PostgreSQL Database via .NET Aspire

**Status:** ready-for-dev

## Story

As a developer,
I want to configure PostgreSQL as the primary data store using .NET Aspire,
so that I can persist workflow state, session data, and audit logs locally with managed orchestration.

## Acceptance Criteria

**Given** the bmadServer Aspire project exists (Story 1.1)  
**When** I run `dotnet package add --output Data` to add Aspire PostgreSQL integration  
**Then** Aspire.Hosting.PostgreSQL is available in the project

**Given** Aspire PostgreSQL is available  
**When** I configure the PostgreSQL service in bmadServer.AppHost/Program.cs  
**Then** the AppHost defines a `postgres` resource with:
  - PostgreSQL 17.x container image
  - Port 5432 exposed to localhost
  - POSTGRES_DB=bmadserver
  - POSTGRES_USER=bmadserver_dev
  - Named volume for persistence
  - Health check configured via Aspire
**And** when I run `aspire run`, the PostgreSQL container starts automatically

**Given** PostgreSQL is running via Aspire  
**When** I check the Aspire dashboard at https://localhost:17360  
**Then** I can see the PostgreSQL resource showing "running"  
**And** the dashboard shows connection endpoints for the database  
**And** I can view PostgreSQL logs in the Aspire dashboard

**Given** PostgreSQL is confirmed working  
**When** I add Microsoft.EntityFrameworkCore.Npgsql to the API project  
**Then** `dotnet add package Microsoft.EntityFrameworkCore.Npgsql` installs without errors

**Given** EF Core is added  
**When** I create `Data/ApplicationDbContext.cs` with DbContext  
**Then** the file includes:
  - Connection string from Aspire IConnectionStringProvider
  - DbSet<User>, DbSet<Session>, DbSet<Workflow> placeholders
  - OnConfiguring override that reads PostgreSQL connection from Aspire
**And** it references the PostgreSQL provider (NpgsqlConnection)

**Given** the DbContext is configured  
**When** I add dependency injection to `bmadServer.ApiService/Program.cs`:
  - `builder.AddServiceDefaults()` (from ServiceDefaults project)
  - `builder.Services.AddDbContext<ApplicationDbContext>()`
**Then** the API can initialize a DbContext without errors

**Given** DbContext is configured  
**When** I run `dotnet ef migrations add InitialCreate`  
**Then** a migration file is generated in `Data/Migrations/`  
**And** the migration includes CREATE TABLE statements for Users, Sessions, Workflows tables  
**And** the migration is version-controlled in git

**Given** a migration exists  
**When** I run `aspire run` and execute `dotnet ef database update` (from AppHost context)  
**Then** the database schema is created in the Aspire-managed PostgreSQL  
**And** I can query `SELECT table_name FROM information_schema.tables WHERE table_schema='public';`  
**And** all expected tables exist: users, sessions, workflows, __EFMigrationsHistory

## Tasks / Subtasks

- [ ] **Task 1: Add Aspire PostgreSQL integration to AppHost** (AC: #1)
  - [ ] Open bmadServer.AppHost/Program.cs
  - [ ] Add PostgreSQL resource: `var postgres = builder.AddPostgres("postgres").WithPgAdmin();`
  - [ ] Configure database: `postgres.AddDatabase("bmadserver", "bmadserver_dev");`
  - [ ] Add health check configuration in Aspire
  - [ ] Verify .csproj includes Aspire.Hosting.PostgreSQL package

- [ ] **Task 2: Configure ApiService to use Aspire PostgreSQL** (AC: #2)
  - [ ] Reference the postgres resource from AppHost: `builder.AddServiceDefaults();`
  - [ ] Update ApiService Program.cs to inject Aspire connection configuration
  - [ ] Verify ApiService.csproj includes Aspire.Hosting NuGet packages
  - [ ] Test that connection string is resolved from Aspire configuration

- [ ] **Task 3: Verify Aspire PostgreSQL in dashboard** (AC: #2-3)
  - [ ] Run `aspire run` from project root
  - [ ] Open Aspire dashboard at https://localhost:17360
  - [ ] Verify PostgreSQL resource shows "running"
  - [ ] Click PostgreSQL resource to view connection endpoints
  - [ ] Verify logs are visible in the Aspire dashboard
  - [ ] Test database connectivity from Aspire dashboard console

- [ ] **Task 4: Add Entity Framework Core with Npgsql** (AC: #3)
  - [ ] Run `dotnet add package Microsoft.EntityFrameworkCore.Npgsql` (ApiService)
  - [ ] Run `dotnet add package Microsoft.EntityFrameworkCore.Design` (ApiService)
  - [ ] Run `dotnet add package Microsoft.EntityFrameworkCore.Tools` (ApiService)
  - [ ] Verify packages appear in .csproj file
  - [ ] Run dotnet restore

- [ ] **Task 5: Create ApplicationDbContext** (AC: #4)
  - [ ] Create Data/ folder in bmadServer.ApiService
  - [ ] Create ApplicationDbContext.cs with DbContext base
  - [ ] Configure OnConfiguring to use Aspire connection provider
  - [ ] Add DbSet<User> Users property (placeholder entity)
  - [ ] Add DbSet<Session> Sessions property (placeholder entity)
  - [ ] Add DbSet<Workflow> Workflows property (placeholder entity)
  - [ ] Create placeholder entity classes (User.cs, Session.cs, Workflow.cs)

- [ ] **Task 6: Register DbContext in Program.cs** (AC: #4-5)
  - [ ] Add `builder.AddServiceDefaults()` for Aspire defaults
  - [ ] Register DbContext: `builder.Services.AddDbContext<ApplicationDbContext>()`
  - [ ] Verify connection string resolution from Aspire configuration
  - [ ] Test DbContext initialization at startup
  - [ ] Verify no connection errors in logs

- [ ] **Task 7: Create and run initial migration** (AC: #6-7)
  - [ ] Install dotnet-ef tool if needed: `dotnet tool install --global dotnet-ef`
  - [ ] Run `dotnet ef migrations add InitialCreate` (from ApiService directory)
  - [ ] Review generated migration file in Data/Migrations/
  - [ ] Verify CREATE TABLE statements for all entities
  - [ ] Run `dotnet ef database update` (while Aspire is running)
  - [ ] Verify tables exist in PostgreSQL using Aspire dashboard or pgAdmin
  - [ ] Commit migration files to git

## Dev Notes

### Project Structure Notes

Database configuration uses Aspire orchestration with EF Core:
- **bmadServer.AppHost/Program.cs**: PostgreSQL resource definition via Aspire
- **bmadServer.ApiService/Data/ApplicationDbContext.cs**: Main DbContext with entity sets
- **bmadServer.ApiService/Data/Entities/**: Entity classes (User.cs, Session.cs, Workflow.cs)
- **bmadServer.ApiService/Data/Migrations/**: EF Core migration files (auto-generated)
- **Aspire Dashboard**: Connection management and monitoring (https://localhost:17360)

### Architecture Alignment

Per architecture.md requirements:
- Data Modeling: Hybrid (EF Core + PostgreSQL JSONB) ✅
- Validation: EF Core Annotations + FluentValidation ✅
- Migrations: EF Core Migrations with Aspire orchestration ✅
- Database: PostgreSQL 17.x LTS (managed by Aspire) ✅
- Orchestration: .NET Aspire (containerized, health-checked) ✅

### Aspire PostgreSQL Integration

The Aspire approach provides:
```csharp
// In bmadServer.AppHost/Program.cs
var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin()  // Optional: adds pgAdmin UI at https://localhost:5050
    .AddDatabase("bmadserver", "bmadserver_dev");

// In bmadServer.ApiService/Program.cs
builder.AddServiceDefaults();  // Aspire service configuration
builder.Services.AddDbContext<ApplicationDbContext>();

// Connection string is automatically injected from Aspire
```

**Advantages over Docker Compose alone:**
- ✅ Single command startup: `aspire run`
- ✅ Unified logging in dashboard
- ✅ Built-in health checks and monitoring
- ✅ Automatic service discovery between AppHost and ApiService
- ✅ Easy local development without manual Docker commands

### Entity Placeholders

Initial entities are placeholders - full schemas defined in later epics:
- **User**: Basic fields (Id, Email, PasswordHash, CreatedAt) - expanded in Epic 2
- **Session**: Basic fields (Id, UserId, ConnectionId, CreatedAt) - expanded in Epic 2
- **Workflow**: Basic fields (Id, Name, Status, CreatedAt) - expanded in Epic 4

### Dependencies

- **Depends on**: Story 1-1 (Aspire project structure must exist)
- **Enables**: Epic 2 (Auth needs database), Epic 4 (Workflows need state persistence)
- **Uses**: .NET Aspire orchestration for PostgreSQL management

### References

- [.NET Aspire PostgreSQL Integration](https://learn.microsoft.com/en-us/dotnet/aspire/reference/aspire-hosting-postgresql)
- [EF Core PostgreSQL Provider](https://www.npgsql.org/efcore/)
- [EF Core Migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
- [Epic 1 Story 1.2](../../planning-artifacts/epics.md#story-12-configure-postgresql-database-for-local-development)
- [Architecture: Data Architecture](../../planning-artifacts/architecture.md#data-architecture)
- [Story 1.1: Initialize Aspire Template](./1-1-initialize-aspire-template.md)

## Dev Agent Record

### Agent Model Used

Claude 3.5 Sonnet

### Completion Notes List

- Story created from epics.md acceptance criteria
- Docker Compose configuration template included
- EF Core setup steps documented
- Architecture alignment verified

### File List

- /Users/cris/bmadServer/src/bmadServer.AppHost/Program.cs (modify - add PostgreSQL resource)
- /Users/cris/bmadServer/src/bmadServer.ApiService/Data/ApplicationDbContext.cs (create)
- /Users/cris/bmadServer/src/bmadServer.ApiService/Data/Entities/User.cs (create)
- /Users/cris/bmadServer/src/bmadServer.ApiService/Data/Entities/Session.cs (create)
- /Users/cris/bmadServer/src/bmadServer.ApiService/Data/Entities/Workflow.cs (create)
- /Users/cris/bmadServer/src/bmadServer.ApiService/Data/Migrations/ (create)
- /Users/cris/bmadServer/src/bmadServer.ApiService/Program.cs (modify - add DbContext)
