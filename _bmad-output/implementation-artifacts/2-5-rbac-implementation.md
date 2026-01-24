# Story 2.5: RBAC (Role-Based Access Control) Implementation

**Status:** ready-for-dev

## Story

As an administrator (Cris),
I want to assign roles to users (Admin, Participant, Viewer),
so that I can control who can perform specific actions in bmadServer workflows.

## Acceptance Criteria

**Given** the system defines three roles  
**When** I review the Role enum in the codebase  
**Then** I see:
```csharp
public enum Role
{
    Admin,      // Full system access, user management, workflow control
    Participant, // Can create/edit workflows, make decisions
    Viewer      // Read-only access, can view workflows but not modify
}
```

**Given** the UserRoles table exists  
**When** I run `dotnet ef migrations add AddUserRolesTable`  
**Then** the migration creates:
  - UserRoles table (UserId, Role, AssignedAt, AssignedBy)
  - Composite primary key on (UserId, Role) - users can have multiple roles
  - Foreign key to Users table
  - Index on UserId for fast role lookups

**Given** I register a new user  
**When** the registration completes  
**Then** the user is automatically assigned the "Participant" role by default  
**And** the UserRoles table has a record for this user

**Given** I am an Admin user  
**When** I send POST `/api/v1/users/{userId}/roles` with:
```json
{
  "role": "Admin"
}
```
**Then** the system validates I have Admin role  
**And** adds the specified role to the target user's UserRoles  
**And** returns 200 OK with updated user roles list

**Given** I am a Participant or Viewer  
**When** I attempt to assign roles via POST `/api/v1/users/{userId}/roles`  
**Then** the system returns 403 Forbidden  
**And** the error indicates "Admin role required for this operation"

**Given** I am authenticated  
**When** I send GET `/api/v1/users/me`  
**Then** the response includes my roles:
```json
{
  "id": "uuid",
  "email": "cris@example.com",
  "displayName": "Cris",
  "roles": ["Admin", "Participant"]
}
```

**Given** an endpoint requires Admin role  
**When** I add `[Authorize(Roles = "Admin")]` attribute to the endpoint  
**Then** the authorization middleware checks the JWT claims for "role" claim  
**And** allows access only if the user has Admin role  
**And** returns 403 Forbidden otherwise

**Given** an endpoint requires any of multiple roles  
**When** I add `[Authorize(Roles = "Admin,Participant")]` attribute  
**Then** users with either Admin OR Participant role can access  
**And** Viewer-only users are denied with 403 Forbidden

**Given** JWT tokens include role claims  
**When** a user logs in  
**Then** the generated JWT includes claims:
```json
{
  "sub": "user-uuid",
  "email": "cris@example.com",
  "role": ["Admin", "Participant"],
  "iat": 1234567890,
  "exp": 1234568790
}
```
**And** the JWT middleware automatically populates `User.IsInRole("Admin")`

**Given** I review protected endpoints  
**When** I check the OpenAPI/Swagger documentation  
**Then** endpoints show required roles in the security section  
**And** I can test role-based access directly from Swagger UI

## Tasks / Subtasks

- [ ] **Task 1: Create Role enum and UserRole entity** (AC: Role definitions)
  - [ ] Create `Models/Role.cs` enum with Admin, Participant, Viewer
  - [ ] Create `Models/UserRole.cs` entity with UserId, Role, AssignedAt, AssignedBy
  - [ ] Add navigation property to User entity for roles
  - [ ] Configure composite primary key (UserId, Role)
  - [ ] Add foreign key constraint to Users table
  - [ ] Add index on UserId for fast lookups

- [ ] **Task 2: Create UserRoles migration** (AC: Database schema)
  - [ ] Add DbSet<UserRole> to ApplicationDbContext
  - [ ] Configure entity relationships in OnModelCreating
  - [ ] Run `dotnet ef migrations add AddUserRolesTable`
  - [ ] Verify migration creates correct schema
  - [ ] Test migration up/down

- [ ] **Task 3: Update registration to assign default role** (AC: Default role)
  - [ ] Modify user registration service
  - [ ] Create UserRole record with Participant role on registration
  - [ ] Wrap in transaction with user creation
  - [ ] Verify in unit tests

- [ ] **Task 4: Add role claims to JWT generation** (AC: JWT claims)
  - [ ] Update TokenService to include role claims
  - [ ] Query UserRoles table during token generation
  - [ ] Add multiple role claims for users with multiple roles
  - [ ] Verify claims appear in decoded JWT
  - [ ] Test with jwt.io or similar tool

- [ ] **Task 5: Create role assignment API** (AC: Admin role management)
  - [ ] Create `Controllers/RolesController.cs`
  - [ ] Implement POST `/api/v1/users/{userId}/roles`
  - [ ] Implement DELETE `/api/v1/users/{userId}/roles/{role}`
  - [ ] Implement GET `/api/v1/users/{userId}/roles`
  - [ ] Add `[Authorize(Roles = "Admin")]` attribute
  - [ ] Return 403 for non-Admin users
  - [ ] Return 404 for non-existent users

- [ ] **Task 6: Update user profile endpoint** (AC: Role visibility)
  - [ ] Update GET `/api/v1/users/me` to include roles
  - [ ] Query UserRoles table for current user
  - [ ] Return roles array in response DTO
  - [ ] Update UserResponseDto to include roles

- [ ] **Task 7: Configure authorization middleware** (AC: Role-based access)
  - [ ] Verify JWT middleware reads role claims
  - [ ] Ensure `User.IsInRole()` works correctly
  - [ ] Add role policy definitions if needed
  - [ ] Test `[Authorize(Roles = "Admin")]` attribute
  - [ ] Test `[Authorize(Roles = "Admin,Participant")]` multiple roles

- [ ] **Task 8: Update OpenAPI documentation** (AC: Swagger)
  - [ ] Add security scheme for roles to Swagger
  - [ ] Document required roles on each endpoint
  - [ ] Test role-based access from Swagger UI
  - [ ] Verify 403 responses show in documentation

- [ ] **Task 9: Write integration tests** (AC: Verification)
  - [ ] Test default role assignment on registration
  - [ ] Test Admin can assign roles
  - [ ] Test non-Admin cannot assign roles
  - [ ] Test role claims in JWT
  - [ ] Test `[Authorize(Roles)]` attribute enforcement

## Dev Notes

### Role Entity Model

```csharp
// Models/Role.cs
public enum Role
{
    Admin,      // Full system access, user management, workflow control
    Participant, // Can create/edit workflows, make decisions
    Viewer      // Read-only access, can view workflows but not modify
}

// Models/UserRole.cs
public class UserRole
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Role Role { get; set; }
    public DateTime AssignedAt { get; set; }
    public Guid? AssignedBy { get; set; } // null for system-assigned defaults
}
```

### Entity Framework Configuration

```csharp
// In ApplicationDbContext.OnModelCreating
modelBuilder.Entity<UserRole>(entity =>
{
    // Composite primary key
    entity.HasKey(e => new { e.UserId, e.Role });
    
    // Index for fast lookups
    entity.HasIndex(e => e.UserId);
    
    // Foreign key to Users
    entity.HasOne(e => e.User)
        .WithMany(u => u.UserRoles)
        .HasForeignKey(e => e.UserId)
        .OnDelete(DeleteBehavior.Cascade);
    
    // Store enum as string for readability
    entity.Property(e => e.Role)
        .HasConversion<string>();
});
```

### JWT Claims Generation

```csharp
// In TokenService.GenerateAccessToken
public string GenerateAccessToken(User user, IEnumerable<Role> roles)
{
    var claims = new List<Claim>
    {
        new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
        new Claim(JwtRegisteredClaimNames.Email, user.Email),
        new Claim("displayName", user.DisplayName ?? ""),
    };
    
    // Add role claims - each role as separate claim
    foreach (var role in roles)
    {
        claims.Add(new Claim(ClaimTypes.Role, role.ToString()));
    }
    
    // ... rest of token generation
}
```

### Role Assignment API

```csharp
// Controllers/RolesController.cs
[ApiController]
[Route("api/v1/users/{userId}/roles")]
public class RolesController : ControllerBase
{
    private readonly IRoleService _roleService;
    
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AssignRole(
        Guid userId, 
        [FromBody] AssignRoleRequest request)
    {
        var adminId = GetUserIdFromClaims();
        await _roleService.AssignRoleAsync(userId, request.Role, adminId);
        
        var roles = await _roleService.GetUserRolesAsync(userId);
        return Ok(new { roles = roles.Select(r => r.ToString()) });
    }
    
    [HttpDelete("{role}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RemoveRole(Guid userId, Role role)
    {
        // Prevent removing last role
        // Prevent removing Admin role from self
        await _roleService.RemoveRoleAsync(userId, role);
        return NoContent();
    }
}
```

### Authorization Policy (Alternative Approach)

```csharp
// In Program.cs - for more complex authorization scenarios
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdmin", policy =>
        policy.RequireRole("Admin"));
    
    options.AddPolicy("RequireParticipantOrAdmin", policy =>
        policy.RequireRole("Admin", "Participant"));
    
    options.AddPolicy("CanEditWorkflows", policy =>
        policy.RequireAssertion(context =>
            context.User.IsInRole("Admin") || 
            context.User.IsInRole("Participant")));
});

// Usage: [Authorize(Policy = "RequireParticipantOrAdmin")]
```

### User Navigation Property

```csharp
// Update Models/User.cs
public class User
{
    // ... existing properties
    
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    
    // Convenience property
    public IEnumerable<Role> Roles => UserRoles.Select(ur => ur.Role);
}
```

### Role Service Interface

```csharp
// Services/IRoleService.cs
public interface IRoleService
{
    Task<IEnumerable<Role>> GetUserRolesAsync(Guid userId);
    Task AssignRoleAsync(Guid userId, Role role, Guid assignedBy);
    Task RemoveRoleAsync(Guid userId, Role role);
    Task AssignDefaultRoleAsync(Guid userId); // Called during registration
}
```

### Architecture Alignment

Per architecture.md requirements:
- Authentication: JWT-based with role claims
- Authorization: ASP.NET Core `[Authorize(Roles = "...")]` attribute
- Data: PostgreSQL with EF Core
- API: RESTful with OpenAPI documentation

### Dependencies

- Microsoft.AspNetCore.Authentication.JwtBearer (already added)
- No additional packages required

## Files to Create/Modify

### New Files
- `bmadServer.ApiService/Models/Role.cs`
- `bmadServer.ApiService/Models/UserRole.cs`
- `bmadServer.ApiService/Services/IRoleService.cs`
- `bmadServer.ApiService/Services/RoleService.cs`
- `bmadServer.ApiService/Controllers/RolesController.cs`
- `bmadServer.ApiService/DTOs/AssignRoleRequest.cs`
- `bmadServer.ApiService/Data/Migrations/YYYYMMDD_AddUserRolesTable.cs`

### Modified Files
- `bmadServer.ApiService/Models/User.cs` - Add UserRoles navigation property
- `bmadServer.ApiService/Data/ApplicationDbContext.cs` - Add DbSet<UserRole>, configure entity
- `bmadServer.ApiService/Services/TokenService.cs` - Include role claims in JWT
- `bmadServer.ApiService/Services/AuthService.cs` - Assign default role on registration
- `bmadServer.ApiService/Controllers/UsersController.cs` - Include roles in `/users/me` response
- `bmadServer.ApiService/DTOs/UserResponseDto.cs` - Add roles array

## References

- Source: [epics.md - Story 2.5](../_bmad-output/planning-artifacts/epics.md)
- Architecture: [architecture.md](../_bmad-output/planning-artifacts/architecture.md) - Security section
- PRD: [prd.md](../_bmad-output/planning-artifacts/prd.md) - FR31 (user/permission management)
