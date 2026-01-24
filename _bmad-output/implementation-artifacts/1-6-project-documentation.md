# Story 1.6: Document Project Setup and Deployment Instructions

**Status:** ready-for-dev

## Story

As a new team member,
I want clear setup and deployment documentation,
so that I can get a working development environment.

## Acceptance Criteria

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

## Tasks / Subtasks

- [ ] **Task 1: Create SETUP.md** (AC: #1-2)
  - [ ] Add Prerequisites section
  - [ ] Add Quick Start instructions
  - [ ] Add Project Structure explanation
  - [ ] Add Development Workflow guide
  - [ ] Add Deployment instructions
  - [ ] Add Monitoring URLs
  - [ ] Add Troubleshooting section
  - [ ] Test instructions work end-to-end

- [ ] **Task 2: Create ARCHITECTURE.md** (AC: #3)
  - [ ] Add component diagram (ASCII or Mermaid)
  - [ ] Document data flow
  - [ ] Explain deployment architecture
  - [ ] Document technology choices with rationale
  - [ ] Add links to detailed docs

- [ ] **Task 3: Update README.md** (AC: #4)
  - [ ] Add project description
  - [ ] Add badges (CI status, license)
  - [ ] Add Quick Start link
  - [ ] Add Architecture link
  - [ ] Add Contributing guidelines
  - [ ] Add License section
  - [ ] Add Support links

- [ ] **Task 4: Validate documentation** (AC: #5)
  - [ ] Fresh clone test: follow SETUP.md exactly
  - [ ] Verify < 10 minute setup time
  - [ ] Test all documented commands work
  - [ ] Have another developer review
  - [ ] Fix any unclear sections

## Dev Notes

### README.md Template

```markdown
# bmadServer

Multi-tenant BMAD workflow orchestration platform enabling collaborative product development through AI-assisted workflows.

[![CI](https://github.com/yourorg/bmadServer/actions/workflows/ci.yml/badge.svg)](https://github.com/yourorg/bmadServer/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

## Features

- 🚀 Real-time BMAD workflow execution
- 👥 Multi-user collaboration with conflict resolution
- 🤖 Multi-agent orchestration with handoffs
- 📊 Decision tracking with version history
- 🔐 Secure authentication with RBAC

## Quick Start

See [SETUP.md](SETUP.md) for detailed instructions.

```bash
git clone https://github.com/yourorg/bmadServer.git
cd bmadServer
docker-compose up
```

Open http://localhost:3000/health to verify.

## Documentation

- [Setup Guide](SETUP.md)
- [Architecture Overview](ARCHITECTURE.md)
- [API Documentation](http://localhost:3000/swagger)

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Submit a pull request

## License

MIT License - see [LICENSE](LICENSE) for details.
```

### ARCHITECTURE.md Component Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                      bmadServer                              │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌─────────────────┐    ┌─────────────────┐                │
│  │  React Client   │───▶│   API Service   │                │
│  │  (port 5173)    │    │   (port 3000)   │                │
│  └─────────────────┘    └────────┬────────┘                │
│                                  │                          │
│         ┌────────────────────────┼────────────────────┐    │
│         ▼                        ▼                    ▼    │
│  ┌─────────────┐         ┌─────────────┐      ┌──────────┐│
│  │  SignalR    │         │ PostgreSQL  │      │Prometheus││
│  │  WebSocket  │         │  (port 5432)│      │(port 9090)││
│  └─────────────┘         └─────────────┘      └──────────┘│
│                                                     │      │
│                                              ┌──────▼─────┐│
│                                              │  Grafana   ││
│                                              │(port 3001) ││
│                                              └────────────┘│
└─────────────────────────────────────────────────────────────┘
```

### Architecture Alignment

Per architecture.md requirements:
- Documentation must explain how to set up locally ✅
- Documentation must explain deployment process ✅
- Component relationships documented ✅

### Dependencies

- **Depends on**: Stories 1-1 through 1-5 (all infrastructure)
- **Enables**: Onboarding new team members, open source contribution

### References

- [Epic 1 Story 1.6](../../planning-artifacts/epics.md#story-16-document-project-setup-and-deployment-instructions)

## Dev Agent Record

### Agent Model Used

Claude 3.5 Sonnet

### Completion Notes List

- Story created with full acceptance criteria
- README and ARCHITECTURE templates included
- Documentation validation checklist included

### File List

- /Users/cris/bmadServer/SETUP.md (create)
- /Users/cris/bmadServer/ARCHITECTURE.md (create)
- /Users/cris/bmadServer/README.md (modify)
- /Users/cris/bmadServer/LICENSE (create if not exists)
