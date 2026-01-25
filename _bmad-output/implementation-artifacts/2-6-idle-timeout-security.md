# Story 2.6: Idle Timeout & Security

**Status:** ready-for-dev

## Story

As a security-conscious user (Marcus),
I want the system to automatically log me out after 30 minutes of inactivity,
so that my account remains secure if I leave my workstation unattended.

## Acceptance Criteria

**Given** I am logged in and active in the application  
**When** I perform any action (type message, click button, scroll)  
**Then** the client-side idle timer resets  
**And** the LastActivityAt timestamp updates in my Session record  
**And** the 30-minute timeout countdown restarts

**Given** I have been idle for 28 minutes  
**When** the client-side timer reaches 28 minutes  
**Then** a warning modal appears:
```
"You've been inactive for 28 minutes.
Your session will expire in 2 minutes.

[Extend Session]  [Logout Now]"
```
**And** the modal is prominently displayed (centered, overlay background)  
**And** keyboard focus moves to the "Extend Session" button

**Given** the warning modal is displayed  
**When** I click "Extend Session"  
**Then** the client sends POST `/api/v1/auth/extend-session`  
**And** the server updates my Session.LastActivityAt to current timestamp  
**And** the modal closes  
**And** the idle timer resets to 0 minutes  
**And** I continue working without interruption

**Given** the warning modal is displayed  
**When** I click "Logout Now"  
**Then** the system immediately logs me out  
**And** calls POST `/api/v1/auth/logout` to revoke refresh token  
**And** redirects me to the login page  
**And** displays message: "You have been logged out"

**Given** I ignore the warning modal and reach 30 minutes idle  
**When** the idle timer reaches 30 minutes  
**Then** the system automatically logs me out  
**And** revokes my refresh token  
**And** clears all session cookies  
**And** redirects to login page with message: "Your session expired due to inactivity"

**Given** I am logged out due to idle timeout  
**When** I return and login again  
**Then** the system attempts to recover my last session state (if < 2 minutes elapsed per NFR15)  
**And** I see a notification: "Welcome back! Your previous session has been restored."  
**And** I resume from where I left off

**Given** the idle timeout endpoint exists  
**When** I send POST `/api/v1/auth/extend-session` with valid access token  
**Then** the system validates the token is not expired  
**And** updates the Session.LastActivityAt timestamp  
**And** returns 204 No Content

**Given** I send extend-session request with expired access token  
**When** the request is processed  
**Then** the system returns 401 Unauthorized  
**And** the client triggers automatic token refresh flow  
**And** retries the extend-session request with new token

**Given** the idle timeout configuration exists  
**When** I review appsettings.json  
**Then** I see:
```json
{
  "Session": {
    "IdleTimeoutMinutes": 30,
    "WarningTimeoutMinutes": 28
  }
}
```
**And** these values are configurable per environment

## Tasks / Subtasks

- [ ] **Task 1: Add session configuration settings** (AC: Configuration)
  - [ ] Create `Configuration/SessionOptions.cs` with IdleTimeoutMinutes, WarningTimeoutMinutes
  - [ ] Add Session section to appsettings.json with default values (30, 28)
  - [ ] Register IOptions<SessionOptions> in DI container
  - [ ] Add environment-specific overrides (appsettings.Development.json)

- [ ] **Task 2: Implement extend-session API endpoint** (AC: Session extension)
  - [ ] Add POST `/api/v1/auth/extend-session` to AuthController
  - [ ] Add `[Authorize]` attribute to require valid access token
  - [ ] Get user ID from JWT claims
  - [ ] Find active session by userId
  - [ ] Update Session.LastActivityAt to current timestamp
  - [ ] Update Session.ExpiresAt to now + idle timeout
  - [ ] Return 204 No Content on success
  - [ ] Return 404 if no active session found

- [ ] **Task 3: Update logout endpoint for idle timeout** (AC: Logout flow)
  - [ ] Ensure POST `/api/v1/auth/logout` revokes refresh token
  - [ ] Mark session as inactive (IsActive = false)
  - [ ] Clear ConnectionId from session
  - [ ] Return 204 No Content
  - [ ] Support optional "reason" parameter for audit log

- [ ] **Task 4: Implement client-side idle detection** (AC: Client timer)
  - [ ] Create IdleTimeoutService/hook in React frontend
  - [ ] Track user activity events (mouse, keyboard, scroll, click)
  - [ ] Debounce activity detection to avoid excessive API calls
  - [ ] Reset timer on any activity
  - [ ] Periodically call extend-session API (e.g., every 5 minutes of activity)

- [ ] **Task 5: Create warning modal component** (AC: Warning UI)
  - [ ] Create IdleTimeoutWarningModal component using Ant Design Modal
  - [ ] Display at 28 minutes of inactivity (configurable)
  - [ ] Show countdown timer for remaining 2 minutes
  - [ ] "Extend Session" button calls extend-session API
  - [ ] "Logout Now" button calls logout API
  - [ ] Modal is centered with overlay backdrop
  - [ ] Focus trap within modal for accessibility

- [ ] **Task 6: Implement automatic logout on timeout** (AC: Auto-logout)
  - [ ] Trigger logout when timer reaches 30 minutes
  - [ ] Clear local storage (tokens, session data)
  - [ ] Disconnect SignalR connection
  - [ ] Redirect to login page
  - [ ] Pass "reason=idle_timeout" query parameter
  - [ ] Display appropriate message on login page

- [ ] **Task 7: Handle session recovery after idle logout** (AC: Session recovery)
  - [ ] On login, check for recoverable session (< 2 minutes per NFR15)
  - [ ] If session recoverable, restore workflow state
  - [ ] Display "Welcome back!" notification
  - [ ] If not recoverable, start fresh session
  - [ ] Integrate with existing session recovery from Story 2.4

- [ ] **Task 8: Update activity tracking on user actions** (AC: Server-side activity)
  - [ ] Update Session.LastActivityAt on API calls
  - [ ] Update on SignalR message receive
  - [ ] Debounce server-side updates (max once per minute)
  - [ ] Use middleware or action filter for automatic tracking

- [ ] **Task 9: Write tests** (AC: Verification)
  - [ ] Unit test extend-session endpoint
  - [ ] Unit test logout with reason parameter
  - [ ] Integration test idle timeout flow
  - [ ] Frontend test for idle detection
  - [ ] Frontend test for warning modal

## Dev Notes

### Session Configuration

```csharp
// Configuration/SessionOptions.cs
public class SessionOptions
{
    public const string SectionName = "Session";
    
    public int IdleTimeoutMinutes { get; set; } = 30;
    public int WarningTimeoutMinutes { get; set; } = 28;
    
    // Derived properties
    public TimeSpan IdleTimeout => TimeSpan.FromMinutes(IdleTimeoutMinutes);
    public TimeSpan WarningTimeout => TimeSpan.FromMinutes(WarningTimeoutMinutes);
}

// In Program.cs
builder.Services.Configure<SessionOptions>(
    builder.Configuration.GetSection(SessionOptions.SectionName));
```

### Extend Session Endpoint

```csharp
// In AuthController.cs
[HttpPost("extend-session")]
[Authorize]
public async Task<IActionResult> ExtendSession()
{
    var userId = GetUserIdFromClaims();
    
    var session = await _dbContext.Sessions
        .Where(s => s.UserId == userId && s.IsActive)
        .OrderByDescending(s => s.LastActivityAt)
        .FirstOrDefaultAsync();
    
    if (session == null)
        return NotFound(new { error = "No active session found" });
    
    session.LastActivityAt = DateTime.UtcNow;
    session.ExpiresAt = DateTime.UtcNow.Add(_sessionOptions.IdleTimeout);
    
    await _dbContext.SaveChangesAsync();
    
    return NoContent();
}
```

### Client-Side Idle Detection (React)

```typescript
// hooks/useIdleTimeout.ts
import { useEffect, useRef, useState, useCallback } from 'react';
import { message } from 'antd';

interface IdleTimeoutConfig {
  idleTimeoutMs: number;      // 30 * 60 * 1000
  warningTimeoutMs: number;   // 28 * 60 * 1000
  activityPingIntervalMs: number; // 5 * 60 * 1000
}

export function useIdleTimeout(config: IdleTimeoutConfig) {
  const [showWarning, setShowWarning] = useState(false);
  const [remainingSeconds, setRemainingSeconds] = useState(120);
  const lastActivityRef = useRef(Date.now());
  const warningTimerRef = useRef<NodeJS.Timeout>();
  const logoutTimerRef = useRef<NodeJS.Timeout>();
  
  const resetTimer = useCallback(() => {
    lastActivityRef.current = Date.now();
    setShowWarning(false);
    
    // Clear existing timers
    if (warningTimerRef.current) clearTimeout(warningTimerRef.current);
    if (logoutTimerRef.current) clearTimeout(logoutTimerRef.current);
    
    // Set warning timer
    warningTimerRef.current = setTimeout(() => {
      setShowWarning(true);
      startCountdown();
    }, config.warningTimeoutMs);
    
    // Set logout timer
    logoutTimerRef.current = setTimeout(() => {
      handleAutoLogout();
    }, config.idleTimeoutMs);
  }, [config]);
  
  const startCountdown = () => {
    let seconds = 120;
    const interval = setInterval(() => {
      seconds--;
      setRemainingSeconds(seconds);
      if (seconds <= 0) clearInterval(interval);
    }, 1000);
  };
  
  const handleExtendSession = async () => {
    try {
      await api.post('/auth/extend-session');
      resetTimer();
      message.success('Session extended');
    } catch (error) {
      message.error('Failed to extend session');
    }
  };
  
  const handleAutoLogout = async () => {
    await api.post('/auth/logout', { reason: 'idle_timeout' });
    localStorage.clear();
    window.location.href = '/login?reason=idle_timeout';
  };
  
  // Track user activity
  useEffect(() => {
    const events = ['mousedown', 'keydown', 'scroll', 'touchstart'];
    const debouncedReset = debounce(resetTimer, 1000);
    
    events.forEach(event => 
      document.addEventListener(event, debouncedReset));
    
    resetTimer(); // Initial setup
    
    return () => {
      events.forEach(event => 
        document.removeEventListener(event, debouncedReset));
    };
  }, [resetTimer]);
  
  return {
    showWarning,
    remainingSeconds,
    handleExtendSession,
    handleLogoutNow: handleAutoLogout
  };
}
```

### Warning Modal Component

```tsx
// components/IdleTimeoutWarningModal.tsx
import { Modal, Button, Typography } from 'antd';
import { ExclamationCircleOutlined } from '@ant-design/icons';

interface Props {
  visible: boolean;
  remainingSeconds: number;
  onExtend: () => void;
  onLogout: () => void;
}

export function IdleTimeoutWarningModal({ 
  visible, 
  remainingSeconds, 
  onExtend, 
  onLogout 
}: Props) {
  const minutes = Math.floor(remainingSeconds / 60);
  const seconds = remainingSeconds % 60;
  
  return (
    <Modal
      open={visible}
      closable={false}
      maskClosable={false}
      centered
      footer={null}
      width={400}
    >
      <div style={{ textAlign: 'center', padding: '20px 0' }}>
        <ExclamationCircleOutlined 
          style={{ fontSize: 48, color: '#faad14', marginBottom: 16 }} 
        />
        <Typography.Title level={4}>
          Session Timeout Warning
        </Typography.Title>
        <Typography.Paragraph>
          You've been inactive. Your session will expire in:
        </Typography.Paragraph>
        <Typography.Title level={2} style={{ color: '#cf1322' }}>
          {minutes}:{seconds.toString().padStart(2, '0')}
        </Typography.Title>
        <div style={{ marginTop: 24, display: 'flex', gap: 12, justifyContent: 'center' }}>
          <Button type="primary" size="large" onClick={onExtend} autoFocus>
            Extend Session
          </Button>
          <Button size="large" onClick={onLogout}>
            Logout Now
          </Button>
        </div>
      </div>
    </Modal>
  );
}
```

### Activity Tracking Middleware

```csharp
// Middleware/ActivityTrackingMiddleware.cs
public class ActivityTrackingMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly TimeSpan _debounceInterval = TimeSpan.FromMinutes(1);
    
    public async Task InvokeAsync(
        HttpContext context, 
        ApplicationDbContext dbContext)
    {
        await _next(context);
        
        // Only track for authenticated users
        if (!context.User.Identity?.IsAuthenticated ?? true)
            return;
        
        var userId = GetUserIdFromClaims(context.User);
        await UpdateLastActivityAsync(dbContext, userId);
    }
    
    private async Task UpdateLastActivityAsync(
        ApplicationDbContext dbContext, 
        Guid userId)
    {
        // Debounce: only update if last update was > 1 minute ago
        var session = await dbContext.Sessions
            .Where(s => s.UserId == userId && s.IsActive)
            .Where(s => s.LastActivityAt < DateTime.UtcNow.Subtract(_debounceInterval))
            .OrderByDescending(s => s.LastActivityAt)
            .FirstOrDefaultAsync();
        
        if (session != null)
        {
            session.LastActivityAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync();
        }
    }
}
```

### Login Page Message Display

```tsx
// pages/Login.tsx
import { useSearchParams } from 'react-router-dom';
import { Alert } from 'antd';

export function LoginPage() {
  const [searchParams] = useSearchParams();
  const reason = searchParams.get('reason');
  
  const getMessage = () => {
    switch (reason) {
      case 'idle_timeout':
        return {
          type: 'warning' as const,
          message: 'Your session expired due to inactivity'
        };
      case 'manual':
        return {
          type: 'success' as const,
          message: 'You have been logged out'
        };
      default:
        return null;
    }
  };
  
  const alertInfo = getMessage();
  
  return (
    <div>
      {alertInfo && (
        <Alert 
          type={alertInfo.type} 
          message={alertInfo.message}
          showIcon
          style={{ marginBottom: 24 }}
        />
      )}
      {/* Login form */}
    </div>
  );
}
```

### appsettings.json

```json
{
  "Session": {
    "IdleTimeoutMinutes": 30,
    "WarningTimeoutMinutes": 28
  }
}
```

### Architecture Alignment

Per architecture.md requirements:
- Session Management: 30-minute idle timeout
- Security: Automatic logout protects unattended sessions
- UX: Warning before automatic logout (NFR15)
- Recovery: Session state preserved for quick recovery

### Dependencies

- Backend: No additional packages required
- Frontend: Ant Design Modal (already included), react-router-dom

## Files to Create/Modify

### New Files
- `bmadServer.ApiService/Configuration/SessionOptions.cs`
- `bmadServer.ApiService/Middleware/ActivityTrackingMiddleware.cs`
- `bmadServer.Web/src/hooks/useIdleTimeout.ts`
- `bmadServer.Web/src/components/IdleTimeoutWarningModal.tsx`

### Modified Files
- `bmadServer.ApiService/appsettings.json` - Add Session section
- `bmadServer.ApiService/appsettings.Development.json` - Override for dev (shorter timeout for testing)
- `bmadServer.ApiService/Controllers/AuthController.cs` - Add extend-session endpoint
- `bmadServer.ApiService/Program.cs` - Register SessionOptions, ActivityTrackingMiddleware
- `bmadServer.Web/src/pages/Login.tsx` - Display timeout message
- `bmadServer.Web/src/App.tsx` - Integrate IdleTimeoutWarningModal

---

## Aspire Development Standards

### PostgreSQL Connection Pattern

This story uses PostgreSQL configured in Story 1.2 via Aspire:
- Connection string automatically injected from Aspire AppHost
- Session LastActivityAt updates against Aspire-managed PostgreSQL
- Pattern: `builder.AddServiceDefaults();` (inherits PostgreSQL reference)
- See Story 1.2 for AppHost configuration pattern

### Project-Wide Standards

This story follows the Aspire-first development pattern:
- **Reference:** [PROJECT-WIDE-RULES.md](../../../PROJECT-WIDE-RULES.md)
- **Primary Documentation:** https://aspire.dev
- **GitHub:** https://github.com/microsoft/aspire

### Aspire-Specific Notes

- ActivityTrackingMiddleware runs within Aspire orchestration
- Session timeout events logged to OpenTelemetry (visible in Aspire Dashboard)
- Configuration via appsettings works with Aspire environment management

---

## References

- Source: [epics.md - Story 2.6](../planning-artifacts/epics.md)
- Architecture: [architecture.md](../planning-artifacts/architecture.md) - Session management, security
- PRD: [prd.md](../planning-artifacts/prd.md) - NFR15 (2 min resume)
- **Aspire Rules:** [PROJECT-WIDE-RULES.md](../../../PROJECT-WIDE-RULES.md)
- **Aspire Docs:** https://aspire.dev
