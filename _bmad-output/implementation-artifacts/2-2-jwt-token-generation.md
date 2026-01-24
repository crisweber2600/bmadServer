# Story 2.2: JWT Token Generation & Validation

**Status:** ready-for-dev

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

- [ ] **Task 1: Configure JWT settings in appsettings.json** (AC: Configuration criteria)
  - [ ] Add Jwt section to appsettings.json and appsettings.Development.json
  - [ ] Generate secure 256-bit SecretKey for development
  - [ ] Set Issuer to "bmadServer"
  - [ ] Set Audience to "bmadServer-api"
  - [ ] Set AccessTokenExpirationMinutes to 15
  - [ ] Document environment variable override for production

- [ ] **Task 2: Create JWT configuration class** (AC: Configuration binding)
  - [ ] Create `Configuration/JwtSettings.cs` with all JWT properties
  - [ ] Configure options binding in Program.cs
  - [ ] Add validation to ensure SecretKey is at least 32 characters

- [ ] **Task 3: Implement JWT token service** (AC: Token generation criteria)
  - [ ] Create `Services/IJwtTokenService.cs` interface
  - [ ] Create `Services/JwtTokenService.cs` implementation
  - [ ] Generate access token with claims: sub (userId), email, iat, exp
  - [ ] Sign token using HMAC-SHA256 algorithm
  - [ ] Return token string with proper format
  - [ ] Write unit tests for token generation

- [ ] **Task 4: Configure JWT authentication middleware** (AC: Middleware criteria)
  - [ ] Add Microsoft.AspNetCore.Authentication.JwtBearer NuGet package
  - [ ] Configure AddAuthentication with JwtBearerDefaults
  - [ ] Set TokenValidationParameters:
    - ValidateIssuer = true
    - ValidateAudience = true
    - ValidateLifetime = true
    - ValidateIssuerSigningKey = true
    - ClockSkew = TimeSpan.Zero (strict expiry)
  - [ ] Add app.UseAuthentication() before app.UseAuthorization()

- [ ] **Task 5: Implement login endpoint** (AC: All login criteria)
  - [ ] Create `DTOs/LoginRequest.cs` with Email, Password
  - [ ] Create `DTOs/LoginResponse.cs` with accessToken, tokenType, expiresIn, user
  - [ ] Add POST `/api/v1/auth/login` to AuthController
  - [ ] Lookup user by email (case-insensitive)
  - [ ] Verify password using IPasswordHasher
  - [ ] Generate JWT token using IJwtTokenService
  - [ ] Return 200 OK with LoginResponse
  - [ ] Implement timing-safe comparison to prevent timing attacks

- [ ] **Task 6: Handle authentication errors** (AC: Error handling criteria)
  - [ ] Return generic "Invalid email or password" for wrong credentials
  - [ ] Implement consistent timing for failed vs successful lookups
  - [ ] Configure JWT events for proper error responses (OnChallenge, OnAuthenticationFailed)
  - [ ] Return ProblemDetails for expired token errors
  - [ ] Return ProblemDetails for invalid signature errors

- [ ] **Task 7: Implement /users/me endpoint** (AC: Token validation criteria)
  - [ ] Create `Controllers/UsersController.cs`
  - [ ] Add GET `/api/v1/users/me` with [Authorize] attribute
  - [ ] Extract userId from JWT claims
  - [ ] Return current user profile
  - [ ] Test with valid, expired, and invalid tokens

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

## References

- Source: [epics.md - Story 2.2](../_bmad-output/planning-artifacts/epics.md)
- Architecture: [architecture.md](../_bmad-output/planning-artifacts/architecture.md) - Authentication section
- PRD: [prd.md](../_bmad-output/planning-artifacts/prd.md) - Security requirements
