# Story 4.1: Workflow Definition & Registry

**Status:** ready-for-dev

## Story

As a developer, I want a workflow registry that defines all supported BMAD workflows, so that the system knows which workflows are available and their step sequences.

## Acceptance Criteria

**Given** I need to define BMAD workflows  
**When** I create `Workflows/WorkflowDefinition.cs`  
**Then** the class includes: WorkflowId, Name, Description, Steps (ordered list), RequiredRoles, EstimatedDuration

**Given** workflow definitions exist  
**When** I create `Workflows/WorkflowRegistry.cs`  
**Then** it provides methods: GetAllWorkflows(), GetWorkflow(id), ValidateWorkflow(id)  
**And** workflows are registered at startup via dependency injection

**Given** the registry is populated  
**When** I query GetAllWorkflows()  
**Then** I receive all BMAD workflows: create-prd, create-architecture, create-stories, design-ux, and others from BMAD spec

**Given** each workflow has steps  
**When** I examine a workflow definition  
**Then** each step includes: StepId, Name, AgentId, InputSchema, OutputSchema, IsOptional, CanSkip

**Given** I request a non-existent workflow  
**When** I call GetWorkflow("invalid-id")  
**Then** the system returns null or throws WorkflowNotFoundException

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

## References
- **Aspire Rules:** [PROJECT-WIDE-RULES.md](../../../PROJECT-WIDE-RULES.md)
- **Aspire Docs:** https://aspire.dev

- Source: [epics.md - Story 4.1](../planning-artifacts/epics.md)
- Architecture: [architecture.md](../planning-artifacts/architecture.md)
- PRD: [prd.md](../planning-artifacts/prd.md)
