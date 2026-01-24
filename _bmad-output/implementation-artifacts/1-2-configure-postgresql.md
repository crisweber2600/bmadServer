# Story 1.2: Configure PostgreSQL Database for Local Development

**Status:** ready-for-dev

## Story

As a developer,
I want to configure PostgreSQL as the primary data store,
so that I can persist workflow state, session data, and audit logs locally.

## Acceptance Criteria

**Given** I have Docker and Docker Compose installed  
**When** I create `docker-compose.yml` in the project root  
**Then** the file defines a `postgres:17` service with:
  - Port 5432 exposed to localhost
  - POSTGRES_DB=bmadserver
  - POSTGRES_USER=bmadserver_dev
  - Named volume for persistence (/var/lib/postgresql/data)
  - Health check: pg_isready  
**And** when I run `docker-compose up -d`, the containers start successfully

**Given** PostgreSQL is running  
**When** I connect from the host using `psql` client  
**Then** I can connect to postgres:5432  
**And** the database `bmadserver` exists  
**And** I can execute `SELECT version();` successfully

**Given** PostgreSQL is confirmed working  
**When** I add Microsoft.EntityFrameworkCore.Npgsql to the API project  
**Then** `dotnet add package Microsoft.EntityFrameworkCore.Npgsql` installs without errors

**Given** EF Core is added  
**When** I create `Data/ApplicationDbContext.cs` with DbContext  
**Then** the file includes:
  - Connection string configuration
  - DbSet<User>, DbSet<Session>, DbSet<Workflow> placeholders
  - OnConfiguring override that reads connection string from appsettings.json  
**And** it references the PostgreSQL provider (NpgsqlConnection)

**Given** the DbContext is configured  
**When** I add the connection string to `appsettings.Development.json`:
```json
"ConnectionStrings": { 
  "DefaultConnection": "Host=localhost;Port=5432;Database=bmadserver;User Id=bmadserver_dev;Password=dev_password;" 
}
```  
**Then** the API can initialize a DbContext without errors

**Given** DbContext is configured  
**When** I run `dotnet ef migrations add InitialCreate`  
**Then** a migration file is generated in `Data/Migrations/`  
**And** the migration includes CREATE TABLE statements for Users, Sessions, Workflows tables  
**And** the migration is version-controlled in git

**Given** a migration exists  
**When** I run `dotnet ef database update`  
**Then** the database schema is created in PostgreSQL  
**And** I can query `SELECT table_name FROM information_schema.tables WHERE table_schema='public';`  
**And** all expected tables exist: users, sessions, workflows, __EFMigrationsHistory

## Tasks / Subtasks

- [ ] **Task 1: Create Docker Compose configuration for PostgreSQL** (AC: #1)
  - [ ] Create docker-compose.yml in project root
  - [ ] Define postgres:17 service with environment variables
  - [ ] Configure port 5432 mapping
  - [ ] Create named volume bmadserver_pgdata
  - [ ] Add health check using pg_isready
  - [ ] Run docker-compose up -d and verify container starts

- [ ] **Task 2: Verify PostgreSQL connectivity** (AC: #2)
  - [ ] Install psql client if not available
  - [ ] Connect to postgres:5432 using psql
  - [ ] Verify bmadserver database exists
  - [ ] Execute SELECT version() to confirm connectivity
  - [ ] Test basic SQL operations (CREATE TABLE, INSERT, SELECT)

- [ ] **Task 3: Add Entity Framework Core with Npgsql** (AC: #3)
  - [ ] Run dotnet add package Microsoft.EntityFrameworkCore.Npgsql
  - [ ] Run dotnet add package Microsoft.EntityFrameworkCore.Design
  - [ ] Run dotnet add package Microsoft.EntityFrameworkCore.Tools
  - [ ] Verify packages appear in .csproj file
  - [ ] Run dotnet restore

- [ ] **Task 4: Create ApplicationDbContext** (AC: #4)
  - [ ] Create Data/ folder in bmadServer.ApiService
  - [ ] Create ApplicationDbContext.cs with DbContext base
  - [ ] Add DbSet<User> Users property (placeholder entity)
  - [ ] Add DbSet<Session> Sessions property (placeholder entity)
  - [ ] Add DbSet<Workflow> Workflows property (placeholder entity)
  - [ ] Create placeholder entity classes (User.cs, Session.cs, Workflow.cs)
  - [ ] Configure OnConfiguring to read connection string

- [ ] **Task 5: Configure connection string** (AC: #5)
  - [ ] Add ConnectionStrings section to appsettings.Development.json
  - [ ] Set DefaultConnection with PostgreSQL connection string
  - [ ] Register DbContext in Program.cs using AddDbContext
  - [ ] Test DbContext initialization at startup
  - [ ] Verify no connection errors in logs

- [ ] **Task 6: Create and run initial migration** (AC: #6-7)
  - [ ] Install dotnet-ef tool if needed: dotnet tool install --global dotnet-ef
  - [ ] Run dotnet ef migrations add InitialCreate
  - [ ] Review generated migration file in Data/Migrations/
  - [ ] Verify CREATE TABLE statements for all entities
  - [ ] Run dotnet ef database update
  - [ ] Verify tables exist in PostgreSQL using psql
  - [ ] Commit migration files to git

## Dev Notes

### Project Structure Notes

Database configuration follows standard EF Core patterns:
- **Data/ApplicationDbContext.cs**: Main DbContext with entity sets
- **Data/Entities/**: Entity classes (User.cs, Session.cs, Workflow.cs)
- **Data/Migrations/**: EF Core migration files (auto-generated)
- **appsettings.Development.json**: Connection string for local development

### Architecture Alignment

Per architecture.md requirements:
- Data Modeling: Hybrid (EF Core 9.0 + PostgreSQL JSONB) ✅
- Validation: EF Core Annotations + FluentValidation 11.9.2 ✅
- Migrations: EF Core Migrations with local testing gate ✅
- Database: PostgreSQL 17.x LTS (incremental VACUUM + GIN indexes) ✅

### Docker Compose Configuration

```yaml
version: '3.8'
services:
  postgres:
    image: postgres:17
    container_name: bmadserver_db
    environment:
      POSTGRES_DB: bmadserver
      POSTGRES_USER: bmadserver_dev
      POSTGRES_PASSWORD: dev_password
    ports:
      - "5432:5432"
    volumes:
      - bmadserver_pgdata:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U bmadserver_dev -d bmadserver"]
      interval: 10s
      timeout: 5s
      retries: 5

volumes:
  bmadserver_pgdata:
```

### Entity Placeholders

Initial entities are placeholders - full schemas defined in later epics:
- **User**: Basic fields (Id, Email, PasswordHash, CreatedAt) - expanded in Epic 2
- **Session**: Basic fields (Id, UserId, ConnectionId, CreatedAt) - expanded in Epic 2
- **Workflow**: Basic fields (Id, Name, Status, CreatedAt) - expanded in Epic 4

### Dependencies

- **Depends on**: Story 1-1 (Aspire project structure must exist)
- **Enables**: Story 1-3 (Docker Compose multi-container), Epic 2 (Auth needs database)

### References

- [EF Core PostgreSQL Provider](https://www.npgsql.org/efcore/)
- [Docker PostgreSQL Image](https://hub.docker.com/_/postgres)
- [Epic 1 Story 1.2](../../planning-artifacts/epics.md#story-12-configure-postgresql-database-for-local-development)
- [Architecture: Data Architecture](../../planning-artifacts/architecture.md#data-architecture)

## Dev Agent Record

### Agent Model Used

Claude 3.5 Sonnet

### Completion Notes List

- Story created from epics.md acceptance criteria
- Docker Compose configuration template included
- EF Core setup steps documented
- Architecture alignment verified

### File List

- /Users/cris/bmadServer/docker-compose.yml (create)
- /Users/cris/bmadServer/bmadServer.ApiService/Data/ApplicationDbContext.cs (create)
- /Users/cris/bmadServer/bmadServer.ApiService/Data/Entities/User.cs (create)
- /Users/cris/bmadServer/bmadServer.ApiService/Data/Entities/Session.cs (create)
- /Users/cris/bmadServer/bmadServer.ApiService/Data/Entities/Workflow.cs (create)
- /Users/cris/bmadServer/bmadServer.ApiService/appsettings.Development.json (modify)
- /Users/cris/bmadServer/bmadServer.ApiService/Program.cs (modify)
