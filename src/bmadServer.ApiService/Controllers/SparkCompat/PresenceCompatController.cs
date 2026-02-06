using System.Text.Json;
using bmadServer.ApiService.Configuration;
using bmadServer.ApiService.Data;
using bmadServer.ApiService.Data.Entities.SparkCompat;
using bmadServer.ApiService.DTOs.SparkCompat;
using bmadServer.ApiService.Services.SparkCompat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace bmadServer.ApiService.Controllers.SparkCompat;

[ApiController]
[Route("v1")]
[Authorize]
public class PresenceCompatController : SparkCompatControllerBase
{
    private static readonly TimeSpan PresenceTimeout = TimeSpan.FromSeconds(30);
    private readonly SparkCompatRolloutOptions _rolloutOptions;

    public PresenceCompatController(
        ApplicationDbContext dbContext,
        IOptions<SparkCompatRolloutOptions> rolloutOptions)
        : base(dbContext, rolloutOptions)
    {
        _rolloutOptions = rolloutOptions.Value;
    }

    [HttpPut("users/{userId:guid}/presence")]
    public async Task<ActionResult<ResponseEnvelope<PresenceDto>>> UpdatePresence(Guid userId, [FromBody] UpdatePresenceRequest request)
    {
        if (!IsCompatEnabled || !_rolloutOptions.EnablePresence)
        {
            return DisabledResponse<PresenceDto>("presence");
        }

        var currentUserId = TryGetCurrentUserId();
        if (!currentUserId.HasValue || currentUserId.Value != userId)
        {
            return Unauthorized(ResponseMapperUtilities.MapError<PresenceDto>(
                StatusCodes.Status401Unauthorized,
                "You can only update your own presence.",
                HttpContext.TraceIdentifier));
        }

        var user = await DbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            return NotFound(ResponseMapperUtilities.MapError<PresenceDto>(
                StatusCodes.Status404NotFound,
                "User not found.",
                HttpContext.TraceIdentifier));
        }

        var snapshot = await DbContext.SparkCompatPresenceSnapshots.FirstOrDefaultAsync(p => p.UserId == userId);
        if (snapshot == null)
        {
            snapshot = new SparkCompatPresenceSnapshot { UserId = userId };
            DbContext.SparkCompatPresenceSnapshots.Add(snapshot);
        }

        snapshot.UserName = user.DisplayName;
        snapshot.AvatarUrl = AvatarFor(user.DisplayName);
        snapshot.ActiveChatId = request.ActiveChat;
        snapshot.IsTyping = request.IsTyping;
        snapshot.TypingChatId = request.TypingChatId;
        snapshot.LastSeenAt = DateTime.UtcNow;
        snapshot.CursorPositionJson = request.CursorPosition == null
            ? null
            : SparkCompatUtilities.ToJson(request.CursorPosition);

        await DbContext.SaveChangesAsync();

        await CleanupStalePresenceAsync();

        var payload = ToPresenceDto(snapshot);
        return Ok(ResponseMapperUtilities.MapToEnvelope(payload, HttpContext.TraceIdentifier));
    }

    [HttpGet("presence")]
    public async Task<ActionResult<ResponseEnvelope<PresenceListDto>>> ListPresence([FromQuery] string? chatId = null)
    {
        if (!IsCompatEnabled || !_rolloutOptions.EnablePresence)
        {
            return DisabledResponse<PresenceListDto>("presence");
        }

        await CleanupStalePresenceAsync();

        var query = DbContext.SparkCompatPresenceSnapshots.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(chatId))
        {
            query = query.Where(snapshot => snapshot.ActiveChatId == chatId);
        }

        var users = await query
            .OrderByDescending(snapshot => snapshot.LastSeenAt)
            .Select(snapshot => ToPresenceDto(snapshot))
            .ToListAsync();

        var payload = new PresenceListDto { Users = users };
        return Ok(ResponseMapperUtilities.MapToEnvelope(payload, HttpContext.TraceIdentifier));
    }

    private async Task CleanupStalePresenceAsync()
    {
        var cutoff = DateTime.UtcNow.Subtract(PresenceTimeout);
        var staleEntries = await DbContext.SparkCompatPresenceSnapshots
            .Where(snapshot => snapshot.LastSeenAt < cutoff)
            .ToListAsync();

        if (staleEntries.Count == 0)
        {
            return;
        }

        DbContext.SparkCompatPresenceSnapshots.RemoveRange(staleEntries);
        await DbContext.SaveChangesAsync();
    }

    private static PresenceDto ToPresenceDto(SparkCompatPresenceSnapshot snapshot)
    {
        return new PresenceDto
        {
            UserId = snapshot.UserId.ToString(),
            UserName = snapshot.UserName,
            AvatarUrl = snapshot.AvatarUrl,
            ActiveChat = snapshot.ActiveChatId,
            LastSeen = SparkCompatUtilities.ToUnixMilliseconds(snapshot.LastSeenAt),
            IsTyping = snapshot.IsTyping,
            TypingChatId = snapshot.TypingChatId,
            CursorPosition = string.IsNullOrWhiteSpace(snapshot.CursorPositionJson)
                ? null
                : JsonSerializer.Deserialize<CursorPositionDto>(snapshot.CursorPositionJson)
        };
    }

    public sealed class UpdatePresenceRequest
    {
        public string? ActiveChat { get; set; }
        public bool IsTyping { get; set; }
        public string? TypingChatId { get; set; }
        public CursorPositionDto? CursorPosition { get; set; }
    }

    public sealed class CursorPositionDto
    {
        public string? ChatId { get; set; }
        public string? MessageId { get; set; }
    }

    public sealed class PresenceDto
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public string? ActiveChat { get; set; }
        public long LastSeen { get; set; }
        public bool IsTyping { get; set; }
        public string? TypingChatId { get; set; }
        public CursorPositionDto? CursorPosition { get; set; }
    }

    public sealed class PresenceListDto
    {
        public List<PresenceDto> Users { get; set; } = new();
    }
}
