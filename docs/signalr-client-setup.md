# SignalR Client Setup Guide

## Overview

This document provides guidance for connecting to the bmadServer SignalR chat hub from JavaScript/TypeScript clients.

## Connection Setup with Authentication

### Basic Connection

```typescript
import * as signalR from "@microsoft/signalr";

// Create connection with JWT authentication
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/chat", {
        accessTokenFactory: () => {
            // Return the JWT access token
            return localStorage.getItem("accessToken") || "";
        }
    })
    .withAutomaticReconnect({
        // Custom reconnect delays (in milliseconds)
        nextRetryDelayInMilliseconds: (retryContext) => {
            // Exponential backoff: 0s, 2s, 10s, 30s, 30s, 30s...
            if (retryContext.previousRetryCount === 0) {
                return 0; // Immediate retry
            } else if (retryContext.previousRetryCount === 1) {
                return 2000; // 2 seconds
            } else if (retryContext.previousRetryCount === 2) {
                return 10000; // 10 seconds
            } else {
                return 30000; // 30 seconds for all subsequent retries
            }
        }
    })
    .configureLogging(signalR.LogLevel.Information)
    .build();
```

### Automatic Reconnection

The connection is configured with automatic reconnection using exponential backoff as specified in AC:
- **First retry**: Immediate (0 seconds)
- **Second retry**: 2 seconds
- **Third retry**: 10 seconds
- **Subsequent retries**: 30 seconds

### Connection Lifecycle Events

```typescript
// Handle reconnecting event
connection.onreconnecting((error) => {
    console.warn("Connection lost. Reconnecting...", error);
    // Update UI to show "Reconnecting..." state
});

// Handle reconnected event
connection.onreconnected((connectionId) => {
    console.log("Reconnected with connection ID:", connectionId);
    // Session recovery flow from Epic 2 executes automatically on server
    // Update UI to show "Connected" state
});

// Handle close event (reconnection failed)
connection.onclose((error) => {
    console.error("Connection closed. Manual reconnect may be required.", error);
    // Update UI to show "Disconnected" state
    // Optionally show "Retry" button
});
```

### Starting the Connection

```typescript
async function startConnection() {
    try {
        await connection.start();
        console.log("Connected to SignalR hub");
    } catch (err) {
        console.error("Error connecting to SignalR:", err);
        // Retry after delay
        setTimeout(startConnection, 5000);
    }
}

startConnection();
```

## Hub Methods

### Sending Messages

```typescript
// Send a chat message
async function sendMessage(message: string) {
    try {
        await connection.invoke("SendMessage", message);
    } catch (err) {
        console.error("Error sending message:", err);
    }
}
```

### Joining/Leaving Workflows

```typescript
// Join a workflow group
async function joinWorkflow(workflowName: string) {
    try {
        await connection.invoke("JoinWorkflow", workflowName);
    } catch (err) {
        console.error("Error joining workflow:", err);
    }
}

// Leave a workflow group
async function leaveWorkflow(workflowName: string) {
    try {
        await connection.invoke("LeaveWorkflow", workflowName);
    } catch (err) {
        console.error("Error leaving workflow:", err);
    }
}
```

### Receiving Messages

```typescript
// Listen for incoming messages
connection.on("ReceiveMessage", (message) => {
    console.log("Received message:", message);
    // Update UI with new message
});

// Listen for session restored event
connection.on("SESSION_RESTORED", (session) => {
    console.log("Session restored:", session);
    // Restore UI state from session
    // session includes: id, workflowName, currentStep, conversationHistory, etc.
});

// Listen for workflow join confirmation
connection.on("JoinedWorkflow", (data) => {
    console.log("Joined workflow:", data.WorkflowName);
});

// Listen for workflow leave confirmation
connection.on("LeftWorkflow", (data) => {
    console.log("Left workflow:", data.WorkflowName);
});
```

## Error Handling

### Token Expiration

If the access token expires during the connection, you'll receive a 401 Unauthorized error. Handle this by:

1. Refreshing the token using the refresh token flow (Epic 2, Story 2-3)
2. Reconnecting with the new access token

```typescript
connection.onclose(async (error) => {
    if (error?.message?.includes("Unauthorized")) {
        // Token expired - refresh and reconnect
        await refreshAccessToken();
        await startConnection();
    }
});
```

## Performance Considerations

- **Message Acknowledgment**: Messages are acknowledged within 2 seconds (NFR1)
- **Reconnection Window**: Session recovery within 60 seconds retains full state (Epic 2, Story 2-4)
- **Connection ID**: Logged on server for debugging purposes

## Example: React Integration

```typescript
import { useEffect, useState } from 'react';
import * as signalR from "@microsoft/signalR";

export function useChatConnection(accessToken: string) {
    const [connection, setConnection] = useState<signalR.HubConnection | null>(null);
    const [connectionState, setConnectionState] = useState<"Disconnected" | "Connecting" | "Connected" | "Reconnecting">("Disconnected");

    useEffect(() => {
        const newConnection = new signalR.HubConnectionBuilder()
            .withUrl("/hubs/chat", {
                accessTokenFactory: () => accessToken
            })
            .withAutomaticReconnect({
                nextRetryDelayInMilliseconds: (retryContext) => {
                    if (retryContext.previousRetryCount === 0) return 0;
                    if (retryContext.previousRetryCount === 1) return 2000;
                    if (retryContext.previousRetryCount === 2) return 10000;
                    return 30000;
                }
            })
            .build();

        newConnection.onreconnecting(() => setConnectionState("Reconnecting"));
        newConnection.onreconnected(() => setConnectionState("Connected"));
        newConnection.onclose(() => setConnectionState("Disconnected"));

        setConnection(newConnection);

        return () => {
            newConnection.stop();
        };
    }, [accessToken]);

    useEffect(() => {
        if (connection) {
            setConnectionState("Connecting");
            connection.start()
                .then(() => setConnectionState("Connected"))
                .catch(() => setConnectionState("Disconnected"));
        }
    }, [connection]);

    return { connection, connectionState };
}
```

## References

- [SignalR JavaScript Client Documentation](https://learn.microsoft.com/en-us/aspnet/core/signalr/javascript-client)
- [Story 3-1: SignalR Hub Setup](../_bmad-output/implementation-artifacts/3-1-signalr-hub-setup-websocket-connection.md)
- [Story 2-4: Session Persistence & Recovery](../_bmad-output/implementation-artifacts/2-4-session-persistence-recovery.md)
