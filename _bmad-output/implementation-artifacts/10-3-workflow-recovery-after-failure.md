# Story 10.3: Workflow Recovery After Failure

**Status:** ready-for-dev

## Story

As a user (Sarah), I want workflows to recover automatically after failures, so that I don't lose progress.

## Acceptance Criteria

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


### Future: Redis Caching Pattern

When caching layer needed in Phase 2:
- Command: `aspire add Redis.Distributed`
- Pattern: DI injection via IConnectionMultiplexer
- Also available: Redis backplane for SignalR scaling
- Reference: https://aspire.dev Redis integration

## References
- **Aspire Rules:** [PROJECT-WIDE-RULES.md](../../../PROJECT-WIDE-RULES.md)
- **Aspire Docs:** https://aspire.dev

- Source: [epics.md - Story 10.3](../planning-artifacts/epics.md)
- Architecture: [architecture.md](../planning-artifacts/architecture.md)
- PRD: [prd.md](../planning-artifacts/prd.md)
