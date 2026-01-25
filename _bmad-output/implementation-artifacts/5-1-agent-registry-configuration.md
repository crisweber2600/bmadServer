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

### Created Files
- `src/bmadServer.ApiService/Agents/AgentDefinition.cs` - Core agent definition model with all required properties
- `src/bmadServer.ApiService/Agents/IAgentRegistry.cs` - Interface for agent registry
- `src/bmadServer.ApiService/Agents/AgentRegistry.cs` - Implementation with all 6 BMAD agents
- `src/bmadServer.ApiService/Controllers/AgentsController.cs` - REST API endpoints for agent registry
- `src/bmadServer.BDD.Tests/Features/AgentRegistryConfiguration.feature` - BDD acceptance tests (7 scenarios)
- `src/bmadServer.BDD.Tests/StepDefinitions/AgentRegistryConfigurationSteps.cs` - BDD step definitions
- `src/bmadServer.Tests/Unit/AgentDefinitionTests.cs` - Unit tests for AgentDefinition (5 tests)
- `src/bmadServer.Tests/Unit/AgentRegistryTests.cs` - Unit tests for AgentRegistry (18 tests)
- `src/bmadServer.ApiService.IntegrationTests/AgentRegistryTests.cs` - Integration tests for API endpoints (8 tests)

### Modified Files
- `src/bmadServer.ApiService/Program.cs` - Registered IAgentRegistry as singleton service


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

## Dev Agent Record

### Implementation Plan

**Approach:** Implemented agent registry following in-process DI pattern for MVP. No database persistence needed as agent definitions are static configuration.

**Architecture Decisions:**
1. AgentDefinition as immutable record with `required` properties and `init` accessors
2. AgentRegistry as singleton service (agent definitions don't change at runtime)
3. In-memory registry with 6 BMAD agents pre-configured
4. REST API endpoints for querying agents (GET /api/v1/agents)
5. Case-insensitive lookups for robustness

**BMAD Agents Configured:**
- Product Manager: PRD creation, requirements gathering, backlog prioritization
- Architect: Architecture design, technical decisions, data modeling
- Designer: UI/UX design, wireframes, component design
- Developer: Feature implementation, testing, bug fixes
- Analyst: Data analysis, reporting, metrics validation
- Orchestrator: Workflow coordination, agent handoffs, task routing

### Testing Strategy

**Red-Green-Refactor Cycle:**
1. RED: Created failing BDD tests, step definitions, and unit tests
2. GREEN: Implemented AgentDefinition, AgentRegistry, and AgentsController
3. REFACTOR: Added interface, DI registration, proper logging

**Test Coverage:**
- 7 BDD scenarios covering all acceptance criteria
- 23 unit tests (AgentDefinition + AgentRegistry)
- 8 integration tests for API endpoints
- Total: 38 tests, 100% pass rate

### Completion Notes

✅ All acceptance criteria met:
- AgentDefinition includes: AgentId, Name, Description, Capabilities, SystemPrompt, ModelPreference
- AgentRegistry provides: GetAllAgents(), GetAgent(id), GetAgentsByCapability(capability)
- Registry populated with all 6 BMAD agents
- Capabilities map to workflow steps (kebab-case format)
- Model preferences configured for cost/quality routing

✅ All tests passing:
- BDD Tests: 7/7 passed
- Unit Tests: 23/23 passed
- Integration Tests: 8/8 passed

## Change Log

- 2026-01-25: Initial implementation of Agent Registry & Configuration (Story 5-1)
  - Created AgentDefinition model with all required properties
  - Implemented AgentRegistry with query methods
  - Registered AgentRegistry as singleton in DI container
  - Added REST API endpoints at /api/v1/agents
  - Created comprehensive test suite (38 tests)
  - All acceptance criteria satisfied
