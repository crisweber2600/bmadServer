# Story 5.2: Agent-to-Agent Messaging

**Status:** done

## Story

As an agent (Architect), I want to request information from other agents, so that I can gather inputs needed for my work.

## Acceptance Criteria

**Given** an agent is processing a step  
**When** it needs input from another agent  
**Then** it can call AgentMessaging.RequestFromAgent(targetAgentId, request, context)

**Given** an agent request is made  
**When** the target agent receives it  
**Then** the request includes: sourceAgentId, requestType, payload, workflowContext, conversationHistory

**Given** the target agent processes the request  
**When** a response is generated  
**Then** the response is returned to the source agent  
**And** the exchange is logged for transparency

**Given** agent-to-agent communication occurs  
**When** I check the message format  
**Then** I see: messageId, timestamp, sourceAgent, targetAgent, messageType, content, workflowInstanceId

**Given** an agent request times out (> 30 seconds)  
**When** no response is received  
**Then** the system retries once  
**And** if still no response, returns error to source agent  
**And** the timeout is logged for debugging

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

- Source: [epics.md - Story 5.2](../planning-artifacts/epics.md)
- Architecture: [architecture.md](../planning-artifacts/architecture.md)
- PRD: [prd.md](../planning-artifacts/prd.md)
