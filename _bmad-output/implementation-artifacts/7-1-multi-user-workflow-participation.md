# Story 7.1: Multi-User Workflow Participation

**Status:** ready-for-dev

## Story

As a user (Sarah), I want to invite team members to my workflow, so that we can collaborate on product development together.

## Acceptance Criteria

**Given** I own a workflow  
**When** I send POST `/api/v1/workflows/{id}/participants` with userId and role (Contributor/Observer)  
**Then** the user is added to the workflow  
**And** they receive an invitation notification  
**And** they appear in the participants list

**Given** a user is added as Contributor  
**When** they access the workflow  
**Then** they can send messages, make decisions, and advance steps  
**And** their actions are attributed to them

**Given** a user is added as Observer  
**When** they access the workflow  
**Then** they can view messages and decisions  
**And** they cannot make changes or send messages  
**And** the UI shows read-only mode

**Given** multiple users are connected  
**When** I view the workflow  
**Then** I see presence indicators showing who is online  
**And** I see typing indicators when others are composing messages

**Given** I want to remove a participant  
**When** I send DELETE `/api/v1/workflows/{id}/participants/{userId}`  
**Then** the user loses access immediately  
**And** they receive a notification  
**And** their future access attempts are denied

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

- Source: [epics.md - Story 7.1](../planning-artifacts/epics.md)
- Architecture: [architecture.md](../planning-artifacts/architecture.md)
- PRD: [prd.md](../planning-artifacts/prd.md)
