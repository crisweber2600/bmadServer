using bmadServer.ApiService.Data;
using bmadServer.ApiService.Models;
using Microsoft.EntityFrameworkCore;

namespace bmadServer.ApiService.Services;

/// <summary>
/// Service for managing chat history and pagination.
/// Supports loading last 50 messages initially and pagination for older messages.
/// </summary>
public class ChatHistoryService : IChatHistoryService
{
    private readonly ApplicationDbContext _context;

    public ChatHistoryService(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets paginated chat history for a session.
    /// Returns messages in reverse chronological order (newest first).
    /// </summary>
    public async Task<ChatHistoryResponse> GetChatHistoryAsync(
        Guid userId, 
        Guid sessionId, 
        int pageSize = 50, 
        int offset = 0)
    {
        // Get session and verify ownership
        var session = await _context.Sessions
            .Where(s => s.Id == sessionId)
            .FirstOrDefaultAsync();

        if (session == null)
        {
            throw new InvalidOperationException($"Session {sessionId} not found");
        }

        if (session.UserId != userId)
        {
            throw new UnauthorizedAccessException(
                $"User {userId} is not authorized to access session {sessionId}");
        }

        // Get conversation history from WorkflowState
        var messages = session.WorkflowState?.ConversationHistory ?? new List<ChatMessage>();
        
        // Sort by timestamp descending (newest first)
        var sortedMessages = messages
            .OrderByDescending(m => m.Timestamp)
            .ToList();

        var totalCount = sortedMessages.Count;
        
        // Apply pagination
        var paginatedMessages = sortedMessages
            .Skip(offset)
            .Take(pageSize)
            .ToList();

        var hasMore = offset + pageSize < totalCount;

        return new ChatHistoryResponse
        {
            Messages = paginatedMessages,
            TotalCount = totalCount,
            HasMore = hasMore,
            Offset = offset,
            PageSize = pageSize
        };
    }
}
