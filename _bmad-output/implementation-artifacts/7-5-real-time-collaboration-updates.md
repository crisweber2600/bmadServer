# Story 7.5: Real-Time Collaboration Updates

**Status:** ready-for-dev

## Story

As a user (Sarah), I want to see changes from other users in real-time, so that I stay in sync with my team.

## Acceptance Criteria

**Given** multiple users are in a workflow  
**When** one user sends a message  
**Then** all other connected users see the message within 500ms via SignalR

**Given** a user makes a decision  
**When** the decision is confirmed  
**Then** all users receive a DECISION_MADE event  
**And** their UIs update to reflect the decision

**Given** a workflow step advances  
**When** the step changes  
**Then** all users receive a STEP_CHANGED event  
**And** everyone sees the new current step

**Given** a user goes offline  
**When** their connection drops  
**Then** other users see their presence indicator change  
**And** when they reconnect, they receive all missed updates

**Given** high-frequency updates occur  
**When** many users are active  
**Then** updates are batched (50ms window) to prevent UI thrashing  
**And** the system remains responsive under load

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


### SignalR Integration Pattern

This story uses SignalR configured in Story 3.1:
- **MVP:** Built-in ASP.NET Core SignalR
- Real-time collaboration updates via SignalR hub
- See Story 3.1 for SignalR configuration pattern

## References
- **Aspire Rules:** [PROJECT-WIDE-RULES.md](../../../PROJECT-WIDE-RULES.md)
- **Aspire Docs:** https://aspire.dev

- Source: [epics.md - Story 7.5](../planning-artifacts/epics.md)
- Architecture: [architecture.md](../planning-artifacts/architecture.md)
- PRD: [prd.md](../planning-artifacts/prd.md)
