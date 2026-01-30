# Story 11.1-H: Implement Per-User Rate Limiting

Status: ready-for-dev

## Story

As an operator,
I want per-user rate limiting,
so that no single user can overwhelm the system.

## Acceptance Criteria

**Given** rate limiting configuration exists  
**When** I check appsettings.json  
**Then** I see: `RateLimiting: { RequestsPerMinute: 60, BurstLimit: 10 }`

**Given** a user exceeds RequestsPerMinute  
**When** they make another request  
**Then** they receive 429 Too Many Requests  
**And** response includes Retry-After header with seconds to wait

**Given** User A is rate limited  
**When** User B makes requests  
**Then** User B is not affected (limits are per-user from JWT)

## Tasks / Subtasks

- [ ] Add AspNetCoreRateLimit NuGet package (AC: 1)
  - [ ] Update bmadServer.ApiService.csproj
  - [ ] Restore packages
- [ ] Configure per-user rate limiting in Program.cs (AC: 1, 2, 3)
  - [ ] Add rate limit configuration from appsettings
  - [ ] Configure per-user (JWT-based) rate limiting
  - [ ] Set default limits: 60 req/min, burst 10
- [ ] Add rate limit headers to responses (AC: 2)
  - [ ] X-RateLimit-Limit header
  - [ ] X-RateLimit-Remaining header
  - [ ] Retry-After header on 429
- [ ] Add rate limit metrics to OpenTelemetry (AC: 2, 3)
  - [ ] Counter for rate limit violations
  - [ ] Gauge for current rate limit usage
  - [ ] Per-user tracking
- [ ] Add tests for rate limiting behavior (AC: 1, 2, 3)
  - [ ] Test: Requests under limit succeed
  - [ ] Test: Requests over limit return 429
  - [ ] Test: Per-user isolation
  - [ ] Test: Rate limit headers present
- [ ] Document rate limit configuration (AC: 1)
  - [ ] Add to README or docs
  - [ ] Document configuration options

## Dev Notes

### Architecture Context

**From:** epic-10-13-implementation-readiness-2026-01-27.md

Story 11.1-H - security hardening for Epic 11. AspNetCoreRateLimit already in dependencies.

### Technical Requirements

1. **AspNetCoreRateLimit**: Industry-standard rate limiting library
2. **JWT-Based**: Extract user ID from JWT claims
3. **Per-User Tracking**: Maintain separate counters per user
4. **Response Headers**: Inform clients of limit status

### Project Structure Notes

**Files to Modify:**
- `src/bmadServer.ApiService/Program.cs` - Configure rate limiting middleware
- `appsettings.json` - Add RateLimiting section
- `tests/bmadServer.Tests/Unit/Middleware/RateLimitTests.cs` - Create tests

### Existing Patterns

- AspNetCoreRateLimit v5.0.0 already in dependencies
- JWT authentication configured
- User identification from claims

### References

- [Source: epic-10-13-implementation-readiness-2026-01-27.md#Story 11.1-H]
- [AspNetCoreRateLimit Documentation](https://github.com/stefanprodan/AspNetCoreRateLimit)

## Dev Agent Record

### Agent Model Used

<!-- To be filled by dev agent -->

### File List

<!-- To be filled by dev agent -->

### Change Log

<!-- To be filled by dev agent -->
