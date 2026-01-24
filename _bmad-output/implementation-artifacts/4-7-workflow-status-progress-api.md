# Story 4.7: Workflow Status & Progress API

**Status:** ready-for-dev

## Story

As a user (Marcus), I want to view detailed workflow status and progress, so that I know exactly where I am in the process.

## Acceptance Criteria

**Given** I have an active workflow  
**When** I send GET `/api/v1/workflows/{id}`  
**Then** I receive comprehensive status including: id, name, status, currentStep, totalSteps, percentComplete, startedAt, estimatedCompletion

**Given** I query workflow status  
**When** the response includes step details  
**Then** each step shows: stepId, name, status (Pending/Current/Completed/Skipped), completedAt, agent name

**Given** I want real-time status updates  
**When** I am connected via SignalR  
**Then** I receive WORKFLOW_STATUS_CHANGED events whenever status or step changes

**Given** I query all my workflows  
**When** I send GET `/api/v1/workflows?status=running`  
**Then** I receive a paginated list of my workflows matching the filter  
**And** I can filter by: status, workflowType, createdAfter, createdBefore

**Given** I view workflow progress  
**When** the UI renders  
**Then** I see a visual progress indicator (stepper component) showing completed, current, and upcoming steps

**Given** workflow completion time is estimated  
**When** I check estimatedCompletion  
**Then** the estimate is based on: average step duration, remaining steps, historical data for this workflow type

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

- Source: [epics.md - Story 4.7](../planning-artifacts/epics.md)
- Architecture: [architecture.md](../planning-artifacts/architecture.md)
- PRD: [prd.md](../planning-artifacts/prd.md)
