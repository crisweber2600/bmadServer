---
title: 'Frontend Epic Gap Comprehensive Implementation'
slug: 'frontend-epic-gap-comprehensive'
created: '2026-01-29'
status: 'ready-for-dev'
stepsCompleted: [1, 2, 3, 4]
adversarial_review: 'passed'
party_mode_review: 'approved'
review_notes: '15 adversarial findings addressed and validated by TEA, Architect, UX Designer agents'
tech_stack:
  - React 18.x (functional components, hooks)
  - TypeScript 5.x (strict mode)
  - Ant Design 5.x (Avatar, Tooltip, Typography, List, Badge, Tag, Timeline, Drawer, Modal, Input.TextArea)
  - Vite 6.x (dev server, build)
  - Vitest 2.x (unit testing)
  - Playwright (E2E testing with Page Object Model)
  - SignalR (@microsoft/signalr 8.x)
files_to_modify:
  - src/frontend/src/components/AgentAttribution.tsx (add relevanceScore prop, relevance badge)
  - src/frontend/src/components/CommandPalette.tsx (add exit commands, onExitPartyMode callback)
  - src/frontend/src/components/DecisionAttributionBanner.tsx (add lock/unlock controls, version link)
  - src/frontend/src/components/TypingIndicator.tsx (extend for multi-user typing)
  - src/frontend/src/hooks/useSignalRHandoffs.ts (extend with USER_ONLINE, USER_OFFLINE, USER_TYPING handlers)
  - src/frontend/src/components/index.ts (add new component exports)
files_to_create:
  - src/frontend/src/types/decisions.ts
  - src/frontend/src/types/persona.ts
  - src/frontend/src/hooks/useDecisions.ts
  - src/frontend/src/hooks/usePresence.ts
  - src/frontend/src/components/ErrorBoundary.tsx
  - src/frontend/src/components/ConnectionStatusBanner.tsx
  - src/frontend/src/components/AgentRosterGrid.tsx
  - src/frontend/src/components/TTSStatusIndicator.tsx
  - src/frontend/src/components/ModeratorAlert.tsx
  - src/frontend/src/components/PresenceIndicator.tsx
  - src/frontend/src/components/PersonaToggle.tsx
  - src/frontend/src/components/VersionHistoryPanel.tsx
  - src/frontend/src/components/DiffViewer.tsx
  - src/frontend/src/components/ConflictAlert.tsx
  - src/frontend/src/components/ConflictResolutionPanel.tsx
  - src/frontend/src/components/ReviewRequestForm.tsx
  - src/frontend/src/components/CheckpointList.tsx
  - src/frontend/src/components/GlossaryPanel.tsx
code_patterns:
  - Functional components with explicit React.FC<Props> typing
  - Props interfaces exported alongside components
  - CSS modules (ComponentName.css in same directory)
  - Ant Design components for UI (Avatar, Tag, Badge, Tooltip, Timeline, Drawer)
  - Color hash from ID for avatars (getColorHash pattern)
  - Intl.DateTimeFormat for timestamps
  - useCallback for stable callbacks
  - useState + useEffect for async data
  - Barrel exports in index.ts
test_patterns:
  - Unit: ComponentName.test.tsx with @testing-library/react
  - vi.mock() for hooks (see ChatWithHandoffs.test.tsx)
  - render + screen + expect pattern
  - data-testid attributes for queries
  - E2E: tests/epic{N}/{story}.spec.ts (ConnectionStatusBanner, VersionHistory, ConflictResolution)
  - Playwright fixtures (ChatPage, SignalRHelper)
api_schemas:
  - DecisionResponse: { Id, WorkflowInstanceId, StepId, DecisionType, Value, DecidedBy, DecidedAt, CurrentVersion, IsLocked, LockedBy, LockedAt, LockReason, Status }
  - DecisionVersionResponse: { Id, VersionNumber, Value, ModifiedBy, ModifiedAt, ChangeReason, Question, Options, Reasoning, Context }
  - ConflictResponse: { Id, DecisionId1, DecisionId2, ConflictType, Description, Severity, Status, DetectedAt, ResolvedAt, Resolution }
  - PersonaType: Business=0, Technical=1, Hybrid=2
signalr_events:
  - AGENT_HANDOFF: { FromAgentId, FromAgentName, ToAgentId, ToAgentName, StepName?, Reason?, Timestamp }
  - USER_ONLINE: { UserId, DisplayName, IsOnline, LastSeen }
  - USER_OFFLINE: { UserId, DisplayName, IsOnline, LastSeen }
  - USER_TYPING: { UserId, DisplayName, WorkflowId }
  - SESSION_RESTORED: { Id, WorkflowName, CurrentStep, ConversationHistory, PendingInput, Message }
party_mode_feedback:
  - "Winston: Phase into 3 deployable increments (Foundation → Party Mode → Decision Mgmt)"
  - "Murat: Add Testing Strategy Matrix, E2E for ConnectionStatusBanner, SignalR reconnection tests"
  - "Sally: Remove PersonaSelector (redundant), add reconnection UX flow, typing allowed during reconnect"
  - "Winston: CONTEXT_UPDATE event does NOT exist in backend - SharedContextPanel/ContextTimeline DEFERRED"
---

# Tech-Spec: Frontend Epic Gap Comprehensive Implementation

**Created:** 2026-01-29

## Overview

### Problem Statement

The frontend implementation has significant gaps against epic requirements, particularly:
- **Epic 3** (Chat Interface): 85% complete - missing multi-line input, character count, draft persistence
- **Epic 5** (Multi-Agent): 75% complete - missing shared context panel, context evolution timeline
- **Epic 6** (Decision Management): 40% complete - missing version history, diff viewer, lock controls, conflict UI
- **Epic 7** (Collaboration): 25% complete - missing presence indicators, checkpoint UI, input queue
- **Epic 8** (Persona Translation): 20% complete - missing persona selector, toggle, glossary
- **Epic 10** (Error Handling): 35% complete - missing connection status banner, error boundaries
- **Party Mode UI**: Missing agent roster grid, relevance badges, TTS indicator, exit commands

### Solution

Implement a phased set of React components and hooks that close the identified gaps, leveraging existing backend APIs (`IDecisionService`, `IContextAnalysisService`, `IContributionMetricsService`, SignalR events) and following established patterns (functional components, Vitest/Playwright tests, Ant Design).

### Scope

**In Scope (Revised per Party Mode Feedback):**

**Phase A: Foundation (Ship First)**
1. TypeScript interfaces (`types/decisions.ts`, `types/persona.ts`)
2. API hooks (`useDecisions.ts`, `usePresence.ts`)
3. `ErrorBoundary` component
4. `ConnectionStatusBanner` extracted from ChatWithHandoffs (with reconnection UX)

**Phase B: Party Mode UI**
5. `AgentRosterGrid` component with relevance badges
6. `AgentAttribution.tsx` modification (add `relevanceScore` prop)
7. `TTSStatusIndicator` component
8. `CommandPalette.tsx` modification (exit commands: `*exit`, `goodbye`, `end party`, `quit`)
9. `ModeratorAlert` component

**Phase C: Collaboration & Presence**
10. `PresenceIndicator` component
11. `TypingIndicator.tsx` modification (multi-user support)
12. `useSignalRHandoffs.ts` extension (`USER_ONLINE`, `USER_OFFLINE`, `USER_TYPING`)
13. `PersonaToggle` component (replaces PersonaSelector per Sally's feedback)

**Phase D: Decision Management**
14. `VersionHistoryPanel` component
15. `DiffViewer` component
16. `DecisionAttributionBanner.tsx` modification (lock/unlock controls)
17. `ConflictAlert` component
18. `ConflictResolutionPanel` component
19. `ReviewRequestForm` component
20. `CheckpointList` component
21. `GlossaryPanel` component

**Out of Scope:**
- ~~SharedContextPanel, ContextTimeline~~ → **DEFERRED** - `CONTEXT_UPDATE` SignalR event does not exist in backend
- ~~PersonaSelector~~ → **REMOVED** - Redundant per UX review; PersonaToggle sufficient for 3 options
- ~~useSharedContext hook, types/context.ts~~ → **DEFERRED** with SharedContextPanel
- Epic 11 (Security/Auth pages) - requires separate auth architecture spec
- Epic 12 (Admin Dashboard) - requires separate admin module spec
- Epic 13 (Webhooks) - requires separate integration spec
- Backend changes (all required APIs verified to exist)

---

## Context for Development

### Codebase Patterns

**Component Pattern (from AgentAttribution.tsx):**
```tsx
import React, { useState } from 'react';
import { Avatar, Tooltip, Typography, Divider } from 'antd';
import './ComponentName.css';

const { Text } = Typography;

export interface ComponentNameProps {
  requiredProp: string;
  optionalProp?: string;
  onAction?: (data: T) => void;
}

export const ComponentName: React.FC<ComponentNameProps> = ({
  requiredProp,
  optionalProp,
  onAction,
}) => {
  const [state, setState] = useState(false);
  
  // Color hash pattern (reuse across components)
  const getColorHash = (id: string): string => {
    let hash = 0;
    for (let i = 0; i < id.length; i++) {
      const char = id.charCodeAt(i);
      hash = (hash << 5) - hash + char;
      hash = hash & hash;
    }
    const hue = Math.abs(hash) % 360;
    return `hsl(${hue}, 70%, 60%)`;
  };

  // Timestamp formatting pattern
  const formatTimestamp = (date: Date): string => {
    return new Intl.DateTimeFormat('en-US', {
      hour: '2-digit', minute: '2-digit',
      month: 'short', day: 'numeric', hour12: true,
    }).format(date);
  };

  return (
    <div className="component-name" role="region" aria-label="Component description">
      {/* Content */}
    </div>
  );
};
```

**Hook Pattern (from useSignalRHandoffs.ts):**
```tsx
export interface UseHookOptions {
  onEvent?: (data: T) => void;
  debug?: boolean;
}

export function useHookName(options?: UseHookOptions) {
  const { onEvent, debug = false } = options || {};
  const [state, setState] = useState<T>(initial);
  const connectionRef = useRef<Connection | null>(null);

  const log = useCallback((msg: string, data?: unknown) => {
    if (debug) console.log(`[HookName] ${msg}`, data || '');
  }, [debug]);

  useEffect(() => {
    // Initialize connection
    return () => { /* Cleanup */ };
  }, [dependencies]);

  return { state, methods };
}
```

**Test Pattern (from ChatWithHandoffs.test.tsx):**
```tsx
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import { ComponentName } from './ComponentName';

vi.mock('../hooks/useHookName', () => ({
  useHookName: vi.fn().mockReturnValue({
    state: 'initial',
    methods: vi.fn(),
  }),
}));

describe('ComponentName', () => {
  beforeEach(() => { vi.clearAllMocks(); });

  it('should render correctly', () => {
    render(<ComponentName requiredProp="value" />);
    expect(screen.getByTestId('component-name')).toBeInTheDocument();
  });

  it('should handle events', async () => {
    const onAction = vi.fn();
    render(<ComponentName requiredProp="value" onAction={onAction} />);
    // trigger and assert
  });
});
```

### Files to Reference

| File | Purpose | Key Patterns |
| ---- | ------- | ------------ |
| [AgentAttribution.tsx](src/frontend/src/components/AgentAttribution.tsx) | Agent display with avatar, tooltip, capabilities | `getColorHash`, `formatTimestamp`, `Tooltip` with content, `variant` prop |
| [DecisionAttributionBanner.tsx](src/frontend/src/components/DecisionAttributionBanner.tsx) | Decision display with confidence bar | Expandable reasoning, `confidence` progressbar |
| [ChatInput.tsx](src/frontend/src/components/ChatInput.tsx) | Message input with draft persistence | `localStorage` draft, debounced save, `TextArea` with `autoSize`, char counter |
| [CommandPalette.tsx](src/frontend/src/components/CommandPalette.tsx) | Slash command menu | Arrow key navigation, filter, `onSelect`, click outside |
| [TypingIndicator.tsx](src/frontend/src/components/TypingIndicator.tsx) | Animated typing dots | CSS animation, `aria-live="polite"` |
| [ChatWithHandoffs.tsx](src/frontend/src/components/ChatWithHandoffs.tsx) | Chat with SignalR handoff events | Connection state banner pattern, merged message list |
| [useSignalRHandoffs.ts](src/frontend/src/hooks/useSignalRHandoffs.ts) | SignalR connection + events | `HubConnectionBuilder`, exponential backoff, event handlers |
| [ChatWithHandoffs.test.tsx](src/frontend/src/components/ChatWithHandoffs.test.tsx) | Test with mocked hooks | `vi.mock()` pattern, async import for mock updates |

### Technical Decisions

1. **ChatInput.tsx already implements draft persistence** - No additional hook needed; Epic 3.3 is actually COMPLETE
2. **ChatWithHandoffs.tsx has connection state banner** - Extract to reusable `ConnectionStatusBanner` component
3. **API response models match backend exactly** - TypeScript interfaces will mirror C# DTOs
4. **Extend useSignalRHandoffs** rather than create separate hooks - Add `USER_ONLINE`, `USER_TYPING`, `CONTEXT_UPDATE` handlers
5. **Lock controls in DecisionAttributionBanner** - Add `onLock`, `onUnlock` props, show controls when `canLock` prop is true
6. **PersonaType enum** - `Business=0, Technical=1, Hybrid=2` - Match backend exactly
7. **Exit commands in CommandPalette** - Add to existing `COMMANDS` array, check for party mode context

### API Endpoints (Backend Already Exists)

| Endpoint | Method | Purpose |
| -------- | ------ | ------- |
| `/api/v1/workflows/{id}/decisions` | GET | List decisions for workflow |
| `/api/v1/decisions/{id}` | GET | Get decision details |
| `/api/v1/decisions/{id}` | PUT | Update decision (creates new version) |
| `/api/v1/decisions/{id}/versions` | GET | Get version history |
| `/api/v1/decisions/{id}/versions/{v1}/diff/{v2}` | GET | Get diff between versions |
| `/api/v1/decisions/{id}/revert` | POST | Revert to previous version |
| `/api/v1/decisions/{id}/lock` | POST | Lock decision |
| `/api/v1/decisions/{id}/unlock` | POST | Unlock decision |
| `/api/v1/decisions/{id}/reviews` | POST | Request review |
| `/api/v1/workflows/{id}/conflicts` | GET | List conflicts for workflow |
| `/api/v1/conflicts/{id}/resolve` | POST | Resolve conflict |
| `/api/v1/conflicts/{id}/override` | POST | Override conflict warning |
| `/api/v1/sessions/{id}/persona` | PUT | Switch session persona |

### SignalR Events (Backend Already Exists)

| Event | Payload | Purpose |
| ----- | ------- | ------- |
| `AGENT_HANDOFF` | `{ FromAgentId, FromAgentName, ToAgentId, ToAgentName, StepName?, Reason?, Timestamp }` | Agent transition |
| `USER_ONLINE` | `{ UserId, DisplayName, IsOnline: true, LastSeen }` | User joined workflow |
| `USER_OFFLINE` | `{ UserId, DisplayName, IsOnline: false, LastSeen }` | User left workflow |
| `USER_TYPING` | `{ UserId, DisplayName, WorkflowId }` | User typing indicator |
| `SESSION_RESTORED` | `{ Id, WorkflowName, CurrentStep, ConversationHistory, PendingInput, Message }` | Session recovery |
| `ReceiveMessage` | `{ Role, Content, PersonaType, WasTranslated, ... }` | Chat message with persona |

---

## Implementation Plan

### Phase A: Foundation (Ship First - Prerequisite for all phases)

#### Task A.1: TypeScript Decision Types
**File:** `src/frontend/src/types/decisions.ts` (NEW)
**Action:** Create TypeScript interfaces matching backend DTOs

```typescript
// Required interfaces:
export interface DecisionResponse { Id: string; WorkflowInstanceId: string; StepId: string; DecisionType: string; Value: unknown; DecidedBy: string; DecidedAt: string; CurrentVersion: number; IsLocked: boolean; LockedBy?: string; LockedAt?: string; LockReason?: string; Status: string; }
export interface DecisionVersionResponse { Id: string; VersionNumber: number; Value: unknown; ModifiedBy: string; ModifiedAt: string; ChangeReason?: string; }
export interface ConflictResponse { Id: string; DecisionId1: string; DecisionId2: string; ConflictType: string; Description: string; Severity: string; Status: string; DetectedAt: string; ResolvedAt?: string; Resolution?: string; }
export interface DecisionVersionDiffResponse { FromVersion: number; ToVersion: number; Changes: DiffChange[]; }
export interface DiffChange { Field: string; OldValue: unknown; NewValue: unknown; }
```

**Acceptance Criteria:**
```gherkin
Given the types/decisions.ts file exists
When imported by useDecisions hook
Then all API response shapes are correctly typed
And TypeScript compilation succeeds with strict mode
```

#### Task A.2: TypeScript Persona Types
**File:** `src/frontend/src/types/persona.ts` (NEW)
**Action:** Create PersonaType enum and related types

```typescript
export enum PersonaType { Business = 0, Technical = 1, Hybrid = 2 }
export interface PersonaSwitchRequest { PersonaType: PersonaType; }
export interface PersonaSwitchResponse { Success: boolean; NewPersona: PersonaType; PreviousPersona: PersonaType; }
```

**Acceptance Criteria:**
```gherkin
Given PersonaType enum is defined
When used in PersonaToggle component
Then values match backend enum (Business=0, Technical=1, Hybrid=2)
```

#### Task A.2b: Types Barrel Export
**File:** `src/frontend/src/types/index.ts` (NEW)
**Action:** Create barrel export for all type modules

```typescript
export * from './decisions';
export * from './persona';
```

**Acceptance Criteria:**
```gherkin
Given types/index.ts exists
When importing { DecisionResponse, PersonaType } from '../types'
Then imports resolve correctly without specifying subpath
```

#### Task A.3: useDecisions Hook
**File:** `src/frontend/src/hooks/useDecisions.ts` (NEW)
**Action:** Create hook for decision CRUD operations with comprehensive error handling

**Required Operations:**
- `getDecisions(workflowId)` - List all decisions
- `getDecision(decisionId)` - Get single decision
- `updateDecision(decisionId, value, reason)` - Update decision (creates version)
- `getVersionHistory(decisionId)` - Get version list
- `getDiff(decisionId, v1, v2)` - Get version diff
- `revertToVersion(decisionId, versionNumber)` - Revert decision
- `lockDecision(decisionId, reason)` - Lock decision
- `unlockDecision(decisionId)` - Unlock decision
- `requestReview(decisionId, reviewerIds, deadline?)` - Request review

**Error Handling Strategy:**
```typescript
// All API calls use this wrapper:
const apiCall = async <T>(fn: () => Promise<T>): Promise<{ data?: T; error?: ApiError }> => {
  try {
    const data = await fn();
    return { data };
  } catch (err) {
    if (err.status === 409) return { error: { code: 'CONFLICT', message: err.body?.message || 'Resource conflict' } };
    if (err.status === 403) return { error: { code: 'FORBIDDEN', message: 'Permission denied' } };
    if (err.status === 404) return { error: { code: 'NOT_FOUND', message: 'Resource not found' } };
    return { error: { code: 'UNKNOWN', message: err.message || 'An error occurred' } };
  }
};
```

**Acceptance Criteria:**
```gherkin
Given useDecisions hook is called with workflowId
When getDecisions() is invoked
Then it returns DecisionResponse[] from GET /api/v1/workflows/{id}/decisions

Given lockDecision() is called with decisionId
When API returns 409 Conflict (already locked)
Then error state includes "Decision is already locked by {user}"
And error.code equals 'CONFLICT'

Given getVersionHistory() is called
When successful
Then it returns DecisionVersionResponse[] sorted by VersionNumber desc

Given updateDecision() is called with value and reason
When successful
Then POST /api/v1/decisions/{id} is called with { value, changeReason }
And the returned DecisionResponse has incremented CurrentVersion

Given getDiff() is called with v1=2, v2=5
When successful
Then GET /api/v1/decisions/{id}/versions/2/diff/5 returns DiffChange[]

Given revertToVersion() is called with versionNumber=3
When successful
Then POST /api/v1/decisions/{id}/revert is called with { versionNumber: 3 }
And onReverted callback is invoked

Given unlockDecision() is called
When successful
Then POST /api/v1/decisions/{id}/unlock is called
And decision.IsLocked becomes false

Given requestReview() is called with reviewerIds and deadline
When successful
Then POST /api/v1/decisions/{id}/reviews is called
And returns ReviewRequestResponse with status 'pending'

Given any API call fails with network error
When error is caught
Then error state is set with code 'NETWORK' and isRetryable=true
And retryCount increments on retry
```

#### Task A.4: usePresence Hook
**File:** `src/frontend/src/hooks/usePresence.ts` (NEW)
**Action:** Create hook for tracking online users via SignalR with error handling

**Error Handling Strategy:**
```typescript
// SignalR event parsing with fallback:
const parseEvent = <T>(payload: unknown, schema: ZodSchema<T>): T | null => {
  const result = schema.safeParse(payload);
  if (!result.success) {
    console.warn('[usePresence] Malformed event payload:', result.error);
    return null;
  }
  return result.data;
};
```

**Acceptance Criteria:**
```gherkin
Given usePresence hook is initialized with workflowId
When USER_ONLINE event is received
Then onlineUsers state adds the user

Given USER_OFFLINE event is received
When user was previously online
Then onlineUsers state removes the user

Given USER_TYPING event is received
When user is online
Then typingUsers state adds user with 3-second timeout

Given a malformed SignalR event is received
When parsing fails
Then a warning is logged to console
And the event is ignored (no state change)
And the hook does not throw

Given SignalR connection drops
When reconnection succeeds
Then onlineUsers state is cleared and refetched from server
```

#### Task A.5: ErrorBoundary Component
**File:** `src/frontend/src/components/ErrorBoundary.tsx` (NEW)
**File:** `src/frontend/src/components/ErrorBoundary.css` (NEW)
**File:** `src/frontend/src/components/ErrorBoundary.test.tsx` (NEW)
**Action:** Create React error boundary with retry capability

**Default Props:**
```typescript
interface ErrorBoundaryProps {
  children: React.ReactNode;
  fallback?: React.ReactNode;           // Default: built-in error UI
  onError?: (error: Error) => void;     // Default: console.error
  onReset?: () => void;                 // Default: no-op
  resetKeys?: unknown[];                // Default: []
}
```

**Acceptance Criteria:**
```gherkin
Given a child component throws an error
When the error boundary catches it
Then a fallback UI shows "Something went wrong" with error details
And a "Try Again" button is visible
And onError callback is invoked with the Error object

Given user clicks "Try Again"
When the button is clicked
Then the error state resets and children re-render
And onReset callback is invoked

Given error boundary is unmounted
When remounted
Then previous error state is cleared

Given no fallback prop is provided
When error occurs
Then the default fallback UI is displayed (not blank)

Given resetKeys prop changes
When any key in the array changes
Then the error state automatically resets
```

#### Task A.6: ConnectionStatusBanner Component
**File:** `src/frontend/src/components/ConnectionStatusBanner.tsx` (NEW)
**File:** `src/frontend/src/components/ConnectionStatusBanner.css` (NEW)
**File:** `src/frontend/src/components/ConnectionStatusBanner.test.tsx` (NEW)
**Action:** Extract and enhance connection banner from ChatWithHandoffs pattern

**State Machine:**
```
┌─────────────┐    disconnect    ┌──────────────┐
│  connected  │ ───────────────► │ disconnected │
│  (hidden)   │                  │   (amber)    │
└─────────────┘                  └──────────────┘
       ▲                                │
       │                                │ auto-retry
       │                                ▼
       │                         ┌──────────────┐
       │    success              │ reconnecting │
       │◄────────────────────────│   (yellow)   │
       │                         └──────────────┘
       │                                │
       │                                │ success
       │                                ▼
       │                         ┌──────────────┐
       │    2s timer             │  reconnected │
       └─────────────────────────│   (green)    │
                                 └──────────────┘
```

**Edge Case Handling:**
- If disconnect occurs during "reconnected" display → immediately show "disconnected"
- If reconnect succeeds during "reconnected" display → reset 2s timer
- Debounce state transitions by 100ms to prevent flicker

**Default Props:**
```typescript
interface ConnectionStatusBannerProps {
  connectionState: 'connected' | 'disconnected' | 'reconnecting';
  attemptNumber?: number;        // Default: 0
  maxAttempts?: number;          // Default: 10
  reconnectedDisplayMs?: number; // Default: 2000
  onRetryClick?: () => void;     // Optional manual retry button
}
```

**Acceptance Criteria:**
```gherkin
Given connectionState is "connected"
When the banner is rendered
Then it is visually hidden (height: 0, opacity: 0)

Given connectionState is "disconnected"
When the banner is rendered
Then it shows amber warning "⚠️ Connection lost. Retrying..."
And the chat input remains enabled (per Sally's UX feedback)

Given connectionState is "reconnecting" with attemptNumber=3
When the banner is rendered
Then it shows "🔄 Reconnecting... Attempt 3 of 10"

Given reconnection succeeds after being disconnected
When connectionState changes to "connected"
Then banner shows "✓ Reconnected" for 2 seconds then hides

Given banner is showing "✓ Reconnected"
When disconnect occurs within 2 seconds
Then banner immediately switches to "⚠️ Connection lost"
And the 2-second timer is cancelled

Given rapid state changes (connected → disconnected → connected within 200ms)
When debounce is active
Then only the final state is displayed (no flicker)
```

---

### Phase B: Party Mode UI (High User Impact)

#### Task B.1: AgentRosterGrid Component
**File:** `src/frontend/src/components/AgentRosterGrid.tsx` (NEW)
**File:** `src/frontend/src/components/AgentRosterGrid.css` (NEW)
**File:** `src/frontend/src/components/AgentRosterGrid.test.tsx` (NEW)
**Action:** Create responsive grid displaying party mode agents

**Acceptance Criteria:**
```gherkin
Given a list of 3 agents with metadata
When AgentRosterGrid is rendered
Then it displays a 3-column grid on desktop (>768px)
And a 1-column stack on mobile (≤768px)

Given each agent card
When rendered
Then it shows Avatar (using getColorHash), name, role, and first 2 capabilities as Tags

Given an agent has relevanceScore > 0.7
When rendered
Then a green "🎯 Highly Relevant" Badge appears

Given an agent card is clicked
When the click event fires
Then onAgentSelect(agentId) is invoked
```

#### Task B.2: AgentAttribution Relevance Badge
**File:** `src/frontend/src/components/AgentAttribution.tsx` (MODIFY)
**Action:** Add optional `relevanceScore?: number` prop with visual indicator

**Modification Point:** In `AgentAttributionProps` interface, add new prop:
```typescript
// Add after 'children?: React.ReactNode;' in props interface:
relevanceScore?: number;
```

**Modification Point:** In the component JSX, after the Avatar render (inside the agent-info div), add relevance badge:
```typescript
// Add after {agentName && <Text strong>{agentName}</Text>}:
{relevanceScore !== undefined && (
  <Tag color={relevanceScore > 0.7 ? 'green' : relevanceScore > 0.4 ? 'blue' : 'default'}>
    {relevanceScore > 0.7 ? 'Highly Relevant' : relevanceScore > 0.4 ? 'Relevant' : 'Low Relevance'}
  </Tag>
)}
```

**Acceptance Criteria:**
```gherkin
Given AgentAttribution with relevanceScore={0.85}
When rendered in 'block' variant
Then a green Tag "Highly Relevant" appears after the agent name

Given AgentAttribution with relevanceScore={0.5}
When rendered
Then a blue Tag "Relevant" appears

Given AgentAttribution without relevanceScore prop
When rendered
Then no relevance badge appears (backward compatible)
```

#### Task B.3: TTSStatusIndicator Component
**File:** `src/frontend/src/components/TTSStatusIndicator.tsx` (NEW)
**File:** `src/frontend/src/components/TTSStatusIndicator.css` (NEW)
**File:** `src/frontend/src/components/TTSStatusIndicator.test.tsx` (NEW)
**Action:** Create TTS playback state indicator

**TTS Integration Point:**
The component receives TTS state via props from a parent component that manages Web Speech API or external TTS service. The parent is responsible for:
1. Calling `speechSynthesis.speak()` and tracking `onstart`/`onend` events
2. Passing `isPlaying`, `agentName`, and `onStop` props to this indicator
3. Handling the `onStop` callback by calling `speechSynthesis.cancel()`

**Example Parent Integration:**
```typescript
// In ChatWithHandoffs or similar parent:
const [ttsState, setTtsState] = useState({ isPlaying: false, agentName: '' });

const speakMessage = (text: string, agentName: string) => {
  const utterance = new SpeechSynthesisUtterance(text);
  utterance.onstart = () => setTtsState({ isPlaying: true, agentName });
  utterance.onend = () => setTtsState({ isPlaying: false, agentName: '' });
  speechSynthesis.speak(utterance);
};

const stopTts = () => {
  speechSynthesis.cancel();
  setTtsState({ isPlaying: false, agentName: '' });
};

// Render:
<TTSStatusIndicator 
  isPlaying={ttsState.isPlaying} 
  agentName={ttsState.agentName}
  onStop={stopTts}
/>
```

**Stale State Fallback:**
```typescript
// Add 30-second timeout to prevent stuck "playing" state:
useEffect(() => {
  if (isPlaying) {
    const timeout = setTimeout(() => onStop?.(), 30000);
    return () => clearTimeout(timeout);
  }
}, [isPlaying, onStop]);
```

**Acceptance Criteria:**
```gherkin
Given isPlaying={true} and agentName="Winston"
When TTSStatusIndicator is rendered
Then it shows pulsing SoundOutlined icon with "Winston speaking..."

Given isPlaying={false}
When rendered
Then it shows muted AudioMutedOutlined icon with "TTS idle"

Given user clicks the indicator while playing
When onClick fires
Then onStop() callback is invoked

Given isPlaying={true} for more than 30 seconds
When timeout fires
Then onStop() is automatically invoked (stale state recovery)
```

#### Task B.4: CommandPalette Exit Commands
**File:** `src/frontend/src/components/CommandPalette.tsx` (MODIFY)
**Action:** Add party mode exit commands to COMMANDS array

**Modification Point:** In the `COMMANDS` constant array (find `const COMMANDS = [`), add exit commands:
```typescript
// Add to COMMANDS array (conditionally shown when isPartyMode=true):
{ name: '/exit', description: 'Exit party mode', partyModeOnly: true },
{ name: '/goodbye', description: 'End conversation and exit', partyModeOnly: true },
```

**Modification Point:** In `CommandPaletteProps` interface, add:
```typescript
isPartyMode?: boolean;        // Default: false
onExitPartyMode?: () => void; // Called when exit command selected
```

**Modification Point:** In the filter logic, add party mode filtering:
```typescript
// Modify the filter to exclude partyModeOnly commands when not in party mode:
const filteredCommands = COMMANDS.filter(cmd => 
  cmd.name.toLowerCase().includes(input.toLowerCase()) &&
  (!cmd.partyModeOnly || isPartyMode)
);
```

**New Prop:** Add `onExitPartyMode?: () => void` to CommandPaletteProps
**New Prop:** Add `isPartyMode?: boolean` to show/hide exit commands

**Acceptance Criteria:**
```gherkin
Given isPartyMode={true}
When user types "/" 
Then "/exit" and "/goodbye" commands appear in the palette

Given user selects "/exit"
When the selection is made
Then onExitPartyMode() callback is invoked

Given isPartyMode={false}
When user types "/"
Then exit commands are not shown
```

#### Task B.5: ModeratorAlert Component
**File:** `src/frontend/src/components/ModeratorAlert.tsx` (NEW)
**File:** `src/frontend/src/components/ModeratorAlert.css` (NEW)
**File:** `src/frontend/src/components/ModeratorAlert.test.tsx` (NEW)
**Action:** Create circular discussion warning banner

**Acceptance Criteria:**
```gherkin
Given message="Discussion appears to be going in circles"
When ModeratorAlert is rendered
Then it shows warning banner with message and "Acknowledge" button

Given user clicks "Acknowledge"
When the button is clicked
Then onAcknowledge() callback is invoked
And the alert dismisses with fade-out animation

Given severity="warning"
When rendered
Then banner has amber background

Given severity="info" 
When rendered
Then banner has blue background
```

---

### Phase C: Collaboration & Presence

#### Task C.1: PresenceIndicator Component
**File:** `src/frontend/src/components/PresenceIndicator.tsx` (NEW)
**File:** `src/frontend/src/components/PresenceIndicator.css` (NEW)
**File:** `src/frontend/src/components/PresenceIndicator.test.tsx` (NEW)
**Action:** Create online/offline dot indicator

**Acceptance Criteria:**
```gherkin
Given isOnline={true}
When PresenceIndicator is rendered
Then a green pulsing dot (8px) appears

Given isOnline={false}
When rendered
Then a gray dot appears without animation

Given users=[{name: "Alice"}, {name: "Bob"}] and showTooltip={true}
When hovering on the indicator
Then Tooltip shows "Alice, Bob online"
```

#### Task C.2: Multi-User TypingIndicator
**File:** `src/frontend/src/components/TypingIndicator.tsx` (MODIFY)
**Action:** Support array of typing users while maintaining backward compatibility

**Props Migration Strategy (Backward Compatible):**
```typescript
interface TypingIndicatorProps {
  // DEPRECATED: Use typingUsers instead. Will be removed in v2.0.
  agentName?: string;
  // NEW: Array of typing user names
  typingUsers?: string[];
}

// Internal normalization:
const normalizedUsers = useMemo(() => {
  if (typingUsers && typingUsers.length > 0) return typingUsers;
  if (agentName) {
    console.warn('[TypingIndicator] agentName prop is deprecated. Use typingUsers instead.');
    return [agentName];
  }
  return [];
}, [agentName, typingUsers]);
```

**Acceptance Criteria:**
```gherkin
Given typingUsers={["Alice"]}
When TypingIndicator is rendered
Then it shows "Alice is typing..."

Given typingUsers={["Alice", "Bob"]}
When rendered
Then it shows "Alice, Bob are typing..."

Given typingUsers={["Alice", "Bob", "Carol", "Dave"]}
When rendered
Then it shows "4 people are typing..."

Given typingUsers={[]} or undefined
When rendered
Then component returns null (no render)

Given agentName="Winston" (deprecated prop)
When rendered
Then it shows "Winston is typing..." (backward compatible)
And a deprecation warning is logged to console

Given both agentName and typingUsers are provided
When rendered
Then typingUsers takes precedence
And agentName is ignored
```

#### Task C.3: Extend useSignalRHandoffs for Presence
**File:** `src/frontend/src/hooks/useSignalRHandoffs.ts` (MODIFY)
**Action:** Add handlers for USER_ONLINE, USER_OFFLINE, USER_TYPING events

**Modification Point:** In the `useEffect` that sets up SignalR handlers, after the existing `connection.on('AGENT_HANDOFF', ...)` handler, add:
```typescript
// Add after the AGENT_HANDOFF handler registration:
connection.on('USER_ONLINE', (payload: { UserId: string; DisplayName: string }) => {
  log('USER_ONLINE received', payload);
  setOnlineUsers(prev => [...prev.filter(u => u.UserId !== payload.UserId), payload]);
});

connection.on('USER_OFFLINE', (payload: { UserId: string }) => {
  log('USER_OFFLINE received', payload);
  setOnlineUsers(prev => prev.filter(u => u.UserId !== payload.UserId));
  setTypingUsers(prev => prev.filter(name => name !== /* lookup DisplayName from UserId */));
});

connection.on('USER_TYPING', (payload: { UserId: string; DisplayName: string }) => {
  log('USER_TYPING received', payload);
  setTypingUsers(prev => [...new Set([...prev, payload.DisplayName])]);
  // Auto-remove after 3 seconds:
  setTimeout(() => {
    setTypingUsers(prev => prev.filter(name => name !== payload.DisplayName));
  }, 3000);
});
```

**New State Variables:**
```typescript
const [onlineUsers, setOnlineUsers] = useState<{ UserId: string; DisplayName: string }[]>([]);
const [typingUsers, setTypingUsers] = useState<string[]>([]);
```

**New Return Values:** `onlineUsers: User[]`, `typingUsers: string[]`

**Acceptance Criteria:**
```gherkin
Given SignalR connection is established
When USER_ONLINE event is received with {UserId, DisplayName}
Then onlineUsers state includes the new user

Given USER_TYPING event is received
When user is already online
Then typingUsers state includes DisplayName for 3 seconds then removes
```

#### Task C.4: PersonaToggle Component
**File:** `src/frontend/src/components/PersonaToggle.tsx` (NEW)
**File:** `src/frontend/src/components/PersonaToggle.css` (NEW)
**File:** `src/frontend/src/components/PersonaToggle.test.tsx` (NEW)
**Action:** Create 3-way toggle for Business/Hybrid/Technical personas

**Acceptance Criteria:**
```gherkin
Given currentPersona={PersonaType.Business}
When PersonaToggle is rendered
Then "Business" segment is highlighted

Given user clicks "Technical" segment
When the click event fires
Then onPersonaChange(PersonaType.Technical) is invoked
And smooth CSS transition animates the highlight

Given persona change API fails
When error occurs
Then previous persona remains selected
And error toast is shown
```

---

### Phase D: Decision Management

#### Task D.1: VersionHistoryPanel Component
**File:** `src/frontend/src/components/VersionHistoryPanel.tsx` (NEW)
**File:** `src/frontend/src/components/VersionHistoryPanel.css` (NEW)
**File:** `src/frontend/src/components/VersionHistoryPanel.test.tsx` (NEW)
**Action:** Create drawer listing decision versions with loading/empty states

**Default Props:**
```typescript
interface VersionHistoryPanelProps {
  decisionId: string;                    // Required
  open: boolean;                         // Required
  onClose: () => void;                   // Required
  onViewDiff?: (fromVersion: number, toVersion: number) => void;
  onRevert?: (versionNumber: number) => void;
  currentVersion?: number;               // Default: latest from API
  maxVersionsToShow?: number;            // Default: 50 (then paginate)
}
```

**Acceptance Criteria:**
```gherkin
Given VersionHistoryPanel is opened
When versions are being fetched
Then it shows a Skeleton loading state with 3 placeholder items

Given decisionId and versions from GET /api/v1/decisions/{id}/versions
When VersionHistoryPanel is opened and data loads
Then it shows Timeline with versions (newest first)
And each entry shows: "v{N} - {ModifiedAt} by {ModifiedBy}" and ChangeReason

Given the decision has no version history (empty array)
When VersionHistoryPanel is opened
Then it shows Empty component with "No version history available"

Given user clicks "View Diff" on version 2
When the button is clicked
Then onViewDiff(2, currentVersion) is invoked

Given user clicks "Revert to v2"
When confirmed via Modal.confirm
Then POST /api/v1/decisions/{id}/revert is called with {versionNumber: 2}
And onRevert() callback is invoked on success

Given decision has 100+ versions
When panel opens
Then only first 50 versions are shown
And a "Load more" button appears at the bottom
```

#### Task D.2: DiffViewer Component
**File:** `src/frontend/src/components/DiffViewer.tsx` (NEW)
**File:** `src/frontend/src/components/DiffViewer.css` (NEW)
**File:** `src/frontend/src/components/DiffViewer.test.tsx` (NEW)
**Action:** Create side-by-side diff display

**Acceptance Criteria:**
```gherkin
Given changes=[{Field: "Value", OldValue: "A", NewValue: "B"}]
When DiffViewer is rendered
Then it shows two columns: "Version {from}" and "Version {to}"
And changed fields are highlighted (red for removed, green for added)

Given changes array is empty
When rendered
Then it shows "No differences found" message
```

#### Task D.3: DecisionAttributionBanner Lock Controls
**File:** `src/frontend/src/components/DecisionAttributionBanner.tsx` (MODIFY)
**Action:** Add lock/unlock buttons and lock status display

**New Props:**
```typescript
isLocked?: boolean;
lockedBy?: string;
lockedAt?: Date;
lockReason?: string;
canLock?: boolean;
canUnlock?: boolean;
onLock?: (reason: string) => void;
onUnlock?: () => void;
onViewHistory?: () => void;
```

**Acceptance Criteria:**
```gherkin
Given isLocked={false} and canLock={true}
When rendered
Then a "🔓 Lock" button appears in the header

Given user clicks "Lock" button
When Modal prompts for reason and user submits "Final decision"
Then onLock("Final decision") is invoked

Given isLocked={true} and lockedBy="Alice" and canUnlock={false}
When rendered
Then "🔒 Locked by Alice" badge appears
And unlock button is disabled with Tooltip "Only Alice or admins can unlock"

Given onViewHistory is provided
When rendered
Then a "📜 History" button appears
And clicking it invokes onViewHistory()
```

#### Task D.4: ConflictAlert Component
**File:** `src/frontend/src/components/ConflictAlert.tsx` (NEW)
**File:** `src/frontend/src/components/ConflictAlert.css` (NEW)
**File:** `src/frontend/src/components/ConflictAlert.test.tsx` (NEW)
**Action:** Create conflict warning banner

**Acceptance Criteria:**
```gherkin
Given conflict with Severity="High" and ConflictType="Contradicting decisions"
When ConflictAlert is rendered
Then it shows red banner with "⚠️ High Severity Conflict: Contradicting decisions"

Given user clicks "Resolve"
When the button is clicked
Then onResolve(conflictId) is invoked

Given user clicks "Dismiss"
When the button is clicked
Then onDismiss() is invoked and banner hides
```

#### Task D.5: ConflictResolutionPanel Component
**File:** `src/frontend/src/components/ConflictResolutionPanel.tsx` (NEW)
**File:** `src/frontend/src/components/ConflictResolutionPanel.css` (NEW)
**File:** `src/frontend/src/components/ConflictResolutionPanel.test.tsx` (NEW)
**Action:** Create modal for resolving conflicts

**Acceptance Criteria:**
```gherkin
Given conflict with two decisions
When ConflictResolutionPanel is opened
Then it shows side-by-side decision values in DiffViewer

Given user selects "Accept Decision 1" and provides resolution notes
When "Resolve" is clicked
Then POST /api/v1/conflicts/{id}/resolve is called
And onResolved() callback is invoked

Given user clicks "Override Warning" 
When justification textarea has content
Then POST /api/v1/conflicts/{id}/override is called with justification
```

#### Task D.6: ReviewRequestForm Component
**File:** `src/frontend/src/components/ReviewRequestForm.tsx` (NEW)
**File:** `src/frontend/src/components/ReviewRequestForm.css` (NEW)
**File:** `src/frontend/src/components/ReviewRequestForm.test.tsx` (NEW)
**Action:** Create form for requesting decision review

**Reviewer Data Source:**
Reviewers are fetched from the workflow's team members via existing API:
- `GET /api/v1/workflows/{workflowId}/members` → Returns `{ Id, DisplayName, Email, Role }[]`
- The parent component (e.g., DecisionAttributionBanner) fetches this list and passes it as a prop
- Alternatively, the form can accept a `fetchReviewers` async function prop for lazy loading

**Props:**
```typescript
interface ReviewRequestFormProps {
  decisionId: string;
  workflowId: string;
  open: boolean;
  onClose: () => void;
  onSuccess?: () => void;
  // Option A: Pre-fetched reviewers
  availableReviewers?: { Id: string; DisplayName: string; Email: string }[];
  // Option B: Lazy-load function
  fetchReviewers?: () => Promise<{ Id: string; DisplayName: string; Email: string }[]>;
}
```

**Acceptance Criteria:**
```gherkin
Given ReviewRequestForm is opened with availableReviewers prop
When form renders
Then it shows Select (multi) populated with reviewer names and DatePicker for deadline

Given ReviewRequestForm is opened with fetchReviewers prop instead
When form renders
Then it shows loading spinner while fetching
And populates Select when fetch completes

Given user selects 2 reviewers and optional deadline
When "Request Review" is clicked
Then POST /api/v1/decisions/{id}/reviews is called with { reviewerIds, deadline }
And form closes on success
And onSuccess callback is invoked

Given no reviewers are selected
When "Request Review" is clicked
Then validation error "Select at least one reviewer" appears

Given fetchReviewers fails
When error occurs
Then error message "Failed to load reviewers" is shown
And a "Retry" button is available
```

#### Task D.7: CheckpointList Component
**File:** `src/frontend/src/components/CheckpointList.tsx` (NEW)
**File:** `src/frontend/src/components/CheckpointList.css` (NEW)
**File:** `src/frontend/src/components/CheckpointList.test.tsx` (NEW)
**Action:** Create checkpoint list with restore capability

**⚠️ Backend API Required (Not Yet Verified):**
Checkpoint functionality requires these backend endpoints which need verification:
- `GET /api/v1/workflows/{id}/checkpoints` → Returns `CheckpointResponse[]`
- `POST /api/v1/workflows/{id}/checkpoints` → Creates checkpoint
- `POST /api/v1/checkpoints/{id}/restore` → Restores to checkpoint

**Note:** If these APIs do not exist, this component should be **DEFERRED** like SharedContextPanel. The implementer MUST verify these endpoints exist before building this component.

**Props:**
```typescript
interface CheckpointListProps {
  workflowId: string;
  checkpoints?: CheckpointResponse[];     // Pre-fetched (optional)
  fetchCheckpoints?: () => Promise<CheckpointResponse[]>;  // Lazy-load
  onRestore?: (checkpointId: string) => Promise<void>;
  onCreateCheckpoint?: (name: string, description?: string) => Promise<void>;
}

interface CheckpointResponse {
  Id: string;
  Name: string;
  Description?: string;
  CreatedAt: string;
  CreatedBy: string;
  WorkflowState: unknown;  // Serialized workflow state
}
```

**Acceptance Criteria:**
```gherkin
Given CheckpointList is rendered
When checkpoints are being fetched
Then it shows Skeleton loading state

Given checkpoints=[{id, name, timestamp, description}]
When CheckpointList data loads
Then it shows Timeline with checkpoint entries

Given checkpoints array is empty
When rendered
Then it shows Empty component with "No checkpoints saved"

Given user clicks "Restore" on a checkpoint
When Modal.confirm is accepted
Then POST /api/v1/checkpoints/{id}/restore is called
And onRestore(checkpointId) callback is invoked on success

Given user clicks "Create Checkpoint"
When form is submitted with name
Then POST /api/v1/workflows/{id}/checkpoints is called
And new checkpoint appears in list
```

#### Task D.8: GlossaryPanel Component
**File:** `src/frontend/src/components/GlossaryPanel.tsx` (NEW)
**File:** `src/frontend/src/components/GlossaryPanel.css` (NEW)
**File:** `src/frontend/src/components/GlossaryPanel.test.tsx` (NEW)
**Action:** Create searchable terminology glossary drawer

**Glossary Data Source Options:**

**Option A: Static JSON (Recommended for MVP)**
Glossary terms are stored in a static JSON file bundled with the frontend:
```typescript
// src/frontend/src/data/glossary.json
[
  { "term": "PRD", "definition": "Product Requirements Document", "category": "business" },
  { "term": "API", "definition": "Application Programming Interface", "category": "technical" },
  // ...
]
```

**Option B: API Endpoint (Future)**
If dynamic glossary is needed later:
- `GET /api/v1/glossary` → Returns `GlossaryTerm[]`
- `GET /api/v1/glossary?category={persona}` → Filtered by persona

**Props:**
```typescript
interface GlossaryPanelProps {
  open: boolean;
  onClose: () => void;
  // Option A: Static data (default - import from JSON)
  terms?: GlossaryTerm[];
  // Option B: Async fetch (if API exists)
  fetchTerms?: () => Promise<GlossaryTerm[]>;
  // Filter by current persona
  currentPersona?: PersonaType;
}

interface GlossaryTerm {
  term: string;
  definition: string;
  category?: 'business' | 'technical' | 'general';
  relatedTerms?: string[];
}
```

**Acceptance Criteria:**
```gherkin
Given GlossaryPanel is opened
When terms are loading (async fetch)
Then it shows Skeleton loading state

Given glossary=[{term: "PRD", definition: "Product Requirements Document"}]
When GlossaryPanel is opened with static data
Then it shows Input.Search and alphabetically sorted term list immediately

Given glossary is empty or undefined
When panel opens
Then it shows Empty component with "No terms available"

Given user types "PRD" in search
When input changes
Then list filters to show only matching terms (case-insensitive)

Given a term is clicked
When the click event fires
Then term definition expands inline with Collapse animation

Given currentPersona={PersonaType.Business}
When panel renders
Then business-category terms are shown first (sorted by relevance)
```

#### Task D.9: Update Component Index
**File:** `src/frontend/src/components/index.ts` (MODIFY)
**Action:** Export all new components

**Acceptance Criteria:**
```gherkin
Given all new components are created
When index.ts is updated
Then all 16 new components are exported
And their Props interfaces are exported as types
```

---

## Testing Strategy

### Testing Strategy Matrix (Per Murat's Feedback)

| Component | Unit Test | E2E Test | SignalR Mock | API Mock |
|-----------|-----------|----------|--------------|----------|
| ErrorBoundary | ✅ Required | ❌ | ❌ | ❌ |
| ConnectionStatusBanner | ✅ Required | ✅ **Critical** | ✅ All states | ❌ |
| AgentRosterGrid | ✅ Required | ❌ | ❌ | ❌ |
| AgentAttribution (mod) | ✅ Extend | ❌ | ❌ | ❌ |
| TTSStatusIndicator | ✅ Required | ❌ | ❌ | ❌ |
| CommandPalette (mod) | ✅ Extend | ❌ | ❌ | ❌ |
| ModeratorAlert | ✅ Required | ❌ | ❌ | ❌ |
| PresenceIndicator | ✅ Required | ✅ Required | ✅ USER_* events | ❌ |
| TypingIndicator (mod) | ✅ Extend | ❌ | ❌ | ❌ |
| PersonaToggle | ✅ Required | ✅ Required | ❌ | ✅ PUT persona |
| VersionHistoryPanel | ✅ Required | ✅ **Critical** | ❌ | ✅ GET versions |
| DiffViewer | ✅ Required | ❌ | ❌ | ❌ |
| DecisionAttributionBanner (mod) | ✅ Extend | ✅ Required | ❌ | ✅ lock/unlock |
| ConflictAlert | ✅ Required | ✅ Required | ❌ | ❌ |
| ConflictResolutionPanel | ✅ Required | ✅ **Critical** | ❌ | ✅ resolve/override |
| ReviewRequestForm | ✅ Required | ❌ | ❌ | ✅ POST reviews |
| CheckpointList | ✅ Required | ❌ | ❌ | ❌ |
| GlossaryPanel | ✅ Required | ❌ | ❌ | ❌ |

### Unit Test Requirements

Each `*.test.tsx` file must include:
1. **Render tests** for all visual states (loading, error, success, empty)
2. **Interaction tests** for all user events (click, hover, keyboard)
3. **Mock verification** for callbacks and API calls
4. **Accessibility** checks (aria-labels present, role attributes)

### E2E Test Files (Playwright)

**Location:** `src/frontend/tests/` (alongside frontend source, not in bmadServer.Playwright.Tests which is for API E2E)

| File | Focus |
|------|-------|
| `src/frontend/tests/epic10/10-1-connection-status.spec.ts` | ConnectionStatusBanner all states, reconnection flow |
| `src/frontend/tests/epic7/7-1-presence-indicators.spec.ts` | PresenceIndicator with real SignalR events |
| `src/frontend/tests/epic8/8-1-persona-toggle.spec.ts` | PersonaToggle API integration |
| `src/frontend/tests/epic6/6-2-version-history.spec.ts` | VersionHistoryPanel, diff, revert |
| `src/frontend/tests/epic6/6-3-decision-locking.spec.ts` | Lock/unlock controls integration |
| `src/frontend/tests/epic6/6-5-conflict-resolution.spec.ts` | ConflictAlert + ConflictResolutionPanel flow |

### Accessibility Testing Strategy (F13 Fix)

**Unit Tests (Vitest):**
Each component test must include accessibility assertions:
```typescript
import { axe, toHaveNoViolations } from 'jest-axe';
expect.extend(toHaveNoViolations);

it('should have no accessibility violations', async () => {
  const { container } = render(<Component />);
  const results = await axe(container);
  expect(results).toHaveNoViolations();
});
```

**E2E Tests (Playwright):**
Add axe-core integration to critical E2E flows:
```typescript
import AxeBuilder from '@axe-core/playwright';

test('version history panel is accessible', async ({ page }) => {
  await page.goto('/workflow/123');
  await page.click('[data-testid="view-history"]');
  
  const accessibilityScanResults = await new AxeBuilder({ page })
    .include('[data-testid="version-history-panel"]')
    .analyze();
  
  expect(accessibilityScanResults.violations).toEqual([]);
});
```

**Required npm Package:**
- `@axe-core/playwright` ^4.x - Playwright accessibility testing
- `jest-axe` ^8.x - Unit test accessibility assertions

---

## Dependencies

### npm Packages (Already Installed)
- `antd` ^5.x - UI components (Avatar, Badge, Tag, Timeline, Drawer, Modal, Select)
- `@microsoft/signalr` ^8.x - Real-time communication
- `@testing-library/react` ^14.x - Unit testing utilities
- `@playwright/test` ^1.x - E2E testing framework
- `vitest` ^2.x - Test runner

### npm Packages (To Install)
- `@axe-core/playwright` ^4.x - Playwright accessibility testing (F13 fix)
- `jest-axe` ^8.x - Unit test accessibility assertions (F13 fix)

### Backend APIs (Verified Exist)
| Endpoint | Method | Used By |
|----------|--------|--------|
| `/api/v1/workflows/{id}/decisions` | GET | useDecisions |
| `/api/v1/decisions/{id}` | GET | useDecisions |
| `/api/v1/decisions/{id}` | PUT | useDecisions |
| `/api/v1/decisions/{id}/versions` | GET | VersionHistoryPanel |
| `/api/v1/decisions/{id}/versions/{v1}/diff/{v2}` | GET | DiffViewer |
| `/api/v1/decisions/{id}/revert` | POST | VersionHistoryPanel |
| `/api/v1/decisions/{id}/lock` | POST | DecisionAttributionBanner |
| `/api/v1/decisions/{id}/unlock` | POST | DecisionAttributionBanner |
| `/api/v1/decisions/{id}/reviews` | POST | ReviewRequestForm |
| `/api/v1/workflows/{id}/conflicts` | GET | ConflictAlert |
| `/api/v1/conflicts/{id}/resolve` | POST | ConflictResolutionPanel |
| `/api/v1/conflicts/{id}/override` | POST | ConflictResolutionPanel |
| `/api/v1/sessions/{id}/persona` | PUT | PersonaToggle |
| `/api/v1/workflows/{id}/members` | GET | ReviewRequestForm (reviewer list) |

### Backend APIs (⚠️ NEEDS VERIFICATION)
| Endpoint | Method | Used By | Status |
|----------|--------|---------|--------|
| `/api/v1/workflows/{id}/checkpoints` | GET | CheckpointList | **VERIFY BEFORE IMPLEMENTING** |
| `/api/v1/workflows/{id}/checkpoints` | POST | CheckpointList | **VERIFY BEFORE IMPLEMENTING** |
| `/api/v1/checkpoints/{id}/restore` | POST | CheckpointList | **VERIFY BEFORE IMPLEMENTING** |

**⚠️ Implementation Note:** If checkpoint APIs do not exist, DEFER CheckpointList component (move to Out of Scope with SharedContextPanel).

### SignalR Events (Verified Exist)
- `USER_ONLINE` - usePresence
- `USER_OFFLINE` - usePresence
- `USER_TYPING` - usePresence
- `AGENT_HANDOFF` - existing useSignalRHandoffs

### ⚠️ Missing Backend (Deferred)
- `CONTEXT_UPDATE` SignalR event - **Does NOT exist** - SharedContextPanel/ContextTimeline deferred

---

## Additional Context

### High-Risk Items (Pre-Mortem)

1. **SignalR Reconnection Race Conditions** - ConnectionStatusBanner must handle rapid state changes without UI flicker. Add debounce to state transitions.

2. **Lock Conflict on Simultaneous Edit** - Two users clicking "Lock" at same time. API returns 409; UI must show "Already locked by {user}" toast immediately.

3. **Large Version History** - Decisions with 50+ versions. VersionHistoryPanel needs virtual scrolling or pagination.

4. **TTS State Sync** - TTSStatusIndicator relies on external TTS state. If TTS crashes, indicator may show stale "playing" state. Add timeout fallback.

### Known Limitations

- **SharedContextPanel and ContextTimeline** are deferred because `CONTEXT_UPDATE` SignalR event does not exist in backend. Requires backend story before frontend implementation.

- **PersonaSelector removed** per UX feedback - PersonaToggle handles all 3 persona options with simpler UX.

- **ErrorBoundary** is a class component (React requirement) - only exception to functional component pattern.

### Future Considerations (Out of Scope)

- Storybook stories for visual regression testing (Murat's suggestion - consider for Phase 2)
- Offline mode overlay with queued message persistence (Sally's suggestion)
- Admin dashboard for conflict management (Epic 12)

---

## File Summary

### New Files (54 total)

**Types (3):**
- `src/frontend/src/types/decisions.ts`
- `src/frontend/src/types/persona.ts`
- `src/frontend/src/types/index.ts` (barrel export)

**Hooks (2):**
- `src/frontend/src/hooks/useDecisions.ts`
- `src/frontend/src/hooks/usePresence.ts`

**Components (16):**
- `src/frontend/src/components/ErrorBoundary.tsx`
- `src/frontend/src/components/ConnectionStatusBanner.tsx`
- `src/frontend/src/components/AgentRosterGrid.tsx`
- `src/frontend/src/components/TTSStatusIndicator.tsx`
- `src/frontend/src/components/ModeratorAlert.tsx`
- `src/frontend/src/components/PresenceIndicator.tsx`
- `src/frontend/src/components/PersonaToggle.tsx`
- `src/frontend/src/components/VersionHistoryPanel.tsx`
- `src/frontend/src/components/DiffViewer.tsx`
- `src/frontend/src/components/ConflictAlert.tsx`
- `src/frontend/src/components/ConflictResolutionPanel.tsx`
- `src/frontend/src/components/ReviewRequestForm.tsx`
- `src/frontend/src/components/CheckpointList.tsx` (⚠️ pending API verification)
- `src/frontend/src/components/GlossaryPanel.tsx`

**CSS Files (16):**
- `src/frontend/src/components/ErrorBoundary.css`
- `src/frontend/src/components/ConnectionStatusBanner.css`
- `src/frontend/src/components/AgentRosterGrid.css`
- `src/frontend/src/components/TTSStatusIndicator.css`
- `src/frontend/src/components/ModeratorAlert.css`
- `src/frontend/src/components/PresenceIndicator.css`
- `src/frontend/src/components/PersonaToggle.css`
- `src/frontend/src/components/VersionHistoryPanel.css`
- `src/frontend/src/components/DiffViewer.css`
- `src/frontend/src/components/ConflictAlert.css`
- `src/frontend/src/components/ConflictResolutionPanel.css`
- `src/frontend/src/components/ReviewRequestForm.css`
- `src/frontend/src/components/CheckpointList.css`
- `src/frontend/src/components/GlossaryPanel.css`

**Unit Test Files (16):**
- `src/frontend/src/components/ErrorBoundary.test.tsx`
- `src/frontend/src/components/ConnectionStatusBanner.test.tsx`
- `src/frontend/src/components/AgentRosterGrid.test.tsx`
- `src/frontend/src/components/TTSStatusIndicator.test.tsx`
- `src/frontend/src/components/ModeratorAlert.test.tsx`
- `src/frontend/src/components/PresenceIndicator.test.tsx`
- `src/frontend/src/components/PersonaToggle.test.tsx`
- `src/frontend/src/components/VersionHistoryPanel.test.tsx`
- `src/frontend/src/components/DiffViewer.test.tsx`
- `src/frontend/src/components/ConflictAlert.test.tsx`
- `src/frontend/src/components/ConflictResolutionPanel.test.tsx`
- `src/frontend/src/components/ReviewRequestForm.test.tsx`
- `src/frontend/src/components/CheckpointList.test.tsx`
- `src/frontend/src/components/GlossaryPanel.test.tsx`

**Static Data (1):**
- `src/frontend/src/data/glossary.json` (glossary terms)

### Modified Files (6)

- `src/frontend/src/components/AgentAttribution.tsx` - Add relevanceScore prop
- `src/frontend/src/components/CommandPalette.tsx` - Add exit commands
- `src/frontend/src/components/DecisionAttributionBanner.tsx` - Add lock controls
- `src/frontend/src/components/TypingIndicator.tsx` - Multi-user support
- `src/frontend/src/hooks/useSignalRHandoffs.ts` - Add presence events
- `src/frontend/src/components/index.ts` - Export new components
2. **Feature Flags:** Consider wrapping Epic 6-8 components in feature flags for staged release
3. **Mobile Responsiveness:** All new components must be tested at 375px width minimum
4. **Accessibility:** Follow WCAG 2.1 AA - proper ARIA labels, keyboard navigation, focus management

### File Creation Summary

**New Files (22):**
- Components: AgentRosterGrid, TTSStatusIndicator, ModeratorAlert, SharedContextPanel, ContextTimeline, VersionHistoryPanel, DiffViewer, ConflictAlert, ConflictResolutionPanel, ReviewRequestForm, PresenceIndicator, CheckpointList, PersonaSelector, PersonaToggle, GlossaryPanel, ConnectionStatusBanner, ErrorBoundary, ReconnectBanner
- Hooks: useDraftPersistence
- CSS: 18 corresponding .css files
- Tests: 22 corresponding .test.tsx files

**Modified Files (5):**
- AgentAttribution.tsx (add relevance badge)
- CommandPalette.tsx (add exit commands)
- ChatInput.tsx (TextArea + character count)
- DecisionAttributionBanner.tsx (lock controls)
- TypingIndicator.tsx (multi-user support)
