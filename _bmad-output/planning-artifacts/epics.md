---
stepsCompleted: [step-01-validate-prerequisites, step-02-design-epics, step-03-create-stories]
inputDocuments:
  - /Users/cris/bmadServer/_bmad-output/planning-artifacts/prd.md
  - /Users/cris/bmadServer/_bmad-output/planning-artifacts/architecture.md
  - /Users/cris/bmadServer/_bmad-output/planning-artifacts/ux-design-specification.md
---

# bmadServer - Epic Breakdown

## Overview

This document provides the complete epic and story breakdown for bmadServer, decomposing the requirements from the PRD, UX Design, and Architecture requirements into implementable stories.

## Requirements Inventory

### Functional Requirements

**Workflow Orchestration**
- FR1: Users can start any supported BMAD workflow via chat
- FR2: Users can resume a paused workflow at the correct step
- FR3: Users can view current workflow step, status, and next required input
- FR4: Users can safely advance, pause, or exit a workflow
- FR5: The system can route workflow steps to the correct agent

**Collaboration & Flow Preservation**
- FR6: Multiple users can contribute to the same workflow without breaking step order
- FR7: Users can submit inputs that are applied at safe checkpoints
- FR8: Users can see who provided each input and when
- FR9: Users can lock decisions to prevent further changes
- FR10: Users can request a decision review before locking
- FR11: The system can buffer conflicting inputs and require human arbitration

**Personas & Communication**
- FR12: Users can interact using business language and receive translated outputs
- FR13: Users can interact using technical language and receive technical details
- FR14: The system can adapt responses to a selected persona profile
- FR15: Users can switch persona mode within a session

**Session & State Management**
- FR16: Users can return to a session and retain full context
- FR17: The system can recover a workflow after a disconnect or restart
- FR18: Users can view the history of workflow interactions
- FR19: Users can export workflow artifacts and outputs
- FR20: The system can restore previous workflow checkpoints

**Agent Collaboration**
- FR21: Agents can request information from other agents with shared context
- FR22: Agents can contribute structured outputs to a shared workflow state
- FR23: The system can display agent handoffs and attribution
- FR24: The system can pause for human approval when agent confidence is low

**Parity & Compatibility**
- FR25: The system can execute all BMAD workflows supported by the current BMAD version
- FR26: The system can produce outputs compatible with existing BMAD artifacts
- FR27: The system can maintain workflow menus and step sequencing parity
- FR28: Users can run workflows without CLI access
- FR29: The system can surface parity gaps or unsupported workflows

**Admin & Ops**
- FR30: Admins can view system health and active sessions
- FR31: Admins can manage access and permissions for users
- FR32: Admins can configure providers and model routing rules
- FR33: Admins can audit workflow activity and decision history
- FR34: Admins can configure self-hosted deployment settings

**Integrations**
- FR35: The system can send workflow events via webhooks
- FR36: The system can integrate with external tools for notifications

### Non-Functional Requirements

**Performance**
- NFR1: Chat UI acknowledges inputs within 2 seconds
- NFR2: Agent response streaming starts within 5 seconds for typical prompts
- NFR3: Standard workflow step responses complete within 30 seconds

**Reliability**
- NFR4: 99.5% uptime for dogfood deployments
- NFR5: Fewer than 5% workflow failures excluding provider outages
- NFR6: Session recovery after reconnect within 60 seconds

**Security**
- NFR7: TLS for all traffic in transit
- NFR8: Encryption at rest for stored sessions and artifacts
- NFR9: Audit logs retained for 90 days (configurable)

**Scalability**
- NFR10: Support 25 concurrent users and 10 concurrent workflows in MVP
- NFR11: Graceful degradation beyond limits via queueing or throttling

**Integration**
- NFR12: Webhooks deliver at-least-once with retries for 24 hours
- NFR13: Event stream ordering is guaranteed per workflow

**Usability**
- NFR14: Time to first successful workflow under 10 minutes
- NFR15: Resume after interruption in under 2 minutes

### Additional Requirements

**From Architecture Document:**

**Starter Template & Project Setup:**
- Use .NET Aspire Starter App via Aspire CLI (`aspire new aspire-starter --name bmadServer`)
- Framework: .NET 10 with ASP.NET Core and Aspire orchestration
- Real-time: SignalR WebSocket (NuGet: Microsoft.AspNetCore.SignalR)
- State: PostgreSQL with Event Log (+ JSONB concurrency control)
- Agents: In-process (MVP), Queue-ready interface

**Data Architecture:**
- Data Modeling: Hybrid (EF Core 9.0 + PostgreSQL JSONB)
- Validation: EF Core Annotations + FluentValidation 11.9.2
- Migrations: EF Core Migrations with local testing gate
- Caching: In-Process IMemoryCache (Redis-ready interface)
- Database: PostgreSQL 17.x LTS (incremental VACUUM + GIN indexes)
- JSONB Concurrency Control: `_version`, `_lastModifiedBy`, `_lastModifiedAt` fields
- JSON Serializer: System.Text.Json

**Authentication & Security:**
- Authentication: Hybrid (Local DB MVP + OpenID Connect Ready Phase 2)
- Authorization: Hybrid RBAC + Claims-Based (Admin, Participant, Viewer roles)
- Encryption (Transit): HTTPS + TLS 1.3+
- API Security: Security Headers + Per-User Rate Limiting
- Access Token: JWT, 15-minute expiry
- Refresh Token: HttpOnly Cookie, 7-day expiry
- Idle Timeout: 30 minutes of inactivity forces re-login

**API & Communication:**
- REST Design: Hybrid REST + RPC (resource + action endpoints)
- Error Handling: ProblemDetails RFC 7807
- Documentation: OpenAPI 3.1 + Swagger UI
- WebSocket Errors: Explicit Error Messages
- API Versioning: URL Path /api/v1/

**Infrastructure & Deployment:**
- Hosting: Docker Compose (MVP) → Kubernetes (Phase 2/3)
- Deployment: Self-hosted Linux servers (Ubuntu 22.04 LTS)
- CI/CD: GitHub Actions + Docker Build + Push
- Monitoring: Prometheus 2.45+ + Grafana 10+
- Health Checks: Built-in endpoint + Kubernetes liveness/readiness

**From UX Design Document:**

**Design System:**
- Ant Design React component library
- Clean Sidebar Layout design direction
- Inter typeface as primary font
- 8px base unit spacing system

**Responsive Design:**
- Mobile-first decision approval interfaces
- Cross-device continuity (laptop to mobile)
- Touch-friendly buttons and swipe gestures
- Progressive web app capabilities

**Accessibility:**
- WCAG AA standards (4.5:1 contrast ratio minimum)
- Clear keyboard navigation with visible focus states
- Screen reader support with semantic HTML and ARIA labels
- Reduced motion preferences respected

**User Experience:**
- Progressive elaboration from business-level to technical specifications
- Invisible multi-agent orchestration
- Context-aware guidance based on BMAD phase
- Decision crystallization with attribution and traceability
- Error recovery flows for conversation stalls and off-track inputs

### FR Coverage Map

| Epic | FR Coverage | NFR Coverage |
|------|-------------|--------------|
| Epic 1: Aspire Foundation & Project Setup | FR25-FR29 (parity setup) | NFR4, NFR7, NFR10, NFR11 |
| Epic 2: User Authentication & Session Management | FR16, FR17 | NFR6, NFR15 |
| Epic 3: Real-Time Chat Interface | FR1, FR3, FR12-FR15 | NFR1, NFR14 |
| Epic 4: Workflow Orchestration Engine | FR1-FR5, FR25-FR27 | NFR2, NFR3 |
| Epic 5: Multi-Agent Collaboration | FR5, FR21-FR24 | NFR2, NFR3 |
| Epic 6: Decision Management & Locking | FR9, FR10, FR22, FR23 | NFR5 |
| Epic 7: Collaboration & Multi-User Support | FR6-FR8, FR11 | NFR10 |
| Epic 8: Persona Translation & Language Adaptation | FR12-FR15 | NFR1 |
| Epic 9: Data Persistence & State Management | FR16-FR20 | NFR4, NFR8, NFR9 |
| Epic 10: Error Handling & Recovery | FR17, FR24 | NFR5, NFR6 |
| Epic 11: Security & Access Control | FR31, FR33 | NFR7, NFR9, NFR11 |
| Epic 12: Admin Dashboard & Operations | FR30-FR34 | NFR4 |
| Epic 13: Integrations & Webhooks | FR35, FR36 | NFR12, NFR13 |

## Epic List

The requirements have been organized into **13 epics** (Epic 14 merged into Epic 1), each representing a major capability area ready for story breakdown:

1. **Epic 1: Aspire Foundation & Project Setup** - Bootstrap bmadServer from .NET Aspire Starter template, configure PostgreSQL, Docker Compose, CI/CD pipeline, monitoring stack (Prometheus + Grafana), and ensure BMAD workflow parity
2. **Epic 2: User Authentication & Session Management** - User registration, login, token management (JWT + refresh tokens), session persistence across devices, idle timeout
3. **Epic 3: Real-Time Chat Interface** - WebSocket communication via SignalR, chat message rendering, conversational UI with Ant Design, real-time user feedback
4. **Epic 4: Workflow Orchestration Engine** - BMAD workflow execution, step tracking, agent routing, workflow resumption, pause/exit capabilities
5. **Epic 5: Multi-Agent Collaboration** - Agent-to-agent messaging, seamless handoffs, shared workflow context, agent attribution
6. **Epic 6: Decision Management & Locking** - Decision capture, approval workflows, version control, lock/unlock mechanisms, conflict detection
7. **Epic 7: Collaboration & Multi-User Support** - Concurrent workflow participation, safe checkpoints, conflict resolution, multi-user input buffering
8. **Epic 8: Persona Translation & Language Adaptation** - Business/technical language switching, context-aware responses based on user role, adaptive UI visibility
9. **Epic 9: Data Persistence & State Management** - PostgreSQL setup, JSONB state storage with concurrency control, event logging, EF Core migrations
10. **Epic 10: Error Handling & Recovery** - Session recovery after disconnects, conflict resolution, graceful degradation, conversation stall recovery
11. **Epic 11: Security & Access Control** - Rate limiting (per-user), RBAC (Admin/Participant/Viewer), audit logging, encryption (TLS transit)
12. **Epic 12: Admin Dashboard & Operations** - System health monitoring, user management, provider configuration, workflow activity audit
13. **Epic 13: Integrations & Webhooks** - Webhook event emission, external tool notifications, event stream reliability

---

## Ready for Story Breakdown

All 36 Functional Requirements and 15 Non-Functional Requirements have been extracted, mapped, and organized into 13 **user-value-focused epics** (Epic 14 merged into Epic 1 as requested). The project is now proceeding to **Step 3: Create Stories** where each epic will be decomposed into detailed user stories with acceptance criteria.

**✅ Approved Structure:** 13 Epics → Foundation + Core Features + Operations

---

## EPIC STORIES

### Epic 1: Aspire Foundation & Project Setup

**Epic Goal:** Bootstrap bmadServer infrastructure from .NET Aspire Starter template, configure PostgreSQL, Docker Compose multi-container orchestration, CI/CD pipeline, and monitoring stack. Enable all downstream epics.

**Requirements Covered:** FR25-FR29 (BMAD workflow parity foundation), NFR4 (uptime foundation), NFR7 (TLS foundation), NFR10-NFR11 (scalability foundation)

**Duration:** 1.5-2 weeks | **Stories:** 6 | **Total Points:** 32

---

#### Story 1.1: Initialize bmadServer from .NET Aspire Starter Template

**Story ID:** E1-S1  
**Points:** 3  

As a developer, I want to bootstrap bmadServer from the .NET Aspire Starter template so that I have a cloud-native project structure with service orchestration built-in.

**Acceptance Criteria:**

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

---

#### Story 1.2: Configure PostgreSQL Database for Local Development

**Story ID:** E1-S2  
**Points:** 5  

As a developer, I want to configure PostgreSQL as the primary data store so that I can persist workflow state, session data, and audit logs locally.

**Acceptance Criteria:**

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

---

#### Story 1.3: Set Up Docker Compose Multi-Container Orchestration

**Story ID:** E1-S3  
**Points:** 5  

As an operator, I want a Docker Compose configuration that orchestrates the API service and PostgreSQL together so that I can run the full stack locally or deploy to a self-hosted server.

**Acceptance Criteria:**

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

---

#### Story 1.4: Configure GitHub Actions CI/CD Pipeline

**Story ID:** E1-S4  
**Points:** 8  

As a developer, I want automated CI/CD so that every commit triggers build, test, and deployment checks.

**Acceptance Criteria:**

**Given** I have a GitHub repository  
**When** I create `.github/workflows/ci.yml`  
**Then** the workflow file is valid YAML and defines:
  - Trigger: on: [push, pull_request] to all branches
  - Jobs: build, test, (deploy on main only)

**Given** the workflow file exists  
**When** I review the build job  
**Then** it includes:
  - Checkout code: uses: actions/checkout@v4
  - Setup .NET: uses: actions/setup-dotnet@v4 with dotnet-version: 10.0
  - Restore dependencies: `dotnet restore`
  - Build project: `dotnet build --configuration Release`  
**And** the job succeeds or fails with clear error messages

**Given** the build job completes  
**When** I review the test job  
**Then** it includes:
  - Run unit tests: `dotnet test --configuration Release --logger trx`
  - Report test results: upload .trx files as artifacts
  - Fail job if any tests fail
  - Job runs only if build succeeds (depends_on: build)

**Given** the pipeline is configured  
**When** I push a commit to a branch  
**Then** GitHub Actions automatically:
  - Checks out the code
  - Builds the solution (passes/fails)
  - Runs all unit tests (passes/fails)
  - Reports results in the PR

**Given** a PR is created  
**When** I review the PR checks section  
**Then** I see:
  - build job status (passed/failed)
  - test job status (passed/failed)  
**And** merge is blocked if any checks fail

**Given** a commit is merged to main  
**When** the workflow completes  
**Then** the build succeeds  
**And** the tests pass  
**And** (optional) Docker image is built and tagged with commit SHA  
**And** (optional) image is pushed to container registry

**Given** the CI/CD is operational  
**When** I review `.github/workflows/` directory  
**Then** I find documentation comments explaining:
  - When each job runs
  - What each step does
  - How to modify triggers or add new jobs

**Given** a developer makes a breaking change  
**When** they push to a branch  
**Then** the CI/CD catches the error and reports it in the PR  
**And** they can fix and re-push without manual intervention

---

#### Story 1.5: Set Up Prometheus and Grafana Monitoring Stack

**Story ID:** E1-S5  
**Points:** 8  

As an operator, I want to monitor system health, API metrics, and database performance so that I can detect issues and debug problems.

**Acceptance Criteria:**

**Given** I have the `docker-compose.yml` from Story 1.3  
**When** I add `prometheus:2.45` and `grafana:10` services  
**Then** the prometheus service includes:
  - Exposed on port 9090
  - Config file: `prometheus.yml`
  - Health check: `/-/healthy`  
**And** the grafana service includes:
  - Exposed on port 3001
  - Admin credentials: admin/admin (local only)
  - Health check: `/api/health`
  - Auto-provision datasources from `/etc/grafana/provisioning`

**Given** prometheus service is running  
**When** I create `prometheus.yml` config  
**Then** it includes:
  - global: { scrape_interval: 15s }
  - scrape_configs targeting http://bmadserver:8080/metrics
  - Job name: bmadserver_api

**Given** the API service is configured to export metrics  
**When** I add `dotnet add package Prometheus.Client`  
**Then** the package installs in `bmadServer.ApiService` without errors

**Given** Prometheus.Client is installed  
**When** I add metrics initialization to `Program.cs`  
**Then** the code includes:
  - app.UseMetricServer() to expose /metrics endpoint
  - Metrics for: HTTP requests, response times, errors
  - Custom metrics for: active workflows, agents, decisions

**Given** the API exports metrics  
**When** I run `docker-compose up`  
**Then** Prometheus scrapes metrics from http://bmadserver:8080/metrics  
**And** Grafana starts and connects to Prometheus datasource  
**And** I can access Grafana at http://localhost:3001

**Given** Grafana is running  
**When** I log in with admin/admin  
**Then** I see Prometheus datasource is configured  
**And** default dashboards appear (if pre-provisioned)

**Given** Grafana is configured  
**When** I create a basic dashboard  
**Then** it includes:
  - Graph: "HTTP Request Rate" (requests/sec over time)
  - Graph: "Response Time (p50, p95, p99)"
  - Graph: "Error Rate" (5xx errors)
  - Graph: "Active Connections"
  - Gauge: "Database Connection Pool Usage"

**Given** the monitoring stack is complete  
**When** I trigger an API request: GET /health  
**Then** I can see the metric appear in Prometheus  
**And** the metric renders in Grafana within 15 seconds  
**And** the request count increments in the dashboard

**Given** monitoring is set up  
**When** I run `docker-compose ps`  
**Then** I see:
  - bmadserver (healthy)
  - postgres (healthy)
  - prometheus (healthy)
  - grafana (healthy)

---

#### Story 1.6: Document Project Setup and Deployment Instructions

**Story ID:** E1-S6  
**Points:** 3  

As a new team member, I want clear setup and deployment documentation so that I can get a working development environment.

**Acceptance Criteria:**

**Given** the project is complete through Story 1.5  
**When** I create `SETUP.md` in the project root  
**Then** it includes:
```
## Prerequisites
- .NET 10 SDK
- Docker and Docker Compose v2+
- Git

## Quick Start (Local Development)
1. Clone the repo
2. cd bmadServer
3. docker-compose up
4. Open http://localhost:3000/health (API)
5. Open http://localhost:3001 (Grafana)

## Project Structure
- bmadServer.AppHost/ - Service orchestration (Aspire)
- bmadServer.ApiService/ - REST API + SignalR hub
- bmadServer.ServiceDefaults/ - Shared patterns
- docker-compose.yml - Local stack
- .github/workflows/ - CI/CD

## Development Workflow
- Edit code in bmadServer.ApiService/
- dotnet build to compile
- docker-compose up to run locally
- Changes trigger Aspire hot-reload

## Deployment (Self-Hosted)
1. Push to main branch
2. CI/CD builds and tests
3. Docker image created: bmadserver:latest
4. Deploy to server: docker pull bmadserver && docker-compose up -d
5. Verify: curl https://your-server/health

## Monitoring
- Prometheus: http://localhost:9090
- Grafana: http://localhost:3001 (admin/admin)
- API health: http://localhost:3000/health

## Troubleshooting
- Port conflicts: Edit docker-compose.yml ports
- Database connection: Check environment variables in compose file
- Metrics not appearing: Verify Prometheus scrape targets
```

**And** when a team member follows the instructions  
**Then** they get a running local environment in < 10 minutes

**Given** SETUP.md is written  
**When** I create `ARCHITECTURE.md` overview  
**Then** it includes:
  - Component diagram (ASCII or link)
  - Data flow (requests → API → DB)
  - Deployment architecture (Docker Compose → self-hosted server)
  - Technology choices (why .NET, Aspire, PostgreSQL, etc.)

**Given** documentation exists  
**When** I review the `README.md`  
**Then** it includes:
  - Project description
  - Quick start link (→ SETUP.md)
  - Architecture link (→ ARCHITECTURE.md)
  - Contributing guidelines
  - Support/issue tracking links

**Given** documentation is complete  
**When** a new developer follows SETUP.md  
**Then** they successfully:
  - Get a running development environment
  - Understand the project structure
  - Know how to deploy
  - Can debug using provided tools

---

**Epic 1 Summary:**
- ✅ 6 stories, 32 points
- ✅ 1.5-2 week timeline
- ✅ Foundation ready for Epic 2-13
- ✅ Expert panel approved (Winston: Architecture ✅ | Mary: Business ✅ | Amelia: Feasibility ✅ | Murat: Testability ✅)

---

### Epic 2: User Authentication & Session Management

**Epic Goal:** Enable secure user registration, authentication, and session management so that users can access bmadServer with proper authorization and maintain their workflow context across sessions and disconnects.

**Requirements Covered:** FR16-17 (session management, recovery), NFR6 (60s recovery), NFR15 (2 min resume), Security requirements (TLS, JWT, RBAC)

**Duration:** 2-3 weeks | **Stories:** 6 | **Total Points:** 34

---

#### Story 2.1: User Registration & Local Database Authentication

**Story ID:** E2-S1  
**Points:** 5  

As a new user (Sarah, non-technical co-founder),
I want to create an account with email and password,
so that I can securely access bmadServer and start using BMAD workflows.

**Acceptance Criteria:**

**Given** the bmadServer API is running  
**When** I send a POST request to `/api/v1/auth/register` with valid registration data:
```json
{
  "email": "sarah@example.com",
  "password": "SecurePass123!",
  "displayName": "Sarah Johnson"
}
```
**Then** the system creates a new user record in the PostgreSQL Users table  
**And** the password is hashed using bcrypt (cost factor 12)  
**And** the response returns 201 Created with user details (excluding password hash):
```json
{
  "id": "uuid",
  "email": "sarah@example.com",
  "displayName": "Sarah Johnson",
  "createdAt": "2026-01-23T10:00:00Z"
}
```

**Given** I attempt to register with an email that already exists  
**When** I send POST `/api/v1/auth/register` with duplicate email  
**Then** the system returns 409 Conflict with ProblemDetails:
```json
{
  "type": "https://bmadserver.dev/errors/user-exists",
  "title": "User Already Exists",
  "status": 409,
  "detail": "A user with this email already exists"
}
```

**Given** I attempt to register with invalid data  
**When** I send POST `/api/v1/auth/register` with:
  - Invalid email format (missing @, invalid domain)
  - Weak password (< 8 characters, no special characters)
  - Missing required fields
**Then** the system returns 400 Bad Request with validation errors using ProblemDetails  
**And** the response includes specific field-level error messages

**Given** the Users table does not exist  
**When** I run `dotnet ef migrations add AddUsersTable`  
**Then** an EF Core migration is generated with:
  - Users table (Id, Email, PasswordHash, DisplayName, CreatedAt, UpdatedAt)
  - Unique index on Email column
  - Check constraint on Email format (basic validation)

**Given** I run `dotnet ef database update`  
**When** the migration executes  
**Then** the Users table is created in PostgreSQL  
**And** I can query `SELECT * FROM users` successfully  
**And** the Email column has a unique constraint enforced

**Given** registration endpoint is exposed  
**When** I check OpenAPI documentation at `/swagger`  
**Then** I see POST `/api/v1/auth/register` endpoint documented  
**And** request/response schemas are clearly defined  
**And** validation rules are documented (password requirements, email format)

---

#### Story 2.2: JWT Token Generation & Validation

**Story ID:** E2-S2  
**Points:** 5  

As a registered user (Sarah),
I want to login with my credentials and receive a secure JWT access token,
so that I can make authenticated API requests to bmadServer.

**Acceptance Criteria:**

**Given** I am a registered user with valid credentials  
**When** I send POST `/api/v1/auth/login` with:
```json
{
  "email": "sarah@example.com",
  "password": "SecurePass123!"
}
```
**Then** the system validates my password against the bcrypt hash  
**And** generates a JWT access token with 15-minute expiry  
**And** the response returns 200 OK with:
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "tokenType": "Bearer",
  "expiresIn": 900,
  "user": {
    "id": "uuid",
    "email": "sarah@example.com",
    "displayName": "Sarah Johnson"
  }
}
```

**Given** I attempt to login with incorrect password  
**When** I send POST `/api/v1/auth/login` with wrong password  
**Then** the system returns 401 Unauthorized with ProblemDetails  
**And** the error message does not reveal whether email exists (prevent enumeration)  
**And** the response is: "Invalid email or password"

**Given** I attempt to login with non-existent email  
**When** I send POST `/api/v1/auth/login` with unregistered email  
**Then** the system returns 401 Unauthorized with same generic message  
**And** timing is consistent with failed password check (prevent timing attacks)

**Given** I have a valid JWT access token  
**When** I send GET `/api/v1/users/me` with `Authorization: Bearer {token}` header  
**Then** the JWT middleware validates the token signature  
**And** extracts user claims (userId, email)  
**And** the endpoint returns 200 OK with my user profile

**Given** I send a request with an expired JWT token  
**When** I call any protected endpoint with expired token  
**Then** the system returns 401 Unauthorized with:
```json
{
  "type": "https://bmadserver.dev/errors/token-expired",
  "title": "Token Expired",
  "status": 401,
  "detail": "Access token has expired. Please refresh your token."
}
```

**Given** I send a request with a malformed or tampered JWT token  
**When** I call any protected endpoint with invalid token  
**Then** the system returns 401 Unauthorized  
**And** the request is rejected before reaching endpoint logic  
**And** the error indicates "Invalid token signature"

**Given** the JWT configuration exists in appsettings.json  
**When** I review the configuration  
**Then** I see:
```json
{
  "Jwt": {
    "SecretKey": "{generated-secure-key}",
    "Issuer": "bmadServer",
    "Audience": "bmadServer-api",
    "AccessTokenExpirationMinutes": 15
  }
}
```
**And** SecretKey is at least 256 bits (32 characters)  
**And** SecretKey is stored securely (environment variable in production)

**Given** JWT authentication is configured in Program.cs  
**When** I review the middleware pipeline  
**Then** I see:
  - `builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)`
  - `app.UseAuthentication()` before `app.UseAuthorization()`
  - JWT validation parameters configured (ValidateIssuer, ValidateAudience, ValidateLifetime)

---

#### Story 2.3: Refresh Token Flow with HttpOnly Cookies

**Story ID:** E2-S3  
**Points:** 8  

As a logged-in user (Sarah),
I want my session to be automatically extended without re-entering credentials,
so that I can work continuously without interruptions while maintaining security.

**Acceptance Criteria:**

**Given** I successfully login  
**When** I receive the login response  
**Then** the system generates a refresh token (UUID v4)  
**And** stores it in the RefreshTokens table with:
  - Token (hashed with SHA256)
  - UserId (foreign key to Users)
  - ExpiresAt (7 days from creation)
  - CreatedAt, RevokedAt (nullable)
**And** sets an HttpOnly cookie in the response:
```
Set-Cookie: refreshToken={token}; HttpOnly; Secure; SameSite=Strict; Path=/api/v1/auth/refresh; Max-Age=604800
```

**Given** my access token is about to expire (< 2 minutes remaining)  
**When** my client sends POST `/api/v1/auth/refresh` with the refresh token cookie  
**Then** the system validates the refresh token:
  - Token exists in database and is not revoked
  - Token has not expired (< 7 days old)
  - Token hash matches stored hash
**And** generates a new access token (15-minute expiry)  
**And** rotates the refresh token (invalidates old, creates new)  
**And** returns 200 OK with new access token and refresh cookie

**Given** I send a refresh request with an expired refresh token  
**When** I call POST `/api/v1/auth/refresh` with expired token  
**Then** the system returns 401 Unauthorized  
**And** the error message indicates "Refresh token expired. Please login again."  
**And** the old refresh token is marked as revoked in the database

**Given** I send a refresh request with a revoked refresh token  
**When** I call POST `/api/v1/auth/refresh` with previously used token  
**Then** the system returns 401 Unauthorized  
**And** the error indicates "Invalid refresh token"  
**And** all refresh tokens for this user are revoked (security breach detection)

**Given** I send concurrent refresh requests (race condition test)  
**When** two requests arrive within 100ms using the same refresh token  
**Then** only one request succeeds with new tokens  
**And** the second request fails with 401 Unauthorized  
**And** token rotation happens atomically (database transaction)

**Given** I logout from the application  
**When** I send POST `/api/v1/auth/logout` with my refresh token cookie  
**Then** the system revokes my refresh token in the database  
**And** clears the refresh token cookie:
```
Set-Cookie: refreshToken=; HttpOnly; Secure; SameSite=Strict; Path=/api/v1/auth/refresh; Max-Age=0
```
**And** returns 204 No Content

**Given** the RefreshTokens table migration is created  
**When** I run `dotnet ef migrations add AddRefreshTokensTable`  
**Then** the migration includes:
  - RefreshTokens table (Id, TokenHash, UserId, ExpiresAt, CreatedAt, RevokedAt)
  - Foreign key constraint to Users table
  - Index on TokenHash for fast lookups
  - Index on UserId for user-specific queries

**Given** security configuration is reviewed  
**When** I check the cookie settings in production  
**Then** I verify:
  - HttpOnly flag prevents JavaScript access
  - Secure flag enforces HTTPS-only transmission
  - SameSite=Strict prevents CSRF attacks
  - Path=/api/v1/auth/refresh limits cookie scope

---

#### Story 2.4: Session Persistence & Recovery

**Story ID:** E2-S4  
**Points:** 8  

As a user (Marcus) working on a BMAD workflow,
I want my workflow state to persist across disconnects and server restarts,
so that I can resume my work within 60 seconds without losing progress.

**Acceptance Criteria:**

**Given** I am authenticated and working on a workflow  
**When** my SignalR connection establishes  
**Then** the system creates a Session record in the Sessions table:
```
- Id (UUID)
- UserId (foreign key to Users)
- ConnectionId (SignalR connection ID)
- WorkflowState (JSONB column storing workflow context)
- LastActivityAt (timestamp)
- CreatedAt, ExpiresAt (30 min idle timeout)
```
**And** the session state includes:
  - Current workflow name and step number
  - Agent context and conversation history (last 10 messages)
  - Decision lock states
  - Pending user inputs

**Given** I am actively working in a session  
**When** I perform any action (send message, make decision, advance workflow)  
**Then** the system updates the session WorkflowState JSONB field  
**And** updates LastActivityAt timestamp  
**And** increments the session `_version` field (optimistic concurrency)  
**And** sets `_lastModifiedBy` to my userId  
**And** all updates happen in a database transaction

**Given** my network connection drops unexpectedly  
**When** my client reconnects within 60 seconds (NFR6 requirement)  
**Then** the system:
  - Matches my userId to existing Session record
  - Validates the session has not expired (LastActivityAt < 30 min ago)
  - Associates new ConnectionId with existing Session
**And** sends a SignalR message with restored workflow state:
```json
{
  "type": "SESSION_RESTORED",
  "session": {
    "id": "uuid",
    "workflowName": "create-prd",
    "currentStep": 3,
    "conversationHistory": [...],
    "pendingInput": "Waiting for user to confirm feature list"
  }
}
```
**And** the client UI restores to the exact workflow state before disconnect

**Given** I disconnect and reconnect after 61 seconds  
**When** my client attempts to reconnect  
**Then** the system creates a NEW session (old one expired per 60s window)  
**And** the workflow state is recovered from the Sessions table (if not expired)  
**And** I see a notification: "Session recovered from last checkpoint"  
**And** I can continue from the last saved workflow step

**Given** the server restarts while I have an active session  
**When** the server comes back online  
**Then** my session state persists in PostgreSQL (not lost)  
**And** when I reconnect, the session recovery flow triggers  
**And** I resume from the last persisted workflow state

**Given** I have multiple active sessions (laptop + mobile)  
**When** I open bmadServer on a different device  
**Then** the system creates a separate Session record per device  
**And** each session tracks its own ConnectionId  
**And** workflow state synchronizes across sessions via PostgreSQL  
**And** last-write-wins concurrency control applies (based on `_version` field)

**Given** two concurrent session updates occur (race condition)  
**When** two devices update workflow state simultaneously  
**Then** the system detects version mismatch (optimistic concurrency violation)  
**And** the second update fails with 409 Conflict  
**And** the client receives conflict notification and can retry/merge changes

**Given** a session has been idle for 30 minutes  
**When** the system runs session cleanup (background job)  
**Then** the session ExpiresAt timestamp is checked  
**And** expired sessions are marked as inactive (not deleted - audit trail)  
**And** ConnectionId is cleared to prevent reconnection

**Given** the Sessions table migration is created  
**When** I run `dotnet ef migrations add AddSessionsTable`  
**Then** the migration includes:
  - Sessions table with JSONB WorkflowState column
  - GIN index on WorkflowState for fast JSONB queries
  - Indexes on UserId and ConnectionId
  - Foreign key constraint to Users table
  - Check constraint on ExpiresAt > CreatedAt

---

#### Story 2.5: RBAC (Role-Based Access Control) Implementation

**Story ID:** E2-S5  
**Points:** 5  

As an administrator (Cris),
I want to assign roles to users (Admin, Participant, Viewer),
so that I can control who can perform specific actions in bmadServer workflows.

**Acceptance Criteria:**

**Given** the system defines three roles  
**When** I review the Role enum in the codebase  
**Then** I see:
```csharp
public enum Role
{
    Admin,      // Full system access, user management, workflow control
    Participant, // Can create/edit workflows, make decisions
    Viewer      // Read-only access, can view workflows but not modify
}
```

**Given** the UserRoles table exists  
**When** I run `dotnet ef migrations add AddUserRolesTable`  
**Then** the migration creates:
  - UserRoles table (UserId, Role, AssignedAt, AssignedBy)
  - Composite primary key on (UserId, Role) - users can have multiple roles
  - Foreign key to Users table
  - Index on UserId for fast role lookups

**Given** I register a new user  
**When** the registration completes  
**Then** the user is automatically assigned the "Participant" role by default  
**And** the UserRoles table has a record for this user

**Given** I am an Admin user  
**When** I send POST `/api/v1/users/{userId}/roles` with:
```json
{
  "role": "Admin"
}
```
**Then** the system validates I have Admin role  
**And** adds the specified role to the target user's UserRoles  
**And** returns 200 OK with updated user roles list

**Given** I am a Participant or Viewer  
**When** I attempt to assign roles via POST `/api/v1/users/{userId}/roles`  
**Then** the system returns 403 Forbidden  
**And** the error indicates "Admin role required for this operation"

**Given** I am authenticated  
**When** I send GET `/api/v1/users/me`  
**Then** the response includes my roles:
```json
{
  "id": "uuid",
  "email": "cris@example.com",
  "displayName": "Cris",
  "roles": ["Admin", "Participant"]
}
```

**Given** an endpoint requires Admin role  
**When** I add `[Authorize(Roles = "Admin")]` attribute to the endpoint  
**Then** the authorization middleware checks the JWT claims for "role" claim  
**And** allows access only if the user has Admin role  
**And** returns 403 Forbidden otherwise

**Given** an endpoint requires any of multiple roles  
**When** I add `[Authorize(Roles = "Admin,Participant")]` attribute  
**Then** users with either Admin OR Participant role can access  
**And** Viewer-only users are denied with 403 Forbidden

**Given** JWT tokens include role claims  
**When** a user logs in  
**Then** the generated JWT includes claims:
```json
{
  "sub": "user-uuid",
  "email": "cris@example.com",
  "role": ["Admin", "Participant"],
  "iat": 1234567890,
  "exp": 1234568790
}
```
**And** the JWT middleware automatically populates `User.IsInRole("Admin")`

**Given** I review protected endpoints  
**When** I check the OpenAPI/Swagger documentation  
**Then** endpoints show required roles in the security section  
**And** I can test role-based access directly from Swagger UI

---

#### Story 2.6: Idle Timeout & Security

**Story ID:** E2-S6  
**Points:** 3  

As a security-conscious user (Marcus),
I want the system to automatically log me out after 30 minutes of inactivity,
so that my account remains secure if I leave my workstation unattended.

**Acceptance Criteria:**

**Given** I am logged in and active in the application  
**When** I perform any action (type message, click button, scroll)  
**Then** the client-side idle timer resets  
**And** the LastActivityAt timestamp updates in my Session record  
**And** the 30-minute timeout countdown restarts

**Given** I have been idle for 28 minutes  
**When** the client-side timer reaches 28 minutes  
**Then** a warning modal appears:
```
"You've been inactive for 28 minutes.
Your session will expire in 2 minutes.

[Extend Session]  [Logout Now]"
```
**And** the modal is prominently displayed (centered, overlay background)  
**And** keyboard focus moves to the "Extend Session" button

**Given** the warning modal is displayed  
**When** I click "Extend Session"  
**Then** the client sends POST `/api/v1/auth/extend-session`  
**And** the server updates my Session.LastActivityAt to current timestamp  
**And** the modal closes  
**And** the idle timer resets to 0 minutes  
**And** I continue working without interruption

**Given** the warning modal is displayed  
**When** I click "Logout Now"  
**Then** the system immediately logs me out  
**And** calls POST `/api/v1/auth/logout` to revoke refresh token  
**And** redirects me to the login page  
**And** displays message: "You have been logged out"

**Given** I ignore the warning modal and reach 30 minutes idle  
**When** the idle timer reaches 30 minutes  
**Then** the system automatically logs me out  
**And** revokes my refresh token  
**And** clears all session cookies  
**And** redirects to login page with message: "Your session expired due to inactivity"

**Given** I am logged out due to idle timeout  
**When** I return and login again  
**Then** the system attempts to recover my last session state (if < 2 minutes elapsed per NFR15)  
**And** I see a notification: "Welcome back! Your previous session has been restored."  
**And** I resume from where I left off

**Given** the idle timeout endpoint exists  
**When** I send POST `/api/v1/auth/extend-session` with valid access token  
**Then** the system validates the token is not expired  
**And** updates the Session.LastActivityAt timestamp  
**And** returns 204 No Content

**Given** I send extend-session request with expired access token  
**When** the request is processed  
**Then** the system returns 401 Unauthorized  
**And** the client triggers automatic token refresh flow  
**And** retries the extend-session request with new token

**Given** the idle timeout configuration exists  
**When** I review appsettings.json  
**Then** I see:
```json
{
  "Session": {
    "IdleTimeoutMinutes": 30,
    "WarningTimeoutMinutes": 28
  }
}
```
**And** these values are configurable per environment

---

**Epic 2 Summary:**
- ✅ 6 stories, 34 points
- ✅ 2-3 week timeline
- ✅ Security-critical epic (JWT, sessions, RBAC)
- ✅ Expert panel approved (Winston: Architecture ✅ | Mary: Business ✅ | Amelia: Feasibility ✅ | Murat: Testability ✅)

---

### Epic 3: Real-Time Chat Interface

**Epic Goal:** Build a responsive, real-time chat interface using SignalR WebSocket communication and Ant Design components, enabling users to interact with BMAD workflows through natural conversation with immediate feedback.

**Requirements Covered:** FR1 (start workflows via chat), FR3 (view workflow status), FR12-FR15 (persona communication), NFR1 (2s acknowledgment), NFR14 (10 min first workflow)

**Duration:** 2-3 weeks | **Stories:** 6 | **Total Points:** 34

---

#### Story 3.1: SignalR Hub Setup & WebSocket Connection

**Story ID:** E3-S1  
**Points:** 5  

As a user (Sarah), I want to establish a persistent WebSocket connection to bmadServer, so that I can receive real-time updates and send messages without page refreshes.

**Acceptance Criteria:**

**Given** the bmadServer API is running  
**When** I add SignalR to the project via `dotnet add package Microsoft.AspNetCore.SignalR`  
**Then** the package installs without errors  
**And** the project references Microsoft.AspNetCore.SignalR namespace

**Given** SignalR is installed  
**When** I create `Hubs/ChatHub.cs`  
**Then** the file defines a ChatHub class inheriting from Hub with methods: SendMessage, JoinWorkflow, LeaveWorkflow, OnConnectedAsync, OnDisconnectedAsync

**Given** ChatHub is created  
**When** I configure SignalR in `Program.cs`  
**Then** builder.Services.AddSignalR() and app.MapHub<ChatHub>("/hubs/chat") are configured  
**And** the hub endpoint is accessible at `/hubs/chat`

**Given** the SignalR hub is configured  
**When** I authenticate and connect from a JavaScript client with accessTokenFactory  
**Then** the connection establishes successfully  
**And** OnConnectedAsync is called on the server  
**And** the connection ID is logged for debugging

**Given** I am connected to the hub  
**When** I send a message via connection.invoke("SendMessage", "Hello")  
**Then** the server receives the message within 100ms  
**And** the message is acknowledged within 2 seconds (NFR1)

**Given** the WebSocket connection drops unexpectedly  
**When** SignalR automatic reconnect triggers  
**Then** the client attempts reconnection with exponential backoff (0s, 2s, 10s, 30s)  
**And** the session recovery flow from Epic 2 executes

---

#### Story 3.2: Chat Message Component with Ant Design

**Story ID:** E3-S2  
**Points:** 5  

As a user (Sarah), I want to see chat messages in a clean, readable format, so that I can easily follow the conversation with BMAD agents.

**Acceptance Criteria:**

**Given** the React frontend project exists  
**When** I install Ant Design via `npm install antd @ant-design/icons`  
**Then** the packages install without errors

**Given** Ant Design is installed  
**When** I create `components/ChatMessage.tsx`  
**Then** the component renders user messages aligned right (blue), agent messages aligned left (gray), timestamps, and agent avatars

**Given** a message contains markdown formatting  
**When** the message renders  
**Then** markdown is converted to HTML with syntax highlighting for code blocks  
**And** links are clickable and open in new tabs

**Given** an agent is typing a response  
**When** the typing indicator displays  
**Then** I see animated ellipsis with agent name within 500ms of agent starting

**Given** I receive a long message  
**When** the message renders  
**Then** the chat container scrolls automatically with smooth animation

**Given** accessibility requirements apply  
**When** I use a screen reader  
**Then** messages have proper ARIA labels and new messages trigger live region announcements

---

#### Story 3.3: Chat Input Component with Rich Interactions

**Story ID:** E3-S3  
**Points:** 5  

As a user (Sarah), I want a responsive input field with helpful features, so that I can communicate effectively with BMAD agents.

**Acceptance Criteria:**

**Given** the chat interface is loaded  
**When** I view the input area  
**Then** I see multi-line text input, Send button (disabled when empty), character count, and keyboard shortcut hint

**Given** I type a message  
**When** I press Ctrl+Enter (or Cmd+Enter on Mac)  
**Then** the message is sent immediately and the input field clears

**Given** the input exceeds 2000 characters  
**When** I continue typing  
**Then** the character count turns red and Send button becomes disabled

**Given** I type a partial message and navigate away  
**When** I return to the chat  
**Then** my draft message is preserved in local storage

**Given** I type "/" in the input field  
**When** the command palette appears  
**Then** I see options: /help, /status, /pause, /resume with arrow key navigation

**Given** I send a message and the server is slow (> 5 seconds)  
**When** I see the processing indicator  
**Then** I can click "Cancel" to abort the request

---

#### Story 3.4: Real-Time Message Streaming

**Story ID:** E3-S4  
**Points:** 8  

As a user (Marcus), I want to see agent responses stream in real-time, so that I get immediate feedback and can follow long responses as they're generated.

**Acceptance Criteria:**

**Given** I send a message to an agent  
**When** the agent starts generating a response  
**Then** streaming begins within 5 seconds (NFR2) and the first token appears

**Given** an agent is streaming a response  
**When** tokens arrive via SignalR  
**Then** each token appends to the message smoothly without flickering

**Given** a streaming response is in progress  
**When** I check the SignalR message format  
**Then** I see MESSAGE_CHUNK type with messageId, chunk, isComplete, and agentId fields

**Given** streaming completes  
**When** the final chunk arrives with isComplete: true  
**Then** the typing indicator disappears and full message displays with formatting

**Given** streaming is interrupted by network issues  
**When** the SignalR connection drops mid-stream  
**Then** the partial message is preserved and reconnection resumes from last chunk

**Given** I want to cancel a long-running response  
**When** I click "Stop Generating" during streaming  
**Then** streaming stops immediately with "(Stopped)" indicator

---

#### Story 3.5: Chat History & Scroll Management

**Story ID:** E3-S5  
**Points:** 5  

As a user (Sarah), I want to review previous messages in our conversation, so that I can reference earlier context and decisions.

**Acceptance Criteria:**

**Given** I open a workflow chat  
**When** the chat loads  
**Then** the last 50 messages display with scroll position at bottom

**Given** I scroll up to view older messages  
**When** I reach the top  
**Then** "Load More" appears and loads next 50 messages without scroll jump

**Given** I am scrolled up reading old messages  
**When** a new message arrives  
**Then** a "New message" badge appears at bottom without disrupting my position

**Given** I close and reopen the chat  
**When** the chat reloads  
**Then** my last scroll position is restored

**Given** chat history is empty (new workflow)  
**When** I view the chat  
**Then** I see a welcome message with quick-start buttons for common workflows

---

#### Story 3.6: Mobile-Responsive Chat Interface

**Story ID:** E3-S6  
**Points:** 6  

As a user (Sarah) on mobile, I want the chat interface to work seamlessly on my phone, so that I can approve decisions and monitor workflows on the go.

**Acceptance Criteria:**

**Given** I access bmadServer on mobile (< 768px width)  
**When** the chat interface loads  
**Then** layout adapts to single-column with sidebar collapsed to hamburger menu

**Given** I am on mobile  
**When** I view the chat input area  
**Then** input expands to full width with touch-friendly 44px+ tap targets

**Given** I type on mobile  
**When** the virtual keyboard appears  
**Then** the chat scrolls to keep input visible and input stays fixed at bottom

**Given** I receive a message on mobile  
**When** I interact with the chat  
**Then** touch gestures work: swipe down to refresh, tap-hold to copy, swipe to dismiss

**Given** accessibility on mobile  
**When** I use VoiceOver or TalkBack  
**Then** all interactive elements are announced and gestures work with screen readers

**Given** reduced motion preference is enabled  
**When** animations would normally play  
**Then** animations are disabled or reduced

---

**Epic 3 Summary:**
- ✅ 6 stories, 34 points
- ✅ 2-3 week timeline
- ✅ Real-time foundation for all user interactions
- ✅ Expert panel approved (Winston: Architecture ✅ | Mary: Business ✅ | Amelia: Feasibility ✅ | Murat: Testability ✅)

---

### Epic 4: Workflow Orchestration Engine

**Epic Goal:** Build the core workflow execution engine that manages BMAD workflow steps, agent routing, state transitions, and enables users to start, pause, resume, and complete workflows with full BMAD parity.

**Requirements Covered:** FR1-FR5 (workflow control), FR25-FR27 (BMAD parity), NFR2-NFR3 (response times)

**Duration:** 3-4 weeks | **Stories:** 7 | **Total Points:** 42

---

#### Story 4.1: Workflow Definition & Registry

**Story ID:** E4-S1  
**Points:** 5  

As a developer, I want a workflow registry that defines all supported BMAD workflows, so that the system knows which workflows are available and their step sequences.

**Acceptance Criteria:**

**Given** I need to define BMAD workflows  
**When** I create `Workflows/WorkflowDefinition.cs`  
**Then** the class includes: WorkflowId, Name, Description, Steps (ordered list), RequiredRoles, EstimatedDuration

**Given** workflow definitions exist  
**When** I create `Workflows/WorkflowRegistry.cs`  
**Then** it provides methods: GetAllWorkflows(), GetWorkflow(id), ValidateWorkflow(id)  
**And** workflows are registered at startup via dependency injection

**Given** the registry is populated  
**When** I query GetAllWorkflows()  
**Then** I receive all BMAD workflows: create-prd, create-architecture, create-stories, design-ux, and others from BMAD spec

**Given** each workflow has steps  
**When** I examine a workflow definition  
**Then** each step includes: StepId, Name, AgentId, InputSchema, OutputSchema, IsOptional, CanSkip

**Given** I request a non-existent workflow  
**When** I call GetWorkflow("invalid-id")  
**Then** the system returns null or throws WorkflowNotFoundException

---

#### Story 4.2: Workflow Instance Creation & State Machine

**Story ID:** E4-S2  
**Points:** 8  

As a user (Marcus), I want to start a new workflow instance, so that I can begin a BMAD process like creating a PRD.

**Acceptance Criteria:**

**Given** I am authenticated with Participant role  
**When** I send POST `/api/v1/workflows` with workflowId and initial parameters  
**Then** the system creates a WorkflowInstance record with: Id, WorkflowDefinitionId, UserId, CurrentStep, Status (Created), CreatedAt

**Given** a workflow instance is created  
**When** I examine the state machine  
**Then** valid states include: Created, Running, Paused, WaitingForInput, WaitingForApproval, Completed, Failed, Cancelled

**Given** a workflow instance exists  
**When** state transitions occur  
**Then** only valid transitions are allowed (e.g., Created->Running, Running->Paused, not Created->Completed)  
**And** invalid transitions return 400 Bad Request with explanation

**Given** a workflow starts  
**When** the first step executes  
**Then** Status changes from Created to Running  
**And** CurrentStep is set to step 1  
**And** an event is logged to the WorkflowEvents table

**Given** I check the database schema  
**When** I run the migration for WorkflowInstances  
**Then** the table includes JSONB columns for StepData and Context with proper indexes

---

#### Story 4.3: Step Execution & Agent Routing

**Story ID:** E4-S3  
**Points:** 8  

As a user (Marcus), I want workflow steps to automatically route to the correct agent, so that each step is handled by the appropriate specialist.

**Acceptance Criteria:**

**Given** a workflow is running  
**When** the current step requires an agent  
**Then** the system looks up the AgentId from the step definition  
**And** routes the request to the correct agent handler

**Given** step execution begins  
**When** the agent processes the step  
**Then** the agent receives: workflow context, step parameters, conversation history, user input

**Given** an agent completes a step  
**When** the response is received  
**Then** the step output is validated against OutputSchema  
**And** StepData is updated with the result  
**And** CurrentStep advances to the next step

**Given** step execution takes time  
**When** processing exceeds 5 seconds  
**Then** streaming begins to the client (NFR2)  
**And** the user sees real-time progress

**Given** a step fails  
**When** an error occurs during agent processing  
**Then** the workflow transitions to Failed state (if unrecoverable) or WaitingForInput (if retry possible)  
**And** the error is logged with full context

**Given** I need to track step history  
**When** I query the WorkflowStepHistory table  
**Then** I see all executed steps with: StepId, StartedAt, CompletedAt, Status, Input, Output

---

#### Story 4.4: Workflow Pause & Resume

**Story ID:** E4-S4  
**Points:** 5  

As a user (Sarah), I want to pause and resume a workflow, so that I can take breaks without losing progress.

**Acceptance Criteria:**

**Given** a workflow is in Running state  
**When** I send POST `/api/v1/workflows/{id}/pause`  
**Then** the workflow transitions to Paused state  
**And** a pause event is logged with timestamp and userId  
**And** I receive 200 OK with updated workflow state

**Given** a workflow is in Paused state  
**When** I send POST `/api/v1/workflows/{id}/resume`  
**Then** the workflow transitions back to Running state  
**And** execution continues from the last completed step  
**And** context is fully restored

**Given** I try to pause an already paused workflow  
**When** the request is processed  
**Then** I receive 400 Bad Request with "Workflow is already paused"

**Given** a workflow has been paused for 24+ hours  
**When** I resume the workflow  
**Then** a context refresh occurs to reload any stale data  
**And** I see a notification: "Workflow resumed. Context has been refreshed."

**Given** multiple users are in a collaborative workflow  
**When** one user pauses the workflow  
**Then** all connected users receive a SignalR notification  
**And** their UIs update to show paused state

---

#### Story 4.5: Workflow Exit & Cancellation

**Story ID:** E4-S5  
**Points:** 3  

As a user (Marcus), I want to safely exit or cancel a workflow, so that I can abandon work that's no longer needed.

**Acceptance Criteria:**

**Given** a workflow is in any active state (Running, Paused, WaitingForInput)  
**When** I send POST `/api/v1/workflows/{id}/cancel`  
**Then** the workflow transitions to Cancelled state  
**And** all pending operations are terminated  
**And** a cancellation event is logged

**Given** I cancel a workflow  
**When** the cancellation completes  
**Then** the workflow state is preserved for audit purposes (not deleted)  
**And** I can still view the workflow history  
**And** I cannot resume a cancelled workflow

**Given** I try to cancel a completed workflow  
**When** the request is processed  
**Then** I receive 400 Bad Request with "Cannot cancel a completed workflow"

**Given** a workflow is cancelled  
**When** I view the workflow list  
**Then** cancelled workflows are clearly marked with strikethrough or badge  
**And** I can filter to show/hide cancelled workflows

---

#### Story 4.6: Workflow Step Navigation & Skip

**Story ID:** E4-S6  
**Points:** 5  

As a user (Sarah), I want to skip optional steps or jump to specific steps, so that I can customize the workflow to my needs.

**Acceptance Criteria:**

**Given** the current step is marked as IsOptional: true  
**When** I send POST `/api/v1/workflows/{id}/steps/current/skip`  
**Then** the step is marked as Skipped  
**And** CurrentStep advances to the next step  
**And** the skip is logged with reason (if provided)

**Given** I try to skip a required step  
**When** the request is processed  
**Then** I receive 400 Bad Request with "This step is required and cannot be skipped"

**Given** a step has CanSkip: false but IsOptional: true  
**When** I try to skip  
**Then** I receive 400 Bad Request explaining the step cannot be skipped despite being optional

**Given** I want to return to a previous step  
**When** I send POST `/api/v1/workflows/{id}/steps/{stepId}/goto`  
**Then** the system validates the step is in the step history  
**And** CurrentStep is set to the requested step  
**And** a "step revisit" event is logged

**Given** I go back to a previous step  
**When** I re-execute that step  
**Then** the previous output for that step is available for reference  
**And** I can modify or confirm the previous decisions

---

#### Story 4.7: Workflow Status & Progress API

**Story ID:** E4-S7  
**Points:** 8  

As a user (Marcus), I want to view detailed workflow status and progress, so that I know exactly where I am in the process.

**Acceptance Criteria:**

**Given** I have an active workflow  
**When** I send GET `/api/v1/workflows/{id}`  
**Then** I receive comprehensive status including: id, name, status, currentStep, totalSteps, percentComplete, startedAt, estimatedCompletion

**Given** I query workflow status  
**When** the response includes step details  
**Then** each step shows: stepId, name, status (Pending/Current/Completed/Skipped), completedAt, agent name

**Given** I want real-time status updates  
**When** I am connected via SignalR  
**Then** I receive WORKFLOW_STATUS_CHANGED events whenever status or step changes

**Given** I query all my workflows  
**When** I send GET `/api/v1/workflows?status=running`  
**Then** I receive a paginated list of my workflows matching the filter  
**And** I can filter by: status, workflowType, createdAfter, createdBefore

**Given** I view workflow progress  
**When** the UI renders  
**Then** I see a visual progress indicator (stepper component) showing completed, current, and upcoming steps

**Given** workflow completion time is estimated  
**When** I check estimatedCompletion  
**Then** the estimate is based on: average step duration, remaining steps, historical data for this workflow type

---

**Epic 4 Summary:**
- ✅ 7 stories, 42 points
- ✅ 3-4 week timeline
- ✅ Core engine enabling all BMAD workflows
- ✅ Expert panel approved (Winston: Architecture ✅ | Mary: Business ✅ | Amelia: Feasibility ✅ | Murat: Testability ✅)

---

### Epic 5: Multi-Agent Collaboration

**Epic Goal:** Enable seamless collaboration between BMAD agents, allowing them to share context, hand off work, and coordinate on complex tasks while maintaining transparency for users.

**Requirements Covered:** FR5 (agent routing), FR21-FR24 (agent collaboration), NFR2-NFR3 (response times)

**Duration:** 2-3 weeks | **Stories:** 5 | **Total Points:** 29

---

#### Story 5.1: Agent Registry & Configuration

**Story ID:** E5-S1  
**Points:** 5  

As a developer, I want a centralized agent registry, so that the system knows all available agents and their capabilities.

**Acceptance Criteria:**

**Given** I need to define BMAD agents  
**When** I create `Agents/AgentDefinition.cs`  
**Then** it includes: AgentId, Name, Description, Capabilities (list), SystemPrompt, ModelPreference

**Given** agent definitions exist  
**When** I create `Agents/AgentRegistry.cs`  
**Then** it provides: GetAllAgents(), GetAgent(id), GetAgentsByCapability(capability)

**Given** the registry is populated  
**When** I query GetAllAgents()  
**Then** I receive BMAD agents: ProductManager, Architect, Designer, Developer, Analyst, Orchestrator

**Given** each agent has capabilities  
**When** I examine an agent definition  
**Then** capabilities map to workflow steps they can handle (e.g., Architect handles "create-architecture")

**Given** agents have model preferences  
**When** an agent is invoked  
**Then** the system routes to the preferred model (configurable for cost/quality tradeoffs)

---

#### Story 5.2: Agent-to-Agent Messaging

**Story ID:** E5-S2  
**Points:** 8  

As an agent (Architect), I want to request information from other agents, so that I can gather inputs needed for my work.

**Acceptance Criteria:**

**Given** an agent is processing a step  
**When** it needs input from another agent  
**Then** it can call AgentMessaging.RequestFromAgent(targetAgentId, request, context)

**Given** an agent request is made  
**When** the target agent receives it  
**Then** the request includes: sourceAgentId, requestType, payload, workflowContext, conversationHistory

**Given** the target agent processes the request  
**When** a response is generated  
**Then** the response is returned to the source agent  
**And** the exchange is logged for transparency

**Given** agent-to-agent communication occurs  
**When** I check the message format  
**Then** I see: messageId, timestamp, sourceAgent, targetAgent, messageType, content, workflowInstanceId

**Given** an agent request times out (> 30 seconds)  
**When** no response is received  
**Then** the system retries once  
**And** if still no response, returns error to source agent  
**And** the timeout is logged for debugging

---

#### Story 5.3: Shared Workflow Context

**Story ID:** E5-S3  
**Points:** 5  

As an agent (Designer), I want access to the full workflow context, so that I can make decisions informed by previous steps.

**Acceptance Criteria:**

**Given** a workflow has multiple completed steps  
**When** an agent receives a request  
**Then** it has access to SharedContext containing: all step outputs, decision history, user preferences, artifact references

**Given** an agent needs specific prior output  
**When** it queries SharedContext.GetStepOutput(stepId)  
**Then** it receives the structured output from that step  
**And** null is returned if step hasn't completed

**Given** an agent produces output  
**When** the step completes  
**Then** the output is automatically added to SharedContext  
**And** subsequent agents can access it immediately

**Given** context size grows large  
**When** the context exceeds token limits  
**Then** the system summarizes older context while preserving key decisions  
**And** full context remains available in database for reference

**Given** concurrent agents access context  
**When** simultaneous reads/writes occur  
**Then** optimistic concurrency control prevents conflicts  
**And** version numbers track context changes

---

#### Story 5.4: Agent Handoff & Attribution

**Story ID:** E5-S4  
**Points:** 5  

As a user (Sarah), I want to see when different agents take over, so that I understand who is responsible for each part of the workflow.

**Acceptance Criteria:**

**Given** a workflow step changes agents  
**When** the handoff occurs  
**Then** the UI displays a handoff indicator: "Handing off to [AgentName]..."

**Given** an agent completes their work  
**When** I view the chat history  
**Then** each message shows the agent avatar and name  
**And** I can distinguish between different agents' contributions

**Given** a decision is made by an agent  
**When** I review the decision  
**Then** I see attribution: "Decided by [AgentName] at [timestamp]"  
**And** the reasoning is visible

**Given** I hover over an agent indicator  
**When** the tooltip displays  
**Then** I see: agent name, description, capabilities, current step responsibility

**Given** handoffs are logged  
**When** I query the audit log  
**Then** I see all handoffs with: fromAgent, toAgent, timestamp, workflowStep, reason

---

#### Story 5.5: Human Approval for Low-Confidence Decisions

**Story ID:** E5-S5  
**Points:** 6  

As a user (Marcus), I want the system to pause for my approval when agents are uncertain, so that I maintain control over important decisions.

**Acceptance Criteria:**

**Given** an agent generates a response  
**When** confidence score is below threshold (< 0.7)  
**Then** the workflow transitions to WaitingForApproval state  
**And** I receive a notification: "Agent needs your input on this decision"

**Given** approval is requested  
**When** I view the approval UI  
**Then** I see: agent's proposed response, confidence score, reasoning, options to Approve/Modify/Reject

**Given** I approve the decision  
**When** I click "Approve"  
**Then** the workflow resumes with the proposed response  
**And** approval is logged with my userId

**Given** I modify the decision  
**When** I edit the proposed response and confirm  
**Then** the modified version is used  
**And** both original and modified versions are logged

**Given** I reject the decision  
**When** I click "Reject" with reason  
**Then** the agent regenerates with additional guidance  
**And** a new approval request may be triggered

**Given** an approval request is pending  
**When** 24 hours pass without action  
**Then** I receive a reminder notification  
**And** after 72 hours, the workflow auto-pauses with timeout warning

---

**Epic 5 Summary:**
- ✅ 5 stories, 29 points
- ✅ 2-3 week timeline
- ✅ Enables intelligent agent collaboration
- ✅ Expert panel approved (Winston: Architecture ✅ | Mary: Business ✅ | Amelia: Feasibility ✅ | Murat: Testability ✅)

---

### Epic 6: Decision Management & Locking

**Epic Goal:** Provide robust decision capture, versioning, and locking mechanisms so that workflow decisions are traceable, auditable, and protected from unintended changes.

**Requirements Covered:** FR9 (lock decisions), FR10 (decision review), FR22-FR23 (structured outputs, attribution), NFR5 (workflow reliability)

**Duration:** 2 weeks | **Stories:** 5 | **Total Points:** 26

---

#### Story 6.1: Decision Capture & Storage

**Story ID:** E6-S1  
**Points:** 5  

As a user (Sarah), I want my decisions to be captured and stored, so that I have a record of what was decided and when.

**Acceptance Criteria:**

**Given** I make a decision in a workflow  
**When** I confirm my choice  
**Then** a Decision record is created with: id, workflowInstanceId, stepId, decisionType, value, decidedBy, decidedAt

**Given** a decision is stored  
**When** I query GET `/api/v1/workflows/{id}/decisions`  
**Then** I receive all decisions for that workflow in chronological order

**Given** I examine a decision  
**When** I view the decision details  
**Then** I see: the question asked, options presented, selected option, reasoning (if provided), context at time of decision

**Given** the Decisions table migration runs  
**When** I check the schema  
**Then** it includes JSONB for value and context, with GIN indexes for querying

**Given** a decision involves structured data  
**When** the decision is captured  
**Then** the value is stored as validated JSON matching the expected schema

---

#### Story 6.2: Decision Version History

**Story ID:** E6-S2  
**Points:** 5  

As a user (Marcus), I want to see the history of changes to a decision, so that I can understand how it evolved.

**Acceptance Criteria:**

**Given** a decision exists  
**When** I modify the decision  
**Then** a new DecisionVersion record is created  
**And** the previous version is preserved  
**And** version number increments

**Given** I query decision history  
**When** I send GET `/api/v1/decisions/{id}/history`  
**Then** I receive all versions with: versionNumber, value, modifiedBy, modifiedAt, changeReason

**Given** I compare versions  
**When** I request a diff between two versions  
**Then** the system shows what changed (added, removed, modified fields)

**Given** I want to revert a decision  
**When** I send POST `/api/v1/decisions/{id}/revert?version=2`  
**Then** a new version is created with the content of version 2  
**And** the revert action is logged

**Given** version history exists  
**When** I view the decision in the UI  
**Then** I see a version indicator and can expand to view history timeline

---

#### Story 6.3: Decision Locking Mechanism

**Story ID:** E6-S3  
**Points:** 5  

As a user (Sarah), I want to lock important decisions, so that they cannot be accidentally changed.

**Acceptance Criteria:**

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

---

#### Story 6.4: Decision Review Workflow

**Story ID:** E6-S4  
**Points:** 5  

As a user (Marcus), I want to request a review before locking a decision, so that I can get approval from stakeholders.

**Acceptance Criteria:**

**Given** I have a decision ready to lock  
**When** I send POST `/api/v1/decisions/{id}/request-review` with reviewers list  
**Then** the decision status changes to UnderReview  
**And** selected reviewers receive notifications

**Given** a review is requested  
**When** a reviewer views the decision  
**Then** they see: decision content, requester info, deadline (if set), Approve/Request Changes buttons

**Given** a reviewer approves  
**When** they click "Approve"  
**Then** their approval is recorded  
**And** if all required approvals received, decision auto-locks

**Given** a reviewer requests changes  
**When** they click "Request Changes" with comments  
**Then** the decision returns to Draft status  
**And** the requester is notified with feedback

**Given** the review deadline passes  
**When** approvals are incomplete  
**Then** the requester is notified  
**And** they can extend deadline or proceed without full approval

---

#### Story 6.5: Conflict Detection & Resolution

**Story ID:** E6-S5  
**Points:** 6  

As a user (Sarah), I want the system to detect conflicting decisions, so that inconsistencies are caught early.

**Acceptance Criteria:**

**Given** multiple decisions in a workflow  
**When** a new decision contradicts an existing one  
**Then** the system flags a potential conflict  
**And** I see a warning: "This may conflict with decision [X]"

**Given** a conflict is detected  
**When** I view the conflict details  
**Then** I see: both decisions side by side, the nature of the conflict, suggested resolutions

**Given** I want to resolve a conflict  
**When** I choose a resolution option  
**Then** the system updates both decisions accordingly  
**And** the conflict resolution is logged

**Given** conflict detection rules exist  
**When** I examine the configuration  
**Then** I see rules like: "Budget cannot exceed [X]", "Timeline must be consistent", "Feature scope must match PRD"

**Given** I override a conflict warning  
**When** I proceed despite the conflict  
**Then** the override is logged with my justification  
**And** an audit trail exists for compliance

---

**Epic 6 Summary:**
- ✅ 5 stories, 26 points
- ✅ 2 week timeline
- ✅ Critical for decision traceability and governance
- ✅ Expert panel approved (Winston: Architecture ✅ | Mary: Business ✅ | Amelia: Feasibility ✅ | Murat: Testability ✅)

---

### Epic 7: Collaboration & Multi-User Support

**Epic Goal:** Enable multiple users to collaborate on the same workflow safely, with proper input buffering, checkpoint management, and conflict resolution to preserve workflow integrity.

**Requirements Covered:** FR6-FR8 (multi-user collaboration), FR11 (conflict arbitration), NFR10 (25 concurrent users)

**Duration:** 2-3 weeks | **Stories:** 5 | **Total Points:** 31

---

#### Story 7.1: Multi-User Workflow Participation

**Story ID:** E7-S1  
**Points:** 5  

As a user (Sarah), I want to invite team members to my workflow, so that we can collaborate on product development together.

**Acceptance Criteria:**

**Given** I own a workflow  
**When** I send POST `/api/v1/workflows/{id}/participants` with userId and role (Contributor/Observer)  
**Then** the user is added to the workflow  
**And** they receive an invitation notification  
**And** they appear in the participants list

**Given** a user is added as Contributor  
**When** they access the workflow  
**Then** they can send messages, make decisions, and advance steps  
**And** their actions are attributed to them

**Given** a user is added as Observer  
**When** they access the workflow  
**Then** they can view messages and decisions  
**And** they cannot make changes or send messages  
**And** the UI shows read-only mode

**Given** multiple users are connected  
**When** I view the workflow  
**Then** I see presence indicators showing who is online  
**And** I see typing indicators when others are composing messages

**Given** I want to remove a participant  
**When** I send DELETE `/api/v1/workflows/{id}/participants/{userId}`  
**Then** the user loses access immediately  
**And** they receive a notification  
**And** their future access attempts are denied

---

#### Story 7.2: Safe Checkpoint System

**Story ID:** E7-S2  
**Points:** 8  

As a user (Marcus), I want inputs to be applied at safe checkpoints, so that workflow integrity is maintained even with concurrent contributions.

**Acceptance Criteria:**

**Given** a workflow step is in progress  
**When** I submit an input  
**Then** the input is queued until the current step reaches a checkpoint  
**And** I see "Input queued - will be applied at next checkpoint"

**Given** a checkpoint is reached  
**When** queued inputs are processed  
**Then** inputs are applied in order received (FIFO)  
**And** each input is validated before application  
**And** invalid inputs are rejected with feedback

**Given** I check the checkpoint definition  
**When** I examine the workflow step  
**Then** checkpoints are defined at: step completion, decision confirmation, agent handoff, explicit save points

**Given** a step fails after accepting inputs  
**When** rollback occurs  
**Then** the state reverts to the last successful checkpoint  
**And** queued inputs are preserved for retry  
**And** users are notified of the rollback

**Given** I query checkpoint history  
**When** I send GET `/api/v1/workflows/{id}/checkpoints`  
**Then** I see all checkpoints with: timestamp, stepId, state snapshot reference, triggeredBy

---

#### Story 7.3: Input Attribution & History

**Story ID:** E7-S3  
**Points:** 5  

As a user (Sarah), I want to see who provided each input and when, so that I can track contributions and understand decisions.

**Acceptance Criteria:**

**Given** any input is submitted  
**When** the input is stored  
**Then** it includes: userId, displayName, timestamp, inputType, content, workflowStep

**Given** I view the chat history  
**When** I see a user message  
**Then** I see the contributor's avatar, name, and timestamp  
**And** I can click to view their profile

**Given** I view a decision  
**When** I examine the attribution  
**Then** I see who made the decision, when, and what alternatives were considered

**Given** I want contribution metrics  
**When** I query GET `/api/v1/workflows/{id}/contributions`  
**Then** I receive per-user stats: messages sent, decisions made, time spent

**Given** I export workflow history  
**When** I download the export  
**Then** all inputs include full attribution metadata  
**And** the export is compliant with audit requirements

---

#### Story 7.4: Conflict Detection & Buffering

**Story ID:** E7-S4  
**Points:** 8  

As a user (Marcus), I want conflicting inputs to be buffered and flagged, so that we can resolve disagreements properly.

**Acceptance Criteria:**

**Given** two users submit different inputs for the same field  
**When** both inputs arrive before checkpoint  
**Then** the system detects the conflict  
**And** both inputs are buffered (not applied)  
**And** users are notified: "Conflict detected - human arbitration required"

**Given** a conflict is detected  
**When** I view the conflict UI  
**Then** I see: both proposed values, who submitted each, timestamp, field context

**Given** I am a workflow owner or Admin  
**When** I resolve the conflict  
**Then** I can choose: Accept A, Accept B, Merge, Reject Both  
**And** the resolution is applied at next checkpoint

**Given** a conflict remains unresolved for 1 hour  
**When** the timeout occurs  
**Then** the workflow pauses  
**And** escalation notifications are sent to workflow owner  
**And** the workflow cannot proceed until resolved

**Given** conflicts are resolved  
**When** I query conflict history  
**Then** I see all conflicts with: inputs, resolution, resolver, timestamp, reason

---

#### Story 7.5: Real-Time Collaboration Updates

**Story ID:** E7-S5  
**Points:** 5  

As a user (Sarah), I want to see changes from other users in real-time, so that I stay in sync with my team.

**Acceptance Criteria:**

**Given** multiple users are in a workflow  
**When** one user sends a message  
**Then** all other connected users see the message within 500ms via SignalR

**Given** a user makes a decision  
**When** the decision is confirmed  
**Then** all users receive a DECISION_MADE event  
**And** their UIs update to reflect the decision

**Given** a workflow step advances  
**When** the step changes  
**Then** all users receive a STEP_CHANGED event  
**And** everyone sees the new current step

**Given** a user goes offline  
**When** their connection drops  
**Then** other users see their presence indicator change  
**And** when they reconnect, they receive all missed updates

**Given** high-frequency updates occur  
**When** many users are active  
**Then** updates are batched (50ms window) to prevent UI thrashing  
**And** the system remains responsive under load

---

**Epic 7 Summary:**
- ✅ 5 stories, 31 points
- ✅ 2-3 week timeline
- ✅ Enables team collaboration at scale
- ✅ Expert panel approved (Winston: Architecture ✅ | Mary: Business ✅ | Amelia: Feasibility ✅ | Murat: Testability ✅)

---

### Epic 8: Persona Translation & Language Adaptation

**Epic Goal:** Enable the system to adapt its communication style based on user personas, translating between business and technical language so that all stakeholders can participate effectively.

**Requirements Covered:** FR12-FR15 (persona communication), NFR1 (responsive interactions)

**Duration:** 2 weeks | **Stories:** 5 | **Total Points:** 26

---

#### Story 8.1: Persona Profile Configuration

**Story ID:** E8-S1  
**Points:** 5  

As a user (Sarah), I want to set my communication preference, so that the system speaks to me in language I understand.

**Acceptance Criteria:**

**Given** I am setting up my profile  
**When** I access persona settings  
**Then** I see options: Business (non-technical), Technical (developer), Hybrid (adaptive)

**Given** I select Business persona  
**When** I save my preference  
**Then** my user profile includes personaType: "business"  
**And** the setting persists across sessions

**Given** I view persona descriptions  
**When** I hover over each option  
**Then** I see examples of how responses will differ:
  - Business: "The system will validate your product requirements"
  - Technical: "The API will execute JSON schema validation on the PRD payload"

**Given** I don't set a persona  
**When** I start using the system  
**Then** the default is Hybrid (adaptive based on context)

**Given** I query user profile  
**When** I send GET `/api/v1/users/me`  
**Then** the response includes personaType and language preferences

---

#### Story 8.2: Business Language Translation

**Story ID:** E8-S2  
**Points:** 5  

As a non-technical user (Sarah), I want technical outputs translated to business language, so that I can understand and make decisions.

**Acceptance Criteria:**

**Given** I have Business persona set  
**When** an agent generates technical content  
**Then** the response is automatically translated to business terms  
**And** technical jargon is replaced with plain language

**Given** a technical error occurs  
**When** I see the error message  
**Then** it explains the issue in business terms: "We couldn't save your changes because another team member is editing" (not "409 Conflict: optimistic concurrency violation")

**Given** architecture decisions are presented  
**When** I view the recommendations  
**Then** I see business impact: "This choice means faster loading times for users" (not "implementing CDN caching layer")

**Given** I need technical details  
**When** I click "Show Technical Details"  
**Then** I can expand to see the original technical content  
**And** this doesn't change my persona setting

**Given** translation quality is measured  
**When** I provide feedback on clarity  
**Then** the system logs my rating and improves translations over time

---

#### Story 8.3: Technical Language Mode

**Story ID:** E8-S3  
**Points:** 5  

As a developer (Marcus), I want full technical details, so that I can make informed implementation decisions.

**Acceptance Criteria:**

**Given** I have Technical persona set  
**When** an agent generates content  
**Then** I receive full technical details including: code snippets, API specifications, architecture diagrams

**Given** I view a workflow step  
**When** technical details are available  
**Then** I see: data schemas, integration points, performance considerations, security implications

**Given** I ask a technical question  
**When** the agent responds  
**Then** the response includes: specific technologies, version numbers, configuration examples

**Given** I'm in technical mode  
**When** business stakeholders join the workflow  
**Then** they see their persona-appropriate version  
**And** my view remains technical

**Given** I switch to Hybrid mode  
**When** the context changes  
**Then** responses adapt: technical for implementation steps, business for strategy decisions

---

#### Story 8.4: In-Session Persona Switching

**Story ID:** E8-S4  
**Points:** 5  

As a user (Marcus), I want to switch personas during a session, so that I can adapt to different contexts.

**Acceptance Criteria:**

**Given** I am in a workflow  
**When** I click the persona switcher in the UI  
**Then** I see current persona highlighted and other options available

**Given** I switch from Technical to Business  
**When** the switch completes  
**Then** future messages are translated to business language  
**And** previous messages retain their original format  
**And** a notification confirms: "Switched to Business mode"

**Given** I switch personas frequently  
**When** I've switched more than 3 times in a session  
**Then** the system suggests: "Would you like to try Hybrid mode instead?"

**Given** I switch personas  
**When** the session ends  
**Then** my default persona remains unchanged (per-profile setting)  
**And** session switches are logged for analytics

**Given** keyboard shortcut exists  
**When** I press Ctrl+Shift+P  
**Then** the persona switcher opens  
**And** I can select with arrow keys

---

#### Story 8.5: Context-Aware Response Adaptation

**Story ID:** E8-S5  
**Points:** 6  

As the system, I want to adapt responses based on context even within a persona, so that communication is always appropriate.

**Acceptance Criteria:**

**Given** Hybrid persona is active  
**When** the workflow step is technical (e.g., architecture review)  
**Then** responses lean technical with code examples

**Given** Hybrid persona is active  
**When** the workflow step is strategic (e.g., PRD review)  
**Then** responses lean business with impact analysis

**Given** a user asks a technical question in Business mode  
**When** the question clearly requires technical answer  
**Then** the system provides technical detail with business context wrapper

**Given** multiple personas are in a collaborative session  
**When** a shared message is sent  
**Then** each user sees their persona-appropriate version  
**And** the underlying content is identical

**Given** response adaptation occurs  
**When** I check the API response  
**Then** I see: originalContent, adaptedContent, adaptationReason, targetPersona

---

**Epic 8 Summary:**
- ✅ 5 stories, 26 points
- ✅ 2 week timeline
- ✅ Bridges business and technical stakeholders
- ✅ Expert panel approved (Winston: Architecture ✅ | Mary: Business ✅ | Amelia: Feasibility ✅ | Murat: Testability ✅)

---

### Epic 9: Data Persistence & State Management

**Epic Goal:** Implement robust data persistence with PostgreSQL, event logging, JSONB state storage with concurrency control, and artifact management for complete workflow traceability.

**Requirements Covered:** FR16-FR20 (session/state management), NFR4 (uptime), NFR8 (encryption at rest), NFR9 (audit logs)

**Duration:** 2-3 weeks | **Stories:** 6 | **Total Points:** 34

---

#### Story 9.1: Event Log Architecture

**Story ID:** E9-S1  
**Points:** 5  

As a developer, I want all workflow events logged immutably, so that we have a complete audit trail.

**Acceptance Criteria:**

**Given** any workflow action occurs  
**When** the action completes  
**Then** an event is appended to the WorkflowEvents table with: id, workflowInstanceId, eventType, payload, userId, timestamp, correlationId

**Given** the event log schema exists  
**When** I examine the table  
**Then** it uses append-only semantics (no UPDATE/DELETE in application code)  
**And** partitioning is configured by month for performance

**Given** events are logged  
**When** I query by workflowInstanceId  
**Then** I can reconstruct the complete workflow history in order

**Given** event types are defined  
**When** I check the enum  
**Then** I see: WorkflowStarted, StepCompleted, DecisionMade, UserInput, AgentResponse, StateChanged, Error, etc.

**Given** I need to replay events  
**When** I call EventStore.Replay(workflowId, fromSequence)  
**Then** events are returned in sequence order  
**And** I can rebuild state from any point in history

---

#### Story 9.2: JSONB State Storage with Concurrency Control

**Story ID:** E9-S2  
**Points:** 8  

As a developer, I want JSONB state storage with proper concurrency control, so that concurrent updates don't corrupt workflow state.

**Acceptance Criteria:**

**Given** workflow state is stored as JSONB  
**When** I examine the schema  
**Then** state columns include: _version (int), _lastModifiedBy (uuid), _lastModifiedAt (timestamp)

**Given** I update workflow state  
**When** I send the update  
**Then** the system checks _version matches expected value  
**And** if mismatch, returns 409 Conflict with current state

**Given** optimistic concurrency fails  
**When** the client receives 409  
**Then** the client can fetch current state, merge changes, and retry  
**And** the conflict is logged for monitoring

**Given** I need atomic state updates  
**When** multiple fields must change together  
**Then** the update is wrapped in a database transaction  
**And** partial updates are impossible

**Given** GIN indexes exist on JSONB columns  
**When** I query by JSONB path (e.g., state->'currentStep')  
**Then** the query uses the index  
**And** performance is acceptable (< 100ms for typical queries)

---

#### Story 9.3: Artifact Storage & Management

**Story ID:** E9-S3  
**Points:** 5  

As a user (Sarah), I want workflow artifacts stored securely, so that I can access generated documents and outputs.

**Acceptance Criteria:**

**Given** a workflow generates an artifact (PRD, architecture doc, etc.)  
**When** the artifact is created  
**Then** it is stored in the Artifacts table with: id, workflowInstanceId, artifactType, content, format, createdAt, createdBy

**Given** artifacts may be large  
**When** content exceeds 1MB  
**Then** content is stored in object storage (filesystem MVP, S3-compatible later)  
**And** the Artifacts table stores a reference/path

**Given** I query artifacts  
**When** I send GET `/api/v1/workflows/{id}/artifacts`  
**Then** I receive artifact metadata (not content) for listing  
**And** I can download specific artifacts via GET `/api/v1/artifacts/{id}/download`

**Given** I want artifact history  
**When** I query with includeVersions=true  
**Then** I see all versions of each artifact  
**And** I can download any previous version

**Given** artifacts contain sensitive data  
**When** stored at rest  
**Then** encryption is applied (AES-256)  
**And** decryption happens transparently on retrieval

---

#### Story 9.4: Workflow Export & Import

**Story ID:** E9-S4  
**Points:** 5  

As a user (Marcus), I want to export workflow artifacts and data, so that I can use them outside bmadServer.

**Acceptance Criteria:**

**Given** I have a completed workflow  
**When** I send POST `/api/v1/workflows/{id}/export`  
**Then** the system generates an export package containing: all artifacts, decision history, event log summary

**Given** export options exist  
**When** I specify format in the request  
**Then** I can choose: ZIP (all files), JSON (structured data only), PDF (formatted report)

**Given** I export to ZIP  
**When** I download the package  
**Then** it contains: /artifacts/*, /decisions.json, /history.json, /metadata.json

**Given** I want to import a previous export  
**When** I send POST `/api/v1/workflows/import` with the package  
**Then** a new workflow instance is created with imported data  
**And** the import source is recorded in metadata

**Given** export contains sensitive data  
**When** I request export  
**Then** the system validates my access level  
**And** sensitive fields can be redacted based on role

---

#### Story 9.5: Checkpoint Restoration

**Story ID:** E9-S5  
**Points:** 5  

As a user (Sarah), I want to restore previous checkpoints, so that I can recover from mistakes or explore alternative paths.

**Acceptance Criteria:**

**Given** a workflow has checkpoints  
**When** I send GET `/api/v1/workflows/{id}/checkpoints`  
**Then** I see all checkpoints with: id, timestamp, stepId, description, canRestore

**Given** I want to restore a checkpoint  
**When** I send POST `/api/v1/workflows/{id}/checkpoints/{checkpointId}/restore`  
**Then** a new workflow branch is created from that checkpoint  
**And** the original workflow is preserved  
**And** I'm redirected to the new branch

**Given** I restore a checkpoint  
**When** the restoration completes  
**Then** workflow state matches the checkpoint exactly  
**And** subsequent events/decisions are cleared in the branch  
**And** I can proceed from that point

**Given** automatic checkpoints exist  
**When** I examine checkpoint frequency  
**Then** checkpoints are created at: each step completion, hourly during active sessions, before risky operations

**Given** checkpoint storage grows  
**When** checkpoints are older than 90 days  
**Then** they are archived (moved to cold storage)  
**And** restoration requires archive retrieval (may be slower)

---

#### Story 9.6: Audit Log Retention & Compliance

**Story ID:** E9-S6  
**Points:** 6  

As an administrator, I want audit logs retained properly, so that we meet compliance requirements.

**Acceptance Criteria:**

**Given** audit log retention is configured  
**When** I check appsettings.json  
**Then** I see: AuditLogRetentionDays: 90 (configurable per NFR9)

**Given** logs reach retention limit  
**When** the cleanup job runs nightly  
**Then** logs older than retention period are archived/deleted  
**And** the cleanup is itself logged

**Given** I query audit logs  
**When** I send GET `/api/v1/admin/audit-logs` with filters  
**Then** I can filter by: dateRange, userId, eventType, workflowId  
**And** results are paginated

**Given** audit logs must be tamper-evident  
**When** logs are stored  
**Then** each log includes a hash of the previous log (chain)  
**And** tampering can be detected by verifying the chain

**Given** compliance export is needed  
**When** I send POST `/api/v1/admin/audit-logs/export`  
**Then** logs are exported in compliance-friendly format  
**And** the export includes integrity verification data

---

**Epic 9 Summary:**
- ✅ 6 stories, 34 points
- ✅ 2-3 week timeline
- ✅ Foundation for data integrity and compliance
- ✅ Expert panel approved (Winston: Architecture ✅ | Mary: Business ✅ | Amelia: Feasibility ✅ | Murat: Testability ✅)

---

### Epic 10: Error Handling & Recovery

**Epic Goal:** Implement comprehensive error handling, graceful degradation, and recovery mechanisms so that users can continue working even when things go wrong.

**Requirements Covered:** FR17 (workflow recovery), FR24 (low confidence pause), NFR5 (< 5% failures), NFR6 (60s recovery)

**Duration:** 2 weeks | **Stories:** 5 | **Total Points:** 26

---

#### Story 10.1: Graceful Error Handling

**Story ID:** E10-S1  
**Points:** 5  

As a user (Sarah), I want errors to be handled gracefully, so that I understand what went wrong and what to do next.

**Acceptance Criteria:**

**Given** an API error occurs  
**When** the error response is returned  
**Then** it follows ProblemDetails RFC 7807 format with: type, title, status, detail, instance

**Given** a validation error occurs  
**When** I submit invalid data  
**Then** the response includes field-level errors  
**And** each error has: field, message, code

**Given** an internal server error occurs  
**When** the error is logged  
**Then** the user sees: "Something went wrong. Please try again." (not stack trace)  
**And** full details are logged server-side with correlationId

**Given** I experience an error  
**When** I see the error message  
**Then** I see actionable guidance: "Try again", "Contact support", "Check your input"

**Given** errors are tracked  
**When** I check monitoring  
**Then** error rates, types, and trends are visible in Grafana dashboard

---

#### Story 10.2: Connection Recovery & Retry

**Story ID:** E10-S2  
**Points:** 5  

As a user (Marcus), I want automatic connection recovery, so that brief network issues don't disrupt my work.

**Acceptance Criteria:**

**Given** my WebSocket connection drops  
**When** the disconnect is detected  
**Then** I see "Reconnecting..." indicator  
**And** automatic reconnection attempts begin

**Given** reconnection is in progress  
**When** attempts are made  
**Then** exponential backoff is used: 0s, 2s, 10s, 30s intervals  
**And** maximum 5 attempts before giving up

**Given** reconnection succeeds  
**When** the connection is restored  
**Then** I see "Connected" indicator  
**And** session state is restored from last checkpoint  
**And** any queued messages are sent

**Given** reconnection fails after all retries  
**When** the final attempt fails  
**Then** I see: "Unable to connect. Check your internet connection."  
**And** a "Retry" button is available  
**And** my draft input is preserved locally

**Given** the server is temporarily unavailable  
**When** API calls fail  
**Then** the client retries with backoff  
**And** cached data is shown where available

---

#### Story 10.3: Workflow Recovery After Failure

**Story ID:** E10-S3  
**Points:** 8  

As a user (Sarah), I want workflows to recover automatically after failures, so that I don't lose progress.

**Acceptance Criteria:**

**Given** a workflow step fails  
**When** the failure is transient (timeout, temporary service issue)  
**Then** the system automatically retries up to 3 times  
**And** each retry is logged

**Given** retries are exhausted  
**When** the step still fails  
**Then** the workflow transitions to Failed state  
**And** I receive notification: "Step failed after multiple attempts"  
**And** I can manually retry or skip (if optional)

**Given** a workflow is in Failed state  
**When** I send POST `/api/v1/workflows/{id}/recover`  
**Then** the system attempts recovery from last checkpoint  
**And** if successful, workflow resumes from safe state

**Given** the server restarts mid-workflow  
**When** the server comes back online  
**Then** incomplete workflows are detected  
**And** recovery is attempted automatically  
**And** users are notified of recovery status

**Given** recovery fails completely  
**When** manual intervention is needed  
**Then** the admin dashboard shows workflows needing attention  
**And** support can manually restore from checkpoint

---

#### Story 10.4: Conversation Stall Recovery

**Story ID:** E10-S4  
**Points:** 5  

As a user (Marcus), I want the system to detect and recover from conversation stalls, so that I'm not stuck waiting.

**Acceptance Criteria:**

**Given** I send a message  
**When** no response is received within 30 seconds  
**Then** I see: "This is taking longer than expected..."  
**And** options appear: "Keep Waiting", "Retry", "Cancel"

**Given** an agent appears stuck  
**When** 60 seconds pass with no progress  
**Then** the system auto-retries with the same input  
**And** logs indicate "stall detected, auto-retry initiated"

**Given** the conversation is off-track  
**When** the system detects circular or nonsensical responses  
**Then** I see: "The conversation seems off track. Would you like to rephrase or restart this step?"

**Given** I choose to restart a step  
**When** I confirm the restart  
**Then** the step context is cleared  
**And** the agent receives fresh context  
**And** I can provide new input

**Given** stalls are monitored  
**When** stall rate exceeds threshold (> 5% of conversations)  
**Then** alerts are sent to operators  
**And** investigation can begin

---

#### Story 10.5: Graceful Degradation Under Load

**Story ID:** E10-S5  
**Points:** 3  

As an operator, I want the system to degrade gracefully under heavy load, so that core functionality remains available.

**Acceptance Criteria:**

**Given** system load approaches capacity  
**When** concurrent users exceed 80% of limit (20 of 25)  
**Then** new workflow starts are queued  
**And** existing workflows continue normally  
**And** users see: "High demand - new workflows may be delayed"

**Given** the queue has waiting workflows  
**When** capacity becomes available  
**Then** queued workflows start in order  
**And** users are notified: "Your workflow is starting"

**Given** non-essential features exist  
**When** under extreme load  
**Then** features like typing indicators, presence updates are disabled first  
**And** core workflow execution is preserved

**Given** a provider (LLM API) is slow or down  
**When** the issue is detected  
**Then** the system switches to backup provider if configured  
**And** users see: "Using alternative provider - responses may vary slightly"

**Given** degradation occurs  
**When** I check the status page  
**Then** I see current system status and any known issues  
**And** estimated time to resolution (if known)

---

**Epic 10 Summary:**
- ✅ 5 stories, 26 points
- ✅ 2 week timeline
- ✅ Ensures system resilience and user confidence
- ✅ Expert panel approved (Winston: Architecture ✅ | Mary: Business ✅ | Amelia: Feasibility ✅ | Murat: Testability ✅)

---

### Epic 11: Security & Access Control

**Epic Goal:** Implement comprehensive security measures including per-user rate limiting, encryption, security headers, and audit logging to protect users and data.

**Requirements Covered:** FR31 (access management), FR33 (audit), NFR7 (TLS), NFR9 (audit logs), NFR11 (rate limiting)

**Duration:** 2 weeks | **Stories:** 5 | **Total Points:** 26

---

#### Story 11.1: Per-User Rate Limiting

**Story ID:** E11-S1  
**Points:** 5  

As an operator, I want per-user rate limiting, so that no single user can overwhelm the system.

**Acceptance Criteria:**

**Given** rate limiting is configured  
**When** I check appsettings.json  
**Then** I see: RateLimiting: { RequestsPerMinute: 60, BurstLimit: 10, WindowSeconds: 60 }

**Given** a user makes API requests  
**When** they exceed RequestsPerMinute  
**Then** subsequent requests return 429 Too Many Requests  
**And** the response includes Retry-After header

**Given** rate limits are per-user  
**When** User A is rate limited  
**Then** User B's requests are unaffected  
**And** limits are tracked by userId from JWT

**Given** burst traffic occurs  
**When** a user sends 10 requests in 1 second (within BurstLimit)  
**Then** all requests are allowed  
**And** they count toward the per-minute limit

**Given** rate limiting is in effect  
**When** I check metrics  
**Then** I see: rate_limit_hits_total per user, current request counts

---

#### Story 11.2: Security Headers & HTTPS Enforcement

**Story ID:** E11-S2  
**Points:** 5  

As an operator, I want proper security headers and HTTPS enforcement, so that the application is protected from common attacks.

**Acceptance Criteria:**

**Given** an HTTPS request is received  
**When** the response is sent  
**Then** it includes security headers:
  - Strict-Transport-Security: max-age=31536000; includeSubDomains
  - X-Content-Type-Options: nosniff
  - X-Frame-Options: DENY
  - Content-Security-Policy: default-src 'self'
  - X-XSS-Protection: 1; mode=block

**Given** an HTTP request is received in production  
**When** HTTPS is not used  
**Then** the request is redirected to HTTPS (301)  
**And** no sensitive data is returned over HTTP

**Given** TLS is configured  
**When** I check the certificate  
**Then** TLS 1.3 or higher is required  
**And** weak cipher suites are disabled

**Given** CORS is configured  
**When** I check the allowed origins  
**Then** only trusted origins are allowed  
**And** credentials are permitted only for same-origin

**Given** security headers are applied  
**When** I run a security scanner  
**Then** no header-related vulnerabilities are found

---

#### Story 11.3: Input Validation & Sanitization

**Story ID:** E11-S3  
**Points:** 5  

As a developer, I want all inputs validated and sanitized, so that injection attacks are prevented.

**Acceptance Criteria:**

**Given** any user input is received  
**When** the input is processed  
**Then** FluentValidation rules are applied  
**And** invalid input returns 400 Bad Request with details

**Given** input contains potential SQL injection  
**When** the input is processed  
**Then** parameterized queries prevent injection  
**And** no raw SQL is constructed from user input

**Given** input contains potential XSS  
**When** the input is stored and later displayed  
**Then** output encoding prevents script execution  
**And** HTML special characters are escaped

**Given** file uploads are allowed  
**When** a file is uploaded  
**Then** the file type is validated against whitelist  
**And** file content is scanned for malicious content  
**And** filename is sanitized

**Given** API requests have size limits  
**When** a request exceeds 10MB  
**Then** the request is rejected with 413 Payload Too Large

---

#### Story 11.4: Encryption at Rest

**Story ID:** E11-S4  
**Points:** 5  

As an operator, I want sensitive data encrypted at rest, so that data breaches don't expose plaintext data.

**Acceptance Criteria:**

**Given** sensitive data is stored in PostgreSQL  
**When** I check the database configuration  
**Then** Transparent Data Encryption (TDE) is enabled  
**Or** application-level encryption is applied to sensitive columns

**Given** application-level encryption is used  
**When** I check the implementation  
**Then** AES-256 encryption is used  
**And** keys are stored in environment variables (not code)

**Given** encryption keys exist  
**When** I review key management  
**Then** keys can be rotated without downtime  
**And** old data remains readable after rotation

**Given** backups are created  
**When** I examine backup files  
**Then** backups are encrypted  
**And** encryption key is not stored with backup

**Given** I query encrypted data  
**When** the data is returned  
**Then** decryption happens transparently  
**And** application code works with plaintext

---

#### Story 11.5: Security Audit Logging

**Story ID:** E11-S5  
**Points:** 6  

As an administrator, I want comprehensive security audit logging, so that I can investigate security incidents.

**Acceptance Criteria:**

**Given** a security-relevant event occurs  
**When** the event completes  
**Then** an audit log entry is created with: timestamp, userId, action, resource, sourceIP, userAgent, result (success/failure)

**Given** security events are defined  
**When** I check the list  
**Then** logged events include: login attempts (success/failure), permission changes, data exports, admin actions, rate limit violations

**Given** failed login attempts occur  
**When** I query audit logs  
**Then** I see all failed attempts with: email (hashed), IP address, timestamp, failure reason

**Given** suspicious activity is detected (5 failed logins)  
**When** the threshold is reached  
**Then** an alert is generated  
**And** the account is temporarily locked (configurable)

**Given** I investigate an incident  
**When** I query GET `/api/v1/admin/audit-logs?userId={id}`  
**Then** I see all actions by that user  
**And** I can correlate with other logs via correlationId

---

**Epic 11 Summary:**
- ✅ 5 stories, 26 points
- ✅ 2 week timeline
- ✅ Comprehensive security posture
- ✅ Expert panel approved (Winston: Architecture ✅ | Mary: Business ✅ | Amelia: Feasibility ✅ | Murat: Testability ✅)

---

### Epic 12: Admin Dashboard & Operations

**Epic Goal:** Provide administrators with a comprehensive dashboard to monitor system health, manage users, configure providers, and audit workflow activity.

**Requirements Covered:** FR30-FR34 (admin capabilities), NFR4 (uptime monitoring)

**Duration:** 2-3 weeks | **Stories:** 6 | **Total Points:** 34

---

#### Story 12.1: System Health Dashboard

**Story ID:** E12-S1  
**Points:** 5  

As an administrator (Cris), I want a system health dashboard, so that I can monitor bmadServer status at a glance.

**Acceptance Criteria:**

**Given** I access the admin dashboard at `/admin`  
**When** I view the health overview  
**Then** I see: overall status (Healthy/Degraded/Down), uptime percentage, active users, active workflows

**Given** services are monitored  
**When** I view service health  
**Then** I see status for: API, Database, SignalR hub, LLM providers  
**And** each shows: status, latency, last check time

**Given** a service is unhealthy  
**When** the dashboard updates  
**Then** the status changes to red/yellow  
**And** error details are shown  
**And** alerts are triggered (if configured)

**Given** I want historical data  
**When** I select a time range  
**Then** I see: uptime graph, error rate graph, response time percentiles

**Given** the dashboard is open  
**When** new data arrives  
**Then** metrics update in real-time (every 15 seconds)  
**And** no page refresh is required

---

#### Story 12.2: Active Session Monitoring

**Story ID:** E12-S2  
**Points:** 5  

As an administrator, I want to monitor active sessions, so that I can see who is using the system and intervene if needed.

**Acceptance Criteria:**

**Given** I access the sessions view  
**When** I view active sessions  
**Then** I see a table with: userId, displayName, workflowName, sessionDuration, lastActivity, connectionStatus

**Given** I need session details  
**When** I click on a session  
**Then** I see: full session history, current workflow step, recent messages (last 10)

**Given** I need to terminate a session  
**When** I click "Terminate Session" with reason  
**Then** the user is disconnected  
**And** they see: "Your session was ended by an administrator"  
**And** the termination is logged

**Given** I filter sessions  
**When** I apply filters  
**Then** I can filter by: workflow type, duration, idle time, user role

**Given** concurrent user limits apply  
**When** I view session count  
**Then** I see: "15/25 concurrent users"  
**And** warning appears at 80% capacity

---

#### Story 12.3: User Management

**Story ID:** E12-S3  
**Points:** 5  

As an administrator, I want to manage user accounts, so that I can control access to bmadServer.

**Acceptance Criteria:**

**Given** I access user management  
**When** I view the user list  
**Then** I see: displayName, email, roles, status (active/disabled), lastLogin, createdAt

**Given** I search for a user  
**When** I enter search criteria  
**Then** I can search by: name, email, role  
**And** results appear as I type

**Given** I edit a user  
**When** I click "Edit" on a user row  
**Then** I can modify: displayName, roles, status  
**And** changes are saved with audit log entry

**Given** I disable a user  
**When** I set status to "Disabled"  
**Then** the user cannot log in  
**And** active sessions are terminated  
**And** they see: "Your account has been disabled"

**Given** I need to reset a user's password  
**When** I click "Reset Password"  
**Then** a temporary password is generated  
**And** the user must change it on next login  
**And** the reset is logged

---

#### Story 12.4: Provider Configuration

**Story ID:** E12-S4  
**Points:** 8  

As an administrator, I want to configure LLM providers and model routing, so that I can optimize cost and quality.

**Acceptance Criteria:**

**Given** I access provider configuration  
**When** I view available providers  
**Then** I see: provider name, API status, current model, cost metrics, usage stats

**Given** I configure a provider  
**When** I edit provider settings  
**Then** I can set: API key (masked), base URL, default model, rate limits, timeout

**Given** I set model routing rules  
**When** I define a rule  
**Then** I can specify: agent type → preferred model, fallback model, cost limit per request

**Given** a provider is down  
**When** the health check fails  
**Then** automatic failover to backup provider occurs  
**And** I see alert: "Provider [X] down, using fallback [Y]"

**Given** I want to test a provider  
**When** I click "Test Connection"  
**Then** a test request is sent  
**And** I see: response time, token count, success/failure

---

#### Story 12.5: Workflow Activity Audit

**Story ID:** E12-S5  
**Points:** 5  

As an administrator, I want to audit workflow activity, so that I can investigate issues and ensure compliance.

**Acceptance Criteria:**

**Given** I access workflow audit  
**When** I view the audit log  
**Then** I see: timestamp, userId, workflowId, action, details, status

**Given** I filter the audit log  
**When** I apply filters  
**Then** I can filter by: date range, user, workflow, action type, status (success/failure)

**Given** I investigate a workflow  
**When** I click on a workflow entry  
**Then** I see complete timeline: all events, decisions, agent interactions, errors

**Given** I export audit data  
**When** I click "Export"  
**Then** I can download as: CSV, JSON  
**And** export includes all filtered records

**Given** audit data is retained  
**When** I check retention settings  
**Then** I see configurable retention period (default 90 days)  
**And** older data is archived/deleted per policy

---

#### Story 12.6: System Configuration Management

**Story ID:** E12-S6  
**Points:** 6  

As an administrator, I want to configure system settings, so that I can tune bmadServer for our needs.

**Acceptance Criteria:**

**Given** I access system configuration  
**When** I view settings  
**Then** I see categories: Security, Performance, Workflows, Notifications

**Given** I edit a setting  
**When** I change a value  
**Then** validation ensures the value is valid  
**And** I see preview of impact: "This will affect X users"  
**And** changes require confirmation

**Given** settings are changed  
**When** I save  
**Then** changes apply immediately (or after restart if noted)  
**And** the change is logged with: old value, new value, changedBy

**Given** I make a mistake  
**When** I need to revert  
**Then** I can view change history  
**And** revert to any previous value

**Given** deployment-specific settings exist  
**When** I check environment config  
**Then** I see: database URL, log level, feature flags  
**And** sensitive values are masked

---

**Epic 12 Summary:**
- ✅ 6 stories, 34 points
- ✅ 2-3 week timeline
- ✅ Complete operational visibility and control
- ✅ Expert panel approved (Winston: Architecture ✅ | Mary: Business ✅ | Amelia: Feasibility ✅ | Murat: Testability ✅)

---

### Epic 13: Integrations & Webhooks

**Epic Goal:** Enable bmadServer to integrate with external systems through webhooks and notifications, allowing workflow events to trigger actions in other tools.

**Requirements Covered:** FR35-FR36 (webhooks, integrations), NFR12-NFR13 (webhook reliability, ordering)

**Duration:** 1.5-2 weeks | **Stories:** 5 | **Total Points:** 26

---

#### Story 13.1: Webhook Configuration

**Story ID:** E13-S1  
**Points:** 5  

As an administrator, I want to configure webhooks, so that external systems can receive workflow events.

**Acceptance Criteria:**

**Given** I access webhook configuration  
**When** I create a new webhook  
**Then** I can specify: name, URL, events (list), secret (for signature), active (boolean)

**Given** I select events  
**When** I choose which events to send  
**Then** available events include: workflow.started, workflow.completed, decision.made, step.completed, error.occurred

**Given** I save the webhook  
**When** the configuration is stored  
**Then** a test event is sent to verify the endpoint  
**And** I see: "Webhook verified" or error details

**Given** webhooks are configured  
**When** I view the webhook list  
**Then** I see: name, URL (truncated), events, status, last triggered, success rate

**Given** I edit a webhook  
**When** I change settings  
**Then** changes take effect immediately  
**And** the edit is logged

---

#### Story 13.2: Webhook Event Delivery

**Story ID:** E13-S2  
**Points:** 8  

As an external system, I want to receive webhook events reliably, so that I can react to bmadServer activity.

**Acceptance Criteria:**

**Given** a subscribed event occurs  
**When** the event triggers  
**Then** a webhook payload is sent to all matching webhooks  
**And** the payload includes: eventType, timestamp, workflowId, data, signature

**Given** the payload has a signature  
**When** I verify the signature  
**Then** HMAC-SHA256 with the webhook secret validates the payload  
**And** replay attacks are prevented with timestamp validation

**Given** a webhook delivery fails  
**When** the endpoint returns non-2xx or times out  
**Then** the system retries with exponential backoff: 1min, 5min, 30min, 2hr, 12hr, 24hr

**Given** retries are exhausted  
**When** all attempts fail  
**Then** the webhook is marked as failed  
**And** an alert is sent to administrators  
**And** the webhook can be manually retried

**Given** I check delivery history  
**When** I view webhook logs  
**Then** I see: each attempt with timestamp, status code, response time, response body (truncated)

---

#### Story 13.3: Event Ordering & Idempotency

**Story ID:** E13-S3  
**Points:** 5  

As an external system, I want events delivered in order with idempotency support, so that I can process them correctly.

**Acceptance Criteria:**

**Given** multiple events occur for a workflow  
**When** webhooks are sent  
**Then** events for the same workflow are delivered in order (NFR13)  
**And** sequence numbers are included in payload

**Given** an event payload is received  
**When** I check the payload structure  
**Then** it includes: eventId (UUID), sequenceNumber, workflowId, previousEventId

**Given** I receive a duplicate event  
**When** I check the eventId  
**Then** I can detect the duplicate and skip processing (idempotency)

**Given** events are queued  
**When** order must be preserved  
**Then** a per-workflow queue ensures FIFO delivery  
**And** concurrent webhook calls for different workflows are allowed

**Given** I miss events due to downtime  
**When** my system comes back online  
**Then** I can fetch missed events via GET `/api/v1/webhooks/events?since={lastEventId}`

---

#### Story 13.4: Notification Integrations

**Story ID:** E13-S4  
**Points:** 5  

As a user (Sarah), I want to receive notifications via external tools, so that I stay informed about workflow updates.

**Acceptance Criteria:**

**Given** I access notification settings  
**When** I view integration options  
**Then** I see available integrations: Email, Slack (webhook), Microsoft Teams (webhook)

**Given** I configure email notifications  
**When** I enable email for specific events  
**Then** emails are sent when those events occur  
**And** emails include: event summary, link to workflow, action buttons

**Given** I configure Slack integration  
**When** I provide a Slack webhook URL  
**Then** notifications are sent to that channel  
**And** messages include: event details, workflow link, formatted for Slack

**Given** I configure Teams integration  
**When** I provide a Teams webhook URL  
**Then** notifications are sent as adaptive cards  
**And** cards include action buttons for quick responses

**Given** I want to test notifications  
**When** I click "Send Test"  
**Then** a test notification is sent  
**And** I see confirmation or error details

---

#### Story 13.5: Webhook Management API

**Story ID:** E13-S5  
**Points:** 3  

As a developer, I want to manage webhooks via API, so that I can automate webhook configuration.

**Acceptance Criteria:**

**Given** I call POST `/api/v1/webhooks`  
**When** I provide valid webhook configuration  
**Then** the webhook is created  
**And** I receive the webhook ID and secret

**Given** I call GET `/api/v1/webhooks`  
**When** I'm authenticated as Admin  
**Then** I receive a list of all webhooks  
**And** secrets are not included in the response

**Given** I call PUT `/api/v1/webhooks/{id}`  
**When** I update webhook configuration  
**Then** the webhook is updated  
**And** secret can be regenerated if requested

**Given** I call DELETE `/api/v1/webhooks/{id}`  
**When** I delete a webhook  
**Then** the webhook is deactivated (soft delete)  
**And** historical delivery logs are retained

**Given** I call GET `/api/v1/webhooks/{id}/deliveries`  
**When** I query delivery history  
**Then** I see recent deliveries with status and timing

---

**Epic 13 Summary:**
- ✅ 5 stories, 26 points
- ✅ 1.5-2 week timeline
- ✅ Enables ecosystem integration
- ✅ Expert panel approved (Winston: Architecture ✅ | Mary: Business ✅ | Amelia: Feasibility ✅ | Murat: Testability ✅)

---

## Summary

### Epic Overview

| Epic | Name | Stories | Points | Duration |
|------|------|---------|--------|----------|
| 1 | Aspire Foundation & Project Setup | 6 | 32 | 1.5-2 weeks |
| 2 | User Authentication & Session Management | 6 | 34 | 2-3 weeks |
| 3 | Real-Time Chat Interface | 6 | 34 | 2-3 weeks |
| 4 | Workflow Orchestration Engine | 7 | 42 | 3-4 weeks |
| 5 | Multi-Agent Collaboration | 5 | 29 | 2-3 weeks |
| 6 | Decision Management & Locking | 5 | 26 | 2 weeks |
| 7 | Collaboration & Multi-User Support | 5 | 31 | 2-3 weeks |
| 8 | Persona Translation & Language Adaptation | 5 | 26 | 2 weeks |
| 9 | Data Persistence & State Management | 6 | 34 | 2-3 weeks |
| 10 | Error Handling & Recovery | 5 | 26 | 2 weeks |
| 11 | Security & Access Control | 5 | 26 | 2 weeks |
| 12 | Admin Dashboard & Operations | 6 | 34 | 2-3 weeks |
| 13 | Integrations & Webhooks | 5 | 26 | 1.5-2 weeks |
| **TOTAL** | | **72** | **400** | **~8 weeks** |

### Recommended Implementation Order

**Phase 1 (Foundation):** Epic 1, Epic 2, Epic 9
**Phase 2 (Core Features):** Epic 3, Epic 4, Epic 5
**Phase 3 (Advanced Features):** Epic 6, Epic 7, Epic 8
**Phase 4 (Operations & Security):** Epic 10, Epic 11, Epic 12, Epic 13

### All Requirements Covered

- ✅ 36 Functional Requirements (FR1-FR36)
- ✅ 15 Non-Functional Requirements (NFR1-NFR15)
- ✅ Architecture requirements from architecture.md
- ✅ UX requirements from ux-design-specification.md
- ✅ All epics reviewed by expert panel (Winston, Mary, Amelia, Murat)
