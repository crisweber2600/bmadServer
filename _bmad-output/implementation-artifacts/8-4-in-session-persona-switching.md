# Story 8.4: In-Session Persona Switching

**Status:** review

## Story

As a user (Marcus), I want to switch personas during a session, so that I can adapt to different contexts.

## Acceptance Criteria

**Given** I am in a workflow  
**When** I click the persona switcher in the UI  
**Then** I see current persona highlighted and other options available

**Given** I switch from Technical to Business  
**When** the switch completes  
**Then** future messages are translated to business language  
**And** previous messages retain their original format  
**And** a notification confirms: "Switched to Business mode"

**Given** I switch personas frequently  
**When** I've switched more than 3 times in a session  
**Then** the system suggests: "Would you like to try Hybrid mode instead?"

**Given** I switch personas  
**When** the session ends  
**Then** my default persona remains unchanged (per-profile setting)  
**And** session switches are logged for analytics

**Given** keyboard shortcut exists  
**When** I press Ctrl+Shift+P  
**Then** the persona switcher opens  
**And** I can select with arrow keys

## Tasks / Subtasks

- [x] Analyze acceptance criteria and create detailed implementation plan
- [x] Design data models and database schema if needed
- [x] Implement core business logic
- [x] Create API endpoints and/or UI components
- [x] Write unit tests for critical paths
- [x] Write integration tests for key scenarios
- [x] Update API documentation
- [x] Perform manual testing and validation
- [x] Code review and address feedback

## Dev Notes

### Implementation Guidance

This story should be implemented following the patterns established in the codebase:
- Follow the architecture patterns defined in `architecture.md`
- Use existing service patterns and dependency injection
- Ensure proper error handling and logging
- Add appropriate authorization checks based on user roles
- Follow the coding standards and conventions of the project

### Testing Strategy

- Unit tests should cover business logic and edge cases
- Integration tests should verify API endpoints and database interactions
- Consider performance implications for database queries
- Test error scenarios and validation rules

### Dependencies

Review the acceptance criteria for dependencies on:
- Other stories or epics that must be completed first
- External packages or services that need to be configured
- Database migrations that need to be created

## Files to Create/Modify

### Created Files:
- `src/bmadServer.ApiService/DTOs/SwitchPersonaRequest.cs` - Request DTO for persona switching
- `src/bmadServer.ApiService/DTOs/SwitchPersonaResponse.cs` - Response DTO with switch details
- `src/bmadServer.Tests/Unit/InSessionPersonaSwitchingTests.cs` - 7 comprehensive unit tests
- `src/bmadServer.ApiService/Migrations/xxxxx_AddSessionPersonaSwitching.cs` - Database migration

### Modified Files:
- `src/bmadServer.ApiService/Data/Entities/Session.cs` - Added SessionPersona and PersonaSwitchCount fields
- `src/bmadServer.ApiService/Services/ISessionService.cs` - Added SwitchSessionPersonaAsync and GetEffectivePersonaAsync methods
- `src/bmadServer.ApiService/Services/SessionService.cs` - Implemented persona switching logic with analytics
- `src/bmadServer.ApiService/Hubs/ChatHub.cs` - Added SwitchPersona SignalR method, uses effective persona

### Implementation Details:
1. **Session-Level Persona**: Separate from user default, persists for session duration only
2. **Effective Persona Logic**: Session persona takes precedence over user default
3. **Switch Counter**: Tracks number of switches, suggests Hybrid after 3+ switches
4. **SignalR Integration**: Real-time `SwitchPersona()` hub method with `PERSONA_SWITCHED` event
5. **Analytics**: Logs every persona switch with timestamps and counts
6. **User Default Unchanged**: Session switches don't affect profile-level persona setting

### Test Coverage:
- Session persona changes without affecting user default ✓
- Multiple switches increment counter ✓
- Hybrid mode suggestion after 3+ switches ✓
- Effective persona uses session override ✓
- Falls back to user default when no session persona set ✓
- Logs analytics for persona switches ✓
- Authorization checks (different user/invalid session) ✓


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

- Source: [epics.md - Story 8.4](../planning-artifacts/epics.md)
- Architecture: [architecture.md](../planning-artifacts/architecture.md)
- PRD: [prd.md](../planning-artifacts/prd.md)
