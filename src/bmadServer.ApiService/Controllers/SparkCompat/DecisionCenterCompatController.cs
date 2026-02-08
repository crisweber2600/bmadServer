using System.Text.Json;
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
[Route("v1/decisions")]
[Authorize]
public class DecisionCenterCompatController : SparkCompatControllerBase
{
    private readonly SparkCompatRolloutOptions _rolloutOptions;
    private readonly IHubContext<ChatHub> _hubContext;

    public DecisionCenterCompatController(
        ApplicationDbContext dbContext,
        IHubContext<ChatHub> hubContext,
        IOptions<SparkCompatRolloutOptions> rolloutOptions)
        : base(dbContext, rolloutOptions)
    {
        _hubContext = hubContext;
        _rolloutOptions = rolloutOptions.Value;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ResponseEnvelope<DecisionListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseEnvelope<DecisionListDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResponseEnvelope<DecisionListDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResponseEnvelope<DecisionListDto>>> ListDecisions(
        [FromQuery] string chatId,
        [FromQuery] string? status = null,
        [FromQuery] int limit = 50,
        [FromQuery] int offset = 0)
    {
        if (!IsCompatEnabled || !_rolloutOptions.EnableDecisionCenter)
        {
            return DisabledResponse<DecisionListDto>("decisions");
        }

        if (string.IsNullOrWhiteSpace(chatId))
        {
            return BadRequest(ResponseMapperUtilities.MapError<DecisionListDto>(StatusCodes.Status400BadRequest, "chatId is required.", HttpContext.TraceIdentifier));
        }

        limit = Math.Clamp(limit, 1, 200);
        offset = Math.Max(offset, 0);

        var query = DbContext.SparkCompatDecisions
            .AsNoTracking()
            .Include(decision => decision.Conflicts)
            .Where(decision => decision.ChatId == chatId);

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(decision => decision.Status == status);
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(decision => decision.UpdatedAt)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();

        var payload = new DecisionListDto
        {
            Decisions = items.Select(MapDecision).ToList(),
            Total = total,
            Limit = limit,
            Offset = offset
        };

        return Ok(ResponseMapperUtilities.MapToEnvelope(payload, HttpContext.TraceIdentifier));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ResponseEnvelope<DecisionDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ResponseEnvelope<DecisionDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResponseEnvelope<DecisionDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ResponseEnvelope<DecisionDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResponseEnvelope<DecisionDto>>> CreateDecision([FromBody] CreateDecisionRequest request)
    {
        if (!IsCompatEnabled || !_rolloutOptions.EnableDecisionCenter)
        {
            return DisabledResponse<DecisionDto>("decisions");
        }

        var user = await GetCurrentUserAsync();
        if (user == null)
        {
            return Unauthorized(ResponseMapperUtilities.MapError<DecisionDto>(StatusCodes.Status401Unauthorized, "Authentication required.", HttpContext.TraceIdentifier));
        }

        if (string.IsNullOrWhiteSpace(request.ChatId) || string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest(ResponseMapperUtilities.MapError<DecisionDto>(StatusCodes.Status400BadRequest, "chatId and title are required.", HttpContext.TraceIdentifier));
        }

        var valueValidationError = ValidateDecisionValue(request.Value, isNew: true);
        if (valueValidationError != null)
        {
            return BadRequest(ResponseMapperUtilities.MapError<DecisionDto>(StatusCodes.Status400BadRequest, valueValidationError, HttpContext.TraceIdentifier));
        }

        var now = DateTime.UtcNow;
        var decision = new SparkCompatDecision
        {
            Id = SparkCompatUtilities.CreateId("decision"),
            ChatId = request.ChatId,
            Title = request.Title.Trim(),
            ValueJson = request.Value.GetRawText(),
            CreatedBy = user.Id,
            CreatedAt = now,
            UpdatedAt = now,
            CurrentVersion = 1,
            Status = "open"
        };

        decision.Versions.Add(new SparkCompatDecisionVersion
        {
            DecisionId = decision.Id,
            VersionNumber = 1,
            ValueJson = decision.ValueJson,
            ChangedBy = user.Id,
            ChangedAt = now,
            Reason = request.Reason ?? "initial"
        });

        DbContext.SparkCompatDecisions.Add(decision);
        await DbContext.SaveChangesAsync();
        await GenerateConflictsAsync(decision);
        await PublishDecisionEventAsync(decision, "decision_created", new { decisionId = decision.Id, chatId = decision.ChatId });

        var envelope = ResponseMapperUtilities.MapToEnvelope(MapDecision(decision), StatusCodes.Status201Created, HttpContext.TraceIdentifier, "Decision created");
        return StatusCode(StatusCodes.Status201Created, envelope);
    }

    [HttpPatch("{decisionId}")]
    [ProducesResponseType(typeof(ResponseEnvelope<DecisionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseEnvelope<DecisionDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResponseEnvelope<DecisionDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ResponseEnvelope<DecisionDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ResponseEnvelope<DecisionDto>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ResponseEnvelope<DecisionDto>), 423)]
    public async Task<ActionResult<ResponseEnvelope<DecisionDto>>> UpdateDecision(string decisionId, [FromBody] UpdateDecisionRequest request)
    {
        if (!IsCompatEnabled || !_rolloutOptions.EnableDecisionCenter)
        {
            return DisabledResponse<DecisionDto>("decisions");
        }

        var user = await GetCurrentUserAsync();
        if (user == null)
        {
            return Unauthorized(ResponseMapperUtilities.MapError<DecisionDto>(StatusCodes.Status401Unauthorized, "Authentication required.", HttpContext.TraceIdentifier));
        }

        var decision = await DbContext.SparkCompatDecisions
            .Include(item => item.Conflicts)
            .FirstOrDefaultAsync(item => item.Id == decisionId);
        if (decision == null)
        {
            return NotFound(ResponseMapperUtilities.MapError<DecisionDto>(StatusCodes.Status404NotFound, "Decision not found.", HttpContext.TraceIdentifier));
        }

        if (decision.IsLocked && decision.LockedBy != user.Id)
        {
            return StatusCode(StatusCodes.Status423Locked, ResponseMapperUtilities.MapError<DecisionDto>(StatusCodes.Status423Locked, "Decision is locked by another user.", HttpContext.TraceIdentifier));
        }

        // Optimistic concurrency check
        if (request.ExpectedVersion.HasValue && decision.CurrentVersion != request.ExpectedVersion.Value)
        {
            return Conflict(ResponseMapperUtilities.MapError<DecisionDto>(
                StatusCodes.Status409Conflict,
                $"Version conflict. Expected version {request.ExpectedVersion.Value} but current version is {decision.CurrentVersion}. Please refresh and retry.",
                HttpContext.TraceIdentifier));
        }

        if (!string.IsNullOrWhiteSpace(request.Title))
        {
            decision.Title = request.Title.Trim();
        }

        if (request.Value.HasValue)
        {
            var valueValidationError = ValidateDecisionValue(request.Value.Value, isNew: false);
            if (valueValidationError != null)
            {
                return BadRequest(ResponseMapperUtilities.MapError<DecisionDto>(StatusCodes.Status400BadRequest, valueValidationError, HttpContext.TraceIdentifier));
            }

            decision.ValueJson = request.Value.Value.GetRawText();
        }

        decision.CurrentVersion++;
        decision.UpdatedAt = DateTime.UtcNow;

        DbContext.SparkCompatDecisionVersions.Add(new SparkCompatDecisionVersion
        {
            DecisionId = decision.Id,
            VersionNumber = decision.CurrentVersion,
            ValueJson = decision.ValueJson,
            ChangedBy = user.Id,
            ChangedAt = DateTime.UtcNow,
            Reason = request.Reason ?? "update",
            AuditMetadataJson = SparkCompatUtilities.ToJson(new
            {
                updatedBy = user.Id,
                updatedAt = SparkCompatUtilities.ToUnixMilliseconds(DateTime.UtcNow)
            })
        });

        await DbContext.SaveChangesAsync();
        await GenerateConflictsAsync(decision);
        await PublishDecisionEventAsync(decision, "decision_updated", new { decisionId = decision.Id, version = decision.CurrentVersion });

        return Ok(ResponseMapperUtilities.MapToEnvelope(MapDecision(decision), HttpContext.TraceIdentifier, "Decision updated"));
    }

    [HttpPost("{decisionId}/lock")]
    [ProducesResponseType(typeof(ResponseEnvelope<DecisionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseEnvelope<DecisionDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ResponseEnvelope<DecisionDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ResponseEnvelope<DecisionDto>), 423)]
    public async Task<ActionResult<ResponseEnvelope<DecisionDto>>> LockDecision(string decisionId, [FromBody] LockDecisionRequest request)
    {
        return await SetLockStateAsync(decisionId, true, request.Reason);
    }

    [HttpPost("{decisionId}/unlock")]
    [ProducesResponseType(typeof(ResponseEnvelope<DecisionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseEnvelope<DecisionDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ResponseEnvelope<DecisionDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResponseEnvelope<DecisionDto>>> UnlockDecision(string decisionId)
    {
        return await SetLockStateAsync(decisionId, false, null);
    }

    [HttpGet("{decisionId}/history")]
    [ProducesResponseType(typeof(ResponseEnvelope<DecisionHistoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseEnvelope<DecisionHistoryDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResponseEnvelope<DecisionHistoryDto>>> GetDecisionHistory(string decisionId)
    {
        if (!IsCompatEnabled || !_rolloutOptions.EnableDecisionCenter)
        {
            return DisabledResponse<DecisionHistoryDto>("decisions");
        }

        var versions = await DbContext.SparkCompatDecisionVersions
            .AsNoTracking()
            .Where(version => version.DecisionId == decisionId)
            .OrderByDescending(version => version.VersionNumber)
            .ToListAsync();

        if (versions.Count == 0)
        {
            return NotFound(ResponseMapperUtilities.MapError<DecisionHistoryDto>(StatusCodes.Status404NotFound, "Decision history not found.", HttpContext.TraceIdentifier));
        }

        var payload = new DecisionHistoryDto
        {
            Versions = versions.Select(version => new DecisionVersionDto
            {
                Id = version.Id,
                VersionNumber = version.VersionNumber,
                Value = JsonDocument.Parse(version.ValueJson).RootElement,
                ChangedBy = version.ChangedBy.ToString(),
                ChangedAt = SparkCompatUtilities.ToUnixMilliseconds(version.ChangedAt),
                Reason = version.Reason
            }).ToList()
        };

        return Ok(ResponseMapperUtilities.MapToEnvelope(payload, HttpContext.TraceIdentifier));
    }

    [HttpGet("{decisionId}/conflicts")]
    [ProducesResponseType(typeof(ResponseEnvelope<DecisionConflictListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseEnvelope<DecisionConflictListDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResponseEnvelope<DecisionConflictListDto>>> GetConflicts(string decisionId)
    {
        if (!IsCompatEnabled || !_rolloutOptions.EnableDecisionCenter)
        {
            return DisabledResponse<DecisionConflictListDto>("decisions");
        }

        var conflicts = await DbContext.SparkCompatDecisionConflicts
            .AsNoTracking()
            .Where(conflict => conflict.DecisionId == decisionId)
            .OrderByDescending(conflict => conflict.DetectedAt)
            .ToListAsync();

        var payload = new DecisionConflictListDto
        {
            Conflicts = conflicts.Select(MapConflict).ToList()
        };

        return Ok(ResponseMapperUtilities.MapToEnvelope(payload, HttpContext.TraceIdentifier));
    }

    [HttpPost("{decisionId}/conflicts/{conflictId:guid}/resolve")]
    [ProducesResponseType(typeof(ResponseEnvelope<DecisionConflictDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseEnvelope<DecisionConflictDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ResponseEnvelope<DecisionConflictDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResponseEnvelope<DecisionConflictDto>>> ResolveConflict(string decisionId, Guid conflictId, [FromBody] ResolveConflictRequest request)
    {
        if (!IsCompatEnabled || !_rolloutOptions.EnableDecisionCenter)
        {
            return DisabledResponse<DecisionConflictDto>("decisions");
        }

        var user = await GetCurrentUserAsync();
        if (user == null)
        {
            return Unauthorized(ResponseMapperUtilities.MapError<DecisionConflictDto>(StatusCodes.Status401Unauthorized, "Authentication required.", HttpContext.TraceIdentifier));
        }

        var conflict = await DbContext.SparkCompatDecisionConflicts.FirstOrDefaultAsync(item => item.Id == conflictId && item.DecisionId == decisionId);
        if (conflict == null)
        {
            return NotFound(ResponseMapperUtilities.MapError<DecisionConflictDto>(StatusCodes.Status404NotFound, "Conflict not found.", HttpContext.TraceIdentifier));
        }

        conflict.Status = "resolved";
        conflict.ResolvedAt = DateTime.UtcNow;
        conflict.ResolvedBy = user.Id;
        conflict.ResolutionJson = SparkCompatUtilities.ToJson(new { resolution = request.Resolution });
        conflict.AuditMetadataJson = SparkCompatUtilities.ToJson(new
        {
            resolvedBy = user.Id,
            resolvedAt = SparkCompatUtilities.ToUnixMilliseconds(DateTime.UtcNow),
            request.Resolution
        });

        await DbContext.SaveChangesAsync();
        await PublishDecisionEventAsync(new SparkCompatDecision { Id = decisionId }, "decision_conflict_resolved", new { decisionId, conflictId });

        return Ok(ResponseMapperUtilities.MapToEnvelope(MapConflict(conflict), HttpContext.TraceIdentifier, "Conflict resolved"));
    }

    private async Task<ActionResult<ResponseEnvelope<DecisionDto>>> SetLockStateAsync(string decisionId, bool lockState, string? reason)
    {
        if (!IsCompatEnabled || !_rolloutOptions.EnableDecisionCenter)
        {
            return DisabledResponse<DecisionDto>("decisions");
        }

        var user = await GetCurrentUserAsync();
        if (user == null)
        {
            return Unauthorized(ResponseMapperUtilities.MapError<DecisionDto>(StatusCodes.Status401Unauthorized, "Authentication required.", HttpContext.TraceIdentifier));
        }

        var decision = await DbContext.SparkCompatDecisions
            .Include(item => item.Conflicts)
            .FirstOrDefaultAsync(item => item.Id == decisionId);
        if (decision == null)
        {
            return NotFound(ResponseMapperUtilities.MapError<DecisionDto>(StatusCodes.Status404NotFound, "Decision not found.", HttpContext.TraceIdentifier));
        }

        if (lockState && decision.IsLocked && decision.LockedBy != user.Id)
        {
            return StatusCode(StatusCodes.Status423Locked, ResponseMapperUtilities.MapError<DecisionDto>(StatusCodes.Status423Locked, "Decision is already locked by another user.", HttpContext.TraceIdentifier));
        }

        decision.IsLocked = lockState;
        decision.LockedBy = lockState ? user.Id : null;
        decision.LockedAt = lockState ? DateTime.UtcNow : null;
        decision.UpdatedAt = DateTime.UtcNow;
        decision.Status = lockState ? "locked" : "open";

        await DbContext.SaveChangesAsync();
        await PublishDecisionEventAsync(decision, lockState ? "decision_locked" : "decision_unlocked", new { decisionId = decision.Id, reason });

        return Ok(ResponseMapperUtilities.MapToEnvelope(MapDecision(decision), HttpContext.TraceIdentifier, lockState ? "Decision locked" : "Decision unlocked"));
    }

    /// <summary>
    /// Validates the decision value JSON structure.
    /// For new decisions: requires question, decisionType, and options with at least 2 items.
    /// For updates: validates structure if present.
    /// </summary>
    private static string? ValidateDecisionValue(JsonElement value, bool isNew)
    {
        try
        {
            if (value.ValueKind != JsonValueKind.Object)
            {
                return "Decision value must be a JSON object.";
            }

            if (isNew)
            {
                if (!value.TryGetProperty("question", out var question) || question.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(question.GetString()))
                {
                    return "Decision value must contain a non-empty 'question' field.";
                }

                if (!value.TryGetProperty("decisionType", out var decisionType) || decisionType.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(decisionType.GetString()))
                {
                    return "Decision value must contain a non-empty 'decisionType' field.";
                }

                if (value.TryGetProperty("options", out var options))
                {
                    if (options.ValueKind != JsonValueKind.Array)
                    {
                        return "Decision 'options' must be an array.";
                    }
                    if (options.GetArrayLength() < 2)
                    {
                        return "Decision must have at least 2 options.";
                    }
                }
            }
            else
            {
                // For updates, validate options count if present
                if (value.TryGetProperty("options", out var options))
                {
                    if (options.ValueKind != JsonValueKind.Array)
                    {
                        return "Decision 'options' must be an array.";
                    }
                    if (options.GetArrayLength() < 2)
                    {
                        return "Decision must have at least 2 options.";
                    }
                }
            }

            return null;
        }
        catch (Exception)
        {
            return "Decision value contains malformed JSON.";
        }
    }

    private async Task GenerateConflictsAsync(SparkCompatDecision decision)
    {
        var siblings = await DbContext.SparkCompatDecisions
            .Where(item => item.ChatId == decision.ChatId && item.Id != decision.Id && item.Title == decision.Title)
            .ToListAsync();

        foreach (var sibling in siblings)
        {
            if (string.Equals(sibling.ValueJson, decision.ValueJson, StringComparison.Ordinal))
            {
                continue;
            }

            var exists = await DbContext.SparkCompatDecisionConflicts
                .AnyAsync(conflict => conflict.DecisionId == decision.Id && conflict.ConflictType == "value_mismatch" && conflict.Status == "open");

            if (exists)
            {
                continue;
            }

            DbContext.SparkCompatDecisionConflicts.Add(new SparkCompatDecisionConflict
            {
                DecisionId = decision.Id,
                ConflictType = "value_mismatch",
                Description = $"Decision '{decision.Title}' conflicts with another active version in this chat.",
                Status = "open",
                DetectedAt = DateTime.UtcNow,
                AuditMetadataJson = SparkCompatUtilities.ToJson(new
                {
                    conflictingDecisionId = sibling.Id,
                    detectedAt = SparkCompatUtilities.ToUnixMilliseconds(DateTime.UtcNow)
                })
            });
        }

        await DbContext.SaveChangesAsync();
    }

    private async Task PublishDecisionEventAsync(SparkCompatDecision decision, string eventType, object metadata)
    {
        var actorId = TryGetCurrentUserId() ?? Guid.Empty;
        var actor = await GetCurrentUserAsync();
        var actorName = actor?.DisplayName ?? "Unknown User";

        var evt = new SparkCompatCollaborationEvent
        {
            Id = SparkCompatUtilities.CreateId("event"),
            Type = eventType,
            UserId = actorId,
            UserName = actorName,
            ChatId = decision.ChatId,
            Timestamp = DateTime.UtcNow,
            DecisionMetadataJson = SparkCompatUtilities.ToJson(metadata)
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
            timestamp = SparkCompatUtilities.ToUnixMilliseconds(evt.Timestamp),
            decisionMetadata = metadata
        });
    }

    private static DecisionDto MapDecision(SparkCompatDecision decision)
    {
        return new DecisionDto
        {
            Id = decision.Id,
            ChatId = decision.ChatId,
            Title = decision.Title,
            Value = JsonDocument.Parse(decision.ValueJson).RootElement,
            Status = decision.Status,
            IsLocked = decision.IsLocked,
            LockedBy = decision.LockedBy?.ToString(),
            LockedAt = decision.LockedAt.HasValue ? SparkCompatUtilities.ToUnixMilliseconds(decision.LockedAt.Value) : null,
            Version = decision.CurrentVersion,
            CreatedAt = SparkCompatUtilities.ToUnixMilliseconds(decision.CreatedAt),
            UpdatedAt = SparkCompatUtilities.ToUnixMilliseconds(decision.UpdatedAt),
            OpenConflictCount = decision.Conflicts.Count(conflict => conflict.Status == "open")
        };
    }

    private static DecisionConflictDto MapConflict(SparkCompatDecisionConflict conflict)
    {
        return new DecisionConflictDto
        {
            Id = conflict.Id,
            DecisionId = conflict.DecisionId,
            ConflictType = conflict.ConflictType,
            Description = conflict.Description,
            Status = conflict.Status,
            DetectedAt = SparkCompatUtilities.ToUnixMilliseconds(conflict.DetectedAt),
            ResolvedAt = conflict.ResolvedAt.HasValue ? SparkCompatUtilities.ToUnixMilliseconds(conflict.ResolvedAt.Value) : null,
            ResolvedBy = conflict.ResolvedBy?.ToString(),
            Resolution = SparkCompatUtilities.FromJson<Dictionary<string, object>>(conflict.ResolutionJson)
        };
    }

    public sealed class CreateDecisionRequest
    {
        public string ChatId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public JsonElement Value { get; set; }
        public string? Reason { get; set; }
    }

    public sealed class UpdateDecisionRequest
    {
        public string? Title { get; set; }
        public JsonElement? Value { get; set; }
        public string? Reason { get; set; }
        public int? ExpectedVersion { get; set; }
    }

    public sealed class LockDecisionRequest
    {
        public string? Reason { get; set; }
    }

    public sealed class ResolveConflictRequest
    {
        public string Resolution { get; set; } = string.Empty;
    }

    public sealed class DecisionListDto
    {
        public List<DecisionDto> Decisions { get; set; } = new();
        public int Total { get; set; }
        public int Limit { get; set; }
        public int Offset { get; set; }
    }

    public sealed class DecisionDto
    {
        public string Id { get; set; } = string.Empty;
        public string ChatId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public JsonElement Value { get; set; }
        public string Status { get; set; } = "open";
        public bool IsLocked { get; set; }
        public string? LockedBy { get; set; }
        public long? LockedAt { get; set; }
        public int Version { get; set; }
        public long CreatedAt { get; set; }
        public long UpdatedAt { get; set; }
        public int OpenConflictCount { get; set; }
    }

    public sealed class DecisionHistoryDto
    {
        public List<DecisionVersionDto> Versions { get; set; } = new();
    }

    public sealed class DecisionVersionDto
    {
        public Guid Id { get; set; }
        public int VersionNumber { get; set; }
        public JsonElement Value { get; set; }
        public string ChangedBy { get; set; } = string.Empty;
        public long ChangedAt { get; set; }
        public string? Reason { get; set; }
    }

    public sealed class DecisionConflictListDto
    {
        public List<DecisionConflictDto> Conflicts { get; set; } = new();
    }

    public sealed class DecisionConflictDto
    {
        public Guid Id { get; set; }
        public string DecisionId { get; set; } = string.Empty;
        public string ConflictType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = "open";
        public long DetectedAt { get; set; }
        public long? ResolvedAt { get; set; }
        public string? ResolvedBy { get; set; }
        public Dictionary<string, object>? Resolution { get; set; }
    }
}
