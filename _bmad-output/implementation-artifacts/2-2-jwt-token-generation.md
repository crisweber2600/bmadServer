# Story 2.2: JWT Token Generation & Validation

**Status:** done

## Story

As a registered user (Sarah),
I want to login with my credentials and receive a secure JWT access token,
so that I can make authenticated API requests to bmadServer.

## Acceptance Criteria

**Given** I am a registered user with valid credentials  
**When** I send POST `/api/v1/auth/login` with:
```json
{
  "email": "sarah@example.com",
  "password": "SecurePass123!"
}
```
**Then** the system validates my password against the bcrypt hash  
**And** generates a JWT access token with 15-minute expiry  
**And** the response returns 200 OK with:
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "tokenType": "Bearer",
  "expiresIn": 900,
  "user": {
    "id": "uuid",
    "email": "sarah@example.com",
    "displayName": "Sarah Johnson"
  }
}
```

**Given** I attempt to login with incorrect password  
**When** I send POST `/api/v1/auth/login` with wrong password  
**Then** the system returns 401 Unauthorized with ProblemDetails  
**And** the error message does not reveal whether email exists (prevent enumeration)  
**And** the response is: "Invalid email or password"

**Given** I attempt to login with non-existent email  
**When** I send POST `/api/v1/auth/login` with unregistered email  
**Then** the system returns 401 Unauthorized with same generic message  
**And** timing is consistent with failed password check (prevent timing attacks)

**Given** I have a valid JWT access token  
**When** I send GET `/api/v1/users/me` with `Authorization: Bearer {token}` header  
**Then** the JWT middleware validates the token signature  
**And** extracts user claims (userId, email)  
**And** the endpoint returns 200 OK with my user profile

**Given** I send a request with an expired JWT token  
**When** I call any protected endpoint with expired token  
**Then** the system returns 401 Unauthorized with:
```json
{
  "type": "https://bmadserver.dev/errors/token-expired",
  "title": "Token Expired",
  "status": 401,
  "detail": "Access token has expired. Please refresh your token."
}
```

**Given** I send a request with a malformed or tampered JWT token  
**When** I call any protected endpoint with invalid token  
**Then** the system returns 401 Unauthorized  
**And** the request is rejected before reaching endpoint logic  
**And** the error indicates "Invalid token signature"

**Given** the JWT configuration exists in appsettings.json  
**When** I review the configuration  
**Then** I see:
```json
{
  "Jwt": {
    "SecretKey": "{generated-secure-key}",
    "Issuer": "bmadServer",
    "Audience": "bmadServer-api",
    "AccessTokenExpirationMinutes": 15
  }
}
```
**And** SecretKey is at least 256 bits (32 characters)  
**And** SecretKey is stored securely (environment variable in production)

**Given** JWT authentication is configured in Program.cs  
**When** I review the middleware pipeline  
**Then** I see:
  - `builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)`
  - `app.UseAuthentication()` before `app.UseAuthorization()`
  - JWT validation parameters configured (ValidateIssuer, ValidateAudience, ValidateLifetime)

## Tasks / Subtasks

- [x] **Task 1: Configure JWT settings in appsettings.json** (AC: Configuration criteria)
  - [x] Add Jwt section to appsettings.json and appsettings.Development.json
  - [x] Generate secure 256-bit SecretKey for development
  - [x] Set Issuer to "bmadServer"
  - [x] Set Audience to "bmadServer-api"
  - [x] Set AccessTokenExpirationMinutes to 15
  - [x] Document environment variable override for production

- [x] **Task 2: Create JWT configuration class** (AC: Configuration binding)
  - [x] Create `Configuration/JwtSettings.cs` with all JWT properties
  - [x] Configure options binding in Program.cs
  - [x] Add validation to ensure SecretKey is at least 32 characters

- [x] **Task 3: Implement JWT token service** (AC: Token generation criteria)
  - [x] Create `Services/IJwtTokenService.cs` interface
  - [x] Create `Services/JwtTokenService.cs` implementation
  - [x] Generate access token with claims: sub (userId), email, iat, exp
  - [x] Sign token using HMAC-SHA256 algorithm
  - [x] Return token string with proper format
  - [x] Write unit tests for token generation

- [x] **Task 4: Configure JWT authentication middleware** (AC: Middleware criteria)
  - [x] Add Microsoft.AspNetCore.Authentication.JwtBearer NuGet package
  - [x] Configure AddAuthentication with JwtBearerDefaults
  - [x] Set TokenValidationParameters:
    - ValidateIssuer = true
    - ValidateAudience = true
    - ValidateLifetime = true
    - ValidateIssuerSigningKey = true
    - ClockSkew = TimeSpan.Zero (strict expiry)
  - [x] Add app.UseAuthentication() before app.UseAuthorization()

- [x] **Task 5: Implement login endpoint** (AC: All login criteria)
  - [x] Create `DTOs/LoginRequest.cs` with Email, Password
  - [x] Create `DTOs/LoginResponse.cs` with accessToken, tokenType, expiresIn, user
  - [x] Add POST `/api/v1/auth/login` to AuthController
  - [x] Lookup user by email (case-insensitive)
  - [x] Verify password using IPasswordHasher
  - [x] Generate JWT token using IJwtTokenService
  - [x] Return 200 OK with LoginResponse
  - [x] Implement timing-safe comparison to prevent timing attacks

- [x] **Task 6: Handle authentication errors** (AC: Error handling criteria)
  - [x] Return generic "Invalid email or password" for wrong credentials
  - [x] Implement consistent timing for failed vs successful lookups
  - [x] Configure JWT events for proper error responses (OnChallenge, OnAuthenticationFailed)
  - [x] Return ProblemDetails for expired token errors
  - [x] Return ProblemDetails for invalid signature errors

- [x] **Task 7: Implement /users/me endpoint** (AC: Token validation criteria)
  - [x] Create `Controllers/UsersController.cs`
  - [x] Add GET `/api/v1/users/me` with [Authorize] attribute
  - [x] Extract userId from JWT claims
  - [x] Return current user profile
  - [x] Test with valid, expired, and invalid tokens

## Dev Notes

### JWT Configuration

```json
// appsettings.json
{
  "Jwt": {
    "SecretKey": "your-256-bit-secret-key-here-minimum-32-characters",
    "Issuer": "bmadServer",
    "Audience": "bmadServer-api",
    "AccessTokenExpirationMinutes": 15
  }
}
```

### JWT Token Service Implementation

```csharp
// Services/JwtTokenService.cs
public class JwtTokenService : IJwtTokenService
{
    private readonly JwtSettings _settings;
    
    public JwtTokenService(IOptions<JwtSettings> settings)
    {
        _settings = settings.Value;
    }
    
    public string GenerateAccessToken(User user)
    {
        var securityKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_settings.SecretKey));
        var credentials = new SigningCredentials(
            securityKey, SecurityAlgorithms.HmacSha256);
        
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, 
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), 
                ClaimValueTypes.Integer64)
        };
        
        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_settings.AccessTokenExpirationMinutes),
            signingCredentials: credentials);
        
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

### JWT Authentication Middleware Setup

```csharp
// Program.cs
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()!;
        
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
            ClockSkew = TimeSpan.Zero // No tolerance for expiry
        };
        
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                if (context.Exception is SecurityTokenExpiredException)
                {
                    context.Response.Headers.Add("Token-Expired", "true");
                }
                return Task.CompletedTask;
            }
        };
    });

// Middleware pipeline (order matters!)
app.UseAuthentication();
app.UseAuthorization();
```

### Timing Attack Prevention

```csharp
// Login implementation with timing-safe comparison
public async Task<LoginResponse?> LoginAsync(LoginRequest request)
{
    var user = await _dbContext.Users
        .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());
    
    // Always perform hash verification to prevent timing attacks
    // even if user doesn't exist
    var dummyHash = _passwordHasher.Hash("dummy-password");
    var hashToVerify = user?.PasswordHash ?? dummyHash;
    var isValid = _passwordHasher.Verify(request.Password, hashToVerify);
    
    if (user == null || !isValid)
    {
        return null; // Generic "Invalid email or password" response
    }
    
    var token = _jwtTokenService.GenerateAccessToken(user);
    return new LoginResponse { /* ... */ };
}
```

### Architecture Alignment

Per architecture.md requirements:
- Access Token: JWT, 15-minute expiry
- Authentication: Hybrid (Local DB MVP + OpenID Connect Ready Phase 2)
- API Versioning: URL Path /api/v1/
- Error Handling: ProblemDetails RFC 7807

### Dependencies

- Microsoft.AspNetCore.Authentication.JwtBearer (NuGet)
- System.IdentityModel.Tokens.Jwt (NuGet)

## Files to Create/Modify

### New Files
- `bmadServer.ApiService/Configuration/JwtSettings.cs`
- `bmadServer.ApiService/Services/IJwtTokenService.cs`
- `bmadServer.ApiService/Services/JwtTokenService.cs`
- `bmadServer.ApiService/DTOs/LoginRequest.cs`
- `bmadServer.ApiService/DTOs/LoginResponse.cs`
- `bmadServer.ApiService/Controllers/UsersController.cs`
- `bmadServer.ApiService/Validators/LoginRequestValidator.cs`

### Modified Files
- `bmadServer.ApiService/appsettings.json` - Add Jwt section
- `bmadServer.ApiService/appsettings.Development.json` - Add dev Jwt config
- `bmadServer.ApiService/Controllers/AuthController.cs` - Add login endpoint
- `bmadServer.ApiService/Program.cs` - Configure JWT authentication
- `bmadServer.ApiService/bmadServer.ApiService.csproj` - Add NuGet packages

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

### Aspire-Specific Notes

- JWT secrets should use Aspire's environment configuration for production
- Health checks inherited from `ServiceDefaults`
- Structured logging via OpenTelemetry (Aspire Dashboard visible at https://localhost:17360)

---

## References

- Source: [epics.md - Story 2.2](../planning-artifacts/epics.md)
- Architecture: [architecture.md](../planning-artifacts/architecture.md) - Authentication section
- PRD: [prd.md](../planning-artifacts/prd.md) - Security requirements
- **Aspire Rules:** [PROJECT-WIDE-RULES.md](../../../PROJECT-WIDE-RULES.md)
- **Aspire Docs:** https://aspire.dev

---

## Dev Agent Record

### Implementation Plan

**Approach:**
Followed red-green-refactor TDD cycle for all tasks. Implemented JWT token generation and validation using ASP.NET Core's built-in authentication middleware with proper security measures:

1. **Configuration:** Added JWT settings to appsettings.json with 256-bit secret key, issuer, audience, and 15-minute expiry
2. **Token Service:** Implemented JwtTokenService to generate signed JWT tokens with user claims (sub, email, iat, exp)
3. **Middleware:** Configured JWT Bearer authentication with strict validation parameters (ClockSkew = 0)
4. **Login Endpoint:** Added POST /api/v1/auth/login with timing-safe password verification to prevent timing attacks
5. **Protected Endpoint:** Added GET /api/v1/users/me with [Authorize] attribute for token validation testing
6. **Error Handling:** Configured custom JWT events to return ProblemDetails for expired/invalid tokens

**Security Measures:**
- Timing-safe password comparison to prevent timing attacks
- Generic error messages to prevent email enumeration
- Strict token validation with zero clock skew
- Proper ProblemDetails responses for all error cases

### Debug Log

No significant issues encountered. All tests passed on first run after fixing test database scoping issue (changed to static database name to ensure in-memory database is shared across test scopes).

### Completion Notes

✅ **All 7 tasks completed successfully**
- JWT configuration added to appsettings.json (256-bit key)
- JwtSettings configuration class with validation
- IJwtTokenService interface and JwtTokenService implementation
- JWT authentication middleware configured in Program.cs
- Login endpoint with timing-safe password verification
- Error handling with ProblemDetails for expired/invalid tokens
- /users/me endpoint with [Authorize] attribute

**Tests Added:**
- 6 unit tests for JwtTokenService (token generation, claims, validation)
- 6 integration tests for login endpoint (valid credentials, invalid password, email enumeration, case sensitivity)
- 6 integration tests for /users/me endpoint (valid token, expired token, tampered token, missing token, deleted user)

**All 55 tests passing** (18 new tests added in this story)

**Security Features Implemented:**
- Timing-safe password comparison
- Generic error messages to prevent enumeration
- Zero clock skew for strict expiry
- ProblemDetails for all authentication errors
- HMAC-SHA256 token signing

---

## File List

### New Files
- src/bmadServer.ApiService/Configuration/JwtSettings.cs
- src/bmadServer.ApiService/Services/IJwtTokenService.cs
- src/bmadServer.ApiService/Services/JwtTokenService.cs
- src/bmadServer.ApiService/DTOs/LoginRequest.cs
- src/bmadServer.ApiService/DTOs/LoginResponse.cs
- src/bmadServer.ApiService/Controllers/UsersController.cs
- src/bmadServer.ApiService/Validators/LoginRequestValidator.cs
- src/bmadServer.Tests/Unit/JwtTokenServiceTests.cs
- src/bmadServer.Tests/Integration/LoginIntegrationTests.cs
- src/bmadServer.Tests/Integration/UsersMeIntegrationTests.cs

### Modified Files
- src/bmadServer.ApiService/appsettings.json
- src/bmadServer.ApiService/appsettings.Development.json
- src/bmadServer.ApiService/Controllers/AuthController.cs
- src/bmadServer.ApiService/Program.cs
- src/bmadServer.ApiService/bmadServer.ApiService.csproj

---

## Change Log

- **2026-01-25:** Story 2.2 implementation complete - JWT token generation and validation
  - Added JWT authentication with 15-minute access tokens
  - Implemented POST /api/v1/auth/login endpoint
  - Implemented GET /api/v1/users/me protected endpoint
  - Added timing-safe password verification
  - Configured JWT Bearer authentication middleware
  - Added comprehensive unit and integration tests (18 new tests)
  - All acceptance criteria satisfied, all tests passing (55/55)
