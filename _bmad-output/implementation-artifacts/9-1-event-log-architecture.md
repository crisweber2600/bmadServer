# Story 9.1: Event Log Architecture

**Status:** ready-for-dev

## Story

As a developer, I want all workflow events logged immutably, so that we have a complete audit trail.

## Acceptance Criteria

**Given** any workflow action occurs  
**When** the action completes  
**Then** an event is appended to the WorkflowEvents table with: id, workflowInstanceId, eventType, payload, userId, timestamp, correlationId

**Given** the event log schema exists  
**When** I examine the table  
**Then** it uses append-only semantics (no UPDATE/DELETE in application code)  
**And** partitioning is configured by month for performance

**Given** events are logged  
**When** I query by workflowInstanceId  
**Then** I can reconstruct the complete workflow history in order

**Given** event types are defined  
**When** I check the enum  
**Then** I see: WorkflowStarted, StepCompleted, DecisionMade, UserInput, AgentResponse, StateChanged, Error, etc.

**Given** I need to replay events  
**When** I call EventStore.Replay(workflowId, fromSequence)  
**Then** events are returned in sequence order  
**And** I can rebuild state from any point in history

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

## References

- Source: [epics.md - Story 9.1](../planning-artifacts/epics.md)
- Architecture: [architecture.md](../planning-artifacts/architecture.md)
- PRD: [prd.md](../planning-artifacts/prd.md)
