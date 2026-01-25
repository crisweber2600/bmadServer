# Story 5.2: Agent-to-Agent Messaging

**Status:** ready-for-dev

## Story

As an agent (Architect),
I want to request information from other agents,
so that I can gather inputs needed for my work.

## Acceptance Criteria

**Given** an agent is processing a step  
**When** it needs input from another agent  
**Then** it can call AgentMessaging.RequestFromAgent(targetAgentId, request, context)

**Given** an agent request is made  
**When** the target agent receives it  
**Then** the request includes: sourceAgentId, requestType, payload, workflowContext, conversationHistory

**Given** the target agent processes the request  
**When** a response is generated  
**Then** the response is returned to the source agent  
**And** the exchange is logged for transparency

**Given** agent-to-agent communication occurs  
**When** I check the message format  
**Then** I see: messageId, timestamp, sourceAgent, targetAgent, messageType, content, workflowInstanceId

**Given** an agent request times out (> 30 seconds)  
**When** no response is received  
**Then** the system retries once  
**And** if still no response, returns error to source agent  
**And** the timeout is logged for debugging

## Tasks / Subtasks

- [ ] Task 1: Create AgentMessage model (AC: 4)
  - [ ] Define messageId (Guid)
  - [ ] Define timestamp (DateTime)
  - [ ] Define sourceAgent (string)
  - [ ] Define targetAgent (string)
  - [ ] Define messageType (enum: Request, Response, Error)
  - [ ] Define content (object/JsonDocument)
  - [ ] Define workflowInstanceId (Guid)
  - [ ] Add validation attributes
- [ ] Task 2: Create AgentRequest model (AC: 2)
  - [ ] Define sourceAgentId (string)
  - [ ] Define requestType (string)
  - [ ] Define payload (JsonDocument)
  - [ ] Define workflowContext (WorkflowContext)
  - [ ] Define conversationHistory (List<AgentMessage>)
  - [ ] Add validation
- [ ] Task 3: Create AgentResponse model (AC: 3)
  - [ ] Define success (bool)
  - [ ] Define content (JsonDocument)
  - [ ] Define metadata (Dictionary<string, string>)
  - [ ] Define errorMessage (string?)
- [ ] Task 4: Create IAgentMessaging interface (AC: 1, 5)
  - [ ] Define RequestFromAgent method signature
  - [ ] Define async Task<AgentResponse> return type
  - [ ] Define timeout and retry parameters
  - [ ] Add CancellationToken support
- [ ] Task 5: Implement AgentMessaging service (AC: 1-5)
  - [ ] Implement RequestFromAgent with AgentRouter integration
  - [ ] Implement timeout logic (30 seconds)
  - [ ] Implement retry logic (1 retry)
  - [ ] Implement message logging
  - [ ] Create error handling for missing agents
  - [ ] Add correlation IDs for traceability
- [ ] Task 6: Integrate with AgentRegistry (from Story 5.1)
  - [ ] Validate target agent exists before sending
  - [ ] Retrieve agent capabilities
  - [ ] Use agent metadata in request context
- [ ] Task 7: Create message logging repository (AC: 3)
  - [ ] Create AgentMessageLog entity
  - [ ] Define DbSet in ApplicationDbContext
  - [ ] Create EF Core migration
  - [ ] Implement save message method
  - [ ] Add indexes for querying (workflowInstanceId, timestamp)
- [ ] Task 8: Write unit tests
  - [ ] Test AgentMessaging.RequestFromAgent success path
  - [ ] Test timeout behavior (> 30 seconds)
  - [ ] Test retry logic
  - [ ] Test error handling (agent not found)
  - [ ] Test message format validation
  - [ ] Test logging
- [ ] Task 9: Write integration tests
  - [ ] Test end-to-end agent-to-agent communication
  - [ ] Test with real AgentRouter and AgentRegistry
  - [ ] Test message persistence to database
  - [ ] Test concurrent requests
  - [ ] Test timeout with real async operations
- [ ] Task 10: Update dependency injection
  - [ ] Register IAgentMessaging service
  - [ ] Configure timeout settings
  - [ ] Configure retry policy
- [ ] Task 11: Add monitoring and observability
  - [ ] Add logging for agent requests
  - [ ] Add metrics for message count
  - [ ] Add metrics for timeout/retry rates
  - [ ] Add distributed tracing support

## Dev Notes

### Epic 5 Context

This is the SECOND story in Epic 5 (Multi-Agent Collaboration). Epic 5 enables seamless collaboration between BMAD agents, allowing them to share context, hand off work, and coordinate on complex tasks.

**Epic 5 Goal:** Enable intelligent agent collaboration with transparency for users.

**Epic 5 Stories:**
- 5.1 (COMPLETED): Agent Registry & Configuration
- **5.2 (THIS STORY):** Agent-to-Agent Messaging
- 5.3: Shared Workflow Context
- 5.4: Agent Handoff & Attribution
- 5.5: Human Approval for Low-Confidence Decisions

### Story Dependencies

**Depends On:**
- **Story 5.1 (Agent Registry):** This story uses AgentRegistry to discover and validate target agents before sending messages

**Enables:**
- **Story 5.3 (Shared Context):** Agent messaging is the transport mechanism for context sharing
- **Story 5.4 (Handoff):** Message logs provide handoff attribution and transparency
- **Story 5.5 (Human Approval):** Messaging infrastructure supports approval request workflows

### Architecture Context

#### Existing Agent Infrastructure

From Epic 4 and Story 5.1, we have:
- **IAgentHandler** interface: Defines agent execution contract
- **AgentContext** class: Provides step context to agents
- **AgentResult** class: Returns agent execution results
- **AgentRouter** class: Routes steps to registered agent handlers
- **AgentRegistry** (Story 5.1): Metadata about all BMAD agents
- **AgentDefinition** (Story 5.1): Agent capabilities and configuration

**Location:** `src/bmadServer.ApiService/Services/Workflows/Agents/`

#### Current AgentRouter Implementation

From `AgentRouter.cs` (Epic 4):
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

**Key Insight:** AgentRouter provides the low-level routing mechanism. This story wraps it with higher-level messaging semantics (request/response, timeout, retry, logging).

#### Current AgentContext Model

From `IAgentHandler.cs`:
```csharp
public class AgentContext
{
    public required Guid WorkflowInstanceId { get; init; }
    public required Guid StepId { get; init; }
    public required string StepName { get; init; }
    public required Dictionary<string, object> StepInput { get; init; }
    public Dictionary<string, object> WorkflowState { get; init; } = new();
    public List<string> ConversationHistory { get; init; } = new();
}
```

**Important:** This story extends this context model for agent-to-agent communication.

### What This Story Adds

This story creates a **messaging layer** on top of AgentRouter:

1. **AgentMessage Model:** Standardized message format for all agent-to-agent communication
2. **AgentRequest/Response Models:** Typed request/response contracts
3. **AgentMessaging Service:** High-level API for requesting information from other agents
4. **Timeout & Retry Logic:** Resilient communication with 30-second timeout and 1 retry
5. **Message Logging:** Persistent audit trail of all agent-to-agent interactions
6. **Integration with AgentRegistry:** Validates target agents exist and have required capabilities

### Implementation Guidance

#### File Structure

Create these files in `src/bmadServer.ApiService/Services/Workflows/Agents/`:
```
Agents/
├── IAgentHandler.cs (existing)
├── MockAgentHandler.cs (existing)
├── AgentDefinition.cs (existing - Story 5.1)
├── IAgentRegistry.cs (existing - Story 5.1)
├── AgentRegistry.cs (existing - Story 5.1)
├── AgentMessage.cs (NEW)
├── AgentRequest.cs (NEW)
├── AgentResponse.cs (NEW)
├── IAgentMessaging.cs (NEW)
└── AgentMessaging.cs (NEW)
```

Create database entities in `src/bmadServer.ApiService/Data/Entities/`:
```
Entities/
└── AgentMessageLog.cs (NEW)
```

#### AgentMessage Model Design

```csharp
public class AgentMessage
{
    public required Guid MessageId { get; init; }
    public required DateTime Timestamp { get; init; }
    public required string SourceAgent { get; init; }
    public required string TargetAgent { get; init; }
    public required MessageType MessageType { get; init; }
    public required JsonDocument Content { get; init; }
    public required Guid WorkflowInstanceId { get; init; }
    public string? CorrelationId { get; init; }  // For request/response tracking
}

public enum MessageType
{
    Request,
    Response,
    Error
}
```

#### AgentRequest Model Design

```csharp
public class AgentRequest
{
    public required string SourceAgentId { get; init; }
    public required string RequestType { get; init; }  // e.g., "get-architecture-input", "validate-requirements"
    public required JsonDocument Payload { get; init; }
    public required WorkflowContext WorkflowContext { get; init; }
    public List<AgentMessage> ConversationHistory { get; init; } = new();
}

// Extended context for agent-to-agent communication
public class WorkflowContext
{
    public required Guid WorkflowInstanceId { get; init; }
    public required Guid CurrentStepId { get; init; }
    public required string CurrentStepName { get; init; }
    public Dictionary<string, object> StepOutputs { get; init; } = new();
    public Dictionary<string, object> WorkflowState { get; init; } = new();
}
```

#### AgentResponse Model Design

```csharp
public class AgentResponse
{
    public required bool Success { get; init; }
    public JsonDocument? Content { get; init; }
    public Dictionary<string, string> Metadata { get; init; } = new();
    public string? ErrorMessage { get; init; }
    public bool IsRetryable { get; init; }
}
```

#### IAgentMessaging Interface

```csharp
public interface IAgentMessaging
{
    Task<AgentResponse> RequestFromAgentAsync(
        string targetAgentId,
        string requestType,
        object payload,
        WorkflowContext context,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default);
    
    Task<List<AgentMessage>> GetConversationHistoryAsync(
        Guid workflowInstanceId,
        CancellationToken cancellationToken = default);
}
```

#### AgentMessaging Implementation Strategy

Key implementation details:

1. **Timeout Implementation:**
   - Default timeout: 30 seconds
   - Use `Task.WhenAny()` with `Task.Delay()` for timeout detection
   - Cancel underlying agent execution on timeout

2. **Retry Logic:**
   - If timeout occurs, retry once immediately
   - If second timeout, return error response
   - Log both timeout events

3. **Message Logging:**
   - Log request message immediately (before sending)
   - Log response/error message after receiving
   - Use same CorrelationId for request/response pair
   - Store in database asynchronously (fire-and-forget)

4. **AgentRegistry Integration:**
   - Before sending request, call `AgentRegistry.GetAgent(targetAgentId)`
   - If agent not found, return error immediately (don't call AgentRouter)
   - Include agent capabilities in request context

5. **AgentRouter Integration:**
   - Convert AgentRequest → AgentContext for AgentRouter
   - Call `AgentRouter.RouteToAgentAsync()`
   - Convert AgentResult → AgentResponse
   - Preserve correlation and tracing information

#### Pseudocode for Core Implementation

```csharp
public async Task<AgentResponse> RequestFromAgentAsync(
    string targetAgentId,
    string requestType,
    object payload,
    WorkflowContext context,
    TimeSpan? timeout = null,
    CancellationToken cancellationToken = default)
{
    var timeoutDuration = timeout ?? TimeSpan.FromSeconds(30);
    var correlationId = Guid.NewGuid().ToString();
    
    // 1. Validate target agent exists
    var targetAgent = await _agentRegistry.GetAgent(targetAgentId);
    if (targetAgent == null)
    {
        return new AgentResponse
        {
            Success = false,
            ErrorMessage = $"Target agent not found: {targetAgentId}",
            IsRetryable = false
        };
    }
    
    // 2. Create request message
    var requestMessage = new AgentMessage
    {
        MessageId = Guid.NewGuid(),
        Timestamp = DateTime.UtcNow,
        SourceAgent = context.CurrentStepName,  // Or derive from context
        TargetAgent = targetAgentId,
        MessageType = MessageType.Request,
        Content = JsonSerializer.SerializeToDocument(payload),
        WorkflowInstanceId = context.WorkflowInstanceId,
        CorrelationId = correlationId
    };
    
    // 3. Log request
    await _messageLogger.LogMessageAsync(requestMessage, cancellationToken);
    
    // 4. Send request with timeout and retry
    var attempt = 0;
    AgentResponse? response = null;
    
    while (attempt < 2 && response == null)
    {
        attempt++;
        
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeoutDuration);
            
            // Convert to AgentContext and call AgentRouter
            var agentContext = ConvertToAgentContext(requestType, payload, context);
            var agentResult = await _agentRouter.RouteToAgentAsync(
                targetAgentId,
                agentContext,
                cts.Token);
            
            response = ConvertToAgentResponse(agentResult);
        }
        catch (OperationCanceledException) when (attempt == 1)
        {
            _logger.LogWarning(
                "Agent request timeout (attempt {Attempt}), retrying: {TargetAgent}, {CorrelationId}",
                attempt, targetAgentId, correlationId);
            // Retry on first timeout
            continue;
        }
        catch (OperationCanceledException)
        {
            _logger.LogError(
                "Agent request timeout after retry: {TargetAgent}, {CorrelationId}",
                targetAgentId, correlationId);
            
            response = new AgentResponse
            {
                Success = false,
                ErrorMessage = $"Agent request timed out after {timeoutDuration.TotalSeconds}s and 1 retry",
                IsRetryable = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Agent request failed: {TargetAgent}, {CorrelationId}",
                targetAgentId, correlationId);
            
            response = new AgentResponse
            {
                Success = false,
                ErrorMessage = ex.Message,
                IsRetryable = false
            };
        }
    }
    
    // 5. Log response
    var responseMessage = new AgentMessage
    {
        MessageId = Guid.NewGuid(),
        Timestamp = DateTime.UtcNow,
        SourceAgent = targetAgentId,
        TargetAgent = requestMessage.SourceAgent,
        MessageType = response!.Success ? MessageType.Response : MessageType.Error,
        Content = JsonSerializer.SerializeToDocument(response),
        WorkflowInstanceId = context.WorkflowInstanceId,
        CorrelationId = correlationId
    };
    
    await _messageLogger.LogMessageAsync(responseMessage, cancellationToken);
    
    return response;
}
```

#### Database Schema for Message Logging

Create `AgentMessageLog` entity:

```csharp
public class AgentMessageLog
{
    public Guid Id { get; set; }
    public Guid MessageId { get; set; }
    public DateTime Timestamp { get; set; }
    public string SourceAgent { get; set; } = string.Empty;
    public string TargetAgent { get; set; } = string.Empty;
    public MessageType MessageType { get; set; }
    public JsonDocument Content { get; set; } = JsonDocument.Parse("{}");
    public Guid WorkflowInstanceId { get; set; }
    public string? CorrelationId { get; set; }
    
    // Navigation
    public WorkflowInstance? WorkflowInstance { get; set; }
}
```

Add to `ApplicationDbContext`:

```csharp
public DbSet<AgentMessageLog> AgentMessageLogs { get; set; }

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // ... existing config
    
    modelBuilder.Entity<AgentMessageLog>(entity =>
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => e.WorkflowInstanceId);
        entity.HasIndex(e => e.Timestamp);
        entity.HasIndex(e => e.CorrelationId);
        
        entity.Property(e => e.Content)
            .HasColumnType("jsonb");
        
        entity.HasOne(e => e.WorkflowInstance)
            .WithMany()
            .HasForeignKey(e => e.WorkflowInstanceId);
    });
}
```

Create EF Core migration:
```bash
dotnet ef migrations add AddAgentMessageLog --project src/bmadServer.ApiService
```

### Previous Story Learnings (Story 5.1)

From Story 5.1 implementation (Agent Registry & Configuration):

1. **Agent Metadata is Available**
   - AgentRegistry provides GetAgent(id) to retrieve agent definitions
   - AgentDefinition includes: AgentId, Name, Description, Capabilities, SystemPrompt, ModelPreference
   - Use this metadata to enrich messaging context

2. **Integration with AgentRouter**
   - AgentRouter is the low-level execution mechanism
   - Don't replace AgentRouter - build on top of it
   - AgentMessaging service should delegate to AgentRouter for actual execution

3. **Capability-Based Routing**
   - Story 5.1 defined capability strings (e.g., "create-prd", "create-architecture")
   - This story should validate target agent has required capability (optional enhancement)
   - Use requestType parameter to specify required capability

4. **Model Preference Handling**
   - Story 5.1 defined model preferences per agent
   - Future: AgentMessaging could pass ModelPreference to LLM provider
   - MVP: Focus on messaging infrastructure, model routing in Phase 2

### Git Intelligence (Recent Work Patterns)

Recent commits show:
- Focus on fixing integration tests and package references
- Strong emphasis on test coverage
- Use of Aspire patterns and dependency injection

**Actionable Insights:**
- Follow same testing patterns: Unit tests + Integration tests
- Register new services in Program.cs using Aspire DI patterns
- Ensure all new packages are added to correct project files
- Test message logging with in-memory or test database

### Testing Strategy

#### Unit Tests

Location: `src/bmadServer.Tests/Unit/Services/Workflows/Agents/`

Create: `AgentMessagingTests.cs`

Test scenarios:
1. **RequestFromAgentAsync - Success Path**
   - Mock AgentRegistry returns valid agent
   - Mock AgentRouter returns success result
   - Verify AgentResponse.Success = true
   - Verify message logging called twice (request + response)

2. **RequestFromAgentAsync - Agent Not Found**
   - Mock AgentRegistry returns null
   - Verify error response returned immediately
   - Verify AgentRouter NOT called

3. **RequestFromAgentAsync - Timeout First Attempt**
   - Mock AgentRouter delays > 30 seconds on first call
   - Mock AgentRouter succeeds on second call
   - Verify retry occurs
   - Verify eventual success

4. **RequestFromAgentAsync - Timeout Both Attempts**
   - Mock AgentRouter delays > 30 seconds on both calls
   - Verify error response with timeout message
   - Verify 2 attempts logged

5. **RequestFromAgentAsync - Exception Handling**
   - Mock AgentRouter throws exception
   - Verify error response with exception message
   - Verify no retry (non-timeout exception)

6. **Message Format Validation**
   - Create AgentMessage with all fields
   - Verify MessageId is Guid
   - Verify Timestamp is UTC
   - Verify MessageType enum values
   - Verify CorrelationId links request/response

7. **GetConversationHistoryAsync**
   - Mock message repository returns messages for workflow
   - Verify messages ordered by timestamp
   - Verify only messages for specified workflow returned

#### Integration Tests

Location: `src/bmadServer.Tests/Integration/Workflows/`

Create: `AgentMessagingIntegrationTests.cs`

Test scenarios:
1. **End-to-End Agent-to-Agent Communication**
   - Create real workflow instance
   - Create real agent handlers (mocked LLM responses)
   - Send agent request
   - Verify response received
   - Verify messages persisted to database
   - Verify correlation ID links request/response

2. **Message Persistence**
   - Send agent request
   - Query AgentMessageLogs table
   - Verify both request and response messages saved
   - Verify JSONB content is queryable

3. **Concurrent Requests**
   - Send multiple agent requests in parallel
   - Verify all succeed
   - Verify message logs don't conflict
   - Verify correlation IDs are unique

4. **Timeout with Real Async Operations**
   - Create agent handler that delays 35 seconds
   - Set timeout to 30 seconds
   - Verify timeout occurs
   - Verify retry happens
   - Verify error response after both timeouts

5. **Integration with AgentRegistry**
   - Request from registered agent
   - Verify success
   - Request from unregistered agent
   - Verify immediate error

### Dependencies

#### Required Packages

- **System.Text.Json** (already in project): For JsonDocument serialization
- **Microsoft.EntityFrameworkCore** (already in project): For AgentMessageLog persistence
- **Npgsql.EntityFrameworkCore.PostgreSQL** (already in project): For PostgreSQL JSONB support
- No new packages required

#### Dependency Injection

Register services in `Program.cs`:

```csharp
// Add agent messaging (depends on AgentRegistry and AgentRouter)
builder.Services.AddScoped<IAgentMessaging, AgentMessaging>();
```

**Important:** AgentMessaging depends on:
- `IAgentRegistry` (from Story 5.1)
- `IAgentRouter` (from Epic 4)
- `ApplicationDbContext` (for message logging)
- `ILogger<AgentMessaging>`

#### Database Migrations

Create migration:
```bash
cd src/bmadServer.ApiService
dotnet ef migrations add AddAgentMessageLog
```

Apply migration (development):
```bash
dotnet ef database update
```

**Testing:** Integration tests should use in-memory SQLite or test PostgreSQL instance.

### Project Structure Notes

#### Alignment with Aspire Architecture

- Follows Aspire service registration patterns
- Uses async/await for all I/O operations
- Uses CancellationToken for graceful cancellation
- Compatible with distributed tracing (Activity IDs)
- Uses structured logging with ILogger

#### Code Organization

- Keep messaging models in `Agents/` subfolder (same location as AgentDefinition)
- Separate interface from implementation (`IAgentMessaging` → `AgentMessaging`)
- Keep database entities in `Data/Entities/` folder
- Follow naming patterns: `AgentMessage`, `AgentRequest`, `AgentResponse`

#### PostgreSQL JSONB Optimization

- Use `jsonb` column type for AgentMessage.Content (not `json`)
- JSONB supports efficient querying and indexing
- Example queries:
  ```sql
  -- Find all requests for specific target agent
  SELECT * FROM "AgentMessageLogs"
  WHERE "TargetAgent" = 'architect'
  AND "MessageType" = 0  -- Request
  
  -- Find all timeouts
  SELECT * FROM "AgentMessageLogs"
  WHERE "Content" @> '{"ErrorMessage": "timeout"}'::jsonb
  ```

### Security Considerations

- **Message Content:** AgentMessages may contain sensitive workflow data
  - Store in JSONB for encryption-at-rest (Phase 2)
  - Implement message retention policies (Phase 2)
  
- **Agent Validation:** Always validate target agent exists before sending
  - Prevents message injection attacks
  - Prevents routing to unauthorized agents

- **Timeout Enforcement:** Prevents denial-of-service from hanging agents
  - 30-second timeout is reasonable for agent processing
  - Configurable via appsettings.json

- **Correlation IDs:** Enable audit trail of agent conversations
  - Track which agent requested what from whom
  - Support forensic analysis of workflow decisions

### Performance Considerations

- **Message Logging:** Use fire-and-forget pattern
  - Don't wait for database write before returning response
  - Use background queue (IHostedService) for message persistence (enhancement)
  
- **Timeout Implementation:** Use efficient async cancellation
  - Use `CancellationTokenSource.CancelAfter()` instead of `Task.Delay().Wait()`
  - Cancel underlying agent execution to free resources

- **Query Optimization:** Index message logs appropriately
  - Index on WorkflowInstanceId (most common query)
  - Index on Timestamp (for time-range queries)
  - Index on CorrelationId (for request/response lookup)

- **Memory Management:** JsonDocument is disposable
  - Properly dispose JsonDocument after use
  - Consider using `JsonSerializer.Deserialize<T>()` for typed access

### Error Handling

- **Agent Not Found:** Return error immediately (don't throw exception)
- **Timeout:** Retry once, then return error (log both attempts)
- **AgentRouter Error:** Convert to AgentResponse with error message
- **Database Error (logging):** Log error but don't fail request
  - Message logging is non-critical (best-effort)
  - Consider retry queue for failed message logs

### Observability and Monitoring

#### Logging

Use structured logging with correlation IDs:

```csharp
_logger.LogInformation(
    "Agent request sent: {SourceAgent} -> {TargetAgent}, {RequestType}, {CorrelationId}, {WorkflowInstanceId}",
    sourceAgent, targetAgentId, requestType, correlationId, context.WorkflowInstanceId);

_logger.LogWarning(
    "Agent request timeout (attempt {Attempt}): {TargetAgent}, {CorrelationId}",
    attempt, targetAgentId, correlationId);

_logger.LogError(exception,
    "Agent request failed: {TargetAgent}, {CorrelationId}, {ErrorMessage}",
    targetAgentId, correlationId, exception.Message);
```

#### Metrics

Consider adding metrics (Phase 2):
- Total agent requests per agent pair
- Request success/failure rate
- Average response time
- Timeout rate
- Retry rate

#### Distributed Tracing

- AgentMessaging should participate in distributed tracing
- Use `Activity.Current` to create spans for agent requests
- Include correlation ID in trace metadata

### Configuration

Add configuration to `appsettings.json`:

```json
{
  "AgentMessaging": {
    "DefaultTimeoutSeconds": 30,
    "EnableRetry": true,
    "MaxRetries": 1,
    "EnableMessageLogging": true,
    "MessageLogRetentionDays": 30
  }
}
```

Load configuration in `AgentMessaging` constructor:

```csharp
public class AgentMessaging : IAgentMessaging
{
    private readonly TimeSpan _defaultTimeout;
    private readonly bool _enableRetry;
    private readonly int _maxRetries;
    
    public AgentMessaging(
        IOptions<AgentMessagingOptions> options,
        IAgentRegistry agentRegistry,
        IAgentRouter agentRouter,
        ApplicationDbContext dbContext,
        ILogger<AgentMessaging> logger)
    {
        _defaultTimeout = TimeSpan.FromSeconds(options.Value.DefaultTimeoutSeconds);
        _enableRetry = options.Value.EnableRetry;
        _maxRetries = options.Value.MaxRetries;
        // ...
    }
}
```

### Documentation Requirements

- XML comments on all public interfaces, classes, and methods
- Document message format and correlation ID usage
- Document timeout and retry behavior
- Document error response format
- Add README.md section in Agents/ folder about messaging

### Future Enhancements (Phase 2)

- **Asynchronous Message Delivery:** Use message queue (Azure Service Bus, RabbitMQ) instead of synchronous calls
- **Message Priority:** Support urgent requests that skip queue
- **Broadcast Messages:** Send message to multiple agents simultaneously
- **Message Subscriptions:** Agents subscribe to topics instead of direct addressing
- **Message Replay:** Replay conversations for debugging
- **Message Encryption:** Encrypt sensitive message content at rest
- **Agent Rate Limiting:** Prevent agent spam with rate limits per agent
- **Message Compression:** Compress large message payloads

### References

- **Source:** [epics.md - Story 5.2](../planning-artifacts/epics.md#story-52-agent-to-agent-messaging)
- **Architecture:** [architecture.md - Agent Architecture](../planning-artifacts/architecture.md#agent-architecture)
- **Story 5.1:** [5-1-agent-registry-configuration.md](5-1-agent-registry-configuration.md) (AgentRegistry dependency)
- **IAgentHandler:** [src/bmadServer.ApiService/Services/Workflows/Agents/IAgentHandler.cs](../../src/bmadServer.ApiService/Services/Workflows/Agents/IAgentHandler.cs)
- **AgentRouter:** [src/bmadServer.ApiService/Services/Workflows/AgentRouter.cs](../../src/bmadServer.ApiService/Services/Workflows/AgentRouter.cs)
- **Epic 4 Retrospective:** [epic-4-retrospective.md](epic-4-retrospective.md#agent-infrastructure)

---

## Aspire Development Standards

### PostgreSQL Connection Pattern

This story requires database access for message logging:

```csharp
public class AgentMessaging : IAgentMessaging
{
    private readonly ApplicationDbContext _dbContext;
    
    public AgentMessaging(
        ApplicationDbContext dbContext,
        // ... other dependencies
    )
    {
        _dbContext = dbContext;
    }
    
    private async Task LogMessageAsync(AgentMessage message, CancellationToken cancellationToken)
    {
        var log = new AgentMessageLog
        {
            Id = Guid.NewGuid(),
            MessageId = message.MessageId,
            Timestamp = message.Timestamp,
            SourceAgent = message.SourceAgent,
            TargetAgent = message.TargetAgent,
            MessageType = message.MessageType,
            Content = message.Content,
            WorkflowInstanceId = message.WorkflowInstanceId,
            CorrelationId = message.CorrelationId
        };
        
        _dbContext.AgentMessageLogs.Add(log);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
```

### Project-Wide Standards

This story follows the Aspire-first development pattern:
- **Reference:** [PROJECT-WIDE-RULES.md](../../../PROJECT-WIDE-RULES.md)
- **Primary Documentation:** https://aspire.dev
- **GitHub:** https://github.com/microsoft/aspire
- **PostgreSQL with EF Core:** Use `Npgsql.EntityFrameworkCore.PostgreSQL` package
- **JSONB Storage:** Use `jsonb` column type for flexible JSON storage
- **Async/Await:** All database operations must be async
- **Dependency Injection:** Register services in Program.cs using Aspire patterns

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
- `src/bmadServer.ApiService/Services/Workflows/Agents/AgentMessage.cs`
- `src/bmadServer.ApiService/Services/Workflows/Agents/AgentRequest.cs`
- `src/bmadServer.ApiService/Services/Workflows/Agents/AgentResponse.cs`
- `src/bmadServer.ApiService/Services/Workflows/Agents/IAgentMessaging.cs`
- `src/bmadServer.ApiService/Services/Workflows/Agents/AgentMessaging.cs`
- `src/bmadServer.ApiService/Data/Entities/AgentMessageLog.cs`
- `src/bmadServer.ApiService/Data/ApplicationDbContext.cs` (updated)
- `src/bmadServer.ApiService/Program.cs` (updated - DI registration)
- `src/bmadServer.ApiService/Migrations/{timestamp}_AddAgentMessageLog.cs`
- `src/bmadServer.Tests/Unit/Services/Workflows/Agents/AgentMessagingTests.cs`
- `src/bmadServer.Tests/Integration/Workflows/AgentMessagingIntegrationTests.cs`
- `src/bmadServer.ApiService/appsettings.json` (updated - configuration)
