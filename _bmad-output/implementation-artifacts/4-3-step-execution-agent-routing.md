# Story 4.3: Step Execution & Agent Routing

**Status:** ready-for-dev

## Story

As a user (Marcus), I want workflow steps to automatically route to the correct agent, so that each step is handled by the appropriate specialist.

## Acceptance Criteria

**Given** a workflow is running  
**When** the current step requires an agent  
**Then** the system looks up the AgentId from the step definition  
**And** routes the request to the correct agent handler

**Given** step execution begins  
**When** the agent processes the step  
**Then** the agent receives: workflow context, step parameters, conversation history, user input

**Given** an agent completes a step  
**When** the response is received  
**Then** the step output is validated against OutputSchema  
**And** StepData is updated with the result  
**And** CurrentStep advances to the next step

**Given** step execution takes time  
**When** processing exceeds 5 seconds  
**Then** streaming begins to the client (NFR2)  
**And** the user sees real-time progress

**Given** a step fails  
**When** an error occurs during agent processing  
**Then** the workflow transitions to Failed state (if unrecoverable) or WaitingForInput (if retry possible)  
**And** the error is logged with full context

**Given** I need to track step history  
**When** I query the WorkflowStepHistory table  
**Then** I see all executed steps with: StepId, StartedAt, CompletedAt, Status, Input, Output

## Tasks / Subtasks

- [ ] Create WorkflowStepHistory entity model (AC: 6)
  - [ ] Properties: Id, WorkflowInstanceId, StepId, StepName, StartedAt, CompletedAt, Status, Input (JSONB), Output (JSONB)
- [ ] Create database migration for WorkflowStepHistory table
  - [ ] Add indexes on WorkflowInstanceId and StepId
  - [ ] Add JSONB columns with GIN indexes
- [ ] Create AgentRouter service (AC: 1)
  - [ ] Implement RouteToAgent(agentId, context) method
  - [ ] Use AgentId from WorkflowDefinition.Step (Story 4.1)
  - [ ] Return appropriate agent handler interface
- [ ] Implement StepExecutor service (AC: 2, 3)
  - [ ] ExecuteStep method that orchestrates step execution
  - [ ] Prepare context: workflow data, step parameters, conversation history
  - [ ] Call agent handler via AgentRouter
  - [ ] Validate output against OutputSchema
  - [ ] Update WorkflowInstance.StepData and CurrentStep
  - [ ] Log to WorkflowStepHistory
- [ ] Add streaming support for long-running steps (AC: 4)
  - [ ] Implement IAsyncEnumerable<StepProgress> for streaming
  - [ ] Start streaming after 5 seconds (NFR2 requirement)
  - [ ] Use SignalR to push progress updates to client
- [ ] Add error handling and retry logic (AC: 5)
  - [ ] Distinguish recoverable vs unrecoverable errors
  - [ ] Transition to WaitingForInput for recoverable errors
  - [ ] Transition to Failed for unrecoverable errors
  - [ ] Log full error context to WorkflowStepHistory
- [ ] Create API endpoint: POST /api/v1/workflows/{id}/steps/execute
  - [ ] Execute current step
  - [ ] Return step result or streaming response
- [ ] Add unit tests for StepExecutor and AgentRouter
  - [ ] Test successful step execution and state updates
  - [ ] Test output validation
  - [ ] Test error handling paths
- [ ] Add integration tests
  - [ ] End-to-end step execution with real workflow
  - [ ] Verify WorkflowStepHistory persistence
  - [ ] Test streaming behavior

## Dev Notes

### Architecture Alignment

**Source:** [ARCHITECTURE.md - Workflow Orchestration, Agent System]

- Entity: `src/bmadServer.ApiService/Models/Workflows/WorkflowStepHistory.cs`
- Services: `src/bmadServer.ApiService/Services/Workflows/AgentRouter.cs`, `StepExecutor.cs`
- API: `src/bmadServer.ApiService/Endpoints/WorkflowsEndpoint.cs` (extend)
- Use SignalR Hub from Story 3.1 for streaming
- NFR2: Response time <3s, stream after 5s for long operations

### Technical Requirements

**Framework Stack:**
- .NET 8 with Aspire
- SignalR for real-time streaming (from Epic 3)
- JSON Schema validation for OutputSchema (consider NJsonSchema)

**Agent Routing Strategy:**
- Agent handlers implement IAgentHandler interface
- AgentRouter uses factory pattern or service locator to resolve handlers
- Each agent receives consistent context structure

**Output Validation:**
- Use JSON Schema to validate agent output against step's OutputSchema
- Return clear validation errors if output doesn't match schema
- Consider using FluentValidation or NJsonSchema.Validation

**Streaming Implementation:**
```csharp
public async IAsyncEnumerable<StepProgress> ExecuteStepWithStreaming(...)
{
    var stopwatch = Stopwatch.StartNew();
    // Execute step
    while (!completed)
    {
        if (stopwatch.Elapsed > TimeSpan.FromSeconds(5))
        {
            yield return new StepProgress { ... };
        }
        await Task.Delay(500);
    }
}
```

### File Structure Requirements

```
src/bmadServer.ApiService/
├── Models/
│   └── Workflows/
│       └── WorkflowStepHistory.cs
├── Services/
│   └── Workflows/
│       ├── IAgentRouter.cs
│       ├── AgentRouter.cs
│       ├── IStepExecutor.cs
│       ├── StepExecutor.cs
│       └── Agents/
│           └── IAgentHandler.cs
└── Data/
    └── Migrations/
        └── XXX_CreateWorkflowStepHistory.cs
```

### Dependencies

**From Previous Stories:**
- Story 4.1: WorkflowDefinition.Step with AgentId and OutputSchema
- Story 4.2: WorkflowInstance for state management
- Story 3.1: SignalR Hub for streaming (from Epic 3)

**Future Dependencies:**
- Epic 5: Multi-agent collaboration will extend AgentRouter

**NuGet Packages:**
- NJsonSchema (for JSON Schema validation) - NEW, check for vulnerabilities
- Microsoft.AspNetCore.SignalR (already in project)

### Testing Requirements

**Unit Tests:** `test/bmadServer.Tests/Services/Workflows/`

**Test Coverage:**
- Agent routing to correct handler
- Step execution with valid/invalid outputs
- Schema validation
- Error handling and state transitions
- Streaming behavior

### Integration Notes

**Connection to Future Stories:**
- Story 4.4: Pause will interrupt step execution
- Story 4.5: Cancel will terminate step execution
- Story 4.7: Status API will query WorkflowStepHistory
- Epic 5: Agent collaboration will use AgentRouter

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 4.3]
- [Source: ARCHITECTURE.md - Agent Routing]
- [NFR2: Response time requirements]

## Dev Agent Record

### Agent Model Used

_To be filled by dev agent_

### Debug Log References

_To be filled by dev agent_

### Completion Notes List

_To be filled by dev agent_

### File List

_To be filled by dev agent_
- API endpoints required
- Service layer components
- Database migrations
- Test files


---

## Aspire Development Standards

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
- **Aspire Rules:** [PROJECT-WIDE-RULES.md](../../../PROJECT-WIDE-RULES.md)
- **Aspire Docs:** https://aspire.dev

- Source: [epics.md - Story 4.3](../planning-artifacts/epics.md)
- Architecture: [architecture.md](../planning-artifacts/architecture.md)
- PRD: [prd.md](../planning-artifacts/prd.md)
