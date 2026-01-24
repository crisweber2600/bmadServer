# Story 9.5: Checkpoint Restoration

**Status:** ready-for-dev

## Story

As a user (Sarah), I want to restore previous checkpoints, so that I can recover from mistakes or explore alternative paths.

## Acceptance Criteria

**Given** a workflow has checkpoints  
**When** I send GET `/api/v1/workflows/{id}/checkpoints`  
**Then** I see all checkpoints with: id, timestamp, stepId, description, canRestore

**Given** I want to restore a checkpoint  
**When** I send POST `/api/v1/workflows/{id}/checkpoints/{checkpointId}/restore`  
**Then** a new workflow branch is created from that checkpoint  
**And** the original workflow is preserved  
**And** I'm redirected to the new branch

**Given** I restore a checkpoint  
**When** the restoration completes  
**Then** workflow state matches the checkpoint exactly  
**And** subsequent events/decisions are cleared in the branch  
**And** I can proceed from that point

**Given** automatic checkpoints exist  
**When** I examine checkpoint frequency  
**Then** checkpoints are created at: each step completion, hourly during active sessions, before risky operations

**Given** checkpoint storage grows  
**When** checkpoints are older than 90 days  
**Then** they are archived (moved to cold storage)  
**And** restoration requires archive retrieval (may be slower)

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

- Source: [epics.md - Story 9.5](../planning-artifacts/epics.md)
- Architecture: [architecture.md](../planning-artifacts/architecture.md)
- PRD: [prd.md](../planning-artifacts/prd.md)
