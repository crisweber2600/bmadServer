# Story 4.1: Workflow Definition & Registry

Status: done

## Story

As a developer,
I want a workflow registry that defines all supported BMAD workflows,
so that the system knows which workflows are available and their step sequences.

## Acceptance Criteria

**Given** I need to define BMAD workflows  
**When** I create `Workflows/WorkflowDefinition.cs`  
**Then** the class includes: WorkflowId, Name, Description, Steps (ordered list), RequiredRoles, EstimatedDuration

**Given** workflow definitions exist  
**When** I create `Workflows/WorkflowRegistry.cs`  
**Then** it provides methods: GetAllWorkflows(), GetWorkflow(id), ValidateWorkflow(id)  
**And** workflows are registered at startup via dependency injection

**Given** the registry is populated  
**When** I query GetAllWorkflows()  
**Then** I receive all BMAD workflows: create-prd, create-architecture, create-stories, design-ux, and others from BMAD spec

**Given** each workflow has steps  
**When** I examine a workflow definition  
**Then** each step includes: StepId, Name, AgentId, InputSchema, OutputSchema, IsOptional, CanSkip

**Given** I request a non-existent workflow  
**When** I call GetWorkflow("invalid-id")  
**Then** the system returns null or throws WorkflowNotFoundException

## Tasks / Subtasks

- [x] Create Workflows/WorkflowDefinition.cs domain model (AC: 1, 4)
  - [x] Add properties: WorkflowId (string), Name (string), Description (string)
  - [x] Add EstimatedDuration (TimeSpan), RequiredRoles (List<string>)
  - [x] Add Steps collection with Step model containing: StepId, Name, AgentId, InputSchema, OutputSchema, IsOptional, CanSkip
- [x] Create Workflows/WorkflowRegistry.cs service (AC: 2, 3)
  - [x] Implement GetAllWorkflows() method
  - [x] Implement GetWorkflow(string id) method
  - [x] Implement ValidateWorkflow(string id) method
  - [x] Add dependency injection registration in Program.cs
- [x] Populate registry with BMAD workflows (AC: 3)
  - [x] Add create-prd workflow definition
  - [x] Add create-architecture workflow definition
  - [x] Add create-stories workflow definition
  - [x] Add design-ux workflow definition
  - [x] Add other core BMAD workflows from specification
- [x] Add error handling for invalid workflows (AC: 5)
  - [x] Return null or throw WorkflowNotFoundException for non-existent workflows
  - [x] Add appropriate logging
- [x] Add unit tests for WorkflowRegistry
  - [x] Test GetAllWorkflows returns all expected workflows
  - [x] Test GetWorkflow with valid id returns correct workflow
  - [x] Test GetWorkflow with invalid id handles error correctly
  - [x] Test ValidateWorkflow with valid and invalid workflows

## Dev Notes

### Architecture Alignment

**Source:** [ARCHITECTURE.md - Workflow Orchestration Engine]

- Place workflow models in `src/bmadServer.ServiceDefaults/Models/Workflows/`
- Place workflow services in `src/bmadServer.ServiceDefaults/Services/Workflows/`
- Use dependency injection pattern established in other services
- Follow C# naming conventions and coding standards
- Ensure compatibility with .NET Aspire service defaults pattern

### Technical Requirements

**Framework Stack:**
- .NET 8 with Aspire service defaults
- C# 12 with nullable reference types enabled
- Use System.Text.Json for any JSON serialization needs

**Design Patterns:**
- Use Repository pattern if database persistence is needed (not in this story)
- Implement as singleton service via DI
- Use immutable collections where appropriate (IReadOnlyList<T>)
- Consider using FluentValidation for workflow validation

**Performance Considerations:**
- Registry should be loaded at startup and cached in memory
- GetWorkflow should be O(1) lookup (use Dictionary internally)
- Consider thread-safety if registry can be modified at runtime

### File Structure Requirements

```
src/bmadServer.ServiceDefaults/
├── Models/
│   └── Workflows/
│       ├── WorkflowDefinition.cs
│       └── WorkflowStep.cs
└── Services/
    └── Workflows/
        ├── IWorkflowRegistry.cs
        └── WorkflowRegistry.cs
```

**Rationale:** Keep workflow domain models and services in ServiceDefaults so they can be shared across ApiService and other Aspire projects.

### Testing Requirements

**Unit Tests Location:** `test/bmadServer.Tests/Services/Workflows/`

**Test Coverage:**
- All public methods in WorkflowRegistry
- Edge cases: null inputs, empty workflow id, invalid workflow id
- Verify all expected BMAD workflows are registered
- Verify workflow definitions have all required properties

**Testing Framework:** xUnit with FluentAssertions

### Dependencies

**NuGet Packages:**
- Microsoft.Extensions.DependencyInjection (already in project)
- No additional packages required for this story

### Previous Story Intelligence

This is the first story in Epic 4. No previous story learnings are available yet.

### Integration Notes

**Connection to Future Stories:**
- Story 4.2 will use WorkflowDefinition to create WorkflowInstance records
- Story 4.3 will use AgentId from step definitions for routing
- Story 4.7 will query workflows through this registry

**API Endpoints (Future):**
- GET /api/v1/workflows - will use GetAllWorkflows()
- GET /api/v1/workflows/{id} - will use GetWorkflow(id)

### Project Context Reference

See `docs/` for overall project documentation and standards.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Epic 4: Workflow Orchestration Engine]
- [Source: ARCHITECTURE.md - Service Structure]
- [Source: ASPIRE-BEST-PRACTICES.md - Dependency Injection Patterns]

## Dev Agent Record

### Agent Model Used

Claude Sonnet 4 - Developer Agent (Amelia)

### Debug Log References

- Created domain models in ServiceDefaults/Models/Workflows/
- Created services in ServiceDefaults/Services/Workflows/
- Registered WorkflowRegistry as singleton in Program.cs
- All 128 tests passing (10 new + 118 existing)

### Completion Notes List

✅ **Implementation Complete** (2026-01-25)
- Created WorkflowDefinition and WorkflowStep domain models
- Implemented IWorkflowRegistry interface and WorkflowRegistry service
- Populated registry with 6 core BMAD workflows: create-prd, create-architecture, create-stories, design-ux, dev-story, code-review
- Each workflow has detailed steps with AgentId, InputSchema, OutputSchema, IsOptional, CanSkip properties
- Implemented null/empty ID handling with appropriate logging
- Added O(1) dictionary-based lookup for performance
- Registered as singleton service in DI container
- Created comprehensive unit tests with 10 test cases covering all functionality
- All tests pass (128/128)

### File List

- src/bmadServer.ServiceDefaults/Models/Workflows/WorkflowDefinition.cs (new)
- src/bmadServer.ServiceDefaults/Models/Workflows/WorkflowStep.cs (new)
- src/bmadServer.ServiceDefaults/Services/Workflows/IWorkflowRegistry.cs (new)
- src/bmadServer.ServiceDefaults/Services/Workflows/WorkflowRegistry.cs (new)
- src/bmadServer.ApiService/Program.cs (modified - added DI registration)
- src/bmadServer.Tests/bmadServer.Tests.csproj (modified - added FluentAssertions and ServiceDefaults reference)
- src/bmadServer.Tests/Unit/Services/Workflows/WorkflowRegistryTests.cs (new)
