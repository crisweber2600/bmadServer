# Story 3.2: Chat Message Component with Ant Design

**Status:** ready-for-dev

## Story

As a user (Sarah), I want to see chat messages in a clean, readable format, so that I can easily follow the conversation with BMAD agents.

## Acceptance Criteria

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

- Source: [epics.md - Story 3.2](../planning-artifacts/epics.md)
- Architecture: [architecture.md](../planning-artifacts/architecture.md)
- PRD: [prd.md](../planning-artifacts/prd.md)
