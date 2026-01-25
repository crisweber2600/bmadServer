using bmadServer.ApiService.Configuration;
using bmadServer.ApiService.Data;
using bmadServer.ApiService.Data.Entities;
using bmadServer.ApiService.DTOs;
using bmadServer.ApiService.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace bmadServer.ApiService.Controllers;

/// <summary>
/// Authentication controller for user registration and login
/// </summary>
[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IRoleService _roleService;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly JwtSettings _jwtSettings;
    private readonly SessionSettings _sessionSettings;
    private readonly ILogger<AuthController> _logger;
    
    // Pre-computed dummy hash to prevent timing attacks without performance penalty
    private static readonly string _dummyPasswordHash = BCrypt.Net.BCrypt.HashPassword("dummy-password-for-timing-safety", 12);

    public AuthController(
        ApplicationDbContext dbContext,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IRefreshTokenService refreshTokenService,
        IRoleService roleService,
        IValidator<RegisterRequest> registerValidator,
        IValidator<LoginRequest> loginValidator,
        IOptions<JwtSettings> jwtSettings,
        IOptions<SessionSettings> sessionSettings,
        ILogger<AuthController> logger)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _refreshTokenService = refreshTokenService;
        _roleService = roleService;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
        _jwtSettings = jwtSettings.Value;
        _sessionSettings = sessionSettings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Register a new user with email and password
    /// </summary>
    /// <param name="request">Registration request containing email, password, and display name</param>
    /// <returns>Created user details (excluding password hash)</returns>
    /// <response code="201">User created successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="409">User with this email already exists</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        // Validate request
        var validationResult = await _registerValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
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
                    Detail = "One or more validation errors occurred"
                });
        }

        // Check for existing user (case-insensitive to prevent duplicate accounts)
        var existingUser = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

        if (existingUser != null)
        {
            return Conflict(new ProblemDetails
            {
                Type = "https://bmadserver.dev/errors/user-exists",
                Title = "User Already Exists",
                Status = StatusCodes.Status409Conflict,
                Detail = "A user with this email already exists"
            });
        }

        // Hash password
        var passwordHash = _passwordHasher.Hash(request.Password);

        // Create user entity
        var user = new User
        {
            Email = request.Email,
            PasswordHash = passwordHash,
            DisplayName = request.DisplayName,
            CreatedAt = DateTime.UtcNow
        };

        // Save to database
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        // Assign default role (Participant)
        await _roleService.AssignDefaultRoleAsync(user.Id);

        _logger.LogInformation("User registered successfully: {Email}", user.Email);

        // Return user response
        var response = new UserResponse
        {
            Id = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            CreatedAt = user.CreatedAt
        };

        return CreatedAtAction(nameof(Register), new { id = user.Id }, response);
    }

    /// <summary>
    /// Login with email and password to receive a JWT access token
    /// </summary>
    /// <param name="request">Login request containing email and password</param>
    /// <returns>JWT access token and user details</returns>
    /// <response code="200">Login successful, returns access token</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="401">Invalid email or password</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        // Validate request
        var validationResult = await _loginValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
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
                    Detail = "One or more validation errors occurred"
                });
        }

        // Lookup user by email (case-insensitive)
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

        // Always perform hash verification to prevent timing attacks
        // Use pre-computed dummy hash if user doesn't exist
        var hashToVerify = user?.PasswordHash ?? _dummyPasswordHash;
        var isValid = _passwordHasher.Verify(request.Password, hashToVerify);

        if (user == null || !isValid)
        {
            // Generic error message to prevent enumeration
            return Unauthorized(new ProblemDetails
            {
                Type = "https://bmadserver.dev/errors/invalid-credentials",
                Title = "Invalid Credentials",
                Status = StatusCodes.Status401Unauthorized,
                Detail = "Invalid email or password"
            });
        }

        // Get user roles for JWT claims
        var roles = await _roleService.GetUserRolesAsync(user.Id);
        
        // Generate JWT token with roles
        var token = _jwtTokenService.GenerateAccessToken(user, roles);

        // Generate and store refresh token
        var (refreshToken, plainRefreshToken) = await _refreshTokenService.CreateRefreshTokenAsync(user);

        // Set HttpOnly cookie for refresh token
        Response.Cookies.Append("refreshToken", plainRefreshToken, GetRefreshTokenCookieOptions());

        _logger.LogInformation("User logged in successfully: {Email}", user.Email);

        // Return login response
        var response = new LoginResponse
        {
            AccessToken = token,
            TokenType = "Bearer",
            ExpiresIn = _jwtSettings.AccessTokenExpirationMinutes * 60, // Convert to seconds
            User = new UserResponse
            {
                Id = user.Id,
                Email = user.Email,
                DisplayName = user.DisplayName,
                CreatedAt = user.CreatedAt
            }
        };

        return Ok(response);
    }

    private CookieOptions GetRefreshTokenCookieOptions() => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = "/api/v1/auth/refresh",
        MaxAge = TimeSpan.FromDays(7)
    };

    /// <summary>
    /// Refresh access token using refresh token from HttpOnly cookie
    /// </summary>
    /// <returns>New access token with rotated refresh token in cookie</returns>
    /// <response code="200">Token refreshed successfully</response>
    /// <response code="401">Invalid, expired, or revoked refresh token</response>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh()
    {
        // Extract refresh token from cookie
        if (!Request.Cookies.TryGetValue("refreshToken", out var refreshToken) || string.IsNullOrEmpty(refreshToken))
        {
            return Unauthorized(new ProblemDetails
            {
                Type = "https://bmadserver.dev/errors/missing-refresh-token",
                Title = "Missing Refresh Token",
                Status = StatusCodes.Status401Unauthorized,
                Detail = "Refresh token not found. Please login again."
            });
        }

        // Validate and rotate refresh token
        var result = await _refreshTokenService.ValidateAndRotateAsync(refreshToken);

        if (!result.IsSuccess)
        {
            return Unauthorized(new ProblemDetails
            {
                Type = "https://bmadserver.dev/errors/invalid-refresh-token",
                Title = "Invalid Refresh Token",
                Status = StatusCodes.Status401Unauthorized,
                Detail = result.Error ?? "Invalid refresh token"
            });
        }

        // Get user roles and generate new access token
        var roles = await _roleService.GetUserRolesAsync(result.Token!.UserId);
        var accessToken = _jwtTokenService.GenerateAccessToken(result.Token.User, roles);

        // Set new refresh token cookie with the plain token
        Response.Cookies.Append("refreshToken", result.PlainToken!, GetRefreshTokenCookieOptions());

        _logger.LogInformation("Token refreshed successfully for user: {UserId}", result.Token.UserId);

        // Return new access token
        var response = new LoginResponse
        {
            AccessToken = accessToken,
            TokenType = "Bearer",
            ExpiresIn = _jwtSettings.AccessTokenExpirationMinutes * 60,
            User = new UserResponse
            {
                Id = result.Token!.User.Id,
                Email = result.Token.User.Email,
                DisplayName = result.Token.User.DisplayName,
                CreatedAt = result.Token.User.CreatedAt
            }
        };

        return Ok(response);
    }

    /// <summary>
    /// Logout and revoke refresh token
    /// </summary>
    /// <param name="reason">Optional reason for logout (e.g., "idle_timeout", "manual")</param>
    /// <returns>No content on successful logout</returns>
    /// <response code="204">Logout successful</response>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout([FromQuery] string? reason = null)
    {
        var userId = GetUserIdFromClaims();

        // Extract refresh token from cookie
        if (Request.Cookies.TryGetValue("refreshToken", out var refreshToken) && !string.IsNullOrEmpty(refreshToken))
        {
            var tokenHash = _refreshTokenService.HashToken(refreshToken);
            await _refreshTokenService.RevokeRefreshTokenAsync(tokenHash, reason ?? "logout");
        }

        // Mark active session as inactive if authenticated
        if (userId.HasValue)
        {
            var session = await _dbContext.Sessions
                .Where(s => s.UserId == userId.Value && s.IsActive)
                .OrderByDescending(s => s.LastActivityAt)
                .FirstOrDefaultAsync();

            if (session != null)
            {
                session.IsActive = false;
                session.ConnectionId = null;
                await _dbContext.SaveChangesAsync();
            }
        }

        // Clear refresh token cookie
        Response.Cookies.Delete("refreshToken", new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/api/v1/auth/refresh"
        });

        _logger.LogInformation("User logged out successfully. Reason: {Reason}", reason ?? "manual");

        return NoContent();
    }

    /// <summary>
    /// Extend session to prevent idle timeout
    /// </summary>
    /// <returns>No content on successful session extension</returns>
    /// <response code="204">Session extended successfully</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="404">No active session found</response>
    [HttpPost("extend-session")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExtendSession()
    {
        var userId = GetUserIdFromClaims();

        if (!userId.HasValue)
        {
            return Unauthorized(new ProblemDetails
            {
                Type = "https://bmadserver.dev/errors/invalid-token",
                Title = "Invalid Token",
                Status = StatusCodes.Status401Unauthorized,
                Detail = "Unable to extract user information from token"
            });
        }

        var session = await _dbContext.Sessions
            .Where(s => s.UserId == userId.Value && s.IsActive)
            .OrderByDescending(s => s.LastActivityAt)
            .FirstOrDefaultAsync();

        if (session == null)
        {
            return NotFound(new ProblemDetails
            {
                Type = "https://bmadserver.dev/errors/no-active-session",
                Title = "No Active Session",
                Status = StatusCodes.Status404NotFound,
                Detail = "No active session found for this user"
            });
        }

        session.LastActivityAt = DateTime.UtcNow;
        session.ExpiresAt = DateTime.UtcNow.AddMinutes(_sessionSettings.IdleTimeoutMinutes);

        await _dbContext.SaveChangesAsync();

        _logger.LogDebug("Session extended for user {UserId}", userId.Value);

        return NoContent();
    }

    private Guid? GetUserIdFromClaims()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return null;
        }

        return userId;
    }
}
