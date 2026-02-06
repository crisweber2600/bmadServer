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
[Route("v1/pull-requests")]
[Authorize]
public class PullRequestsCompatController : SparkCompatControllerBase
{
    private readonly SparkCompatRolloutOptions _rolloutOptions;
    private readonly IRoleService _roleService;
    private readonly IHubContext<ChatHub> _hubContext;

    public PullRequestsCompatController(
        ApplicationDbContext dbContext,
        IRoleService roleService,
        IHubContext<ChatHub> hubContext,
        IOptions<SparkCompatRolloutOptions> rolloutOptions)
        : base(dbContext, rolloutOptions)
    {
        _roleService = roleService;
        _hubContext = hubContext;
        _rolloutOptions = rolloutOptions.Value;
    }

    [HttpGet]
    public async Task<ActionResult<ResponseEnvelope<PullRequestListDto>>> ListPullRequests(
        [FromQuery] string? status = null,
        [FromQuery] string? chatId = null,
        [FromQuery] string? author = null,
        [FromQuery] int limit = 50,
        [FromQuery] int offset = 0)
    {
        if (!IsCompatEnabled || !_rolloutOptions.EnablePullRequests)
        {
            return DisabledResponse<PullRequestListDto>("pull-requests");
        }

        limit = Math.Clamp(limit, 1, 100);
        offset = Math.Max(offset, 0);

        var query = BuildPullRequestQuery();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(pr => pr.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(chatId))
        {
            query = query.Where(pr => pr.ChatId == chatId);
        }

        if (!string.IsNullOrWhiteSpace(author))
        {
            query = query.Where(pr => pr.AuthorName == author || pr.AuthorUserId.ToString() == author);
        }

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(pr => pr.UpdatedAt).Skip(offset).Take(limit).ToListAsync();

        var payload = new PullRequestListDto
        {
            PullRequests = items.Select(MapPullRequest).ToList(),
            Total = total,
            Limit = limit,
            Offset = offset
        };

        return Ok(ResponseMapperUtilities.MapToEnvelope(payload, HttpContext.TraceIdentifier));
    }

    [HttpPost]
    public async Task<ActionResult<ResponseEnvelope<PullRequestDto>>> CreatePullRequest([FromBody] CreatePullRequestRequest request)
    {
        if (!IsCompatEnabled || !_rolloutOptions.EnablePullRequests)
        {
            return DisabledResponse<PullRequestDto>("pull-requests");
        }

        var user = await GetCurrentUserAsync();
        if (user == null)
        {
            return Unauthorized(ResponseMapperUtilities.MapError<PullRequestDto>(StatusCodes.Status401Unauthorized, "Authentication required.", HttpContext.TraceIdentifier));
        }

        if (!await CanReviewAsync(user.Id))
        {
            return StatusCode(StatusCodes.Status403Forbidden, ResponseMapperUtilities.MapError<PullRequestDto>(StatusCodes.Status403Forbidden, "Role is not allowed to create pull requests.", HttpContext.TraceIdentifier));
        }

        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Description))
        {
            return BadRequest(ResponseMapperUtilities.MapError<PullRequestDto>(StatusCodes.Status400BadRequest, "Title and description are required.", HttpContext.TraceIdentifier));
        }

        var pullRequest = new SparkCompatPullRequest
        {
            Id = SparkCompatUtilities.CreateId("pr"),
            ChatId = request.ChatId,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            AuthorUserId = user.Id,
            AuthorName = user.DisplayName,
            Status = "open",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            ApprovalsJson = "[]"
        };

        foreach (var file in request.FileChanges)
        {
            pullRequest.FileChanges.Add(new SparkCompatPullRequestFileChange
            {
                PullRequestId = pullRequest.Id,
                Path = file.Path,
                AdditionsJson = SparkCompatUtilities.ToJson(file.Additions),
                DeletionsJson = SparkCompatUtilities.ToJson(file.Deletions),
                Status = string.IsNullOrWhiteSpace(file.Status) ? "staged" : file.Status
            });
        }

        DbContext.SparkCompatPullRequests.Add(pullRequest);
        await DbContext.SaveChangesAsync();

        var envelope = ResponseMapperUtilities.MapToEnvelope(MapPullRequest(pullRequest), StatusCodes.Status201Created, HttpContext.TraceIdentifier, "Pull request created");
        return StatusCode(StatusCodes.Status201Created, envelope);
    }

    [HttpGet("{prId}")]
    public async Task<ActionResult<ResponseEnvelope<PullRequestDto>>> GetPullRequest(string prId)
    {
        if (!IsCompatEnabled || !_rolloutOptions.EnablePullRequests)
        {
            return DisabledResponse<PullRequestDto>("pull-requests");
        }

        var pullRequest = await BuildPullRequestQuery().FirstOrDefaultAsync(pr => pr.Id == prId);
        if (pullRequest == null)
        {
            return NotFound(ResponseMapperUtilities.MapError<PullRequestDto>(StatusCodes.Status404NotFound, "Pull request not found.", HttpContext.TraceIdentifier));
        }

        return Ok(ResponseMapperUtilities.MapToEnvelope(MapPullRequest(pullRequest), HttpContext.TraceIdentifier));
    }

    [HttpPost("{prId}/approve")]
    public async Task<ActionResult<ResponseEnvelope<PullRequestDto>>> ApprovePullRequest(string prId)
    {
        if (!IsCompatEnabled || !_rolloutOptions.EnablePullRequests)
        {
            return DisabledResponse<PullRequestDto>("pull-requests");
        }

        var user = await GetCurrentUserAsync();
        if (user == null)
        {
            return Unauthorized(ResponseMapperUtilities.MapError<PullRequestDto>(StatusCodes.Status401Unauthorized, "Authentication required.", HttpContext.TraceIdentifier));
        }

        if (!await CanReviewAsync(user.Id))
        {
            return StatusCode(StatusCodes.Status403Forbidden, ResponseMapperUtilities.MapError<PullRequestDto>(StatusCodes.Status403Forbidden, "Role is not allowed to approve pull requests.", HttpContext.TraceIdentifier));
        }

        var pullRequest = await BuildPullRequestQuery(track: true).FirstOrDefaultAsync(pr => pr.Id == prId);
        if (pullRequest == null)
        {
            return NotFound(ResponseMapperUtilities.MapError<PullRequestDto>(StatusCodes.Status404NotFound, "Pull request not found.", HttpContext.TraceIdentifier));
        }

        if (pullRequest.AuthorUserId == user.Id)
        {
            return StatusCode(StatusCodes.Status403Forbidden, ResponseMapperUtilities.MapError<PullRequestDto>(StatusCodes.Status403Forbidden, "Author cannot approve their own pull request.", HttpContext.TraceIdentifier));
        }

        if (pullRequest.Status != "open" && pullRequest.Status != "approved")
        {
            return Conflict(ResponseMapperUtilities.MapError<PullRequestDto>(StatusCodes.Status409Conflict, "Only open pull requests can be approved.", HttpContext.TraceIdentifier));
        }

        var approvals = SparkCompatUtilities.FromJson<List<string>>(pullRequest.ApprovalsJson) ?? new List<string>();
        if (!approvals.Contains(user.Id.ToString(), StringComparer.OrdinalIgnoreCase))
        {
            approvals.Add(user.Id.ToString());
        }

        pullRequest.ApprovalsJson = SparkCompatUtilities.ToJson(approvals);
        pullRequest.Status = approvals.Count > 0 ? "approved" : "open";
        pullRequest.UpdatedAt = DateTime.UtcNow;

        await DbContext.SaveChangesAsync();
        await BroadcastPullRequestEventAsync(pullRequest, "pr_approved", new { prId, status = pullRequest.Status });

        return Ok(ResponseMapperUtilities.MapToEnvelope(MapPullRequest(pullRequest), HttpContext.TraceIdentifier, "Pull request approved"));
    }

    [HttpPost("{prId}/merge")]
    public async Task<ActionResult<ResponseEnvelope<PullRequestDto>>> MergePullRequest(string prId)
    {
        if (!IsCompatEnabled || !_rolloutOptions.EnablePullRequests)
        {
            return DisabledResponse<PullRequestDto>("pull-requests");
        }

        var user = await GetCurrentUserAsync();
        if (user == null)
        {
            return Unauthorized(ResponseMapperUtilities.MapError<PullRequestDto>(StatusCodes.Status401Unauthorized, "Authentication required.", HttpContext.TraceIdentifier));
        }

        if (!await CanReviewAsync(user.Id))
        {
            return StatusCode(StatusCodes.Status403Forbidden, ResponseMapperUtilities.MapError<PullRequestDto>(StatusCodes.Status403Forbidden, "Role is not allowed to merge pull requests.", HttpContext.TraceIdentifier));
        }

        var pullRequest = await BuildPullRequestQuery(track: true).FirstOrDefaultAsync(pr => pr.Id == prId);
        if (pullRequest == null)
        {
            return NotFound(ResponseMapperUtilities.MapError<PullRequestDto>(StatusCodes.Status404NotFound, "Pull request not found.", HttpContext.TraceIdentifier));
        }

        if (pullRequest.Status != "approved")
        {
            return Conflict(ResponseMapperUtilities.MapError<PullRequestDto>(StatusCodes.Status409Conflict, "Pull request must be approved before merge.", HttpContext.TraceIdentifier));
        }

        pullRequest.Status = "merged";
        pullRequest.UpdatedAt = DateTime.UtcNow;
        await DbContext.SaveChangesAsync();

        await BroadcastPullRequestEventAsync(pullRequest, "pr_merged", new { prId, status = "merged" });

        return Ok(ResponseMapperUtilities.MapToEnvelope(MapPullRequest(pullRequest), HttpContext.TraceIdentifier, "Pull request merged"));
    }

    [HttpPost("{prId}/close")]
    public async Task<ActionResult<ResponseEnvelope<PullRequestDto>>> ClosePullRequest(string prId)
    {
        if (!IsCompatEnabled || !_rolloutOptions.EnablePullRequests)
        {
            return DisabledResponse<PullRequestDto>("pull-requests");
        }

        var pullRequest = await BuildPullRequestQuery(track: true).FirstOrDefaultAsync(pr => pr.Id == prId);
        if (pullRequest == null)
        {
            return NotFound(ResponseMapperUtilities.MapError<PullRequestDto>(StatusCodes.Status404NotFound, "Pull request not found.", HttpContext.TraceIdentifier));
        }

        if (pullRequest.Status == "merged" || pullRequest.Status == "closed")
        {
            return Conflict(ResponseMapperUtilities.MapError<PullRequestDto>(StatusCodes.Status409Conflict, "Pull request is already finalized.", HttpContext.TraceIdentifier));
        }

        pullRequest.Status = "closed";
        pullRequest.UpdatedAt = DateTime.UtcNow;
        await DbContext.SaveChangesAsync();

        await BroadcastPullRequestEventAsync(pullRequest, "pr_closed", new { prId, status = "closed" });

        return Ok(ResponseMapperUtilities.MapToEnvelope(MapPullRequest(pullRequest), HttpContext.TraceIdentifier, "Pull request closed"));
    }

    [HttpPost("{prId}/comments")]
    public async Task<ActionResult<ResponseEnvelope<PullRequestDto>>> CommentOnPullRequest(string prId, [FromBody] AddPullRequestCommentRequest request)
    {
        if (!IsCompatEnabled || !_rolloutOptions.EnablePullRequests)
        {
            return DisabledResponse<PullRequestDto>("pull-requests");
        }

        var user = await GetCurrentUserAsync();
        if (user == null)
        {
            return Unauthorized(ResponseMapperUtilities.MapError<PullRequestDto>(StatusCodes.Status401Unauthorized, "Authentication required.", HttpContext.TraceIdentifier));
        }

        if (!await CanReviewAsync(user.Id))
        {
            return StatusCode(StatusCodes.Status403Forbidden, ResponseMapperUtilities.MapError<PullRequestDto>(StatusCodes.Status403Forbidden, "Role is not allowed to comment on pull requests.", HttpContext.TraceIdentifier));
        }

        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return BadRequest(ResponseMapperUtilities.MapError<PullRequestDto>(StatusCodes.Status400BadRequest, "Comment content is required.", HttpContext.TraceIdentifier));
        }

        var pullRequest = await BuildPullRequestQuery(track: true).FirstOrDefaultAsync(pr => pr.Id == prId);
        if (pullRequest == null)
        {
            return NotFound(ResponseMapperUtilities.MapError<PullRequestDto>(StatusCodes.Status404NotFound, "Pull request not found.", HttpContext.TraceIdentifier));
        }

        pullRequest.Comments.Add(new SparkCompatPullRequestComment
        {
            Id = SparkCompatUtilities.CreateId("comment"),
            PullRequestId = prId,
            AuthorUserId = user.Id,
            AuthorName = user.DisplayName,
            Content = request.Content.Trim(),
            Timestamp = DateTime.UtcNow
        });
        pullRequest.UpdatedAt = DateTime.UtcNow;

        await DbContext.SaveChangesAsync();
        await BroadcastPullRequestEventAsync(pullRequest, "pr_commented", new { prId });

        return Ok(ResponseMapperUtilities.MapToEnvelope(MapPullRequest(pullRequest), HttpContext.TraceIdentifier, "Comment added"));
    }

    [HttpPost("{prId}/files/{fileId}/comments")]
    public async Task<ActionResult<ResponseEnvelope<LineCommentDto>>> AddLineComment(
        string prId,
        string fileId,
        [FromBody] AddLineCommentRequest request)
    {
        if (!IsCompatEnabled || !_rolloutOptions.EnablePullRequests)
        {
            return DisabledResponse<LineCommentDto>("pull-requests");
        }

        var user = await GetCurrentUserAsync();
        if (user == null)
        {
            return Unauthorized(ResponseMapperUtilities.MapError<LineCommentDto>(StatusCodes.Status401Unauthorized, "Authentication required.", HttpContext.TraceIdentifier));
        }

        if (string.IsNullOrWhiteSpace(request.Content) || request.LineNumber < 1)
        {
            return BadRequest(ResponseMapperUtilities.MapError<LineCommentDto>(StatusCodes.Status400BadRequest, "Valid line number and content are required.", HttpContext.TraceIdentifier));
        }

        var pullRequest = await BuildPullRequestQuery(track: true).FirstOrDefaultAsync(pr => pr.Id == prId);
        if (pullRequest == null)
        {
            return NotFound(ResponseMapperUtilities.MapError<LineCommentDto>(StatusCodes.Status404NotFound, "Pull request not found.", HttpContext.TraceIdentifier));
        }

        var lineComment = new SparkCompatLineComment
        {
            Id = SparkCompatUtilities.CreateId("line-comment"),
            PullRequestId = prId,
            FileId = fileId,
            ParentId = request.ParentId,
            LineNumber = request.LineNumber,
            LineType = string.IsNullOrWhiteSpace(request.LineType) ? "unchanged" : request.LineType,
            AuthorUserId = user.Id,
            AuthorName = user.DisplayName,
            AuthorAvatar = AvatarFor(user.DisplayName),
            Content = request.Content.Trim(),
            Timestamp = DateTime.UtcNow,
            Resolved = false
        };

        pullRequest.LineComments.Add(lineComment);
        pullRequest.UpdatedAt = DateTime.UtcNow;
        await DbContext.SaveChangesAsync();
        await BroadcastPullRequestEventAsync(pullRequest, "line_comment_added", new { prId, fileId, commentId = lineComment.Id });

        return StatusCode(StatusCodes.Status201Created, ResponseMapperUtilities.MapToEnvelope(MapLineComment(lineComment), StatusCodes.Status201Created, HttpContext.TraceIdentifier, "Line comment added"));
    }

    [HttpPost("{prId}/line-comments/{commentId}/resolve")]
    public async Task<ActionResult<ResponseEnvelope<LineCommentDto>>> ResolveLineComment(string prId, string commentId)
    {
        if (!IsCompatEnabled || !_rolloutOptions.EnablePullRequests)
        {
            return DisabledResponse<LineCommentDto>("pull-requests");
        }

        var pullRequest = await BuildPullRequestQuery(track: true).FirstOrDefaultAsync(pr => pr.Id == prId);
        if (pullRequest == null)
        {
            return NotFound(ResponseMapperUtilities.MapError<LineCommentDto>(StatusCodes.Status404NotFound, "Pull request not found.", HttpContext.TraceIdentifier));
        }

        var lineComment = pullRequest.LineComments.FirstOrDefault(comment => comment.Id == commentId);
        if (lineComment == null)
        {
            return NotFound(ResponseMapperUtilities.MapError<LineCommentDto>(StatusCodes.Status404NotFound, "Line comment not found.", HttpContext.TraceIdentifier));
        }

        lineComment.Resolved = true;
        pullRequest.UpdatedAt = DateTime.UtcNow;
        await DbContext.SaveChangesAsync();
        await BroadcastPullRequestEventAsync(pullRequest, "line_comment_resolved", new { prId, commentId });

        return Ok(ResponseMapperUtilities.MapToEnvelope(MapLineComment(lineComment), HttpContext.TraceIdentifier, "Line comment resolved"));
    }

    [HttpPost("{prId}/line-comments/{commentId}/reactions/toggle")]
    public async Task<ActionResult<ResponseEnvelope<LineCommentDto>>> ToggleLineCommentReaction(
        string prId,
        string commentId,
        [FromBody] ToggleReactionRequest request)
    {
        if (!IsCompatEnabled || !_rolloutOptions.EnablePullRequests)
        {
            return DisabledResponse<LineCommentDto>("pull-requests");
        }

        var user = await GetCurrentUserAsync();
        if (user == null)
        {
            return Unauthorized(ResponseMapperUtilities.MapError<LineCommentDto>(StatusCodes.Status401Unauthorized, "Authentication required.", HttpContext.TraceIdentifier));
        }

        if (string.IsNullOrWhiteSpace(request.Emoji))
        {
            return BadRequest(ResponseMapperUtilities.MapError<LineCommentDto>(StatusCodes.Status400BadRequest, "Emoji is required.", HttpContext.TraceIdentifier));
        }

        var pullRequest = await BuildPullRequestQuery(track: true).FirstOrDefaultAsync(pr => pr.Id == prId);
        if (pullRequest == null)
        {
            return NotFound(ResponseMapperUtilities.MapError<LineCommentDto>(StatusCodes.Status404NotFound, "Pull request not found.", HttpContext.TraceIdentifier));
        }

        var lineComment = pullRequest.LineComments.FirstOrDefault(comment => comment.Id == commentId);
        if (lineComment == null)
        {
            return NotFound(ResponseMapperUtilities.MapError<LineCommentDto>(StatusCodes.Status404NotFound, "Line comment not found.", HttpContext.TraceIdentifier));
        }

        var existingReaction = lineComment.Reactions
            .FirstOrDefault(reaction => reaction.UserId == user.Id && reaction.Emoji == request.Emoji);

        if (existingReaction == null)
        {
            lineComment.Reactions.Add(new SparkCompatLineCommentReaction
            {
                LineCommentId = lineComment.Id,
                Emoji = request.Emoji,
                UserId = user.Id,
                UserName = user.DisplayName
            });
        }
        else
        {
            lineComment.Reactions.Remove(existingReaction);
        }

        pullRequest.UpdatedAt = DateTime.UtcNow;
        await DbContext.SaveChangesAsync();
        await BroadcastPullRequestEventAsync(pullRequest, "line_comment_reaction_toggled", new { prId, commentId, emoji = request.Emoji });

        return Ok(ResponseMapperUtilities.MapToEnvelope(MapLineComment(lineComment), HttpContext.TraceIdentifier, "Reaction toggled"));
    }

    private IQueryable<SparkCompatPullRequest> BuildPullRequestQuery(bool track = false)
    {
        var query = track
            ? DbContext.SparkCompatPullRequests
            : DbContext.SparkCompatPullRequests.AsNoTracking();

        return query
            .Include(pr => pr.FileChanges)
            .Include(pr => pr.Comments)
            .Include(pr => pr.LineComments)
            .ThenInclude(line => line.Reactions);
    }

    private async Task<bool> CanReviewAsync(Guid userId)
    {
        var roles = await _roleService.GetUserRolesAsync(userId);
        return roles.Contains(Role.Admin) || roles.Contains(Role.Participant);
    }

    private async Task BroadcastPullRequestEventAsync(SparkCompatPullRequest pullRequest, string eventType, object metadata)
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
            ChatId = pullRequest.ChatId,
            PrId = pullRequest.Id,
            Timestamp = DateTime.UtcNow,
            MetadataJson = SparkCompatUtilities.ToJson(metadata)
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

    private static PullRequestDto MapPullRequest(SparkCompatPullRequest pullRequest)
    {
        var approvals = SparkCompatUtilities.FromJson<List<string>>(pullRequest.ApprovalsJson) ?? new List<string>();

        return new PullRequestDto
        {
            Id = pullRequest.Id,
            Title = pullRequest.Title,
            Description = pullRequest.Description,
            ChatId = pullRequest.ChatId,
            Author = pullRequest.AuthorName,
            Status = pullRequest.Status,
            CreatedAt = SparkCompatUtilities.ToUnixMilliseconds(pullRequest.CreatedAt),
            UpdatedAt = SparkCompatUtilities.ToUnixMilliseconds(pullRequest.UpdatedAt),
            FileChanges = pullRequest.FileChanges.Select(change => new FileChangeDto
            {
                Path = change.Path,
                Additions = SparkCompatUtilities.FromJson<List<string>>(change.AdditionsJson) ?? new List<string>(),
                Deletions = SparkCompatUtilities.FromJson<List<string>>(change.DeletionsJson) ?? new List<string>(),
                Status = change.Status
            }).ToList(),
            Comments = pullRequest.Comments.Select(comment => new PullRequestCommentDto
            {
                Id = comment.Id,
                PrId = comment.PullRequestId,
                Author = comment.AuthorName,
                Content = comment.Content,
                Timestamp = SparkCompatUtilities.ToUnixMilliseconds(comment.Timestamp)
            }).ToList(),
            Approvals = approvals,
            LineComments = pullRequest.LineComments.Select(MapLineComment).ToList()
        };
    }

    private static LineCommentDto MapLineComment(SparkCompatLineComment lineComment)
    {
        var groupedReactions = lineComment.Reactions
            .GroupBy(reaction => reaction.Emoji)
            .Select(group => new EmojiReactionDto
            {
                Emoji = group.Key,
                UserIds = group.Select(reaction => reaction.UserId.ToString()).Distinct().ToList(),
                UserNames = group.Select(reaction => reaction.UserName).Distinct().ToList()
            })
            .ToList();

        return new LineCommentDto
        {
            Id = lineComment.Id,
            FileId = lineComment.FileId,
            ParentId = lineComment.ParentId,
            LineNumber = lineComment.LineNumber,
            LineType = lineComment.LineType,
            Author = lineComment.AuthorName,
            AuthorAvatar = lineComment.AuthorAvatar,
            Content = lineComment.Content,
            Timestamp = SparkCompatUtilities.ToUnixMilliseconds(lineComment.Timestamp),
            Resolved = lineComment.Resolved,
            Reactions = groupedReactions
        };
    }

    public sealed class CreatePullRequestRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? ChatId { get; set; }
        public List<FileChangeDto> FileChanges { get; set; } = new();
    }

    public sealed class AddPullRequestCommentRequest
    {
        public string Content { get; set; } = string.Empty;
    }

    public sealed class AddLineCommentRequest
    {
        public int LineNumber { get; set; }
        public string LineType { get; set; } = "unchanged";
        public string Content { get; set; } = string.Empty;
        public string? ParentId { get; set; }
    }

    public sealed class ToggleReactionRequest
    {
        public string Emoji { get; set; } = string.Empty;
    }

    public sealed class PullRequestListDto
    {
        public List<PullRequestDto> PullRequests { get; set; } = new();
        public int Total { get; set; }
        public int Limit { get; set; }
        public int Offset { get; set; }
    }

    public sealed class PullRequestDto
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? ChatId { get; set; }
        public string Author { get; set; } = string.Empty;
        public string Status { get; set; } = "open";
        public long CreatedAt { get; set; }
        public long UpdatedAt { get; set; }
        public List<FileChangeDto> FileChanges { get; set; } = new();
        public List<PullRequestCommentDto> Comments { get; set; } = new();
        public List<string> Approvals { get; set; } = new();
        public List<LineCommentDto> LineComments { get; set; } = new();
    }

    public sealed class PullRequestCommentDto
    {
        public string Id { get; set; } = string.Empty;
        public string PrId { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public long Timestamp { get; set; }
    }

    public sealed class FileChangeDto
    {
        public string Path { get; set; } = string.Empty;
        public List<string> Additions { get; set; } = new();
        public List<string> Deletions { get; set; } = new();
        public string Status { get; set; } = "staged";
    }

    public sealed class LineCommentDto
    {
        public string Id { get; set; } = string.Empty;
        public string FileId { get; set; } = string.Empty;
        public string? ParentId { get; set; }
        public int LineNumber { get; set; }
        public string LineType { get; set; } = "unchanged";
        public string Author { get; set; } = string.Empty;
        public string AuthorAvatar { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public long Timestamp { get; set; }
        public bool Resolved { get; set; }
        public List<EmojiReactionDto> Reactions { get; set; } = new();
    }

    public sealed class EmojiReactionDto
    {
        public string Emoji { get; set; } = string.Empty;
        public List<string> UserIds { get; set; } = new();
        public List<string> UserNames { get; set; } = new();
    }
}
