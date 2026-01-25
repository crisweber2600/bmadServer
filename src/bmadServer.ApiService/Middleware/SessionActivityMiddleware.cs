using bmadServer.ApiService.Configuration;
using bmadServer.ApiService.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace bmadServer.ApiService.Middleware;

/// <summary>
/// Middleware to automatically update session LastActivityAt on API calls
/// Implements debounce to prevent excessive database updates
/// </summary>
public class SessionActivityMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly TimeSpan DebounceInterval = TimeSpan.FromMinutes(1);

    public SessionActivityMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ApplicationDbContext dbContext, IOptions<SessionSettings> sessionSettings)
    {
        await _next(context);

        // Only update activity for authenticated users
        if (context.User.Identity?.IsAuthenticated != true)
            return;

        var userId = GetUserIdFromClaims(context.User);
        if (!userId.HasValue)
            return;

        await UpdateSessionActivityAsync(dbContext, userId.Value, sessionSettings.Value);
    }

    private static Guid? GetUserIdFromClaims(ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return null;

        return userId;
    }

    private static async Task UpdateSessionActivityAsync(ApplicationDbContext dbContext, Guid userId, SessionSettings settings)
    {
        // Debounce: only update if last activity was more than 1 minute ago
        var debounceThreshold = DateTime.UtcNow.Subtract(DebounceInterval);

        var session = await dbContext.Sessions
            .Where(s => s.UserId == userId && s.IsActive)
            .Where(s => s.LastActivityAt < debounceThreshold)
            .OrderByDescending(s => s.LastActivityAt)
            .FirstOrDefaultAsync();

        if (session != null)
        {
            session.LastActivityAt = DateTime.UtcNow;
            session.ExpiresAt = DateTime.UtcNow.AddMinutes(settings.IdleTimeoutMinutes);
            await dbContext.SaveChangesAsync();
        }
    }
}

public static class SessionActivityMiddlewareExtensions
{
    public static IApplicationBuilder UseSessionActivity(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SessionActivityMiddleware>();
    }
}
