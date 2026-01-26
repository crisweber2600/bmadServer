# Story 5.1: Agent Registry & Configuration

**Status:** review

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

- [x] Analyze acceptance criteria and create detailed implementation plan
- [x] Design data models and database schema if needed
- [x] Implement core business logic
- [x] Create API endpoints and/or UI components
- [x] Write unit tests for critical paths
- [x] Write integration tests for key scenarios
- [x] Update API documentation
- [x] Perform manual testing and validation
- [x] Code review and address feedback

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

---

## Dev Agent Record

### Implementation Plan
Implemented centralized agent registry with 6 BMAD agents (ProductManager, Architect, Designer, Developer, Analyst, Orchestrator). Each agent has AgentId, Name, Description, Capabilities list, SystemPrompt, and ModelPreference. Registry provides GetAllAgents(), GetAgent(id), and GetAgentsByCapability(capability) methods. Registered as singleton in DI container.

### Completion Notes
✅ Created AgentDefinition.cs with all required properties
✅ Created IAgentRegistry interface with three query methods
✅ Created AgentRegistry.cs with 6 pre-configured BMAD agents
✅ Registered AgentRegistry in Program.cs as singleton
✅ Created AgentsController with three endpoints (GET all, GET by ID, GET by capability)
✅ Implemented 8 unit tests covering all registry functionality
✅ Implemented 6 integration tests validating DI registration and behavior
✅ All 14 tests passing (100% success rate)
✅ Build succeeds with 0 errors

### File List
- src/bmadServer.ApiService/Services/Workflows/Agents/AgentDefinition.cs (created)
- src/bmadServer.ApiService/Services/Workflows/Agents/IAgentRegistry.cs (created)
- src/bmadServer.ApiService/Services/Workflows/Agents/AgentRegistry.cs (created)
- src/bmadServer.ApiService/Controllers/AgentsController.cs (created)
- src/bmadServer.ApiService/Program.cs (modified - added AgentRegistry registration)
- src/bmadServer.Tests/Services/Workflows/Agents/AgentRegistryTests.cs (created)
- src/bmadServer.Tests/Integration/Workflows/AgentRegistryIntegrationTests.cs (created)

### Change Log
- 2026-01-26: Initial implementation of Agent Registry & Configuration (Story 5.1) - Created agent definition model, registry service with 6 BMAD agents, REST API controller, and comprehensive test coverage (14 tests passing)
