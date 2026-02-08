using bmadServer.ApiService.Configuration;
using bmadServer.ApiService.Data;
using bmadServer.ApiService.Data.Entities.SparkCompat;
using bmadServer.ApiService.DTOs.SparkCompat;
using bmadServer.ApiService.Hubs;
using bmadServer.ApiService.Services.SparkCompat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace bmadServer.ApiService.Controllers.SparkCompat;

[ApiController]
[Route("v1/collaboration-events")]
[Authorize]
public class CollaborationEventsCompatController : SparkCompatControllerBase
{
    private readonly SparkCompatRolloutOptions _rolloutOptions;
    private readonly IHubContext<ChatHub> _hubContext;

    public CollaborationEventsCompatController(
        ApplicationDbContext dbContext,
        IHubContext<ChatHub> hubContext,
        IOptions<SparkCompatRolloutOptions> rolloutOptions)
        : base(dbContext, rolloutOptions)
    {
        _hubContext = hubContext;
        _rolloutOptions = rolloutOptions.Value;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ResponseEnvelope<CollaborationEventListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseEnvelope<CollaborationEventListDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResponseEnvelope<CollaborationEventListDto>>> GetEvents(
        [FromQuery] string? domain = null,
        [FromQuery] long? since = null,
        [FromQuery] int limit = 50,
        [FromQuery] int offset = 0)
    {
        if (!IsCompatEnabled || !_rolloutOptions.EnableCollaborationEvents)
        {
            return DisabledResponse<CollaborationEventListDto>("collaboration-events");
        }

        limit = Math.Clamp(limit, 1, 200);
        offset = Math.Max(offset, 0);

        var query = DbContext.SparkCompatCollaborationEvents.AsNoTracking();

        // Default to last 24 hours if no since provided
        var sinceTime = since.HasValue
            ? SparkCompatUtilities.FromUnixMilliseconds(since.Value)
            : DateTime.UtcNow.AddHours(-24);
        query = query.Where(evt => evt.Timestamp > sinceTime);

        if (!string.IsNullOrWhiteSpace(domain))
        {
            query = ApplyDomainFilter(query, domain);
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(evt => evt.Timestamp)
            .ThenBy(evt => evt.Id)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();

        var payload = new CollaborationEventListDto
        {
            Events = items.Select(MapEvent).ToList(),
            Total = total,
            Limit = limit,
            Offset = offset
        };

        return Ok(ResponseMapperUtilities.MapToEnvelope(payload, HttpContext.TraceIdentifier));
    }

    /// <summary>
    /// Applies domain-based type filtering using explicit OR conditions
    /// to ensure EF Core can translate to SQL properly.
    /// </summary>
    private static IQueryable<SparkCompatCollaborationEvent> ApplyDomainFilter(
        IQueryable<SparkCompatCollaborationEvent> query, string domain)
    {
        return domain.ToLowerInvariant() switch
        {
            "pr" => query.Where(evt => evt.Type.StartsWith("pr_") || evt.Type.StartsWith("line_comment_")),
            "chat" => query.Where(evt => evt.Type.StartsWith("message_")),
            "decision" => query.Where(evt => evt.Type.StartsWith("decision_")),
            "auth" => query.Where(evt => evt.Type.StartsWith("user_")),
            _ => query
        };
    }

    [HttpPost]
    [ProducesResponseType(typeof(ResponseEnvelope<CollaborationEventDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ResponseEnvelope<CollaborationEventDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResponseEnvelope<CollaborationEventDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ResponseEnvelope<CollaborationEventDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResponseEnvelope<CollaborationEventDto>>> PublishEvent([FromBody] CreateCollaborationEventRequest request)
    {
        if (!IsCompatEnabled || !_rolloutOptions.EnableCollaborationEvents)
        {
            return DisabledResponse<CollaborationEventDto>("collaboration-events");
        }

        var user = await GetCurrentUserAsync();
        if (user == null)
        {
            return Unauthorized(ResponseMapperUtilities.MapError<CollaborationEventDto>(StatusCodes.Status401Unauthorized, "Authentication required.", HttpContext.TraceIdentifier));
        }

        if (string.IsNullOrWhiteSpace(request.Type))
        {
            return BadRequest(ResponseMapperUtilities.MapError<CollaborationEventDto>(StatusCodes.Status400BadRequest, "Event type is required.", HttpContext.TraceIdentifier));
        }

        var now = DateTime.UtcNow;
        var evt = new SparkCompatCollaborationEvent
        {
            Id = SparkCompatUtilities.CreateId("event"),
            Type = request.Type,
            UserId = user.Id,
            UserName = user.DisplayName,
            ChatId = request.ChatId,
            PrId = request.PrId,
            Timestamp = now,
            MetadataJson = request.Metadata == null ? null : SparkCompatUtilities.ToJson(request.Metadata),
            WorkflowMetadataJson = request.WorkflowMetadata == null ? null : SparkCompatUtilities.ToJson(request.WorkflowMetadata),
            DecisionMetadataJson = request.DecisionMetadata == null ? null : SparkCompatUtilities.ToJson(request.DecisionMetadata)
        };

        DbContext.SparkCompatCollaborationEvents.Add(evt);
        await DbContext.SaveChangesAsync();

        var dto = MapEvent(evt);
        await _hubContext.Clients.All.SendAsync("SparkCompatEvent", dto);
        if (!string.IsNullOrWhiteSpace(evt.ChatId))
        {
            await _hubContext.Clients.Group($"chat-{evt.ChatId}").SendAsync("SparkCompatEvent", dto);
        }

        var envelope = ResponseMapperUtilities.MapToEnvelope(dto, StatusCodes.Status201Created, HttpContext.TraceIdentifier, "Event published");
        return StatusCode(StatusCodes.Status201Created, envelope);
    }

    private static CollaborationEventDto MapEvent(SparkCompatCollaborationEvent evt)
    {
        return new CollaborationEventDto
        {
            Id = evt.Id,
            Type = evt.Type,
            UserId = evt.UserId.ToString(),
            UserName = evt.UserName,
            ChatId = evt.ChatId,
            PrId = evt.PrId,
            Timestamp = SparkCompatUtilities.ToUnixMilliseconds(evt.Timestamp),
            Metadata = SparkCompatUtilities.FromJson<Dictionary<string, object>>(evt.MetadataJson),
            WorkflowMetadata = SparkCompatUtilities.FromJson<Dictionary<string, object>>(evt.WorkflowMetadataJson),
            DecisionMetadata = SparkCompatUtilities.FromJson<Dictionary<string, object>>(evt.DecisionMetadataJson)
        };
    }

    public sealed class CreateCollaborationEventRequest
    {
        public string Type { get; set; } = string.Empty;
        public string? ChatId { get; set; }
        public string? PrId { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
        public Dictionary<string, object>? WorkflowMetadata { get; set; }
        public Dictionary<string, object>? DecisionMetadata { get; set; }
    }

    public sealed class CollaborationEventListDto
    {
        public List<CollaborationEventDto> Events { get; set; } = new();
        public int Total { get; set; }
        public int Limit { get; set; }
        public int Offset { get; set; }
    }

    public sealed class CollaborationEventDto
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string? ChatId { get; set; }
        public string? PrId { get; set; }
        public long Timestamp { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
        public Dictionary<string, object>? WorkflowMetadata { get; set; }
        public Dictionary<string, object>? DecisionMetadata { get; set; }
    }
}
