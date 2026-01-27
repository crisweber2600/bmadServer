# Story 8.2: Business Language Translation

**Status:** review

## Story

As a non-technical user (Sarah), I want technical outputs translated to business language, so that I can understand and make decisions.

## Acceptance Criteria

**Given** I have Business persona set  
**When** an agent generates technical content  
**Then** the response is automatically translated to business terms  
**And** technical jargon is replaced with plain language

**Given** a technical error occurs  
**When** I see the error message  
**Then** it explains the issue in business terms: "We couldn't save your changes because another team member is editing" (not "409 Conflict: optimistic concurrency violation")

**Given** architecture decisions are presented  
**When** I view the recommendations  
**Then** I see business impact: "This choice means faster loading times for users" (not "implementing CDN caching layer")

**Given** I need technical details  
**When** I click "Show Technical Details"  
**Then** I can expand to see the original technical content  
**And** this doesn't change my persona setting

**Given** translation quality is measured  
**When** I provide feedback on clarity  
**Then** the system logs my rating and improves translations over time

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
- `src/bmadServer.ApiService/Data/Entities/TranslationMapping.cs` - Entity for storing translation mappings
- `src/bmadServer.ApiService/Services/ITranslationService.cs` - Service interface for translation operations
- `src/bmadServer.ApiService/Services/TranslationService.cs` - Implementation with caching and pattern matching
- `src/bmadServer.ApiService/Controllers/TranslationsController.cs` - Admin API endpoints for managing mappings
- `src/bmadServer.ApiService/DTOs/TranslationMappingRequest.cs` - Request DTO
- `src/bmadServer.ApiService/DTOs/TranslationMappingResponse.cs` - Response DTO
- `src/bmadServer.Tests/Unit/TranslationServiceTests.cs` - Comprehensive unit tests (11 tests)
- `src/bmadServer.Tests/Integration/BusinessLanguageTranslationIntegrationTests.cs` - Integration tests (3 tests)
- `src/bmadServer.ApiService/Migrations/xxxxx_AddTranslationMappings.cs` - Database migration

### Modified Files:
- `src/bmadServer.ApiService/Data/ApplicationDbContext.cs` - Added TranslationMappings DbSet and configuration
- `src/bmadServer.ApiService/Program.cs` - Registered ITranslationService
- `src/bmadServer.ApiService/Hubs/ChatHub.cs` - Integrated translation service into message pipeline

### Implementation Details:
1. **Translation Service**: Pattern-based translation with word boundary matching and 5-minute cache
2. **Database**: PostgreSQL table with indexes on TechnicalTerm and IsActive
3. **Chat Integration**: Automatic translation in ChatHub based on user's PersonaType
4. **API Endpoints**: Admin-only CRUD operations at `/api/v1/translations/mappings`
5. **Response Format**: Includes both translated and original content for "Show Technical Details" feature


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

- Source: [epics.md - Story 8.2](../planning-artifacts/epics.md)
- Architecture: [architecture.md](../planning-artifacts/architecture.md)
- PRD: [prd.md](../planning-artifacts/prd.md)
