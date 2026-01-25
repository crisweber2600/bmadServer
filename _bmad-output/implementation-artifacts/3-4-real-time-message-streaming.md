# Story 3.4: Real-Time Message Streaming

**Status:** done

## Story

As a user (Marcus), I want to see agent responses stream in real-time, so that I get immediate feedback and can follow long responses as they're generated.

## Acceptance Criteria

**Given** I send a message to an agent  
**When** the agent starts generating a response  
**Then** streaming begins within 5 seconds (NFR2) and the first token appears

**Given** an agent is streaming a response  
**When** tokens arrive via SignalR  
**Then** each token appends to the message smoothly without flickering

**Given** a streaming response is in progress  
**When** I check the SignalR message format  
**Then** I see MESSAGE_CHUNK type with messageId, chunk, isComplete, and agentId fields

**Given** streaming completes  
**When** the final chunk arrives with isComplete: true  
**Then** the typing indicator disappears and full message displays with formatting

**Given** streaming is interrupted by network issues  
**When** the SignalR connection drops mid-stream  
**Then** the partial message is preserved and reconnection resumes from last chunk

**Given** I want to cancel a long-running response  
**When** I click "Stop Generating" during streaming  
**Then** streaming stops immediately with "(Stopped)" indicator

## Tasks / Subtasks

- [x] Analyze acceptance criteria and create detailed implementation plan
- [x] Design data models and database schema if needed
- [x] Implement core business logic
  - [x] ChatHub MESSAGE_CHUNK streaming with messageId, chunk, isComplete, agentId
  - [x] Server-side streaming simulation with token-by-token delivery
  - [x] Partial message persistence for interruption recovery
  - [x] StopGenerating endpoint for cancellation
- [x] Create API endpoints and/or UI components
  - [x] useStreamingMessage React hook for client-side streaming
  - [x] Token-by-token UI updates with smooth rendering
  - [x] Stop Generating button integration
- [x] Write unit tests for critical paths
  - [x] useStreamingMessage hook tests (chunk handling, completion, stopping)
- [x] Write integration tests for key scenarios
  - [x] ChatHubStreamingTests for MESSAGE_CHUNK events
  - [x] Partial message recovery tests
  - [x] Conversation history limit tests
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

**Backend:**
- `/src/bmadServer.ApiService/Hubs/ChatHub.cs` - Added MESSAGE_CHUNK streaming, StopGenerating, partial message persistence
- `/src/bmadServer.Tests/Integration/ChatHubStreamingTests.cs` - Comprehensive streaming tests

**Frontend:**
- `/src/frontend/src/hooks/useStreamingMessage.ts` - React hook for streaming message handling
- `/src/frontend/src/hooks/useStreamingMessage.test.ts` - Hook unit tests
- `/src/frontend/src/hooks/index.ts` - Hook exports

---

## Aspire Development Standards

### SignalR Integration Pattern

This story uses SignalR configured in Story 3.1:
- **MVP:** Built-in ASP.NET Core SignalR (no external dependency)
- MESSAGE_CHUNK streaming via SignalR hub
- See Story 3.1 for SignalR configuration pattern

### PostgreSQL Connection Pattern

This story uses PostgreSQL configured in Story 1.2 via Aspire:
- Partial messages persisted for recovery
- Connection string automatically injected from Aspire AppHost
- Pattern: `builder.AddServiceDefaults();` (inherits PostgreSQL reference)

### Project-Wide Standards

This story follows the Aspire-first development pattern:
- **Reference:** [PROJECT-WIDE-RULES.md](../../../PROJECT-WIDE-RULES.md)
- **Primary Documentation:** https://aspire.dev
- **GitHub:** https://github.com/microsoft/aspire

---

## References

- Source: [epics.md - Story 3.4](../planning-artifacts/epics.md)
- Architecture: [architecture.md](../planning-artifacts/architecture.md)
- PRD: [prd.md](../planning-artifacts/prd.md)
- **Aspire Rules:** [PROJECT-WIDE-RULES.md](../../../PROJECT-WIDE-RULES.md)
- **Aspire Docs:** https://aspire.dev
