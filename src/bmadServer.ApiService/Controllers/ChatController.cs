using bmadServer.ApiService.Models;
using bmadServer.ApiService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace bmadServer.ApiService.Controllers;

/// <summary>
/// API endpoints for chat message history and management.
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly ISessionService _sessionService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(ISessionService sessionService, ILogger<ChatController> logger)
    {
        _sessionService = sessionService;
        _logger = logger;
    }

    /// <summary>
    /// Gets paginated chat history for the active session.
    /// Loads 50 messages per page by default.
    /// </summary>
    /// <param name="page">Page number (1-indexed)</param>
    /// <param name="pageSize">Number of messages per page (default: 50, max: 100)</param>
    /// <returns>Paginated chat messages</returns>
    [HttpGet("history")]
    public async Task<ActionResult<ChatHistoryResponse>> GetChatHistory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        if (page < 1)
        {
            return BadRequest("Page number must be at least 1");
        }

        if (pageSize < 1 || pageSize > 100)
        {
            return BadRequest("Page size must be between 1 and 100");
        }

        var userId = GetUserIdFromClaims();

        try
        {
            var session = await _sessionService.GetMostRecentActiveSessionAsync(userId);

            if (session?.WorkflowState == null)
            {
                return Ok(new ChatHistoryResponse
                {
                    Messages = new List<ChatMessage>(),
                    TotalCount = 0,
                    Page = page,
                    PageSize = pageSize,
                    HasMore = false
                });
            }

            var allMessages = session.WorkflowState.ConversationHistory
                .OrderByDescending(m => m.Timestamp)
                .ToList();

            var totalCount = allMessages.Count;
            var skip = (page - 1) * pageSize;
            var messages = allMessages
                .Skip(skip)
                .Take(pageSize)
                .OrderBy(m => m.Timestamp) // Return in chronological order
                .ToList();

            return Ok(new ChatHistoryResponse
            {
                Messages = messages,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                HasMore = skip + messages.Count < totalCount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving chat history for user {UserId}", userId);
            return Problem(
                detail: "An error occurred while retrieving chat history",
                statusCode: 500,
                title: "Chat History Error");
        }
    }

    /// <summary>
    /// Gets the last N messages from chat history.
    /// Used for initial chat load.
    /// </summary>
    /// <param name="count">Number of recent messages to retrieve (default: 50)</param>
    /// <returns>Most recent chat messages</returns>
    [HttpGet("recent")]
    public async Task<ActionResult<List<ChatMessage>>> GetRecentMessages([FromQuery] int count = 50)
    {
        var userId = GetUserIdFromClaims();

        if (count < 1 || count > 100)
        {
            return BadRequest("Count must be between 1 and 100");
        }

        try
        {
            var session = await _sessionService.GetMostRecentActiveSessionAsync(userId);

            if (session?.WorkflowState == null)
            {
                return Ok(new List<ChatMessage>());
            }

            var recentMessages = session.WorkflowState.ConversationHistory
                .OrderByDescending(m => m.Timestamp)
                .Take(count)
                .OrderBy(m => m.Timestamp)
                .ToList();

            return Ok(recentMessages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recent messages for user {UserId}", userId);
            return Problem(
                detail: "An error occurred while retrieving recent messages",
                statusCode: 500,
                title: "Recent Messages Error");
        }
    }

    private Guid GetUserIdFromClaims()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("User ID not found in claims");
        }

        return userId;
    }
}

/// <summary>
/// Response model for paginated chat history.
/// </summary>
public class ChatHistoryResponse
{
    public List<ChatMessage> Messages { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public bool HasMore { get; set; }
}
