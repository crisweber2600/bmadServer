# Story 3.6: Mobile-Responsive Chat Interface

**Status:** ready-for-dev

## Story

As a user (Sarah) on mobile, I want the chat interface to work seamlessly on my phone, so that I can approve decisions and monitor workflows on the go.

## Acceptance Criteria

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

- Source: [epics.md - Story 3.6](../planning-artifacts/epics.md)
- Architecture: [architecture.md](../planning-artifacts/architecture.md)
- PRD: [prd.md](../planning-artifacts/prd.md)
