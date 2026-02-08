using bmadServer.ApiService.Configuration;
using bmadServer.ApiService.Data;
using bmadServer.ApiService.Data.Entities;
using bmadServer.ApiService.Data.Entities.SparkCompat;
using bmadServer.ApiService.DTOs.SparkCompat;
using bmadServer.ApiService.Hubs;
using bmadServer.ApiService.Services;
using bmadServer.ApiService.Services.SparkCompat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace bmadServer.ApiService.Controllers.SparkCompat;

[ApiController]
[Route("v1/auth")]
[Produces("application/json")]
public class AuthCompatController : SparkCompatControllerBase
{
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IRoleService _roleService;
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly SparkCompatRolloutOptions _rolloutOptions;
    private readonly JwtSettings _jwtSettings;

    public AuthCompatController(
        ApplicationDbContext dbContext,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IRefreshTokenService refreshTokenService,
        IRoleService roleService,
        IHubContext<ChatHub> hubContext,
        IOptions<SparkCompatRolloutOptions> rolloutOptions,
        IOptions<JwtSettings> jwtSettings)
        : base(dbContext, rolloutOptions)
    {
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _refreshTokenService = refreshTokenService;
        _roleService = roleService;
        _hubContext = hubContext;
        _rolloutOptions = rolloutOptions.Value;
        _jwtSettings = jwtSettings.Value;
    }

    [HttpPost("signup")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ResponseEnvelope<SparkAuthResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ResponseEnvelope<SparkAuthResponseDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResponseEnvelope<SparkAuthResponseDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ResponseEnvelope<SparkAuthResponseDto>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ResponseEnvelope<SparkAuthResponseDto>>> SignUp([FromBody] SparkSignUpRequest request)
    {
        if (!IsCompatEnabled || !_rolloutOptions.EnableAuth)
        {
            return DisabledResponse<SparkAuthResponseDto>("auth");
        }

        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password) || string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(ResponseMapperUtilities.MapError<SparkAuthResponseDto>(
                StatusCodes.Status400BadRequest,
                "Email, password, and name are required.",
                HttpContext.TraceIdentifier));
        }

        var passwordError = ValidatePasswordStrength(request.Password);
        if (passwordError != null)
        {
            return BadRequest(ResponseMapperUtilities.MapError<SparkAuthResponseDto>(
                StatusCodes.Status400BadRequest,
                passwordError,
                HttpContext.TraceIdentifier));
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var existingUser = await DbContext.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email);
        if (existingUser != null)
        {
            return Conflict(ResponseMapperUtilities.MapError<SparkAuthResponseDto>(
                StatusCodes.Status409Conflict,
                "User with this email already exists.",
                HttpContext.TraceIdentifier));
        }

        var user = new User
        {
            Email = email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            DisplayName = request.Name.Trim(),
            PersonaType = SparkCompatUtilities.ToPersonaType(request.Role)
        };

        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync();
        await _roleService.AssignDefaultRoleAsync(user.Id);

        var token = _jwtTokenService.GenerateAccessToken(user);

        // Issue refresh token and set HttpOnly cookie (backend-canonical)
        var (_, plainRefreshToken) = await _refreshTokenService.CreateRefreshTokenAsync(user);
        Response.Cookies.Append("refreshToken", plainRefreshToken, GetRefreshTokenCookieOptions());

        // Emit user_joined collaboration event
        await EmitCollaborationEventAsync("user_joined", user.Id, user.DisplayName, null, null,
            new { userId = user.Id.ToString(), email = user.Email, name = user.DisplayName });

        var payload = new SparkAuthResponseDto
        {
            User = MapUser(user),
            Token = token,
            ExpiresIn = _jwtSettings.AccessTokenExpirationMinutes * 60
        };

        return StatusCode(StatusCodes.Status201Created, ResponseMapperUtilities.MapToEnvelope(payload, StatusCodes.Status201Created, HttpContext.TraceIdentifier, "Account created"));
    }

    [HttpPost("signin")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ResponseEnvelope<SparkAuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseEnvelope<SparkAuthResponseDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResponseEnvelope<SparkAuthResponseDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ResponseEnvelope<SparkAuthResponseDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResponseEnvelope<SparkAuthResponseDto>>> SignIn([FromBody] SparkSignInRequest request)
    {
        if (!IsCompatEnabled || !_rolloutOptions.EnableAuth)
        {
            return DisabledResponse<SparkAuthResponseDto>("auth");
        }

        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(ResponseMapperUtilities.MapError<SparkAuthResponseDto>(
                StatusCodes.Status400BadRequest,
                "Email and password are required.",
                HttpContext.TraceIdentifier));
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var user = await DbContext.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email);
        if (user == null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized(ResponseMapperUtilities.MapError<SparkAuthResponseDto>(
                StatusCodes.Status401Unauthorized,
                "Invalid email or password.",
                HttpContext.TraceIdentifier));
        }

        var token = _jwtTokenService.GenerateAccessToken(user);

        // Issue refresh token and set HttpOnly cookie (backend-canonical)
        var (_, plainRefreshToken) = await _refreshTokenService.CreateRefreshTokenAsync(user);
        Response.Cookies.Append("refreshToken", plainRefreshToken, GetRefreshTokenCookieOptions());

        var payload = new SparkAuthResponseDto
        {
            User = MapUser(user),
            Token = token,
            ExpiresIn = _jwtSettings.AccessTokenExpirationMinutes * 60
        };

        return Ok(ResponseMapperUtilities.MapToEnvelope(payload, HttpContext.TraceIdentifier, "Signed in"));
    }

    [HttpPost("signout")]
    [Authorize]
    [ProducesResponseType(typeof(ResponseEnvelope<SparkSignOutResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseEnvelope<SparkSignOutResponse>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResponseEnvelope<SparkSignOutResponse>>> SignOutCompat()
    {
        if (!IsCompatEnabled || !_rolloutOptions.EnableAuth)
        {
            return DisabledResponse<SparkSignOutResponse>("auth");
        }

        // Revoke refresh token and clear cookie (backend-canonical)
        if (Request.Cookies.TryGetValue("refreshToken", out var refreshToken) && !string.IsNullOrEmpty(refreshToken))
        {
            var tokenHash = _refreshTokenService.HashToken(refreshToken);
            await _refreshTokenService.RevokeRefreshTokenAsync(tokenHash, "signout");
        }

        Response.Cookies.Delete("refreshToken", new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/v1/auth/refresh"
        });

        var payload = new SparkSignOutResponse
        {
            SignedOut = true,
            SignedOutAt = SparkCompatUtilities.ToUnixMilliseconds(DateTime.UtcNow)
        };

        return Ok(ResponseMapperUtilities.MapToEnvelope(payload, HttpContext.TraceIdentifier, "Signed out"));
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ResponseEnvelope<SparkAuthUserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseEnvelope<SparkAuthUserDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ResponseEnvelope<SparkAuthUserDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResponseEnvelope<SparkAuthUserDto>>> Me()
    {
        if (!IsCompatEnabled || !_rolloutOptions.EnableAuth)
        {
            return DisabledResponse<SparkAuthUserDto>("auth");
        }

        var user = await GetCurrentUserAsync();
        if (user == null)
        {
            return Unauthorized(ResponseMapperUtilities.MapError<SparkAuthUserDto>(
                StatusCodes.Status401Unauthorized,
                "Authentication required.",
                HttpContext.TraceIdentifier));
        }

        return Ok(ResponseMapperUtilities.MapToEnvelope(MapUser(user), HttpContext.TraceIdentifier));
    }

    /// <summary>
    /// Refresh access token using refresh token from HttpOnly cookie.
    /// Mirrors native AuthController refresh with SparkCompat envelope format.
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ResponseEnvelope<SparkAuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseEnvelope<SparkAuthResponseDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ResponseEnvelope<SparkAuthResponseDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResponseEnvelope<SparkAuthResponseDto>>> Refresh()
    {
        if (!IsCompatEnabled || !_rolloutOptions.EnableAuth)
        {
            return DisabledResponse<SparkAuthResponseDto>("auth");
        }

        if (!Request.Cookies.TryGetValue("refreshToken", out var refreshToken) || string.IsNullOrEmpty(refreshToken))
        {
            return Unauthorized(ResponseMapperUtilities.MapError<SparkAuthResponseDto>(
                StatusCodes.Status401Unauthorized,
                "Refresh token not found. Please sign in again.",
                HttpContext.TraceIdentifier));
        }

        var (newToken, plainRefreshToken, error) = await _refreshTokenService.ValidateAndRotateAsync(refreshToken);

        if (error != null || newToken == null || plainRefreshToken == null)
        {
            return Unauthorized(ResponseMapperUtilities.MapError<SparkAuthResponseDto>(
                StatusCodes.Status401Unauthorized,
                error ?? "Invalid refresh token.",
                HttpContext.TraceIdentifier));
        }

        var accessToken = _jwtTokenService.GenerateAccessToken(newToken.User);

        // Set rotated refresh token cookie (plaintext, NOT the hash)
        Response.Cookies.Append("refreshToken", plainRefreshToken, GetRefreshTokenCookieOptions());

        var payload = new SparkAuthResponseDto
        {
            User = MapUser(newToken.User),
            Token = accessToken,
            ExpiresIn = _jwtSettings.AccessTokenExpirationMinutes * 60
        };

        return Ok(ResponseMapperUtilities.MapToEnvelope(payload, HttpContext.TraceIdentifier, "Token refreshed"));
    }

    private async Task EmitCollaborationEventAsync(string eventType, Guid userId, string userName, string? chatId, string? prId, object? metadata)
    {
        var evt = new SparkCompatCollaborationEvent
        {
            Id = SparkCompatUtilities.CreateId("event"),
            Type = eventType,
            UserId = userId,
            UserName = userName,
            ChatId = chatId,
            PrId = prId,
            Timestamp = DateTime.UtcNow,
            MetadataJson = metadata == null ? null : SparkCompatUtilities.ToJson(metadata)
        };

        DbContext.SparkCompatCollaborationEvents.Add(evt);
        await DbContext.SaveChangesAsync();

        await _hubContext.Clients.All.SendAsync("SparkCompatEvent", new
        {
            id = evt.Id,
            type = evt.Type,
            userId = evt.UserId.ToString(),
            userName = evt.UserName,
            chatId = evt.ChatId,
            prId = evt.PrId,
            timestamp = SparkCompatUtilities.ToUnixMilliseconds(evt.Timestamp),
            metadata
        });
    }

    private static string? ValidatePasswordStrength(string password)
    {
        if (password.Length < 8)
            return "Password must be at least 8 characters long.";
        if (!password.Any(char.IsUpper))
            return "Password must contain at least one uppercase letter.";
        if (!password.Any(char.IsLower))
            return "Password must contain at least one lowercase letter.";
        if (!password.Any(char.IsDigit))
            return "Password must contain at least one digit.";
        return null;
    }

    private CookieOptions GetRefreshTokenCookieOptions() => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = "/v1/auth/refresh",
        MaxAge = TimeSpan.FromDays(7)
    };

    private static SparkAuthUserDto MapUser(User user)
    {
        return new SparkAuthUserDto
        {
            Id = user.Id.ToString(),
            Email = user.Email,
            Name = user.DisplayName,
            Role = SparkCompatUtilities.ToSparkRole(user.PersonaType),
            AvatarUrl = AvatarFor(user.DisplayName),
            CreatedAt = SparkCompatUtilities.ToUnixMilliseconds(user.CreatedAt)
        };
    }

    public sealed class SparkSignUpRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Role { get; set; } = "business";
    }

    public sealed class SparkSignInRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public sealed class SparkAuthUserDto
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Role { get; set; } = "business";
        public string AvatarUrl { get; set; } = string.Empty;
        public long CreatedAt { get; set; }
    }

    public sealed class SparkAuthResponseDto
    {
        public SparkAuthUserDto User { get; set; } = null!;
        public string Token { get; set; } = string.Empty;
        public int ExpiresIn { get; set; }
    }

    public sealed class SparkSignOutResponse
    {
        public bool SignedOut { get; set; }
        public long SignedOutAt { get; set; }
    }
}
