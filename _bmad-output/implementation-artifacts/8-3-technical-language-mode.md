# Story 8.3: Technical Language Mode

Status: ready-for-dev

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a developer (Marcus),
I want full technical details in all system responses,
so that I can make informed implementation decisions.

## Acceptance Criteria

### AC1: Full Technical Details in Agent Responses
**Given** I have Technical persona set  
**When** an agent generates content  
**Then** I receive full technical details including:
- Code snippets with syntax highlighting
- API specifications with HTTP methods and parameters
- Architecture diagrams and component relationships
- Database schemas and entity relationships
- Technology stack versions and configurations

### AC2: Technical Workflow Step Details
**Given** I view a workflow step  
**When** technical details are available  
**Then** I see:
- Data schemas (JSON, XML, database DDL)
- Integration points with endpoint URLs
- Performance considerations and optimization strategies
- Security implications and authentication patterns
- Error handling patterns and status codes

### AC3: Technical Question Responses
**Given** I ask a technical question  
**When** the agent responds  
**Then** the response includes:
- Specific technologies by name (e.g., "PostgreSQL 16", "ASP.NET Core 8.0")
- Version numbers and compatibility requirements
- Configuration examples (code blocks, config files)
- Command-line examples for execution
- Links to official documentation

### AC4: Multi-Persona Collaboration
**Given** I'm in technical mode  
**When** business stakeholders join the workflow  
**Then** they see their persona-appropriate version (translated)  
**And** my view remains technical (untranslated)  
**And** we can collaborate on the same workflow simultaneously

### AC5: Hybrid Mode Context-Aware Adaptation
**Given** I switch to Hybrid mode  
**When** the context changes during conversation  
**Then** responses adapt dynamically:
- Technical details for implementation steps (code, APIs)
- Business language for strategy decisions (goals, outcomes)
- Automatic detection of context type without manual switching

## Tasks / Subtasks

- [ ] **Task 1: Technical Content Enhancement Service** (AC: #1, #2, #3)
  - [ ] Create `ITechnicalContentEnhancerService` interface
  - [ ] Implement `TechnicalContentEnhancerService` with enhancement logic
  - [ ] Add content type detection: Code, API, Architecture, Schema, Configuration
  - [ ] Implement code snippet formatting with language detection
  - [ ] Add version information injection for known technologies
  - [ ] Register service in DI container

- [ ] **Task 2: Technical Content Formatting** (AC: #1, #3)
  - [ ] Create `CodeBlockFormatter` for syntax highlighting metadata
  - [ ] Implement `ApiSpecFormatter` for endpoint documentation
  - [ ] Create `SchemaFormatter` for database and JSON schemas
  - [ ] Add `VersionDetector` for technology stack version identification
  - [ ] Implement markdown enhancement for technical content

- [ ] **Task 3: Persona-Based Response Router** (AC: #4)
  - [ ] Extend `PersonaTranslationMiddleware` to skip translation for Technical persona
  - [ ] Ensure Business persona receives translated content (Story 8.2)
  - [ ] Ensure Technical persona receives raw technical content
  - [ ] Add routing logic based on User.PersonaType from JWT claims
  - [ ] Test multi-user scenarios with different personas

- [ ] **Task 4: Hybrid Mode Context Detection** (AC: #5)
  - [ ] Create `IContextDetectionService` interface
  - [ ] Implement `ContextDetectionService` with ML or rule-based detection
  - [ ] Detect context types: Implementation, Strategy, Question, Documentation
  - [ ] Create context-to-persona mapping rules
  - [ ] Apply appropriate translation based on detected context
  - [ ] Add confidence scoring for context detection

- [ ] **Task 5: Technical Content Storage** (AC: #1, #2)
  - [ ] Extend response DTOs to include `technicalDetails` object
  - [ ] Store rich technical metadata: `{ codeLanguage, versions, apiEndpoints, schemas }`
  - [ ] Add `TechnicalMetadata` entity for persisting enhanced content
  - [ ] Create database migration for technical_metadata table
  - [ ] Link technical metadata to workflow steps and messages

- [ ] **Task 6: Chat Integration for Technical Mode** (AC: #1, #4)
  - [ ] Extend ChatHub to include technical details in messages
  - [ ] Add SignalR method: `SendTechnicalMessage(messageId, content, metadata)`
  - [ ] Update chat client to render code blocks with syntax highlighting
  - [ ] Add expandable sections for API specs and schemas
  - [ ] Test real-time delivery of technical content

- [ ] **Task 7: API Documentation Enhancement** (AC: #3)
  - [ ] Extend OpenAPI documentation with persona-specific examples
  - [ ] Add technical response examples for all endpoints
  - [ ] Document `X-Persona-Type` header usage
  - [ ] Create developer guide for technical mode features
  - [ ] Add code samples in C#, TypeScript for common scenarios

- [ ] **Task 8: Testing** (AC: #1-5)
  - [ ] Unit tests for TechnicalContentEnhancerService
  - [ ] Unit tests for context detection logic
  - [ ] Integration test: Technical persona receives enhanced content
  - [ ] Integration test: Business persona receives translated content
  - [ ] Integration test: Hybrid mode adapts to context changes
  - [ ] Integration test: Multi-persona collaboration in same workflow
  - [ ] Performance test: Content enhancement latency < 50ms

- [ ] **Task 9: Documentation** (AC: #1-5)
  - [ ] Document technical mode features in user guide
  - [ ] Create examples of technical vs business responses
  - [ ] Document Hybrid mode behavior and context detection
  - [ ] Add troubleshooting guide for persona-related issues
  - [ ] Update API reference with persona headers

## Dev Notes

### Architecture Compliance

**Persona Translation Engine (Architecture.md lines 290-296):**
- This story completes the Technical persona implementation (complementing Story 8.2)
- Technical mode provides full technical depth without simplification
- Content enhancement adds rich metadata (versions, schemas, code samples)
- Multi-persona collaboration allows different users to see appropriate views
- Hybrid mode bridges technical and business contexts dynamically

**Integration Points:**
- Extends Epic 8, Story 1 (PersonaType field: Technical, Business, Hybrid)
- Complements Epic 8, Story 2 (Business translation) with Technical enhancement
- Uses existing JWT authentication for persona detection
- Integrates with ChatHub (Epic 3) for real-time technical content delivery
- Prepares for Story 8.4 (In-Session Persona Switching)

**State Persistence (ADR-001):**
- Store technical metadata in PostgreSQL `technical_metadata` table
- Cache enhanced content in Redis for performance (TTL: 1 hour)
- Link metadata to messages and workflow steps via foreign keys

### Project Structure Notes

**Files to Create:**
```
src/bmadServer.ApiService/
  Services/
    TechnicalContent/
      ITechnicalContentEnhancerService.cs          # Interface
      TechnicalContentEnhancerService.cs           # Core service
      CodeBlockFormatter.cs                        # Code formatting
      ApiSpecFormatter.cs                          # API spec formatting
      SchemaFormatter.cs                           # Schema formatting
      VersionDetector.cs                           # Version detection
    Context/
      IContextDetectionService.cs                  # Interface
      ContextDetectionService.cs                   # Context detection
      ContextType.cs                               # Enum: Implementation, Strategy, etc.
  Data/
    Entities/
      TechnicalMetadata.cs                         # EF entity
  DTOs/
    TechnicalDetails.cs                            # Nested DTO
    TechnicalMessageResponse.cs                    # Response DTO
  Migrations/
    {timestamp}_AddTechnicalMetadata.cs            # EF migration
```

**Files to Modify:**
```
src/bmadServer.ApiService/
  Program.cs                                       # Register new services
  Middleware/PersonaTranslationMiddleware.cs       # Skip translation for Technical
  Hubs/ChatHub.cs                                  # Add technical message sending
  DTOs/MessageResponse.cs                          # Add technicalDetails field
```

**Files to Test:**
```
src/bmadServer.Tests/
  Unit/Services/TechnicalContentEnhancerTests.cs   # Service tests
  Unit/Services/ContextDetectionServiceTests.cs    # Context detection tests
  Integration/TechnicalPersonaIntegrationTests.cs  # End-to-end tests
  Integration/MultiPersonaCollaborationTests.cs    # Multi-user tests
  Integration/HybridModeIntegrationTests.cs        # Hybrid mode tests
```

### Technical Requirements

**Technical Content Enhancement Strategy:**
1. **Code Detection:** Identify code blocks by language patterns (C#, SQL, JSON, YAML, Bash)
2. **Version Injection:** Detect technology mentions and add version info (e.g., "PostgreSQL" → "PostgreSQL 16")
3. **API Formatting:** Structure API endpoint info with method, path, params, responses
4. **Schema Formatting:** Format JSON schemas, database DDL in readable markdown tables
5. **Link Injection:** Add references to official docs for mentioned technologies

**Content Type Detection:**
```csharp
public enum ContentType
{
    Code,              // Contains code snippets
    ApiSpecification,  // API endpoint documentation
    Schema,            // Database or JSON schema
    Configuration,     // Config files or settings
    Architecture,      // System design and diagrams
    General            // Other technical content
}
```

**Technical Metadata Structure (PostgreSQL JSONB):**
```json
{
  "contentType": "Code",
  "language": "csharp",
  "technologies": [
    { "name": "ASP.NET Core", "version": "8.0" },
    { "name": "Entity Framework Core", "version": "8.0" }
  ],
  "apiEndpoints": [
    {
      "method": "POST",
      "path": "/api/v1/users/me/persona",
      "description": "Update user persona preference",
      "parameters": [{"name": "personaType", "type": "string", "required": true}]
    }
  ],
  "codeSnippets": [
    {
      "language": "csharp",
      "code": "builder.Services.AddScoped<IPersonaService, PersonaService>();",
      "description": "Service registration example"
    }
  ],
  "schemas": [
    {
      "type": "database",
      "name": "users",
      "fields": [
        {"name": "persona_type", "type": "VARCHAR(20)", "nullable": false}
      ]
    }
  ]
}
```

**Context Detection Rules (Hybrid Mode):**
- **Implementation Context:** Keywords: "implement", "code", "build", "create", "develop" → Technical
- **Strategy Context:** Keywords: "why", "business", "goals", "users", "value" → Business
- **Question Context:** Analyze question complexity and technical depth → Adaptive
- **Documentation Context:** Default to requester's persona preference

**Middleware Enhancement:**
```csharp
// In PersonaTranslationMiddleware
if (userPersona == PersonaType.Technical)
{
    // Skip translation - pass through raw technical content
    await _next(context);
    return;
}
else if (userPersona == PersonaType.Business)
{
    // Apply translation (Story 8.2 logic)
    await TranslateToBusinessLanguage(context);
}
else if (userPersona == PersonaType.Hybrid)
{
    // Detect context and adapt
    var contextType = await _contextDetection.DetectContext(requestContent);
    if (contextType == ContextType.Implementation)
    {
        // Technical content - no translation
        await _next(context);
    }
    else
    {
        // Business context - translate
        await TranslateToBusinessLanguage(context);
    }
}
```

### Library & Framework Requirements

**Existing Stack (No New Major Dependencies):**
- .NET 8.0 / C# 12
- ASP.NET Core 8.0
- Entity Framework Core 8.0 with Npgsql
- PostgreSQL 16 (technical_metadata table)
- Redis (for caching enhanced content)
- SignalR (for real-time technical message delivery)

**Potential New NuGet Packages:**
- **Markdig** - Markdown parsing and rendering (for code block extraction)
- **System.Text.RegularExpressions** - Pattern matching (built-in)
- Consider lightweight syntax highlighting library if needed

**Check Existing Packages:**
- Verify if Markdown rendering is already present
- Check for existing code formatting utilities in Workflow services

### File Structure Requirements

**Follow existing conventions:**
- Services in `Services/` with interface + implementation pattern
- Nested folders for logical grouping (`TechnicalContent/`, `Context/`)
- Entities in `Data/Entities/` with EF navigation properties
- DTOs in `DTOs/` with XML documentation
- Middleware modifications follow existing patterns

### Testing Requirements

**Code Coverage:**
- Maintain existing coverage standards
- Test content type detection with various inputs
- Test version injection for all major technologies
- Test code block formatting with multiple languages
- Test context detection accuracy (aim for >90%)
- Test multi-persona scenarios with concurrent users
- Test Hybrid mode context switching

**Test Scenarios:**
- Technical persona user receives enhanced code snippets
- Business persona user receives translated responses (Story 8.2)
- Hybrid persona user receives technical content for implementation questions
- Hybrid persona user receives business content for strategy questions
- Multiple users with different personas collaborate on same workflow
- Context detection correctly identifies implementation vs strategy
- Technical metadata is stored and retrievable

**Performance Testing:**
- Content enhancement < 50ms per message
- Context detection < 20ms per request
- Middleware overhead < 30ms per request
- No degradation with concurrent multi-persona users

### Previous Story Intelligence

**Story 8.1 (Persona Profile Configuration):**
- User entity includes `PersonaType` property (Business, Technical, Hybrid)
- `PersonaType` is accessible via JWT claims
- GET `/api/v1/users/me` returns persona information
- PATCH `/api/v1/users/me/persona` allows persona updates
- Default persona is `Hybrid`

**Story 8.2 (Business Language Translation):**
- `PersonaTranslationService` translates technical → business language
- `PersonaTranslationMiddleware` intercepts responses
- Translation only applies to Business persona users
- Original content stored in cache for "Show Details" feature
- Translation rules stored in `translation_rules` table
- Redis caching for performance

**Key Learnings from Story 8.2:**
- Middleware approach works well for persona-based content adaptation
- Check `User.PersonaType` from JWT claims in middleware
- Skip translation for Technical persona (pass-through)
- Use Redis caching aggressively for performance
- Store metadata in PostgreSQL for rich querying
- Integration tests use InMemory database with TestAuthHandler

**Files to Reference:**
- `src/bmadServer.ApiService/Models/PersonaType.cs` - Enum definition
- `src/bmadServer.ApiService/Data/Entities/User.cs` - User.PersonaType property
- `src/bmadServer.ApiService/Middleware/PersonaTranslationMiddleware.cs` - Middleware pattern
- `src/bmadServer.ApiService/Services/Translation/PersonaTranslationService.cs` - Service pattern
- `src/bmadServer.ApiService/Data/ApplicationDbContext.cs` - Enum conversion and EF config

### Git Intelligence Summary

**Recent Patterns from Codebase:**
- Services registered in Program.cs with `builder.Services.AddScoped<IService, Service>()`
- Middleware registered with `app.UseMiddleware<MiddlewareClass>()`
- EF migrations created with: `dotnet ef migrations add MigrationName --project src/bmadServer.ApiService`
- Integration tests follow pattern: `{Feature}IntegrationTests.cs`
- DTOs use XML documentation for OpenAPI generation

**Existing Infrastructure to Leverage:**
- ChatHub for real-time technical content delivery
- JWT authentication for persona detection
- PersonaTranslationMiddleware for routing (modify to support Technical)
- Redis caching for enhanced content
- PostgreSQL JSONB for flexible metadata storage

### Latest Tech Information

**Markdig (Markdown Library) for .NET:**
- Version: 0.33+ (latest stable)
- Use for parsing markdown and extracting code blocks
- Pattern:
  ```csharp
  var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
  var document = Markdown.Parse(text, pipeline);
  // Extract code blocks, inline code, etc.
  ```

**Code Block Language Detection:**
- Use file extension patterns: `.cs` → C#, `.sql` → SQL, `.json` → JSON
- Regex patterns for syntax: `using` → C#, `SELECT` → SQL, `{` at start → JSON
- Default to `text` if language cannot be determined

**ASP.NET Core 8.0 Middleware Chaining:**
- Order: Auth → Translation → Routing → Endpoints
- Technical persona: Skip translation middleware (early return)
- Business persona: Apply translation (Story 8.2)
- Hybrid persona: Detect context, then decide

**EF Core 8.0 JSONB Best Practices:**
- Use `HasColumnType("jsonb")` for metadata columns
- Index JSONB fields for query performance: `.HasIndex(m => m.Metadata).HasMethod("gin")`
- Query JSONB: `.Where(m => EF.Functions.JsonContains(m.Metadata, searchCriteria))`

**SignalR Message Structure for Technical Content:**
```csharp
await Clients.User(userId).SendAsync("ReceiveTechnicalMessage", new
{
    MessageId = messageId,
    Content = rawTechnicalContent,
    TechnicalDetails = new
    {
        CodeSnippets = codeBlocks,
        ApiEndpoints = apiSpecs,
        Versions = detectedVersions,
        Schemas = schemas
    },
    Timestamp = DateTime.UtcNow
});
```

### Project Context Reference

**Architecture Document:** `_bmad-output/planning-artifacts/architecture.md`
- Section: Persona Translation Engine (lines 290-296)
- Technical persona provides full technical depth

**PRD Document:** `_bmad-output/planning-artifacts/prd.md`
- FR13: Users can interact using technical language and receive technical details
- FR14: System adapts responses to selected persona profile
- FR15: Users can switch persona mode within a session (Hybrid mode)

**Epic Details:** `_bmad-output/planning-artifacts/epics.md`
- Epic 8: Persona Translation & Language Adaptation (lines 2245-2352)
- Story 8.3: Technical Language Mode (lines 2322-2352)
- BDD acceptance criteria provided

**Previous Stories:**
- `8-1-persona-profile-configuration.md` - PersonaType infrastructure
- `8-2-business-language-translation.md` - Translation service and middleware

### Critical Implementation Notes

**🚨 PREVENT COMMON MISTAKES:**

1. **Don't Over-Enhance Technical Content:**
   - Keep original content intact - enhancement is metadata overlay
   - Don't modify code snippets or API specs
   - Don't inject versions that aren't accurate
   - Preserve formatting and whitespace in code blocks

2. **Performance is Critical:**
   - Content enhancement must not block responses
   - Use caching for repeated enhancements
   - Context detection should be fast (< 20ms)
   - Async processing for non-critical enhancements

3. **Multi-Persona Collaboration:**
   - Each user sees their persona-appropriate view
   - Same workflow, different perspectives
   - Don't cache by workflow ID - cache by (workflow ID + persona)
   - Test concurrent access with different personas

4. **Hybrid Mode Context Detection:**
   - Start with simple rule-based detection
   - Don't over-engineer ML if rules work
   - Default to user's primary persona if uncertain
   - Allow manual override if detection is wrong

5. **Technical Metadata Quality:**
   - Only add metadata when confident (>80% confidence)
   - Don't hallucinate version numbers
   - Reference official docs when available
   - Mark uncertain data with confidence score

6. **Testing Technical Features:**
   - Test with real-world technical content
   - Verify code blocks render correctly
   - Ensure API specs are accurate
   - Test edge cases: malformed code, unknown languages

7. **Middleware Modification:**
   - Don't break existing Business translation (Story 8.2)
   - Ensure Technical persona bypasses translation
   - Hybrid mode must make correct decisions
   - Log context detection decisions for debugging

8. **Security Considerations:**
   - Don't expose sensitive technical details inappropriately
   - Validate persona from JWT (don't trust client headers)
   - Sanitize code snippets before storing (XSS prevention)
   - Limit metadata size to prevent abuse

### Example Technical Content Enhancement

**Original Input:**
```
The user registration endpoint is ready. It uses Entity Framework to store data.
```

**Enhanced for Technical Persona:**
```markdown
## User Registration Endpoint

**API Specification:**
- **Method:** POST
- **Path:** `/api/v1/auth/register`
- **Authentication:** None (public endpoint)

**Request Body:**
```json
{
  "email": "user@example.com",
  "password": "SecurePass123!",
  "displayName": "John Doe"
}
```

**Response:** 201 Created
```json
{
  "id": "uuid",
  "email": "user@example.com",
  "displayName": "John Doe",
  "personaType": "Hybrid"
}
```

**Technology Stack:**
- ASP.NET Core 8.0 (Web API)
- Entity Framework Core 8.0 (ORM)
- PostgreSQL 16 (Database)

**Database Schema:**
```sql
CREATE TABLE users (
  id UUID PRIMARY KEY,
  email VARCHAR(255) UNIQUE NOT NULL,
  password_hash VARCHAR(255) NOT NULL,
  display_name VARCHAR(100),
  persona_type VARCHAR(20) DEFAULT 'Hybrid',
  created_at TIMESTAMP DEFAULT NOW()
);
```

**Code Example:**
```csharp
[HttpPost("register")]
[ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
public async Task<IActionResult> Register([FromBody] RegisterRequest request)
{
    var user = new User
    {
        Email = request.Email,
        PasswordHash = _passwordHasher.Hash(request.Password),
        DisplayName = request.DisplayName,
        PersonaType = PersonaType.Hybrid
    };
    
    await _context.Users.AddAsync(user);
    await _context.SaveChangesAsync();
    
    return CreatedAtAction(nameof(GetCurrentUser), new UserResponse(user));
}
```

**References:**
- [ASP.NET Core Web API Documentation](https://learn.microsoft.com/en-us/aspnet/core/web-api/)
- [Entity Framework Core Documentation](https://learn.microsoft.com/en-us/ef/core/)
```

### Hybrid Mode Context Examples

**Implementation Question (→ Technical Response):**
User: "How do I implement the persona update endpoint?"

Context Detection: Keywords "implement", "endpoint" → Implementation context → Technical response
Response: Full code example, API spec, testing guidance

**Strategy Question (→ Business Response):**
User: "Why do we need different personas?"

Context Detection: Keyword "why" → Strategy context → Business response
Response: "Different team members have different expertise. Business personas get simplified explanations, while technical personas get full implementation details."

**Mixed Context (→ Hybrid Response):**
User: "What's the performance impact of persona translation?"

Context Detection: Technical ("performance") + Business concern ("impact") → Hybrid response
Response: "Translation adds ~20-50ms per request. For business users, this is acceptable for clarity. Technical users bypass translation entirely for zero overhead."

### References

- [Source: prd.md#FR13-FR15] - Technical language and persona requirements
- [Source: architecture.md#Persona Translation Engine (lines 290-296)] - Architecture for persona system
- [Source: epics.md#Story 8.3 (lines 2322-2352)] - Detailed BDD acceptance criteria
- [Source: 8-1-persona-profile-configuration.md] - PersonaType infrastructure
- [Source: 8-2-business-language-translation.md] - Translation service and middleware patterns
- [Source: src/bmadServer.ApiService/Data/Entities/User.cs] - User entity with PersonaType
- [Source: src/bmadServer.ApiService/Middleware/PersonaTranslationMiddleware.cs] - Middleware to extend
- [Source: src/bmadServer.ApiService/Hubs/ChatHub.cs] - Chat integration for technical messages

## Dev Agent Record

### Agent Model Used

_To be filled by Dev Agent_

### Debug Log References

_To be filled by Dev Agent_

### Completion Notes List

_To be filled by Dev Agent_

### File List

_To be filled by Dev Agent_
