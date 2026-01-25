# Story 6.5: Conflict Detection & Resolution

**Status:** ready-for-dev

## Story

As a user (Sarah), I want the system to detect conflicting decisions, so that inconsistencies are caught early.

## Acceptance Criteria

**Given** multiple decisions in a workflow  
**When** a new decision contradicts an existing one  
**Then** the system flags a potential conflict  
**And** I see a warning: "This may conflict with decision [X]"

**Given** a conflict is detected  
**When** I view the conflict details  
**Then** I see: both decisions side by side, the nature of the conflict, suggested resolutions

**Given** I want to resolve a conflict  
**When** I choose a resolution option  
**Then** the system updates both decisions accordingly  
**And** the conflict resolution is logged

**Given** conflict detection rules exist  
**When** I examine the configuration  
**Then** I see rules like: "Budget cannot exceed [X]", "Timeline must be consistent", "Feature scope must match PRD"

**Given** I override a conflict warning  
**When** I proceed despite the conflict  
**Then** the override is logged with my justification  
**And** an audit trail exists for compliance

## Tasks / Subtasks

- [ ] Design ConflictDetection data model and database schema (AC: 1, 2, 3, 4, 5)
  - [ ] Create DecisionConflict entity class
  - [ ] Add ConflictType enum (DataInconsistency, LogicalContradiction, BusinessRuleViolation, ConstraintViolation)
  - [ ] Add DecisionId1, DecisionId2 foreign keys
  - [ ] Add ConflictDescription, Severity (Critical, Warning, Info)
  - [ ] Add Status (Detected, Reviewing, Resolved, Overridden)
  - [ ] Add DetectedAt, DetectedBy, ResolvedAt, ResolvedBy, ResolutionStrategy
  - [ ] Add OverrideJustification for tracking manual overrides
  - [ ] Add SuggestedResolutions JSONB field for storing resolution options
- [ ] Create ConflictDetectionRule configuration model (AC: 4)
  - [ ] Create ConflictDetectionRule entity
  - [ ] Add RuleName, RuleType, RuleCondition (JSON-based), Severity
  - [ ] Add IsActive boolean for enabling/disabling rules
  - [ ] Add examples: BudgetConstraintRule, TimelineConsistencyRule, ScopeAlignmentRule
  - [ ] Design rule evaluation engine interface IConflictDetectionRuleEngine
  - [ ] Consider using simple JSON-based rules for MVP (complex rule engine for Phase 2)
- [ ] Create EF Core migration for conflict detection tables (AC: 1, 4)
  - [ ] Generate migration with `dotnet ef migrations add AddConflictDetection`
  - [ ] Create decision_conflicts table with foreign keys to decisions
  - [ ] Create conflict_detection_rules table for configurable rules
  - [ ] Create conflict_resolutions table for logging resolution history
  - [ ] Add indexes on DecisionId1, DecisionId2 for query performance
  - [ ] Add index on Status for filtering active conflicts
  - [ ] Test migration locally before committing
- [ ] Implement core conflict detection logic in ConflictDetectionService (AC: 1, 2)
  - [ ] Create IConflictDetectionService interface
  - [ ] Implement DetectConflictsAsync(Guid decisionId) - checks against existing decisions
  - [ ] Implement EvaluateRulesAsync(Decision decision, List<Decision> existingDecisions)
  - [ ] Load active ConflictDetectionRules from database
  - [ ] For each rule, evaluate condition against decision pairs
  - [ ] Create DecisionConflict records for detected conflicts
  - [ ] Return ConflictDetectionResult with list of conflicts and warnings
  - [ ] Consider caching rules for performance
- [ ] Implement built-in conflict detection rules (AC: 1, 4)
  - [ ] BudgetConstraintRule: Sum of budget decisions cannot exceed workflow budget
  - [ ] TimelineConsistencyRule: Dates must be chronologically ordered
  - [ ] ScopeAlignmentRule: Feature decisions must match PRD scope
  - [ ] DuplicateDecisionRule: Same decision type with different values
  - [ ] DependencyConsistencyRule: Decisions with dependencies must be compatible
  - [ ] Store rules in conflict_detection_rules table with JSON conditions
- [ ] Implement conflict resolution logic (AC: 3, 5)
  - [ ] Create ResolveConflictAsync(Guid conflictId, ResolutionStrategy strategy)
  - [ ] ResolutionStrategy enum: KeepBoth, KeepFirst, KeepSecond, Merge, Custom
  - [ ] Update affected decisions based on strategy
  - [ ] Update DecisionConflict.Status to Resolved
  - [ ] Log resolution action with ResolutionStrategy and timestamp
  - [ ] Create ConflictResolution audit record
  - [ ] Trigger notification to relevant users
- [ ] Implement conflict override functionality (AC: 5)
  - [ ] Create OverrideConflictAsync(Guid conflictId, string justification, Guid userId)
  - [ ] Validate user has Participant or Admin role
  - [ ] Update DecisionConflict.Status to Overridden
  - [ ] Store OverrideJustification, OverriddenBy, OverriddenAt
  - [ ] Create audit log entry for compliance tracking
  - [ ] Return confirmation with override details
- [ ] Add conflict detection hooks to DecisionService (AC: 1)
  - [ ] Inject IConflictDetectionService into DecisionService
  - [ ] Call DetectConflictsAsync after CreateDecisionAsync
  - [ ] Call DetectConflictsAsync after UpdateDecisionAsync
  - [ ] If conflicts detected, include in response with warnings
  - [ ] Do NOT block decision creation - only warn (user decides to proceed)
  - [ ] Log all conflict detections for analytics
- [ ] Add conflict endpoints to DecisionController (AC: 1, 2, 3, 4, 5)
  - [ ] GET `/api/v1/decisions/{id}/conflicts` - Get conflicts for a decision
  - [ ] GET `/api/v1/workflows/{id}/conflicts` - Get all conflicts in workflow
  - [ ] POST `/api/v1/conflicts/{id}/resolve` - Resolve conflict with strategy
  - [ ] POST `/api/v1/conflicts/{id}/override` - Override conflict with justification
  - [ ] GET `/api/v1/conflict-rules` - List active conflict detection rules (Admin only)
  - [ ] POST `/api/v1/conflict-rules` - Create custom rule (Admin only)
  - [ ] Add [Authorize] with role checks (Admin for rules, Participant for overrides)
  - [ ] Return proper DTOs with conflict details and resolution options
- [ ] Create DTOs for conflict detection (AC: 1, 2, 3, 4, 5)
  - [ ] DecisionConflictDto - { id, decisionId1, decisionId2, conflictType, description, severity, status, suggestedResolutions, detectedAt }
  - [ ] ConflictDetectionResultDto - { hasConflicts, conflicts: DecisionConflictDto[], warnings: string[] }
  - [ ] ResolveConflictRequest - { strategy: ResolutionStrategy, customResolution?: any }
  - [ ] OverrideConflictRequest - { justification: string }
  - [ ] ConflictDetectionRuleDto - { id, ruleName, ruleType, severity, isActive }
  - [ ] CreateConflictRuleRequest - { ruleName, ruleType, ruleCondition: JSON, severity }
  - [ ] Update DecisionDto to include: hasConflicts, conflictCount, conflicts: DecisionConflictDto[]
- [ ] Implement suggested resolutions generation (AC: 2)
  - [ ] Create SuggestResolutionsAsync(Guid conflictId) method
  - [ ] Analyze conflict type and generate context-aware suggestions
  - [ ] For BudgetConstraint: "Reduce budget allocation", "Request additional funding"
  - [ ] For Timeline: "Adjust timeline", "Re-sequence dependencies"
  - [ ] For Scope: "Remove feature", "Modify scope definition"
  - [ ] Store suggestions in DecisionConflict.SuggestedResolutions JSONB
  - [ ] Return list of actionable resolution options with impact analysis
- [ ] Add SignalR real-time conflict notifications (AC: 1)
  - [ ] Create ConflictDetected hub method
  - [ ] Broadcast to workflow participants when conflict detected
  - [ ] Include conflict details and suggested actions
  - [ ] Update UI to show conflict badge/indicator in real-time
  - [ ] Use existing ChatHub or create dedicated ConflictNotificationHub
- [ ] Write unit tests for conflict detection logic (AC: 1, 2, 3, 4, 5)
  - [ ] Test DetectConflictsAsync with various conflict scenarios
  - [ ] Test BudgetConstraintRule detects over-budget decisions
  - [ ] Test TimelineConsistencyRule flags date inconsistencies
  - [ ] Test ResolveConflictAsync applies correct resolution strategy
  - [ ] Test OverrideConflictAsync logs justification and audit trail
  - [ ] Test rule evaluation with active and inactive rules
  - [ ] Test authorization rules for override functionality
  - [ ] Mock dependencies (DbContext, DecisionService)
- [ ] Write integration tests for conflict detection (AC: 1, 2, 3, 4, 5)
  - [ ] Test POST decision with conflicting data returns warning
  - [ ] Test GET /conflicts returns detected conflicts
  - [ ] Test conflict resolution updates both decisions correctly
  - [ ] Test override creates audit log entry
  - [ ] Test conflict rules can be configured by Admin
  - [ ] Test SignalR broadcasts conflict notifications
  - [ ] Use WebApplicationFactory for end-to-end testing
- [ ] Add API documentation for conflict endpoints (AC: 1, 2, 3, 4, 5)
  - [ ] Document conflict detection process in OpenAPI/Swagger
  - [ ] Add examples for conflict response format
  - [ ] Document resolution strategies and their effects
  - [ ] Document override justification requirements
  - [ ] Add curl examples for each endpoint
  - [ ] Document conflict rule configuration format

## Dev Notes

### Architecture Context

**Decision Management System:**
This story completes Epic 6 by implementing intelligent conflict detection and resolution for workflow decisions. The system must detect logical inconsistencies, business rule violations, and data conflicts across multiple decisions within a workflow.

**Key Architectural Patterns from Epic 6:**
- **Decision Capture (6.1):** Foundation - Decision entity, JSONB storage, audit trail
- **Version History (6.2):** Track decision evolution and changes over time
- **Locking Mechanism (6.3):** Prevent changes to locked decisions
- **Review Workflow (6.4):** Multi-user approval process before locking
- **Conflict Detection (6.5):** This story - intelligent validation and resolution

### Technical Implementation Strategy

**Conflict Detection Engine:**
1. **Rule-Based Detection:** JSON-configured rules evaluated on decision create/update
2. **Relationship Analysis:** Compare new decision against existing decisions in workflow
3. **Business Logic Validation:** Built-in rules for common constraints (budget, timeline, scope)
4. **Real-Time Warnings:** Non-blocking detection - warn but allow user to proceed

**Resolution Strategies:**
- **KeepBoth:** Mark conflict as acknowledged but retain both decisions
- **KeepFirst:** Retain original decision, reject new one
- **KeepSecond:** Accept new decision, deprecate original
- **Merge:** Combine both decisions (user-guided)
- **Custom:** User-defined resolution with manual updates

**Audit Trail Requirements:**
- Every conflict detection logged with timestamp
- Resolution actions tracked with strategy and user
- Override justifications stored for compliance
- Full audit trail for regulatory requirements

### Database Schema Design

**decision_conflicts table:**
```sql
CREATE TABLE decision_conflicts (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    decision_id_1 UUID NOT NULL REFERENCES decisions(id),
    decision_id_2 UUID NOT NULL REFERENCES decisions(id),
    conflict_type VARCHAR(50) NOT NULL, -- DataInconsistency, LogicalContradiction, etc.
    description TEXT NOT NULL,
    severity VARCHAR(20) NOT NULL, -- Critical, Warning, Info
    status VARCHAR(20) NOT NULL, -- Detected, Reviewing, Resolved, Overridden
    suggested_resolutions JSONB,
    detected_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    detected_by VARCHAR(100),
    resolved_at TIMESTAMPTZ,
    resolved_by UUID REFERENCES users(id),
    resolution_strategy VARCHAR(50),
    override_justification TEXT,
    overridden_at TIMESTAMPTZ,
    overridden_by UUID REFERENCES users(id),
    CONSTRAINT chk_different_decisions CHECK (decision_id_1 <> decision_id_2)
);

CREATE INDEX idx_conflicts_decision1 ON decision_conflicts(decision_id_1);
CREATE INDEX idx_conflicts_decision2 ON decision_conflicts(decision_id_2);
CREATE INDEX idx_conflicts_status ON decision_conflicts(status) WHERE status IN ('Detected', 'Reviewing');
```

**conflict_detection_rules table:**
```sql
CREATE TABLE conflict_detection_rules (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    rule_name VARCHAR(200) NOT NULL,
    rule_type VARCHAR(100) NOT NULL,
    rule_condition JSONB NOT NULL, -- JSON-based rule evaluation criteria
    severity VARCHAR(20) NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

### Conflict Detection Rule Examples

**Budget Constraint Rule:**
```json
{
  "ruleType": "BudgetConstraint",
  "condition": {
    "decisionType": "budget_allocation",
    "aggregationType": "sum",
    "maxValue": 1000000,
    "scope": "workflow"
  }
}
```

**Timeline Consistency Rule:**
```json
{
  "ruleType": "TimelineConsistency",
  "condition": {
    "decisionTypes": ["start_date", "end_date", "milestone_date"],
    "validation": "chronological_order",
    "allowOverlaps": false
  }
}
```

**Scope Alignment Rule:**
```json
{
  "ruleType": "ScopeAlignment",
  "condition": {
    "decisionType": "feature_scope",
    "mustMatchSource": "prd_requirements",
    "allowExpansion": false
  }
}
```

### Service Layer Design

**IConflictDetectionService:**
```csharp
public interface IConflictDetectionService
{
    Task<ConflictDetectionResult> DetectConflictsAsync(Guid decisionId);
    Task<List<DecisionConflict>> GetConflictsForDecisionAsync(Guid decisionId);
    Task<List<DecisionConflict>> GetConflictsForWorkflowAsync(Guid workflowId);
    Task<ConflictResolutionResult> ResolveConflictAsync(Guid conflictId, ResolutionStrategy strategy);
    Task<ConflictOverrideResult> OverrideConflictAsync(Guid conflictId, string justification, Guid userId);
    Task<List<string>> GenerateSuggestedResolutionsAsync(Guid conflictId);
}
```

**Integration with DecisionService:**
```csharp
// After creating/updating decision
var conflictResult = await _conflictDetectionService.DetectConflictsAsync(decision.Id);
if (conflictResult.HasConflicts)
{
    // Include conflicts in response but don't block
    response.HasConflicts = true;
    response.Conflicts = conflictResult.Conflicts;
    response.Warnings = conflictResult.Warnings;
    
    // Optionally notify via SignalR
    await _chatHub.Clients.Group(workflowId.ToString())
        .SendAsync("ConflictDetected", conflictResult);
}
```

### Authorization Rules

- **View Conflicts:** Any workflow participant (Contributor, Observer, Admin)
- **Resolve Conflicts:** Contributor or Admin role required
- **Override Conflicts:** Participant or Admin role required + justification mandatory
- **Manage Rules:** Admin only
- **Create Custom Rules:** Admin only

### Real-Time Notification Flow

1. Decision created/updated → Conflict detection runs
2. If conflict detected → Create DecisionConflict record
3. Broadcast via SignalR to workflow participants: `ConflictDetected` event
4. UI shows conflict badge/warning with details
5. User can view conflict comparison and choose resolution
6. Resolution/override → Broadcast `ConflictResolved` event

### Performance Considerations

- **Rule Caching:** Cache active rules in memory with cache invalidation on updates
- **Lazy Loading:** Only load conflicts when explicitly requested (not on every decision query)
- **Indexed Queries:** Use indexes on decision_id_1, decision_id_2, status
- **JSONB Indexing:** Create GIN indexes on suggested_resolutions and rule_condition for fast queries
- **Batch Detection:** For bulk decision imports, run detection in background job

### Error Handling

- Invalid resolution strategy → 400 Bad Request with valid options
- Conflict already resolved → 409 Conflict with current status
- Unauthorized override attempt → 403 Forbidden
- Missing justification for override → 400 Bad Request
- Database errors → 500 Internal Server Error with ProblemDetails

### Testing Strategy

**Unit Tests:**
- Rule evaluation logic with various conflict scenarios
- Resolution strategy application
- Override validation and audit logging
- Suggested resolutions generation

**Integration Tests:**
- End-to-end conflict detection on decision create
- Conflict resolution updates decisions correctly
- Override creates proper audit trail
- SignalR broadcasts work correctly
- Rule configuration by Admin

**Performance Tests:**
- Conflict detection with 100+ decisions in workflow
- Rule evaluation performance with 50+ active rules
- Concurrent conflict resolution by multiple users

### Dependencies

**Required Stories:**
- ✅ 6.1: Decision Capture & Storage (foundation)
- ✅ 6.2: Decision Version History (track changes)
- ✅ 6.3: Decision Locking Mechanism (prevent locked decision conflicts)
- ✅ 6.4: Decision Review Workflow (approval process)

**External Dependencies:**
- PostgreSQL 17.x with JSONB support
- EF Core 9.0 for migrations
- SignalR for real-time notifications
- System.Text.Json for rule condition parsing

### Project Structure

**Files to Create:**
```
src/bmadServer.ApiService/
├── Models/
│   └── Decisions/
│       ├── DecisionConflict.cs (entity)
│       ├── ConflictDetectionRule.cs (entity)
│       ├── ConflictType.cs (enum)
│       ├── ConflictSeverity.cs (enum)
│       ├── ConflictStatus.cs (enum)
│       └── ResolutionStrategy.cs (enum)
├── Services/
│   ├── IConflictDetectionService.cs (interface)
│   ├── ConflictDetectionService.cs (implementation)
│   └── ConflictDetectionRuleEngine.cs (rule evaluation)
├── DTOs/
│   └── Decisions/
│       ├── DecisionConflictDto.cs
│       ├── ConflictDetectionResultDto.cs
│       ├── ResolveConflictRequest.cs
│       ├── OverrideConflictRequest.cs
│       └── ConflictDetectionRuleDto.cs
├── Controllers/
│   └── DecisionController.cs (add conflict endpoints)
└── Migrations/
    └── YYYYMMDDHHMMSS_AddConflictDetection.cs

src/bmadServer.Tests/
├── Services/
│   └── ConflictDetectionServiceTests.cs
└── Controllers/
    └── DecisionControllerConflictTests.cs

src/bmadServer.ApiService.IntegrationTests/
└── ConflictDetectionIntegrationTests.cs
```

### Previous Story Learnings (from 6.4)

**Patterns to Follow:**
- DecisionReview model pattern → Use similar pattern for DecisionConflict
- Review notification system → Extend for conflict notifications
- Multi-step workflow (request → approve → lock) → Apply to detection → resolve → audit
- Authorization checks on controller endpoints → Apply same role checks

**Database Conventions:**
- Snake_case table names (decision_conflicts, conflict_detection_rules)
- TIMESTAMPTZ for all timestamps
- UUID primary keys with gen_random_uuid()
- Foreign keys to decisions table
- Indexes on commonly queried fields

**Service Layer Patterns:**
- Async methods with CancellationToken support
- Validation before state changes
- Notification triggers after successful operations
- Audit logging for all mutations
- Return DTOs, not entities

### API Examples

**Detect Conflicts on Decision Create:**
```bash
POST /api/v1/decisions
{
  "workflowInstanceId": "abc-123",
  "stepId": "step-5",
  "decisionType": "budget_allocation",
  "value": { "amount": 500000, "category": "development" }
}

Response (200 OK):
{
  "decision": { ... },
  "hasConflicts": true,
  "conflicts": [
    {
      "id": "conflict-123",
      "decisionId1": "decision-456",
      "decisionId2": "decision-789",
      "conflictType": "BudgetConstraintViolation",
      "description": "Total budget allocation exceeds workflow budget of $1,000,000",
      "severity": "Critical",
      "suggestedResolutions": [
        "Reduce this allocation to $400,000",
        "Request budget increase",
        "Reallocate from another category"
      ]
    }
  ],
  "warnings": [
    "This may conflict with decision [decision-456]"
  ]
}
```

**Get Conflicts for Workflow:**
```bash
GET /api/v1/workflows/{workflowId}/conflicts

Response (200 OK):
{
  "workflowId": "abc-123",
  "conflictCount": 2,
  "conflicts": [
    {
      "id": "conflict-123",
      "status": "Detected",
      "severity": "Critical",
      "detectedAt": "2026-01-25T10:30:00Z"
    },
    {
      "id": "conflict-456",
      "status": "Overridden",
      "severity": "Warning",
      "overrideJustification": "Business approved exception"
    }
  ]
}
```

**Resolve Conflict:**
```bash
POST /api/v1/conflicts/{conflictId}/resolve
{
  "strategy": "KeepSecond",
  "notes": "Updated decision reflects latest requirements"
}

Response (200 OK):
{
  "conflictId": "conflict-123",
  "status": "Resolved",
  "resolutionStrategy": "KeepSecond",
  "resolvedAt": "2026-01-25T11:00:00Z",
  "affectedDecisions": ["decision-789"]
}
```

**Override Conflict:**
```bash
POST /api/v1/conflicts/{conflictId}/override
{
  "justification": "CFO approved budget increase for Q1 2026. Email ref: CFO-2026-0125"
}

Response (200 OK):
{
  "conflictId": "conflict-123",
  "status": "Overridden",
  "overriddenBy": "user-sarah-123",
  "overriddenAt": "2026-01-25T11:15:00Z",
  "justification": "CFO approved budget increase for Q1 2026. Email ref: CFO-2026-0125"
}
```

### Validation Rules

- Justification for override: min 20 characters, max 500 characters
- Conflict must be in "Detected" status to resolve (not already Resolved/Overridden)
- User must have Participant or Admin role for resolution/override
- Both decisions in conflict must exist and be from same workflow
- Resolution strategy must be valid enum value
- Custom resolutions must include detailed explanation

---

## Aspire Development Standards

### PostgreSQL Connection Pattern

This story uses PostgreSQL configured in Story 1.2 via Aspire:
- Connection string automatically injected from Aspire AppHost
- Pattern: `builder.AddServiceDefaults();` (inherits PostgreSQL reference)
- Use `ApplicationDbContext` for all database operations
- See Story 1.2 for AppHost configuration pattern

### Project-Wide Standards

This story follows the Aspire-first development pattern:
- **Reference:** [PROJECT-WIDE-RULES.md](../../../PROJECT-WIDE-RULES.md)
- **Primary Documentation:** https://aspire.dev
- **GitHub:** https://github.com/microsoft/aspire
- Use Aspire service defaults for observability and health checks
- Follow .NET 10 patterns and best practices

---

## Dev Agent Record

### Agent Model Used

_To be filled by dev agent during implementation_

### Debug Log References

_To be filled by dev agent during implementation_

### Completion Notes List

_To be filled by dev agent during implementation_

### File List

_To be filled by dev agent during implementation_

---

## References

- **Aspire Rules:** [PROJECT-WIDE-RULES.md](../../../PROJECT-WIDE-RULES.md)
- **Aspire Docs:** https://aspire.dev
- Source: [epics.md - Story 6.5](../planning-artifacts/epics.md)
- Architecture: [architecture.md - Decision Tracker Component](../planning-artifacts/architecture.md#4-decision-tracker)
- PRD: [prd.md - FR9, FR10, FR22, FR23](../planning-artifacts/prd.md)
- Previous Story: [6-4-decision-review-workflow.md](./6-4-decision-review-workflow.md)

**Epic 6 Context:**
- Story 6.1: Decision Capture & Storage (foundation)
- Story 6.2: Decision Version History (versioning)
- Story 6.3: Decision Locking Mechanism (immutability)
- Story 6.4: Decision Review Workflow (approval process)
- **Story 6.5: Conflict Detection & Resolution** (this story - intelligent validation)
