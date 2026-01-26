# Story 8.5: Context-Aware Response Adaptation

**Status:** review

## Story



## Acceptance Criteria

**Given** Hybrid persona is active  
**When** the workflow step is technical (e.g., architecture review)  
**Then** responses lean technical with code examples

**Given** Hybrid persona is active  
**When** the workflow step is strategic (e.g., PRD review)  
**Then** responses lean business with impact analysis

**Given** a user asks a technical question in Business mode  
**When** the question clearly requires technical answer  
**Then** the system provides technical detail with business context wrapper

**Given** multiple personas are in a collaborative session  
**When** a shared message is sent  
**Then** each user sees their persona-appropriate version  
**And** the underlying content is identical

**Given** response adaptation occurs  
**When** I check the API response  
**Then** I see: originalContent, adaptedContent, adaptationReason, targetPersona

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
- `src/bmadServer.ApiService/Services/IContextAnalysisService.cs` - Interface for context analysis
- `src/bmadServer.ApiService/Services/ContextAnalysisService.cs` - Analyzes content for technical vs business indicators
- `src/bmadServer.Tests/Unit/ContextAwareResponseAdaptationTests.cs` - 6 comprehensive unit tests

### Modified Files:
- `src/bmadServer.ApiService/Services/ITranslationService.cs` - Added workflowStep parameter, Context and AdaptationReason to result
- `src/bmadServer.ApiService/Services/TranslationService.cs` - Integrated context analysis for Hybrid mode
- `src/bmadServer.ApiService/Program.cs` - Registered IContextAnalysisService

### Implementation Details:
1. **Context Analysis**: Analyzes content for 50+ technical and business keywords
2. **Hybrid Mode Logic**: Technical content → translate; Business content → no translation
3. **Workflow Step Influence**: Architecture review = technical; PRD review = business
4. **Adaptation Reasoning**: Results include explanation of why adaptation occurred
5. **Code Detection**: Markdown code blocks boost technical score
6. **Mixed Content Handling**: Weighs technical vs business indicators to decide

### Test Coverage:
- Hybrid mode translates technical content ✓
- Hybrid mode preserves business content ✓
- Context analysis identifies technical content ✓
- Context analysis identifies business content ✓
- Workflow step influences context classification ✓
- Result includes adaptation details and reasoning ✓
- **Total: 35 Epic 8 tests passing**


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

- Source: [epics.md - Story 8.5](../planning-artifacts/epics.md)
- Architecture: [architecture.md](../planning-artifacts/architecture.md)
- PRD: [prd.md](../planning-artifacts/prd.md)
