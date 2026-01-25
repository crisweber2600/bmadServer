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

### Dev Agent Record

**Files Created:**
- `src/bmadServer.ApiService/Hubs/ChatHub.cs` - SignalR hub with SendMessage, JoinWorkflow, LeaveWorkflow methods
- `src/bmadServer.Tests/Integration/ChatHubPerformanceTests.cs` - NFR1 performance validation tests
- `docs/examples/signalr-client-reconnection.ts` - Client-side reconnection example with automatic reconnect
- `docs/api/signalr-chathub-api.md` - Complete API documentation for SignalR hub endpoints

**Files Modified:**
- `src/bmadServer.ApiService/bmadServer.ApiService.csproj` - Added Microsoft.AspNetCore.SignalR package reference
- `src/bmadServer.ApiService/Program.cs` - Added OnMessageReceived event handler for WebSocket JWT authentication
- `src/bmadServer.ApiService/Hubs/ChatHub.cs` - Added connection ID logging, performance logging, JoinWorkflow, and LeaveWorkflow methods

---

## Aspire Development Standards

### SignalR Implementation Pattern (VERIFIED 2026-01-24)

**MVP Approach: Self-Hosted SignalR (RECOMMENDED)**

For bmadServer MVP, use the built-in ASP.NET Core SignalR which requires no external dependencies:

```csharp
// In bmadServer.ApiService/Program.cs
builder.Services.AddSignalR();

var app = builder.Build();
app.MapHub<ChatHub>("/hubs/chat");
```

**Why Self-Hosted for MVP:**
- ✅ No Azure subscription required
- ✅ No additional Aspire configuration
- ✅ Built into ASP.NET Core (no extra packages)
- ✅ Simple to implement and test
- ✅ Scales vertically (single server handles many connections)

**Future: Azure SignalR Service (Production Scaling)**

When horizontal scaling is needed, Aspire provides `Aspire.Hosting.Azure.SignalR`:

```csharp
// AppHost/Program.cs (future - not for MVP)
var signalR = builder.AddAzureSignalR("signalr");
// Or with local emulator:
var signalR = builder.AddAzureSignalR("signalr", AzureSignalRServiceMode.Serverless)
    .RunAsEmulator();
```

**Scaling Path:**
1. **MVP (Now):** Self-hosted SignalR - handles thousands of connections per server
2. **Scale-Out (Future):** Add Redis backplane via `aspire add Redis.Distributed` (see Epic 10)
3. **Enterprise (Future):** Use Azure SignalR Service for unlimited horizontal scaling

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

- Source: [epics.md - Story 3.1](../planning-artifacts/epics.md)
- Architecture: [architecture.md](../planning-artifacts/architecture.md)
- PRD: [prd.md](../planning-artifacts/prd.md)
- **Aspire Rules:** [PROJECT-WIDE-RULES.md](../../../PROJECT-WIDE-RULES.md)
- **Aspire Docs:** https://aspire.dev
- **SignalR Analysis:** [ASPIRE_ALIGNMENT_ANALYSIS.md](../../../ASPIRE_ALIGNMENT_ANALYSIS.md#epic-3-real-time-chat-interface)
