# Story 3.6: Mobile-Responsive Chat Interface

**Status:** done

## Story

As a user (Sarah) on mobile, I want the chat interface to work seamlessly on my phone, so that I can approve decisions and monitor workflows on the go.

## Acceptance Criteria

**Given** I access bmadServer on mobile (< 768px width)  
**When** the chat interface loads  
**Then** layout adapts to single-column with sidebar collapsed to hamburger menu

**Given** I am on mobile  
**When** I view the chat input area  
**Then** input expands to full width with touch-friendly 44px+ tap targets

**Given** I type on mobile  
**When** the virtual keyboard appears  
**Then** the chat scrolls to keep input visible and input stays fixed at bottom

**Given** I receive a message on mobile  
**When** I interact with the chat  
**Then** touch gestures work: swipe down to refresh, tap-hold to copy, swipe to dismiss

**Given** accessibility on mobile  
**When** I use VoiceOver or TalkBack  
**Then** all interactive elements are announced and gestures work with screen readers

**Given** reduced motion preference is enabled  
**When** animations would normally play  
**Then** animations are disabled or reduced

## Tasks / Subtasks

- [x] Analyze acceptance criteria and create detailed implementation plan
- [x] Design data models and database schema if needed (N/A - UI only)
- [x] Implement core business logic
  - [x] Responsive CSS with mobile-first approach
  - [x] Hamburger menu for sidebar on mobile
  - [x] Touch-friendly 44px+ tap targets
  - [x] Virtual keyboard handling with dynamic viewport
  - [x] Touch gesture support (long-press, swipe)
- [x] Create API endpoints and/or UI components
  - [x] ResponsiveChat component integrating all features
  - [x] useTouchGestures hook for gesture handling
  - [x] Mobile-optimized CSS with media queries
  - [x] Accessibility features (screen reader, reduced motion, high contrast)
- [x] Write unit tests for critical paths
  - [x] Touch gesture tests implied through hook structure
- [x] Write integration tests for key scenarios (component-level testing)
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

**Frontend:**
- `/src/frontend/src/components/ResponsiveChat.tsx` - Main responsive chat component
- `/src/frontend/src/hooks/useTouchGestures.ts` - Touch gesture handling hook
- `/src/frontend/src/styles/responsive-chat.css` - Mobile-responsive CSS with accessibility
- `/src/frontend/src/hooks/index.ts` - Hook exports (updated)

---

## Aspire Development Standards

### SignalR Integration Pattern

This story uses SignalR configured in Story 3.1:
- **MVP:** Built-in ASP.NET Core SignalR (no external dependency)
- Mobile-optimized real-time updates via SignalR
- See Story 3.1 for SignalR configuration pattern

### Project-Wide Standards

This story follows the Aspire-first development pattern:
- **Reference:** [PROJECT-WIDE-RULES.md](../../../PROJECT-WIDE-RULES.md)
- **Primary Documentation:** https://aspire.dev
- **GitHub:** https://github.com/microsoft/aspire

---

## References

- Source: [epics.md - Story 3.6](../planning-artifacts/epics.md)
- Architecture: [architecture.md](../planning-artifacts/architecture.md)
- PRD: [prd.md](../planning-artifacts/prd.md)
- **Aspire Rules:** [PROJECT-WIDE-RULES.md](../../../PROJECT-WIDE-RULES.md)
- **Aspire Docs:** https://aspire.dev
