using bmadServer.ApiService.Configuration;
using bmadServer.ApiService.Data;
using bmadServer.ApiService.Data.Entities;
using bmadServer.ApiService.DTOs.SparkCompat;
using bmadServer.ApiService.Services;
using bmadServer.ApiService.Services.SparkCompat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    private readonly IRoleService _roleService;
    private readonly SparkCompatRolloutOptions _rolloutOptions;

    public AuthCompatController(
        ApplicationDbContext dbContext,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IRoleService roleService,
        IOptions<SparkCompatRolloutOptions> rolloutOptions)
        : base(dbContext, rolloutOptions)
    {
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _roleService = roleService;
        _rolloutOptions = rolloutOptions.Value;
    }

    [HttpPost("signup")]
    [AllowAnonymous]
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
        var payload = new SparkAuthResponseDto
        {
            User = MapUser(user),
            Token = token
        };

        return StatusCode(StatusCodes.Status201Created, ResponseMapperUtilities.MapToEnvelope(payload, StatusCodes.Status201Created, HttpContext.TraceIdentifier, "Account created"));
    }

    [HttpPost("signin")]
    [AllowAnonymous]
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
        var payload = new SparkAuthResponseDto
        {
            User = MapUser(user),
            Token = token
        };

        return Ok(ResponseMapperUtilities.MapToEnvelope(payload, HttpContext.TraceIdentifier, "Signed in"));
    }

    [HttpPost("signout")]
    [Authorize]
    public ActionResult<ResponseEnvelope<SparkSignOutResponse>> SignOutCompat()
    {
        if (!IsCompatEnabled || !_rolloutOptions.EnableAuth)
        {
            return DisabledResponse<SparkSignOutResponse>("auth");
        }

        var payload = new SparkSignOutResponse
        {
            SignedOut = true,
            SignedOutAt = SparkCompatUtilities.ToUnixMilliseconds(DateTime.UtcNow)
        };

        return Ok(ResponseMapperUtilities.MapToEnvelope(payload, HttpContext.TraceIdentifier, "Signed out"));
    }

    [HttpGet("me")]
    [Authorize]
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
    }

    public sealed class SparkSignOutResponse
    {
        public bool SignedOut { get; set; }
        public long SignedOutAt { get; set; }
    }
}
