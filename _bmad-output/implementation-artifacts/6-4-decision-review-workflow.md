# Story 6.4: Decision Review Workflow

**Status:** ready-for-dev

## Story

As a user (Marcus), I want to request a review before locking a decision, so that I can get approval from stakeholders.

## Acceptance Criteria

**Given** I have a decision ready to lock  
**When** I send POST `/api/v1/decisions/{id}/request-review` with reviewers list  
**Then** the decision status changes to UnderReview  
**And** selected reviewers receive notifications

**Given** a review is requested  
**When** a reviewer views the decision  
**Then** they see: decision content, requester info, deadline (if set), Approve/Request Changes buttons

**Given** a reviewer approves  
**When** they click "Approve"  
**Then** their approval is recorded  
**And** if all required approvals received, decision auto-locks

**Given** a reviewer requests changes  
**When** they click "Request Changes" with comments  
**Then** the decision returns to Draft status  
**And** the requester is notified with feedback

**Given** the review deadline passes  
**When** approvals are incomplete  
**Then** the requester is notified  
**And** they can extend deadline or proceed without full approval

## Tasks / Subtasks

- [ ] Design DecisionReview data model and database schema (AC: 1, 2, 3, 4, 5)
  - [ ] Create DecisionReview entity class
  - [ ] Add ReviewerId, ReviewerName, Status (Pending/Approved/ChangesRequested), Comments, ReviewedAt
  - [ ] Add relationship: Decision 1-to-Many DecisionReviews
  - [ ] Add RequiredApprovals count to Decision entity
  - [ ] Add ReviewDeadline (DateTime?) to Decision entity
  - [ ] Update DecisionStatus enum to include UnderReview state
- [ ] Create EF Core migration for review tables (AC: 1)
  - [ ] Generate migration with `dotnet ef migrations add AddDecisionReviewWorkflow`
  - [ ] Create decision_reviews table with foreign key to decisions
  - [ ] Add review-related fields to decisions table
  - [ ] Add indexes on ReviewerId and DecisionId for query performance
  - [ ] Test migration locally before committing
- [ ] Implement review request logic in DecisionService (AC: 1)
  - [ ] Create RequestReviewAsync(Guid decisionId, List<Guid> reviewerIds, DateTime? deadline, int requiredApprovals)
  - [ ] Validate decision exists and is in Draft/Unlocked status
  - [ ] Update Decision.Status to UnderReview
  - [ ] Create DecisionReview records for each reviewer with Status=Pending
  - [ ] Store ReviewDeadline and RequiredApprovals on Decision entity
  - [ ] Trigger notification event for each reviewer
  - [ ] Return DecisionReviewResponse with decision and reviewers list
- [ ] Implement approve/request-changes logic in DecisionService (AC: 3, 4)
  - [ ] Create ApproveReviewAsync(Guid decisionId, Guid reviewerId)
  - [ ] Create RequestChangesAsync(Guid decisionId, Guid reviewerId, string comments)
  - [ ] Validate reviewer is in reviewers list
  - [ ] Update DecisionReview status (Approved or ChangesRequested)
  - [ ] Record ReviewedAt timestamp and Comments
  - [ ] Check if all required approvals received → auto-lock decision
  - [ ] If changes requested → change Decision.Status back to Draft
  - [ ] Trigger notification to requester with outcome
- [ ] Implement deadline monitoring logic (AC: 5)
  - [ ] Create CheckReviewDeadlinesAsync background job
  - [ ] Query decisions with Status=UnderReview and ReviewDeadline < Now
  - [ ] For expired reviews, send notification to requester
  - [ ] Provide options: extend deadline or proceed without full approval
  - [ ] Consider using background service or hangfire for scheduling
- [ ] Add review endpoints to DecisionController (AC: 1, 2, 3, 4, 5)
  - [ ] POST `/api/v1/decisions/{id}/request-review` - Request review
  - [ ] GET `/api/v1/decisions/{id}/reviews` - Get all reviews for decision
  - [ ] POST `/api/v1/decisions/{id}/reviews/approve` - Approve as reviewer
  - [ ] POST `/api/v1/decisions/{id}/reviews/request-changes` - Request changes
  - [ ] POST `/api/v1/decisions/{id}/reviews/extend-deadline` - Extend deadline
  - [ ] Add [Authorize] with appropriate role checks
  - [ ] Return proper DTOs with review status and metadata
- [ ] Create DTOs for review workflow (AC: 1, 2, 3, 4, 5)
  - [ ] RequestReviewRequest - { reviewerIds: Guid[], deadline?: DateTime, requiredApprovals: int }
  - [ ] RequestChangesRequest - { comments: string }
  - [ ] DecisionReviewDto - { id, decisionId, reviewerId, reviewerName, status, comments, reviewedAt }
  - [ ] DecisionReviewResponse - includes decision + list of reviews
  - [ ] ExtendDeadlineRequest - { newDeadline: DateTime }
  - [ ] Update DecisionDto to include: reviewStatus, reviewers, deadline, pendingApprovals
- [ ] Implement notification service integration (AC: 1, 4, 5)
  - [ ] Create INotificationService interface if not exists
  - [ ] Implement SendReviewRequestNotification(userId, decisionId, requester)
  - [ ] Implement SendReviewOutcomeNotification(userId, decisionId, outcome)
  - [ ] Implement SendDeadlineExpiredNotification(userId, decisionId)
  - [ ] Use in-app notifications (MVP) - email/webhook optional Phase 2
- [ ] Write unit tests for review workflow logic (AC: 1, 2, 3, 4, 5)
  - [ ] Test RequestReviewAsync creates reviews and changes status
  - [ ] Test ApproveReviewAsync records approval and auto-locks when complete
  - [ ] Test RequestChangesAsync returns decision to Draft
  - [ ] Test partial approvals don't auto-lock
  - [ ] Test deadline monitoring detects expired reviews
  - [ ] Test authorization rules for reviewers
  - [ ] Test edge cases: invalid reviewer, decision already locked, etc.
- [ ] Write integration tests for review API (AC: 1, 2, 3, 4, 5)
  - [ ] Test POST request-review endpoint with multiple reviewers
  - [ ] Test GET reviews endpoint returns all reviews with status
  - [ ] Test POST approve endpoint and verify auto-lock behavior
  - [ ] Test POST request-changes endpoint and verify status change
  - [ ] Test deadline expiration workflow
  - [ ] Test with real PostgreSQL database
  - [ ] Test concurrent approvals (race conditions)
- [ ] Update OpenAPI documentation (AC: 1, 2, 3, 4, 5)
  - [ ] Document all review endpoints with examples
  - [ ] Document request/response schemas
  - [ ] Document review workflow state transitions
  - [ ] Add sequence diagram showing review flow
- [ ] UI integration guidance for frontend (AC: 2)
  - [ ] Document "Request Review" button display logic
  - [ ] Document reviewer selection UI component
  - [ ] Document review panel with Approve/Request Changes buttons
  - [ ] Document deadline display and extension UI
  - [ ] Provide API call examples for UI components

## Dev Notes

### Architecture Context

**Decision Management Domain:**
This story is part of Epic 6 (Decision Management & Locking), building the review workflow on top of the foundation established in:
- Story 6.1: Decision capture and storage (Decision entity, DecisionService, API endpoints)
- Story 6.2: Version history (DecisionVersion tracking, diff capability, revert functionality)
- Story 6.3: Locking mechanism (Lock/Unlock endpoints, status management, authorization)

**Related Systems:**
- **Workflow Engine:** Decisions are made during workflow execution steps
- **Session Management:** Reviews are tied to user sessions and permissions
- **Authentication/Authorization:** Role-based access control for reviewers
- **Notification System:** Alerts for review requests, approvals, and deadline expiration

### Technical Requirements

**Database Schema Design:**

```sql
-- Add to Decision entity
ALTER TABLE decisions ADD COLUMN review_deadline TIMESTAMP NULL;
ALTER TABLE decisions ADD COLUMN required_approvals INTEGER DEFAULT 1;

-- New table for reviews
CREATE TABLE decision_reviews (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    decision_id UUID NOT NULL REFERENCES decisions(id) ON DELETE CASCADE,
    reviewer_id UUID NOT NULL REFERENCES users(id),
    reviewer_name VARCHAR(255) NOT NULL,
    status VARCHAR(50) NOT NULL, -- Pending, Approved, ChangesRequested
    comments TEXT NULL,
    reviewed_at TIMESTAMP NULL,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    UNIQUE(decision_id, reviewer_id)
);

CREATE INDEX idx_decision_reviews_decision_id ON decision_reviews(decision_id);
CREATE INDEX idx_decision_reviews_reviewer_id ON decision_reviews(reviewer_id);
CREATE INDEX idx_decision_reviews_status ON decision_reviews(status);
```

**DecisionStatus Enum Extension:**
```csharp
public enum DecisionStatus
{
    Draft,
    UnderReview,  // NEW for this story
    Locked
}
```

**Review Status Enum:**
```csharp
public enum ReviewStatus
{
    Pending,
    Approved,
    ChangesRequested
}
```

### Library & Framework Requirements

**Core Dependencies (Already in project from previous stories):**
- .NET 10 with ASP.NET Core
- Entity Framework Core 9.0 for data access
- PostgreSQL 17.x with JSONB support
- Microsoft.AspNetCore.Identity for user management
- FluentValidation 11.9.2 for request validation

**Notification Infrastructure (New for this story):**
- Consider using existing SignalR infrastructure for real-time notifications
- Alternative: In-memory notification queue for MVP, message broker Phase 2

**Background Job Processing (New for this story):**
- Option 1: BackgroundService in ASP.NET Core (simplest MVP)
- Option 2: Hangfire for robust scheduling (recommended if available)
- Option 3: Quartz.NET for advanced scheduling needs

### File Structure Requirements

**New Files to Create:**
```
src/bmadServer.ApiService/
  Models/
    Decisions/
      DecisionReview.cs                    # Entity model
      ReviewStatus.cs                      # Enum
  DTOs/
    Decisions/
      RequestReviewRequest.cs
      DecisionReviewDto.cs
      DecisionReviewResponse.cs
      RequestChangesRequest.cs
      ExtendDeadlineRequest.cs
  Services/
    Notifications/
      INotificationService.cs              # Interface
      NotificationService.cs               # Implementation
  BackgroundServices/
    ReviewDeadlineMonitorService.cs        # Background job
  Migrations/
    {timestamp}_AddDecisionReviewWorkflow.cs

src/bmadServer.Tests/
  Unit/
    Services/
      DecisionReviewServiceTests.cs
  Integration/
    Controllers/
      DecisionReviewWorkflowTests.cs
```

**Files to Modify:**
```
src/bmadServer.ApiService/
  Models/Decisions/Decision.cs            # Add ReviewDeadline, RequiredApprovals
  Models/Decisions/DecisionStatus.cs      # Add UnderReview enum value
  Services/DecisionService.cs             # Add review methods
  Controllers/DecisionController.cs       # Add review endpoints
  DTOs/Decisions/DecisionDto.cs           # Add review fields
  Data/AppDbContext.cs                    # Add DecisionReviews DbSet
  Program.cs                              # Register notification service and background service
```

### Previous Story Intelligence

**From Story 6.3 (Decision Locking):**
- Lock/Unlock mechanism already implemented
- Auto-lock on full approval should reuse POST `/api/v1/decisions/{id}/lock` logic
- Authorization pattern: Only Participant/Admin can lock → extend to reviewers
- DecisionService already has proper error handling and audit logging patterns

**From Story 6.2 (Version History):**
- Version tracking infrastructure exists
- Review approvals should create version records automatically
- Change requests returning to Draft should create new version
- Use existing versioning patterns for consistency

**From Story 6.1 (Decision Capture):**
- Base Decision entity and DecisionService patterns established
- API endpoint patterns follow RESTful conventions with ProblemDetails
- JSONB storage for flexible decision value/context
- Authorization and validation patterns well-established

**Key Patterns to Reuse:**
1. Service method signatures: `async Task<Result<TResponse>>` pattern
2. Controller error handling: ProblemDetails with proper status codes
3. Authorization: `[Authorize]` with role-based checks
4. Validation: FluentValidation for request DTOs
5. Audit logging: Log all review actions with user context

### Testing Requirements

**Unit Test Coverage:**
- Service layer business logic (80%+ coverage target)
- Edge cases: expired deadlines, invalid reviewers, concurrent approvals
- Authorization rules enforcement
- Auto-lock trigger conditions
- Notification trigger logic

**Integration Test Coverage:**
- End-to-end review workflow from request to approval/rejection
- Multi-reviewer scenarios with partial approvals
- Deadline expiration and extension flows
- Database transaction integrity
- Real PostgreSQL interactions with test data

**Manual Testing Scenarios:**
1. Request review with 3 reviewers, 2 required approvals
2. Approve as 2 reviewers → verify auto-lock
3. Request changes as 1 reviewer → verify status returns to Draft
4. Set deadline 1 minute in future → wait → verify notification
5. Extend deadline → verify notification doesn't trigger prematurely

### Latest Technical Specifics

**ASP.NET Core Background Services (for deadline monitoring):**
- Use `BackgroundService` base class
- Implement `ExecuteAsync` with periodic timer (check every 5 minutes)
- Register in `Program.cs` with `builder.Services.AddHostedService<ReviewDeadlineMonitorService>()`
- Graceful shutdown support via CancellationToken

**SignalR for Real-Time Notifications (recommended):**
- Reuse existing ChatHub infrastructure if available
- Send targeted messages to specific users: `Clients.User(userId).SendAsync("ReviewNotification", payload)`
- No additional packages needed if SignalR already configured

**FluentValidation Patterns:**
```csharp
public class RequestReviewRequestValidator : AbstractValidator<RequestReviewRequest>
{
    public RequestReviewRequestValidator()
    {
        RuleFor(x => x.ReviewerIds)
            .NotEmpty().WithMessage("At least one reviewer required")
            .Must(ids => ids.Distinct().Count() == ids.Count).WithMessage("Duplicate reviewers not allowed");
        
        RuleFor(x => x.RequiredApprovals)
            .GreaterThan(0).WithMessage("Required approvals must be positive")
            .LessThanOrEqualTo(x => x.ReviewerIds.Count).WithMessage("Required approvals cannot exceed reviewer count");
        
        RuleFor(x => x.Deadline)
            .Must(d => !d.HasValue || d.Value > DateTime.UtcNow).WithMessage("Deadline must be in the future");
    }
}
```

**PostgreSQL-Specific Optimizations:**
- Use GIN indexes on JSONB fields if querying decision content during reviews
- Consider partial index: `CREATE INDEX idx_decisions_under_review ON decisions(id) WHERE status = 'UnderReview'`
- Use `ON DELETE CASCADE` for decision_reviews → decisions foreign key (cleanup on decision deletion)

### Project Structure Notes

**Aspire Integration:**
- All services must use `builder.AddServiceDefaults()` for consistent configuration
- PostgreSQL connection inherited from AppHost configuration (Story 1.2)
- Health checks automatically included for background services
- OpenTelemetry tracing will capture review workflow spans

**Naming Conventions (from existing codebase):**
- Entity classes: Singular nouns (Decision, DecisionReview)
- DTOs: Suffix with "Dto" or Request/Response (DecisionReviewDto, RequestReviewRequest)
- Services: Suffix with "Service" (DecisionService, NotificationService)
- Controllers: Suffix with "Controller", route: `/api/v1/{resource}`
- Test classes: Suffix with "Tests" (DecisionReviewServiceTests)

**Error Handling Patterns:**
- Use `Results<T>` pattern for service responses (success/failure)
- Controller maps to ProblemDetails with appropriate status codes
- Custom exceptions: NotFoundException, ValidationException, UnauthorizedException
- All exceptions logged with context (userId, decisionId, operation)

### Security & Authorization

**Role-Based Access Control:**
- **Requester:** Must be Participant or Admin to request review
- **Reviewer:** Must be valid user in system, any role can review if invited
- **Approver:** Only invited reviewers can approve/request changes
- **Viewer:** Cannot request or participate in reviews (read-only access)

**Authorization Checks:**
- Verify requester owns decision or is Admin before allowing review request
- Verify reviewer is in reviewers list before accepting approval/changes
- Prevent self-approval if requester is also a reviewer (optional business rule)
- Check decision status before allowing review actions (must be UnderReview)

**Audit Trail:**
- Log all review requests with requester and reviewers list
- Log all approvals and change requests with reviewer and timestamp
- Log auto-lock events triggered by review completion
- Log deadline extensions with reason

### Performance Considerations

**Query Optimization:**
- Eager load DecisionReviews when fetching Decision: `.Include(d => d.Reviews)`
- Use pagination for GET reviews endpoint if many reviewers possible
- Consider caching active reviews in Redis for frequent status checks (Phase 2)

**Concurrency Handling:**
- Use optimistic concurrency (Version field on Decision entity)
- Handle concurrent approvals gracefully (last approval might trigger lock)
- Prevent race condition: Check approval count atomically before auto-lock

**Background Job Efficiency:**
- Query only decisions with status=UnderReview and deadline soon (<5 min buffer)
- Batch notifications to reduce database round-trips
- Use indexed query with `WHERE status = 'UnderReview' AND review_deadline < NOW()`

### UI Integration Guidance

**Frontend Components Needed:**
1. **Request Review Dialog:**
   - Multi-select dropdown for reviewers (fetch from GET `/api/v1/users`)
   - Number input for required approvals (default to all reviewers)
   - Date-time picker for optional deadline
   - Submit button calling POST `/api/v1/decisions/{id}/request-review`

2. **Review Panel (for reviewers):**
   - Display decision content, requester name, deadline countdown
   - Two primary actions: "Approve" (green) and "Request Changes" (yellow)
   - Text area for comments (required for "Request Changes")
   - Show existing reviews with status icons (pending/approved/changes)

3. **Review Status Badge:**
   - Display on decision card: "Under Review (2/3 approvals)"
   - Click to expand review details panel
   - Color-coded: yellow for pending, green for approved, red for changes requested

4. **Notification Toast:**
   - Real-time notification when review requested: "Marcus requested your review on Decision #42"
   - Notification when review completed: "Decision #42 approved and locked"
   - Notification on deadline expiration: "Review deadline passed for Decision #42"

**API Call Examples:**

```typescript
// Request review
const requestReview = async (decisionId: string, reviewers: string[], requiredApprovals: number) => {
  const response = await fetch(`/api/v1/decisions/${decisionId}/request-review`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      reviewerIds: reviewers,
      requiredApprovals: requiredApprovals,
      deadline: new Date(Date.now() + 7 * 24 * 60 * 60 * 1000) // 7 days
    })
  });
  return response.json();
};

// Approve review
const approveReview = async (decisionId: string) => {
  const response = await fetch(`/api/v1/decisions/${decisionId}/reviews/approve`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' }
  });
  return response.json();
};

// Request changes
const requestChanges = async (decisionId: string, comments: string) => {
  const response = await fetch(`/api/v1/decisions/${decisionId}/reviews/request-changes`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ comments })
  });
  return response.json();
};
```

### References & Source Hints

- **Epic 6.4 Acceptance Criteria:** [epics.md lines 1971-2003](../planning-artifacts/epics.md)
- **Decision Tracking Architecture:** [architecture.md lines 276-282](../planning-artifacts/architecture.md)
- **Multi-User Collaboration Requirements:** [architecture.md lines 219-289](../planning-artifacts/architecture.md)
- **Notification Patterns:** Referenced in Epic 13 (Integrations & Webhooks)
- **Previous Story Patterns:** Stories 6.1, 6.2, 6.3 in implementation-artifacts folder


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

- Source: [epics.md - Story 6.4](../planning-artifacts/epics.md)
- Architecture: [architecture.md](../planning-artifacts/architecture.md)
- PRD: [prd.md](../planning-artifacts/prd.md)
