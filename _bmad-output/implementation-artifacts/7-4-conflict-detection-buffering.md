# Story 7.4: Conflict Detection & Buffering

**Status:** ready-for-dev

## Story

As a user (Marcus), I want conflicting inputs to be buffered and flagged, so that we can resolve disagreements properly.

## Acceptance Criteria

**Given** two users submit different inputs for the same field  
**When** both inputs arrive before checkpoint  
**Then** the system detects the conflict  
**And** both inputs are buffered (not applied)  
**And** users are notified: "Conflict detected - human arbitration required"

**Given** a conflict is detected  
**When** I view the conflict UI  
**Then** I see: both proposed values, who submitted each, timestamp, field context

**Given** I am a workflow owner or Admin  
**When** I resolve the conflict  
**Then** I can choose: Accept A, Accept B, Merge, Reject Both  
**And** the resolution is applied at next checkpoint

**Given** a conflict remains unresolved for 1 hour  
**When** the timeout occurs  
**Then** the workflow pauses  
**And** escalation notifications are sent to workflow owner  
**And** the workflow cannot proceed until resolved

**Given** conflicts are resolved  
**When** I query conflict history  
**Then** I see all conflicts with: inputs, resolution, resolver, timestamp, reason

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

- Source: [epics.md - Story 7.4](../planning-artifacts/epics.md)
- Architecture: [architecture.md](../planning-artifacts/architecture.md)
- PRD: [prd.md](../planning-artifacts/prd.md)
