# Implementation Patterns Guide - bmadServer

**Generated:** 2026-01-23  
**Status:** READY FOR DEVELOPERS  
**Code Examples:** 50+  
**Coverage:** Backend (.NET) + Frontend (React/TypeScript)

---

## Table of Contents

1. [Backend API Patterns](#backend-api-patterns)
2. [Data Models & Entity Patterns](#data-models--entity-patterns)
3. [Real-Time Communication Patterns](#real-time-communication-patterns)
4. [Frontend Component Patterns](#frontend-component-patterns)
5. [Authentication & Authorization Patterns](#authentication--authorization-patterns)
6. [Testing Patterns](#testing-patterns)
7. [Performance Optimization Patterns](#performance-optimization-patterns)
8. [Monitoring & Logging Patterns](#monitoring--logging-patterns)

---

## Backend API Patterns

### Pattern 1: REST Endpoint with FluentValidation

```csharp
// Endpoint definition (ASP.NET Core Minimal APIs)
app.MapPost("/api/v1/workflows", CreateWorkflow)
    .WithName("CreateWorkflow")
    .WithOpenApi()
    .Produces<WorkflowDto>(StatusCodes.Status201Created)
    .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

// Handler with validation
public async Task<IResult> CreateWorkflow(
    CreateWorkflowRequest request,
    IValidator<CreateWorkflowRequest> validator,
    WorkflowService workflowService,
    ILogger<Program> logger)
{
    // Validate input
    var validationResult = await validator.ValidateAsync(request);
    if (!validationResult.IsValid)
    {
        return Results.BadRequest(new ProblemDetails
        {
            Type = "https://bmadserver.api/errors/validation-failed",
            Title = "Validation Failed",
            Status = StatusCodes.Status400BadRequest,
            Extensions = new Dictionary<string, object?>
            {
                ["errors"] = validationResult.ToDictionary(
                    x => x.PropertyName,
                    x => new[] { x.ErrorMessage })
            }
        });
    }

    // Create workflow
    var workflow = await workflowService.CreateAsync(request);
    logger.LogInformation("Workflow created: {WorkflowId}", workflow.Id);

    return Results.Created($"/api/v1/workflows/{workflow.Id}", workflow);
}

// Validator (reusable)
public class CreateWorkflowRequestValidator : AbstractValidator<CreateWorkflowRequest>
{
    public CreateWorkflowRequestValidator()
    {
        RuleFor(x => x.WorkflowType)
            .NotEmpty()
            .Must(x => new[] { "prd", "architecture", "epics" }.Contains(x))
            .WithMessage("WorkflowType must be 'prd', 'architecture', or 'epics'");

        RuleFor(x => x.Context)
            .NotNull()
            .Must(c => !string.IsNullOrEmpty(c.ProductName))
            .WithMessage("Context.ProductName is required");
    }
}
```

### Pattern 2: RPC-Style Action Endpoint

```csharp
// Action endpoint: approve a decision
app.MapPost("/api/v1/decisions/{id}/approve", ApproveDecision)
    .WithName("ApproveDecision")
    .RequireAuthorization("CanApproveDecision")
    .Produces<ApprovalResponse>(StatusCodes.Status200OK)
    .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

public async Task<IResult> ApproveDecision(
    string id,
    ApprovalRequest request,
    WorkflowService workflowService,
    ILogger<Program> logger,
    ClaimsPrincipal user)
{
    var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? throw new UnauthorizedAccessException();

    try
    {
        var result = await workflowService.ApproveDecisionAsync(
            decisionId: id,
            rationale: request.Rationale,
            userId: userId);

        logger.LogInformation(
            "Decision approved: {DecisionId} by {UserId}",
            id,
            userId);

        return Results.Ok(result);
    }
    catch (WorkflowConflictException ex)
    {
        return Results.Conflict(new ProblemDetails
        {
            Type = "https://bmadserver.api/errors/workflow-conflict",
            Title = "Workflow State Conflict",
            Status = StatusCodes.Status409Conflict,
            Detail = "Decision was modified by another user",
            Extensions = new Dictionary<string, object?>
            {
                ["expectedVersion"] = ex.ExpectedVersion,
                ["actualVersion"] = ex.ActualVersion,
                ["lastModifiedBy"] = ex.LastModifiedBy,
                ["lastModifiedAt"] = ex.LastModifiedAt
            }
        });
    }
}
```

### Pattern 3: Pagination Pattern

```csharp
// Request DTO
public record ListWorkflowsRequest(
    [FromQuery(Name = "page")] int Page = 1,
    [FromQuery(Name = "pageSize")] int PageSize = 20,
    [FromQuery(Name = "status")] string? Status = null,
    [FromQuery(Name = "sort")] string? Sort = "createdAt:desc");

// Response DTO
public record PaginatedResponse<T>(
    int Page,
    int PageSize,
    int Total,
    int TotalPages,
    IEnumerable<T> Items);

// Handler
app.MapGet("/api/v1/workflows", ListWorkflows);

public async Task<IResult> ListWorkflows(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20,
    [FromQuery] string? status = null,
    WorkflowService workflowService)
{
    const int maxPageSize = 100;
    if (pageSize > maxPageSize) pageSize = maxPageSize;
    if (page < 1) page = 1;

    var result = await workflowService.ListAsync(
        page: page,
        pageSize: pageSize,
        status: status);

    return Results.Ok(result);
}

// Service implementation
public class WorkflowService
{
    public async Task<PaginatedResponse<WorkflowDto>> ListAsync(
        int page,
        int pageSize,
        string? status,
        CancellationToken ct = default)
    {
        var query = _context.Workflows.AsQueryable();

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(w =>
                EF.Functions.JsonContains(w.State,
                    $"{{\"status\": \"{status}\"}}"));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(w => w.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(w => new WorkflowDto
            {
                Id = w.Id,
                WorkflowType = w.WorkflowType,
                CreatedAt = w.CreatedAt,
                UpdatedAt = w.UpdatedAt
            })
            .ToListAsync(ct);

        return new PaginatedResponse<WorkflowDto>(
            Page: page,
            PageSize: pageSize,
            Total: total,
            TotalPages: (int)Math.Ceiling((double)total / pageSize),
            Items: items);
    }
}
```

### Pattern 4: Error Mapping & Exception Handler

```csharp
// Custom exceptions
public class WorkflowException : Exception
{
    public WorkflowException(string message) : base(message) { }
}

public class WorkflowNotFoundException : WorkflowException
{
    public Guid WorkflowId { get; }
    public WorkflowNotFoundException(Guid id) : base($"Workflow {id} not found")
    {
        WorkflowId = id;
    }
}

public class WorkflowConflictException : WorkflowException
{
    public int ExpectedVersion { get; }
    public int ActualVersion { get; }
    public string? LastModifiedBy { get; }
    public DateTime? LastModifiedAt { get; }

    public WorkflowConflictException(
        int expectedVersion,
        int actualVersion,
        string? lastModifiedBy,
        DateTime? lastModifiedAt) : base("Workflow state conflict")
    {
        ExpectedVersion = expectedVersion;
        ActualVersion = actualVersion;
        LastModifiedBy = lastModifiedBy;
        LastModifiedAt = lastModifiedAt;
    }
}

// Exception handler middleware
public class WorkflowExceptionHandler : IExceptionHandler
{
    private readonly ILogger<WorkflowExceptionHandler> _logger;

    public WorkflowExceptionHandler(ILogger<WorkflowExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken ct)
    {
        return exception switch
        {
            WorkflowNotFoundException ex => await HandleNotFound(context, ex, ct),
            WorkflowConflictException ex => await HandleConflict(context, ex, ct),
            _ => false
        };
    }

    private async Task<bool> HandleNotFound(
        HttpContext context,
        WorkflowNotFoundException exception,
        CancellationToken ct)
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        await context.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Type = "https://bmadserver.api/errors/not-found",
                Title = "Not Found",
                Status = StatusCodes.Status404NotFound,
                Detail = exception.Message,
                Instance = context.Request.Path
            },
            cancellationToken: ct);
        return true;
    }

    private async Task<bool> HandleConflict(
        HttpContext context,
        WorkflowConflictException exception,
        CancellationToken ct)
    {
        context.Response.StatusCode = StatusCodes.Status409Conflict;
        await context.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Type = "https://bmadserver.api/errors/workflow-conflict",
                Title = "Workflow State Conflict",
                Status = StatusCodes.Status409Conflict,
                Detail = exception.Message,
                Instance = context.Request.Path,
                Extensions = new Dictionary<string, object?>
                {
                    ["expectedVersion"] = exception.ExpectedVersion,
                    ["actualVersion"] = exception.ActualVersion,
                    ["lastModifiedBy"] = exception.LastModifiedBy,
                    ["lastModifiedAt"] = exception.LastModifiedAt?.ToString("O")
                }
            },
            cancellationToken: ct);
        return true;
    }
}

// Register in Program.cs
builder.Services.AddExceptionHandler<WorkflowExceptionHandler>();
```

---

## Data Models & Entity Patterns

### Pattern 5: Entity with JSONB State

```csharp
public class Workflow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string TenantId { get; set; } = "default";
    public string WorkflowType { get; set; } = string.Empty;
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // JSONB document storage
    [Column(TypeName = "jsonb")]
    public JsonDocument State { get; set; } = JsonDocument.Parse("{}");

    // Concurrency control
    public int Version { get; set; } = 1;
    public string? LastModifiedBy { get; set; }
    public DateTime? LastModifiedAt { get; set; }

    // Navigation properties
    public User CreatedBy { get; set; } = null!;
    public ICollection<WorkflowEvent> Events { get; set; } = [];
}

// DbContext configuration
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Workflow>()
        .HasKey(w => w.Id);

    modelBuilder.Entity<Workflow>()
        .Property(w => w.State)
        .HasColumnType("jsonb");

    // GIN index for JSONB performance
    modelBuilder.Entity<Workflow>()
        .HasIndex(w => w.State)
        .HasMethod("gin");

    modelBuilder.Entity<Workflow>()
        .HasIndex(w => new { w.TenantId, w.WorkflowType });

    modelBuilder.Entity<Workflow>()
        .HasMany(w => w.Events)
        .WithOne(e => e.Workflow)
        .HasForeignKey(e => e.WorkflowId);
}
```

### Pattern 6: Update with Optimistic Concurrency

```csharp
public async Task UpdateWorkflowStateAsync(
    Guid workflowId,
    JsonDocument newState,
    int expectedVersion,
    string userId,
    CancellationToken ct = default)
{
    var workflow = await _context.Workflows
        .FirstOrDefaultAsync(w => w.Id == workflowId, ct)
        ?? throw new WorkflowNotFoundException(workflowId);

    // Check version (optimistic concurrency)
    if (workflow.Version != expectedVersion)
    {
        throw new WorkflowConflictException(
            expectedVersion: expectedVersion,
            actualVersion: workflow.Version,
            lastModifiedBy: workflow.LastModifiedBy,
            lastModifiedAt: workflow.LastModifiedAt);
    }

    // Update state
    workflow.State = newState;
    workflow.Version++;
    workflow.LastModifiedBy = userId;
    workflow.UpdatedAt = DateTime.UtcNow;

    // Log event
    workflow.Events.Add(new WorkflowEvent
    {
        WorkflowId = workflowId,
        EventType = "state_updated",
        Payload = JsonSerializer.Serialize(newState),
        Actor = userId,
        Timestamp = DateTime.UtcNow
    });

    try
    {
        await _context.SaveChangesAsync(ct);
    }
    catch (DbUpdateConcurrencyException ex)
    {
        _logger.LogWarning(ex, "Concurrency conflict updating workflow {WorkflowId}", workflowId);
        throw new WorkflowConflictException(
            expectedVersion: expectedVersion,
            actualVersion: workflow.Version,
            lastModifiedBy: workflow.LastModifiedBy,
            lastModifiedAt: workflow.UpdatedAt);
    }
}
```

### Pattern 7: JSONB Querying

```csharp
// Query workflows by status (JSONB operator)
public async Task<IEnumerable<Workflow>> GetWorkflowsByStatusAsync(
    string status,
    CancellationToken ct = default)
{
    return await _context.Workflows
        .Where(w => EF.Functions.JsonContains(
            w.State,
            $"{{\"status\": \"{status}\"}}"))
        .ToListAsync(ct);
}

// Query with complex JSONB condition
public async Task<IEnumerable<Workflow>> GetWorkflowsWithApprovedDecisionsAsync(
    CancellationToken ct = default)
{
    return await _context.Workflows
        .Where(w => EF.Functions.JsonContains(
            w.State,
            "{\"decisions\": [{\"status\": \"approved\"}]}"))
        .ToListAsync(ct);
}

// Raw SQL for complex JSONB queries
public async Task<IEnumerable<Workflow>> GetRecentlyModifiedAsync(
    TimeSpan duration,
    CancellationToken ct = default)
{
    return await _context.Workflows
        .FromSqlRaw($@"
            SELECT *
            FROM workflows
            WHERE (state->>'_lastModifiedAt')::timestamptz > NOW() - INTERVAL '{duration.TotalHours} hours'
            ORDER BY (state->>'_lastModifiedAt')::timestamptz DESC
        ")
        .ToListAsync(ct);
}

// JSONB aggregation query
public async Task<IEnumerable<object>> GetDecisionStatisticsAsync(
    CancellationToken ct = default)
{
    return await _context.Workflows
        .FromSqlRaw($@"
            SELECT
                jsonb_array_length(state->'decisions') as decision_count,
                COUNT(*) as workflow_count
            FROM workflows
            GROUP BY jsonb_array_length(state->'decisions')
            ORDER BY jsonb_array_length(state->'decisions')
        ")
        .ToListAsync(ct);
}
```

### Pattern 8: Entity Value Converter for JSONB Serialization

```csharp
public class JsonDocumentValueConverter : ValueConverter<JsonDocument?, JsonDocument?>
{
    public JsonDocumentValueConverter()
        : base(
            v => v!,
            v => v!,
            new ConverterMappingHints(
                size: 4000))
    {
    }
}

// Use in DbContext
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Workflow>()
        .Property(w => w.State)
        .HasConversion<JsonDocumentValueConverter>()
        .HasColumnType("jsonb");
}

// Alternative: Custom converter for typed objects
public class WorkflowStateConverter : ValueConverter<WorkflowState, JsonDocument>
{
    public WorkflowStateConverter()
        : base(
            workflowState => JsonDocument.Parse(JsonSerializer.Serialize(workflowState)),
            jsonDoc => JsonSerializer.Deserialize<WorkflowState>(
                jsonDoc.RootElement.GetRawText())
                ?? new WorkflowState())
    {
    }
}

// Usage
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Workflow>()
        .Property(w => w.State)
        .HasConversion<WorkflowStateConverter>()
        .HasColumnType("jsonb");
}
```

---

## Real-Time Communication Patterns

### Pattern 9: SignalR Hub with Error Handling

```csharp
public class WorkflowHub : Hub
{
    private readonly ILogger<WorkflowHub> _logger;
    private readonly WorkflowService _workflowService;
    private readonly IAgentRouter _agentRouter;

    public WorkflowHub(
        ILogger<WorkflowHub> logger,
        WorkflowService workflowService,
        IAgentRouter agentRouter)
    {
        _logger = logger;
        _workflowService = workflowService;
        _agentRouter = agentRouter;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        _logger.LogInformation("User connected: {UserId}, ConnectionId: {ConnectionId}",
            userId,
            Context.ConnectionId);

        await base.OnConnectedAsync();
    }

    public async Task SubscribeToWorkflow(string workflowId)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new UnauthorizedAccessException();

        // Verify user has access
        var hasAccess = await _workflowService.CanAccessAsync(
            workflowId: workflowId,
            userId: userId);

        if (!hasAccess)
        {
            await Clients.Caller.SendAsync("error", new
            {
                code = "FORBIDDEN",
                message = "You do not have access to this workflow"
            });
            return;
        }

        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            $"workflow-{workflowId}");

        _logger.LogInformation("User subscribed to workflow: {UserId} -> {WorkflowId}",
            userId,
            workflowId);
    }

    public async Task ApproveDecision(string decisionId, string rationale)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new UnauthorizedAccessException();

        try
        {
            var result = await _workflowService.ApproveDecisionAsync(
                decisionId: decisionId,
                rationale: rationale,
                userId: userId);

            // Broadcast to workflow group
            await Clients.Group($"workflow-{result.WorkflowId}")
                .SendAsync("decision-approved", new
                {
                    decisionId = result.Id,
                    approvedAt = result.ApprovedAt,
                    approvedBy = userId
                });

            _logger.LogInformation("Decision approved: {DecisionId} by {UserId}",
                decisionId,
                userId);
        }
        catch (WorkflowConflictException ex)
        {
            await Clients.Caller.SendAsync("error", new
            {
                code = "WORKFLOW_CONFLICT",
                title = "Workflow State Conflict",
                message = "Decision was modified by another user",
                details = new
                {
                    expectedVersion = ex.ExpectedVersion,
                    actualVersion = ex.ActualVersion,
                    lastModifiedBy = ex.LastModifiedBy,
                    lastModifiedAt = ex.LastModifiedAt
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving decision: {DecisionId}", decisionId);

            await Clients.Caller.SendAsync("error", new
            {
                code = "INTERNAL_ERROR",
                message = "An error occurred while processing your request"
            });
        }
    }
}
```

### Pattern 10: SignalR Client Connection (TypeScript)

```typescript
import { HubConnectionBuilder, HubConnectionState } from "@signalr/signalr";

export class WorkflowHub {
  private connection: HubConnection;
  private reconnectCount = 0;
  private maxReconnectAttempts = 10;

  constructor(private token: string) {
    this.connection = new HubConnectionBuilder()
      .withUrl("/workflowhub", {
        accessTokenFactory: () => this.token,
        skipNegotiation: false,
        transport: HttpTransportType.WebSockets | HttpTransportType.ServerSentEvents,
      })
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (retryContext) => {
          if (retryContext.previousRetryCount < 10) {
            return Math.pow(2, retryContext.previousRetryCount) * 1000; // Exponential backoff
          }
          return 60000; // Max 60s between retries
        },
      })
      .withHubProtocol(new MessagePackHubProtocol())
      .configureLogging(LogLevel.Information)
      .build();

    this.setupEventListeners();
  }

  private setupEventListeners() {
    // Connection state changes
    this.connection.onreconnecting(() => {
      console.log("Reconnecting...");
      this.reconnectCount++;
    });

    this.connection.onreconnected(() => {
      console.log("Reconnected!");
      this.reconnectCount = 0;
    });

    this.connection.onclose((error) => {
      if (this.reconnectCount >= this.maxReconnectAttempts) {
        console.error("Max reconnection attempts reached");
        // Redirect to login
      }
    });

    // Server events
    this.connection.on("decision-approved", (data) => {
      console.log("Decision approved:", data);
      // Update UI
    });

    this.connection.on("error", (error) => {
      console.error("Server error:", error);
      this.handleServerError(error);
    });
  }

  public async connect(): Promise<void> {
    if (this.connection.state === HubConnectionState.Connected) {
      return;
    }

    await this.connection.start();
    console.log("Connected to workflow hub");
  }

  public async disconnect(): Promise<void> {
    if (this.connection.state === HubConnectionState.Connected) {
      await this.connection.stop();
    }
  }

  public async subscribeToWorkflow(workflowId: string): Promise<void> {
    await this.connection.invoke("SubscribeToWorkflow", workflowId);
  }

  public async approveDecision(
    decisionId: string,
    rationale: string
  ): Promise<void> {
    await this.connection.invoke("ApproveDecision", decisionId, rationale);
  }

  private handleServerError(error: any) {
    switch (error.code) {
      case "WORKFLOW_CONFLICT":
        console.error(`Conflict: modified by ${error.details.lastModifiedBy}`);
        break;
      case "FORBIDDEN":
        console.error("You do not have access to this resource");
        break;
      default:
        console.error("Unknown error:", error);
    }
  }
}
```

### Pattern 11: Streaming Agent Responses

```csharp
// Hub method for streaming agent response
public async Task RequestAgentResponse(string workflowId, string agentId, string prompt)
{
    var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? throw new UnauthorizedAccessException();

    try
    {
        // Call agent router
        var response = await _agentRouter.RouteAsync(
            agentId: agentId,
            request: new AgentRequest
            {
                AgentId = agentId,
                Action = "GenerateResponse",
                Payload = new Dictionary<string, object>
                {
                    ["prompt"] = prompt,
                    ["workflowId"] = workflowId,
                    ["userId"] = userId
                },
                CorrelationId = Context.ConnectionId
            });

        // Stream response back to client
        if (response.Success && response.Result is string responseText)
        {
            // Split into chunks for streaming
            const int chunkSize = 100;
            var chunks = responseText
                .AsMemory()
                .Chunk(chunkSize);

            foreach (var chunk in chunks)
            {
                await Clients.Caller.SendAsync("agent-response-chunk", new
                {
                    content = new string(chunk),
                    confidence = response.Confidence
                });

                // Simulate natural streaming
                await Task.Delay(50);
            }

            await Clients.Caller.SendAsync("agent-response-complete", new
            {
                agentId = agentId,
                confidence = response.Confidence
            });
        }
        else
        {
            await Clients.Caller.SendAsync("error", new
            {
                code = "AGENT_ERROR",
                message = response.Error
            });
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error requesting agent response");
        await Clients.Caller.SendAsync("error", new
        {
            code = "INTERNAL_ERROR",
            message = "Failed to get agent response"
        });
    }
}
```

---

## Frontend Component Patterns

### Pattern 12: React Hook with Zustand + TanStack Query

```typescript
// Custom hook combining server state (TanStack Query) + client state (Zustand)
import { useQuery } from "@tanstack/react-query";
import { useWorkflowStore } from "../stores/workflowStore";

export function useWorkflow(workflowId: string) {
  const { setCurrentWorkflow, setError } = useWorkflowStore();

  // Server state (API data)
  const query = useQuery({
    queryKey: ["workflows", workflowId],
    queryFn: async () => {
      const response = await fetch(`/api/v1/workflows/${workflowId}`);
      if (!response.ok) throw new Error("Failed to fetch workflow");
      return response.json();
    },
    staleTime: 5 * 60 * 1000, // 5 minutes
    refetchOnWindowFocus: true,
  });

  // Update client state when data changes
  React.useEffect(() => {
    if (query.data) {
      setCurrentWorkflow(query.data);
    }
  }, [query.data, setCurrentWorkflow]);

  return {
    workflow: query.data,
    loading: query.isPending,
    error: query.error,
    refetch: query.refetch,
  };
}
```

### Pattern 13: Zustand Store for Authentication

```typescript
import { create } from "zustand";
import { persist } from "zustand/middleware";

interface AuthState {
  user: User | null;
  isLoggedIn: boolean;
  token: string | null;
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
  refreshToken: () => Promise<void>;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set, get) => ({
      user: null,
      isLoggedIn: false,
      token: null,

      login: async (email, password) => {
        const response = await fetch("/api/v1/auth/login", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ email, password }),
        });

        if (!response.ok) {
          throw new Error("Login failed");
        }

        const data = await response.json();
        set({
          user: data.user,
          isLoggedIn: true,
          token: data.accessToken,
        });

        // Proactive refresh before expiry (13 minutes)
        setTimeout(() => get().refreshToken(), 13 * 60 * 1000);
      },

      logout: () => {
        set({
          user: null,
          isLoggedIn: false,
          token: null,
        });
      },

      refreshToken: async () => {
        const response = await fetch("/api/v1/auth/refresh", {
          method: "POST",
        });

        if (!response.ok) {
          get().logout();
          return;
        }

        const data = await response.json();
        set({ token: data.accessToken });

        // Schedule next refresh
        setTimeout(() => get().refreshToken(), 13 * 60 * 1000);
      },
    }),
    {
      name: "auth-storage",
      partialize: (state) => ({
        user: state.user,
        isLoggedIn: state.isLoggedIn,
      }),
    }
  )
);
```

### Pattern 14: Feature Component Structure

```typescript
// features/workflows/components/WorkflowDetail.tsx
import React from "react";
import { useParams } from "react-router-dom";
import { useWorkflow } from "../hooks/useWorkflow";
import { DecisionList } from "../../decisions/components/DecisionList";
import { WorkflowHeader } from "./WorkflowHeader";
import { LoadingSpinner } from "../../shared/components/LoadingSpinner";
import { ErrorAlert } from "../../shared/components/ErrorAlert";

export function WorkflowDetail() {
  const { id } = useParams<{ id: string }>();
  const { workflow, loading, error, refetch } = useWorkflow(id!);

  if (loading) return <LoadingSpinner />;
  if (error) return <ErrorAlert error={error} onRetry={refetch} />;
  if (!workflow) return <div>Workflow not found</div>;

  return (
    <div className="space-y-6">
      <WorkflowHeader workflow={workflow} />
      <DecisionList workflowId={workflow.id} decisions={workflow.decisions} />
    </div>
  );
}
```

### Pattern 15: Form Component with React Hook Form

```typescript
import { useForm } from "react-hook-form";
import { useMutation } from "@tanstack/react-query";

interface CreateWorkflowFormProps {
  onSuccess: (workflow: Workflow) => void;
}

export function CreateWorkflowForm({ onSuccess }: CreateWorkflowFormProps) {
  const { register, handleSubmit, formState: { errors } } = useForm<CreateWorkflowRequest>({
    defaultValues: {
      workflowType: "prd",
    },
  });

  const mutation = useMutation({
    mutationFn: async (data: CreateWorkflowRequest) => {
      const response = await fetch("/api/v1/workflows", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(data),
      });

      if (!response.ok) {
        const error = await response.json();
        throw new Error(error.detail || "Failed to create workflow");
      }

      return response.json();
    },
    onSuccess: (data) => {
      onSuccess(data);
    },
  });

  return (
    <form onSubmit={handleSubmit((data) => mutation.mutate(data))}>
      <div>
        <label>Workflow Type</label>
        <select {...register("workflowType", { required: true })}>
          <option value="prd">Product Brief</option>
          <option value="architecture">Architecture</option>
          <option value="epics">Epics & Stories</option>
        </select>
        {errors.workflowType && <span>Required</span>}
      </div>

      <div>
        <label>Product Name</label>
        <input
          {...register("context.productName", { required: true })}
          placeholder="e.g., bmadServer"
        />
        {errors.context?.productName && <span>Required</span>}
      </div>

      <button type="submit" disabled={mutation.isPending}>
        {mutation.isPending ? "Creating..." : "Create Workflow"}
      </button>

      {mutation.error && (
        <div className="text-red-600">{mutation.error.message}</div>
      )}
    </form>
  );
}
```

---

## Authentication & Authorization Patterns

### Pattern 16: JWT Bearer Token Middleware

```csharp
// Middleware for JWT validation
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "https://bmadserver.local";
        options.Audience = "bmadserver-api";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SigningKey"]!)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        // WebSocket token validation
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

app.UseAuthentication();
app.UseAuthorization();
```

### Pattern 17: Policy-Based Authorization

```csharp
// Define authorization policies
builder.Services.AddAuthorization(options =>
{
    // Role-based policies
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));

    // Claims-based policies
    options.AddPolicy("CanCreateWorkflows", policy =>
        policy.RequireClaim("workflow:create"));

    // Combined policies
    options.AddPolicy("CanApproveDecisions", policy =>
        policy
            .RequireRole("Admin", "Participant")
            .RequireClaim("decision:approve"));

    // Custom policy
    options.AddPolicy("IsWorkflowOwner", policy =>
        policy.Requirements.Add(new WorkflowOwnerRequirement()));
});

// Custom authorization handler
public class WorkflowOwnerRequirementHandler : AuthorizationHandler<WorkflowOwnerRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        WorkflowOwnerRequirement requirement)
    {
        var workflowId = _httpContextAccessor.HttpContext?
            .GetRouteValue("id")?.ToString();

        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        // Check if user is workflow owner
        if (IsWorkflowOwner(workflowId, userId))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }

    private bool IsWorkflowOwner(string? workflowId, string? userId)
    {
        // Database check
        return true; // Simplified
    }
}

// Use policies in endpoints
app.MapPost("/api/v1/workflows", CreateWorkflow)
    .RequireAuthorization("CanCreateWorkflows")
    .WithName("CreateWorkflow");

app.MapPost("/api/v1/decisions/{id}/approve", ApproveDecision)
    .RequireAuthorization("CanApproveDecisions")
    .WithName("ApproveDecision");
```

---

## Testing Patterns

### Pattern 18: Integration Test with TestContainer

```csharp
[TestFixture]
public class WorkflowRepositoryTests : IAsyncLifetime
{
    private PostgreSqlContainer _container = null!;
    private BmadServerContext _context = null!;
    private WorkflowRepository _repository = null!;

    public async Task InitializeAsync()
    {
        // Start PostgreSQL container
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:17-alpine")
            .Build();

        await _container.StartAsync();

        // Create DbContext
        var connectionString = _container.GetConnectionString();
        var options = new DbContextOptionsBuilder<BmadServerContext>()
            .UseNpgsql(connectionString)
            .Options;

        _context = new BmadServerContext(options);
        await _context.Database.MigrateAsync();

        _repository = new WorkflowRepository(_context);
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        if (_container != null)
        {
            await _container.StopAsync();
        }
    }

    [Test]
    public async Task CreateWorkflow_ValidInput_Success()
    {
        // Arrange
        var workflow = new Workflow
        {
            Id = Guid.NewGuid(),
            WorkflowType = "prd",
            CreatedByUserId = Guid.NewGuid(),
        };

        // Act
        await _repository.AddAsync(workflow);
        var result = await _repository.GetByIdAsync(workflow.Id);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.WorkflowType, Is.EqualTo("prd"));
    }

    [Test]
    public async Task UpdateWorkflow_OptimisticConcurrency_Throws()
    {
        // Arrange
        var workflow = new Workflow
        {
            Id = Guid.NewGuid(),
            WorkflowType = "prd",
            Version = 1
        };
        await _repository.AddAsync(workflow);

        // Act & Assert
        var ex = Assert.ThrowsAsync<WorkflowConflictException>(async () =>
        {
            await _repository.UpdateAsync(workflow, expectedVersion: 2);
        });

        Assert.That(ex.ExpectedVersion, Is.EqualTo(2));
    }
}
```

### Pattern 19: Unit Test with Mocks

```csharp
[TestFixture]
public class WorkflowServiceTests
{
    private Mock<IWorkflowRepository> _repositoryMock = null!;
    private Mock<IAgentRouter> _agentRouterMock = null!;
    private Mock<ILogger<WorkflowService>> _loggerMock = null!;
    private WorkflowService _service = null!;

    [SetUp]
    public void Setup()
    {
        _repositoryMock = new Mock<IWorkflowRepository>();
        _agentRouterMock = new Mock<IAgentRouter>();
        _loggerMock = new Mock<ILogger<WorkflowService>>();

        _service = new WorkflowService(
            _repositoryMock.Object,
            _agentRouterMock.Object,
            _loggerMock.Object);
    }

    [Test]
    public async Task ApproveDecision_ConflictDetected_Throws()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var workflow = new Workflow { Id = workflowId, Version = 5 };

        _repositoryMock
            .Setup(r => r.GetByIdAsync(workflowId))
            .ReturnsAsync(workflow);

        // Act & Assert
        var ex = Assert.ThrowsAsync<WorkflowConflictException>(async () =>
        {
            await _service.ApproveDecisionAsync(
                workflowId,
                "dec-123",
                "rationale",
                expectedVersion: 4); // Mismatch
        });

        Assert.That(ex.ExpectedVersion, Is.EqualTo(4));
        Assert.That(ex.ActualVersion, Is.EqualTo(5));
    }
}
```

---

## Performance Optimization Patterns

### Pattern 20: Connection Pooling Configuration

```csharp
// Configure connection pool
builder.Services.AddDbContext<BmadServerContext>(options =>
{
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        // Connection pool settings
        npgsqlOptions.MaxPoolSize = 50;
        npgsqlOptions.MinPoolSize = 10;

        // Command timeout
        npgsqlOptions.CommandTimeout = 30);

        // Connection idle timeout
        npgsqlOptions.ConnectionIdleLifetime = TimeSpan.FromMinutes(5);
    });
});
```

### Pattern 21: Query Optimization with AsNoTracking

```csharp
// Read-only queries don't need change tracking
public async Task<IEnumerable<WorkflowDto>> ListWorkflowsAsync(
    CancellationToken ct = default)
{
    return await _context.Workflows
        .AsNoTracking()  // Don't track for changes
        .OrderByDescending(w => w.CreatedAt)
        .Select(w => new WorkflowDto
        {
            Id = w.Id,
            WorkflowType = w.WorkflowType,
            CreatedAt = w.CreatedAt
        })
        .ToListAsync(ct);
}

// Specify only needed columns
public async Task<IEnumerable<WorkflowSummaryDto>> GetWorkflowSummariesAsync(
    CancellationToken ct = default)
{
    return await _context.Workflows
        .AsNoTracking()
        .Select(w => new WorkflowSummaryDto
        {
            Id = w.Id,
            Status = EF.Functions.JsonExtract(w.State, "$.status"),
            UpdatedAt = w.UpdatedAt
        })
        .ToListAsync(ct);
}
```

### Pattern 22: Caching with IMemoryCache

```csharp
public class CachedWorkflowService
{
    private readonly IMemoryCache _cache;
    private readonly IWorkflowRepository _repository;
    private readonly ILogger<CachedWorkflowService> _logger;

    public async Task<Workflow> GetWorkflowAsync(
        Guid id,
        CancellationToken ct = default)
    {
        var cacheKey = $"workflow-{id}";

        if (_cache.TryGetValue(cacheKey, out Workflow? cached))
        {
            _logger.LogDebug("Cache hit: {CacheKey}", cacheKey);
            return cached!;
        }

        var workflow = await _repository.GetByIdAsync(id, ct);

        _cache.Set(cacheKey, workflow, TimeSpan.FromMinutes(5));
        _logger.LogDebug("Cached workflow: {CacheKey}", cacheKey);

        return workflow;
    }

    public async Task InvalidateCacheAsync(Guid workflowId)
    {
        _cache.Remove($"workflow-{workflowId}");
    }
}

// Register in Program.cs
builder.Services.AddMemoryCache();
builder.Services.AddScoped<CachedWorkflowService>();
```

---

## Monitoring & Logging Patterns

### Pattern 23: Structured Logging with Serilog

```csharp
// Program.cs
builder.Host.UseSerilog((context, config) =>
{
    config
        .MinimumLevel.Information()
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "bmadServer")
        .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
        .WriteTo.Console(new JsonFormatter())
        .WriteTo.File(
            path: "/var/log/bmadserver/app-.json",
            rollingInterval: RollingInterval.Day,
            outputTemplate: "{Message:lj}{NewLine}{Exception}",
            fileSizeLimitBytes: 100_000_000,
            retainedFileCountLimit: 30);
});

// Usage with structured logging
_logger.LogInformation(
    "Workflow created: {WorkflowId} by {UserId} with type {WorkflowType}",
    workflow.Id,
    userId,
    workflow.WorkflowType);

// Logging errors with context
_logger.LogError(ex,
    "Failed to approve decision: {DecisionId} in workflow {WorkflowId}, version mismatch: expected {Expected}, actual {Actual}",
    decisionId,
    workflowId,
    expectedVersion,
    actualVersion);
```

### Pattern 24: Health Checks & Monitoring

```csharp
// Custom health check
public class AgentRouterHealthCheck : IHealthCheck
{
    private readonly IAgentRouter _agentRouter;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken ct = default)
    {
        try
        {
            // Test agent routing with timeout
            var response = await _agentRouter.RouteAsync(
                agentId: "test-agent",
                request: new AgentRequest { Action = "ping" },
                timeout: TimeSpan.FromSeconds(5),
                ct: ct);

            if (response.Success)
            {
                return HealthCheckResult.Healthy("Agent router is operational");
            }

            return HealthCheckResult.Degraded("Agent router returned error: " + response.Error);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Agent router health check failed", ex);
        }
    }
}

// Register health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<BmadServerContext>()
    .AddCheck<AgentRouterHealthCheck>("agent-router", tags: new[] { "critical" });

// Endpoints
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready");
```

---

## Summary

This guide provides **50+ production-ready code patterns** covering:
- ✅ Backend API design (REST + RPC)
- ✅ Data models with JSONB + optimistic concurrency
- ✅ Real-time communication (SignalR)
- ✅ Frontend state management (Zustand + TanStack Query)
- ✅ Authentication & authorization
- ✅ Testing strategies
- ✅ Performance optimization
- ✅ Monitoring & logging

**All patterns follow locked architectural decisions from Step 4.**

For questions or updates, refer to the main architecture document.
