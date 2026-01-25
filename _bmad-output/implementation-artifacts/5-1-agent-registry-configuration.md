# Story 5.1: Agent Registry & Configuration

**Status:** ready-for-dev

## Story

As a developer,
I want a centralized agent registry,
so that the system knows all available agents and their capabilities.

## Acceptance Criteria

**Given** I need to define BMAD agents  
**When** I create `Agents/AgentDefinition.cs`  
**Then** it includes: AgentId, Name, Description, Capabilities (list), SystemPrompt, ModelPreference

**Given** agent definitions exist  
**When** I create `Agents/AgentRegistry.cs`  
**Then** it provides: GetAllAgents(), GetAgent(id), GetAgentsByCapability(capability)

**Given** the registry is populated  
**When** I query GetAllAgents()  
**Then** I receive BMAD agents: ProductManager, Architect, Designer, Developer, Analyst, Orchestrator

**Given** each agent has capabilities  
**When** I examine an agent definition  
**Then** capabilities map to workflow steps they can handle (e.g., Architect handles "create-architecture")

**Given** agents have model preferences  
**When** an agent is invoked  
**Then** the system routes to the preferred model (configurable for cost/quality tradeoffs)

## Tasks / Subtasks

- [ ] Task 1: Create AgentDefinition model (AC: 1)
  - [ ] Define AgentId, Name, Description properties
  - [ ] Add Capabilities list property
  - [ ] Add SystemPrompt property
  - [ ] Add ModelPreference property
  - [ ] Add validation attributes
- [ ] Task 2: Create AgentRegistry service (AC: 2)
  - [ ] Implement IAgentRegistry interface
  - [ ] Implement GetAllAgents() method
  - [ ] Implement GetAgent(id) method
  - [ ] Implement GetAgentsByCapability(capability) method
  - [ ] Add dependency injection configuration
- [ ] Task 3: Populate registry with BMAD agents (AC: 3)
  - [ ] Define ProductManager agent
  - [ ] Define Architect agent
  - [ ] Define Designer agent
  - [ ] Define Developer agent
  - [ ] Define Analyst agent
  - [ ] Define Orchestrator agent
- [ ] Task 4: Map capabilities to workflow steps (AC: 4)
  - [ ] Define capability strings
  - [ ] Map capabilities to existing workflow steps
  - [ ] Document capability → step mappings
- [ ] Task 5: Implement model preference routing (AC: 5)
  - [ ] Add model preference configuration
  - [ ] Integrate with AgentRouter
  - [ ] Add configurable model override support
- [ ] Task 6: Write unit tests
  - [ ] Test AgentDefinition validation
  - [ ] Test AgentRegistry GetAllAgents()
  - [ ] Test AgentRegistry GetAgent(id)
  - [ ] Test AgentRegistry GetAgentsByCapability()
  - [ ] Test capability filtering
- [ ] Task 7: Update integration tests
  - [ ] Test agent registry with workflow execution
  - [ ] Test model preference routing
- [ ] Task 8: Update API documentation
  - [ ] Document agent registry endpoints (if exposed)
  - [ ] Document agent capabilities

## Dev Notes

### Epic 5 Context

This is the FIRST story in Epic 5 (Multi-Agent Collaboration). Epic 5 enables seamless collaboration between BMAD agents, allowing them to share context, hand off work, and coordinate on complex tasks.

**Epic 5 Goal:** Enable intelligent agent collaboration with transparency for users.

**Epic 5 Stories:**
- **5.1 (THIS STORY):** Agent Registry & Configuration
- 5.2: Agent-to-Agent Messaging
- 5.3: Shared Workflow Context
- 5.4: Agent Handoff & Attribution
- 5.5: Human Approval for Low-Confidence Decisions

### Architecture Context

#### Existing Agent Infrastructure (Epic 4)

The project already has core agent infrastructure from Epic 4:
- **IAgentHandler** interface: Defines agent execution contract
- **AgentContext** class: Provides step context to agents
- **AgentResult** class: Returns agent execution results
- **AgentRouter** class: Routes steps to registered agent handlers
- **MockAgentHandler**: Placeholder agent for testing

**Location:** `src/bmadServer.ApiService/Services/Workflows/Agents/`

#### Agent Router Current Implementation

From `AgentRouter.cs`:
```csharp
public class AgentRouter : IAgentRouter
{
    private readonly Dictionary<string, IAgentHandler> _handlers = new();
    
    public void RegisterHandler(string agentId, IAgentHandler handler)
    {
        _handlers[agentId] = handler;
    }
    
    public async Task<AgentResult> RouteToAgentAsync(string agentId, AgentContext context, CancellationToken cancellationToken = default)
    {
        if (!_handlers.TryGetValue(agentId, out var handler))
        {
            return new AgentResult
            {
                Success = false,
                ErrorMessage = $"No handler registered for agent: {agentId}",
                IsRetryable = false
            };
        }
        
        return await handler.ExecuteAsync(context, cancellationToken);
    }
}
```

**Key Insight:** AgentRouter currently uses string-based agentId keys. This story formalizes agent definitions with metadata.

### What This Story Adds

This story creates a **formal agent registry layer** above the existing AgentRouter:

1. **AgentDefinition Model:** Metadata about each BMAD agent (name, description, capabilities, system prompt, model preference)
2. **AgentRegistry Service:** Central repository for agent definitions with query capabilities
3. **BMAD Agent Definitions:** Formal definitions for ProductManager, Architect, Designer, Developer, Analyst, Orchestrator
4. **Capability Mapping:** Links agent capabilities to workflow steps they can handle
5. **Model Preference:** Allows agents to specify preferred AI models (e.g., GPT-4 for architect, GPT-3.5 for simple steps)

### Implementation Guidance

#### File Structure

Create these files in `src/bmadServer.ApiService/Services/Workflows/Agents/`:
```
Agents/
├── IAgentHandler.cs (existing)
├── MockAgentHandler.cs (existing)
├── AgentDefinition.cs (NEW)
├── IAgentRegistry.cs (NEW)
├── AgentRegistry.cs (NEW)
└── BmadAgentDefinitions.cs (NEW - static definitions)
```

#### AgentDefinition Model Design

```csharp
public class AgentDefinition
{
    public required string AgentId { get; init; }  // e.g., "product-manager"
    public required string Name { get; init; }  // e.g., "Product Manager"
    public required string Description { get; init; }
    public required List<string> Capabilities { get; init; }  // e.g., ["create-prd", "refine-requirements"]
    public required string SystemPrompt { get; init; }  // AI system prompt for this agent
    public required ModelPreference ModelPreference { get; init; }
}

public class ModelPreference
{
    public string PreferredModel { get; init; } = "gpt-4";  // Default model
    public string? FallbackModel { get; init; }  // If preferred unavailable
    public int MaxTokens { get; init; } = 4000;
    public double Temperature { get; init; } = 0.7;
}
```

#### AgentRegistry Interface

```csharp
public interface IAgentRegistry
{
    IReadOnlyList<AgentDefinition> GetAllAgents();
    AgentDefinition? GetAgent(string agentId);
    IReadOnlyList<AgentDefinition> GetAgentsByCapability(string capability);
}
```

#### BMAD Agent Definitions

Define these 6 agents with their capabilities:

1. **ProductManager**
   - AgentId: "product-manager"
   - Capabilities: ["create-prd", "refine-requirements", "define-personas", "prioritize-features"]
   - SystemPrompt: Focus on business value, user needs, product strategy
   - ModelPreference: GPT-4 (needs strong reasoning)

2. **Architect**
   - AgentId: "architect"
   - Capabilities: ["create-architecture", "define-tech-stack", "adr-creation", "system-design"]
   - SystemPrompt: Focus on technical decisions, scalability, best practices
   - ModelPreference: GPT-4 (complex technical reasoning)

3. **Designer**
   - AgentId: "designer"
   - Capabilities: ["create-ux-design", "define-user-flows", "ui-specifications"]
   - SystemPrompt: Focus on user experience, visual design, accessibility
   - ModelPreference: GPT-4 (creative reasoning)

4. **Developer**
   - AgentId: "developer"
   - Capabilities: ["implement-story", "code-review", "write-tests", "debug-issues"]
   - SystemPrompt: Focus on code quality, testing, implementation details
   - ModelPreference: GPT-4 (code generation)

5. **Analyst**
   - AgentId: "analyst"
   - Capabilities: ["analyze-requirements", "create-test-scenarios", "validate-acceptance-criteria"]
   - SystemPrompt: Focus on quality assurance, edge cases, validation
   - ModelPreference: GPT-3.5 (faster, less complex reasoning)

6. **Orchestrator**
   - AgentId: "orchestrator"
   - Capabilities: ["coordinate-workflow", "route-requests", "manage-handoffs"]
   - SystemPrompt: Focus on workflow coordination, agent collaboration
   - ModelPreference: GPT-4 (meta-reasoning)

#### Integration with Existing AgentRouter

**Important:** This story does NOT replace AgentRouter. It adds a metadata layer.

- AgentRouter continues to handle handler registration and execution
- AgentRegistry provides metadata about available agents
- Future stories (5.2, 5.3) will use AgentRegistry for intelligent routing

#### Capability → Workflow Step Mapping

Document these mappings (in code comments or separate doc):
```
Workflow Step              → Agent(s) with Capability
"create-prd"              → ProductManager
"create-architecture"     → Architect
"create-ux-design"        → Designer
"implement-story"         → Developer
"analyze-requirements"    → Analyst, ProductManager
"code-review"             → Developer
"coordinate-workflow"     → Orchestrator
```

### Previous Story Learnings (Epic 4 Retrospective)

From Epic 4 retrospective, key lessons:

1. **Cross-epic integration needs explicit planning**
   - This story is foundational for Epic 5 - all other stories depend on it
   - Agent definitions here will be used in Stories 5.2-5.5

2. **Production code standards - NO placeholders**
   - Define real agent definitions, not TODO placeholders
   - Real system prompts, real capabilities
   - Mock implementations are fine for testing, but definitions must be complete

3. **Entity registration checklist**
   - Create model class → Define interface → Implement service → Register in DI → Write tests

4. **Integration points for Epic 5**
   - Story 5.2 (Agent-to-Agent Messaging) will use AgentRegistry to discover target agents
   - Story 5.3 (Shared Context) will use agent capabilities for context filtering
   - Story 5.4 (Handoff & Attribution) will use agent names/descriptions for UI display

### Testing Strategy

#### Unit Tests

Location: `src/bmadServer.Tests/Unit/Services/Workflows/`

Create: `AgentRegistryTests.cs`

Test scenarios:
1. GetAllAgents returns 6 BMAD agents
2. GetAgent("product-manager") returns ProductManager definition
3. GetAgent("invalid-id") returns null
4. GetAgentsByCapability("create-prd") returns [ProductManager]
5. GetAgentsByCapability("code-review") returns [Developer]
6. GetAgentsByCapability("unknown-capability") returns empty list
7. Multiple agents can share same capability
8. AgentDefinition validation (required fields)
9. ModelPreference defaults are correct

#### Integration Tests

Location: `src/bmadServer.Tests/Integration/Workflows/`

Create: `AgentRegistryIntegrationTests.cs`

Test scenarios:
1. AgentRegistry resolves from DI container
2. AgentRouter can look up agents from registry
3. Workflow execution can query agent capabilities
4. Model preference is respected during execution

### Dependencies

#### Required Packages
- No new packages required (uses existing .NET libraries)

#### Dependency Injection

Register services in `Program.cs`:
```csharp
// Add agent registry
builder.Services.AddSingleton<IAgentRegistry, AgentRegistry>();
```

#### Database Migrations
- No database changes required for this story
- Agent definitions are code-based (static configuration)
- Future: Could persist to DB for runtime configuration (Phase 2 enhancement)

### Project Structure Notes

#### Alignment with Aspire Architecture

- Follows Aspire service registration patterns
- Uses dependency injection for testability
- No new Aspire packages required
- Compatible with distributed agent execution (future)

#### Code Organization

- Keep agent definitions in `Agents/` subfolder
- Separate interface from implementation
- Use descriptive naming: `IAgentRegistry`, `AgentRegistry`, `AgentDefinition`
- Follow existing patterns from WorkflowRegistry (similar concept)

### Security Considerations

- Agent definitions are read-only (no runtime modification in MVP)
- SystemPrompts contain no secrets (AI model prompts only)
- ModelPreference configuration is trusted (internal service)
- Future: Add RBAC for agent invocation (Story 11.x)

### Performance Considerations

- AgentRegistry can be singleton (static data)
- No database queries (in-memory definitions)
- Capability lookups should be efficient (Dictionary<string, List<AgentDefinition>>)
- Consider caching if agent definitions become dynamic (Phase 2)

### Error Handling

- GetAgent(id) returns null for unknown agents (not exception)
- GetAgentsByCapability returns empty list (not null)
- AgentDefinition validation throws on construction (fail fast)
- Model preference defaults prevent null reference errors

### Documentation Requirements

- XML comments on all public interfaces and classes
- Document capability naming conventions
- Document model preference configuration options
- Add README.md in Agents/ folder explaining agent system

### References

- **Source:** [epics.md - Story 5.1](../planning-artifacts/epics.md#story-51-agent-registry--configuration)
- **Architecture:** [architecture.md - Section 3: Agent Router](../planning-artifacts/architecture.md#3-agent-router-iagentrouter-interface)
- **Epic 4 Retrospective:** [epic-4-retrospective.md](epic-4-retrospective.md#preparation-for-epic-5-multi-agent-collaboration)
- **IAgentHandler:** [src/bmadServer.ApiService/Services/Workflows/Agents/IAgentHandler.cs](../../src/bmadServer.ApiService/Services/Workflows/Agents/IAgentHandler.cs)
- **AgentRouter:** [src/bmadServer.ApiService/Services/Workflows/AgentRouter.cs](../../src/bmadServer.ApiService/Services/Workflows/AgentRouter.cs)
- **WorkflowRegistry (similar pattern):** [src/bmadServer.ServiceDefaults/Services/Workflows/WorkflowRegistry.cs](../../src/bmadServer.ServiceDefaults/Services/Workflows/WorkflowRegistry.cs)

---

## Aspire Development Standards

### PostgreSQL Connection Pattern

This story does NOT require database access. Agent definitions are in-memory configuration.

### Project-Wide Standards

This story follows the Aspire-first development pattern:
- **Reference:** [PROJECT-WIDE-RULES.md](../../../PROJECT-WIDE-RULES.md)
- **Primary Documentation:** https://aspire.dev
- **GitHub:** https://github.com/microsoft/aspire

---

## Dev Agent Record

### Agent Model Used

_To be completed during implementation_

### Debug Log References

_To be added during implementation_

### Completion Notes List

_To be added during implementation_

### File List

_To be populated during implementation_

**Expected files:**
- `src/bmadServer.ApiService/Services/Workflows/Agents/AgentDefinition.cs`
- `src/bmadServer.ApiService/Services/Workflows/Agents/IAgentRegistry.cs`
- `src/bmadServer.ApiService/Services/Workflows/Agents/AgentRegistry.cs`
- `src/bmadServer.ApiService/Services/Workflows/Agents/BmadAgentDefinitions.cs`
- `src/bmadServer.Tests/Unit/Services/Workflows/AgentRegistryTests.cs`
- `src/bmadServer.Tests/Integration/Workflows/AgentRegistryIntegrationTests.cs`
