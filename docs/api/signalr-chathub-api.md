# SignalR ChatHub API Documentation

## Overview

The ChatHub provides real-time bidirectional communication between clients and the bmadServer using WebSocket connections via SignalR. This hub manages chat sessions, message delivery, workflow group membership, and automatic session recovery.

**Hub Endpoint:** `/hubs/chat`

**Authentication:** Required (JWT Bearer token)

## Connection

### Establishing a Connection

**Client Libraries:**
- JavaScript/TypeScript: `@microsoft/signalr`
- .NET: `Microsoft.AspNetCore.SignalR.Client`

**JavaScript Example:**
```javascript
import * as signalR from '@microsoft/signalr';

const connection = new signalR.HubConnectionBuilder()
  .withUrl('https://api.bmadserver.dev/hubs/chat', {
    accessTokenFactory: () => getAccessToken()
  })
  .withAutomaticReconnect([0, 2000, 10000, 30000]) // Exponential backoff
  .build();

await connection.start();
```

**.NET Example:**
```csharp
var connection = new HubConnectionBuilder()
    .WithUrl("https://api.bmadserver.dev/hubs/chat", options =>
    {
        options.AccessTokenProvider = () => Task.FromResult(GetAccessToken());
    })
    .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), 
        TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30) })
    .Build();

await connection.StartAsync();
```

### Authentication

The hub requires JWT authentication. The access token must be provided via:
- Query string parameter: `?access_token={token}`
- Or via `accessTokenFactory` in SignalR client options (recommended)

The JWT token must include:
- `sub` or `NameIdentifier` claim with user ID (GUID)
- Valid signature from bmadServer issuer
- Not expired

## Server Methods (Client → Server)

### SendMessage

Sends a chat message and updates the session state.

**Signature:**
```csharp
Task SendMessage(string message)
```

**Parameters:**
- `message` (string): The chat message content

**Returns:** `Task` (completes when message is processed)

**Performance:** Acknowledges within 2 seconds (NFR1)

**JavaScript Example:**
```javascript
await connection.invoke('SendMessage', 'Hello, I need help');
```

**.NET Example:**
```csharp
await connection.InvokeAsync("SendMessage", "Hello, I need help");
```

**Behavior:**
- Retrieves active session for the authenticated user
- Adds message to conversation history (keeps last 10 messages)
- Updates session activity timestamp
- Logs performance metrics (receipt and processing time)
- Sends acknowledgment via `ReceiveMessage` event

**Exceptions:**
- `HubException`: "No active session found" - if user has no active session

---

### JoinWorkflow

Joins a workflow-specific SignalR group for targeted messaging.

**Signature:**
```csharp
Task JoinWorkflow(string workflowName)
```

**Parameters:**
- `workflowName` (string): The name of the workflow to join

**Returns:** `Task`

**JavaScript Example:**
```javascript
await connection.invoke('JoinWorkflow', 'onboarding');
```

**.NET Example:**
```csharp
await connection.InvokeAsync("JoinWorkflow", "onboarding");
```

**Behavior:**
- Adds connection to a workflow-specific group
- Enables server to send workflow-specific broadcasts
- Logs group membership for debugging

**Use Cases:**
- Receiving workflow-specific notifications
- Multi-user workflow collaboration
- Targeted message delivery

---

### LeaveWorkflow

Leaves a workflow-specific SignalR group.

**Signature:**
```csharp
Task LeaveWorkflow(string workflowName)
```

**Parameters:**
- `workflowName` (string): The name of the workflow to leave

**Returns:** `Task`

**JavaScript Example:**
```javascript
await connection.invoke('LeaveWorkflow', 'onboarding');
```

**.NET Example:**
```csharp
await connection.InvokeAsync("LeaveWorkflow", "onboarding");
```

**Behavior:**
- Removes connection from workflow-specific group
- Stops receiving workflow-specific broadcasts
- Logs group membership change

---

## Client Methods (Server → Client)

These are events that the server sends to clients. Clients must register handlers for these events.

### ReceiveMessage

Receives a chat message (acknowledgment or assistant response).

**Signature:**
```typescript
ReceiveMessage(message: {
  Role: string;
  Content: string;
  Timestamp: Date;
})
```

**Parameters:**
- `message.Role` (string): Message role ("user", "assistant", "system")
- `message.Content` (string): Message content
- `message.Timestamp` (Date): UTC timestamp

**JavaScript Example:**
```javascript
connection.on('ReceiveMessage', (message) => {
  console.log(`[${message.Role}] ${message.Content}`);
  // Update UI with new message
});
```

**.NET Example:**
```csharp
connection.On<ChatMessage>("ReceiveMessage", message =>
{
    Console.WriteLine($"[{message.Role}] {message.Content}");
});
```

---

### SESSION_RESTORED

Sent when a client reconnects and their session is recovered.

**Signature:**
```typescript
SESSION_RESTORED(session: {
  Id: string;
  WorkflowName: string;
  CurrentStep: string;
  ConversationHistory: ChatMessage[];
  PendingInput: string | null;
  Message: string;
})
```

**Parameters:**
- `session.Id` (string): Session ID (GUID)
- `session.WorkflowName` (string): Active workflow name
- `session.CurrentStep` (string): Current workflow step
- `session.ConversationHistory` (array): Array of previous chat messages
- `session.PendingInput` (string?): Pending user input if any
- `session.Message` (string): Human-readable recovery message

**JavaScript Example:**
```javascript
connection.on('SESSION_RESTORED', (session) => {
  console.log(`Session restored: ${session.Message}`);
  
  // Restore conversation history in UI
  session.ConversationHistory.forEach(msg => {
    displayMessage(msg);
  });
  
  // Resume workflow at current step
  resumeWorkflow(session.WorkflowName, session.CurrentStep);
});
```

**.NET Example:**
```csharp
connection.On<SessionRestored>("SESSION_RESTORED", session =>
{
    Console.WriteLine($"Session restored: {session.Message}");
    RestoreConversationHistory(session.ConversationHistory);
    ResumeWorkflow(session.WorkflowName, session.CurrentStep);
});
```

**Recovery Scenarios:**
1. **Within 60s window:** Full state recovery with pending input
2. **After 60s:** Recovery from last checkpoint

---

## Connection Lifecycle Events

### OnConnectedAsync

Called automatically when a client connects.

**Server Behavior:**
- Extracts user ID from JWT claims
- Attempts to recover existing session within 60s window
- Creates new session if no recovery possible
- Logs connection with connection ID
- Sends `SESSION_RESTORED` if session recovered
- Acknowledges successful connection

**Client Detection:**
Connection state changes to `Connected`:

**JavaScript:**
```javascript
connection.onconnected = (connectionId) => {
  console.log(`Connected with ID: ${connectionId}`);
};
```

**.NET:**
```csharp
connection.Closed += async (error) =>
{
    // Connection closed
};

connection.Reconnecting += async (error) =>
{
    // Reconnection in progress
};

connection.Reconnected += async (connectionId) =>
{
    // Reconnection successful
};
```

---

### OnDisconnectedAsync

Called automatically when a client disconnects.

**Server Behavior:**
- Logs disconnection with connection ID and exception (if any)
- Does NOT immediately expire session (allows 60s reconnection window)
- Session cleanup service handles expiration after idle timeout

**Client Detection:**
Connection state changes to `Disconnected`:

**JavaScript:**
```javascript
connection.onclose = (error) => {
  console.error('Connection closed:', error);
};
```

---

## Automatic Reconnection

SignalR clients should be configured with automatic reconnection using exponential backoff to match NFR requirements.

**Recommended Retry Policy:**
- Attempt 1: 0 seconds (immediate)
- Attempt 2: 2 seconds
- Attempt 3: 10 seconds
- Attempt 4+: 30 seconds

**JavaScript Configuration:**
```javascript
.withAutomaticReconnect([0, 2000, 10000, 30000])
```

**.NET Configuration:**
```csharp
.WithAutomaticReconnect(new[] { 
    TimeSpan.Zero, 
    TimeSpan.FromSeconds(2), 
    TimeSpan.FromSeconds(10), 
    TimeSpan.FromSeconds(30) 
})
```

**Reconnection Flow:**
1. Connection lost → `onreconnecting` event fires
2. Client attempts reconnection with backoff
3. On success → `onreconnected` event fires → Server sends `SESSION_RESTORED`
4. On failure after max attempts → `onclose` event fires

For complete implementation example, see: [`docs/examples/signalr-client-reconnection.ts`](../examples/signalr-client-reconnection.ts)

---

## Error Handling

### HubException

Thrown by server methods when business logic errors occur.

**Common Exceptions:**
- `"User ID not found in claims"` - Invalid or missing JWT token
- `"No active session found"` - User has no active session

**JavaScript Handling:**
```javascript
try {
  await connection.invoke('SendMessage', 'Hello');
} catch (err) {
  if (err instanceof Error) {
    console.error('Hub error:', err.message);
  }
}
```

**.NET Handling:**
```csharp
try
{
    await connection.InvokeAsync("SendMessage", "Hello");
}
catch (HubException ex)
{
    Console.WriteLine($"Hub error: {ex.Message}");
}
```

---

## Performance Guarantees

### NFR1: Message Acknowledgment

**Requirement:** Messages must be acknowledged within 2 seconds.

**Measurement:** Time from client `invoke()` to `ReceiveMessage` callback

**Server Implementation:**
- Performance timing logged for each message
- Processing time tracked in milliseconds
- Alerts if threshold exceeded

**Client Verification:**
```javascript
const startTime = Date.now();
await connection.invoke('SendMessage', 'Test');
// Wait for ReceiveMessage callback
const elapsed = Date.now() - startTime;
console.log(`Acknowledgment time: ${elapsed}ms`);
```

---

## Testing

### Performance Tests

Located in: `src/bmadServer.Tests/Integration/ChatHubPerformanceTests.cs`

**Tests:**
1. `SendMessage_ShouldAcknowledgeWithin2Seconds` - Validates NFR1 for single message
2. `SendMessage_ShouldCompleteWithin2Seconds` - Validates invoke completion time
3. `SendMessage_MultipleMessages_ShouldEachAcknowledgeWithin2Seconds` - Sequential load test

### Integration Tests

Located in: `src/bmadServer.Tests/Integration/ChatHubIntegrationTests.cs`

**Tests:**
- Connection establishment
- Authentication via JWT
- Session recovery
- Message sending and receiving
- Workflow group membership

---

## Complete Usage Example

```typescript
import * as signalR from '@microsoft/signalr';

// Create connection with automatic reconnection
const connection = new signalR.HubConnectionBuilder()
  .withUrl('https://api.bmadserver.dev/hubs/chat', {
    accessTokenFactory: () => localStorage.getItem('accessToken')
  })
  .withAutomaticReconnect([0, 2000, 10000, 30000])
  .build();

// Handle connection events
connection.onconnected = (id) => console.log('Connected:', id);
connection.onreconnecting = (err) => console.log('Reconnecting...', err);
connection.onreconnected = (id) => console.log('Reconnected:', id);
connection.onclose = (err) => console.error('Disconnected:', err);

// Register server event handlers
connection.on('ReceiveMessage', (message) => {
  console.log(`[${message.Role}] ${message.Content}`);
  displayMessage(message);
});

connection.on('SESSION_RESTORED', (session) => {
  console.log('Session restored:', session.Message);
  restoreConversationHistory(session.ConversationHistory);
});

// Connect to hub
await connection.start();

// Join workflow
await connection.invoke('JoinWorkflow', 'onboarding');

// Send message
await connection.invoke('SendMessage', 'Hello, I need help with onboarding');

// Leave workflow when done
await connection.invoke('LeaveWorkflow', 'onboarding');

// Disconnect when done
await connection.stop();
```

---

## Security

### Authentication
- All hub methods require authentication via `[Authorize]` attribute
- JWT token validated on connection and method invocation
- User ID extracted from `NameIdentifier` or `sub` claim

### Authorization
- Users can only access their own sessions
- Workflow group membership does not bypass session ownership
- Connection ID tied to authenticated user

### Token Expiry
- Expired tokens rejected at connection time
- Client should refresh token before expiry
- Reconnection requires valid token

---

## Related Documentation

- [Session Persistence & Recovery (Story 2.4)](../../_bmad-output/implementation-artifacts/2-4-session-persistence-recovery.md)
- [SignalR Client Reconnection Example](../examples/signalr-client-reconnection.ts)
- [ChatHub Performance Tests](../../src/bmadServer.Tests/Integration/ChatHubPerformanceTests.cs)
- [Microsoft SignalR Documentation](https://learn.microsoft.com/en-us/aspnet/core/signalr/introduction)
