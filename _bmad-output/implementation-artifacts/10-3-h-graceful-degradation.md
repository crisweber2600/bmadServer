# Story 10.3-H: Add Graceful Degradation Under Load

Status: ready-for-dev

## Story

As an operator,
I want graceful degradation,
so that core functionality remains available under heavy load.

## Acceptance Criteria

**Given** concurrent users reach 80% of capacity (20/25)  
**When** a new workflow start is requested  
**Then** it is queued with estimated wait time  
**And** user sees: "High demand - your request is queued"

**Given** the system is under load  
**When** non-essential features are identified  
**Then** typing indicators and presence updates are disabled first  
**And** core workflow execution continues

## Tasks / Subtasks

- [ ] Add request queuing for workflow starts at capacity (AC: 1)
  - [ ] Create CapacityMiddleware
  - [ ] Track concurrent workflow executions
  - [ ] Queue requests when at 80% capacity
  - [ ] Calculate estimated wait time
- [ ] Implement feature flags for non-essential features (AC: 2)
  - [ ] Add feature flag configuration
  - [ ] Identify non-essential features (typing, presence)
  - [ ] Add degradation thresholds
- [ ] Add degradation middleware (AC: 2)
  - [ ] Check system load
  - [ ] Disable non-essential features when degraded
  - [ ] Log degradation state changes
- [ ] Add capacity metrics to Grafana dashboard (AC: 1)
  - [ ] Concurrent workflow count metric
  - [ ] Queue depth metric
  - [ ] Degradation state metric
- [ ] Add load test to verify degradation behavior (AC: 1, 2)
  - [ ] Test: Requests queued at 80% capacity
  - [ ] Test: Non-essential features disabled under load
  - [ ] Test: Core functionality continues

## Dev Notes

### Architecture Context

**From:** epic-10-13-implementation-readiness-2026-01-27.md

Story 10.3-H - graceful degradation hardening for Epic 10.

### Technical Requirements

1. **Capacity Management**: Track concurrent workflows, queue at threshold
2. **Feature Flags**: Runtime configuration for non-essential features
3. **Middleware**: Intercept requests and apply degradation rules
4. **Metrics**: Expose capacity and degradation state

### Project Structure Notes

**Files to Create:**
- `src/bmadServer.ApiService/Middleware/CapacityMiddleware.cs`
- `src/bmadServer.ApiService/Services/FeatureFlagService.cs`
- `tests/bmadServer.Tests/Unit/Middleware/CapacityMiddlewareTests.cs`

**Files to Modify:**
- `src/bmadServer.ApiService/Controllers/WorkflowsController.cs` - Add queueing logic
- `src/bmadServer.ApiService/Hubs/ChatHub.cs` - Add feature flag checks
- `src/bmadServer.ApiService/Program.cs` - Register middleware
- `appsettings.json` - Add capacity configuration

### References

- [Source: epic-10-13-implementation-readiness-2026-01-27.md#Story 10.3-H]

## Dev Agent Record

### Agent Model Used

<!-- To be filled by dev agent -->

### File List

<!-- To be filled by dev agent -->

### Change Log

<!-- To be filled by dev agent -->
