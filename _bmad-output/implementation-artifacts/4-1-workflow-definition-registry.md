# Story 4.1: Workflow Definition & Registry

Status: ready-for-dev

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

- [ ] Create Workflows/WorkflowDefinition.cs domain model (AC: 1, 4)
  - [ ] Add properties: WorkflowId (string), Name (string), Description (string)
  - [ ] Add EstimatedDuration (TimeSpan), RequiredRoles (List<string>)
  - [ ] Add Steps collection with Step model containing: StepId, Name, AgentId, InputSchema, OutputSchema, IsOptional, CanSkip
- [ ] Create Workflows/WorkflowRegistry.cs service (AC: 2, 3)
  - [ ] Implement GetAllWorkflows() method
  - [ ] Implement GetWorkflow(string id) method
  - [ ] Implement ValidateWorkflow(string id) method
  - [ ] Add dependency injection registration in Program.cs
- [ ] Populate registry with BMAD workflows (AC: 3)
  - [ ] Add create-prd workflow definition
  - [ ] Add create-architecture workflow definition
  - [ ] Add create-stories workflow definition
  - [ ] Add design-ux workflow definition
  - [ ] Add other core BMAD workflows from specification
- [ ] Add error handling for invalid workflows (AC: 5)
  - [ ] Return null or throw WorkflowNotFoundException for non-existent workflows
  - [ ] Add appropriate logging
- [ ] Add unit tests for WorkflowRegistry
  - [ ] Test GetAllWorkflows returns all expected workflows
  - [ ] Test GetWorkflow with valid id returns correct workflow
  - [ ] Test GetWorkflow with invalid id handles error correctly
  - [ ] Test ValidateWorkflow with valid and invalid workflows

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

_To be filled by dev agent_

### Debug Log References

_To be filled by dev agent_

### Completion Notes List

_To be filled by dev agent_

### File List

_To be filled by dev agent_
