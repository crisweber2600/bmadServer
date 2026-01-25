# Story 5.4: Agent Handoff & Attribution

**Story ID:** E5-S4  
**Epic:** Epic 5 - Multi-Agent Collaboration  
**Points:** 5  
**Status:** ready-for-dev

---

## Story

As a user (Sarah),  
I want to see when different agents take over,  
so that I understand who is responsible for each part of the workflow.

---

## Acceptance Criteria

**Given** a workflow step changes agents  
**When** the handoff occurs  
**Then** the UI displays a handoff indicator: "Handing off to [AgentName]..."

**Given** an agent completes their work  
**When** I view the chat history  
**Then** each message shows the agent avatar and name  
**And** I can distinguish between different agents' contributions

**Given** a decision is made by an agent  
**When** I review the decision  
**Then** I see attribution: "Decided by [AgentName] at [timestamp]"  
**And** the reasoning is visible

**Given** I hover over an agent indicator  
**When** the tooltip displays  
**Then** I see: agent name, description, capabilities, current step responsibility

**Given** handoffs are logged  
**When** I query the audit log  
**Then** I see all handoffs with: fromAgent, toAgent, timestamp, workflowStep, reason

---

## Tasks / Subtasks

- [ ] Task 1: Create AgentHandoff domain model (AC: 5)
  - [ ] Define AgentHandoff class in Models/Workflows/
  - [ ] Add properties: Id, WorkflowInstanceId, FromAgentId, ToAgentId, Timestamp, WorkflowStepId, Reason
  - [ ] Add EF Core entity configuration
  - [ ] Create database migration
  - [ ] Add DbSet to ApplicationDbContext
  
- [ ] Task 2: Extend WorkflowEvent for handoff tracking (AC: 5)
  - [ ] Add AgentHandoffOccurred event type to WorkflowEvent model
  - [ ] Ensure WorkflowEvent.Metadata supports handoff details (fromAgent, toAgent, reason)
  - [ ] Update WorkflowEventService to persist handoff events
  
- [ ] Task 3: Create backend handoff tracking service (AC: 1, 5)
  - [ ] Create IAgentHandoffService interface in Services/Workflows/
  - [ ] Define RecordHandoffAsync(workflowInstanceId, fromAgentId, toAgentId, stepId, reason) method
  - [ ] Define GetHandoffsAsync(workflowInstanceId) method for audit retrieval
  - [ ] Implement AgentHandoffService with database persistence
  - [ ] Inject service into DI container (Program.cs)
  
- [ ] Task 4: Integrate handoff tracking into StepExecutor (AC: 1)
  - [ ] Detect when CurrentAgentId changes between workflow steps
  - [ ] Call IAgentHandoffService.RecordHandoffAsync before agent switch
  - [ ] Set handoff reason based on step metadata (e.g., "Step requires Architect expertise")
  - [ ] Emit handoff event to workflow event log
  - [ ] Ensure handoff is recorded BEFORE new agent starts execution
  
- [ ] Task 5: Create SignalR event for handoff notification (AC: 1)
  - [ ] Add "AGENT_HANDOFF" SignalR event type in ChatHub
  - [ ] Send handoff event to client with payload: { fromAgent, toAgent, stepName, timestamp }
  - [ ] Trigger event from StepExecutor when handoff occurs
  - [ ] Include handoff message template: "Handing off to [AgentName]..."
  
- [ ] Task 6: Create backend DTOs for agent attribution (AC: 2, 3, 4)
  - [ ] Create AgentAttributionDto in DTOs/ folder
  - [ ] Add properties: AgentId, AgentName, AgentDescription, AgentAvatarUrl, Capabilities, CurrentStepResponsibility
  - [ ] Create AgentHandoffDto for API responses
  - [ ] Add properties: FromAgent (AgentAttributionDto), ToAgent (AgentAttributionDto), Timestamp, StepName, Reason
  
- [ ] Task 7: Extend WorkflowStatusResponse for agent attribution (AC: 2)
  - [ ] Add CurrentAgent property (AgentAttributionDto) to WorkflowStatusResponse
  - [ ] Add RecentHandoffs list (AgentHandoffDto[]) for history context
  - [ ] Update WorkflowInstanceService to populate agent attribution data
  - [ ] Ensure GET /api/v1/workflows/{id}/status includes agent info
  
- [ ] Task 8: Create audit log endpoint (AC: 5)
  - [ ] Add GET /api/v1/workflows/{id}/handoffs endpoint in WorkflowsController
  - [ ] Return list of AgentHandoffDto with pagination support
  - [ ] Add authorization check (user must own workflow or have admin role)
  - [ ] Include filtering by date range (optional query params)
  - [ ] Document endpoint in OpenAPI/Swagger
  
- [ ] Task 9: Frontend - Create AgentAttribution component (AC: 2, 4)
  - [ ] Create src/components/AgentAttribution.tsx
  - [ ] Display agent avatar (use placeholder or Ant Design Avatar)
  - [ ] Display agent name and timestamp
  - [ ] Add hover tooltip showing: agent description, capabilities, current step responsibility
  - [ ] Style with Tailwind CSS for consistency
  - [ ] Make component reusable for chat messages and handoff indicators
  
- [ ] Task 10: Frontend - Integrate AgentAttribution into ChatMessage (AC: 2)
  - [ ] Import AgentAttribution component into ChatMessage.tsx
  - [ ] Add agentAttribution prop to ChatMessage interface
  - [ ] Render AgentAttribution at top of each agent message
  - [ ] Ensure distinct visual styling for different agents (use avatar colors)
  - [ ] Add visual separator between different agents' message groups
  
- [ ] Task 11: Frontend - Create AgentHandoffIndicator component (AC: 1)
  - [ ] Create src/components/AgentHandoffIndicator.tsx
  - [ ] Display handoff message: "Handing off to [AgentName]..."
  - [ ] Show transition animation (fade in, slide effect)
  - [ ] Include fromAgent and toAgent avatars side by side
  - [ ] Add timestamp display
  - [ ] Style as distinct system message (different from user/agent messages)
  
- [ ] Task 12: Frontend - Handle AGENT_HANDOFF SignalR event (AC: 1)
  - [ ] Add AGENT_HANDOFF event handler in SignalR connection setup
  - [ ] Parse handoff payload from backend
  - [ ] Insert AgentHandoffIndicator component into chat message stream
  - [ ] Update current agent state in frontend (Zustand store)
  - [ ] Trigger notification sound/animation (optional, subtle)
  
- [ ] Task 13: Frontend - Display decision attribution (AC: 3)
  - [ ] Identify decision messages (tag from backend or message type)
  - [ ] Add decision banner to decision messages: "Decided by [AgentName] at [timestamp]"
  - [ ] Include expandable reasoning section (collapse/expand toggle)
  - [ ] Style decision messages distinctly (border, background color)
  - [ ] Add decision icon (checkmark or badge)
  
- [ ] Task 14: Frontend - Create handoff audit log view (AC: 5)
  - [ ] Create src/pages/WorkflowHandoffLog.tsx page (or modal)
  - [ ] Fetch handoffs from GET /api/v1/workflows/{id}/handoffs
  - [ ] Display handoffs in timeline format (chronological order)
  - [ ] Show fromAgent, toAgent, timestamp, stepName, reason for each handoff
  - [ ] Add filtering by date range or agent
  - [ ] Add export functionality (CSV or JSON download)
  
- [ ] Task 15: Write backend unit tests
  - [ ] Test AgentHandoffService.RecordHandoffAsync creates record
  - [ ] Test AgentHandoffService.GetHandoffsAsync returns correct handoffs
  - [ ] Test StepExecutor detects agent changes and calls handoff service
  - [ ] Test WorkflowsController GET /handoffs endpoint authorization
  - [ ] Test WorkflowStatusResponse includes CurrentAgent and RecentHandoffs
  
- [ ] Task 16: Write backend integration tests
  - [ ] Test full workflow with multiple agent handoffs
  - [ ] Test handoff event propagation to SignalR clients
  - [ ] Test audit log retrieval with pagination
  - [ ] Test handoff persistence and retrieval across workflow steps
  
- [ ] Task 17: Write frontend component tests
  - [ ] Test AgentAttribution renders correctly with all props
  - [ ] Test AgentAttribution tooltip displays on hover
  - [ ] Test AgentHandoffIndicator displays handoff message
  - [ ] Test ChatMessage displays agent attribution
  - [ ] Test decision attribution banner renders
  
- [ ] Task 18: Write BDD acceptance tests (AC: 1-5)
  - [ ] Write Given/When/Then for handoff indicator display (AC: 1)
  - [ ] Write Given/When/Then for chat history with agent attribution (AC: 2)
  - [ ] Write Given/When/Then for decision attribution (AC: 3)
  - [ ] Write Given/When/Then for agent tooltip (AC: 4)
  - [ ] Write Given/When/Then for audit log query (AC: 5)

---

## Dev Notes

### Architecture Context

**Agent Collaboration Architecture** (from Story 5.1-5.3):
- Agent definitions in `Services/Workflows/Agents/` (Story 5.1)
- AgentRegistry provides agent metadata: Name, Description, Capabilities, Avatar
- AgentRouter handles agent-to-agent communication (Story 5.2)
- SharedContext provides workflow context to agents (Story 5.3)
- StepExecutor orchestrates step execution and agent assignment

**SignalR Real-Time Communication** (from Epic 3):
- ChatHub.cs handles real-time message streaming
- SignalR events pattern: Clients.Caller.SendAsync(eventName, payload)
- Frontend SignalR connection in src/services/signalr.ts
- Connection recovery and session restoration already implemented

**Workflow Event Logging** (from Epic 4):
- WorkflowEvent model in Models/Workflows/WorkflowEvent.cs
- WorkflowEventService persists events to event_logs table
- Event types: StepStarted, StepCompleted, WorkflowPaused, etc.
- Metadata stored as JSONB for flexible event data

**Frontend Component Patterns** (from Epic 3):
- React + TypeScript with strict mode
- Ant Design components for UI consistency
- Tailwind CSS for custom styling
- Zustand for state management
- Components in src/components/ folder
- Chat components: ChatMessage, ChatContainer, ChatInput

### Key Technical Requirements

**Database Schema**:
- Create `agent_handoffs` table with: id (UUID), workflow_instance_id (FK), from_agent_id, to_agent_id, timestamp, workflow_step_id, reason (text)
- Index on workflow_instance_id for fast retrieval
- Foreign key to workflow_instances table

**SignalR Event Pattern**:
```csharp
await Clients.Caller.SendAsync("AGENT_HANDOFF", new {
    FromAgent = new { Id, Name, AvatarUrl },
    ToAgent = new { Id, Name, AvatarUrl },
    StepName = currentStep.Name,
    Timestamp = DateTimeOffset.UtcNow,
    Message = $"Handing off to {toAgent.Name}..."
});
```

**Frontend Event Handling**:
```typescript
connection.on("AGENT_HANDOFF", (handoff: AgentHandoff) => {
  // Insert handoff indicator into chat
  addMessage({
    type: "handoff",
    fromAgent: handoff.FromAgent,
    toAgent: handoff.ToAgent,
    timestamp: handoff.Timestamp
  });
});
```

**Agent Attribution Display**:
- Use Ant Design Avatar component for agent icons
- Color-code agents (use consistent colors: PM=blue, Architect=purple, Dev=green)
- Display agent name with typography: font-semibold, text-sm
- Tooltip with Ant Design Tooltip component

**Decision Attribution Pattern**:
- Detect decision messages by checking message metadata (isDecision flag)
- Display banner: "Decided by {AgentName} at {timestamp}"
- Include reasoning in expandable section (Ant Design Collapse)

### File Structure Requirements

**Backend Files**:
```
src/bmadServer.ApiService/
  Models/Workflows/
    AgentHandoff.cs (NEW)
  DTOs/
    AgentAttributionDto.cs (NEW)
    AgentHandoffDto.cs (NEW)
  Services/Workflows/
    IAgentHandoffService.cs (NEW)
    AgentHandoffService.cs (NEW)
  Migrations/
    {timestamp}_AddAgentHandoffsTable.cs (NEW)
```

**Frontend Files**:
```
src/frontend/src/
  components/
    AgentAttribution.tsx (NEW)
    AgentHandoffIndicator.tsx (NEW)
  pages/
    WorkflowHandoffLog.tsx (NEW - optional, or modal)
  types/
    agent.ts (NEW - TypeScript interfaces)
```

### Testing Standards

**Unit Tests** (xUnit):
- Test coverage: 80%+ for new services
- Mock dependencies with Moq
- Test file naming: {ClassName}Tests.cs
- Location: src/bmadServer.Tests/Unit/Services/Workflows/

**Integration Tests** (xUnit + WebApplicationFactory):
- Test full handoff flow with database persistence
- Test SignalR event propagation
- Location: src/bmadServer.Tests/Integration/Workflows/

**BDD Tests** (SpecFlow):
- One .feature file per acceptance criterion
- Location: src/bmadServer.BDD.Tests/Features/AgentHandoff.feature
- Step definitions in src/bmadServer.BDD.Tests/StepDefinitions/

**Frontend Tests** (Vitest + React Testing Library):
- Component tests for AgentAttribution, AgentHandoffIndicator
- Mock SignalR connection
- Test file naming: {ComponentName}.test.tsx
- Location: src/frontend/src/components/

### Dependencies from Previous Stories

**Story 5.1 (Agent Registry)** - REQUIRED:
- AgentRegistry provides agent metadata for attribution display
- Agent definitions include Name, Description, Capabilities, AvatarUrl
- Must load agent details from registry when displaying attribution

**Story 5.2 (Agent-to-Agent Messaging)** - INFORMATIONAL:
- AgentRouter handles agent communication
- Handoff tracking is separate from messaging (handoff = step transition)
- Messaging and handoffs are complementary but independent

**Story 5.3 (Shared Workflow Context)** - INFORMATIONAL:
- SharedContext provides workflow state to agents
- Handoff tracking should record which agent accessed context
- Context changes can trigger handoffs (not implemented in this story)

### Learnings from Previous Epic 5 Stories

**From Story 5.1 (Agent Registry)**:
- AgentRegistry is singleton, injected as `IAgentRegistry`
- Agent definitions use AgentId (string, kebab-case: "product-manager")
- Agent metadata includes Capabilities list (used for routing)
- Avatar URLs can be placeholders initially (use initials or default icons)

**From Story 5.2 (Agent-to-Agent Messaging)**:
- Message persistence uses JSONB columns for flexible metadata
- Timeout handling is critical (30s timeout, retry once)
- All inter-agent communication is logged for transparency
- Frontend displays agent messages with visual distinction

**From Story 5.3 (Shared Workflow Context)**:
- Optimistic concurrency control using version numbers
- Context size management required (summarization if >50k tokens)
- Context persistence happens before step completion
- Frontend should display context updates in real-time

### UX Design Guidelines

**Invisible Orchestration Principle** (from UX spec):
- "Multi-agent orchestration disguised as single conversation"
- Handoffs should feel natural, not jarring
- Use subtle transitions and animations
- Keep handoff indicators concise (one line)

**Agent Attribution Transparency** (from UX spec):
- "Attribution transparency showing source of insights"
- Always show which agent made decisions
- Display reasoning for important decisions
- Enable users to trace decision lineage

**Visual Design Standards** (from UX spec):
- Agent avatars: 32px diameter for chat, 24px for attribution badges
- Agent colors: Primary blue for system, distinct colors per agent type
- Typography: H3 (20px) for agent names, Caption (14px) for timestamps
- Spacing: 12px vertical gap between different agents' message groups

**Decision Crystallization** (from UX spec):
- Decisions are visually distinct (border, background)
- Attribution always visible: "Decided by [Agent] at [time]"
- Reasoning accessible but not overwhelming (collapse/expand)
- Decision icons: checkmark or badge for quick identification

### API Endpoint Specifications

**GET /api/v1/workflows/{id}/handoffs**:
- Authorization: JWT required, user must own workflow or have admin role
- Query params: ?page=1&pageSize=20&fromDate=2024-01-01&toDate=2024-12-31
- Response: PagedResult<AgentHandoffDto>
- Status codes: 200 OK, 401 Unauthorized, 403 Forbidden, 404 Not Found

**GET /api/v1/workflows/{id}/status** (EXTENDED):
- Add CurrentAgent and RecentHandoffs to existing response
- No breaking changes to existing fields
- CurrentAgent: AgentAttributionDto | null (null if no workflow active)
- RecentHandoffs: AgentHandoffDto[] (last 5 handoffs)

### Performance Considerations

- Handoff recording is non-blocking (fire-and-forget with background task)
- Audit log queries use pagination (default 20 per page, max 100)
- Frontend caches agent metadata to avoid repeated lookups
- SignalR event payload kept minimal (<1KB)

### Security Considerations

- Audit log access restricted to workflow owner and admins
- No PII in handoff reason field (use generic descriptions)
- Handoff events logged for compliance and auditing
- Frontend validates agent IDs against known agents from registry

### Edge Cases and Error Handling

- **Agent not found in registry**: Display "Unknown Agent" with placeholder avatar
- **Handoff service unavailable**: Log error, continue workflow execution (handoff tracking is non-critical)
- **SignalR connection lost during handoff**: Handoff recorded in DB, client retrieves on reconnect
- **Multiple rapid handoffs**: Each handoff recorded separately, frontend debounces display (max 1 per second)
- **Handoff to same agent**: Allow (rare but valid, e.g., multi-step agent execution)

### References

- [Source: _bmad-output/planning-artifacts/epics.md, Epic 5 - Story 5.4]
- [Source: _bmad-output/planning-artifacts/architecture.md, Agent Router Architecture]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md, Agent Attribution Design]
- [Source: _bmad-output/implementation-artifacts/5-1-agent-registry-configuration.md, Agent Registry Implementation]
- [Source: _bmad-output/implementation-artifacts/5-2-agent-to-agent-messaging.md, Messaging Patterns]
- [Source: _bmad-output/implementation-artifacts/5-3-shared-workflow-context.md, Context Management]

---

## Dev Agent Record

### Agent Model Used

_To be filled by dev agent_

### Debug Log References

_To be filled by dev agent_

### Completion Notes List

_To be filled by dev agent_

### File List

_To be filled by dev agent_
