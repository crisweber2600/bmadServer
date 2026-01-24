# Story 7.3: Input Attribution & History

**Status:** ready-for-dev

## Story

As a user (Sarah), I want to see who provided each input and when, so that I can track contributions and understand decisions.

## Acceptance Criteria

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

- Source: [epics.md - Story 7.3](../planning-artifacts/epics.md)
- Architecture: [architecture.md](../planning-artifacts/architecture.md)
- PRD: [prd.md](../planning-artifacts/prd.md)
