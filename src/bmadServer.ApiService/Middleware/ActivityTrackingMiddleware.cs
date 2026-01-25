using bmadServer.ApiService.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace bmadServer.ApiService.Middleware;

public class ActivityTrackingMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly TimeSpan DebounceInterval = TimeSpan.FromMinutes(1);

    public ActivityTrackingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ApplicationDbContext dbContext)
    {
        await _next(context);

        if (context.User.Identity?.IsAuthenticated != true)
            return;

        var userId = GetUserIdFromClaims(context.User);
        if (!userId.HasValue)
            return;

        await UpdateLastActivityAsync(dbContext, userId.Value);
    }

    private static Guid? GetUserIdFromClaims(ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return null;

        return userId;
    }

    private static async Task UpdateLastActivityAsync(ApplicationDbContext dbContext, Guid userId)
    {
        var debounceThreshold = DateTime.UtcNow.Subtract(DebounceInterval);

        var session = await dbContext.Sessions
            .Where(s => s.UserId == userId && s.IsActive)
            .Where(s => s.LastActivityAt < debounceThreshold)
            .OrderByDescending(s => s.LastActivityAt)
            .FirstOrDefaultAsync();

        if (session != null)
        {
            session.LastActivityAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync();
        }
    }
}

public static class ActivityTrackingMiddlewareExtensions
{
    public static IApplicationBuilder UseActivityTracking(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ActivityTrackingMiddleware>();
    }
}
