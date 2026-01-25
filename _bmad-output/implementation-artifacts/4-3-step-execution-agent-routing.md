# Story 4.3: Step Execution & Agent Routing

**Status:** ready-for-dev

## Story

As a user (Marcus), I want workflow steps to automatically route to the correct agent, so that each step is handled by the appropriate specialist.

## Acceptance Criteria

**Given** a workflow is running  
**When** the current step requires an agent  
**Then** the system looks up the AgentId from the step definition  
**And** routes the request to the correct agent handler

**Given** step execution begins  
**When** the agent processes the step  
**Then** the agent receives: workflow context, step parameters, conversation history, user input

**Given** an agent completes a step  
**When** the response is received  
**Then** the step output is validated against OutputSchema  
**And** StepData is updated with the result  
**And** CurrentStep advances to the next step

**Given** step execution takes time  
**When** processing exceeds 5 seconds  
**Then** streaming begins to the client (NFR2)  
**And** the user sees real-time progress

**Given** a step fails  
**When** an error occurs during agent processing  
**Then** the workflow transitions to Failed state (if unrecoverable) or WaitingForInput (if retry possible)  
**And** the error is logged with full context

**Given** I need to track step history  
**When** I query the WorkflowStepHistory table  
**Then** I see all executed steps with: StepId, StartedAt, CompletedAt, Status, Input, Output

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

- Source: [epics.md - Story 4.3](../planning-artifacts/epics.md)
- Architecture: [architecture.md](../planning-artifacts/architecture.md)
- PRD: [prd.md](../planning-artifacts/prd.md)
