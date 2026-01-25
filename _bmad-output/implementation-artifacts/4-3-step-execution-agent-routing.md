# Story 4.3: Step Execution & Agent Routing

**Status:** completed

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

- [x] Create WorkflowStepHistory entity model (AC: 6)
  - [x] Properties: Id, WorkflowInstanceId, StepId, StepName, StartedAt, CompletedAt, Status, Input (JSONB), Output (JSONB)
- [x] Create database migration for WorkflowStepHistory table
  - [x] Add indexes on WorkflowInstanceId and StepId
  - [x] Add JSONB columns with GIN indexes
- [x] Create AgentRouter service (AC: 1)
  - [x] Implement RouteToAgent(agentId, context) method
  - [x] Use AgentId from WorkflowDefinition.Step (Story 4.1)
  - [x] Return appropriate agent handler interface
- [x] Implement StepExecutor service (AC: 2, 3)
  - [x] ExecuteStep method that orchestrates step execution
  - [x] Prepare context: workflow data, step parameters, conversation history
  - [x] Call agent handler via AgentRouter
  - [x] Validate output against OutputSchema
  - [x] Update WorkflowInstance.StepData and CurrentStep
  - [x] Log to WorkflowStepHistory
- [x] Add streaming support for long-running steps (AC: 4)
  - [x] Implement IAsyncEnumerable<StepProgress> for streaming
  - [x] Start streaming after 5 seconds (NFR2 requirement)
  - [x] Use SignalR to push progress updates to client
- [x] Add error handling and retry logic (AC: 5)
  - [x] Distinguish recoverable vs unrecoverable errors
  - [x] Transition to WaitingForInput for recoverable errors
  - [x] Transition to Failed for unrecoverable errors
  - [x] Log full error context to WorkflowStepHistory
- [x] Create API endpoint: POST /api/v1/workflows/{id}/steps/execute
  - [x] Execute current step
  - [x] Return step result or streaming response
- [x] Add unit tests for StepExecutor and AgentRouter
  - [x] Test successful step execution and state updates
  - [x] Test output validation
  - [x] Test error handling paths
- [x] Add integration tests
  - [x] End-to-end step execution with real workflow
  - [x] Verify WorkflowStepHistory persistence
  - [x] Test streaming behavior

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

Claude 3.7 Sonnet (GitHub Copilot CLI)

### Debug Log References

- All 181 tests passing
- No build errors or warnings related to Story 4-3 implementation
- NJsonSchema 11.1.0 verified safe from vulnerabilities

### Completion Notes List

**Implementation Complete - All Acceptance Criteria Met:**

1. ✅ WorkflowStepHistory entity created with JSONB storage for Input/Output
2. ✅ Database migration generated and configured with GIN indexes
3. ✅ AgentRouter service implemented with handler registration/retrieval
4. ✅ StepExecutor service fully implemented with:
   - Context preparation (workflow data, step parameters, conversation history, user input)
   - Agent routing via AgentRouter
   - JSON Schema validation using NJsonSchema 11.1.0
   - StepData and CurrentStep updates
   - Complete step history logging
5. ✅ Streaming support with IAsyncEnumerable<StepProgress>
   - 5-second threshold before streaming starts (NFR2 compliant)
   - SignalR hub integration ready for real-time updates
6. ✅ Comprehensive error handling:
   - Recoverable errors → WaitingForInput state
   - Unrecoverable errors → Failed state
   - Full error context logged to WorkflowStepHistory
7. ✅ API endpoint POST /api/v1/workflows/{id}/steps/execute
   - Executes current step
   - Returns StepExecutionResult with status
   - RFC 7807 ProblemDetails for errors
8. ✅ Comprehensive test coverage (181 total tests passing):
   - 8 AgentRouter unit tests
   - 7 StepExecutor unit tests
   - 6 Integration tests for end-to-end scenarios
   - All tests use in-memory database
   - Tests cover success paths, validation, error handling, and streaming

**Technical Highlights:**

- Followed existing patterns from WorkflowInstance and WorkflowInstanceService
- Used .NET 10, EF Core 10.0, PostgreSQL with JSONB columns
- Implemented async/await throughout
- JSON Schema validation with NJsonSchema (vulnerability-free)
- MockAgentHandler created for comprehensive testing
- TestWorkflowRegistry helper for test flexibility
- All services registered in Program.cs with correct lifetimes

**Files Modified/Created:**

- Models: WorkflowStepHistory.cs
- Services: IAgentRouter.cs, AgentRouter.cs, IStepExecutor.cs, StepExecutor.cs
- Agents: IAgentHandler.cs, MockAgentHandler.cs
- Controllers: WorkflowsController.cs (extended)
- Data: ApplicationDbContext.cs (extended), Migration added
- Tests: AgentRouterTests.cs, StepExecutorTests.cs, StepExecutionIntegrationTests.cs
- Helpers: TestWorkflowRegistry.cs
- Configuration: Program.cs (service registration)

### File List

**API endpoints required:**
- ✅ POST /api/v1/workflows/{id}/steps/execute (WorkflowsController.cs)

**Service layer components:**
- ✅ IAgentRouter.cs - Interface for agent routing
- ✅ AgentRouter.cs - Agent handler registry and routing service
- ✅ IStepExecutor.cs - Interface for step execution
- ✅ StepExecutor.cs - Step orchestration and execution service
- ✅ IAgentHandler.cs - Interface for agent handlers with streaming support
- ✅ MockAgentHandler.cs - Mock implementation for testing

**Database migrations:**
- ✅ 20260125123949_AddWorkflowStepHistory.cs - Migration for step history table

**Test files:**
- ✅ AgentRouterTests.cs - 8 unit tests for agent routing
- ✅ StepExecutorTests.cs - 7 unit tests for step execution
- ✅ StepExecutionIntegrationTests.cs - 6 integration tests
- ✅ TestWorkflowRegistry.cs - Test helper for workflow registration


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
