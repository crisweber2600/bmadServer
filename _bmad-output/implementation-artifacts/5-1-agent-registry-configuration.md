# Story 5.1: Agent Registry & Configuration

**Status:** ready-for-dev

## Story

As a developer, I want a centralized agent registry, so that the system knows all available agents and their capabilities.

## Acceptance Criteria

**Given** I need to define BMAD agents  
**When** I create `Agents/AgentDefinition.cs`  
**Then** it includes: AgentId, Name, Description, Capabilities (list), SystemPrompt, ModelPreference

**Given** agent definitions exist  
**When** I create `Agents/AgentRegistry.cs`  
**Then** it provides: GetAllAgents(), GetAgent(id), GetAgentsByCapability(capability)

**Given** the registry is populated  
**When** I query GetAllAgents()  
**Then** I receive BMAD agents: ProductManager, Architect, Designer, Developer, Analyst, Orchestrator

**Given** each agent has capabilities  
**When** I examine an agent definition  
**Then** capabilities map to workflow steps they can handle (e.g., Architect handles "create-architecture")

**Given** agents have model preferences  
**When** an agent is invoked  
**Then** the system routes to the preferred model (configurable for cost/quality tradeoffs)

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


### Future: Distributed Messaging Pattern

When distributed agents needed in Phase 2:
- Check: https://aspire.dev for messaging components
- Options: RabbitMQ (`aspire add RabbitMq.Aspire`) or Kafka
- Current MVP: In-process messaging via Service Collection DI

## References
- **Aspire Rules:** [PROJECT-WIDE-RULES.md](../../../PROJECT-WIDE-RULES.md)
- **Aspire Docs:** https://aspire.dev

- Source: [epics.md - Story 5.1](../planning-artifacts/epics.md)
- Architecture: [architecture.md](../planning-artifacts/architecture.md)
- PRD: [prd.md](../planning-artifacts/prd.md)
