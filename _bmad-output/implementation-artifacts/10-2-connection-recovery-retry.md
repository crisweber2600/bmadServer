# Story 10.2: Connection Recovery & Retry

**Status:** ready-for-dev

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a user (Marcus), I want automatic connection recovery, so that brief network issues don't disrupt my work.

## Acceptance Criteria

**Given** my WebSocket connection drops  
**When** the disconnect is detected  
**Then** I see "Reconnecting..." indicator  
**And** automatic reconnection attempts begin

**Given** reconnection is in progress  
**When** attempts are made  
**Then** exponential backoff is used: 0s, 2s, 10s, 30s intervals  
**And** maximum 5 attempts before giving up

**Given** reconnection succeeds  
**When** the connection is restored  
**Then** I see "Connected" indicator  
**And** session state is restored from last checkpoint  
**And** any queued messages are sent

**Given** reconnection fails after all retries  
**When** the final attempt fails  
**Then** I see: "Unable to connect. Check your internet connection."  
**And** a "Retry" button is available  
**And** my draft input is preserved locally

**Given** the server is temporarily unavailable  
**When** API calls fail  
**Then** the client retries with backoff  
**And** cached data is shown where available

## Tasks / Subtasks

- [ ] Task 1: Frontend SignalR Connection Management with Exponential Backoff (AC: 1, 2, 3, 4)
  - [ ] Create SignalR connection service with automatic reconnection logic
  - [ ] Implement exponential backoff: 0s, 2s, 10s, 30s intervals (max 5 attempts)
  - [ ] Add connection state tracking (Connected, Reconnecting, Disconnected)
  - [ ] Implement message queue for failed sends during disconnection
  - [ ] Add local storage persistence for draft messages
  - [ ] Create connection status UI component with indicators
  - [ ] Add manual retry button for failed connections
- [ ] Task 2: Session Recovery Integration (AC: 3)
  - [ ] Implement session restoration after reconnection
  - [ ] Integrate with existing ChatHub SESSION_RESTORED event
  - [ ] Test conversation history recovery
  - [ ] Test workflow state recovery
  - [ ] Handle queued message delivery after reconnection
- [ ] Task 3: API Retry Logic with Circuit Breaker (AC: 5)
  - [ ] Implement HTTP client retry policy with exponential backoff
  - [ ] Add Polly resilience library for retry patterns
  - [ ] Configure circuit breaker for API calls
  - [ ] Add request caching for failed API calls
  - [ ] Handle offline scenarios gracefully
- [ ] Task 4: UI/UX for Connection States (AC: 1, 3, 4)
  - [ ] Design connection status banner/indicator
  - [ ] Add "Reconnecting..." animated indicator
  - [ ] Add "Connected" confirmation message (dismissible)
  - [ ] Add "Unable to connect" error message with retry button
  - [ ] Style connection indicators following Ant Design patterns
- [ ] Task 5: Testing and Validation
  - [ ] Unit tests for connection retry logic
  - [ ] Unit tests for exponential backoff timing
  - [ ] Integration tests for session recovery after reconnection
  - [ ] Manual testing with network throttling/disconnect
  - [ ] Test message queue and delivery after reconnection
  - [ ] Test draft preservation in local storage

## Dev Notes

### 🎯 CRITICAL IMPLEMENTATION REQUIREMENTS

#### Existing SignalR Infrastructure

**IMPORTANT:** bmadServer already has SignalR configured with session recovery!

```csharp
// src/bmadServer.ApiService/Hubs/ChatHub.cs
// Existing session recovery logic (lines 35-73)
public override async Task OnConnectedAsync()
{
    var userId = GetUserIdFromClaims();
    var connectionId = Context.ConnectionId;

    // Attempt to recover existing session or create new one
    var (session, isRecovered) = await _sessionService.RecoverSessionAsync(userId, connectionId);

    if (isRecovered && session.WorkflowState != null)
    {
        // Send recovery message to client
        await Clients.Caller.SendAsync("SESSION_RESTORED", new
        {
            session.Id,
            session.WorkflowState.WorkflowName,
            session.WorkflowState.CurrentStep,
            session.WorkflowState.ConversationHistory,
            session.WorkflowState.PendingInput,
            Message = session.IsWithinRecoveryWindow 
                ? "Session restored - resuming from where you left off"
                : "Session recovered from last checkpoint"
        });
    }
    ...
}
```

**Backend is READY** - This story focuses on **FRONTEND implementation** of reconnection logic!

### 🏗️ Architecture Context

**Frontend Stack (from architecture.md):**
- **Build Tool:** Vite
- **State Management:** Zustand 4.5+
- **Server State:** TanStack Query 5.x
- **Routing:** React Router v7
- **TypeScript:** 5.x (strict mode)
- **Styling:** Tailwind CSS + Ant Design
- **Forms:** React Hook Form

**SignalR Client:**
- Package: `@microsoft/signalr` (already installed in Story 3.1)
- Connection URL: `/hubs/chat` (configured in ChatHub.cs)
- Authentication: JWT token via `accessTokenFactory`

**Existing Frontend Components:**
- `src/frontend/src/components/ResponsiveChat.tsx` - Main chat component
- `src/frontend/src/hooks/useStreamingMessage.ts` - Message streaming hook
- `src/frontend/src/hooks/useScrollManagement.ts` - Scroll management

**Frontend Structure:**
```
src/frontend/src/
├── components/
│   ├── ResponsiveChat.tsx (existing)
│   ├── ChatMessage.tsx (existing)
│   └── ConnectionStatus.tsx (NEW - create this)
├── services/
│   ├── signalrService.ts (NEW - create this)
│   └── apiRetryService.ts (NEW - create this)
├── hooks/
│   ├── useSignalRConnection.ts (NEW - create this)
│   └── useConnectionStatus.ts (NEW - create this)
├── stores/
│   └── connectionStore.ts (NEW - create this)
└── utils/
    └── retryPolicies.ts (NEW - create this)
```

### 📋 Implementation Checklist

#### 1. Create SignalR Connection Service

**File:** `src/frontend/src/services/signalrService.ts`

**Requirements:**
- Singleton service for managing SignalR HubConnection
- Automatic reconnection with exponential backoff
- Message queue for offline messages
- Connection state management
- Event emission for connection state changes

**Implementation Pattern:**
```typescript
import * as signalR from '@microsoft/signalr';

export enum ConnectionState {
  Connected = 'Connected',
  Reconnecting = 'Reconnecting',
  Disconnected = 'Disconnected',
}

export class SignalRService {
  private connection: signalR.HubConnection | null = null;
  private messageQueue: Array<{ method: string; args: any[] }> = [];
  private connectionState: ConnectionState = ConnectionState.Disconnected;
  private reconnectAttempts = 0;
  private maxReconnectAttempts = 5;
  private backoffIntervals = [0, 2000, 10000, 30000]; // 0s, 2s, 10s, 30s

  async connect(accessToken: string): Promise<void> {
    if (this.connection) {
      await this.connection.stop();
    }

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/chat', {
        accessTokenFactory: () => accessToken,
      })
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (retryContext) => {
          // Custom exponential backoff
          if (retryContext.previousRetryCount >= this.maxReconnectAttempts) {
            return null; // Stop reconnecting
          }
          const interval = this.backoffIntervals[
            Math.min(retryContext.previousRetryCount, this.backoffIntervals.length - 1)
          ];
          return interval;
        },
      })
      .configureLogging(signalR.LogLevel.Information)
      .build();

    this.setupEventHandlers();
    await this.connection.start();
  }

  private setupEventHandlers(): void {
    if (!this.connection) return;

    // Connection state events
    this.connection.onclose(() => {
      this.updateConnectionState(ConnectionState.Disconnected);
    });

    this.connection.onreconnecting(() => {
      this.updateConnectionState(ConnectionState.Reconnecting);
      this.reconnectAttempts++;
    });

    this.connection.onreconnected(() => {
      this.updateConnectionState(ConnectionState.Connected);
      this.reconnectAttempts = 0;
      this.processQueuedMessages();
    });

    // Handle SESSION_RESTORED from server
    this.connection.on('SESSION_RESTORED', (data) => {
      // Emit event for UI to handle
      this.emit('sessionRestored', data);
    });

    // Handle ReceiveMessage from server
    this.connection.on('ReceiveMessage', (message) => {
      this.emit('receiveMessage', message);
    });
  }

  async sendMessage(message: string): Promise<void> {
    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      await this.connection.invoke('SendMessage', message);
    } else {
      // Queue message for later delivery
      this.messageQueue.push({ method: 'SendMessage', args: [message] });
      // Persist to local storage
      this.persistDraftMessage(message);
    }
  }

  private async processQueuedMessages(): Promise<void> {
    while (this.messageQueue.length > 0) {
      const { method, args } = this.messageQueue.shift()!;
      try {
        await this.connection?.invoke(method, ...args);
      } catch (error) {
        console.error('Failed to send queued message:', error);
        // Re-queue if failed
        this.messageQueue.unshift({ method, args });
        break;
      }
    }
  }

  private persistDraftMessage(message: string): void {
    localStorage.setItem('bmadServer_draftMessage', message);
  }

  getDraftMessage(): string | null {
    return localStorage.getItem('bmadServer_draftMessage');
  }

  clearDraftMessage(): void {
    localStorage.removeItem('bmadServer_draftMessage');
  }

  // Event emitter pattern (simplified - consider using EventEmitter library)
  private listeners: Map<string, Array<(data: any) => void>> = new Map();

  on(event: string, callback: (data: any) => void): void {
    if (!this.listeners.has(event)) {
      this.listeners.set(event, []);
    }
    this.listeners.get(event)!.push(callback);
  }

  private emit(event: string, data: any): void {
    this.listeners.get(event)?.forEach((callback) => callback(data));
  }

  getConnectionState(): ConnectionState {
    return this.connectionState;
  }

  private updateConnectionState(state: ConnectionState): void {
    this.connectionState = state;
    this.emit('connectionStateChanged', state);
  }

  getReconnectAttempts(): number {
    return this.reconnectAttempts;
  }
}

// Singleton instance
export const signalRService = new SignalRService();
```

#### 2. Create Connection Status UI Component

**File:** `src/frontend/src/components/ConnectionStatus.tsx`

**Requirements:**
- Display connection status indicator
- Show reconnection progress
- Show retry button when connection fails
- Animate transitions between states
- Dismissible success message

**Pattern:**
```typescript
import React from 'react';
import { Alert, Button } from 'antd';
import { LoadingOutlined, CheckCircleOutlined, CloseCircleOutlined } from '@ant-design/icons';
import { ConnectionState } from '../services/signalrService';

interface ConnectionStatusProps {
  connectionState: ConnectionState;
  reconnectAttempts: number;
  maxAttempts: number;
  onRetry: () => void;
}

export const ConnectionStatus: React.FC<ConnectionStatusProps> = ({
  connectionState,
  reconnectAttempts,
  maxAttempts,
  onRetry,
}) => {
  if (connectionState === ConnectionState.Connected && reconnectAttempts === 0) {
    // Don't show anything when connected (or show briefly and auto-dismiss)
    return null;
  }

  if (connectionState === ConnectionState.Reconnecting) {
    return (
      <Alert
        message={
          <span>
            <LoadingOutlined spin style={{ marginRight: 8 }} />
            Reconnecting... (Attempt {reconnectAttempts} of {maxAttempts})
          </span>
        }
        type="warning"
        banner
        closable={false}
      />
    );
  }

  if (connectionState === ConnectionState.Connected && reconnectAttempts > 0) {
    return (
      <Alert
        message={
          <span>
            <CheckCircleOutlined style={{ marginRight: 8 }} />
            Connected
          </span>
        }
        type="success"
        banner
        closable
        afterClose={() => {
          // Reset attempts after dismissal
        }}
      />
    );
  }

  if (connectionState === ConnectionState.Disconnected) {
    return (
      <Alert
        message={
          <span style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <span>
              <CloseCircleOutlined style={{ marginRight: 8 }} />
              Unable to connect. Check your internet connection.
            </span>
            <Button size="small" type="primary" onClick={onRetry}>
              Retry
            </Button>
          </span>
        }
        type="error"
        banner
        closable={false}
      />
    );
  }

  return null;
};
```

#### 3. Create React Hook for Connection Management

**File:** `src/frontend/src/hooks/useSignalRConnection.ts`

**Requirements:**
- Manage SignalR connection lifecycle
- Provide connection state to components
- Handle reconnection logic
- Provide send message function with queuing

**Pattern:**
```typescript
import { useState, useEffect, useCallback } from 'react';
import { signalRService, ConnectionState } from '../services/signalrService';
import { useAuthStore } from '../stores/authStore'; // Assuming auth store exists

export const useSignalRConnection = () => {
  const [connectionState, setConnectionState] = useState<ConnectionState>(
    ConnectionState.Disconnected
  );
  const [reconnectAttempts, setReconnectAttempts] = useState(0);
  const { accessToken } = useAuthStore();

  useEffect(() => {
    if (!accessToken) return;

    // Connect to SignalR hub
    signalRService.connect(accessToken);

    // Listen for connection state changes
    const handleStateChange = (state: ConnectionState) => {
      setConnectionState(state);
      setReconnectAttempts(signalRService.getReconnectAttempts());
    };

    signalRService.on('connectionStateChanged', handleStateChange);

    return () => {
      // Cleanup if needed (don't disconnect as it's a singleton)
    };
  }, [accessToken]);

  const sendMessage = useCallback(async (message: string) => {
    await signalRService.sendMessage(message);
  }, []);

  const retryConnection = useCallback(async () => {
    if (accessToken) {
      await signalRService.connect(accessToken);
    }
  }, [accessToken]);

  const getDraftMessage = useCallback(() => {
    return signalRService.getDraftMessage();
  }, []);

  const clearDraftMessage = useCallback(() => {
    signalRService.clearDraftMessage();
  }, []);

  return {
    connectionState,
    reconnectAttempts,
    sendMessage,
    retryConnection,
    getDraftMessage,
    clearDraftMessage,
  };
};
```

#### 4. Implement API Retry Logic with Polly

**Prerequisites:**
- Install Polly resilience library: `npm install polly-js` or similar
- OR use built-in fetch retry with exponential backoff

**File:** `src/frontend/src/utils/retryPolicies.ts`

**Pattern:**
```typescript
export interface RetryOptions {
  maxAttempts: number;
  backoffIntervals: number[]; // milliseconds
  shouldRetry?: (error: any) => boolean;
}

export const defaultRetryOptions: RetryOptions = {
  maxAttempts: 3,
  backoffIntervals: [1000, 3000, 9000], // 1s, 3s, 9s
  shouldRetry: (error) => {
    // Retry on network errors or 5xx server errors
    return !error.response || error.response.status >= 500;
  },
};

export async function fetchWithRetry<T>(
  url: string,
  options: RequestInit,
  retryOptions: RetryOptions = defaultRetryOptions
): Promise<T> {
  let lastError: any;

  for (let attempt = 0; attempt < retryOptions.maxAttempts; attempt++) {
    try {
      const response = await fetch(url, options);

      if (!response.ok) {
        const error: any = new Error(`HTTP ${response.status}`);
        error.response = response;

        if (retryOptions.shouldRetry && !retryOptions.shouldRetry(error)) {
          throw error;
        }

        lastError = error;
      } else {
        return await response.json();
      }
    } catch (error) {
      lastError = error;

      if (retryOptions.shouldRetry && !retryOptions.shouldRetry(error)) {
        throw error;
      }
    }

    // Wait before retry (exponential backoff)
    if (attempt < retryOptions.maxAttempts - 1) {
      const delay = retryOptions.backoffIntervals[
        Math.min(attempt, retryOptions.backoffIntervals.length - 1)
      ];
      await new Promise((resolve) => setTimeout(resolve, delay));
    }
  }

  throw lastError;
}
```

**File:** `src/frontend/src/services/apiRetryService.ts`

**Pattern:**
```typescript
import { fetchWithRetry } from '../utils/retryPolicies';

export class ApiRetryService {
  private cache: Map<string, { data: any; timestamp: number }> = new Map();
  private cacheExpiryMs = 60000; // 1 minute

  async get<T>(url: string, useCache: boolean = true): Promise<T> {
    // Check cache first
    if (useCache) {
      const cached = this.getCached<T>(url);
      if (cached) {
        return cached;
      }
    }

    try {
      const data = await fetchWithRetry<T>(url, {
        method: 'GET',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${this.getAccessToken()}`,
        },
      });

      // Cache successful response
      this.setCache(url, data);

      return data;
    } catch (error) {
      // If offline or error, try to return stale cache
      if (useCache) {
        const staleCache = this.getCached<T>(url, true);
        if (staleCache) {
          console.warn('Returning stale cache due to error:', error);
          return staleCache;
        }
      }
      throw error;
    }
  }

  private getCached<T>(url: string, allowStale: boolean = false): T | null {
    const cached = this.cache.get(url);
    if (!cached) return null;

    const isExpired = Date.now() - cached.timestamp > this.cacheExpiryMs;
    if (isExpired && !allowStale) return null;

    return cached.data as T;
  }

  private setCache(url: string, data: any): void {
    this.cache.set(url, {
      data,
      timestamp: Date.now(),
    });
  }

  private getAccessToken(): string {
    // Get from auth store or local storage
    return localStorage.getItem('accessToken') || '';
  }
}

export const apiRetryService = new ApiRetryService();
```

#### 5. Integrate with ResponsiveChat Component

**File:** `src/frontend/src/components/ResponsiveChat.tsx` (modify)

**Changes Needed:**
```typescript
import { ConnectionStatus } from './ConnectionStatus';
import { useSignalRConnection } from '../hooks/useSignalRConnection';

export const ResponsiveChat: React.FC<ResponsiveChatProps> = ({
  // ... existing props
}) => {
  // Add connection management
  const {
    connectionState,
    reconnectAttempts,
    sendMessage: sendSignalRMessage,
    retryConnection,
    getDraftMessage,
    clearDraftMessage,
  } = useSignalRConnection();

  // Load draft message on mount
  useEffect(() => {
    const draft = getDraftMessage();
    if (draft) {
      setInputValue(draft);
      // Optionally show notification: "Draft message restored"
    }
  }, []);

  // Save draft to local storage when typing
  useEffect(() => {
    if (inputValue) {
      localStorage.setItem('bmadServer_draftMessage', inputValue);
    }
  }, [inputValue]);

  // Clear draft after successful send
  const handleSendMessage = async () => {
    if (!inputValue.trim()) return;

    try {
      await sendSignalRMessage(inputValue);
      setInputValue('');
      clearDraftMessage();
    } catch (error) {
      console.error('Failed to send message:', error);
      // Message is already queued by signalRService
    }
  };

  return (
    <div className="responsive-chat">
      {/* Add connection status banner */}
      <ConnectionStatus
        connectionState={connectionState}
        reconnectAttempts={reconnectAttempts}
        maxAttempts={5}
        onRetry={retryConnection}
      />
      
      {/* Existing chat UI */}
      {/* ... */}
    </div>
  );
};
```

### 🧪 Testing Strategy

#### Unit Tests

**File:** `src/frontend/src/services/__tests__/signalrService.test.ts`

Test Cases:
- Connection succeeds with valid token
- Reconnection attempts follow exponential backoff
- Max reconnection attempts respected
- Messages queued when disconnected
- Queued messages sent after reconnection
- Draft messages persisted to local storage
- Connection state changes emit events

**File:** `src/frontend/src/utils/__tests__/retryPolicies.test.ts`

Test Cases:
- Fetch retries on network failure
- Fetch retries on 5xx errors
- Fetch does not retry on 4xx errors
- Exponential backoff delays are correct
- Max retry attempts respected

#### Integration Tests

**File:** `src/frontend/src/components/__tests__/ResponsiveChat.integration.test.tsx`

Test Cases:
- Draft message restored on mount
- Connection status indicator updates on state change
- Retry button triggers reconnection
- Messages sent successfully when connected
- Messages queued when disconnected
- Queued messages sent after reconnection
- Draft cleared after successful send

#### Manual Testing Checklist

1. **Disconnect Network:**
   - Disable network connection
   - Verify "Reconnecting..." indicator appears
   - Verify reconnection attempts with delays
   - Type a message and verify it's saved to local storage

2. **Reconnect Network:**
   - Re-enable network
   - Verify "Connected" indicator appears
   - Verify queued messages are sent
   - Verify draft message is cleared

3. **Max Retries:**
   - Keep network disabled for 45+ seconds
   - Verify "Unable to connect" message after 5 attempts
   - Verify retry button appears
   - Click retry button and verify new connection attempt

4. **Server Unavailable:**
   - Stop backend server
   - Make API calls
   - Verify cached data is shown
   - Verify retry logic with backoff

### 🔐 Security Considerations

**Access Token Management:**
- Token refresh should trigger new SignalR connection
- Expired tokens should disconnect gracefully
- Store tokens securely (HttpOnly cookies for refresh tokens)

**Local Storage:**
- Draft messages are NOT sensitive but should be cleared on logout
- Connection state should not expose sensitive data

**Error Messages:**
- Don't expose internal connection details to users
- Log detailed errors server-side only

### 📊 Connection State Diagram

```
[Disconnected] --connect()--> [Connecting] --success--> [Connected]
                                    |
                                    |--failure--> [Reconnecting]
                                                        |
                                                        |--retry 1 (0s)--> [Reconnecting]
                                                        |--retry 2 (2s)--> [Reconnecting]
                                                        |--retry 3 (10s)--> [Reconnecting]
                                                        |--retry 4 (30s)--> [Reconnecting]
                                                        |--retry 5 fails--> [Disconnected]
                                                        |
                                                        |--success--> [Connected]
```

### 🔗 Dependencies

**Required NuGet/NPM Packages:**
- `@microsoft/signalr` (already installed in Story 3.1)
- Optional: `polly-js` or similar retry library (OR implement custom retry logic)

**Dependencies on Other Stories:**
- Story 3.1: SignalR Hub Setup (DONE) - provides ChatHub infrastructure
- Story 2.4: Session Persistence & Recovery (DONE) - provides SESSION_RESTORED event
- Story 10.1: Graceful Error Handling (ready-for-dev) - complementary error handling

**Backend Requirements:**
- ChatHub.OnConnectedAsync() - already implements session recovery
- ChatHub.SESSION_RESTORED event - already implemented
- SessionService.RecoverSessionAsync() - already implemented

### 📂 Files to Create/Modify

**New Files:**
1. `src/frontend/src/services/signalrService.ts` - SignalR connection management
2. `src/frontend/src/services/apiRetryService.ts` - API retry logic
3. `src/frontend/src/components/ConnectionStatus.tsx` - Connection status UI
4. `src/frontend/src/hooks/useSignalRConnection.ts` - Connection React hook
5. `src/frontend/src/utils/retryPolicies.ts` - Retry utility functions
6. `src/frontend/src/services/__tests__/signalrService.test.ts` - Unit tests
7. `src/frontend/src/utils/__tests__/retryPolicies.test.ts` - Unit tests
8. `src/frontend/src/components/__tests__/ResponsiveChat.integration.test.tsx` - Integration tests

**Modify Existing Files:**
1. `src/frontend/src/components/ResponsiveChat.tsx` - Add connection status and hooks
2. `src/frontend/package.json` - Add polly-js or retry library if needed

**No Backend Changes Needed** - All server-side logic already exists!

---

## Aspire Development Standards

### Rule 1: Use Aspire Service Defaults

This story is **frontend-focused** and does not require Aspire changes. However, the backend SignalR infrastructure already leverages:
- `builder.AddServiceDefaults()` provides OpenTelemetry logging for SignalR events
- Structured logging for connection/disconnection events
- Health checks for SignalR connectivity

**Reference:** [PROJECT-WIDE-RULES.md](../../../PROJECT-WIDE-RULES.md) - Rule 4: OpenTelemetry from Day 1

### Rule 2: Documentation Sources

**Primary:** https://learn.microsoft.com/en-us/aspnet/core/signalr/javascript-client
**Secondary:** https://github.com/SignalR/SignalR/tree/main/clients/ts

### PostgreSQL Connection Pattern

This story does NOT require database changes. Session recovery is already implemented in Story 2.4.

---

## 🎓 Learning from Previous Stories

### Story 10.1 Insights (Graceful Error Handling)

**Good Patterns to Follow:**
- Use ProblemDetails RFC 7807 format for errors
- Log full error details server-side, show user-friendly messages client-side
- Include correlation IDs for tracing
- Add actionable guidance ("Try again", "Check connection")

**Frontend Error Handling Patterns:**
- Show error messages in connection status banner (similar to validation errors)
- Provide retry button for user control
- Don't expose technical details to users

### Story 3.1 Insights (SignalR Hub Setup)

**Existing Patterns:**
- SignalR already configured with JWT authentication
- ChatHub handles OnConnectedAsync and OnDisconnectedAsync
- SESSION_RESTORED event already implemented

**What This Story Adds:**
- **Frontend** automatic reconnection (server already supports it)
- Exponential backoff retry logic
- Message queuing during disconnection
- Connection status UI

### Story 2.4 Insights (Session Persistence & Recovery)

**Existing Session Recovery:**
- SessionService.RecoverSessionAsync() already implemented
- 60-second recovery window for seamless reconnection
- Conversation history and workflow state restored

**What This Story Adds:**
- Frontend integration with SESSION_RESTORED event
- UI feedback during reconnection
- Draft message preservation

---

## References

- **Aspire Rules:** [PROJECT-WIDE-RULES.md](../../../PROJECT-WIDE-RULES.md)
- **Aspire Docs:** https://aspire.dev
- **SignalR JavaScript Client:** https://learn.microsoft.com/en-us/aspnet/core/signalr/javascript-client
- **Source:** [epics.md - Epic 10, Story 10.2](../planning-artifacts/epics.md#story-102-connection-recovery--retry)
- **Architecture:** [architecture.md](../planning-artifacts/architecture.md) - Real-time Communication section
- **PRD:** [prd.md](../planning-artifacts/prd.md) - NFR6 (Session recovery within 60s)
- **Existing Hub:** [ChatHub.cs](../../src/bmadServer.ApiService/Hubs/ChatHub.cs) - Session recovery logic

---

## Dev Agent Record

### Agent Model Used

_To be filled by dev agent during implementation_

### Debug Log References

_To be filled by dev agent during implementation_

### Completion Notes List

_To be filled by dev agent during implementation_

### File List

_To be filled by dev agent during implementation_
