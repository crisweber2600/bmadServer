# Story 11.1: Per-User Rate Limiting

**Status:** ready-for-dev

## Story

As an operator, I want per-user rate limiting, so that no single user can overwhelm the system.

## Acceptance Criteria

**Given** rate limiting is configured  
**When** I check appsettings.json  
**Then** I see: RateLimiting: { RequestsPerMinute: 60, BurstLimit: 10, WindowSeconds: 60 }

**Given** a user makes API requests  
**When** they exceed RequestsPerMinute  
**Then** subsequent requests return 429 Too Many Requests  
**And** the response includes Retry-After header

**Given** rate limits are per-user  
**When** User A is rate limited  
**Then** User B's requests are unaffected  
**And** limits are tracked by userId from JWT

**Given** burst traffic occurs  
**When** a user sends 10 requests in 1 second (within BurstLimit)  
**Then** all requests are allowed  
**And** they count toward the per-minute limit

**Given** rate limiting is in effect  
**When** I check metrics  
**Then** I see: rate_limit_hits_total per user, current request counts

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

- Source: [epics.md - Story 11.1](../planning-artifacts/epics.md)
- Architecture: [architecture.md](../planning-artifacts/architecture.md)
- PRD: [prd.md](../planning-artifacts/prd.md)
