# Story 3.1: SignalR Hub Setup & WebSocket Connection

**Status:** ready-for-dev

## Story

As a user (Sarah), I want to establish a persistent WebSocket connection to bmadServer, so that I can receive real-time updates and send messages without page refreshes.

## Acceptance Criteria

**Given** the bmadServer API is running  
**When** I add SignalR to the project via `dotnet add package Microsoft.AspNetCore.SignalR`  
**Then** the package installs without errors  
**And** the project references Microsoft.AspNetCore.SignalR namespace

**Given** SignalR is installed  
**When** I create `Hubs/ChatHub.cs`  
**Then** the file defines a ChatHub class inheriting from Hub with methods: SendMessage, JoinWorkflow, LeaveWorkflow, OnConnectedAsync, OnDisconnectedAsync

**Given** ChatHub is created  
**When** I configure SignalR in `Program.cs`  
**Then** builder.Services.AddSignalR() and app.MapHub<ChatHub>("/hubs/chat") are configured  
**And** the hub endpoint is accessible at `/hubs/chat`

**Given** the SignalR hub is configured  
**When** I authenticate and connect from a JavaScript client with accessTokenFactory  
**Then** the connection establishes successfully  
**And** OnConnectedAsync is called on the server  
**And** the connection ID is logged for debugging

**Given** I am connected to the hub  
**When** I send a message via connection.invoke("SendMessage", "Hello")  
**Then** the server receives the message within 100ms  
**And** the message is acknowledged within 2 seconds (NFR1)

**Given** the WebSocket connection drops unexpectedly  
**When** SignalR automatic reconnect triggers  
**Then** the client attempts reconnection with exponential backoff (0s, 2s, 10s, 30s)  
**And** the session recovery flow from Epic 2 executes

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

- Source: [epics.md - Story 3.1](../planning-artifacts/epics.md)
- Architecture: [architecture.md](../planning-artifacts/architecture.md)
- PRD: [prd.md](../planning-artifacts/prd.md)
