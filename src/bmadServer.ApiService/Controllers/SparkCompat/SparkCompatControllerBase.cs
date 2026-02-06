using bmadServer.ApiService.Configuration;
using bmadServer.ApiService.Data;
using bmadServer.ApiService.Data.Entities;
using bmadServer.ApiService.DTOs.SparkCompat;
using bmadServer.ApiService.Services.SparkCompat;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace bmadServer.ApiService.Controllers.SparkCompat;

public abstract class SparkCompatControllerBase : ControllerBase
{
    private readonly SparkCompatRolloutOptions _rolloutOptions;
    protected readonly ApplicationDbContext DbContext;

    protected SparkCompatControllerBase(ApplicationDbContext dbContext, IOptions<SparkCompatRolloutOptions> rolloutOptions)
    {
        DbContext = dbContext;
        _rolloutOptions = rolloutOptions.Value;
    }

    protected bool IsCompatEnabled => _rolloutOptions.EnableCompatV1;

    protected ActionResult<ResponseEnvelope<T>> DisabledResponse<T>(string featureName)
    {
        var envelope = ResponseMapperUtilities.MapError<T>(
            StatusCodes.Status404NotFound,
            $"Spark compatibility endpoint '{featureName}' is disabled for this environment.",
            HttpContext.TraceIdentifier);
        return StatusCode(StatusCodes.Status404NotFound, envelope);
    }

    protected Guid? TryGetCurrentUserId()
    {
        var claimValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        return Guid.TryParse(claimValue, out var userId) ? userId : null;
    }

    protected async Task<User?> GetCurrentUserAsync()
    {
        var userId = TryGetCurrentUserId();
        if (!userId.HasValue)
        {
            return null;
        }

        return await DbContext.Users.FirstOrDefaultAsync(u => u.Id == userId.Value);
    }

    protected static string AvatarFor(string displayName)
    {
        return $"https://api.dicebear.com/7.x/initials/svg?seed={Uri.EscapeDataString(displayName)}";
    }
}
