# Story 8.5: Context-Aware Response Adaptation

Status: ready-for-dev

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As the system,
I want to adapt responses based on context even within a persona,
so that communication is always appropriate and users receive the most relevant information for their current activity.

## Acceptance Criteria

### AC1: Hybrid Persona Technical Context Adaptation
**Given** Hybrid persona is active  
**When** the workflow step is technical (e.g., architecture review)  
**Then** responses lean technical with code examples
- System detects workflow step type from WorkflowDefinition metadata
- Technical steps include: code snippets, API specifications, implementation details
- Response includes technical vocabulary and examples
- Maintains business context wrapper for clarity

### AC2: Hybrid Persona Business Context Adaptation
**Given** Hybrid persona is active  
**When** the workflow step is strategic (e.g., PRD review)  
**Then** responses lean business with impact analysis
- System detects workflow step type from WorkflowDefinition metadata
- Business steps include: ROI analysis, user impact, trade-offs, success metrics
- Response uses business language with minimal jargon
- Technical details available on-demand via expansion

### AC3: Cross-Persona Question Handling
**Given** a user asks a technical question in Business mode  
**When** the question clearly requires technical answer  
**Then** the system provides technical detail with business context wrapper
- Question classification detects technical intent (keywords: API, database, code, architecture)
- Response structure: Business summary → Technical details → Business implications
- User can expand/collapse technical sections
- Notification explains: "This question requires technical detail"

### AC4: Multi-User Persona-Appropriate Rendering
**Given** multiple personas are in a collaborative session  
**When** a shared message is sent  
**Then** each user sees their persona-appropriate version  
**And** the underlying content is identical
- Original content stored once in database
- Translation/enhancement applied per-user based on their effective persona
- Semantic equivalence maintained across all versions
- All users see the same decisions, just presented differently

### AC5: Response Metadata Transparency
**Given** response adaptation occurs  
**When** I check the API response  
**Then** I see: originalContent, adaptedContent, adaptationReason, targetPersona
- API returns full adaptation metadata for transparency
- Metadata includes: sourcePersona, targetPersona, contextType, adaptationRules
- Useful for debugging, auditing, and user feedback
- Optional field in UI (developer/admin mode)

## Tasks / Subtasks

This story requires persona infrastructure from Stories 8.1-8.4. Check if PersonaType enum and related services exist before implementing.

- [ ] **Task 0: Verify Persona Infrastructure** (All AC)
  - [ ] Check if PersonaType enum exists (Technical, Business, Hybrid)
  - [ ] Check if User/Session entities have persona properties
  - [ ] Check if PersonaTranslationMiddleware or similar exists
  - [ ] If missing, create minimal persona infrastructure or defer to previous stories

- [ ] **Task 1: Context Detection Service** (AC: #1, #2)
  - [ ] Create `IWorkflowContextAnalyzer` interface
  - [ ] Implement `WorkflowContextAnalyzer` service
  - [ ] Add `ContextType` enum: Technical, Business, Hybrid, Administrative
  - [ ] Analyze WorkflowDefinition metadata to determine step context
  - [ ] Extract context from step name, description, tags
  - [ ] Return confidence score for context classification
  - [ ] Register service in DI container

- [ ] **Task 2: Question Intent Classifier** (AC: #3)
  - [ ] Create `IQuestionClassifier` interface
  - [ ] Implement `QuestionClassifier` service with keyword matching
  - [ ] Technical keywords: API, database, code, architecture, schema, deployment
  - [ ] Business keywords: ROI, impact, user, customer, cost, revenue, strategy
  - [ ] Return classification: Technical, Business, Mixed, Unclear
  - [ ] Include confidence level (0.0-1.0)
  - [ ] Register service in DI container

- [ ] **Task 3: Adaptive Response Strategy** (AC: #1, #2, #3)
  - [ ] Create `IResponseAdaptationStrategy` interface
  - [ ] Implement `ResponseAdaptationStrategy` service
  - [ ] Input: originalContent, userPersona, workflowContext, questionIntent
  - [ ] Output: adaptedContent, adaptationReason, targetPersona
  - [ ] Strategy logic for all persona × context combinations
  - [ ] Register service in DI container

- [ ] **Task 4: Adaptation Metadata DTO** (AC: #5)
  - [ ] Create `AdaptationMetadataDto` class
  - [ ] Fields: OriginalContent, AdaptedContent, AdaptationReason, TargetPersona
  - [ ] Fields: ContextType, QuestionIntent, ConfidenceScore, WasAdapted
  - [ ] Add XML documentation for OpenAPI
  - [ ] Add to message response DTOs

- [ ] **Task 5: Multi-User Adaptation Service** (AC: #4)
  - [ ] Create `IMultiUserAdaptationService` interface
  - [ ] Implement `MultiUserAdaptationService`
  - [ ] Load all active users in workflow session
  - [ ] Resolve effective persona for each user
  - [ ] Apply adaptation strategy per user
  - [ ] Cache adaptations (1 minute TTL)
  - [ ] Register service in DI container

- [ ] **Task 6: SignalR Multi-User Broadcast** (AC: #4)
  - [ ] Extend ChatHub with persona-aware broadcast method
  - [ ] Load workflow context for current step
  - [ ] Apply adaptation per user via MultiUserAdaptationService
  - [ ] Send personalized version to each user
  - [ ] Include adaptation metadata in message

- [ ] **Task 7: Response Middleware/Interceptor** (AC: #1-5)
  - [ ] Create or extend middleware for response adaptation
  - [ ] Inject context analyzer and question classifier
  - [ ] Determine workflow context from active step
  - [ ] Classify question intent if user message
  - [ ] Apply adaptation strategy
  - [ ] Add adaptation metadata to response

- [ ] **Task 8: Frontend Adaptation Metadata Display** (AC: #5)
  - [ ] Create AdaptationMetadataViewer component
  - [ ] Show adaptation indicator icon (hidden by default)
  - [ ] Click to expand and view metadata
  - [ ] Display: Persona, Context, Reason, Confidence
  - [ ] Add developer/admin mode toggle

- [ ] **Task 9: Testing** (AC: #1-5)
  - [ ] Unit tests for WorkflowContextAnalyzer
  - [ ] Unit tests for QuestionClassifier
  - [ ] Unit tests for ResponseAdaptationStrategy
  - [ ] Integration tests for all AC scenarios
  - [ ] Performance tests for adaptation timing

- [ ] **Task 10: Documentation** (AC: #1-5)
  - [ ] Document adaptation algorithm
  - [ ] API documentation for metadata
  - [ ] User guide for response adaptation
  - [ ] Developer guide for adding context types

## Dev Notes

### Critical Implementation Warning

**⚠️ PERSONA INFRASTRUCTURE DEPENDENCY:**
- Stories 8.1-8.4 are marked "ready-for-dev" but may NOT be implemented yet
- Check codebase for PersonaType enum, User.PersonaType, Session.CurrentPersona
- This story assumes persona infrastructure exists from previous stories
- If missing: either implement minimal persona support OR defer this story until 8.1-8.4 are done

**Search codebase for:**
```bash
# Check if persona infrastructure exists
grep -r "PersonaType" src/
grep -r "CurrentPersona" src/
grep -r "PersonaTranslation" src/
```

**If persona infrastructure missing:**
1. Implement PersonaType enum (Technical, Business, Hybrid)
2. Add User.PersonaType property
3. Add Session.CurrentPersona property
4. Create basic persona resolution logic
5. Then proceed with this story's adaptation logic

### Architecture Compliance

**Persona Translation Engine (Architecture.md lines 290-296):**
- Context-aware response adaptation based on workflow step type
- Maintains semantic equivalence across translations
- Enables Hybrid persona dynamic adjustment
- Provides transparency via metadata

**Integration Points:**
- Depends on Epic 8, Stories 1-4 (persona infrastructure)
- Integrates with Epic 4 (workflow orchestration)
- Uses session and workflow state management

### Project Structure Notes

**Files to Create:**
```
src/bmadServer.ApiService/
  Models/
    ContextType.cs                    # Enum: Technical, Business, Hybrid, Administrative
    QuestionIntent.cs                 # Enum: Technical, Business, Mixed, Unclear
  DTOs/
    AdaptationMetadataDto.cs          # Metadata DTO
  Services/
    Persona/
      IWorkflowContextAnalyzer.cs
      WorkflowContextAnalyzer.cs
      IQuestionClassifier.cs
      QuestionClassifier.cs
      IResponseAdaptationStrategy.cs
      ResponseAdaptationStrategy.cs
      IMultiUserAdaptationService.cs
      MultiUserAdaptationService.cs

src/bmadServer.Web/
  src/
    components/
      AdaptationMetadataViewer.tsx
```

**Files to Modify (if they exist):**
```
src/bmadServer.ApiService/
  Program.cs                          # Register services
  Hubs/ChatHub.cs                     # Add multi-user broadcast
  Middleware/PersonaTranslationMiddleware.cs  # Add context awareness

src/bmadServer.Web/
  src/
    components/ChatMessage.tsx        # Display adaptation indicator
```

### Technical Requirements Summary

See full technical implementation details in story sections above including:
- ContextType and QuestionIntent enums
- WorkflowContextAnalyzer with keyword matching
- QuestionClassifier with confidence scoring
- ResponseAdaptationStrategy with all persona combinations
- AdaptationMetadataDto with full transparency
- Multi-user adaptation with caching
- SignalR persona-aware broadcasting

**Performance Targets:**
- Context analysis < 50ms
- Question classification < 30ms
- Adaptation strategy < 100ms
- Multi-user adaptation < 200ms per user

### Library & Framework Requirements

- .NET 8.0 / C# 12
- ASP.NET Core 8.0
- Entity Framework Core 8.0
- PostgreSQL 16
- SignalR
- IMemoryCache (built-in)
- React 18+ TypeScript

**No new packages required**

### Testing Requirements

**Key Test Scenarios:**
1. Hybrid persona in technical workflow → technical response
2. Hybrid persona in business workflow → business response
3. Business persona + technical question → wrapped response
4. Multi-user session → personalized responses
5. Adaptation metadata accuracy
6. Caching behavior
7. Performance within targets

### Previous Story Intelligence

**Stories 8.1-8.4 provide:**
- PersonaType enum and infrastructure
- Session persona override logic
- Translation/enhancement services
- SignalR real-time updates

**If not implemented:**
- This story can create minimal persona infrastructure
- Or be deferred until prerequisite stories complete

### Critical Implementation Notes

**🚨 PREVENT COMMON MISTAKES:**

1. **Verify Persona Infrastructure First** - Don't assume it exists
2. **Semantic Equivalence** - Never change content meaning
3. **Performance** - Cache adaptations, lightweight keyword matching
4. **Graceful Degradation** - Fall back to original content on failure
5. **Multi-User** - Each user gets personalized version via targeted SignalR
6. **Metadata** - Always include for transparency and debugging
7. **Testing** - Cover all persona × context × intent combinations

### References

- [Source: epics.md#Story 8.5 (lines 2389-2418)] - BDD acceptance criteria
- [Source: architecture.md#Persona Translation Engine (lines 290-296)]
- [Source: prd.md#FR13, FR14] - Requirements
- [Source: 8-1 through 8-4 stories] - Persona infrastructure (if implemented)
- [Source: Epic 4 stories] - Workflow orchestration context source

---

## Aspire Development Standards

### PostgreSQL Connection Pattern

- Connection string from Aspire AppHost
- Pattern: `builder.AddServiceDefaults();`
- No new tables required
- See Story 1.2 for AppHost configuration

### Project-Wide Standards

- **Reference:** [PROJECT-WIDE-RULES.md](../../../PROJECT-WIDE-RULES.md)
- **Documentation:** https://aspire.dev
- **GitHub:** https://github.com/microsoft/aspire

---

## Dev Agent Record

### Agent Model Used

_To be filled by Dev Agent_

### Debug Log References

_To be filled by Dev Agent_

### Completion Notes List

_To be filled by Dev Agent_

### File List

_To be filled by Dev Agent_
