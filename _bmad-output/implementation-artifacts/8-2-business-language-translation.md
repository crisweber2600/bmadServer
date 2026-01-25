# Story 8.2: Business Language Translation

Status: ready-for-dev

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a non-technical user (Sarah),
I want technical outputs translated to business language,
so that I can understand and make decisions.

## Acceptance Criteria

### AC1: Automatic Translation for Business Persona
**Given** I have Business persona set  
**When** an agent generates technical content  
**Then** the response is automatically translated to business terms  
**And** technical jargon is replaced with plain language

### AC2: Error Message Translation
**Given** a technical error occurs  
**When** I see the error message  
**Then** it explains the issue in business terms: "We couldn't save your changes because another team member is editing" (not "409 Conflict: optimistic concurrency violation")

### AC3: Architecture Decision Translation
**Given** architecture decisions are presented  
**When** I view the recommendations  
**Then** I see business impact: "This choice means faster loading times for users" (not "implementing CDN caching layer")

### AC4: Technical Details Expansion
**Given** I need technical details  
**When** I click "Show Technical Details"  
**Then** I can expand to see the original technical content  
**And** this doesn't change my persona setting

### AC5: Translation Quality Feedback
**Given** translation quality is measured  
**When** I provide feedback on clarity  
**Then** the system logs my rating and improves translations over time

## Tasks / Subtasks

- [ ] **Task 1: Translation Service Infrastructure** (AC: #1, #2, #3)
  - [ ] Create `IPersonaTranslationService` interface in Services folder
  - [ ] Implement `PersonaTranslationService` with core translation logic
  - [ ] Add translation context enum: `ErrorMessage`, `ArchitectureDecision`, `AgentResponse`, `General`
  - [ ] Register service in DI container (Program.cs)
  - [ ] Add translation configuration to appsettings.json

- [ ] **Task 2: Translation Rules Engine** (AC: #1, #2, #3)
  - [ ] Create `TranslationRule` entity with pattern matching
  - [ ] Implement technical-to-business dictionary/mapping
  - [ ] Add context-aware rule selection logic
  - [ ] Create seed data for common technical terms
  - [ ] Database migration for translation_rules table

- [ ] **Task 3: LLM-Based Translation Fallback** (AC: #1, #3)
  - [ ] Integrate with existing LLM service for complex translations
  - [ ] Create translation prompt templates
  - [ ] Implement caching for repeated translations (Redis)
  - [ ] Add confidence scoring for translation quality
  - [ ] Fallback to original text if translation confidence is low

- [ ] **Task 4: Middleware for Response Interception** (AC: #1)
  - [ ] Create `PersonaTranslationMiddleware` to intercept API responses
  - [ ] Extract user persona from JWT claims or session
  - [ ] Apply translation only for Business persona
  - [ ] Preserve original content in `x-original-content` header
  - [ ] Add middleware to request pipeline

- [ ] **Task 5: Error Message Translation** (AC: #2)
  - [ ] Create custom `ProblemDetailsFactory` override
  - [ ] Translate exception messages based on persona
  - [ ] Map common HTTP status codes to business-friendly messages
  - [ ] Add examples: 409 → "Another team member is editing", 401 → "Please sign in again"
  - [ ] Update existing error handling to use new factory

- [ ] **Task 6: Technical Details Expansion API** (AC: #4)
  - [ ] Add `originalContent` field to response DTOs
  - [ ] Create `TranslatedResponse<T>` wrapper DTO
  - [ ] Implement endpoint: `GET /api/v1/translations/{messageId}/original`
  - [ ] Store original content in session cache (15 min TTL)
  - [ ] Return both translated and original in dual-mode responses

- [ ] **Task 7: Translation Feedback System** (AC: #5)
  - [ ] Create `TranslationFeedback` entity (messageId, userId, rating, comment)
  - [ ] Implement endpoint: `POST /api/v1/translations/{messageId}/feedback`
  - [ ] Store feedback in database for future ML training
  - [ ] Add analytics dashboard query for feedback trends
  - [ ] Database migration for translation_feedback table

- [ ] **Task 8: Chat Integration** (AC: #1, #4)
  - [ ] Extend ChatHub to apply translation to outbound messages
  - [ ] Add SignalR method: `SendTranslatedMessage(messageId, translated, hasOriginal)`
  - [ ] Update chat client to show "Show Technical Details" button when applicable
  - [ ] Implement expand/collapse UI for original content
  - [ ] Add unit tests for translation in real-time context

- [ ] **Task 9: Testing** (AC: #1-5)
  - [ ] Unit tests for PersonaTranslationService with various inputs
  - [ ] Unit tests for translation rule matching
  - [ ] Integration test: Business persona receives translated responses
  - [ ] Integration test: Technical persona receives untranslated responses
  - [ ] Integration test: Error messages are translated correctly
  - [ ] Integration test: Original content is retrievable
  - [ ] Integration test: Feedback is stored and queryable
  - [ ] Performance test: Translation latency < 50ms for rule-based

- [ ] **Task 10: Documentation** (AC: #1-5)
  - [ ] OpenAPI documentation for translation endpoints
  - [ ] Document translation rules and customization
  - [ ] Add examples of before/after translations
  - [ ] Create admin guide for managing translation rules
  - [ ] Update user guide with "Show Technical Details" feature

## Dev Notes

### Architecture Compliance

**Persona Translation Engine (Architecture.md lines 290-296):**
- This story implements the core translation functionality
- Business ↔ Technical language translation service
- Context-aware response adaptation
- Content classification by message type (error, decision, response)
- Maintains semantic equivalence across translations
- Role-based access to technical depth (expandable details)

**Integration Points:**
- Extends Epic 8, Story 1 (PersonaType field in User entity)
- Uses existing JWT authentication for persona detection
- Integrates with ChatHub (Epic 3) for real-time translation
- Prepares foundation for Story 8.3 (Technical Language Mode)

**State Persistence (ADR-001):**
- Store translation rules in PostgreSQL `translation_rules` table
- Cache translations in Redis for performance (TTL: 1 hour)
- Store feedback in `translation_feedback` table for analytics
- Store original content in session cache (TTL: 15 minutes)

### Project Structure Notes

**Files to Create:**
```
src/bmadServer.ApiService/
  Services/
    Translation/
      IPersonaTranslationService.cs                # Interface
      PersonaTranslationService.cs                 # Core service
      TranslationContext.cs                        # Enum for context types
      TranslationRule.cs                           # Rule model
      TranslationCache.cs                          # Redis caching layer
  Middleware/
    PersonaTranslationMiddleware.cs                # Response interceptor
  Data/
    Entities/
      TranslationRule.cs                           # EF entity
      TranslationFeedback.cs                       # Feedback entity
  DTOs/
    TranslatedResponse.cs                          # Wrapper DTO
    TranslationFeedbackRequest.cs                  # Feedback DTO
  Factories/
    PersonaAwareProblemDetailsFactory.cs           # Custom error factory
  Migrations/
    {timestamp}_AddTranslationTables.cs            # EF migration
```

**Files to Modify:**
```
src/bmadServer.ApiService/
  Program.cs                                       # Register services, middleware
  appsettings.json                                 # Translation config
  Hubs/ChatHub.cs                                  # Add translation to messages
  DTOs/UserResponse.cs                             # Already has PersonaType (Story 8.1)
```

**Files to Test:**
```
src/bmadServer.Tests/
  Unit/Services/PersonaTranslationServiceTests.cs  # Service tests
  Unit/Middleware/TranslationMiddlewareTests.cs    # Middleware tests
  Integration/TranslationIntegrationTests.cs       # End-to-end tests
  Integration/ChatTranslationTests.cs              # Real-time chat tests
```

### Technical Requirements

**Translation Strategy - Hybrid Approach:**
1. **Rule-Based (Fast Path):** Pre-defined mappings for common terms (< 10ms)
2. **LLM-Based (Slow Path):** Complex translations using existing LLM service (< 500ms)
3. **Caching:** Redis cache for repeated translations (< 5ms)

**Translation Rules Format (PostgreSQL JSONB):**
```json
{
  "pattern": "409 Conflict: optimistic concurrency violation",
  "translation": "We couldn't save your changes because another team member is editing",
  "context": "ErrorMessage",
  "confidence": 1.0
}
```

**Middleware Design:**
- Intercept responses with status code 2xx, 4xx, 5xx
- Check user persona from `HttpContext.User.Claims`
- Apply translation only if `PersonaType == "Business"`
- Add `X-Original-Content-Id` header with cache key
- Preserve original HTTP status code

**LLM Integration:**
- Use existing LLM service infrastructure (from Workflow/Agent system)
- Translation prompt template:
  ```
  Translate this technical message to business language:
  "{technical_message}"
  
  Context: {context_type}
  Target Audience: Non-technical business user
  
  Requirements:
  - Use plain language, avoid jargon
  - Maintain semantic accuracy
  - Keep the message concise
  - Focus on impact, not implementation
  ```

**Redis Caching Strategy:**
- Key format: `translation:{hash(original_text)}:{persona}`
- TTL: 1 hour for translations
- TTL: 15 minutes for original content storage
- Invalidate on rule updates

### Library & Framework Requirements

**Existing Stack (No New Major Dependencies):**
- .NET 8.0 / C# 12
- ASP.NET Core 8.0 (Middleware)
- Entity Framework Core 8.0 with Npgsql
- PostgreSQL 16 (translation_rules, translation_feedback tables)
- Redis (already in stack for caching - verify configuration)
- SignalR (already exists for ChatHub)

**Potential New NuGet Packages:**
- `StackExchange.Redis` - If not already present for Redis caching
- `System.Text.RegularExpressions` - For pattern matching (built-in)

**Check Existing Redis Configuration:**
- Verify Redis connection string in appsettings.json
- Verify IDistributedCache registration in Program.cs
- If missing, add Redis to docker-compose.yml

### File Structure Requirements

**Follow existing conventions:**
- Services in `Services/` with interface + implementation pattern
- Middleware in `Middleware/` directory
- Register middleware in Program.cs after authentication, before routing
- Entities in `Data/Entities/` with EF navigation properties
- DTOs in `DTOs/` with XML documentation
- Factories in `Factories/` or `Infrastructure/` directory

### Testing Requirements

**Code Coverage:**
- Maintain existing coverage standards
- Test rule-based translation with various inputs
- Test LLM fallback behavior
- Test caching hit/miss scenarios
- Test middleware integration with different personas
- Test error message translation for common HTTP codes
- Test feedback storage and retrieval

**Test Scenarios:**
- Business persona user receives translated chat messages
- Technical persona user receives original technical messages
- Hybrid persona user receives context-appropriate messages
- Translation cache improves performance on repeated messages
- Original content is retrievable via API
- Feedback is stored with correct userId and messageId
- Translation fails gracefully if LLM is unavailable

**Performance Testing:**
- Rule-based translation < 10ms
- LLM-based translation < 500ms (acceptable for real-time chat)
- Cached translation < 5ms
- Middleware overhead < 20ms per request

### Previous Story Intelligence

**Story 8.1 (Persona Profile Configuration):**
- User entity now includes `PersonaType` property (Business, Technical, Hybrid)
- `PersonaType` is accessible via JWT claims
- GET `/api/v1/users/me` returns persona information
- PATCH `/api/v1/users/me/persona` allows persona updates
- Default persona is `Hybrid`

**Key Learnings:**
- PersonaType enum uses string conversion (not integers)
- Enum configured with `.HasConversion<string>()` in ApplicationDbContext
- JWT claims include user ID, can be extended for persona
- Integration tests use InMemory database with TestAuthHandler

**Story 8.1 Files to Reference:**
- `src/bmadServer.ApiService/Models/PersonaType.cs` - Enum definition
- `src/bmadServer.ApiService/Data/Entities/User.cs` - User.PersonaType property
- `src/bmadServer.ApiService/Controllers/UsersController.cs` - Persona endpoints
- `src/bmadServer.ApiService/Data/ApplicationDbContext.cs` - Enum conversion

### Git Intelligence Summary

**Recent Patterns from Codebase:**
- Services registered in Program.cs with `builder.Services.AddScoped<IService, Service>()`
- Middleware registered with `app.UseMiddleware<MiddlewareClass>()`
- EF migrations created with: `dotnet ef migrations add MigrationName --project src/bmadServer.ApiService`
- Integration tests follow pattern: `{Feature}IntegrationTests.cs`
- DTOs use XML documentation for OpenAPI generation

**Existing Chat Infrastructure (Epic 3):**
- ChatHub exists at `src/bmadServer.ApiService/Hubs/ChatHub.cs`
- SignalR configured in Program.cs
- Real-time message streaming already implemented
- Chat history and session recovery patterns established

**Existing LLM Service (Epic 4):**
- Workflow orchestration includes agent routing
- LLM integration likely exists in `Services/` directory
- Check for existing `ILlmService` or similar abstraction

### Latest Tech Information

**ASP.NET Core 8.0 Middleware Best Practices:**
- Middleware order matters: Auth → Translation → Routing → Endpoints
- Use `HttpContext.Response.OnStarting()` to intercept before headers are sent
- For response body interception, buffer the response stream
- Example pattern:
  ```csharp
  var originalBody = context.Response.Body;
  using var newBody = new MemoryStream();
  context.Response.Body = newBody;
  await _next(context);
  newBody.Seek(0, SeekOrigin.Begin);
  var responseBody = await new StreamReader(newBody).ReadToEndAsync();
  // Translate responseBody here
  newBody.Seek(0, SeekOrigin.Begin);
  await newBody.CopyToAsync(originalBody);
  ```

**EF Core 8.0 JSONB with PostgreSQL:**
- Use `HasColumnType("jsonb")` for JSON columns
- Pattern matching on JSONB: `.Where(r => EF.Functions.JsonContains(r.Pattern, searchTerm))`
- Index JSONB fields for performance: `.HasIndex(r => r.Pattern).HasMethod("gin")`

**Redis with .NET 8.0:**
- Use `IDistributedCache` abstraction for Redis
- Register with: `builder.Services.AddStackExchangeRedisCache(options => { options.Configuration = "localhost:6379"; })`
- Key patterns: Use consistent prefixes and delimiters
- Consider compression for large translations

**SignalR Translation Pattern:**
- Don't translate inside hub method (synchronous constraint)
- Translate in background service or before sending
- Send dual payload: `{ translated: "...", hasOriginal: true, messageId: "..." }`
- Client decides whether to show "Show Details" button

### Project Context Reference

**Architecture Document:** `_bmad-output/planning-artifacts/architecture.md`
- Section: Persona Translation Engine (lines 290-296)
- Section: State Persistence Layer (lines 297-302)

**PRD Document:** `_bmad-output/planning-artifacts/prd.md`
- Requirements: FR12-FR15 (Personas & Communication, lines 320-325)
- FR12: Business language interaction
- FR14: Adapt responses to persona profile

**Epic Details:** `_bmad-output/planning-artifacts/epics.md`
- Epic 8: Persona Translation & Language Adaptation (lines 2245-2320)
- Story 8.2: Business Language Translation (lines 2289-2320)
- BDD acceptance criteria provided

**Previous Story:** `_bmad-output/implementation-artifacts/8-1-persona-profile-configuration.md`
- PersonaType enum and User entity changes
- API endpoints for persona management
- Integration test patterns

### Critical Implementation Notes

**🚨 PREVENT COMMON MISTAKES:**

1. **Don't Translate Everything Blindly:**
   - Only translate for `PersonaType == "Business"`
   - Preserve technical terms when necessary (e.g., HTTP method names in API docs)
   - Don't translate code snippets, JSON, or structured data

2. **Performance is Critical:**
   - Translation must not block chat responses
   - Use caching aggressively
   - Rule-based first, LLM fallback
   - Consider async translation for non-critical messages

3. **Preserve Original Content:**
   - Always store original in cache for "Show Details" feature
   - Include messageId in response for retrieval
   - TTL must be long enough for user to click "Show Details"

4. **Context Matters:**
   - Error messages need different translation than architecture decisions
   - Use `TranslationContext` enum to guide translation
   - Chat messages vs API responses may need different handling

5. **Testing Translation Quality:**
   - Maintain semantic equivalence (meaning doesn't change)
   - Business users should not be confused
   - Technical users can still access details
   - Edge cases: Empty strings, special characters, HTML

6. **Security Considerations:**
   - Don't expose sensitive technical details in translations
   - Sanitize user feedback before storing
   - Validate messageId in feedback API (prevent injection)

7. **Gradual Rollout:**
   - Start with rule-based translations (high confidence)
   - Add LLM translations iteratively
   - Monitor feedback to improve rules
   - Feature flag if needed for safe rollout

### Example Translations (Seed Data)

**Error Messages:**
- `401 Unauthorized` → "Please sign in to continue"
- `403 Forbidden` → "You don't have permission to access this"
- `404 Not Found` → "We couldn't find what you're looking for"
- `409 Conflict: optimistic concurrency violation` → "Another team member is editing this. Please refresh and try again."
- `500 Internal Server Error` → "Something went wrong. Our team has been notified."

**Technical Terms:**
- "API endpoint" → "System connection point"
- "Database migration" → "Data structure update"
- "JWT token" → "Session credential"
- "WebSocket connection" → "Real-time communication channel"
- "Rate limiting" → "Usage protection"
- "CDN caching" → "Fast content delivery"
- "Microservices" → "Independent system components"
- "Container orchestration" → "Automated deployment management"

**Architecture Decisions:**
- "Implementing CDN caching layer" → "Setting up faster loading times for users worldwide"
- "Adding Redis for session state" → "Improving system performance and reliability"
- "Using PostgreSQL JSONB" → "Storing flexible data for better adaptability"
- "Event sourcing pattern" → "Maintaining complete history of changes"

### References

- [Source: architecture.md#Persona Translation Engine (lines 290-296)] - Core architecture for translation
- [Source: prd.md#FR12-FR15 (lines 320-325)] - Persona communication requirements
- [Source: epics.md#Story 8.2 (lines 2289-2320)] - Detailed BDD acceptance criteria
- [Source: 8-1-persona-profile-configuration.md] - Previous story with PersonaType implementation
- [Source: src/bmadServer.ApiService/Data/Entities/User.cs] - User entity with PersonaType
- [Source: src/bmadServer.ApiService/Hubs/ChatHub.cs] - Existing chat infrastructure for integration

## Dev Agent Record

### Agent Model Used

_To be filled by Dev Agent_

### Debug Log References

_To be filled by Dev Agent_

### Completion Notes List

_To be filled by Dev Agent_

### File List

_To be filled by Dev Agent_
