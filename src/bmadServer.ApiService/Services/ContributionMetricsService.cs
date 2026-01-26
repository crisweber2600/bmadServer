using System.Text.Json;
using bmadServer.ApiService.Data;
using bmadServer.ApiService.DTOs;
using bmadServer.ApiService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace bmadServer.ApiService.Services;

/// <summary>
/// Service for calculating contribution metrics from workflow history (Story 7.3)
/// </summary>
public class ContributionMetricsService : IContributionMetricsService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IDistributedCache _cache;
    private const int CacheTtlMinutes = 5;

    public ContributionMetricsService(ApplicationDbContext dbContext, IDistributedCache cache)
    {
        _dbContext = dbContext;
        _cache = cache;
    }

    public async Task<ContributionMetricsResponse> GetContributionMetricsAsync(
        Guid workflowId,
        CancellationToken cancellationToken = default)
    {
        // Try cache first
        var cacheKey = $"ContributionMetrics_{workflowId}";
        var cachedData = await _cache.GetStringAsync(cacheKey, cancellationToken);
        
        if (!string.IsNullOrEmpty(cachedData))
        {
            var cached = JsonSerializer.Deserialize<ContributionMetricsResponse>(cachedData);
            if (cached != null)
            {
                return cached;
            }
        }

        // Get all sessions related to this workflow
        var sessions = await _dbContext.Sessions
            .Where(s => s.WorkflowState != null && 
                       s.WorkflowState.ActiveWorkflowInstanceId == workflowId)
            .ToListAsync(cancellationToken);

        // Get all workflow events for this workflow - use database filtering, not in-memory
        var events = await _dbContext.WorkflowEvents
            .Where(e => e.WorkflowInstanceId == workflowId && e.InputType == InputTypes.Decision)
            .ToListAsync(cancellationToken);

        // Calculate metrics per user
        var contributorMetrics = new Dictionary<Guid, UserContribution>();

        // Process chat messages from sessions
        foreach (var session in sessions)
        {
            if (session.WorkflowState?.ConversationHistory == null) continue;

            foreach (var message in session.WorkflowState.ConversationHistory)
            {
                if (!message.UserId.HasValue) continue;

                var userId = message.UserId.Value;
                if (!contributorMetrics.ContainsKey(userId))
                {
                    contributorMetrics[userId] = new UserContribution
                    {
                        UserId = userId,
                        DisplayName = message.DisplayName ?? "Unknown",
                        FirstContribution = message.Timestamp,
                        LastContribution = message.Timestamp
                    };
                }

                var contrib = contributorMetrics[userId];
                contrib.MessagesSent++;
                
                // Update time range
                if (message.Timestamp < contrib.FirstContribution)
                    contrib.FirstContribution = message.Timestamp;
                if (message.Timestamp > contrib.LastContribution)
                    contrib.LastContribution = message.Timestamp;
            }

            // Calculate time spent from session duration
            if (session.WorkflowState?.ConversationHistory?.Any(m => m.UserId.HasValue) == true)
            {
                var userId = session.UserId;
                if (contributorMetrics.ContainsKey(userId))
                {
                    var timeSpent = session.LastActivityAt - session.CreatedAt;
                    contributorMetrics[userId].TimeSpent += timeSpent;
                }
            }
        }

        // Process workflow events (decisions)
        foreach (var evt in events)
        {
            if (!contributorMetrics.ContainsKey(evt.UserId))
            {
                contributorMetrics[evt.UserId] = new UserContribution
                {
                    UserId = evt.UserId,
                    DisplayName = evt.DisplayName ?? "Unknown",
                    FirstContribution = evt.Timestamp,
                    LastContribution = evt.Timestamp
                };
            }

            contributorMetrics[evt.UserId].DecisionsMade++;
            
            // Update time range
            var contrib = contributorMetrics[evt.UserId];
            if (evt.Timestamp < contrib.FirstContribution)
                contrib.FirstContribution = evt.Timestamp;
            if (evt.Timestamp > contrib.LastContribution)
                contrib.LastContribution = evt.Timestamp;
        }

        // Build response
        var contributors = contributorMetrics.Values.ToList();
        var response = new ContributionMetricsResponse
        {
            WorkflowId = workflowId,
            Contributors = contributors,
            Summary = new ContributionSummary
            {
                TotalMessages = contributors.Sum(c => c.MessagesSent),
                TotalDecisions = contributors.Sum(c => c.DecisionsMade),
                TotalContributors = contributors.Count,
                TotalTimeSpent = TimeSpan.FromTicks(contributors.Sum(c => c.TimeSpent.Ticks))
            }
        };

        // Cache for 5 minutes
        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheTtlMinutes)
        };
        await _cache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(response),
            cacheOptions,
            cancellationToken);

        return response;
    }

    public async Task<UserContribution> GetUserContributionAsync(
        Guid workflowId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var metrics = await GetContributionMetricsAsync(workflowId, cancellationToken);
        var userContrib = metrics.Contributors.FirstOrDefault(c => c.UserId == userId);
        
        if (userContrib == null)
        {
            // Return empty contribution if user hasn't contributed
            return new UserContribution
            {
                UserId = userId,
                DisplayName = "Unknown",
                MessagesSent = 0,
                DecisionsMade = 0,
                TimeSpent = TimeSpan.Zero,
                FirstContribution = DateTime.UtcNow,
                LastContribution = DateTime.UtcNow
            };
        }

        return userContrib;
    }
}
