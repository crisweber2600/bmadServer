# Story 6.2: Decision Version History

**Status:** ready-for-dev

## Story

As a user (Marcus), I want to see the history of changes to a decision, so that I can understand how it evolved.

## Acceptance Criteria

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

- Source: [epics.md - Story 6.2](../planning-artifacts/epics.md)
- Architecture: [architecture.md](../planning-artifacts/architecture.md)
- PRD: [prd.md](../planning-artifacts/prd.md)
