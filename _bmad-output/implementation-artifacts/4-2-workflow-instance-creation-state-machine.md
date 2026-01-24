# Story 4.2: Workflow Instance Creation & State Machine

**Status:** ready-for-dev

## Story

As a user (Marcus), I want to start a new workflow instance, so that I can begin a BMAD process like creating a PRD.

## Acceptance Criteria

**Given** I am authenticated with Participant role  
**When** I send POST `/api/v1/workflows` with workflowId and initial parameters  
**Then** the system creates a WorkflowInstance record with: Id, WorkflowDefinitionId, UserId, CurrentStep, Status (Created), CreatedAt

**Given** a workflow instance is created  
**When** I examine the state machine  
**Then** valid states include: Created, Running, Paused, WaitingForInput, WaitingForApproval, Completed, Failed, Cancelled

**Given** a workflow instance exists  
**When** state transitions occur  
**Then** only valid transitions are allowed (e.g., Created->Running, Running->Paused, not Created->Completed)  
**And** invalid transitions return 400 Bad Request with explanation

**Given** a workflow starts  
**When** the first step executes  
**Then** Status changes from Created to Running  
**And** CurrentStep is set to step 1  
**And** an event is logged to the WorkflowEvents table

**Given** I check the database schema  
**When** I run the migration for WorkflowInstances  
**Then** the table includes JSONB columns for StepData and Context with proper indexes

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

- Source: [epics.md - Story 4.2](../planning-artifacts/epics.md)
- Architecture: [architecture.md](../planning-artifacts/architecture.md)
- PRD: [prd.md](../planning-artifacts/prd.md)
