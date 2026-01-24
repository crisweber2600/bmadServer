# Story 6.4: Decision Review Workflow

**Status:** ready-for-dev

## Story

As a user (Marcus), I want to request a review before locking a decision, so that I can get approval from stakeholders.

## Acceptance Criteria

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

- Source: [epics.md - Story 6.4](../planning-artifacts/epics.md)
- Architecture: [architecture.md](../planning-artifacts/architecture.md)
- PRD: [prd.md](../planning-artifacts/prd.md)
