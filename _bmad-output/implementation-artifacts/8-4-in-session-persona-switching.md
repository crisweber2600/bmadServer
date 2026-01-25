# Story 8.4: In-Session Persona Switching

Status: ready-for-dev

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a user (Marcus),
I want to switch personas during an active session,
so that I can adapt my communication mode to different contexts without logging out.

## Acceptance Criteria

### AC1: Persona Switcher UI Component
**Given** I am in a workflow  
**When** I click the persona switcher in the UI  
**Then** I see current persona highlighted and other options available
- Current persona is visually distinct (highlighted, checkmark, or badge)
- All three options are displayed: Technical, Business, Hybrid
- UI component is accessible and responsive

### AC2: Persona Switch Execution
**Given** I switch from Technical to Business  
**When** the switch completes  
**Then** future messages are translated to business language  
**And** previous messages retain their original format  
**And** a notification confirms: "Switched to Business mode"
- Switch happens immediately (< 1 second)
- Future agent responses are persona-appropriate
- Chat history is NOT retroactively translated
- Toast/notification displays confirmation

### AC3: Frequent Switching Detection
**Given** I switch personas frequently  
**When** I've switched more than 3 times in a session  
**Then** the system suggests: "Would you like to try Hybrid mode instead?"
- System tracks switches per session
- Suggestion appears as non-intrusive notification
- User can dismiss or accept suggestion
- If accepted, persona switches to Hybrid

### AC4: Session vs Profile Persona Separation
**Given** I switch personas  
**When** the session ends  
**Then** my default persona remains unchanged (per-profile setting)  
**And** session switches are logged for analytics
- User profile's default persona is NOT modified
- Session-level persona is temporary
- Analytics log includes: timestamp, from_persona, to_persona, session_id

### AC5: Keyboard Shortcut
**Given** keyboard shortcut exists  
**When** I press Ctrl+Shift+P  
**Then** the persona switcher opens  
**And** I can select with arrow keys
- Keyboard shortcut works globally within the app
- Arrow key navigation cycles through options
- Enter key confirms selection
- Escape key closes switcher without changes

## Tasks / Subtasks

- [ ] **Task 1: Session-Level Persona Storage** (AC: #2, #4)
  - [ ] Extend `Session` entity with `CurrentPersona` property (nullable)
  - [ ] Add `PersonaChanges` collection to track switches
  - [ ] Create `PersonaChange` entity: { SessionId, FromPersona, ToPersona, Timestamp }
  - [ ] Create database migration for session persona tracking
  - [ ] Update `SessionService` to manage session persona

- [ ] **Task 2: Persona Switch API Endpoint** (AC: #2, #4)
  - [ ] Create `PUT /api/v1/sessions/me/persona` endpoint
  - [ ] Implement `UpdateSessionPersonaCommand` handler
  - [ ] Validate persona value (Technical, Business, Hybrid)
  - [ ] Update session's current persona
  - [ ] Log persona change in `PersonaChanges` table
  - [ ] Return updated session state with confirmation

- [ ] **Task 3: Frequent Switch Detection Service** (AC: #3)
  - [ ] Create `IPersonaSwitchAnalyzer` interface
  - [ ] Implement `PersonaSwitchAnalyzer` service
  - [ ] Track switches per session (in-memory or cache)
  - [ ] Detect threshold: 3+ switches → suggest Hybrid
  - [ ] Return suggestion flag in API response
  - [ ] Register service in DI container

- [ ] **Task 4: Persona Resolution Middleware Enhancement** (AC: #2)
  - [ ] Extend `PersonaTranslationMiddleware` to check session persona first
  - [ ] Persona resolution order: Session.CurrentPersona > User.PersonaType (default)
  - [ ] Pass effective persona to translation/enhancement services
  - [ ] Ensure middleware respects session overrides

- [ ] **Task 5: SignalR Real-Time Persona Change Notification** (AC: #2)
  - [ ] Extend ChatHub with `NotifyPersonaChanged(personaType)` method
  - [ ] Broadcast persona change confirmation to client
  - [ ] Client displays toast notification with new persona
  - [ ] Update client-side persona state immediately

- [ ] **Task 6: Frontend Persona Switcher UI Component** (AC: #1, #5)
  - [ ] Create React component: `PersonaSwitcher.tsx`
  - [ ] Display current persona with highlight/badge
  - [ ] Radio buttons or dropdown for selection
  - [ ] Call API on persona change
  - [ ] Handle API response and update local state
  - [ ] Display confirmation notification
  - [ ] Implement keyboard shortcut (Ctrl+Shift+P)
  - [ ] Arrow key navigation for options
  - [ ] Integrate into chat interface header

- [ ] **Task 7: Frequent Switch Suggestion UI** (AC: #3)
  - [ ] Create suggestion notification component
  - [ ] Display when API response includes `suggestHybrid: true`
  - [ ] "Try Hybrid mode?" with Accept/Dismiss buttons
  - [ ] If accepted, call persona switch API with Hybrid
  - [ ] Dismiss removes notification without action

- [ ] **Task 8: Analytics Logging** (AC: #4)
  - [ ] Ensure `PersonaChange` entity is properly logged
  - [ ] Include fields: session_id, user_id, from_persona, to_persona, timestamp, reason
  - [ ] Create analytics query endpoints (admin only)
  - [ ] Dashboard widget: persona switch frequency (future enhancement)

- [ ] **Task 9: Testing** (AC: #1-5)
  - [ ] Unit tests for PersonaSwitchAnalyzer
  - [ ] Unit tests for session persona resolution logic
  - [ ] Integration test: Switch persona via API
  - [ ] Integration test: Session persona overrides user default
  - [ ] Integration test: Frequent switch detection triggers suggestion
  - [ ] Integration test: Profile default persona unchanged after session switch
  - [ ] Integration test: Keyboard shortcut triggers switcher
  - [ ] E2E test: Full persona switch flow in UI

- [ ] **Task 10: Documentation** (AC: #1-5)
  - [ ] Document persona switching in user guide
  - [ ] Add API documentation for persona switch endpoint
  - [ ] Document session vs profile persona behavior
  - [ ] Add keyboard shortcut to shortcut reference
  - [ ] Update developer guide with persona resolution logic

## Dev Notes

### Architecture Compliance

**Persona Translation Engine (Architecture.md lines 290-296):**
- This story enables dynamic persona switching within a session
- Session-level persona overrides user's default profile persona
- Switching applies to future responses only (no retroactive translation)
- Switch detection helps users discover Hybrid mode for flexible needs
- Analytics provide insights into persona usage patterns

**Integration Points:**
- Builds on Epic 8, Story 1 (PersonaType infrastructure)
- Extends Epic 8, Stories 2 & 3 (translation and technical enhancement)
- Integrates with Epic 3 (ChatHub for real-time notifications)
- Integrates with Epic 2 (Session management)
- Uses existing JWT authentication for user identification

**State Persistence:**
- Session entity tracks current persona (temporary override)
- PersonaChange entity logs all switches for analytics
- User entity's PersonaType remains the default (unchanged by session switches)
- Session persona is cleared when session ends

### Project Structure Notes

**Files to Create:**
```
src/bmadServer.ApiService/
  Data/
    Entities/
      PersonaChange.cs                           # EF entity for switch tracking
  DTOs/
    UpdateSessionPersonaRequest.cs               # Request DTO
    UpdateSessionPersonaResponse.cs              # Response DTO with suggestHybrid flag
  Controllers/
    SessionPersonaController.cs                  # Persona switch endpoint
  Services/
    Persona/
      IPersonaSwitchAnalyzer.cs                  # Interface
      PersonaSwitchAnalyzer.cs                   # Frequent switch detection
  Migrations/
    {timestamp}_AddSessionPersonaTracking.cs     # EF migration

src/bmadServer.Web/
  src/
    components/
      PersonaSwitcher.tsx                        # Persona switcher UI
      PersonaSuggestionNotification.tsx          # Hybrid mode suggestion
    hooks/
      usePersonaSwitcher.ts                      # React hook for state management
      useKeyboardShortcut.ts                     # Keyboard shortcut handler
    services/
      personaApi.ts                              # API calls for persona switching
```

**Files to Modify:**
```
src/bmadServer.ApiService/
  Program.cs                                     # Register PersonaSwitchAnalyzer
  Data/Entities/Session.cs                      # Add CurrentPersona property
  Data/ApplicationDbContext.cs                  # Add PersonaChanges DbSet
  Middleware/PersonaTranslationMiddleware.cs    # Check session persona first
  Hubs/ChatHub.cs                               # Add persona change notification method

src/bmadServer.Web/
  src/
    components/ChatInterface.tsx                 # Integrate PersonaSwitcher
    App.tsx                                      # Register keyboard shortcut
```

**Files to Test:**
```
src/bmadServer.Tests/
  Unit/Services/PersonaSwitchAnalyzerTests.cs    # Switch detection tests
  Integration/PersonaSwitchingIntegrationTests.cs # End-to-end API tests
  E2E/PersonaSwitcherUITests.cs                  # UI interaction tests (if E2E exists)
```

### Technical Requirements

**Session Persona Data Model:**
```csharp
// Extension to existing Session entity
public class Session
{
    // Existing properties...
    
    public PersonaType? CurrentPersona { get; set; }  // Nullable: null = use User.PersonaType
    public ICollection<PersonaChange> PersonaChanges { get; set; } = [];
}

public class PersonaChange
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public Session Session { get; set; } = null!;
    
    public PersonaType FromPersona { get; set; }
    public PersonaType ToPersona { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? Reason { get; set; }  // e.g., "manual", "suggestion_accepted"
}
```

**Persona Resolution Logic:**
```csharp
// In PersonaTranslationMiddleware or service
public PersonaType ResolveEffectivePersona(User user, Session? session)
{
    // Priority: Session override > User default
    return session?.CurrentPersona ?? user.PersonaType;
}
```

**Frequent Switch Detection Algorithm:**
```csharp
public class PersonaSwitchAnalyzer : IPersonaSwitchAnalyzer
{
    private const int SuggestionThreshold = 3;
    
    public async Task<bool> ShouldSuggestHybrid(Guid sessionId)
    {
        var switchCount = await _context.PersonaChanges
            .Where(pc => pc.SessionId == sessionId)
            .CountAsync();
            
        return switchCount >= SuggestionThreshold;
    }
}
```

**API Endpoint Specification:**
```csharp
[HttpPut("me/persona")]
[ProducesResponseType(typeof(UpdateSessionPersonaResponse), 200)]
[ProducesResponseType(typeof(ProblemDetails), 400)]
public async Task<IActionResult> UpdateSessionPersona(
    [FromBody] UpdateSessionPersonaRequest request)
{
    var userId = GetUserIdFromClaims();
    var session = await _sessionService.GetActiveSessionAsync(userId);
    
    if (session == null)
        return BadRequest("No active session found");
    
    var oldPersona = session.CurrentPersona ?? session.User.PersonaType;
    session.CurrentPersona = request.PersonaType;
    
    // Log the change
    session.PersonaChanges.Add(new PersonaChange
    {
        FromPersona = oldPersona,
        ToPersona = request.PersonaType,
        Timestamp = DateTime.UtcNow,
        Reason = "manual"
    });
    
    await _context.SaveChangesAsync();
    
    // Check if suggestion should be shown
    var suggestHybrid = await _switchAnalyzer.ShouldSuggestHybrid(session.Id);
    
    // Notify via SignalR
    await _hubContext.Clients.User(userId.ToString())
        .SendAsync("PersonaChanged", request.PersonaType);
    
    return Ok(new UpdateSessionPersonaResponse
    {
        SessionId = session.Id,
        NewPersona = request.PersonaType,
        Message = $"Switched to {request.PersonaType} mode",
        SuggestHybrid = suggestHybrid && request.PersonaType != PersonaType.Hybrid
    });
}
```

**SignalR Notification:**
```csharp
// In ChatHub.cs
public async Task NotifyPersonaChanged(PersonaType newPersona)
{
    var userId = GetUserIdFromClaims();
    await Clients.User(userId.ToString()).SendAsync("PersonaChanged", new
    {
        PersonaType = newPersona,
        Message = $"Switched to {newPersona} mode",
        Timestamp = DateTime.UtcNow
    });
}
```

**Frontend React Hook:**
```typescript
export const usePersonaSwitcher = () => {
  const [currentPersona, setCurrentPersona] = useState<PersonaType>('Hybrid');
  const [showSuggestion, setShowSuggestion] = useState(false);

  const switchPersona = async (newPersona: PersonaType) => {
    try {
      const response = await personaApi.updateSessionPersona(newPersona);
      setCurrentPersona(newPersona);
      
      if (response.suggestHybrid) {
        setShowSuggestion(true);
      }
      
      toast.success(response.message);
    } catch (error) {
      toast.error('Failed to switch persona');
    }
  };

  return { currentPersona, switchPersona, showSuggestion, setShowSuggestion };
};
```

**Keyboard Shortcut Implementation:**
```typescript
useEffect(() => {
  const handleKeyDown = (event: KeyboardEvent) => {
    if (event.ctrlKey && event.shiftKey && event.key === 'P') {
      event.preventDefault();
      setShowSwitcher(true);
    }
  };

  document.addEventListener('keydown', handleKeyDown);
  return () => document.removeEventListener('keydown', handleKeyDown);
}, []);
```

### Library & Framework Requirements

**Existing Stack (No New Major Dependencies):**
- .NET 8.0 / C# 12
- ASP.NET Core 8.0
- Entity Framework Core 8.0 with Npgsql
- PostgreSQL 16 (persona_changes table)
- SignalR (for real-time persona change notifications)
- React 18+ with TypeScript
- React Hot Toast (for notifications) - check if already present, or use similar

**No New Backend NuGet Packages Required**

**Frontend Dependencies (Likely Existing):**
- react-hot-toast or similar notification library
- Existing state management (Zustand per architecture.md)
- Existing SignalR client connection

### File Structure Requirements

**Follow existing conventions:**
- Services in `Services/` with interface + implementation pattern
- Entities in `Data/Entities/` with EF navigation properties
- DTOs in `DTOs/` with XML documentation
- Controllers in `Controllers/` with standard REST patterns
- React components in `src/components/`
- React hooks in `src/hooks/`
- API services in `src/services/`

### Testing Requirements

**Code Coverage:**
- Maintain existing coverage standards
- Test persona resolution priority (session > profile)
- Test frequent switch detection at threshold boundaries (2, 3, 4 switches)
- Test that profile persona remains unchanged after session switch
- Test keyboard shortcut triggers UI component
- Test SignalR notification delivery

**Test Scenarios:**
1. User switches from Technical to Business → future responses are translated
2. User switches personas 3+ times → hybrid suggestion appears
3. Session ends → user's default persona is unchanged
4. User switches to Hybrid via suggestion → switches logged correctly
5. Keyboard shortcut opens switcher → selection works via arrow keys
6. Multiple sessions with different personas → each session independent
7. Session persona overrides user default → middleware respects override
8. API returns 400 when no active session exists

**Performance Testing:**
- Persona switch API response < 500ms
- Frequent switch detection query < 50ms
- SignalR notification delivery < 200ms
- UI switcher opens instantly (< 100ms)

### Previous Story Intelligence

**Story 8.1 (Persona Profile Configuration):**
- User entity includes `PersonaType` property (Technical, Business, Hybrid)
- Default persona is stored in user profile
- GET `/api/v1/users/me` returns persona information
- PATCH `/api/v1/users/me/persona` updates default persona
- This story adds SESSION-LEVEL persona that overrides the profile default

**Story 8.2 (Business Language Translation):**
- `PersonaTranslationService` handles translation logic
- `PersonaTranslationMiddleware` intercepts responses
- This story extends middleware to check session persona first
- Translation applies based on effective persona (session override or user default)

**Story 8.3 (Technical Language Mode):**
- Technical persona receives enhanced content
- Hybrid mode adapts based on context
- This story enables switching between modes dynamically
- Content enhancement/translation applies to future messages only

**Key Learnings:**
- Use session state for temporary overrides
- Middleware is the right place for persona resolution
- Check `Session.CurrentPersona` before `User.PersonaType`
- SignalR is established pattern for real-time updates
- Use EF navigation properties for tracking relationships

**Files to Reference:**
- `src/bmadServer.ApiService/Data/Entities/User.cs` - User.PersonaType property
- `src/bmadServer.ApiService/Data/Entities/Session.cs` - Extend with CurrentPersona
- `src/bmadServer.ApiService/Models/PersonaType.cs` - Enum definition
- `src/bmadServer.ApiService/Middleware/PersonaTranslationMiddleware.cs` - Extend persona resolution
- `src/bmadServer.ApiService/Hubs/ChatHub.cs` - Add persona change notification

### Git Intelligence Summary

**Recent Patterns from Codebase:**
- Services registered in Program.cs with `builder.Services.AddScoped<IService, Service>()`
- Entities use navigation properties for relationships
- EF migrations created with: `dotnet ef migrations add MigrationName --project src/bmadServer.ApiService`
- DTOs use XML documentation for OpenAPI generation
- SignalR used for real-time notifications (established in ChatHub)

**Existing Infrastructure to Leverage:**
- Session management (Epic 2, Story 4)
- ChatHub for real-time notifications (Epic 3)
- PersonaTranslationMiddleware for persona resolution (Epic 8, Story 2)
- JWT authentication for user identification

### Latest Tech Information

**ASP.NET Core 8.0 Session Management:**
- Session state stored in PostgreSQL via EF Core
- Session entity extensible with custom properties
- Add nullable `CurrentPersona` property for temporary override

**Entity Framework Core 8.0 Nullable References:**
```csharp
public PersonaType? CurrentPersona { get; set; }  // Null = use default from User
```

**SignalR Targeted Notifications:**
```csharp
await Clients.User(userId.ToString()).SendAsync("PersonaChanged", payload);
```

**React Keyboard Event Handling:**
- Use `useEffect` hook with `addEventListener`
- Check `event.ctrlKey`, `event.shiftKey`, `event.key`
- Call `event.preventDefault()` to stop default browser behavior

**React Hot Toast (or similar):**
```typescript
import toast from 'react-hot-toast';
toast.success('Switched to Business mode');
```

### Project Context Reference

**Architecture Document:** `_bmad-output/planning-artifacts/architecture.md`
- Section: Persona Translation Engine (lines 290-296)
- Context-aware response adaptation

**PRD Document:** `_bmad-output/planning-artifacts/prd.md`
- FR15: Users can switch persona mode within a session
- NFR7: Response adaptation < 2 seconds

**Epic Details:** `_bmad-output/planning-artifacts/epics.md`
- Epic 8: Persona Translation & Language Adaptation (lines 2245-2387)
- Story 8.4: In-Session Persona Switching (lines 2354-2387)
- BDD acceptance criteria provided

**Previous Stories:**
- `8-1-persona-profile-configuration.md` - User default persona
- `8-2-business-language-translation.md` - Translation middleware
- `8-3-technical-language-mode.md` - Technical enhancement and hybrid mode

### Critical Implementation Notes

**🚨 PREVENT COMMON MISTAKES:**

1. **Session vs Profile Persona Separation:**
   - Session persona is TEMPORARY and does NOT modify User.PersonaType
   - When session ends, user's default persona remains unchanged
   - Always resolve: `session.CurrentPersona ?? user.PersonaType`

2. **No Retroactive Translation:**
   - Previous messages in chat history keep their original format
   - Only FUTURE agent responses use the new persona
   - Don't re-translate or re-enhance existing messages

3. **Frequent Switch Detection:**
   - Count ALL switches in current session (not just recent)
   - Threshold is 3+ switches total
   - Only suggest Hybrid if user is NOT already in Hybrid mode
   - Suggestion is non-intrusive (can be dismissed)

4. **Keyboard Shortcut Conflicts:**
   - Ensure Ctrl+Shift+P doesn't conflict with browser shortcuts
   - Test in multiple browsers (Chrome, Firefox, Safari)
   - Provide alternative UI button for accessibility

5. **SignalR Connection State:**
   - Ensure user is connected before sending notifications
   - Handle reconnection scenarios gracefully
   - Notification delivery should be fire-and-forget (no blocking)

6. **Performance Considerations:**
   - Frequent switch detection query must be fast (< 50ms)
   - Cache switch count in session memory if querying becomes slow
   - Persona resolution should not add latency to every request

7. **Testing Edge Cases:**
   - Switch persona when no active session → return 400
   - Switch to same persona → still log change, still count toward threshold
   - Multiple concurrent sessions → each has independent persona
   - Session expires → CurrentPersona is cleared

8. **UI/UX Considerations:**
   - Current persona must be visually obvious
   - Switch confirmation should be immediate (< 1 second)
   - Keyboard navigation must be intuitive (Tab, Arrow keys, Enter)
   - Accessible for screen readers (ARIA labels)

### Example Persona Switch Flow

**Scenario: User switches from Technical to Business mid-workflow**

1. **User Action:** Clicks persona switcher in UI (or presses Ctrl+Shift+P)
2. **Frontend:** Opens switcher, highlights current persona (Technical)
3. **User Action:** Selects "Business" and confirms
4. **Frontend:** Calls `PUT /api/v1/sessions/me/persona` with `{ personaType: "Business" }`
5. **Backend:**
   - Resolves active session for user
   - Updates `session.CurrentPersona = Business`
   - Logs change in `persona_changes` table
   - Checks switch count → if >= 3, set `suggestHybrid = true`
   - Saves to database
   - Sends SignalR notification to user
6. **Frontend:**
   - Receives API response
   - Updates local persona state
   - Displays toast: "Switched to Business mode"
   - If `suggestHybrid = true`, shows suggestion notification
7. **Future Messages:**
   - Middleware checks `session.CurrentPersona` (Business)
   - Applies business language translation
   - User sees translated responses

**Session Ends:**
- Session record persists in database
- `CurrentPersona` is cleared (or session is archived)
- User's `PersonaType` (default) remains unchanged
- Next session starts with user's default persona

### References

- [Source: epics.md#Story 8.4 (lines 2354-2387)] - Detailed BDD acceptance criteria
- [Source: architecture.md#Persona Translation Engine (lines 290-296)] - Architecture for persona system
- [Source: prd.md#FR15] - In-session persona switching requirement
- [Source: 8-1-persona-profile-configuration.md] - User default persona infrastructure
- [Source: 8-2-business-language-translation.md] - Translation middleware patterns
- [Source: 8-3-technical-language-mode.md] - Technical enhancement and hybrid mode
- [Source: src/bmadServer.ApiService/Data/Entities/Session.cs] - Session entity to extend
- [Source: src/bmadServer.ApiService/Middleware/PersonaTranslationMiddleware.cs] - Middleware to modify
- [Source: src/bmadServer.ApiService/Hubs/ChatHub.cs] - Chat hub for notifications

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

## Dev Agent Record

### Agent Model Used

_To be filled by Dev Agent_

### Debug Log References

_To be filled by Dev Agent_

### Completion Notes List

_To be filled by Dev Agent_

### File List

_To be filled by Dev Agent_
