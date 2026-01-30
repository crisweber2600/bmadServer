# Story 10.2-H: Implement Circuit Breaker for External Services

Status: ready-for-dev

## Story

As an operator,
I want circuit breakers on external service calls,
so that cascading failures are prevented.

## Acceptance Criteria

**Given** 5 consecutive failures occur to an external service  
**When** the circuit breaker trips  
**Then** subsequent calls fail fast for 30 seconds  
**And** the circuit state is logged and exposed via metrics

**Given** the circuit is open  
**When** the timeout expires  
**Then** the circuit moves to half-open state  
**And** a single test request is allowed  
**And** success closes the circuit, failure reopens it

## Tasks / Subtasks

- [ ] Configure Polly CircuitBreaker policy (AC: 1, 2)
  - [ ] Create CircuitBreakerPolicy in Infrastructure/Policies
  - [ ] Configure: 5 consecutive failures trips circuit
  - [ ] Configure: 30-second break duration
  - [ ] Configure: Half-open state allows 1 test request
- [ ] Apply to LLM provider calls and external HTTP clients (AC: 1)
  - [ ] Wrap HttpClient calls with circuit breaker
  - [ ] Apply to agent handler calls if external
  - [ ] Log circuit state changes
- [ ] Expose circuit state via /health endpoint (AC: 1)
  - [ ] Add circuit breaker health check
  - [ ] Return degraded when circuit open
  - [ ] Include circuit state in health response
- [ ] Add Grafana alert for circuit breaker trips (AC: 1)
  - [ ] Add OpenTelemetry metric for circuit state
  - [ ] Create counter for circuit breaks
  - [ ] Document alerting configuration
- [ ] Add tests for circuit breaker behavior (AC: 1, 2)
  - [ ] Test: 5 failures trips circuit
  - [ ] Test: Circuit opens and fails fast
  - [ ] Test: Half-open allows test request
  - [ ] Test: Success in half-open closes circuit
  - [ ] Test: Failure in half-open reopens circuit

## Dev Notes

### Architecture Context

**From:** epic-10-13-implementation-readiness-2026-01-27.md

This is story 10.2-H, a hardening story for Epic 10: Error Handling & Recovery. Builds upon 10.1-H (Polly retry policies).

### Technical Requirements

1. **Circuit Breaker Pattern**: Prevents cascading failures by fast-failing when service is unhealthy
2. **Polly Integration**: Use existing AgentCallPolicy as foundation
3. **State Transitions**: Closed → Open (after failures) → Half-Open (after timeout) → Closed/Open
4. **Health Monitoring**: Expose circuit state for operational visibility
5. **Metrics**: Track circuit state for alerting

### Project Structure Notes

**Files to Create:**
- `src/bmadServer.ApiService/Infrastructure/Policies/CircuitBreakerPolicy.cs`
- `src/bmadServer.ApiService/Infrastructure/HealthChecks/CircuitBreakerHealthCheck.cs`
- `tests/bmadServer.Tests/Unit/Infrastructure/Policies/CircuitBreakerPolicyTests.cs`

**Files to Modify:**
- `src/bmadServer.ApiService/Program.cs` - Register circuit breaker policy and health check
- `src/bmadServer.ServiceDefaults/Extensions.cs` - Add health check registration (if needed)

### Existing Patterns

- Polly retry policies already implemented (Story 10.1-H)
- OpenTelemetry metrics collection configured
- Health checks registered in ServiceDefaults

### References

- [Source: epic-10-13-implementation-readiness-2026-01-27.md#Story 10.2-H]
- [Polly Circuit Breaker Documentation](https://github.com/App-vNext/Polly/wiki/Circuit-Breaker)

## Dev Agent Record

### Agent Model Used

<!-- To be filled by dev agent -->

### File List

<!-- To be filled by dev agent with all files created/modified -->

### Change Log

<!-- To be filled by dev agent with summary of changes -->
