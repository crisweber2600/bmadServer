# Story 3.5: Chat History & Scroll Management

**Status:** done

## Story

As a user (Sarah), I want to review previous messages in our conversation, so that I can reference earlier context and decisions.

## Acceptance Criteria

**Given** I open a workflow chat  
**When** the chat loads  
**Then** the last 50 messages display with scroll position at bottom

**Given** I scroll up to view older messages  
**When** I reach the top  
**Then** "Load More" appears and loads next 50 messages without scroll jump

**Given** I am scrolled up reading old messages  
**When** a new message arrives  
**Then** a "New message" badge appears at bottom without disrupting my position

**Given** I close and reopen the chat  
**When** the chat reloads  
**Then** my last scroll position is restored

**Given** chat history is empty (new workflow)  
**When** I view the chat  
**Then** I see a welcome message with quick-start buttons for common workflows

## Tasks / Subtasks

- [x] Analyze acceptance criteria and create detailed implementation plan
- [x] Design data models and database schema if needed
  - [x] Reused existing WorkflowState.ConversationHistory model
- [x] Implement core business logic
  - [x] ChatController with paginated history endpoints
  - [x] GetChatHistory endpoint with 50 messages per page
  - [x] GetRecentMessages endpoint for initial load
  - [x] Scroll position management logic
- [x] Create API endpoints and/or UI components
  - [x] useScrollManagement React hook for scroll tracking
  - [x] Auto-scroll to bottom on new messages
  - [x] "Load More" button at top for pagination
  - [x] "New message" badge when scrolled up
  - [x] Welcome message for empty chats
- [x] Write unit tests for critical paths
  - [x] useScrollManagement hook tests (scroll detection, badge display)
- [x] Write integration tests for key scenarios
  - [x] ChatControllerTests for pagination and history retrieval
  - [x] Empty state handling tests
  - [x] Validation tests for invalid parameters
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

**Backend:**
- `/src/bmadServer.ApiService/Controllers/ChatController.cs` - Paginated chat history endpoints
- `/src/bmadServer.Tests/Integration/ChatControllerTests.cs` - Comprehensive controller tests

**Frontend:**
- `/src/frontend/src/hooks/useScrollManagement.ts` - React hook for scroll position management
- `/src/frontend/src/hooks/useScrollManagement.test.ts` - Hook unit tests
- `/src/frontend/src/hooks/index.ts` - Hook exports (updated)

---

## Aspire Development Standards

### PostgreSQL Connection Pattern

This story uses PostgreSQL configured in Story 1.2 via Aspire:
- Chat history paginated from Aspire-managed PostgreSQL
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

- Source: [epics.md - Story 3.5](../planning-artifacts/epics.md)
- Architecture: [architecture.md](../planning-artifacts/architecture.md)
- PRD: [prd.md](../planning-artifacts/prd.md)
- **Aspire Rules:** [PROJECT-WIDE-RULES.md](../../../PROJECT-WIDE-RULES.md)
- **Aspire Docs:** https://aspire.dev
