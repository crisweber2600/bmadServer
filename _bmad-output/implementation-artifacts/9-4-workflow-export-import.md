# Story 9.4: Workflow Export & Import

**Status:** ready-for-dev

## Story

As a user (Marcus), I want to export workflow artifacts and data, so that I can use them outside bmadServer.

## Acceptance Criteria

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

## References
- **Aspire Rules:** [PROJECT-WIDE-RULES.md](../../../PROJECT-WIDE-RULES.md)
- **Aspire Docs:** https://aspire.dev

- Source: [epics.md - Story 9.4](../planning-artifacts/epics.md)
- Architecture: [architecture.md](../planning-artifacts/architecture.md)
- PRD: [prd.md](../planning-artifacts/prd.md)
