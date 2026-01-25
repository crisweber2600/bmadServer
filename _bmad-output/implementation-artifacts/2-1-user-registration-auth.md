# Story 2.1: User Registration & Local Database Authentication

**Status:** ready-for-dev

## Story

As a new user (Sarah, non-technical co-founder),
I want to create an account with email and password,
so that I can securely access bmadServer and start using BMAD workflows.

## Acceptance Criteria

**Given** the bmadServer API is running  
**When** I send a POST request to `/api/v1/auth/register` with valid registration data:
```json
{
  "email": "sarah@example.com",
  "password": "SecurePass123!",
  "displayName": "Sarah Johnson"
}
```
**Then** the system creates a new user record in the PostgreSQL Users table  
**And** the password is hashed using bcrypt (cost factor 12)  
**And** the response returns 201 Created with user details (excluding password hash):
```json
{
  "id": "uuid",
  "email": "sarah@example.com",
  "displayName": "Sarah Johnson",
  "createdAt": "2026-01-23T10:00:00Z"
}
```

**Given** I attempt to register with an email that already exists  
**When** I send POST `/api/v1/auth/register` with duplicate email  
**Then** the system returns 409 Conflict with ProblemDetails:
```json
{
  "type": "https://bmadserver.dev/errors/user-exists",
  "title": "User Already Exists",
  "status": 409,
  "detail": "A user with this email already exists"
}
```

**Given** I attempt to register with invalid data  
**When** I send POST `/api/v1/auth/register` with:
  - Invalid email format (missing @, invalid domain)
  - Weak password (< 8 characters, no special characters)
  - Missing required fields
**Then** the system returns 400 Bad Request with validation errors using ProblemDetails  
**And** the response includes specific field-level error messages

**Given** the Users table does not exist  
**When** I run `dotnet ef migrations add AddUsersTable`  
**Then** an EF Core migration is generated with:
  - Users table (Id, Email, PasswordHash, DisplayName, CreatedAt, UpdatedAt)
  - Unique index on Email column
  - Check constraint on Email format (basic validation)

**Given** I run `dotnet ef database update`  
**When** the migration executes  
**Then** the database schema is created in PostgreSQL  
**And** I can query `SELECT * FROM users` successfully  
**And** the Email column has a unique constraint enforced

**Given** registration endpoint is exposed  
**When** I check OpenAPI documentation at `/swagger`  
**Then** I see POST `/api/v1/auth/register` endpoint documented  
**And** request/response schemas are clearly defined  
**And** validation rules are documented (password requirements, email format)

## Tasks / Subtasks

- [ ] **Task 1: Create User entity and DbContext configuration** (AC: Database schema)
  - [ ] Create `Models/User.cs` entity class with Id, Email, PasswordHash, DisplayName, CreatedAt, UpdatedAt
  - [ ] Add DbSet<User> to ApplicationDbContext
  - [ ] Configure entity in OnModelCreating (indexes, constraints)
  - [ ] Add unique index on Email column
  - [ ] Configure Id as UUID/GUID with database-generated values

- [ ] **Task 2: Generate EF Core migration for Users table** (AC: Migration criteria)
  - [ ] Run `dotnet ef migrations add AddUsersTable`
  - [ ] Review generated migration file for correctness
  - [ ] Verify CREATE TABLE includes all columns with proper types
  - [ ] Run `dotnet ef database update` to apply migration
  - [ ] Verify table exists with `SELECT * FROM users`

- [ ] **Task 3: Implement password hashing service** (AC: bcrypt hashing)
  - [ ] Add BCrypt.Net-Next NuGet package
  - [ ] Create `Services/IPasswordHasher.cs` interface
  - [ ] Create `Services/PasswordHasher.cs` implementation using bcrypt (cost factor 12)
  - [ ] Register service in DI container
  - [ ] Write unit tests for hash generation and verification

- [ ] **Task 4: Create registration DTO and validation** (AC: Validation criteria)
  - [ ] Create `DTOs/RegisterRequest.cs` with Email, Password, DisplayName
  - [ ] Create `DTOs/UserResponse.cs` for response (excludes PasswordHash)
  - [ ] Add FluentValidation validators for:
    - Email format validation (regex)
    - Password strength (min 8 chars, special char, number)
    - DisplayName required, max 100 chars
  - [ ] Register validators in DI container

- [ ] **Task 5: Implement registration endpoint** (AC: All registration criteria)
  - [ ] Create `Controllers/AuthController.cs`
  - [ ] Add POST `/api/v1/auth/register` endpoint
  - [ ] Check for existing email (return 409 Conflict if exists)
  - [ ] Hash password using IPasswordHasher
  - [ ] Create User entity and save to database
  - [ ] Return 201 Created with UserResponse DTO
  - [ ] Add proper error handling with ProblemDetails

- [ ] **Task 6: Configure OpenAPI documentation** (AC: Swagger documentation)
  - [ ] Add XML documentation comments to controller and DTOs
  - [ ] Configure Swagger to include validation rules in schema
  - [ ] Verify endpoint appears in /swagger UI
  - [ ] Test endpoint from Swagger UI

## Dev Notes

### Entity Model

```csharp
// Models/User.cs
public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation properties for future stories
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
```

### Password Hashing Configuration

```csharp
// Services/PasswordHasher.cs
public class PasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12; // Cost factor per architecture.md
    
    public string Hash(string password) => 
        BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
    
    public bool Verify(string password, string hash) => 
        BCrypt.Net.BCrypt.Verify(password, hash);
}
```

### Validation Rules

```csharp
// Validators/RegisterRequestValidator.cs
public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");
            
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain a special character")
            .Matches("[0-9]").WithMessage("Password must contain a number");
            
        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("Display name is required")
            .MaximumLength(100).WithMessage("Display name must be 100 characters or less");
    }
}
```

### ProblemDetails Error Format

Per architecture.md, all errors use RFC 7807 ProblemDetails format:

```csharp
// Error response example
{
  "type": "https://bmadserver.dev/errors/validation-error",
  "title": "Validation Failed",
  "status": 400,
  "detail": "One or more validation errors occurred",
  "errors": {
    "password": ["Password must be at least 8 characters"]
  }
}
```

### Architecture Alignment

Per architecture.md requirements:
- Authentication: Hybrid (Local DB MVP + OpenID Connect Ready Phase 2) - this story implements Local DB
- Validation: EF Core Annotations + FluentValidation 11.9.2
- Error Handling: ProblemDetails RFC 7807
- API Versioning: URL Path /api/v1/

### Dependencies

- BCrypt.Net-Next (NuGet) - password hashing
- FluentValidation.AspNetCore (NuGet) - request validation
- Microsoft.EntityFrameworkCore.Design (NuGet) - EF migrations

## Files to Create/Modify

### New Files
- `bmadServer.ApiService/Models/User.cs`
- `bmadServer.ApiService/DTOs/RegisterRequest.cs`
- `bmadServer.ApiService/DTOs/UserResponse.cs`
- `bmadServer.ApiService/Services/IPasswordHasher.cs`
- `bmadServer.ApiService/Services/PasswordHasher.cs`
- `bmadServer.ApiService/Validators/RegisterRequestValidator.cs`
- `bmadServer.ApiService/Controllers/AuthController.cs`
- `bmadServer.ApiService/Data/Migrations/YYYYMMDD_AddUsersTable.cs`

### Modified Files
- `bmadServer.ApiService/Data/ApplicationDbContext.cs` - Add DbSet<User>
- `bmadServer.ApiService/Program.cs` - Register services, validators
- `bmadServer.ApiService/bmadServer.ApiService.csproj` - Add NuGet packages

---

## Aspire Development Standards

### PostgreSQL Connection Pattern

This story uses PostgreSQL configured in Story 1.2 via Aspire:
- Connection string automatically injected from Aspire AppHost
- Pattern: `builder.AddServiceDefaults();` (inherits PostgreSQL reference)
- EF Core migrations run against Aspire-managed PostgreSQL
- See Story 1.2 for AppHost configuration pattern

### Project-Wide Standards

This story follows the Aspire-first development pattern:
- **Reference:** [PROJECT-WIDE-RULES.md](../../../PROJECT-WIDE-RULES.md)
- **Primary Documentation:** https://aspire.dev
- **GitHub:** https://github.com/microsoft/aspire

### Aspire-Specific Notes

- Database connection string auto-injected via `IConnectionStringProvider`
- Health checks inherited from `ServiceDefaults`
- Structured logging via OpenTelemetry (Aspire Dashboard visible at https://localhost:17360)
- No manual environment variables needed - Aspire handles service discovery

---

## References

- Source: [epics.md - Story 2.1](../planning-artifacts/epics.md)
- Architecture: [architecture.md](../planning-artifacts/architecture.md) - Authentication section
- PRD: [prd.md](../planning-artifacts/prd.md) - FR16, FR17
- **Aspire Rules:** [PROJECT-WIDE-RULES.md](../../../PROJECT-WIDE-RULES.md)
- **Aspire Docs:** https://aspire.dev
