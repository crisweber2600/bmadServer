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
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<AuthController> _logger;
    
    // Pre-computed dummy hash to prevent timing attacks without performance penalty
    private static readonly string _dummyPasswordHash = BCrypt.Net.BCrypt.HashPassword("dummy-password-for-timing-safety", 12);

    public AuthController(
        ApplicationDbContext dbContext,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IRefreshTokenService refreshTokenService,
        IValidator<RegisterRequest> registerValidator,
        IValidator<LoginRequest> loginValidator,
        IOptions<JwtSettings> jwtSettings,
        ILogger<AuthController> logger)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _refreshTokenService = refreshTokenService;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
        _jwtSettings = jwtSettings.Value;
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

        // Check for existing user
        var existingUser = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email);

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

        // Generate JWT token
        var token = _jwtTokenService.GenerateAccessToken(user);

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
        var (newToken, error) = await _refreshTokenService.ValidateAndRotateAsync(refreshToken);

        if (error != null || newToken == null)
        {
            return Unauthorized(new ProblemDetails
            {
                Type = "https://bmadserver.dev/errors/invalid-refresh-token",
                Title = "Invalid Refresh Token",
                Status = StatusCodes.Status401Unauthorized,
                Detail = error ?? "Invalid refresh token"
            });
        }

        // Generate new access token
        var accessToken = _jwtTokenService.GenerateAccessToken(newToken.User);

        // Set new refresh token cookie (plain token is in TokenHash temporarily)
        Response.Cookies.Append("refreshToken", newToken.TokenHash, GetRefreshTokenCookieOptions());

        _logger.LogInformation("Token refreshed successfully for user: {UserId}", newToken.UserId);

        // Return new access token
        var response = new LoginResponse
        {
            AccessToken = accessToken,
            TokenType = "Bearer",
            ExpiresIn = _jwtSettings.AccessTokenExpirationMinutes * 60,
            User = new UserResponse
            {
                Id = newToken.User.Id,
                Email = newToken.User.Email,
                DisplayName = newToken.User.DisplayName,
                CreatedAt = newToken.User.CreatedAt
            }
        };

        return Ok(response);
    }

    /// <summary>
    /// Get current authenticated user details
    /// </summary>
    /// <returns>User details</returns>
    /// <response code="200">User is authenticated</response>
    /// <response code="401">User is not authenticated</response>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMe()
    {
        // Get user ID from claims
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized();
        }

        var user = await _dbContext.Users.FindAsync(userId);
        if (user == null)
        {
            return Unauthorized();
        }

        return Ok(new UserResponse
        {
            Id = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            CreatedAt = user.CreatedAt
        });
    }

    /// <summary>
    /// Logout and revoke refresh token
    /// </summary>
    /// <returns>No content on successful logout</returns>
    /// <response code="204">Logout successful</response>
    /// <response code="401">User is not authenticated</response>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout()
    {
        // Extract refresh token from cookie
        if (Request.Cookies.TryGetValue("refreshToken", out var refreshToken) && !string.IsNullOrEmpty(refreshToken))
        {
            var tokenHash = _refreshTokenService.HashToken(refreshToken);
            await _refreshTokenService.RevokeRefreshTokenAsync(tokenHash, "logout");
        }

        // Clear refresh token cookie
        Response.Cookies.Delete("refreshToken", new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/api/v1/auth/refresh"
        });

        _logger.LogInformation("User logged out successfully");

        return NoContent();
    }
}
