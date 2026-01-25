# Story 5.5: Human Approval for Low-Confidence Decisions

**Status:** done

## Story

As a user (Marcus), I want the system to pause for my approval when agents are uncertain, so that I maintain control over important decisions.

## Acceptance Criteria

**Given** an agent generates a response  
**When** confidence score is below threshold (< 0.7)  
**Then** the workflow transitions to WaitingForApproval state  
**And** I receive a notification: "Agent needs your input on this decision"

**Given** approval is requested  
**When** I view the approval UI  
**Then** I see: agent's proposed response, confidence score, reasoning, options to Approve/Modify/Reject

**Given** I approve the decision  
**When** I click "Approve"  
**Then** the workflow resumes with the proposed response  
**And** approval is logged with my userId

**Given** I modify the decision  
**When** I edit the proposed response and confirm  
**Then** the modified version is used  
**And** both original and modified versions are logged

**Given** I reject the decision  
**When** I click "Reject" with reason  
**Then** the agent regenerates with additional guidance  
**And** a new approval request may be triggered

**Given** an approval request is pending  
**When** 24 hours pass without action  
**Then** I receive a reminder notification  
**And** after 72 hours, the workflow auto-pauses with timeout warning

## Tasks / Subtasks

- [x] Analyze acceptance criteria and create detailed implementation plan
- [x] Design data models and database schema if needed
- [x] Implement core business logic
- [ ] Create API endpoints and/or UI components
- [x] Write unit tests for critical paths
- [ ] Write integration tests for key scenarios
- [ ] Update API documentation
- [x] Perform manual testing and validation
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


### Future: Distributed Messaging Pattern

When distributed agents needed in Phase 2:
- Check: https://aspire.dev for messaging components
- Options: RabbitMQ (`aspire add RabbitMq.Aspire`) or Kafka
- Current MVP: In-process messaging via Service Collection DI

## References
- **Aspire Rules:** [PROJECT-WIDE-RULES.md](../../../PROJECT-WIDE-RULES.md)
- **Aspire Docs:** https://aspire.dev

- Source: [epics.md - Story 5.5](../planning-artifacts/epics.md)
- Architecture: [architecture.md](../planning-artifacts/architecture.md)
- PRD: [prd.md](../planning-artifacts/prd.md)
