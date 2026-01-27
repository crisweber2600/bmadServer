# Story 8.3: Technical Language Mode

**Status:** review

## Story

As a developer (Marcus), I want full technical details, so that I can make informed implementation decisions.

## Acceptance Criteria

**Given** I have Technical persona set  
**When** an agent generates content  
**Then** I receive full technical details including: code snippets, API specifications, architecture diagrams

**Given** I view a workflow step  
**When** technical details are available  
**Then** I see: data schemas, integration points, performance considerations, security implications

**Given** I ask a technical question  
**When** the agent responds  
**Then** the response includes: specific technologies, version numbers, configuration examples

**Given** I'm in technical mode  
**When** business stakeholders join the workflow  
**Then** they see their persona-appropriate version  
**And** my view remains technical

**Given** I switch to Hybrid mode  
**When** the context changes  
**Then** responses adapt: technical for implementation steps, business for strategy decisions

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
- `src/bmadServer.ApiService/Services/IResponseMetadataService.cs` - Interface for response metadata service
- `src/bmadServer.ApiService/Services/ResponseMetadataService.cs` - Analyzes content for technical indicators
- `src/bmadServer.Tests/Unit/TechnicalLanguageModeTests.cs` - 8 comprehensive tests for technical mode

### Modified Files:
- `src/bmadServer.ApiService/Services/ITranslationService.cs` - Changed to return TranslationResult object
- `src/bmadServer.ApiService/Services/TranslationService.cs` - Returns structured result with metadata
- `src/bmadServer.ApiService/Hubs/ChatHub.cs` - Added ContentMetadata to SignalR responses
- `src/bmadServer.ApiService/Program.cs` - Registered IResponseMetadataService

### Implementation Details:
1. **TranslationResult**: Structured return type with Content, OriginalContent, WasTranslated, PersonaType
2. **Technical Mode**: Returns original content unchanged for Technical personas
3. **Content Metadata**: Responses include metadata indicating content type (technical/business)
4. **Preservation**: Code snippets, version numbers, architecture details all preserved for technical users
5. **SignalR Integration**: ChatHub sends both translated and original content with metadata

### Test Coverage:
- Technical persona receives full technical details (code, APIs, architecture)
- Code snippets preserved (markdown code blocks)
- Version numbers maintained
- Architecture and security details intact
- Performance metrics preserved
- Multi-persona views work correctly (each sees their persona-appropriate version)


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

- Source: [epics.md - Story 8.3](../planning-artifacts/epics.md)
- Architecture: [architecture.md](../planning-artifacts/architecture.md)
- PRD: [prd.md](../planning-artifacts/prd.md)
