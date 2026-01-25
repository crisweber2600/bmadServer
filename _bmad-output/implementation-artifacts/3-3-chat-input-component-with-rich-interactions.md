# Story 3.3: Chat Input Component with Rich Interactions

**Status:** done

## Story

As a user (Sarah), I want a responsive input field with helpful features, so that I can communicate effectively with BMAD agents.

## Acceptance Criteria

**Given** the chat interface is loaded  
**When** I view the input area  
**Then** I see multi-line text input, Send button (disabled when empty), character count, and keyboard shortcut hint

**Given** I type a message  
**When** I press Ctrl+Enter (or Cmd+Enter on Mac)  
**Then** the message is sent immediately and the input field clears

**Given** the input exceeds 2000 characters  
**When** I continue typing  
**Then** the character count turns red and Send button becomes disabled

**Given** I type a partial message and navigate away  
**When** I return to the chat  
**Then** my draft message is preserved in local storage

**Given** I type "/" in the input field  
**When** the command palette appears  
**Then** I see options: /help, /status, /pause, /resume with arrow key navigation

**Given** I send a message and the server is slow (> 5 seconds)  
**When** I see the processing indicator  
**Then** I can click "Cancel" to abort the request

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

### Created Files

- `src/frontend/src/components/ChatInput.tsx` - Main chat input component with multi-line text input, character count, keyboard shortcuts, draft persistence, and request cancellation
- `src/frontend/src/components/ChatInput.css` - Styling for chat input component
- `src/frontend/src/components/ChatInput.test.tsx` - Comprehensive unit tests (36 test cases) for ChatInput component
- `src/frontend/src/components/CommandPalette.tsx` - Command palette component with autocomplete and keyboard navigation
- `src/frontend/src/components/CommandPalette.css` - Styling for command palette component
- `src/frontend/src/components/CommandPalette.test.tsx` - Comprehensive unit tests (20 test cases) for CommandPalette component

### Modified Files

- `src/frontend/src/components/index.ts` - Added exports for ChatInput and CommandPalette components
- `src/frontend/src/test/setup.ts` - Added mocks for ResizeObserver and getBoundingClientRect to support Ant Design components in tests
- `src/frontend/package.json` - Added @testing-library/user-event as dev dependency

---

## Implementation Summary

### Components Implemented

1. **ChatInput Component** (`ChatInput.tsx`)
   - Multi-line text input using Ant Design TextArea
   - Character count display (max 2000 chars, turns red when exceeded)
   - Send button (disabled when empty or > 2000 chars)
   - Keyboard shortcut hint display (Ctrl+Enter / Cmd+Enter)
   - Ctrl+Enter / Cmd+Enter to send (not just Enter for multi-line support)
   - Draft persistence to localStorage (debounced saves, clears after send)
   - Request cancellation with AbortController (Cancel button after 5 seconds)
   - Error handling and loading states
   - Full accessibility (ARIA labels, keyboard navigation, screen reader support)

2. **CommandPalette Component** (`CommandPalette.tsx`)
   - Triggers when user types "/"
   - Shows commands: /help, /status, /pause, /resume
   - Arrow key navigation (up/down with wrapping)
   - Enter key to select command
   - Click outside to close
   - Escape key to close
   - Full accessibility (ARIA roles and labels)

### Test Coverage

- **Total Test Cases: 56** (well exceeding the 20+ requirement)
  - ChatInput: 36 tests covering:
    - Basic rendering (5 tests)
    - Text input and character count (4 tests)
    - Send button behavior (7 tests)
    - Keyboard shortcuts (3 tests)
    - Draft persistence (4 tests)
    - Request cancellation (4 tests)
    - Error handling (3 tests)
    - Loading state (2 tests)
    - Disabled state (1 test)
    - Command palette integration (3 tests)
  - CommandPalette: 20 tests covering:
    - Basic rendering (4 tests)
    - Command selection (3 tests)
    - Keyboard navigation (5 tests)
    - Command filtering (4 tests)
    - Click outside behavior (2 tests)
    - Visual states (2 tests)

### Acceptance Criteria Met

✅ **AC1**: Multi-line text input, Send button (disabled when empty), character count, and keyboard shortcut hint displayed

✅ **AC2**: Message sent on Ctrl+Enter (or Cmd+Enter on Mac), input clears after send

✅ **AC3**: Character count turns red and Send button disabled when > 2000 characters

✅ **AC4**: Draft message preserved in localStorage, restored on return

✅ **AC5**: Command palette appears on "/" with /help, /status, /pause, /resume options and arrow key navigation

✅ **AC6**: Cancel button appears for slow requests (> 5 seconds) and aborts request when clicked

---

## Aspire Development Standards

### SignalR Integration Pattern

This story uses SignalR configured in Story 3.1:
- **MVP:** Built-in ASP.NET Core SignalR (no external dependency)
- Message sending via SignalR hub connection
- See Story 3.1 for SignalR configuration pattern

### Project-Wide Standards

This story follows the Aspire-first development pattern:
- **Reference:** [PROJECT-WIDE-RULES.md](../../../PROJECT-WIDE-RULES.md)
- **Primary Documentation:** https://aspire.dev
- **GitHub:** https://github.com/microsoft/aspire

---

## References

- Source: [epics.md - Story 3.3](../planning-artifacts/epics.md)
- Architecture: [architecture.md](../planning-artifacts/architecture.md)
- PRD: [prd.md](../planning-artifacts/prd.md)
- **Aspire Rules:** [PROJECT-WIDE-RULES.md](../../../PROJECT-WIDE-RULES.md)
- **Aspire Docs:** https://aspire.dev
