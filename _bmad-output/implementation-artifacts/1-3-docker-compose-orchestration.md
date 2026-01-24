# Story 1.3: Set Up Docker Compose Multi-Container Orchestration

**Status:** cancelled

## Cancellation Reason

This story has been **superseded by .NET Aspire orchestration** implemented in Stories 1.1 and 1.2.

**Why?**
- .NET Aspire (Story 1.1) already provides unified service orchestration via `aspire run`
- PostgreSQL is now orchestrated via Aspire (Story 1.2) with integrated health checks and monitoring
- Docker Compose orchestration is redundant and adds maintenance burden
- The Aspire dashboard provides superior visibility and developer experience compared to manual Docker Compose setup

**Future Consideration:**
- For **production deployments** (beyond MVP), Docker Compose *may* be reconsidered if Aspire cloud deployment patterns prove insufficient
- For **MVP development**, all orchestration needs are met by Aspire

**Status:** ready-for-dev

## Story

As an operator,
I want a Docker Compose configuration that orchestrates the API service and PostgreSQL together,
so that I can run the full stack locally or deploy to a self-hosted server.

## Acceptance Criteria

**Given** I have the `docker-compose.yml` from Story 1.2 (PostgreSQL service)  
**When** I add a `bmadserver` service to the compose file  
**Then** the service definition includes:
  - build: { context: ., dockerfile: bmadServer.ApiService/Dockerfile }
  - ports: ["3000:8080"] (API accessible on localhost:3000)
  - environment variables for database connection
  - depends_on: [postgres] (ensures DB starts first)
  - health check: GET /health every 10s
  - restart policy: unless-stopped

**Given** a Dockerfile exists in `bmadServer.ApiService/`  
**When** I review the Dockerfile  
**Then** it includes:
  - Multi-stage build (SDK stage → runtime stage)
  - FROM mcr.microsoft.com/dotnet/sdk:10 (build stage)
  - FROM mcr.microsoft.com/dotnet/aspnet:10 (runtime stage)
  - COPY built binaries into runtime image
  - EXPOSE 8080
  - ENTRYPOINT for running the API

**Given** the Dockerfile and docker-compose.yml are complete  
**When** I run `docker-compose up --build`  
**Then** both postgres and bmadserver containers start  
**And** postgres reports healthy within 10s  
**And** bmadserver reports healthy within 20s  
**And** the API is accessible on http://localhost:3000/health

**Given** the services are running  
**When** I make a request to `http://localhost:3000/health`  
**Then** I receive a 200 OK response with:
```json
{
  "status": "Healthy",
  "checks": {
    "database": "Connected",
    "dependencies": "Ready"
  }
}
```

**Given** the compose stack is running  
**When** I stop it with `docker-compose down`  
**Then** both containers stop gracefully  
**And** when I run `docker-compose up` again  
**And** the database state persists (volume was not deleted)  
**And** no data loss occurs

**Given** the compose file is complete  
**When** a new developer clones the repo and runs `docker-compose up`  
**Then** they have a fully functional local development environment  
**And** no additional setup steps are required  
**And** the API logs show no errors or warnings

## Tasks / Subtasks

- [ ] **Task 1: Create multi-stage Dockerfile** (AC: #2)
  - [ ] Create bmadServer.ApiService/Dockerfile
  - [ ] Add SDK stage for building (mcr.microsoft.com/dotnet/sdk:10)
  - [ ] Copy project files and restore dependencies
  - [ ] Build and publish to /app/publish
  - [ ] Add runtime stage (mcr.microsoft.com/dotnet/aspnet:10)
  - [ ] Copy published files from build stage
  - [ ] EXPOSE 8080 and set ENTRYPOINT
  - [ ] Test docker build locally

- [ ] **Task 2: Update docker-compose.yml with API service** (AC: #1)
  - [ ] Add bmadserver service to docker-compose.yml
  - [ ] Configure build context and Dockerfile path
  - [ ] Map ports 3000:8080
  - [ ] Add environment variables for database connection
  - [ ] Add depends_on: postgres with condition: service_healthy
  - [ ] Add health check for /health endpoint
  - [ ] Add restart: unless-stopped policy

- [ ] **Task 3: Configure environment variables** (AC: #1, #6)
  - [ ] Define CONNECTION_STRING environment variable
  - [ ] Update Program.cs to read connection from environment
  - [ ] Support both appsettings and environment variable fallback
  - [ ] Add ASPNETCORE_ENVIRONMENT=Development for local
  - [ ] Document all required environment variables

- [ ] **Task 4: Test full stack startup** (AC: #3)
  - [ ] Run docker-compose up --build
  - [ ] Verify postgres container starts and reports healthy
  - [ ] Verify bmadserver container starts and reports healthy
  - [ ] Check logs for any errors or warnings
  - [ ] Verify containers can communicate (API → database)

- [ ] **Task 5: Verify health check endpoint** (AC: #4)
  - [ ] Test http://localhost:3000/health returns 200 OK
  - [ ] Verify response includes database status
  - [ ] Verify JSON response format matches spec
  - [ ] Test health check under various conditions

- [ ] **Task 6: Test persistence and restart** (AC: #5)
  - [ ] Insert test data into database
  - [ ] Run docker-compose down
  - [ ] Run docker-compose up
  - [ ] Verify test data persists
  - [ ] Verify no data loss occurred

- [ ] **Task 7: Document zero-config setup** (AC: #6)
  - [ ] Update README with docker-compose up instructions
  - [ ] Document expected ports and URLs
  - [ ] Add troubleshooting section
  - [ ] Test fresh clone → docker-compose up workflow

## Dev Notes

### Dockerfile Template

```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10 AS build
WORKDIR /src
COPY ["bmadServer.ApiService/bmadServer.ApiService.csproj", "bmadServer.ApiService/"]
COPY ["bmadServer.ServiceDefaults/bmadServer.ServiceDefaults.csproj", "bmadServer.ServiceDefaults/"]
RUN dotnet restore "bmadServer.ApiService/bmadServer.ApiService.csproj"
COPY . .
WORKDIR "/src/bmadServer.ApiService"
RUN dotnet build -c Release -o /app/build
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "bmadServer.ApiService.dll"]
```

### Updated docker-compose.yml

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

  bmadserver:
    build:
      context: .
      dockerfile: bmadServer.ApiService/Dockerfile
    container_name: bmadserver_api
    ports:
      - "3000:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=bmadserver;User Id=bmadserver_dev;Password=dev_password;
    depends_on:
      postgres:
        condition: service_healthy
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 10s
      timeout: 5s
      retries: 3
    restart: unless-stopped

volumes:
  bmadserver_pgdata:
```

### Architecture Alignment

Per architecture.md requirements:
- Hosting: Docker Compose (MVP) → Kubernetes (Phase 2/3) ✅
- Deployment: Self-hosted Linux servers (Ubuntu 22.04 LTS) ✅
- Health Checks: Built-in endpoint + container health checks ✅

### Dependencies

- **Depends on**: Story 1-1 (project structure), Story 1-2 (PostgreSQL configuration)
- **Enables**: Story 1-4 (CI/CD can build Docker images), Story 1-5 (monitoring can scrape)

### References

- [Docker Compose Documentation](https://docs.docker.com/compose/)
- [.NET Docker Images](https://hub.docker.com/_/microsoft-dotnet)
- [Epic 1 Story 1.3](../../planning-artifacts/epics.md#story-13-set-up-docker-compose-multi-container-orchestration)

## Dev Agent Record

### Agent Model Used

Claude 3.5 Sonnet

### Completion Notes List

- Story created with full acceptance criteria from epics.md
- Dockerfile and docker-compose templates included
- Multi-stage build pattern documented

### File List

- /Users/cris/bmadServer/bmadServer.ApiService/Dockerfile (create)
- /Users/cris/bmadServer/docker-compose.yml (modify)
- /Users/cris/bmadServer/README.md (modify)
