# Story 2.4: Session Persistence & Recovery

**Status:** ready-for-dev

## Story

As a user (Marcus) working on a BMAD workflow,
I want my workflow state to persist across disconnects and server restarts,
so that I can resume my work within 60 seconds without losing progress.

## Acceptance Criteria

**Given** I am authenticated and working on a workflow  
**When** my SignalR connection establishes  
**Then** the system creates a Session record in the Sessions table:
```
- Id (UUID)
- UserId (foreign key to Users)
- ConnectionId (SignalR connection ID)
- WorkflowState (JSONB column storing workflow context)
- LastActivityAt (timestamp)
- CreatedAt, ExpiresAt (30 min idle timeout)
```
**And** the session state includes:
  - Current workflow name and step number
  - Agent context and conversation history (last 10 messages)
  - Decision lock states
  - Pending user inputs

**Given** I am actively working in a session  
**When** I perform any action (send message, make decision, advance workflow)  
**Then** the system updates the session WorkflowState JSONB field  
**And** updates LastActivityAt timestamp  
**And** increments the session `_version` field (optimistic concurrency)  
**And** sets `_lastModifiedBy` to my userId  
**And** all updates happen in a database transaction

**Given** my network connection drops unexpectedly  
**When** my client reconnects within 60 seconds (NFR6 requirement)  
**Then** the system:
  - Matches my userId to existing Session record
  - Validates the session has not expired (LastActivityAt < 30 min ago)
  - Associates new ConnectionId with existing Session
**And** sends a SignalR message with restored workflow state:
```json
{
  "type": "SESSION_RESTORED",
  "session": {
    "id": "uuid",
    "workflowName": "create-prd",
    "currentStep": 3,
    "conversationHistory": [...],
    "pendingInput": "Waiting for user to confirm feature list"
  }
}
```
**And** the client UI restores to the exact workflow state before disconnect

**Given** I disconnect and reconnect after 61 seconds  
**When** my client attempts to reconnect  
**Then** the system creates a NEW session (old one expired per 60s window)  
**And** the workflow state is recovered from the Sessions table (if not expired)  
**And** I see a notification: "Session recovered from last checkpoint"  
**And** I can continue from the last saved workflow step

**Given** the server restarts while I have an active session  
**When** the server comes back online  
**Then** my session state persists in PostgreSQL (not lost)  
**And** when I reconnect, the session recovery flow triggers  
**And** I resume from the last persisted workflow state

**Given** I have multiple active sessions (laptop + mobile)  
**When** I open bmadServer on a different device  
**Then** the system creates a separate Session record per device  
**And** each session tracks its own ConnectionId  
**And** workflow state synchronizes across sessions via PostgreSQL  
**And** last-write-wins concurrency control applies (based on `_version` field)

**Given** two concurrent session updates occur (race condition)  
**When** two devices update workflow state simultaneously  
**Then** the system detects version mismatch (optimistic concurrency violation)  
**And** the second update fails with 409 Conflict  
**And** the client receives conflict notification and can retry/merge changes

**Given** a session has been idle for 30 minutes  
**When** the system runs session cleanup (background job)  
**Then** the session ExpiresAt timestamp is checked  
**And** expired sessions are marked as inactive (not deleted - audit trail)  
**And** ConnectionId is cleared to prevent reconnection

**Given** the Sessions table migration is created  
**When** I run `dotnet ef migrations add AddSessionsTable`  
**Then** the migration includes:
  - Sessions table with JSONB WorkflowState column
  - GIN index on WorkflowState for fast JSONB queries
  - Indexes on UserId and ConnectionId
  - Foreign key constraint to Users table
  - Check constraint on ExpiresAt > CreatedAt

## Tasks / Subtasks

- [ ] **Task 1: Create Session entity and migration** (AC: Database schema)
  - [ ] Create `Models/Session.cs` entity with all required fields
  - [ ] Create `Models/WorkflowState.cs` for JSONB structure
  - [ ] Add DbSet<Session> to ApplicationDbContext
  - [ ] Configure JSONB column for WorkflowState using EF Core
  - [ ] Add GIN index for JSONB queries
  - [ ] Add indexes on UserId, ConnectionId
  - [ ] Run migration and verify table structure

- [ ] **Task 2: Implement session service** (AC: Session management)
  - [ ] Create `Services/ISessionService.cs` interface
  - [ ] Create `Services/SessionService.cs` implementation
  - [ ] Implement CreateSession() - new session on connection
  - [ ] Implement GetActiveSession() - find by userId and ConnectionId
  - [ ] Implement UpdateSessionState() - persist workflow state
  - [ ] Implement RecoverSession() - restore from disconnect
  - [ ] Implement ExpireSession() - mark as inactive

- [ ] **Task 3: Implement optimistic concurrency control** (AC: Concurrency criteria)
  - [ ] Add _version, _lastModifiedBy, _lastModifiedAt to WorkflowState
  - [ ] Configure EF Core concurrency token on _version field
  - [ ] Handle DbUpdateConcurrencyException in updates
  - [ ] Return 409 Conflict with conflict details
  - [ ] Write unit tests for concurrent update scenarios

- [ ] **Task 4: Integrate with SignalR connection lifecycle** (AC: Connection criteria)
  - [ ] Handle OnConnectedAsync in ChatHub
  - [ ] Create or recover session on connection
  - [ ] Update ConnectionId when reconnecting
  - [ ] Handle OnDisconnectedAsync - don't immediately expire
  - [ ] Send SESSION_RESTORED message on recovery
  - [ ] Test reconnection within 60 seconds

- [ ] **Task 5: Implement session state updates** (AC: Activity tracking)
  - [ ] Update LastActivityAt on every user action
  - [ ] Persist WorkflowState changes to database
  - [ ] Increment _version on every update
  - [ ] Use database transactions for atomic updates
  - [ ] Batch updates where appropriate for performance

- [ ] **Task 6: Implement session cleanup background job** (AC: Cleanup criteria)
  - [ ] Create `BackgroundServices/SessionCleanupService.cs`
  - [ ] Register as IHostedService in DI
  - [ ] Run every 5 minutes to check expired sessions
  - [ ] Mark sessions as inactive (IsActive = false)
  - [ ] Clear ConnectionId on expired sessions
  - [ ] Don't delete for audit trail purposes

- [ ] **Task 7: Handle multi-device scenarios** (AC: Multi-device criteria)
  - [ ] Allow multiple sessions per user (one per device)
  - [ ] Sync workflow state across sessions via database
  - [ ] Handle last-write-wins for concurrent edits
  - [ ] Consider websocket broadcast for cross-device updates

## Dev Notes

### Session Entity Model

```csharp
// Models/Session.cs
public class Session
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string? ConnectionId { get; set; } // SignalR connection ID
    public WorkflowState? WorkflowState { get; set; } // JSONB
    public DateTime LastActivityAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
    
    // Computed property for NFR6 (60s recovery window)
    public bool IsWithinRecoveryWindow => 
        DateTime.UtcNow.Subtract(LastActivityAt).TotalSeconds <= 60;
}

// Models/WorkflowState.cs (JSONB structure)
public class WorkflowState
{
    public string? WorkflowName { get; set; }
    public int CurrentStep { get; set; }
    public List<ChatMessage> ConversationHistory { get; set; } = new();
    public Dictionary<string, bool> DecisionLocks { get; set; } = new();
    public string? PendingInput { get; set; }
    public string? AgentContext { get; set; }
    
    // Concurrency control per architecture.md
    public int _version { get; set; } = 1;
    public Guid _lastModifiedBy { get; set; }
    public DateTime _lastModifiedAt { get; set; }
}

public class ChatMessage
{
    public string Id { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty; // "user" or "agent"
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string? AgentId { get; set; }
}
```

### Entity Framework JSONB Configuration

```csharp
// In ApplicationDbContext.OnModelCreating
modelBuilder.Entity<Session>(entity =>
{
    entity.HasKey(e => e.Id);
    entity.HasIndex(e => e.UserId);
    entity.HasIndex(e => e.ConnectionId);
    entity.HasIndex(e => e.ExpiresAt);
    entity.HasIndex(e => e.IsActive);
    
    // JSONB column with GIN index for fast queries
    entity.Property(e => e.WorkflowState)
        .HasColumnType("jsonb");
    
    entity.HasIndex(e => e.WorkflowState)
        .HasMethod("gin");
    
    // Concurrency token on _version within JSONB
    // Note: EF Core handles this via RowVersion or shadow property
    entity.Property<uint>("xmin")
        .IsRowVersion();
    
    entity.HasOne(e => e.User)
        .WithMany()
        .HasForeignKey(e => e.UserId)
        .OnDelete(DeleteBehavior.Cascade);
    
    // Check constraint
    entity.HasCheckConstraint("CK_Session_Expiry", 
        "\"ExpiresAt\" > \"CreatedAt\"");
});
```

### Session Recovery Flow

```csharp
// Services/SessionService.cs
public async Task<Session?> RecoverSessionAsync(Guid userId, string newConnectionId)
{
    // Find most recent active session for user
    var session = await _dbContext.Sessions
        .Where(s => s.UserId == userId && s.IsActive)
        .OrderByDescending(s => s.LastActivityAt)
        .FirstOrDefaultAsync();
    
    if (session == null)
        return null;
    
    // Check if within 60-second recovery window (NFR6)
    if (session.IsWithinRecoveryWindow)
    {
        // Direct recovery - same session
        session.ConnectionId = newConnectionId;
        session.LastActivityAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();
        return session;
    }
    
    // Outside recovery window but session still valid (< 30 min idle)
    var idleMinutes = DateTime.UtcNow.Subtract(session.LastActivityAt).TotalMinutes;
    if (idleMinutes < 30)
    {
        // Create new session but restore workflow state
        var newSession = new Session
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ConnectionId = newConnectionId,
            WorkflowState = session.WorkflowState, // Restore state!
            LastActivityAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(30),
            IsActive = true
        };
        
        // Mark old session as inactive
        session.IsActive = false;
        session.ConnectionId = null;
        
        _dbContext.Sessions.Add(newSession);
        await _dbContext.SaveChangesAsync();
        return newSession;
    }
    
    // Session expired - no recovery
    return null;
}
```

### SignalR Integration

```csharp
// Hubs/ChatHub.cs
public override async Task OnConnectedAsync()
{
    var userId = GetUserIdFromClaims();
    var connectionId = Context.ConnectionId;
    
    var session = await _sessionService.RecoverSessionAsync(userId, connectionId);
    
    if (session?.WorkflowState != null)
    {
        // Send recovery message to client
        await Clients.Caller.SendAsync("SESSION_RESTORED", new
        {
            session.Id,
            session.WorkflowState.WorkflowName,
            session.WorkflowState.CurrentStep,
            session.WorkflowState.ConversationHistory,
            session.WorkflowState.PendingInput
        });
    }
    else
    {
        // New session - create fresh
        await _sessionService.CreateSessionAsync(userId, connectionId);
    }
    
    await base.OnConnectedAsync();
}
```

### Background Cleanup Service

```csharp
// BackgroundServices/SessionCleanupService.cs
public class SessionCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SessionCleanupService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await CleanupExpiredSessionsAsync();
            await Task.Delay(_interval, stoppingToken);
        }
    }
    
    private async Task CleanupExpiredSessionsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();
        
        var expiredSessions = await dbContext.Sessions
            .Where(s => s.IsActive && s.ExpiresAt < DateTime.UtcNow)
            .ToListAsync();
        
        foreach (var session in expiredSessions)
        {
            session.IsActive = false;
            session.ConnectionId = null;
            _logger.LogInformation("Expired session {SessionId} for user {UserId}",
                session.Id, session.UserId);
        }
        
        await dbContext.SaveChangesAsync();
    }
}
```

### Architecture Alignment

Per architecture.md requirements:
- Data Modeling: Hybrid (EF Core 9.0 + PostgreSQL JSONB)
- JSONB Concurrency Control: `_version`, `_lastModifiedBy`, `_lastModifiedAt` fields
- Session Recovery: NFR6 - within 60 seconds
- Idle Timeout: 30 minutes of inactivity forces re-login

### Dependencies

- Npgsql.EntityFrameworkCore.PostgreSQL (already added)
- Microsoft.AspNetCore.SignalR (added in Epic 3)

## Files to Create/Modify

### New Files
- `bmadServer.ApiService/Models/Session.cs`
- `bmadServer.ApiService/Models/WorkflowState.cs`
- `bmadServer.ApiService/Services/ISessionService.cs`
- `bmadServer.ApiService/Services/SessionService.cs`
- `bmadServer.ApiService/BackgroundServices/SessionCleanupService.cs`
- `bmadServer.ApiService/Data/Migrations/YYYYMMDD_AddSessionsTable.cs`

### Modified Files
- `bmadServer.ApiService/Data/ApplicationDbContext.cs` - Add DbSet<Session>, configure entity
- `bmadServer.ApiService/Program.cs` - Register SessionService, SessionCleanupService
- `bmadServer.ApiService/Hubs/ChatHub.cs` - Add OnConnectedAsync/OnDisconnectedAsync (created in Epic 3)

## References

- Source: [epics.md - Story 2.4](../_bmad-output/planning-artifacts/epics.md)
- Architecture: [architecture.md](../_bmad-output/planning-artifacts/architecture.md) - Data architecture, JSONB section
- PRD: [prd.md](../_bmad-output/planning-artifacts/prd.md) - FR16, FR17, NFR6
