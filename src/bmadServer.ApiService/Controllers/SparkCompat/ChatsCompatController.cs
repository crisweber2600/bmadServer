using System.Text.Json;
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
[Route("v1/chats")]
[Authorize]
public class ChatsCompatController : SparkCompatControllerBase
{
    private readonly SparkCompatRolloutOptions _rolloutOptions;
    private readonly ITranslationService _translationService;
    private readonly IResponseMetadataService _responseMetadataService;
    private readonly IHubContext<ChatHub> _hubContext;

    public ChatsCompatController(
        ApplicationDbContext dbContext,
        ITranslationService translationService,
        IResponseMetadataService responseMetadataService,
        IHubContext<ChatHub> hubContext,
        IOptions<SparkCompatRolloutOptions> rolloutOptions)
        : base(dbContext, rolloutOptions)
    {
        _translationService = translationService;
        _responseMetadataService = responseMetadataService;
        _hubContext = hubContext;
        _rolloutOptions = rolloutOptions.Value;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ResponseEnvelope<ChatListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseEnvelope<ChatListDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResponseEnvelope<ChatListDto>>> ListChats(
        [FromQuery] string? domain = null,
        [FromQuery] string? service = null,
        [FromQuery] string? feature = null,
        [FromQuery] int limit = 50,
        [FromQuery] int offset = 0)
    {
        if (!IsCompatEnabled || !_rolloutOptions.EnableChats)
        {
            return DisabledResponse<ChatListDto>("chats");
        }

        limit = Math.Clamp(limit, 1, 100);
        offset = Math.Max(offset, 0);

        var query = DbContext.SparkCompatChats.AsNoTracking()
            .Where(chat => !chat.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(domain))
        {
            query = query.Where(chat => chat.Domain == domain);
        }

        if (!string.IsNullOrWhiteSpace(service))
        {
            query = query.Where(chat => chat.Service == service);
        }

        if (!string.IsNullOrWhiteSpace(feature))
        {
            query = query.Where(chat => chat.Feature == feature);
        }

        var total = await query.CountAsync();
        var chats = await query
            .OrderByDescending(chat => chat.UpdatedAt)
            .Skip(offset)
            .Take(limit)
            .Select(chat => new ChatSummaryDto
            {
                Id = chat.Id,
                Title = chat.Title,
                CreatedAt = SparkCompatUtilities.ToUnixMilliseconds(chat.CreatedAt),
                UpdatedAt = SparkCompatUtilities.ToUnixMilliseconds(chat.UpdatedAt),
                Domain = chat.Domain,
                Service = chat.Service,
                Feature = chat.Feature,
                MessageCount = chat.Messages.Count,
                LastMessageAt = chat.Messages.Any()
                    ? SparkCompatUtilities.ToUnixMilliseconds(chat.Messages.Max(m => m.Timestamp))
                    : (long?)null,
                CreatedByUserId = chat.CreatedByUserId.ToString()
            })
            .ToListAsync();

        var payload = new ChatListDto
        {
            Chats = chats,
            Total = total,
            Limit = limit,
            Offset = offset
        };

        return Ok(ResponseMapperUtilities.MapToEnvelope(payload, HttpContext.TraceIdentifier));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ResponseEnvelope<ChatDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ResponseEnvelope<ChatDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResponseEnvelope<ChatDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ResponseEnvelope<ChatDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResponseEnvelope<ChatDto>>> CreateChat([FromBody] CreateChatRequest request)
    {
        if (!IsCompatEnabled || !_rolloutOptions.EnableChats)
        {
            return DisabledResponse<ChatDto>("chats");
        }

        var user = await GetCurrentUserAsync();
        if (user == null)
        {
            return Unauthorized(ResponseMapperUtilities.MapError<ChatDto>(StatusCodes.Status401Unauthorized, "Authentication required.", HttpContext.TraceIdentifier));
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest(ResponseMapperUtilities.MapError<ChatDto>(StatusCodes.Status400BadRequest, "Chat title is required.", HttpContext.TraceIdentifier));
        }

        var chat = new SparkCompatChat
        {
            Id = SparkCompatUtilities.CreateId("chat"),
            Title = request.Title.Trim(),
            Domain = request.Domain?.Trim(),
            Service = request.Service?.Trim(),
            Feature = request.Feature?.Trim(),
            CreatedByUserId = user.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        DbContext.SparkCompatChats.Add(chat);
        await DbContext.SaveChangesAsync();

        var envelope = ResponseMapperUtilities.MapToEnvelope(MapChat(chat), StatusCodes.Status201Created, HttpContext.TraceIdentifier, "Chat created");
        return StatusCode(StatusCodes.Status201Created, envelope);
    }

    [HttpGet("{chatId}")]
    [ProducesResponseType(typeof(ResponseEnvelope<ChatDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseEnvelope<ChatDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResponseEnvelope<ChatDto>>> GetChat(string chatId)
    {
        if (!IsCompatEnabled || !_rolloutOptions.EnableChats)
        {
            return DisabledResponse<ChatDto>("chats");
        }

        var chat = await DbContext.SparkCompatChats
            .AsNoTracking()
            .Include(candidate => candidate.Messages)
            .FirstOrDefaultAsync(candidate => candidate.Id == chatId && !candidate.IsDeleted);

        if (chat == null)
        {
            return NotFound(ResponseMapperUtilities.MapError<ChatDto>(StatusCodes.Status404NotFound, "Chat not found.", HttpContext.TraceIdentifier));
        }

        return Ok(ResponseMapperUtilities.MapToEnvelope(MapChat(chat), HttpContext.TraceIdentifier));
    }

    [HttpPatch("{chatId}")]
    [ProducesResponseType(typeof(ResponseEnvelope<ChatDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseEnvelope<ChatDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResponseEnvelope<ChatDto>>> UpdateChat(string chatId, [FromBody] UpdateChatRequest request)
    {
        if (!IsCompatEnabled || !_rolloutOptions.EnableChats)
        {
            return DisabledResponse<ChatDto>("chats");
        }

        var chat = await DbContext.SparkCompatChats.Include(candidate => candidate.Messages).FirstOrDefaultAsync(candidate => candidate.Id == chatId && !candidate.IsDeleted);
        if (chat == null)
        {
            return NotFound(ResponseMapperUtilities.MapError<ChatDto>(StatusCodes.Status404NotFound, "Chat not found.", HttpContext.TraceIdentifier));
        }

        if (!string.IsNullOrWhiteSpace(request.Title))
        {
            chat.Title = request.Title.Trim();
        }

        if (request.Domain is not null)
        {
            chat.Domain = request.Domain;
        }

        if (request.Service is not null)
        {
            chat.Service = request.Service;
        }

        if (request.Feature is not null)
        {
            chat.Feature = request.Feature;
        }

        chat.UpdatedAt = DateTime.UtcNow;
        await DbContext.SaveChangesAsync();

        return Ok(ResponseMapperUtilities.MapToEnvelope(MapChat(chat), HttpContext.TraceIdentifier, "Chat updated"));
    }

    [HttpDelete("{chatId}")]
    [ProducesResponseType(typeof(ResponseEnvelope<DeleteResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseEnvelope<DeleteResponse>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResponseEnvelope<DeleteResponse>>> DeleteChat(string chatId)
    {
        if (!IsCompatEnabled || !_rolloutOptions.EnableChats)
        {
            return DisabledResponse<DeleteResponse>("chats");
        }

        var chat = await DbContext.SparkCompatChats.FirstOrDefaultAsync(candidate => candidate.Id == chatId && !candidate.IsDeleted);
        if (chat == null)
        {
            return NotFound(ResponseMapperUtilities.MapError<DeleteResponse>(StatusCodes.Status404NotFound, "Chat not found.", HttpContext.TraceIdentifier));
        }

        chat.IsDeleted = true;
        chat.UpdatedAt = DateTime.UtcNow;
        await DbContext.SaveChangesAsync();

        return Ok(ResponseMapperUtilities.MapToEnvelope(new DeleteResponse { Deleted = true }, HttpContext.TraceIdentifier, "Chat soft-deleted"));
    }

    [HttpGet("{chatId}/messages")]
    [ProducesResponseType(typeof(ResponseEnvelope<MessageListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseEnvelope<MessageListDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResponseEnvelope<MessageListDto>>> ListMessages(
        string chatId,
        [FromQuery] int limit = 50,
        [FromQuery] long? before = null,
        [FromQuery] long? after = null)
    {
        if (!IsCompatEnabled || !_rolloutOptions.EnableChats)
        {
            return DisabledResponse<MessageListDto>("chats");
        }

        var chatExists = await DbContext.SparkCompatChats.AsNoTracking()
            .AnyAsync(c => c.Id == chatId && !c.IsDeleted);
        if (!chatExists)
        {
            return NotFound(ResponseMapperUtilities.MapError<MessageListDto>(StatusCodes.Status404NotFound, "Chat not found.", HttpContext.TraceIdentifier));
        }

        limit = Math.Clamp(limit, 1, 100);
        var query = DbContext.SparkCompatMessages.AsNoTracking().Where(message => message.ChatId == chatId);

        if (before.HasValue)
        {
            var beforeTime = SparkCompatUtilities.FromUnixMilliseconds(before.Value);
            query = query.Where(message => message.Timestamp < beforeTime);
        }

        if (after.HasValue)
        {
            var afterTime = SparkCompatUtilities.FromUnixMilliseconds(after.Value);
            query = query.Where(message => message.Timestamp > afterTime);
        }

        var messages = await query
            .OrderByDescending(message => message.Timestamp)
            .Take(limit + 1)
            .ToListAsync();

        var hasMore = messages.Count > limit;
        var limited = messages.Take(limit).OrderBy(message => message.Timestamp).Select(MapMessage).ToList();

        return Ok(ResponseMapperUtilities.MapToEnvelope(new MessageListDto
        {
            Messages = limited,
            HasMore = hasMore
        }, HttpContext.TraceIdentifier));
    }

    [HttpPost("{chatId}/messages")]
    [ProducesResponseType(typeof(ResponseEnvelope<SendMessageResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ResponseEnvelope<SendMessageResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResponseEnvelope<SendMessageResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ResponseEnvelope<SendMessageResponse>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResponseEnvelope<SendMessageResponse>>> SendMessage(string chatId, [FromBody] SendMessageRequest request)
    {
        if (!IsCompatEnabled || !_rolloutOptions.EnableChats)
        {
            return DisabledResponse<SendMessageResponse>("chats");
        }

        var user = await GetCurrentUserAsync();
        if (user == null)
        {
            return Unauthorized(ResponseMapperUtilities.MapError<SendMessageResponse>(StatusCodes.Status401Unauthorized, "Authentication required.", HttpContext.TraceIdentifier));
        }

        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return BadRequest(ResponseMapperUtilities.MapError<SendMessageResponse>(StatusCodes.Status400BadRequest, "Message content is required.", HttpContext.TraceIdentifier));
        }

        var chat = await DbContext.SparkCompatChats.FirstOrDefaultAsync(candidate => candidate.Id == chatId && !candidate.IsDeleted);
        if (chat == null)
        {
            return NotFound(ResponseMapperUtilities.MapError<SendMessageResponse>(StatusCodes.Status404NotFound, "Chat not found.", HttpContext.TraceIdentifier));
        }

        var persona = string.IsNullOrWhiteSpace(request.PersonaOverride)
            ? user.PersonaType
            : SparkCompatUtilities.ToPersonaType(request.PersonaOverride);

        var userMessage = new SparkCompatMessage
        {
            Id = SparkCompatUtilities.CreateId("msg"),
            ChatId = chatId,
            Role = "user",
            Content = request.Content.Trim(),
            Timestamp = DateTime.UtcNow,
            UserId = user.Id,
            WorkflowContextJson = request.WorkflowContext == null ? null : SparkCompatUtilities.ToJson(request.WorkflowContext),
            AttributionJson = request.AttributionMetadata == null ? null : SparkCompatUtilities.ToJson(request.AttributionMetadata),
            PersonaMetadataJson = SparkCompatUtilities.ToJson(new PersonaMetadataDto
            {
                Persona = SparkCompatUtilities.ToSparkRole(persona),
                ContentType = "user-input",
                HasTechnicalDetails = false,
                WasTranslated = false,
                DisplayName = user.DisplayName,
                AvatarUrl = user.AvatarUrl
            })
        };

        var aiBaseContent = $"Captured decision context for '{chat.Title}'. Next recommended step: review and confirm assumptions before merge.";
        var translated = await _translationService.TranslateToBusinessLanguageAsync(aiBaseContent, persona, request.WorkflowContext?.StepId);
        var metadata = _responseMetadataService.CreateMetadata(translated.Content, persona, translated.WasTranslated);

        var aiMessage = new SparkCompatMessage
        {
            Id = SparkCompatUtilities.CreateId("msg"),
            ChatId = chatId,
            Role = "assistant",
            Content = translated.Content,
            Timestamp = DateTime.UtcNow,
            PersonaMetadataJson = SparkCompatUtilities.ToJson(new PersonaMetadataDto
            {
                Persona = SparkCompatUtilities.ToSparkRole(persona),
                ContentType = metadata.ContentType,
                HasTechnicalDetails = metadata.HasTechnicalDetails,
                TechnicalTermsFound = metadata.TechnicalTermsFound,
                WasTranslated = translated.WasTranslated,
                DisplayName = "Spark Assistant",
                AvatarUrl = null
            })
        };

        DbContext.SparkCompatMessages.Add(userMessage);
        DbContext.SparkCompatMessages.Add(aiMessage);
        chat.UpdatedAt = DateTime.UtcNow;
        await DbContext.SaveChangesAsync();

        // Emit message_sent collaboration event
        await EmitCollaborationEventAsync("message_sent", user.Id, user.DisplayName, chatId, null,
            new { messageId = userMessage.Id, chatId, chatTitle = chat.Title });

        var payload = new SendMessageResponse
        {
            UserMessage = MapMessage(userMessage),
            AiMessage = MapMessage(aiMessage),
            RoutingAssessment = "correctly routed",
            MomentumIndicator = "medium"
        };

        var envelope = ResponseMapperUtilities.MapToEnvelope(payload, StatusCodes.Status201Created, HttpContext.TraceIdentifier, "Message sent");
        return StatusCode(StatusCodes.Status201Created, envelope);
    }

    [HttpPost("{chatId}/messages/{messageId}/translate")]
    [ProducesResponseType(typeof(ResponseEnvelope<TranslateMessageResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseEnvelope<TranslateMessageResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ResponseEnvelope<TranslateMessageResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ResponseEnvelope<TranslateMessageResponse>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ResponseEnvelope<TranslateMessageResponse>>> TranslateMessage(string chatId, string messageId, [FromBody] TranslateMessageRequest request)
    {
        if (!IsCompatEnabled || !_rolloutOptions.EnableChats)
        {
            return DisabledResponse<TranslateMessageResponse>("chats");
        }

        var user = await GetCurrentUserAsync();
        if (user == null)
        {
            return Unauthorized(ResponseMapperUtilities.MapError<TranslateMessageResponse>(StatusCodes.Status401Unauthorized, "Authentication required.", HttpContext.TraceIdentifier));
        }

        var message = await DbContext.SparkCompatMessages.FirstOrDefaultAsync(candidate => candidate.ChatId == chatId && candidate.Id == messageId);
        if (message == null)
        {
            return NotFound(ResponseMapperUtilities.MapError<TranslateMessageResponse>(StatusCodes.Status404NotFound, "Message not found.", HttpContext.TraceIdentifier));
        }

        try
        {
            var targetRole = request.TargetPersona ?? request.Role;
            var persona = string.IsNullOrWhiteSpace(targetRole)
                ? user.PersonaType
                : SparkCompatUtilities.ToPersonaType(targetRole);

            var translated = await _translationService.TranslateToBusinessLanguageAsync(message.Content, persona, request.WorkflowStep);
            var metadata = _responseMetadataService.CreateMetadata(translated.Content, persona, translated.WasTranslated);

            var segment = new TranslatedSegmentDto
            {
                OriginalText = message.Content,
                StartIndex = 0,
                EndIndex = message.Content.Length,
                Explanation = translated.Content,
                Context = translated.AdaptationReason ?? "Translation generated for active persona.",
                SimplifiedText = translated.WasTranslated ? translated.Content : null
            };

            // Persist the translation on the message
            var existingTranslations = SparkCompatUtilities.FromJson<Dictionary<string, object>>(message.TranslationsJson)
                                       ?? new Dictionary<string, object>();
            existingTranslations[SparkCompatUtilities.ToSparkRole(persona)] = new
            {
                content = translated.Content,
                wasTranslated = translated.WasTranslated,
                adaptationReason = translated.AdaptationReason,
                translatedAt = DateTime.UtcNow
            };
            message.TranslationsJson = SparkCompatUtilities.ToJson(existingTranslations);
            await DbContext.SaveChangesAsync();

            var payload = new TranslateMessageResponse
            {
                Translation = new MessageTranslationDto
                {
                    Role = SparkCompatUtilities.ToSparkRole(persona),
                    Segments = new List<TranslatedSegmentDto> { segment }
                },
                PersonaMetadata = new PersonaMetadataDto
                {
                    Persona = SparkCompatUtilities.ToSparkRole(persona),
                    ContentType = metadata.ContentType,
                    HasTechnicalDetails = metadata.HasTechnicalDetails,
                    TechnicalTermsFound = metadata.TechnicalTermsFound,
                    WasTranslated = translated.WasTranslated,
                    DisplayName = user.DisplayName,
                    AvatarUrl = user.AvatarUrl
                }
            };

            return Ok(ResponseMapperUtilities.MapToEnvelope(payload, HttpContext.TraceIdentifier, "Translation complete"));
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                ResponseMapperUtilities.MapError<TranslateMessageResponse>(
                    StatusCodes.Status500InternalServerError,
                    "Translation failed. Retry after refreshing context or selecting a different persona.",
                    HttpContext.TraceIdentifier));
        }
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

        var payload = new
        {
            id = evt.Id,
            type = evt.Type,
            userId = evt.UserId.ToString(),
            userName = evt.UserName,
            chatId = evt.ChatId,
            prId = evt.PrId,
            timestamp = SparkCompatUtilities.ToUnixMilliseconds(evt.Timestamp),
            metadata
        };

        await _hubContext.Clients.All.SendAsync("SparkCompatEvent", payload);
        if (!string.IsNullOrWhiteSpace(evt.ChatId))
        {
            await _hubContext.Clients.Group($"chat-{evt.ChatId}").SendAsync("SparkCompatEvent", payload);
        }
    }

    private static ChatDto MapChat(SparkCompatChat chat)
    {
        var messageDtos = chat.Messages
            .OrderBy(message => message.Timestamp)
            .Select(MapMessage)
            .ToList();

        var participantIds = chat.Messages
            .Where(message => message.UserId.HasValue)
            .Select(message => message.UserId!.Value.ToString())
            .Append(chat.CreatedByUserId.ToString())
            .Distinct()
            .ToList();

        return new ChatDto
        {
            Id = chat.Id,
            Title = chat.Title,
            CreatedAt = SparkCompatUtilities.ToUnixMilliseconds(chat.CreatedAt),
            UpdatedAt = SparkCompatUtilities.ToUnixMilliseconds(chat.UpdatedAt),
            Domain = chat.Domain,
            Service = chat.Service,
            Feature = chat.Feature,
            Participants = participantIds,
            Messages = messageDtos
        };
    }

    private static MessageDto MapMessage(SparkCompatMessage message)
    {
        return new MessageDto
        {
            Id = message.Id,
            ChatId = message.ChatId,
            Content = message.Content,
            Role = message.Role,
            Timestamp = SparkCompatUtilities.ToUnixMilliseconds(message.Timestamp),
            UserId = message.UserId?.ToString(),
            FileChanges = SparkCompatUtilities.FromJson<List<FileChangeDto>>(message.FileChangesJson) ?? new List<FileChangeDto>(),
            PersonaMetadata = SparkCompatUtilities.FromJson<PersonaMetadataDto>(message.PersonaMetadataJson)
        };
    }

    public sealed class CreateChatRequest
    {
        public string Title { get; set; } = string.Empty;
        public string? Domain { get; set; }
        public string? Service { get; set; }
        public string? Feature { get; set; }
    }

    public sealed class UpdateChatRequest
    {
        public string? Title { get; set; }
        public string? Domain { get; set; }
        public string? Service { get; set; }
        public string? Feature { get; set; }
    }

    public sealed class SendMessageRequest
    {
        public string Content { get; set; } = string.Empty;
        public WorkflowContextDto? WorkflowContext { get; set; }
        public Dictionary<string, object>? AttributionMetadata { get; set; }
        public string? PersonaOverride { get; set; }
    }

    public sealed class TranslateMessageRequest
    {
        public string? TargetPersona { get; set; }
        /// <summary>Alias for TargetPersona (backward compat)</summary>
        public string? Role { get; set; }
        public string? WorkflowStep { get; set; }
    }

    public sealed class WorkflowContextDto
    {
        public string? StepId { get; set; }
        public string? WorkflowId { get; set; }
        public string? WorkflowName { get; set; }
    }

    public sealed class DeleteResponse
    {
        public bool Deleted { get; set; }
    }

    public sealed class ChatListDto
    {
        public List<ChatSummaryDto> Chats { get; set; } = new();
        public int Total { get; set; }
        public int Limit { get; set; }
        public int Offset { get; set; }
    }

    /// <summary>
    /// Lightweight chat summary for list endpoints — no messages loaded.
    /// </summary>
    public sealed class ChatSummaryDto
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public long CreatedAt { get; set; }
        public long UpdatedAt { get; set; }
        public string? Domain { get; set; }
        public string? Service { get; set; }
        public string? Feature { get; set; }
        public int MessageCount { get; set; }
        public long? LastMessageAt { get; set; }
        public string CreatedByUserId { get; set; } = string.Empty;
    }

    public sealed class ChatDto
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public long CreatedAt { get; set; }
        public long UpdatedAt { get; set; }
        public List<MessageDto> Messages { get; set; } = new();
        public List<string> Participants { get; set; } = new();
        public string? Domain { get; set; }
        public string? Service { get; set; }
        public string? Feature { get; set; }
    }

    public sealed class MessageListDto
    {
        public List<MessageDto> Messages { get; set; } = new();
        public bool HasMore { get; set; }
    }

    public sealed class MessageDto
    {
        public string Id { get; set; } = string.Empty;
        public string ChatId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public long Timestamp { get; set; }
        public string? UserId { get; set; }
        public List<FileChangeDto> FileChanges { get; set; } = new();
        public PersonaMetadataDto? PersonaMetadata { get; set; }
    }

    public sealed class FileChangeDto
    {
        public string Path { get; set; } = string.Empty;
        public List<string> Additions { get; set; } = new();
        public List<string> Deletions { get; set; } = new();
        public string Status { get; set; } = "pending";
    }

    public sealed class SendMessageResponse
    {
        public MessageDto UserMessage { get; set; } = null!;
        public MessageDto AiMessage { get; set; } = null!;
        public string RoutingAssessment { get; set; } = "correctly routed";
        public string MomentumIndicator { get; set; } = "medium";
    }

    public sealed class TranslateMessageResponse
    {
        public MessageTranslationDto Translation { get; set; } = null!;
        public PersonaMetadataDto PersonaMetadata { get; set; } = null!;
    }

    public sealed class MessageTranslationDto
    {
        public string Role { get; set; } = "business";
        public List<TranslatedSegmentDto> Segments { get; set; } = new();
    }

    public sealed class TranslatedSegmentDto
    {
        public string OriginalText { get; set; } = string.Empty;
        public int StartIndex { get; set; }
        public int EndIndex { get; set; }
        public string Explanation { get; set; } = string.Empty;
        public string Context { get; set; } = string.Empty;
        public string? SimplifiedText { get; set; }
    }

    public sealed class PersonaMetadataDto
    {
        public string Persona { get; set; } = "business";
        public string ContentType { get; set; } = "business";
        public bool HasTechnicalDetails { get; set; }
        public List<string> TechnicalTermsFound { get; set; } = new();
        public bool WasTranslated { get; set; }
        public string? DisplayName { get; set; }
        public string? AvatarUrl { get; set; }
    }
}
