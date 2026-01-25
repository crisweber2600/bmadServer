# Story 10.1: Graceful Error Handling

**Status:** ready-for-dev

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a user (Sarah), I want errors to be handled gracefully, so that I understand what went wrong and what to do next.

## Acceptance Criteria

**Given** an API error occurs  
**When** the error response is returned  
**Then** it follows ProblemDetails RFC 7807 format with: type, title, status, detail, instance

**Given** a validation error occurs  
**When** I submit invalid data  
**Then** the response includes field-level errors  
**And** each error has: field, message, code

**Given** an internal server error occurs  
**When** the error is logged  
**Then** the user sees: "Something went wrong. Please try again." (not stack trace)  
**And** full details are logged server-side with correlationId

**Given** I experience an error  
**When** I see the error message  
**Then** I see actionable guidance: "Try again", "Contact support", "Check your input"

**Given** errors are tracked  
**When** I check monitoring  
**Then** error rates, types, and trends are visible in Grafana dashboard

## Tasks / Subtasks

- [ ] Task 1: Enhance Global Exception Handling Middleware (AC: 1, 3, 4)
  - [ ] Create ExceptionHandlingMiddleware with RFC 7807 ProblemDetails
  - [ ] Add correlation ID generation and tracking
  - [ ] Implement user-friendly error messages for 500 errors
  - [ ] Add actionable guidance based on error type
  - [ ] Configure exception middleware in Program.cs
  - [ ] Add OpenTelemetry tracing for all exceptions
- [ ] Task 2: Standardize Validation Error Responses (AC: 2)
  - [ ] Create ValidationProblemDetailsFactory for consistent field-level errors
  - [ ] Ensure all FluentValidation errors follow standard format
  - [ ] Add error codes to validation failures
  - [ ] Update existing controllers to use standardized validation
- [ ] Task 3: Implement Structured Error Logging (AC: 3)
  - [ ] Configure structured JSON logging with correlation IDs
  - [ ] Add error context (user ID, request path, timestamp)
  - [ ] Ensure sensitive data is not logged (passwords, tokens)
  - [ ] Configure log retention and rotation
- [ ] Task 4: Add Error Monitoring and Metrics (AC: 5)
  - [ ] Configure Prometheus metrics for error rates
  - [ ] Add error type counters and labels
  - [ ] Create Grafana dashboard queries for error visualization
  - [ ] Add alerting thresholds for error rate spikes
  - [ ] Document dashboard access and queries
- [ ] Task 5: Testing and Validation
  - [ ] Write unit tests for exception middleware
  - [ ] Write integration tests for error scenarios
  - [ ] Test validation error formatting
  - [ ] Verify correlation IDs in logs
  - [ ] Manual testing of error responses in UI

## Dev Notes

### 🎯 CRITICAL IMPLEMENTATION REQUIREMENTS

#### Existing ProblemDetails Infrastructure

**IMPORTANT:** bmadServer ALREADY has ProblemDetails configured!

```csharp
// src/bmadServer.ApiService/Program.cs line 40-43
// Add structured exception handling with RFC 7807 problem details format
// This provides consistent error responses across all API endpoints
// Errors will be returned as JSON with status code, title, detail, and trace ID
builder.Services.AddProblemDetails();
```

**Existing Pattern in AuthController (lines 77-84, 93-99):**
```csharp
// Validation error pattern
return ValidationProblem(
    new ValidationProblemDetails(errors)
    {
        Type = "https://bmadserver.dev/errors/validation-error",
        Title = "Validation Failed",
        Status = StatusCodes.Status400BadRequest,
        Detail = "One or more validation errors occurred"
    });

// Conflict error pattern
return Conflict(new ProblemDetails
{
    Type = "https://bmadserver.dev/errors/user-exists",
    Title = "User Already Exists",
    Status = StatusCodes.Status409Conflict,
    Detail = "A user with this email already exists"
});
```

### 🏗️ Architecture Context

**Project Structure:**
- **API Service:** `src/bmadServer.ApiService/`
- **Controllers:** `src/bmadServer.ApiService/Controllers/`
- **Services:** `src/bmadServer.ApiService/Services/`
- **Models/DTOs:** `src/bmadServer.ApiService/Models/` or `DTOs/`
- **Configuration:** `src/bmadServer.ApiService/Configuration/`
- **Middleware:** Create new folder `src/bmadServer.ApiService/Middleware/`
- **Tests:** `src/bmadServer.Tests/Integration/` and `src/bmadServer.Tests/Unit/Services/`

**Technology Stack (from architecture.md):**
- .NET 10 with ASP.NET Core
- Aspire for orchestration and service defaults
- FluentValidation 11.9.2 (already in use)
- OpenTelemetry (configured via Aspire ServiceDefaults)
- PostgreSQL 17.x with EF Core 9.0
- Prometheus + Grafana for monitoring

**Aspire Service Defaults (Program.cs line 10-17):**
```csharp
// Add Aspire service defaults: health checks, telemetry (logging + tracing), service discovery
// - OpenTelemetry with structured JSON logging
// - Distributed tracing with trace ID support
// - Health check endpoints at /health and /alive
builder.AddServiceDefaults();
```

### 📋 Implementation Checklist

#### 1. Create Global Exception Handling Middleware

**File:** `src/bmadServer.ApiService/Middleware/ExceptionHandlingMiddleware.cs`

**Requirements:**
- Catch all unhandled exceptions
- Generate correlation ID if not present (use Activity.Current?.Id or Guid)
- Log full exception details with correlation ID, user ID, request path
- Return RFC 7807 ProblemDetails to client
- Map exception types to appropriate HTTP status codes
- Add user-friendly messages and actionable guidance
- Integrate with OpenTelemetry tracing (use Activity.Current)
- DO NOT expose stack traces or sensitive data to clients

**Pattern to Follow:**
```csharp
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // Implementation here
    }
}
```

**Register in Program.cs:**
```csharp
// After app.Build() but before app.MapDefaultEndpoints()
app.UseMiddleware<ExceptionHandlingMiddleware>();
```

#### 2. Standardize Validation Errors Across All Controllers

**Current Controllers to Update:**
- `AuthController.cs` (pattern already good, ensure consistency)
- `ChatController.cs`
- `RolesController.cs`
- `UsersController.cs`
- `WorkflowsController.cs`

**Standard Format:**
```csharp
var errors = validationResult.Errors
    .GroupBy(e => e.PropertyName.ToLowerInvariant())
    .ToDictionary(
        g => g.Key,
        g => g.Select(e => e.ErrorMessage).ToArray()
    );

return ValidationProblem(
    new ValidationProblemDetails(errors)
    {
        Type = "https://bmadserver.dev/errors/validation-error",
        Title = "Validation Failed",
        Status = StatusCodes.Status400BadRequest,
        Detail = "One or more validation errors occurred.",
        Instance = context.Request.Path
    });
```

**Error Codes:** Add error codes to FluentValidation rules using `.WithErrorCode("ERR_001")`

#### 3. Structured Logging Configuration

**Aspire already provides:**
- OpenTelemetry with structured JSON logging
- Trace ID support via Activity.Current
- Distributed tracing

**What to add:**
- Ensure correlation IDs are included in all error logs
- Add user context (user ID from JWT claims)
- Filter sensitive data (use log redaction)

**Logging Pattern:**
```csharp
_logger.LogError(exception, 
    "Unhandled exception occurred. CorrelationId: {CorrelationId}, UserId: {UserId}, Path: {Path}",
    correlationId, userId, requestPath);
```

#### 4. Prometheus Metrics for Error Tracking

**Aspire Integration:**
- Aspire ServiceDefaults already configures OpenTelemetry metrics
- Prometheus scraping is available via `/metrics` endpoint

**Metrics to Add:**
```csharp
using System.Diagnostics.Metrics;

public class ErrorMetrics
{
    private static readonly Meter Meter = new("bmadServer.Errors", "1.0.0");
    private static readonly Counter<int> ErrorCounter = Meter.CreateCounter<int>("errors_total", 
        description: "Total number of errors by type");
    private static readonly Histogram<double> ErrorDuration = Meter.CreateHistogram<double>("error_handling_duration_seconds",
        description: "Duration of error handling in seconds");

    public static void RecordError(string errorType, int statusCode)
    {
        ErrorCounter.Add(1, new KeyValuePair<string, object?>("type", errorType), 
                            new KeyValuePair<string, object?>("status", statusCode));
    }
}
```

#### 5. Grafana Dashboard Queries

**Create Grafana Dashboard with Panels:**

1. **Error Rate Panel:**
```promql
rate(errors_total[5m])
```

2. **Errors by Type:**
```promql
sum by (type) (errors_total)
```

3. **Error Rate by Status Code:**
```promql
sum by (status) (rate(errors_total[5m]))
```

4. **Error Trend (Last 24h):**
```promql
increase(errors_total[1h])
```

**Dashboard Configuration:**
- Save to Grafana instance (configured in Aspire AppHost or docker-compose)
- Set refresh interval to 30s
- Add alerts for error rate > 10/min

### 🧪 Testing Strategy

#### Unit Tests

**File:** `src/bmadServer.Tests/Unit/Middleware/ExceptionHandlingMiddlewareTests.cs`

Test Cases:
- Exception results in 500 status code
- ProblemDetails contains required fields
- Correlation ID is generated
- Stack trace is NOT included in response
- Actionable guidance is included
- Logger is called with correct parameters

#### Integration Tests

**File:** `src/bmadServer.Tests/Integration/ErrorHandlingIntegrationTests.cs`

Test Cases:
- Unhandled exception returns ProblemDetails
- Validation errors return 400 with field-level errors
- Database connection errors return 500 with generic message
- Unauthorized requests return 401 with ProblemDetails
- NotFound errors return 404 with ProblemDetails
- Correlation ID is present in response headers
- Error metrics are recorded in Prometheus

### 🔐 Security Considerations

**DO NOT expose:**
- Stack traces (except in Development environment for debugging)
- Database connection strings
- Internal file paths
- Sensitive user data
- JWT secrets or tokens

**DO log (server-side only):**
- Full exception details
- Stack traces
- Request context
- User ID
- Correlation ID

### 📊 Error Type Mapping

| Exception Type | HTTP Status | User Message | Log Level |
|----------------|-------------|--------------|-----------|
| ValidationException | 400 | Field-level errors | Warning |
| UnauthorizedAccessException | 401 | "Please log in to continue" | Warning |
| DbUpdateConcurrencyException | 409 | "Data was modified by another user. Please refresh and try again." | Warning |
| DbUpdateException | 500 | "Unable to save changes. Please try again." | Error |
| TimeoutException | 504 | "Request timed out. Please try again." | Error |
| All Others | 500 | "Something went wrong. Please try again." | Error |

### 📚 Actionable Guidance Examples

- **500 Error:** "Something went wrong. Please try again. If the problem persists, contact support."
- **400 Validation:** "Please check your input and try again."
- **401 Unauthorized:** "Please log in to continue."
- **404 Not Found:** "The requested resource was not found. Please check the URL."
- **409 Conflict:** "Data was modified by another user. Please refresh and try again."
- **503 Service Unavailable:** "Service is temporarily unavailable. Please try again in a few moments."

### 🔗 Dependencies

**Required NuGet Packages (Already Installed):**
- Microsoft.AspNetCore.Mvc.Core (ProblemDetails support)
- FluentValidation.AspNetCore 11.9.2
- System.Diagnostics.DiagnosticSource (OpenTelemetry)

**No New Packages Needed** - Everything is already available!

### 📂 Files to Create/Modify

**New Files:**
1. `src/bmadServer.ApiService/Middleware/ExceptionHandlingMiddleware.cs`
2. `src/bmadServer.ApiService/Services/ErrorMetrics.cs` (optional, can be inline)
3. `src/bmadServer.Tests/Unit/Middleware/ExceptionHandlingMiddlewareTests.cs`
4. `src/bmadServer.Tests/Integration/ErrorHandlingIntegrationTests.cs`

**Modify Existing Files:**
1. `src/bmadServer.ApiService/Program.cs` - Register middleware
2. `src/bmadServer.ApiService/Controllers/ChatController.cs` - Standardize error responses
3. `src/bmadServer.ApiService/Controllers/RolesController.cs` - Standardize error responses
4. `src/bmadServer.ApiService/Controllers/UsersController.cs` - Standardize error responses
5. `src/bmadServer.ApiService/Controllers/WorkflowsController.cs` - Standardize error responses

**Note:** AuthController.cs already has good ProblemDetails patterns - use it as reference!

---

## Aspire Development Standards

### Rule 1: Use Aspire Service Defaults

This story leverages Aspire's built-in observability:
- `builder.AddServiceDefaults()` provides OpenTelemetry logging and tracing
- Correlation IDs are automatically available via `Activity.Current?.Id`
- Structured JSON logging is pre-configured
- Prometheus metrics endpoint is available at `/metrics`

**Reference:** [PROJECT-WIDE-RULES.md](../../../PROJECT-WIDE-RULES.md) - Rule 4: OpenTelemetry from Day 1

### Rule 2: Documentation Sources

**Primary:** https://aspire.dev/docs/fundamentals/observability/
**Secondary:** https://github.com/microsoft/aspire/tree/main/src/Aspire.Hosting.Azure/

### PostgreSQL Connection Pattern

This story uses PostgreSQL configured in Story 1.2 via Aspire:
- Connection string automatically injected from Aspire AppHost
- Pattern: `builder.AddServiceDefaults();` (inherits PostgreSQL reference)
- See Story 1.2 for AppHost configuration pattern

---

## 🎓 Learning from Previous Stories

### Story 4.7 Insights (Workflow Status & Progress API)

**Good Patterns:**
- ProblemDetails already in use
- FluentValidation integration working well
- OpenTelemetry tracing configured

**Lessons:**
- JSON serialization needs testing for complex types
- Integration tests should cover error scenarios
- Documentation updates are important

### Story 2.1 Insights (User Authentication)

**Good Patterns:**
- BCrypt for password hashing with proper work factor
- JWT token generation and refresh flows
- Timing attack prevention in login endpoint

**Error Handling Patterns to Maintain:**
- ValidationProblemDetails for bad requests
- ProblemDetails for business logic errors
- Consistent error message format

---

## References

- **Aspire Rules:** [PROJECT-WIDE-RULES.md](../../../PROJECT-WIDE-RULES.md)
- **Aspire Docs:** https://aspire.dev
- **RFC 7807 ProblemDetails:** https://datatracker.ietf.org/doc/html/rfc7807
- **Source:** [epics.md - Epic 10, Story 10.1](../planning-artifacts/epics.md#story-101-graceful-error-handling)
- **Architecture:** [architecture.md](../planning-artifacts/architecture.md) - Error Handling section
- **PRD:** [prd.md](../planning-artifacts/prd.md) - NFR5 (< 5% failures)

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
