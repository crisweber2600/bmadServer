# Story 12.1: System Health Dashboard

**Status:** ready-for-dev

## Story

As an administrator (Cris), I want a system health dashboard, so that I can monitor bmadServer status at a glance.

## Acceptance Criteria

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

## Tasks / Subtasks

- [ ] Analyze acceptance criteria and create detailed implementation plan
- [ ] Design data models and database schema if needed
- [ ] Implement core business logic
- [ ] Create API endpoints and/or UI components
- [ ] Write unit tests for critical paths
- [ ] Write integration tests for key scenarios
- [ ] Update API documentation
- [ ] Perform manual testing and validation
- [ ] Code review and address feedback

## Dev Notes

### Implementation Guidance

This story should be implemented following the patterns established in the codebase:
- Follow the architecture patterns defined in `architecture.md`
- Use existing service patterns and dependency injection
- Ensure proper error handling and logging
- Add appropriate authorization checks based on user roles
- Follow the coding standards and conventions of the project

### Testing Strategy

- Unit tests should cover business logic and edge cases
- Integration tests should verify API endpoints and database interactions
- Consider performance implications for database queries
- Test error scenarios and validation rules

### Dependencies

Review the acceptance criteria for dependencies on:
- Other stories or epics that must be completed first
- External packages or services that need to be configured
- Database migrations that need to be created

## Files to Create/Modify

Files will be determined during implementation based on:
- Data models and entities needed
- API endpoints required
- Service layer components
- Database migrations
- Test files


---

## Aspire Development Standards

### PostgreSQL Connection Pattern

This story uses PostgreSQL configured in Story 1.2 via Aspire:
- Connection string automatically injected from Aspire AppHost
- Pattern: `builder.AddServiceDefaults();` (inherits PostgreSQL reference)
- See Story 1.2 for AppHost configuration pattern

### Project-Wide Standards

This story follows the Aspire-first development pattern:
- **Reference:** [PROJECT-WIDE-RULES.md](../../../PROJECT-WIDE-RULES.md)
- **Primary Documentation:** https://aspire.dev
- **GitHub:** https://github.com/microsoft/aspire

---


### Aspire Dashboard Integration

Admin monitoring leverages Aspire Dashboard:
- Health checks visible at https://localhost:17360
- Traces and metrics via OpenTelemetry
- Supersedes Prometheus/Grafana for MVP (Story 1.5 cancelled)

## References
- **Aspire Rules:** [PROJECT-WIDE-RULES.md](../../../PROJECT-WIDE-RULES.md)
- **Aspire Docs:** https://aspire.dev

- Source: [epics.md - Story 12.1](../planning-artifacts/epics.md)
- Architecture: [architecture.md](../planning-artifacts/architecture.md)
- PRD: [prd.md](../planning-artifacts/prd.md)
