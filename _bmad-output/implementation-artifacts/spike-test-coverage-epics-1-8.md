# Spike: Test Coverage Analysis - Epics 1-8

**Date:** 2026-01-27
**Author:** Test Architect (Party Mode Analysis)
**Status:** SPIKE COMPLETE
**Type:** Technical Investigation

---

## Executive Summary

Analysis of completed Epics 1-8 reveals significant gaps in automated test coverage:

- **7 UI stories** require Playwright E2E tests (Epic 3: 6 stories, Epic 8.4: 1 story)
- **22+ stories** have acceptance criteria not covered in existing Reqnroll feature files
- **Existing coverage**: Only 2 feature files covering Epic 4 (Workflow) and Epic 1.4 (CI/CD)

**Recommendation:** Create dedicated feature files per epic and establish Playwright test infrastructure for all UI stories.

---

## Part 1: Playwright Test Plan for UI Stories

### Overview

Epic 3 (Real-Time Chat Interface) and Epic 8.4 (In-Session Persona Switching) contain UI acceptance criteria that require E2E browser testing.

### Playwright Infrastructure Setup

```
src/bmadServer.Playwright.Tests/
├── playwright.config.ts
├── package.json
├── Pages/
│   ├── ChatPage.ts          # Page Object Model
│   ├── AuthPage.ts
│   └── SettingsPage.ts
├── Tests/
│   ├── Epic3/
│   │   ├── 3-1-signalr-connection.spec.ts
│   │   ├── 3-2-chat-message-component.spec.ts
│   │   ├── 3-3-chat-input-component.spec.ts
│   │   ├── 3-4-message-streaming.spec.ts
│   │   ├── 3-5-chat-history-scroll.spec.ts
│   │   └── 3-6-mobile-responsive.spec.ts
│   └── Epic8/
│       └── 8-4-persona-switching.spec.ts
├── Fixtures/
│   ├── auth.fixture.ts
│   └── chat.fixture.ts
└── Utils/
    └── signalr-helper.ts
```

### Story 3.1: SignalR Hub Setup & WebSocket Connection

**File:** `3-1-signalr-connection.spec.ts`

| Test Case | Priority | AC Reference |
|-----------|----------|--------------|
| WebSocket connection establishes with valid JWT | P0 | Given authenticated, When connect with accessTokenFactory, Then connection established |
| OnConnectedAsync callback executes on connect | P1 | Given connect, Then OnConnectedAsync called |
| Message transmission within 100ms | P1 | Given connected, When send message, Then received within 100ms |
| Automatic reconnect with exponential backoff | P0 | Given connection drops, Then reconnect at 0s, 2s, 10s, 30s |
| Session recovery on reconnect | P0 | Given reconnect, Then session recovery flow executes |

### Story 3.2: Chat Message Component with Ant Design

**File:** `3-2-chat-message-component.spec.ts`

| Test Case | Priority | AC Reference |
|-----------|----------|--------------|
| User messages aligned right with blue background | P1 | Given user message, Then aligned right, color blue |
| Agent messages aligned left with gray background | P1 | Given agent message, Then aligned left, color gray |
| Markdown to HTML conversion | P1 | Given markdown content, Then rendered as HTML |
| Code blocks syntax highlighted | P2 | Given code block, Then syntax highlighted |
| Links clickable in new tabs | P2 | Given link in message, Then target="_blank" |
| Typing indicator within 500ms | P1 | Given agent typing, Then indicator appears within 500ms |
| ARIA labels for accessibility | P1 | Given message, Then aria-label present |
| Live region announcements | P1 | Given new message, Then aria-live region updated |

### Story 3.3: Chat Input Component with Rich Interactions

**File:** `3-3-chat-input-component.spec.ts`

| Test Case | Priority | AC Reference |
|-----------|----------|--------------|
| Multi-line input with Send button | P0 | Given input area, Then multi-line + Send button |
| Send button disabled when empty | P0 | Given empty input, Then Send disabled |
| Ctrl+Enter sends message | P0 | Given message, When Ctrl+Enter, Then message sent + input cleared |
| Character count turns red at 2000+ | P1 | Given 2000+ chars, Then counter red |
| Draft preservation in localStorage | P1 | Given partial message, When navigate away, Then draft preserved |
| "/" command palette appears | P1 | Given "/" typed, Then palette shows /help, /status, /pause, /resume |
| Arrow key navigation in palette | P2 | Given palette open, Then arrow keys navigate |
| Cancel button for slow requests | P1 | Given >5s request, Then Cancel button visible |

### Story 3.4: Real-Time Message Streaming

**File:** `3-4-message-streaming.spec.ts`

| Test Case | Priority | AC Reference |
|-----------|----------|--------------|
| Streaming starts within 5 seconds | P0 | Given message sent, When agent responds, Then streaming within 5s |
| Tokens append without flickering | P0 | Given streaming, Then smooth append, no flicker |
| MESSAGE_CHUNK format validation | P1 | Given chunk received, Then has messageId, chunk, isComplete, agentId |
| Typing indicator disappears on complete | P1 | Given isComplete:true, Then indicator gone |
| Partial message preserved on disconnect | P0 | Given network drop mid-stream, Then partial message preserved |
| Stop Generating button works | P1 | Given streaming, When click Stop, Then streaming stops + "(Stopped)" shown |

### Story 3.5: Chat History & Scroll Management

**File:** `3-5-chat-history-scroll.spec.ts`

| Test Case | Priority | AC Reference |
|-----------|----------|--------------|
| Last 50 messages load at bottom | P0 | Given chat load, Then 50 messages, scrolled to bottom |
| Load More loads next 50 | P1 | Given scroll to top, When click Load More, Then 50 more loaded |
| No scroll jump on Load More | P1 | Given Load More, Then scroll position maintained |
| New message badge when scrolled up | P1 | Given scrolled up, When new message, Then badge appears |
| Scroll position restored on reopen | P2 | Given close/reopen chat, Then scroll position restored |
| Welcome message for empty chat | P1 | Given empty chat, Then welcome + quick-start buttons |

### Story 3.6: Mobile-Responsive Chat Interface

**File:** `3-6-mobile-responsive.spec.ts`

| Test Case | Priority | AC Reference |
|-----------|----------|--------------|
| Single-column layout on mobile (<768px) | P0 | Given viewport <768px, Then single-column |
| Hamburger menu for sidebar | P1 | Given mobile, Then hamburger menu visible |
| 44px+ touch targets on input | P1 | Given mobile, Then input targets ≥44px |
| Virtual keyboard doesn't hide input | P0 | Given keyboard open, Then input visible (sticky bottom) |
| Swipe down refresh | P2 | Given mobile, When swipe down, Then refresh |
| Tap-hold to copy | P2 | Given mobile message, When tap-hold, Then copy |
| VoiceOver announcements | P1 | Given VoiceOver, Then elements announced |
| Reduced motion respected | P2 | Given prefers-reduced-motion, Then animations disabled |

### Story 8.4: In-Session Persona Switching

**File:** `8-4-persona-switching.spec.ts`

| Test Case | Priority | AC Reference |
|-----------|----------|--------------|
| Persona switcher displays current + options | P1 | Given settings, Then current persona + Business/Technical/Hybrid options |
| Switching to Business translates future messages | P0 | Given switch to Business, Then future messages in business language |
| Previous messages unchanged on switch | P1 | Given switch, Then historical messages unchanged |
| "Switched to X mode" notification | P1 | Given switch, Then notification appears |
| Hybrid mode suggestion after 3+ switches | P2 | Given 3+ switches, Then suggest Hybrid |
| Session switches logged | P2 | Given switch, Then logged for analytics |
| Ctrl+Shift+P opens switcher | P1 | Given anywhere, When Ctrl+Shift+P, Then switcher opens |

### Playwright Test Effort Estimate

| Story | Tests | Hours/Test | Total Hours |
|-------|-------|------------|-------------|
| 3.1 SignalR | 5 | 2.0 | 10 |
| 3.2 Chat Message | 8 | 1.5 | 12 |
| 3.3 Chat Input | 8 | 1.5 | 12 |
| 3.4 Streaming | 6 | 2.0 | 12 |
| 3.5 History/Scroll | 6 | 1.5 | 9 |
| 3.6 Mobile | 8 | 2.0 | 16 |
| 8.4 Persona | 7 | 1.5 | 10.5 |
| **Total** | **48** | - | **81.5 hours (~10 days)** |

---

## Part 2: Reqnroll Feature File Gap Analysis

### Current Coverage

| Feature File | Epic | Scenarios | Status |
|--------------|------|-----------|--------|
| WorkflowOrchestration.feature | Epic 4 | 26 scenarios | ✅ Good coverage |
| GitHubActionsCICD.feature | Epic 1.4 | 6 scenarios | ✅ Good coverage |

### Missing Feature Files

| Epic | Stories | Status | Priority |
|------|---------|--------|----------|
| Epic 1 | 1.1, 1.2 | ❌ Missing | P1 |
| Epic 2 | 2.1-2.4 | ❌ Missing | P0 (Security) |
| Epic 3 | 3.1-3.6 | ❌ Missing | P1 (covered by Playwright) |
| Epic 5 | 5.1-5.4 | ❌ Missing | P1 |
| Epic 6 | 6.1 | ❌ Missing | P1 |
| Epic 7 | 7.1 | ❌ Missing | P1 |
| Epic 8 | 8.1 | ❌ Missing | P2 |

### Required Feature Files

#### 1. Epic1Foundation.feature (Stories 1.1, 1.2)

```gherkin
Feature: Aspire Foundation & Project Setup
  As a developer
  I want a properly configured Aspire foundation
  So that I can build reliable microservices

  @epic-1 @story-1-1
  Scenario: Project initializes from Aspire template
    Given .NET 10 SDK is installed
    When I run the Aspire starter template
    Then the project structure is created with AppHost, ApiService, ServiceDefaults
    And Directory.Build.props exists

  @epic-1 @story-1-1
  Scenario: Aspire dashboard accessible after startup
    Given the project is built
    When I run aspire run
    Then the Aspire dashboard appears at the expected port
    And the API responds to GET /health with 200 OK

  @epic-1 @story-1-1
  Scenario: Distributed tracing is configured
    Given AppHost is running
    When I check the AppHost logs
    Then I see structured JSON logs with trace IDs

  @epic-1 @story-1-2
  Scenario: PostgreSQL resource is configured
    Given the AppHost is defined
    When I examine the PostgreSQL configuration
    Then PostgreSQL container runs with correct version
    And connection string is properly exposed to services
```

#### 2. Epic2Authentication.feature (Stories 2.1-2.4)

```gherkin
Feature: User Authentication & Session Management
  As a user
  I want secure authentication
  So that my data is protected

  # Story 2.1: User Registration
  @epic-2 @story-2-1 @security
  Scenario: Register new user with valid credentials
    Given bmadServer API is running
    When I send POST /api/v1/auth/register with email, password, displayName
    Then a User record is created
    And the password is hashed using bcrypt with cost 12
    And the response returns 201 with user details

  @epic-2 @story-2-1
  Scenario: Reject duplicate email registration
    Given a user exists with email "test@example.com"
    When I attempt to register with the same email
    Then the system returns 409 Conflict
    And the error indicates user already exists

  @epic-2 @story-2-1
  Scenario: Reject weak password
    Given bmadServer API is running
    When I attempt to register with password "123"
    Then the system returns 400 Bad Request
    And the error specifies password requirements

  # Story 2.2: JWT Token Generation
  @epic-2 @story-2-2 @security
  Scenario: Login generates JWT token
    Given I am a registered user
    When I send POST /api/v1/auth/login with correct credentials
    Then the system validates password
    And returns 200 OK with accessToken
    And the JWT expires in 15 minutes

  @epic-2 @story-2-2
  Scenario: Login with wrong password fails
    Given I am a registered user
    When I send POST /api/v1/auth/login with wrong password
    Then the system returns 401 Unauthorized
    And the message is generic "Invalid email or password"

  @epic-2 @story-2-2 @security
  Scenario: Protected endpoint requires valid JWT
    Given I have a valid JWT token
    When I send GET /api/v1/users/me with Authorization Bearer header
    Then the JWT is validated and claims extracted
    And the endpoint returns 200 OK with my profile

  @epic-2 @story-2-2
  Scenario: Expired JWT is rejected
    Given I have an expired JWT token
    When I call any protected endpoint
    Then the system returns 401 Unauthorized
    And the error indicates token expired

  # Story 2.3: Refresh Token Flow
  @epic-2 @story-2-3 @security
  Scenario: Login sets HttpOnly refresh token cookie
    Given I successfully login
    When I receive the login response
    Then a refresh token UUID is generated
    And the token hash is stored in RefreshTokens table
    And an HttpOnly cookie is set with Secure, SameSite=Strict

  @epic-2 @story-2-3
  Scenario: Refresh token rotation works
    Given my access token is about to expire
    When I send POST /api/v1/auth/refresh with cookie
    Then the system validates the token
    And generates a new access token
    And rotates the refresh token

  @epic-2 @story-2-3 @security
  Scenario: Reused refresh token revokes all user tokens
    Given I have a previously used refresh token
    When I send POST /api/v1/auth/refresh with the old token
    Then the system returns 401 Unauthorized
    And ALL user tokens are revoked (security breach detection)

  @epic-2 @story-2-3
  Scenario: Logout clears refresh token
    Given I am logged in
    When I send POST /api/v1/auth/logout
    Then the refresh token is revoked
    And the cookie is cleared (Max-Age=0)
    And the response is 204 No Content

  # Story 2.4: Session Persistence
  @epic-2 @story-2-4
  Scenario: SignalR connection creates session record
    Given I am authenticated
    When my SignalR connection establishes
    Then a Session record is created
    And it includes UserId, ConnectionId, WorkflowState, ExpiresAt (30 min)

  @epic-2 @story-2-4
  Scenario: Session recovery within 60 seconds
    Given my network connection drops
    When I reconnect within 60 seconds
    Then the system matches userId and validates session
    And associates new ConnectionId
    And sends SESSION_RESTORED SignalR message

  @epic-2 @story-2-4
  Scenario: Session recovery after 60 seconds creates new session
    Given I disconnect
    When I reconnect after 61 seconds
    Then a NEW session is created
    But workflow state is recovered
    And message displays "Session recovered from last checkpoint"

  @epic-2 @story-2-4
  Scenario: Optimistic concurrency prevents conflicts
    Given I have multiple active sessions (laptop + mobile)
    When two devices update workflow state simultaneously
    Then the system detects version mismatch
    And the second update fails with 409 Conflict
```

#### 3. Epic5MultiAgentCollaboration.feature (Stories 5.1-5.4)

```gherkin
Feature: Multi-Agent Collaboration
  As a workflow participant
  I want multiple agents to collaborate
  So that complex tasks are handled by specialists

  # Story 5.1: Agent Registry
  @epic-5 @story-5-1
  Scenario: Query all registered agents
    Given the agent registry is initialized
    When I query GetAllAgents()
    Then I receive ProductManager, Architect, Designer, Developer, Analyst, Orchestrator

  @epic-5 @story-5-1
  Scenario: Agent has required properties
    Given the agent registry is initialized
    When I examine any agent
    Then it includes AgentId, Name, Description, Capabilities, SystemPrompt, ModelPreference

  @epic-5 @story-5-1
  Scenario: Query agents by capability
    Given the agent registry has agents
    When I call GetAgentsByCapability("create-prd")
    Then I receive ProductManager agent with matching capability

  # Story 5.2: Agent-to-Agent Messaging
  @epic-5 @story-5-2
  Scenario: Agent can send message to another agent
    Given two agents are registered
    When Agent A sends a message to Agent B
    Then Agent B receives the message
    And the message includes sender, content, timestamp, correlation ID

  @epic-5 @story-5-2
  Scenario: Message routing to correct agent
    Given a workflow step requires collaboration
    When the orchestrator routes message to specialist
    Then the correct agent receives based on capability match

  # Story 5.3: Shared Workflow Context
  @epic-5 @story-5-3
  Scenario: Agent accesses shared context
    Given a workflow has multiple completed steps
    When an agent receives a request
    Then it has access to SharedContext with all step outputs

  @epic-5 @story-5-3
  Scenario: Query specific prior output
    Given a workflow has completed step "step-1"
    When agent queries SharedContext.GetStepOutput("step-1")
    Then it receives the structured output

  @epic-5 @story-5-3
  Scenario: Output automatically added to context
    Given an agent produces output
    When the step completes
    Then output is automatically added to SharedContext
    And subsequent agents can access it immediately

  @epic-5 @story-5-3
  Scenario: Large context is summarized
    Given context grows large (exceeds token limits)
    When agent accesses context
    Then older context is summarized
    And key decisions are preserved

  @epic-5 @story-5-3
  Scenario: Concurrent context access is safe
    Given multiple agents access context simultaneously
    When reads and writes occur
    Then optimistic concurrency control prevents conflicts
    And version numbers track changes

  # Story 5.4: Agent Handoff & Attribution
  @epic-5 @story-5-4
  Scenario: Handoff displays transition message
    Given a workflow step changes agents
    When handoff occurs
    Then UI displays "Handing off to [AgentName]..."

  @epic-5 @story-5-4
  Scenario: Chat history shows agent attribution
    Given an agent completes work
    When I view chat history
    Then each message shows agent avatar and name

  @epic-5 @story-5-4
  Scenario: Decisions show agent attribution
    Given a decision was made by an agent
    When I review the decision
    Then I see "Decided by [AgentName] at [timestamp]" with reasoning

  @epic-5 @story-5-4
  Scenario: Handoffs are logged for audit
    Given handoffs occur during workflow
    When I query the audit log
    Then I see all handoffs with fromAgent, toAgent, timestamp, workflowStep, reason
```

#### 4. Epic6DecisionManagement.feature (Story 6.1)

```gherkin
Feature: Decision Management & Locking
  As a workflow participant
  I want decisions captured and stored
  So that I have a record of all choices made

  # Story 6.1: Decision Capture & Storage
  @epic-6 @story-6-1
  Scenario: Decision is captured when confirmed
    Given I am in a workflow
    When I make a decision and confirm my choice
    Then a Decision record is created
    And it includes id, workflowInstanceId, stepId, decisionType, value, decidedBy, decidedAt

  @epic-6 @story-6-1
  Scenario: Query all workflow decisions
    Given a workflow has multiple decisions
    When I send GET /api/v1/workflows/{id}/decisions
    Then I receive all decisions in chronological order

  @epic-6 @story-6-1
  Scenario: Decision includes full context
    Given a decision was made
    When I view decision details
    Then I see the question asked, options presented, selected option, reasoning, context at time

  @epic-6 @story-6-1
  Scenario: Structured data stored as validated JSON
    Given a decision involves structured data
    When the decision is captured
    Then the value is stored as validated JSON matching expected schema
```

#### 5. Epic7Collaboration.feature (Story 7.1)

```gherkin
Feature: Collaboration & Multi-User Support
  As a workflow owner
  I want to invite collaborators
  So that others can participate in the workflow

  # Story 7.1: Multi-User Workflow Participation
  @epic-7 @story-7-1
  Scenario: Invite user as Contributor
    Given I own a workflow
    When I send POST /api/v1/workflows/{id}/participants with userId and role "Contributor"
    Then the user is added
    And they receive an invitation notification
    And appear in the participants list

  @epic-7 @story-7-1
  Scenario: Contributor can interact with workflow
    Given a user is added as Contributor
    When they access the workflow
    Then they can send messages
    And make decisions
    And advance steps
    And their actions are attributed to them

  @epic-7 @story-7-1
  Scenario: Observer has read-only access
    Given a user is added as Observer
    When they access the workflow
    Then they can view messages and decisions
    But cannot make changes or send messages
    And UI shows read-only mode

  @epic-7 @story-7-1
  Scenario: Presence indicators show online users
    Given multiple users are connected
    When I view the workflow
    Then I see presence indicators showing who is online
    And typing indicators when others compose

  @epic-7 @story-7-1
  Scenario: Remove participant revokes access
    Given a user is a participant
    When I send DELETE /api/v1/workflows/{id}/participants/{userId}
    Then the user loses access immediately
    And receives notification
    And future access is denied
```

#### 6. Epic8PersonaTranslation.feature (Story 8.1)

```gherkin
Feature: Persona Translation & Language Adaptation
  As a user
  I want to configure my communication persona
  So that responses match my preferred style

  # Story 8.1: Persona Profile Configuration
  @epic-8 @story-8-1
  Scenario: Persona options are available
    Given I am setting up my profile
    When I access persona settings
    Then I see options: Business, Technical, Hybrid (adaptive)

  @epic-8 @story-8-1
  Scenario: Select Business persona
    Given I am configuring my profile
    When I select Business persona and save
    Then my user profile includes personaType: "business"
    And the setting persists across sessions

  @epic-8 @story-8-1
  Scenario: Persona descriptions show examples
    Given I view persona descriptions
    When I hover over each option
    Then I see examples of response differences

  @epic-8 @story-8-1
  Scenario: Default persona is Hybrid
    Given I don't set a persona
    When I start using the system
    Then the default is Hybrid (adaptive based on context)

  @epic-8 @story-8-1
  Scenario: User profile includes persona
    Given I have configured my persona
    When I send GET /api/v1/users/me
    Then the response includes personaType and language preferences
```

### Reqnroll Test Effort Estimate

| Feature File | Scenarios | Hours/Scenario | Total Hours |
|--------------|-----------|----------------|-------------|
| Epic1Foundation.feature | 4 | 1.0 | 4 |
| Epic2Authentication.feature | 14 | 1.5 | 21 |
| Epic5MultiAgentCollaboration.feature | 14 | 1.5 | 21 |
| Epic6DecisionManagement.feature | 4 | 1.0 | 4 |
| Epic7Collaboration.feature | 5 | 1.5 | 7.5 |
| Epic8PersonaTranslation.feature | 5 | 1.0 | 5 |
| **Total** | **46** | - | **62.5 hours (~8 days)** |

---

## Part 3: Implementation Roadmap

### Phase 1: Infrastructure Setup (2 days)

1. **Playwright Setup**
   - Create `bmadServer.Playwright.Tests` project
   - Configure `playwright.config.ts` with base URL, browsers
   - Set up CI/CD integration in `.github/workflows/ci.yml`
   - Create authentication fixture for JWT handling

2. **Reqnroll Expansion**
   - Create new feature files in `bmadServer.BDD.Tests/Features/`
   - Set up step definition base classes for reuse

### Phase 2: Security-Critical Tests (3 days) - P0

1. **Epic2Authentication.feature** (14 scenarios)
   - All authentication scenarios (security-critical)
   - Refresh token security tests
   - Session management tests

### Phase 3: Core UI Tests (5 days) - P0/P1

1. **Playwright Epic 3 tests**
   - 3.1 SignalR connection (P0)
   - 3.4 Message streaming (P0)
   - 3.6 Mobile responsive (P0 for critical paths)
   - 3.2, 3.3, 3.5 (P1)

### Phase 4: Business Logic Tests (5 days) - P1

1. **Epic5MultiAgentCollaboration.feature** (14 scenarios)
2. **Epic6DecisionManagement.feature** (4 scenarios)
3. **Epic7Collaboration.feature** (5 scenarios)

### Phase 5: Remaining Tests (3 days) - P2

1. **Epic1Foundation.feature** (4 scenarios)
2. **Epic8PersonaTranslation.feature** (5 scenarios)
3. **Playwright 8.4 Persona Switching** (7 tests)

---

## Total Effort Summary

| Component | Tests | Hours | Days |
|-----------|-------|-------|------|
| Playwright Infrastructure | - | 8 | 1 |
| Playwright UI Tests | 48 | 81.5 | 10 |
| Reqnroll Feature Files | 46 | 62.5 | 8 |
| **Total** | **94** | **152** | **~19 days** |

---

## Quality Gate Criteria

### Before Implementation Complete

- [ ] All P0 tests pass (100%)
- [ ] All P1 tests pass (≥95%)
- [ ] No high-risk (security) scenarios without coverage
- [ ] CI/CD runs all tests on PR to main

### Test Coverage Targets

- **Authentication (Epic 2)**: 100% AC coverage
- **Critical UI paths (Epic 3)**: ≥80% AC coverage
- **Core business logic (Epic 4-7)**: ≥80% AC coverage

---

## Recommendations

1. **Prioritize Epic 2 authentication tests** - Security-critical, must be P0
2. **Establish Playwright infrastructure first** - Enables parallel UI test development
3. **Use page object model** - Maintainability for 48 UI tests
4. **Share step definitions** - Reduce duplication across feature files
5. **Run tests in parallel** - Critical for CI/CD performance

---

## Next Steps

1. Review and approve spike recommendations
2. Create Story: "Set up Playwright test infrastructure"
3. Create Story: "Add Epic 2 authentication BDD tests"
4. Create Story: "Add Epic 3 Playwright UI tests"
5. Add remaining stories to backlog with priorities

---

**Generated by:** Test Architect Analysis (Party Mode)
**Workflow:** `_bmad/bmm/testarch/test-design` (Spike Mode)
**Version:** 1.0
