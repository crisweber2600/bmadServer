# Story 2.3: Refresh Token Flow with HttpOnly Cookies

**Status:** ready-for-dev

## Story

As a logged-in user (Sarah),
I want my session to be automatically extended without re-entering credentials,
so that I can work continuously without interruptions while maintaining security.

## Acceptance Criteria

**Given** I successfully login  
**When** I receive the login response  
**Then** the system generates a refresh token (UUID v4)  
**And** stores it in the RefreshTokens table with:
  - Token (hashed with SHA256)
  - UserId (foreign key to Users)
  - ExpiresAt (7 days from creation)
  - CreatedAt, RevokedAt (nullable)
**And** sets an HttpOnly cookie in the response:
```
Set-Cookie: refreshToken={token}; HttpOnly; Secure; SameSite=Strict; Path=/api/v1/auth/refresh; Max-Age=604800
```

**Given** my access token is about to expire (< 2 minutes remaining)  
**When** my client sends POST `/api/v1/auth/refresh` with the refresh token cookie  
**Then** the system validates the refresh token:
  - Token exists in database and is not revoked
  - Token has not expired (< 7 days old)
  - Token hash matches stored hash
**And** generates a new access token (15-minute expiry)  
**And** rotates the refresh token (invalidates old, creates new)  
**And** returns 200 OK with new access token and refresh cookie

**Given** I send a refresh request with an expired refresh token  
**When** I call POST `/api/v1/auth/refresh` with expired token  
**Then** the system returns 401 Unauthorized  
**And** the error message indicates "Refresh token expired. Please login again."  
**And** the old refresh token is marked as revoked in the database

**Given** I send a refresh request with a revoked refresh token  
**When** I call POST `/api/v1/auth/refresh` with previously used token  
**Then** the system returns 401 Unauthorized  
**And** the error indicates "Invalid refresh token"  
**And** all refresh tokens for this user are revoked (security breach detection)

**Given** I send concurrent refresh requests (race condition test)  
**When** two requests arrive within 100ms using the same refresh token  
**Then** only one request succeeds with new tokens  
**And** the second request fails with 401 Unauthorized  
**And** token rotation happens atomically (database transaction)

**Given** I logout from the application  
**When** I send POST `/api/v1/auth/logout` with my refresh token cookie  
**Then** the system revokes my refresh token in the database  
**And** clears the refresh token cookie:
```
Set-Cookie: refreshToken=; HttpOnly; Secure; SameSite=Strict; Path=/api/v1/auth/refresh; Max-Age=0
```
**And** returns 204 No Content

**Given** the RefreshTokens table migration is created  
**When** I run `dotnet ef migrations add AddRefreshTokensTable`  
**Then** the migration includes:
  - RefreshTokens table (Id, TokenHash, UserId, ExpiresAt, CreatedAt, RevokedAt)
  - Foreign key constraint to Users table
  - Index on TokenHash for fast lookups
  - Index on UserId for user-specific queries

**Given** security configuration is reviewed  
**When** I check the cookie settings in production  
**Then** I verify:
  - HttpOnly flag prevents JavaScript access
  - Secure flag enforces HTTPS-only transmission
  - SameSite=Strict prevents CSRF attacks
  - Path=/api/v1/auth/refresh limits cookie scope

## Tasks / Subtasks

- [ ] **Task 1: Create RefreshToken entity and migration** (AC: Database schema)
  - [ ] Create `Models/RefreshToken.cs` entity
  - [ ] Add DbSet<RefreshToken> to ApplicationDbContext
  - [ ] Configure entity relationships and indexes in OnModelCreating
  - [ ] Run `dotnet ef migrations add AddRefreshTokensTable`
  - [ ] Apply migration with `dotnet ef database update`
  - [ ] Verify table structure in PostgreSQL

- [ ] **Task 2: Implement refresh token service** (AC: Token generation/validation)
  - [ ] Create `Services/IRefreshTokenService.cs` interface
  - [ ] Create `Services/RefreshTokenService.cs` implementation
  - [ ] Implement GenerateRefreshToken() - UUID v4 generation
  - [ ] Implement HashToken() - SHA256 hashing
  - [ ] Implement StoreRefreshToken() - save to database
  - [ ] Implement ValidateRefreshToken() - check validity
  - [ ] Implement RevokeRefreshToken() - mark as revoked
  - [ ] Implement RevokeAllUserTokens() - security breach response
  - [ ] Write unit tests for all methods

- [ ] **Task 3: Update login to issue refresh token** (AC: Login response with cookie)
  - [ ] Modify login endpoint to generate refresh token
  - [ ] Store hashed refresh token in database
  - [ ] Set HttpOnly cookie with proper attributes
  - [ ] Configure cookie options for security
  - [ ] Test cookie is set correctly in response

- [ ] **Task 4: Implement refresh endpoint** (AC: Token refresh criteria)
  - [ ] Add POST `/api/v1/auth/refresh` endpoint
  - [ ] Extract refresh token from cookie
  - [ ] Validate token against database
  - [ ] Check token not expired and not revoked
  - [ ] Generate new access token
  - [ ] Rotate refresh token (invalidate old, create new)
  - [ ] Use database transaction for atomic rotation
  - [ ] Return new access token and set new cookie

- [ ] **Task 5: Implement logout endpoint** (AC: Logout criteria)
  - [ ] Add POST `/api/v1/auth/logout` endpoint
  - [ ] Extract refresh token from cookie
  - [ ] Revoke token in database
  - [ ] Clear refresh token cookie (Max-Age=0)
  - [ ] Return 204 No Content

- [ ] **Task 6: Handle concurrent refresh requests** (AC: Race condition handling)
  - [ ] Implement optimistic locking on refresh token rotation
  - [ ] Use database transaction with serializable isolation
  - [ ] Handle DbUpdateConcurrencyException gracefully
  - [ ] Return 401 for concurrent request that loses race
  - [ ] Write integration test for concurrent requests

- [ ] **Task 7: Implement security breach detection** (AC: Token reuse detection)
  - [ ] Detect refresh token reuse (token already rotated)
  - [ ] Revoke ALL user refresh tokens on reuse detection
  - [ ] Log security event for monitoring
  - [ ] Return appropriate error response
  - [ ] Write test for breach detection scenario

## Dev Notes

### RefreshToken Entity Model

```csharp
// Models/RefreshToken.cs
public class RefreshToken
{
    public Guid Id { get; set; }
    public string TokenHash { get; set; } = string.Empty; // SHA256 hash
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? RevokedReason { get; set; } // "logout", "rotation", "breach"
    public string? ReplacedByTokenId { get; set; } // Token chain for rotation
    
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt.HasValue;
    public bool IsActive => !IsExpired && !IsRevoked;
}
```

### Entity Framework Configuration

```csharp
// In ApplicationDbContext.OnModelCreating
modelBuilder.Entity<RefreshToken>(entity =>
{
    entity.HasKey(e => e.Id);
    entity.HasIndex(e => e.TokenHash).IsUnique();
    entity.HasIndex(e => e.UserId);
    entity.HasIndex(e => e.ExpiresAt);
    
    entity.HasOne(e => e.User)
        .WithMany(u => u.RefreshTokens)
        .HasForeignKey(e => e.UserId)
        .OnDelete(DeleteBehavior.Cascade);
});
```

### Refresh Token Service Implementation

```csharp
// Services/RefreshTokenService.cs
public class RefreshTokenService : IRefreshTokenService
{
    private readonly ApplicationDbContext _dbContext;
    
    public string GenerateRefreshToken()
    {
        return Guid.NewGuid().ToString("N"); // UUID v4 without dashes
    }
    
    public string HashToken(string token)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
    
    public async Task<RefreshToken> CreateRefreshTokenAsync(User user)
    {
        var token = GenerateRefreshToken();
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            TokenHash = HashToken(token),
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };
        
        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync();
        
        return refreshToken; // Note: Return plain token in cookie, not hash
    }
    
    public async Task<(RefreshToken? oldToken, string? error)> ValidateAndRotateAsync(
        string tokenHash)
    {
        await using var transaction = await _dbContext.Database
            .BeginTransactionAsync(IsolationLevel.Serializable);
        
        try
        {
            var token = await _dbContext.RefreshTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);
            
            if (token == null)
                return (null, "Invalid refresh token");
            
            if (token.IsRevoked)
            {
                // Token reuse detected - security breach!
                await RevokeAllUserTokensAsync(token.UserId, "breach");
                return (null, "Refresh token has been revoked. All sessions terminated.");
            }
            
            if (token.IsExpired)
            {
                token.RevokedAt = DateTime.UtcNow;
                token.RevokedReason = "expired";
                await _dbContext.SaveChangesAsync();
                return (null, "Refresh token expired. Please login again.");
            }
            
            // Revoke old token and create new one
            token.RevokedAt = DateTime.UtcNow;
            token.RevokedReason = "rotation";
            
            var newToken = await CreateRefreshTokenAsync(token.User);
            token.ReplacedByTokenId = newToken.Id.ToString();
            
            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
            
            return (newToken, null);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();
            return (null, "Token already refreshed. Please retry.");
        }
    }
}
```

### Cookie Configuration

```csharp
// Cookie options for refresh token
private CookieOptions GetRefreshTokenCookieOptions() => new()
{
    HttpOnly = true,
    Secure = true, // HTTPS only
    SameSite = SameSiteMode.Strict,
    Path = "/api/v1/auth/refresh",
    MaxAge = TimeSpan.FromDays(7)
};

// Setting the cookie in login response
Response.Cookies.Append("refreshToken", plainToken, GetRefreshTokenCookieOptions());

// Clearing the cookie on logout
Response.Cookies.Delete("refreshToken", GetRefreshTokenCookieOptions());
```

### Architecture Alignment

Per architecture.md requirements:
- Refresh Token: HttpOnly Cookie, 7-day expiry
- Access Token: JWT, 15-minute expiry (from Story 2.2)
- Security: SameSite=Strict prevents CSRF attacks

### Security Considerations

1. **Token Storage**: Only store SHA256 hash in database, never the plain token
2. **Token Rotation**: Always rotate on refresh to limit exposure window
3. **Breach Detection**: If revoked token is reused, revoke ALL user tokens
4. **Atomic Operations**: Use database transactions for token rotation
5. **Cookie Security**: HttpOnly, Secure, SameSite=Strict, limited Path

## Files to Create/Modify

### New Files
- `bmadServer.ApiService/Models/RefreshToken.cs`
- `bmadServer.ApiService/Services/IRefreshTokenService.cs`
- `bmadServer.ApiService/Services/RefreshTokenService.cs`
- `bmadServer.ApiService/Data/Migrations/YYYYMMDD_AddRefreshTokensTable.cs`

### Modified Files
- `bmadServer.ApiService/Models/User.cs` - Add RefreshTokens navigation property
- `bmadServer.ApiService/Data/ApplicationDbContext.cs` - Add DbSet<RefreshToken>, configure entity
- `bmadServer.ApiService/Controllers/AuthController.cs` - Add refresh/logout endpoints
- `bmadServer.ApiService/DTOs/LoginResponse.cs` - Note: access token only (refresh in cookie)
- `bmadServer.ApiService/Program.cs` - Register RefreshTokenService

---

## Aspire Development Standards

### PostgreSQL Connection Pattern

This story uses PostgreSQL configured in Story 1.2 via Aspire:
- Connection string automatically injected from Aspire AppHost
- RefreshTokens table created via EF Core migrations against Aspire-managed PostgreSQL
- Pattern: `builder.AddServiceDefaults();` (inherits PostgreSQL reference)
- See Story 1.2 for AppHost configuration pattern

### Project-Wide Standards

This story follows the Aspire-first development pattern:
- **Reference:** [PROJECT-WIDE-RULES.md](../../../PROJECT-WIDE-RULES.md)
- **Primary Documentation:** https://aspire.dev
- **GitHub:** https://github.com/microsoft/aspire

### Aspire-Specific Notes

- Database transactions handled by Aspire-managed PostgreSQL
- Health checks inherited from `ServiceDefaults`
- Structured logging for security events via OpenTelemetry

---

## References

- Source: [epics.md - Story 2.3](../planning-artifacts/epics.md)
- Architecture: [architecture.md](../planning-artifacts/architecture.md) - Token management section
- PRD: [prd.md](../planning-artifacts/prd.md) - Security requirements
- **Aspire Rules:** [PROJECT-WIDE-RULES.md](../../../PROJECT-WIDE-RULES.md)
- **Aspire Docs:** https://aspire.dev
