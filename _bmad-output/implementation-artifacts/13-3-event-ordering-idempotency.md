# Story 13.3: Event Ordering & Idempotency

**Status:** ready-for-dev

## Story

As an external system, I want events delivered in order with idempotency support, so that I can process them correctly.

## Acceptance Criteria

**Given** multiple events occur for a workflow  
**When** webhooks are sent  
**Then** events for the same workflow are delivered in order (NFR13)  
**And** sequence numbers are included in payload

**Given** an event payload is received  
**When** I check the payload structure  
**Then** it includes: eventId (UUID), sequenceNumber, workflowId, previousEventId

**Given** I receive a duplicate event  
**When** I check the eventId  
**Then** I can detect the duplicate and skip processing (idempotency)

**Given** events are queued  
**When** order must be preserved  
**Then** a per-workflow queue ensures FIFO delivery  
**And** concurrent webhook calls for different workflows are allowed

**Given** I miss events due to downtime  
**When** my system comes back online  
**Then** I can fetch missed events via GET `/api/v1/webhooks/events?since={lastEventId}`

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


### Future: Webhook Queue Pattern

When webhook queue needed for high volume:
- Options: RabbitMQ (`aspire add RabbitMq.Aspire`) or Kafka
- Current MVP: In-process queue with database persistence
- See Story 1.2 for PostgreSQL persistence pattern

## References
- **Aspire Rules:** [PROJECT-WIDE-RULES.md](../../../PROJECT-WIDE-RULES.md)
- **Aspire Docs:** https://aspire.dev

- Source: [epics.md - Story 13.3](../planning-artifacts/epics.md)
- Architecture: [architecture.md](../planning-artifacts/architecture.md)
- PRD: [prd.md](../planning-artifacts/prd.md)
