# Story 5.3: Shared Workflow Context

**Story ID:** E5-S3  
**Epic:** Epic 5 - Multi-Agent Collaboration  
**Points:** 5  
**Status:** ready-for-dev

---

## Story

As an agent (Designer),  
I want access to the full workflow context,  
so that I can make decisions informed by previous steps.

---

## Acceptance Criteria

**Given** a workflow has multiple completed steps  
**When** an agent receives a request  
**Then** it has access to SharedContext containing: all step outputs, decision history, user preferences, artifact references

**Given** an agent needs specific prior output  
**When** it queries SharedContext.GetStepOutput(stepId)  
**Then** it receives the structured output from that step  
**And** null is returned if step hasn't completed

**Given** an agent produces output  
**When** the step completes  
**Then** the output is automatically added to SharedContext  
**And** subsequent agents can access it immediately

**Given** context size grows large  
**When** the context exceeds token limits  
**Then** the system summarizes older context while preserving key decisions  
**And** full context remains available in database for reference

**Given** concurrent agents access context  
**When** simultaneous reads/writes occur  
**Then** optimistic concurrency control prevents conflicts  
**And** version numbers track context changes

---

## Tasks / Subtasks

- [ ] Task 1: Create SharedContext model (AC: 1)
  - [ ] Define SharedContext class with required properties
  - [ ] Add StepOutputs dictionary (stepId → JsonDocument)
  - [ ] Add DecisionHistory list (track all decisions made)
  - [ ] Add UserPreferences dictionary (preferences passed from workflow)
  - [ ] Add ArtifactReferences list (references to generated artifacts)
  - [ ] Add concurrency control fields (_version, _lastModifiedAt, _lastModifiedBy)
  - [ ] Add validation attributes
- [ ] Task 2: Create ISharedContextService interface (AC: 1-3)
  - [ ] Define GetContextAsync(workflowInstanceId) method
  - [ ] Define GetStepOutputAsync(workflowInstanceId, stepId) method
  - [ ] Define AddStepOutputAsync(workflowInstanceId, stepId, output) method
  - [ ] Define UpdateContextAsync(workflowInstanceId, context) method
  - [ ] Add CancellationToken support
- [ ] Task 3: Implement SharedContextService (AC: 1-5)
  - [ ] Implement GetContextAsync with database retrieval
  - [ ] Implement GetStepOutputAsync with null handling (AC: 2)
  - [ ] Implement AddStepOutputAsync with immediate persistence (AC: 3)
  - [ ] Implement optimistic concurrency control (AC: 5)
  - [ ] Add version number tracking
  - [ ] Add concurrency conflict detection and retry logic
  - [ ] Add error handling for missing workflows
- [ ] Task 4: Integrate SharedContext into WorkflowInstance (AC: 1, 3)
  - [ ] Add SharedContext JSON column to WorkflowInstance entity
  - [ ] Update WorkflowInstance model to include SharedContext property
  - [ ] Create EF Core migration
  - [ ] Update ApplicationDbContext configuration
- [ ] Task 5: Integrate SharedContext into AgentContext (AC: 1)
  - [ ] Add SharedContext property to AgentContext class
  - [ ] Update StepExecutor to load SharedContext before agent execution
  - [ ] Ensure agents receive full context in every execution
- [ ] Task 6: Implement context size management (AC: 4)
  - [ ] Define token limit threshold (e.g., 50,000 tokens)
  - [ ] Implement context size calculation
  - [ ] Create summarization service interface
  - [ ] Implement basic summarization (preserve decisions, summarize details)
  - [ ] Store full context in database for reference
  - [ ] Add configuration for token limit
- [ ] Task 7: Update StepExecutor to persist agent outputs (AC: 3)
  - [ ] After agent execution, extract output from AgentResult
  - [ ] Call SharedContextService.AddStepOutputAsync with stepId and output
  - [ ] Ensure persistence happens before marking step complete
  - [ ] Handle persistence failures gracefully
- [ ] Task 8: Write unit tests
  - [ ] Test SharedContextService.GetContextAsync
  - [ ] Test SharedContextService.GetStepOutputAsync returns null for missing step
  - [ ] Test SharedContextService.AddStepOutputAsync
  - [ ] Test optimistic concurrency control (version conflicts)
  - [ ] Test context size management and summarization
  - [ ] Test concurrent access with version tracking
- [ ] Task 9: Write integration tests
  - [ ] Test multi-step workflow with context accumulation
  - [ ] Test agent accessing previous step outputs
  - [ ] Test context persistence to database
  - [ ] Test concurrent agents accessing same context
  - [ ] Test context summarization when size limit exceeded
  - [ ] Test version conflict resolution
- [ ] Task 10: Update dependency injection
  - [ ] Register ISharedContextService
  - [ ] Configure context size limits
  - [ ] Configure summarization service if needed
- [ ] Task 11: Update API documentation
  - [ ] Document SharedContext structure
  - [ ] Document concurrency control mechanism

---

## Dev Notes

### Epic 5 Context

This is the **THIRD story** in Epic 5 (Multi-Agent Collaboration). Epic 5 enables seamless collaboration between BMAD agents through shared context, messaging, handoffs, and human approval gates.

**Epic 5 Goal:** Enable intelligent agent collaboration with transparency for users while maintaining consistent workflow context.

**Epic 5 Stories:**
- **5.1 (READY):** Agent Registry & Configuration - Centralized agent definitions with capabilities
- **5.2 (READY):** Agent-to-Agent Messaging - Direct agent communication with logging
- **5.3 (THIS STORY):** Shared Workflow Context - Unified context access for all agents
- **5.4 (NEXT):** Agent Handoff & Attribution - Visible agent transitions and responsibility
- **5.5 (FUTURE):** Human Approval for Low-Confidence Decisions - Safety gates for uncertain decisions

**This Story's Role:** Provides the foundational shared context that allows agents to make informed decisions based on all previous workflow steps, enabling true multi-agent collaboration.

---

### Architecture Context

#### Existing Workflow Infrastructure (Epic 4)

From Epic 4, the workflow orchestration system has these critical components:

**WorkflowInstance Model:**
```csharp
public class WorkflowInstance
{
    public Guid Id { get; set; }
    public required string WorkflowDefinitionId { get; set; }
    public Guid UserId { get; set; }
    public int CurrentStep { get; set; }
    public WorkflowStatus Status { get; set; }
    public JsonDocument? StepData { get; set; }
    public JsonDocument? Context { get; set; }  // <-- Currently stores workflow-level context
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    // ...
}
```

**AgentContext Class** (passed to all agents):
```csharp
public class AgentContext
{
    public required Guid WorkflowInstanceId { get; init; }
    public required string StepId { get; init; }
    public required string StepName { get; init; }
    public required JsonDocument? WorkflowContext { get; init; }  // <-- General workflow context
    public required JsonDocument? StepData { get; init; }          // <-- Current step data
    public required JsonDocument? StepParameters { get; init; }
    public required List<ConversationMessage> ConversationHistory { get; init; }
    public required string? UserInput { get; init; }
}
```

**Current Context Storage:**
- `WorkflowInstance.Context`: Stores general workflow-level context (JsonDocument)
- `WorkflowInstance.StepData`: Stores current step's data (JsonDocument)
- **LIMITATION:** No structured access to previous step outputs; agents can't easily query "what did step X produce?"

#### What This Story Adds

**SharedContext Enhancement:**
- Extends WorkflowInstance with structured SharedContext
- Provides indexed access to all step outputs: `GetStepOutput(stepId)`
- Tracks decision history across all steps
- Stores user preferences for agent personalization
- Manages artifact references (files/documents generated)
- Implements concurrency control for multi-agent safety

**Integration Points:**
1. **StepExecutor**: Load SharedContext → Pass to AgentContext → Persist agent output
2. **AgentContext**: Add SharedContext property for agent access
3. **WorkflowInstance**: Add SharedContext JSONB column
4. **ISharedContextService**: New service for context management

---

### Technical Requirements

#### Database Schema

**WorkflowInstance Table Update:**
```sql
-- Add SharedContext column to existing WorkflowInstance table
ALTER TABLE "WorkflowInstances" 
ADD COLUMN "SharedContext" jsonb NULL;

-- Add index for efficient context queries
CREATE INDEX idx_workflow_instances_shared_context 
ON "WorkflowInstances" USING gin("SharedContext");
```

**SharedContext JSON Structure:**
```json
{
  "stepOutputs": {
    "step-1": { /* JsonDocument from step 1 */ },
    "step-2": { /* JsonDocument from step 2 */ }
  },
  "decisionHistory": [
    {
      "stepId": "step-1",
      "decision": "selected-architecture-pattern",
      "timestamp": "2026-01-25T10:00:00Z",
      "agentId": "architect"
    }
  ],
  "userPreferences": {
    "verbosityLevel": "detailed",
    "technicalDepth": "expert"
  },
  "artifactReferences": [
    {
      "stepId": "step-2",
      "artifactId": "doc-123",
      "artifactType": "architecture-diagram",
      "path": "/artifacts/doc-123.md"
    }
  ],
  "_version": 5,
  "_lastModifiedAt": "2026-01-25T10:05:00Z",
  "_lastModifiedBy": "agent-designer"
}
```

#### C# Models

**SharedContext Class:**
```csharp
namespace bmadServer.ApiService.Models.Workflows;

public class SharedContext
{
    /// <summary>
    /// Outputs from all completed steps, indexed by stepId
    /// </summary>
    public Dictionary<string, JsonDocument> StepOutputs { get; set; } = new();
    
    /// <summary>
    /// History of all decisions made across workflow steps
    /// </summary>
    public List<DecisionRecord> DecisionHistory { get; set; } = new();
    
    /// <summary>
    /// User preferences for agent personalization
    /// </summary>
    public Dictionary<string, string> UserPreferences { get; set; } = new();
    
    /// <summary>
    /// References to generated artifacts (documents, diagrams, etc.)
    /// </summary>
    public List<ArtifactReference> ArtifactReferences { get; set; } = new();
    
    // Concurrency control (per architecture.md pattern)
    public int _version { get; set; } = 1;
    public DateTime _lastModifiedAt { get; set; } = DateTime.UtcNow;
    public string _lastModifiedBy { get; set; } = string.Empty;
}

public class DecisionRecord
{
    public required string StepId { get; init; }
    public required string Decision { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public string? AgentId { get; init; }
    public string? Reasoning { get; init; }
}

public class ArtifactReference
{
    public required string StepId { get; init; }
    public required string ArtifactId { get; init; }
    public required string ArtifactType { get; init; }
    public required string Path { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
```

#### Service Interface

**ISharedContextService:**
```csharp
namespace bmadServer.ApiService.Services.Workflows;

public interface ISharedContextService
{
    /// <summary>
    /// Get the full shared context for a workflow instance
    /// </summary>
    Task<SharedContext?> GetContextAsync(Guid workflowInstanceId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get output from a specific step (returns null if step not completed)
    /// </summary>
    Task<JsonDocument?> GetStepOutputAsync(Guid workflowInstanceId, string stepId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Add output from a completed step to shared context
    /// </summary>
    Task AddStepOutputAsync(Guid workflowInstanceId, string stepId, JsonDocument output, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Update shared context with optimistic concurrency control
    /// </summary>
    Task<bool> UpdateContextAsync(Guid workflowInstanceId, SharedContext context, CancellationToken cancellationToken = default);
}
```

---

### Context Size Management Strategy

**Problem:** Long workflows accumulate large contexts that exceed token limits for agent prompts.

**Solution (AC: 4):**

1. **Token Limit Threshold:** Configure max context size (default: 50,000 tokens)
2. **Size Calculation:** Estimate tokens from JSON content (approximate: characters / 4)
3. **Summarization Strategy:**
   - **Preserve:** All decision history (critical for workflow logic)
   - **Preserve:** Most recent 3 step outputs (full detail)
   - **Summarize:** Older step outputs (extract key fields only)
   - **Archive:** Full context stored in database for reference
4. **Implementation:**
   ```csharp
   if (EstimateTokenCount(context) > TokenLimit)
   {
       var summarized = await SummarizeOlderSteps(context);
       context.StepOutputs = summarized.StepOutputs;
       // DecisionHistory always preserved in full
   }
   ```

---

### Optimistic Concurrency Control (AC: 5)

**Scenario:** Multiple agents might modify SharedContext simultaneously (rare in sequential workflows but possible with parallel steps in future).

**Implementation Pattern (from architecture.md):**

```csharp
public async Task<bool> UpdateContextAsync(Guid workflowInstanceId, SharedContext context, CancellationToken ct)
{
    var instance = await _dbContext.WorkflowInstances
        .FirstOrDefaultAsync(w => w.Id == workflowInstanceId, ct);
    
    if (instance == null) return false;
    
    var currentContext = DeserializeContext(instance.SharedContext);
    
    // Check version for conflicts
    if (currentContext._version != context._version)
    {
        _logger.LogWarning("Context version conflict for workflow {Id}. Expected {Expected}, got {Actual}",
            workflowInstanceId, currentContext._version, context._version);
        return false; // Caller must reload and retry
    }
    
    // Increment version and update
    context._version++;
    context._lastModifiedAt = DateTime.UtcNow;
    
    instance.SharedContext = JsonDocument.Parse(JsonSerializer.Serialize(context));
    await _dbContext.SaveChangesAsync(ct);
    
    return true;
}
```

**Retry Logic:** Callers should retry on version conflict (max 3 attempts with exponential backoff).

---

### Integration with AgentContext

**Current AgentContext (Story 4.3):**
```csharp
public class AgentContext
{
    public required JsonDocument? WorkflowContext { get; init; }  // General context
    // ...
}
```

**Enhanced AgentContext (This Story):**
```csharp
public class AgentContext
{
    public required JsonDocument? WorkflowContext { get; init; }  // Legacy general context
    public SharedContext? SharedContext { get; init; }            // NEW: Structured shared context
    // ...
}
```

**StepExecutor Enhancement:**
```csharp
public async Task<AgentResult> ExecuteStepAsync(...)
{
    // Load shared context
    var sharedContext = await _sharedContextService.GetContextAsync(workflowInstanceId);
    
    // Build agent context with shared context
    var agentContext = new AgentContext
    {
        // ... existing fields ...
        SharedContext = sharedContext  // NEW
    };
    
    // Execute agent
    var result = await agentHandler.ExecuteAsync(agentContext, ct);
    
    // Persist agent output to shared context
    if (result.Success && result.Output != null)
    {
        await _sharedContextService.AddStepOutputAsync(
            workflowInstanceId, 
            currentStep.Id, 
            result.Output, 
            ct);
    }
    
    return result;
}
```

---

### Learnings from Previous Stories

#### From Epic 4 Retrospective:

1. **Integration is Explicit:** Don't assume components will "just work together"
   - **This Story:** Explicitly integrate SharedContextService with StepExecutor
   - **Action:** Add integration test for StepExecutor → SharedContext persistence

2. **Entity → DbSet → Configuration:** All three must be done together
   - **This Story:** Add SharedContext column → Update WorkflowInstance model → Create migration
   - **Action:** Verify all three in code review checklist

3. **Real Implementations Required:** No placeholder code
   - **This Story:** Implement full SharedContextService with real database operations
   - **Action:** Code review must verify no "TODO" or "placeholder" comments

#### From Story 5.1 (Agent Registry):

- **Pattern:** Service → Interface → DI Registration
- **This Story:** Follow same pattern for ISharedContextService

#### From Story 5.2 (Agent Messaging):

- **Pattern:** Logging for transparency
- **This Story:** Log all context updates for audit trail (who modified, when, what changed)

---

### File Structure

**New Files to Create:**

```
src/bmadServer.ApiService/
├── Models/Workflows/
│   ├── SharedContext.cs           # NEW: SharedContext, DecisionRecord, ArtifactReference
├── Services/Workflows/
│   ├── ISharedContextService.cs   # NEW: Service interface
│   ├── SharedContextService.cs    # NEW: Service implementation

src/bmadServer.ApiService/Migrations/
├── YYYYMMDDHHMMSS_AddSharedContextToWorkflowInstance.cs  # NEW: EF migration

src/bmadServer.Tests/Unit/Services/Workflows/
├── SharedContextServiceTests.cs   # NEW: Unit tests

src/bmadServer.Tests/Integration/Workflows/
├── SharedContextIntegrationTests.cs  # NEW: Integration tests
```

**Files to Modify:**

```
src/bmadServer.ApiService/
├── Models/Workflows/WorkflowInstance.cs       # Add SharedContext property
├── Services/Workflows/Agents/IAgentHandler.cs # Add SharedContext to AgentContext
├── Services/Workflows/StepExecutor.cs         # Load/persist SharedContext
├── Data/ApplicationDbContext.cs               # Configure SharedContext column
├── Program.cs                                 # Register ISharedContextService
```

---

### Testing Requirements

#### Unit Tests (SharedContextServiceTests.cs)

1. **GetContextAsync:**
   - Returns context for existing workflow
   - Returns null for non-existent workflow
   - Deserializes JSON correctly

2. **GetStepOutputAsync (AC: 2):**
   - Returns output for completed step
   - Returns null for non-existent step
   - Returns null for workflow without context

3. **AddStepOutputAsync (AC: 3):**
   - Adds new step output to context
   - Updates existing step output
   - Initializes context if null
   - Increments version number

4. **UpdateContextAsync (AC: 5):**
   - Succeeds with matching version
   - Fails with version conflict
   - Increments version on success
   - Updates lastModifiedAt timestamp

5. **Context Size Management (AC: 4):**
   - Detects when context exceeds token limit
   - Summarizes older steps
   - Preserves decision history
   - Preserves recent step outputs

#### Integration Tests (SharedContextIntegrationTests.cs)

1. **Multi-Step Context Accumulation:**
   - Execute 3 steps in sequence
   - Verify each step's output stored in SharedContext
   - Verify step 3 can access outputs from steps 1 and 2

2. **Concurrent Access:**
   - Two agents access same context simultaneously
   - Verify optimistic concurrency control prevents data loss
   - Verify version conflict detection

3. **Context Persistence:**
   - Create context with step outputs
   - Reload workflow instance
   - Verify SharedContext persisted correctly

4. **StepExecutor Integration:**
   - Execute step via StepExecutor
   - Verify agent receives SharedContext in AgentContext
   - Verify agent output persisted to SharedContext
   - Verify next step has access to previous output

---

### Dependencies

**Required from Previous Stories:**
- ✅ Epic 4 (Workflow Orchestration): WorkflowInstance, StepExecutor, AgentContext
- ✅ Story 1.2 (PostgreSQL): Database configured with EF Core
- ⏳ Story 5.1 (Agent Registry): Optional but recommended for agent attribution in DecisionHistory

**NuGet Packages:**
- ✅ `Microsoft.EntityFrameworkCore.Design` (already installed)
- ✅ `Npgsql.EntityFrameworkCore.PostgreSQL` (already installed)
- ✅ `System.Text.Json` (built-in)

**No new packages required.**

---

### Aspire Development Standards

#### PostgreSQL Connection Pattern

This story extends the existing WorkflowInstance table configured in Story 1.2:
- ✅ Connection string automatically injected from Aspire AppHost
- ✅ Pattern: `builder.AddServiceDefaults();` (inherits PostgreSQL reference)
- ✅ Migration: Use `dotnet ef migrations add AddSharedContextToWorkflowInstance`

#### Aspire Service Registration

```csharp
// In Program.cs
builder.Services.AddScoped<ISharedContextService, SharedContextService>();
```

#### Project-Wide Standards

- **Reference:** [PROJECT-WIDE-RULES.md](../../../PROJECT-WIDE-RULES.md)
- **Aspire Docs:** https://aspire.dev
- **GitHub:** https://github.com/dotnet/aspire

---

### Performance Considerations

1. **Context Size:** Monitor SharedContext size and implement summarization as it approaches the 50,000-token context limit (roughly a few hundred KB of JSON, depending on content)
2. **Indexing:** GIN index on SharedContext JSONB for efficient queries
3. **Caching:** Consider IMemoryCache for frequently accessed contexts (future optimization)
4. **Serialization:** Use System.Text.Json for fast serialization/deserialization

---

### Security Considerations

1. **Authorization:** Only agents/users authorized for workflow can access its SharedContext
2. **Audit Trail:** Log all context modifications (who, when, what changed)
3. **Sensitive Data:** Be cautious about storing sensitive data in SharedContext (consider encryption in Phase 2)

---

## References

- **Source:** [epics.md - Story 5.3](../planning-artifacts/epics.md#story-53-shared-workflow-context)
- **Architecture:** [architecture.md - Shared Workflow Context](../planning-artifacts/architecture.md)
- **Epic 4 Retrospective:** [epic-4-retrospective.md](epic-4-retrospective.md)
- **Story 5.1:** [5-1-agent-registry-configuration.md](5-1-agent-registry-configuration.md)
- **Story 5.2:** [5-2-agent-to-agent-messaging.md](5-2-agent-to-agent-messaging.md)
- **Aspire Rules:** [PROJECT-WIDE-RULES.md](../../../PROJECT-WIDE-RULES.md)
- **Aspire Documentation:** https://learn.microsoft.com/en-us/dotnet/aspire/

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
