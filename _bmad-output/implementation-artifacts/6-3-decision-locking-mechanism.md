# Story 6.3: Decision Locking Mechanism

**Status:** ready-for-dev

## Story

As a user (Sarah), I want to lock important decisions, so that they cannot be accidentally changed.

## Acceptance Criteria

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

## References

- Source: [epics.md - Story 6.3](../planning-artifacts/epics.md)
- Architecture: [architecture.md](../planning-artifacts/architecture.md)
- PRD: [prd.md](../planning-artifacts/prd.md)
