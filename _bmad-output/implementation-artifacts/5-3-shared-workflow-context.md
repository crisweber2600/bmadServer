# Story 5.3: Shared Workflow Context

**Status:** done

## Story

As an agent (Designer), I want access to the full workflow context, so that I can make decisions informed by previous steps.

## Acceptance Criteria

**Given** a workflow has multiple completed steps  
**When** an agent receives a request  
**Then** it has access to SharedContext containing: all step outputs, decision history, user preferences, artifact references

**Given** an agent needs specific prior output  
**When** it queries SharedContext.GetStepOutput(stepId)  
**Then** it receives the structured output from that step  
**And** null is returned if step hasn't completed

**Given** an agent produces output  
**When** the step completes  
**Then** the output is automatically added to SharedContext  
**And** subsequent agents can access it immediately

**Given** context size grows large  
**When** the context exceeds token limits  
**Then** the system summarizes older context while preserving key decisions  
**And** full context remains available in database for reference

**Given** concurrent agents access context  
**When** simultaneous reads/writes occur  
**Then** optimistic concurrency control prevents conflicts  
**And** version numbers track context changes

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

### Implementation Summary

**Created Files:**
- `src/bmadServer.ApiService/Workflow/SharedContext.cs` - Core domain model for shared workflow context with step outputs, decisions, user preferences, and artifact references
- `src/bmadServer.ApiService/Workflow/ISharedContextService.cs` - Service interface and implementation for managing shared context with database persistence
- `src/bmadServer.ApiService/Data/Entities/WorkflowContextEntity.cs` - Database entity for storing workflow context as JSONB
- `src/bmadServer.BDD.Tests/Features/SharedWorkflowContext.feature` - BDD feature file with 10 comprehensive scenarios
- `src/bmadServer.BDD.Tests/StepDefinitions/SharedWorkflowContextSteps.cs` - Step definitions for BDD tests
- `src/bmadServer.Tests/Unit/SharedContextTests.cs` - Unit tests for SharedContext domain model (18 tests)
- `src/bmadServer.Tests/Unit/SharedContextServiceTests.cs` - Unit tests for SharedContextService (13 tests)
- `src/bmadServer.ApiService/Migrations/20260125125714_AddWorkflowContextTable.cs` - Database migration for workflow_contexts table

**Modified Files:**
- `src/bmadServer.ApiService/Data/ApplicationDbContext.cs` - Added WorkflowContexts DbSet and entity configuration

**Key Features Implemented:**
- ✅ SharedContext with step outputs, decision history, user preferences, and artifact references
- ✅ GetStepOutput(stepId) method returning null for incomplete steps
- ✅ Auto-add outputs when steps complete with version increment
- ✅ Context summarization when exceeding 8000 token limit
- ✅ Optimistic concurrency control with version tracking
- ✅ Database persistence with JSONB column type for flexibility
- ✅ Comprehensive BDD and unit test coverage (31 tests)

**Test Results:**
- All 10 BDD scenarios passing
- All 31 unit tests passing
- Full integration with existing test suite (218 total tests passing)


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

- Source: [epics.md - Story 5.3](../planning-artifacts/epics.md)
- Architecture: [architecture.md](../planning-artifacts/architecture.md)
- PRD: [prd.md](../planning-artifacts/prd.md)
