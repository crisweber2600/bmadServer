# Story 8.1: Persona Profile Configuration

Status: ready-for-dev

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a user (Sarah),
I want to set my communication preference (Business, Technical, or Hybrid persona),
so that the system speaks to me in language I understand.

## Acceptance Criteria

### AC1: Persona Options Available
**Given** I am setting up my profile  
**When** I access persona settings  
**Then** I see three options:
- **Business (non-technical):** "The system will validate your product requirements"
- **Technical (developer):** "The API will execute JSON schema validation on the PRD payload"
- **Hybrid (adaptive):** Context-aware responses that adapt based on conversation

### AC2: Persona Selection and Persistence
**Given** I select Business persona  
**When** I save my preference  
**Then** my user profile includes `personaType: "business"`  
**And** the setting persists across sessions

### AC3: Persona Descriptions with Examples
**Given** I view persona descriptions  
**When** I hover over each option  
**Then** I see concrete examples of how responses will differ:
- Business: "The system will validate your product requirements"
- Technical: "The API will execute JSON schema validation on the PRD payload"
- Hybrid: "Adapts language based on context and technical depth required"

### AC4: Default Persona Behavior
**Given** I don't explicitly set a persona  
**When** I start using the system  
**Then** the default is Hybrid (adaptive based on context)

### AC5: User Profile API Returns Persona
**Given** I query user profile  
**When** I send GET `/api/v1/users/me`  
**Then** the response includes:
```json
{
  "id": "guid",
  "email": "sarah@example.com",
  "displayName": "Sarah",
  "createdAt": "2026-01-25T...",
  "personaType": "business",
  "preferredLanguage": "en"
}
```

## Tasks / Subtasks

- [ ] **Task 1: Database Schema** (AC: #2, #5)
  - [ ] Add `PersonaType` enum: `Business`, `Technical`, `Hybrid` with string conversion
  - [ ] Add `PersonaType` property to `User` entity (required, default: `Hybrid`)
  - [ ] Add `PreferredLanguage` property to `User` entity (optional, default: `"en"`)
  - [ ] Create EF migration: `AddPersonaProfileFields`
  - [ ] Apply migration to database

- [ ] **Task 2: Update API DTOs** (AC: #5)
  - [ ] Add `PersonaType` and `PreferredLanguage` to `UserResponse` DTO
  - [ ] Update XML documentation for new fields
  - [ ] Ensure proper enum serialization (string values, not integers)

- [ ] **Task 3: Update UsersController** (AC: #5)
  - [ ] Modify `GetCurrentUser()` to include persona fields in response
  - [ ] Add endpoint `PATCH /api/v1/users/me/persona` for updating persona
  - [ ] Validate persona type in request (Business, Technical, Hybrid)
  - [ ] Add proper OpenAPI documentation

- [ ] **Task 4: Update Registration Flow** (AC: #4)
  - [ ] Modify `AuthController.Register()` to set default `personaType: Hybrid`
  - [ ] Ensure `preferredLanguage: "en"` is set by default
  - [ ] Update integration tests for registration

- [ ] **Task 5: Testing** (AC: #1-5)
  - [ ] Unit tests for `PersonaType` enum conversions
  - [ ] Unit tests for User entity with persona fields
  - [ ] Integration test: GET `/api/v1/users/me` returns persona fields
  - [ ] Integration test: PATCH `/api/v1/users/me/persona` updates persona
  - [ ] Integration test: New user has default `Hybrid` persona
  - [ ] Integration test: Persona persists across sessions (login/logout)

- [ ] **Task 6: Documentation** (AC: #1, #3)
  - [ ] Update OpenAPI/Swagger with persona endpoints
  - [ ] Add examples showing different persona types in responses
  - [ ] Document persona enum values and their meanings

## Dev Notes

### Architecture Compliance

**Database Strategy (ADR-001):**
- PostgreSQL as primary data store for user profiles
- Add columns to existing `users` table: `persona_type` (VARCHAR, default 'Hybrid'), `preferred_language` (VARCHAR, default 'en')
- Use EF Core migrations for schema changes
- Maintain backward compatibility: existing users default to Hybrid

**API Patterns:**
- RESTful endpoint: `GET /api/v1/users/me` (already exists, extend response)
- New endpoint: `PATCH /api/v1/users/me/persona` for partial profile updates
- Return RFC 7807 ProblemDetails for validation errors
- Follow existing authentication patterns (JWT + [Authorize] attribute)

**Enum Handling:**
- Use string conversion for `PersonaType` enum (not integers) for API clarity
- Follow pattern from `WorkflowStatus` enum in existing codebase
- Configure with `.HasConversion<string>()` in EF Core

### Project Structure Notes

**Files to Create:**
```
src/bmadServer.ApiService/
  Models/
    PersonaType.cs                           # New enum
  DTOs/
    UpdatePersonaRequest.cs                  # New DTO for PATCH
  Migrations/
    {timestamp}_AddPersonaProfileFields.cs   # EF migration
```

**Files to Modify:**
```
src/bmadServer.ApiService/
  Data/Entities/User.cs                      # Add PersonaType, PreferredLanguage
  Data/ApplicationDbContext.cs               # Configure persona_type enum conversion
  DTOs/UserResponse.cs                       # Add persona fields
  Controllers/UsersController.cs             # Add PATCH endpoint, update GET
  Controllers/AuthController.cs              # Set defaults in Register()
```

**Files to Test:**
```
src/bmadServer.Tests/
  Unit/UserEntityTests.cs                    # Test User entity defaults
  Integration/UsersMeIntegrationTests.cs     # Extend existing tests
  Integration/PersonaUpdateTests.cs          # New test file
```

### Technical Requirements

**Entity Framework Core:**
- Use `.HasConversion<string>()` for PersonaType enum
- Default value: `PersonaType.Hybrid`
- Migration command: `dotnet ef migrations add AddPersonaProfileFields --project src/bmadServer.ApiService`

**ASP.NET Core:**
- Controller: `UsersController` already exists at `Controllers/UsersController.cs`
- Authentication: Use existing `[Authorize]` attribute
- Model validation: Use `[Required]` and `[EnumDataType]` attributes

**Testing:**
- Integration tests use InMemory database provider (existing pattern)
- Follow patterns from `UsersMeIntegrationTests.cs` and `AuthIntegrationTests.cs`
- Mock JWT tokens using `TestAuthHandler` (existing helper)

### Library & Framework Requirements

**Existing Stack (No New Dependencies):**
- .NET 8.0 / C# 12
- ASP.NET Core 8.0 (Web API)
- Entity Framework Core 8.0 with Npgsql provider
- PostgreSQL 16
- xUnit for testing
- FluentAssertions for test assertions

**Serialization:**
- System.Text.Json (existing)
- Configure `JsonStringEnumConverter` for PersonaType enum in Program.cs if not already configured

### File Structure Requirements

**Follow existing conventions:**
- Enums in `Models/` directory (see `WorkflowStatus.cs` pattern)
- DTOs in `DTOs/` directory with XML documentation
- Entities in `Data/Entities/` directory
- Controllers in `Controllers/` with route pattern `api/v1/{resource}`
- Migrations in `Migrations/` directory (auto-generated by EF)

### Testing Requirements

**Code Coverage:**
- Maintain existing coverage standards
- Test all new endpoints (GET includes persona, PATCH updates persona)
- Test default behavior (new users get Hybrid)
- Test validation (invalid persona types rejected)
- Test persistence (persona survives login/logout)

**Test Patterns:**
- Use `WebApplicationFactory<Program>` for integration tests
- Use InMemory database with unique database names per test
- Clean up test data after each test
- Follow existing test naming: `{Method}_{Scenario}_{ExpectedResult}`

### Previous Story Intelligence

**Epic 8 Context:**
- This is the **first story** in Epic 8: Persona Translation & Language Adaptation
- Story establishes foundation: user profile configuration for persona preference
- Future stories (8.2-8.5) will use this persona field to drive translation behavior
- Keep implementation simple: just storage and retrieval, no translation logic yet

**Related Completed Epics:**
- Epic 2 (User Authentication) provides User entity and authentication infrastructure
- UsersController already exists with `/api/v1/users/me` endpoint
- JWT authentication patterns established and working
- Database migrations pattern established (see existing migrations)

### Git Intelligence Summary

**Recent Patterns from Codebase:**
- Database schema changes use EF Core migrations with timestamps
- Controllers follow RESTful patterns with OpenAPI documentation
- Integration tests use WebApplicationFactory with InMemory database
- Enum to string conversion configured in ApplicationDbContext.OnModelCreating()
- JWT claims extraction pattern: `User.FindFirst(ClaimTypes.NameIdentifier)`

### Latest Tech Information

**EF Core 8.0 Best Practices:**
- Use `.HasConversion<string>()` for enum-to-string mapping (clean JSON APIs)
- Default values set in C# entity properties (not in migrations)
- Use `IsRequired()` for non-nullable reference types
- PostgreSQL-specific: `HasMaxLength()` creates VARCHAR(n), no length = TEXT

**ASP.NET Core 8.0 Patterns:**
- Use `[ProducesResponseType]` for OpenAPI documentation
- ProblemDetails for error responses (RFC 7807 standard)
- Model validation automatic with `[ApiController]` attribute
- Use `async/await` for all database operations

### Project Context Reference

**Architecture Document:** `_bmad-output/planning-artifacts/architecture.md`
- Section: ADR-001 (State Persistence Strategy)
- Section: Persona Translation Engine (lines 290-295)

**PRD Document:** `_bmad-output/planning-artifacts/prd.md`
- Requirements: FR12-FR15 (Personas & Communication)
- Section: User personas and language preferences

**Epic Details:** `_bmad-output/planning-artifacts/epics.md`
- Epic 8: Persona Translation & Language Adaptation (lines 2245-2287)
- Story 8.1: Persona Profile Configuration (lines 2255-2287)

### References

- [Source: prd.md#Personas & Communication] - FR12-FR15 define persona requirements
- [Source: architecture.md#Persona Translation Engine] - Architecture overview
- [Source: epics.md#Epic 8] - Detailed story acceptance criteria with BDD format
- [Source: src/bmadServer.ApiService/Data/Entities/User.cs] - Existing User entity
- [Source: src/bmadServer.ApiService/Controllers/UsersController.cs] - Existing GET /api/v1/users/me endpoint
- [Source: src/bmadServer.ApiService/Data/ApplicationDbContext.cs] - Enum conversion patterns (WorkflowStatus)

## Dev Agent Record

### Agent Model Used

_To be filled by Dev Agent_

### Debug Log References

_To be filled by Dev Agent_

### Completion Notes List

_To be filled by Dev Agent_

### File List

_To be filled by Dev Agent_
