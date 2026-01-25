# Story 10.4: Conversation Stall Recovery

**Status:** ready-for-dev

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a user (Marcus), I want the system to detect and recover from conversation stalls, so that I'm not stuck waiting.

## Acceptance Criteria

**Given** I send a message  
**When** no response is received within 30 seconds  
**Then** I see: "This is taking longer than expected..."  
**And** options appear: "Keep Waiting", "Retry", "Cancel"

**Given** an agent appears stuck  
**When** 60 seconds pass with no progress  
**Then** the system auto-retries with the same input  
**And** logs indicate "stall detected, auto-retry initiated"

**Given** the conversation is off-track  
**When** the system detects circular or nonsensical responses  
**Then** I see: "The conversation seems off track. Would you like to rephrase or restart this step?"

**Given** I choose to restart a step  
**When** I confirm the restart  
**Then** the step context is cleared  
**And** the agent receives fresh context  
**And** I can provide new input

**Given** stalls are monitored  
**When** stall rate exceeds threshold (> 5% of conversations)  
**Then** alerts are sent to operators  
**And** investigation can begin

## Tasks / Subtasks

- [ ] Task 1: Response Timeout Detection with User Options (AC: 1)
  - [ ] Implement frontend timeout detection (30-second threshold)
  - [ ] Create timeout UI component with action buttons
  - [ ] Add "Keep Waiting" action (extends timeout)
  - [ ] Add "Retry" action (resends last message)
  - [ ] Add "Cancel" action (cancels current operation)
  - [ ] Track timeout events in analytics
  - [ ] Test timeout detection with various network conditions
- [ ] Task 2: Backend Agent Stall Detection with Auto-Retry (AC: 2)
  - [ ] Implement agent activity heartbeat mechanism
  - [ ] Add 60-second stall detection on server side
  - [ ] Create automatic retry logic for stalled agents
  - [ ] Add structured logging for stall events with correlation IDs
  - [ ] Implement exponential backoff for auto-retries (2s, 5s, 10s)
  - [ ] Add stall metrics to Prometheus
  - [ ] Test with simulated agent hangs
- [ ] Task 3: Circular Response Detection (AC: 3)
  - [ ] Implement conversation history analysis
  - [ ] Create similarity detection algorithm for repeated responses
  - [ ] Add circular conversation detection heuristics
  - [ ] Create UI prompt for off-track conversations
  - [ ] Add "Rephrase" and "Restart Step" options
  - [ ] Track circular conversation patterns in metrics
  - [ ] Test with mock circular conversations
- [ ] Task 4: Step Context Reset and Restart (AC: 4)
  - [ ] Implement step context clearing logic
  - [ ] Create fresh context generation for agent
  - [ ] Add confirmation UI for step restart
  - [ ] Update workflow state after restart
  - [ ] Send fresh context to agent via SignalR
  - [ ] Allow new user input after restart
  - [ ] Test context reset doesn't affect other steps
- [ ] Task 5: Stall Monitoring and Alerting (AC: 5)
  - [ ] Implement stall rate calculation (5% threshold)
  - [ ] Add Prometheus metrics for stall tracking
  - [ ] Create Grafana alert for high stall rates
  - [ ] Implement operator notification system
  - [ ] Add stall investigation dashboard
  - [ ] Log detailed stall patterns for analysis
  - [ ] Test alert triggering at various thresholds
- [ ] Task 6: Testing and Validation
  - [ ] Unit tests for timeout detection logic
  - [ ] Unit tests for stall detection algorithm
  - [ ] Unit tests for circular conversation detection
  - [ ] Integration tests for auto-retry flow
  - [ ] Integration tests for step restart
  - [ ] BDD tests for all acceptance criteria
  - [ ] Manual testing with real agent interactions
  - [ ] Performance testing with high conversation load

## Dev Notes

### 🎯 CRITICAL IMPLEMENTATION REQUIREMENTS

#### Epic 10 Context: Error Handling & Recovery - Story 4 of 5

This story builds on the comprehensive error handling and recovery infrastructure from previous stories:
- **Story 10.1 (Graceful Error Handling):** ProblemDetails RFC 7807, structured logging, correlation IDs, error metrics
- **Story 10.2 (Connection Recovery):** SignalR reconnection, exponential backoff, message queuing, session restoration
- **Story 10.3 (Workflow Recovery):** Checkpoint restoration, automatic retry with Polly, failure state management

**Key Dependencies & Integration Points:**
- Leverage SignalR infrastructure from Story 10.2 for real-time stall notifications
- Use retry patterns and exponential backoff from Stories 10.2 and 10.3
- Integrate with error metrics and alerting from Story 10.1
- Build on workflow execution patterns from Epic 4 (Stories 4.1-4.7)

### 🏗️ Architecture Context

**Existing Infrastructure (MUST USE):**

**1. SignalR Chat Hub (from Epic 3, Story 3.1):**
```csharp
// src/bmadServer.ApiService/Hubs/ChatHub.cs
// Already handles real-time message streaming and session recovery
public class ChatHub : Hub
{
    // Existing SendMessage handler
    public async Task SendMessage(string message)
    
    // Existing OnConnectedAsync with session recovery
    public override async Task OnConnectedAsync()
}
```

**2. Message Streaming (from Epic 3, Story 3.4):**
```csharp
// Frontend already implements streaming message display
// src/frontend/src/hooks/useStreamingMessage.ts
// This provides the foundation for progress detection
```

**3. Workflow Execution Service (from Epic 4):**
```csharp
// src/bmadServer.ApiService/Services/WorkflowExecutionService.cs
// Handles step execution and agent routing
public async Task<WorkflowStepResult> ExecuteStepAsync(
    Guid workflowId, 
    string stepDefinitionId, 
    Dictionary<string, object> inputs)
```

**4. Error Handling Patterns (from Story 10.1):**
- ProblemDetails RFC 7807 format
- Correlation IDs for request tracking
- Structured logging with OpenTelemetry
- Prometheus metrics endpoint at `/metrics`

**5. Retry Infrastructure (from Stories 10.2 & 10.3):**
- Polly resilience policies already configured
- Exponential backoff: 2s, 5s, 10s pattern
- Automatic retry with logging

### 📋 Implementation Checklist

#### 1. Frontend Timeout Detection and UI

**File:** `src/frontend/src/components/MessageTimeout.tsx` (NEW)

**Requirements:**
- Detect when no response received within 30 seconds
- Display user-friendly message and action buttons
- Handle "Keep Waiting", "Retry", and "Cancel" actions
- Track timeout events for analytics

**Implementation Pattern:**
```typescript
import React, { useState, useEffect } from 'react';
import { Alert, Button, Space } from 'antd';
import { ClockCircleOutlined } from '@ant-design/icons';

interface MessageTimeoutProps {
  onKeepWaiting: () => void;
  onRetry: () => void;
  onCancel: () => void;
  visible: boolean;
}

export const MessageTimeout: React.FC<MessageTimeoutProps> = ({
  onKeepWaiting,
  onRetry,
  onCancel,
  visible,
}) => {
  if (!visible) return null;

  return (
    <Alert
      message={
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <span>
            <ClockCircleOutlined style={{ marginRight: 8 }} />
            This is taking longer than expected...
          </span>
          <Space>
            <Button size="small" onClick={onKeepWaiting}>
              Keep Waiting
            </Button>
            <Button size="small" type="primary" onClick={onRetry}>
              Retry
            </Button>
            <Button size="small" danger onClick={onCancel}>
              Cancel
            </Button>
          </Space>
        </div>
      }
      type="warning"
      banner
      closable={false}
    />
  );
};
```

**File:** `src/frontend/src/hooks/useMessageTimeout.ts` (NEW)

**Pattern:**
```typescript
import { useState, useEffect, useCallback, useRef } from 'react';

interface UseMessageTimeoutOptions {
  timeoutMs?: number; // Default: 30000 (30 seconds)
  onTimeout?: () => void;
}

export const useMessageTimeout = (options: UseMessageTimeoutOptions = {}) => {
  const { timeoutMs = 30000, onTimeout } = options;
  const [isTimedOut, setIsTimedOut] = useState(false);
  const [isWaiting, setIsWaiting] = useState(false);
  const timerRef = useRef<NodeJS.Timeout | null>(null);
  const startTimeRef = useRef<number>(0);

  const startTimer = useCallback(() => {
    setIsWaiting(true);
    setIsTimedOut(false);
    startTimeRef.current = Date.now();
    
    timerRef.current = setTimeout(() => {
      setIsTimedOut(true);
      setIsWaiting(false);
      onTimeout?.();
      
      // Track timeout event
      logTimeoutEvent({
        duration: Date.now() - startTimeRef.current,
        timestamp: new Date().toISOString(),
      });
    }, timeoutMs);
  }, [timeoutMs, onTimeout]);

  const resetTimer = useCallback(() => {
    if (timerRef.current) {
      clearTimeout(timerRef.current);
      timerRef.current = null;
    }
    setIsTimedOut(false);
    setIsWaiting(false);
  }, []);

  const extendTimer = useCallback((additionalMs: number = timeoutMs) => {
    // Keep waiting - extend the timeout
    if (timerRef.current) {
      clearTimeout(timerRef.current);
    }
    
    timerRef.current = setTimeout(() => {
      setIsTimedOut(true);
      setIsWaiting(false);
      onTimeout?.();
    }, additionalMs);
    
    setIsTimedOut(false);
  }, [timeoutMs, onTimeout]);

  useEffect(() => {
    return () => {
      if (timerRef.current) {
        clearTimeout(timerRef.current);
      }
    };
  }, []);

  return {
    isTimedOut,
    isWaiting,
    startTimer,
    resetTimer,
    extendTimer,
  };
};

function logTimeoutEvent(data: { duration: number; timestamp: string }) {
  // Send to analytics or logging service
  console.log('[Timeout Event]', data);
  // TODO: Send to backend for metrics tracking
}
```

**Integration with ResponsiveChat:**
```typescript
// src/frontend/src/components/ResponsiveChat.tsx (modify)
import { MessageTimeout } from './MessageTimeout';
import { useMessageTimeout } from '../hooks/useMessageTimeout';

export const ResponsiveChat: React.FC = () => {
  const { 
    isTimedOut, 
    isWaiting, 
    startTimer, 
    resetTimer, 
    extendTimer 
  } = useMessageTimeout({
    timeoutMs: 30000,
    onTimeout: () => {
      // Notify backend of timeout
      signalRService.notifyTimeout();
    },
  });

  const handleSendMessage = async (message: string) => {
    startTimer(); // Start timeout detection
    try {
      await signalRService.sendMessage(message);
    } catch (error) {
      resetTimer();
      // Handle error
    }
  };

  const handleMessageReceived = () => {
    resetTimer(); // Reset timer when response received
  };

  const handleKeepWaiting = () => {
    extendTimer(30000); // Extend by another 30 seconds
  };

  const handleRetry = async () => {
    resetTimer();
    // Resend last message
    await handleSendMessage(lastMessage);
  };

  const handleCancel = () => {
    resetTimer();
    // Cancel current operation
    signalRService.cancelCurrentOperation();
  };

  return (
    <div>
      <MessageTimeout
        visible={isTimedOut}
        onKeepWaiting={handleKeepWaiting}
        onRetry={handleRetry}
        onCancel={handleCancel}
      />
      {/* Rest of chat UI */}
    </div>
  );
};
```

#### 2. Backend Agent Stall Detection with Auto-Retry

**File:** `src/bmadServer.ApiService/Services/ConversationStallDetectionService.cs` (NEW)

**Requirements:**
- Monitor agent activity heartbeat
- Detect 60-second stalls
- Implement automatic retry logic
- Log stall events with correlation IDs
- Track stall metrics

**Implementation Pattern:**
```csharp
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;

namespace bmadServer.ApiService.Services;

public class ConversationStallDetectionService
{
    private readonly ILogger<ConversationStallDetectionService> _logger;
    private readonly IWorkflowExecutionService _workflowExecutionService;
    private readonly Dictionary<Guid, AgentActivityTracker> _activityTrackers = new();
    private static readonly Meter Meter = new("bmadServer.Conversations", "1.0.0");
    private static readonly Counter<int> StallCounter = Meter.CreateCounter<int>(
        "conversation_stalls_total",
        description: "Total number of conversation stalls detected");
    private static readonly Histogram<double> StallDuration = Meter.CreateHistogram<double>(
        "conversation_stall_duration_seconds",
        description: "Duration of conversation stalls in seconds");

    private const int StallThresholdSeconds = 60;
    private const int MaxAutoRetries = 3;

    public ConversationStallDetectionService(
        ILogger<ConversationStallDetectionService> logger,
        IWorkflowExecutionService workflowExecutionService)
    {
        _logger = logger;
        _workflowExecutionService = workflowExecutionService;
    }

    public void StartTracking(Guid conversationId, Guid workflowId, string userId)
    {
        var tracker = new AgentActivityTracker
        {
            ConversationId = conversationId,
            WorkflowId = workflowId,
            UserId = userId,
            LastActivity = DateTime.UtcNow,
            IsActive = true
        };

        _activityTrackers[conversationId] = tracker;

        // Start monitoring task
        _ = MonitorConversationAsync(conversationId);
    }

    public void RecordActivity(Guid conversationId)
    {
        if (_activityTrackers.TryGetValue(conversationId, out var tracker))
        {
            tracker.LastActivity = DateTime.UtcNow;
        }
    }

    public void StopTracking(Guid conversationId)
    {
        if (_activityTrackers.TryGetValue(conversationId, out var tracker))
        {
            tracker.IsActive = false;
            _activityTrackers.Remove(conversationId);
        }
    }

    private async Task MonitorConversationAsync(Guid conversationId)
    {
        while (_activityTrackers.TryGetValue(conversationId, out var tracker) && tracker.IsActive)
        {
            await Task.Delay(TimeSpan.FromSeconds(10)); // Check every 10 seconds

            if (!tracker.IsActive) break;

            var timeSinceLastActivity = DateTime.UtcNow - tracker.LastActivity;
            
            if (timeSinceLastActivity.TotalSeconds >= StallThresholdSeconds && !tracker.StallDetected)
            {
                // Stall detected!
                tracker.StallDetected = true;
                await HandleStallAsync(tracker, timeSinceLastActivity);
            }
        }
    }

    private async Task HandleStallAsync(AgentActivityTracker tracker, TimeSpan stallDuration)
    {
        var correlationId = Activity.Current?.Id ?? Guid.NewGuid().ToString();

        _logger.LogWarning(
            "Conversation stall detected. ConversationId: {ConversationId}, WorkflowId: {WorkflowId}, " +
            "Duration: {Duration}s, CorrelationId: {CorrelationId}",
            tracker.ConversationId, 
            tracker.WorkflowId, 
            stallDuration.TotalSeconds,
            correlationId);

        // Record metrics
        StallCounter.Add(1, 
            new KeyValuePair<string, object?>("workflow_id", tracker.WorkflowId.ToString()),
            new KeyValuePair<string, object?>("user_id", tracker.UserId));
        StallDuration.Record(stallDuration.TotalSeconds);

        // Attempt auto-retry
        if (tracker.RetryCount < MaxAutoRetries)
        {
            tracker.RetryCount++;
            
            _logger.LogInformation(
                "Stall detected, auto-retry initiated. ConversationId: {ConversationId}, " +
                "Attempt: {Attempt} of {MaxAttempts}, CorrelationId: {CorrelationId}",
                tracker.ConversationId,
                tracker.RetryCount,
                MaxAutoRetries,
                correlationId);

            try
            {
                // Retry with same input (stored in tracker.LastInput)
                await _workflowExecutionService.RetryCurrentStepAsync(
                    tracker.WorkflowId, 
                    tracker.LastInput);

                // Reset stall flag to monitor retry
                tracker.StallDetected = false;
                tracker.LastActivity = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Auto-retry failed for conversation {ConversationId}. CorrelationId: {CorrelationId}",
                    tracker.ConversationId,
                    correlationId);
            }
        }
        else
        {
            _logger.LogError(
                "Max auto-retries exhausted for conversation {ConversationId}. Manual intervention required.",
                tracker.ConversationId);

            // Notify user via SignalR
            // TODO: Send CONVERSATION_STALLED event
        }
    }

    private class AgentActivityTracker
    {
        public Guid ConversationId { get; set; }
        public Guid WorkflowId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public DateTime LastActivity { get; set; }
        public bool IsActive { get; set; }
        public bool StallDetected { get; set; }
        public int RetryCount { get; set; }
        public Dictionary<string, object>? LastInput { get; set; }
    }
}
```

**Register Service in Program.cs:**
```csharp
// src/bmadServer.ApiService/Program.cs
builder.Services.AddSingleton<ConversationStallDetectionService>();
```

**Integrate with ChatHub:**
```csharp
// src/bmadServer.ApiService/Hubs/ChatHub.cs (modify)
public class ChatHub : Hub
{
    private readonly ConversationStallDetectionService _stallDetectionService;

    public async Task SendMessage(string message)
    {
        var conversationId = GetConversationId();
        var workflowId = GetWorkflowId();
        var userId = GetUserIdFromClaims();

        // Start stall tracking
        _stallDetectionService.StartTracking(conversationId, workflowId, userId);

        try
        {
            // Process message
            var response = await ProcessMessageAsync(message);

            // Record activity (response received)
            _stallDetectionService.RecordActivity(conversationId);

            await Clients.Caller.SendAsync("ReceiveMessage", response);
        }
        finally
        {
            // Stop tracking after completion
            _stallDetectionService.StopTracking(conversationId);
        }
    }

    // Add heartbeat method for long-running operations
    public async Task SendHeartbeat(Guid conversationId)
    {
        _stallDetectionService.RecordActivity(conversationId);
    }
}
```

#### 3. Circular Response Detection

**File:** `src/bmadServer.ApiService/Services/CircularConversationDetectionService.cs` (NEW)

**Requirements:**
- Analyze conversation history for patterns
- Detect repeated or similar responses
- Identify circular conversations
- Provide recommendations for recovery

**Implementation Pattern:**
```csharp
using System.Text.RegularExpressions;

namespace bmadServer.ApiService.Services;

public class CircularConversationDetectionService
{
    private readonly ILogger<CircularConversationDetectionService> _logger;
    private const double SimilarityThreshold = 0.85; // 85% similarity = circular
    private const int MinMessagesForDetection = 4;

    public CircularConversationDetectionService(
        ILogger<CircularConversationDetectionService> logger)
    {
        _logger = logger;
    }

    public CircularConversationResult AnalyzeConversation(List<ConversationMessage> messages)
    {
        if (messages.Count < MinMessagesForDetection)
        {
            return new CircularConversationResult { IsCircular = false };
        }

        // Get last 6 messages for analysis
        var recentMessages = messages.TakeLast(6).ToList();

        // Check for exact repeats
        var exactRepeats = DetectExactRepeats(recentMessages);
        if (exactRepeats.HasRepeats)
        {
            _logger.LogWarning(
                "Exact conversation repeat detected. MessageCount: {Count}, Pattern: {Pattern}",
                exactRepeats.RepeatCount,
                exactRepeats.Pattern);

            return new CircularConversationResult
            {
                IsCircular = true,
                DetectionReason = "Exact message repeats detected",
                SimilarityScore = 1.0,
                RecommendedAction = "Rephrase or restart step"
            };
        }

        // Check for semantic similarity (simplified version)
        var similarityScore = CalculateAverageSimilarity(recentMessages);
        if (similarityScore >= SimilarityThreshold)
        {
            _logger.LogWarning(
                "Circular conversation detected. SimilarityScore: {Score:F2}",
                similarityScore);

            return new CircularConversationResult
            {
                IsCircular = true,
                DetectionReason = "High semantic similarity between recent messages",
                SimilarityScore = similarityScore,
                RecommendedAction = "Try rephrasing your request or restart this step"
            };
        }

        // Check for "stuck" keywords pattern
        var stuckPatterns = DetectStuckPatterns(recentMessages);
        if (stuckPatterns.IsStuck)
        {
            return new CircularConversationResult
            {
                IsCircular = true,
                DetectionReason = "Conversation appears stuck in a loop",
                SimilarityScore = 0.9,
                RecommendedAction = "Restart step with fresh context"
            };
        }

        return new CircularConversationResult { IsCircular = false };
    }

    private ExactRepeatResult DetectExactRepeats(List<ConversationMessage> messages)
    {
        var userMessages = messages.Where(m => m.IsUserMessage).ToList();
        if (userMessages.Count < 2) 
            return new ExactRepeatResult { HasRepeats = false };

        // Check if last message repeats any previous message
        var lastMessage = userMessages.Last().Content.Trim().ToLowerInvariant();
        var previousMessages = userMessages.SkipLast(1)
            .Select(m => m.Content.Trim().ToLowerInvariant())
            .ToList();

        var repeatIndex = previousMessages.IndexOf(lastMessage);
        if (repeatIndex >= 0)
        {
            return new ExactRepeatResult
            {
                HasRepeats = true,
                RepeatCount = previousMessages.Count(m => m == lastMessage) + 1,
                Pattern = lastMessage.Substring(0, Math.Min(50, lastMessage.Length))
            };
        }

        return new ExactRepeatResult { HasRepeats = false };
    }

    private double CalculateAverageSimilarity(List<ConversationMessage> messages)
    {
        // Simplified similarity calculation using Levenshtein distance
        var userMessages = messages
            .Where(m => m.IsUserMessage)
            .Select(m => NormalizeText(m.Content))
            .ToList();

        if (userMessages.Count < 2) return 0.0;

        var similarities = new List<double>();
        for (int i = 0; i < userMessages.Count - 1; i++)
        {
            for (int j = i + 1; j < userMessages.Count; j++)
            {
                var similarity = CalculateLevenshteinSimilarity(
                    userMessages[i], 
                    userMessages[j]);
                similarities.Add(similarity);
            }
        }

        return similarities.Any() ? similarities.Average() : 0.0;
    }

    private double CalculateLevenshteinSimilarity(string s1, string s2)
    {
        if (string.IsNullOrEmpty(s1) && string.IsNullOrEmpty(s2)) 
            return 1.0;
        if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2)) 
            return 0.0;

        var distance = LevenshteinDistance(s1, s2);
        var maxLength = Math.Max(s1.Length, s2.Length);
        return 1.0 - (double)distance / maxLength;
    }

    private int LevenshteinDistance(string s1, string s2)
    {
        var d = new int[s1.Length + 1, s2.Length + 1];

        for (int i = 0; i <= s1.Length; i++)
            d[i, 0] = i;
        for (int j = 0; j <= s2.Length; j++)
            d[0, j] = j;

        for (int i = 1; i <= s1.Length; i++)
        {
            for (int j = 1; j <= s2.Length; j++)
            {
                var cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(Math.Min(
                    d[i - 1, j] + 1,      // deletion
                    d[i, j - 1] + 1),     // insertion
                    d[i - 1, j - 1] + cost); // substitution
            }
        }

        return d[s1.Length, s2.Length];
    }

    private string NormalizeText(string text)
    {
        // Remove punctuation, extra spaces, convert to lowercase
        text = Regex.Replace(text, @"[^\w\s]", "");
        text = Regex.Replace(text, @"\s+", " ");
        return text.Trim().ToLowerInvariant();
    }

    private StuckPatternResult DetectStuckPatterns(List<ConversationMessage> messages)
    {
        // Keywords that indicate stuck conversation
        var stuckKeywords = new[]
        {
            "i don't understand",
            "can you clarify",
            "i'm not sure",
            "please rephrase",
            "could you explain"
        };

        var recentAgentMessages = messages
            .Where(m => !m.IsUserMessage)
            .TakeLast(3)
            .Select(m => m.Content.ToLowerInvariant())
            .ToList();

        var stuckCount = recentAgentMessages.Count(msg => 
            stuckKeywords.Any(keyword => msg.Contains(keyword)));

        return new StuckPatternResult
        {
            IsStuck = stuckCount >= 2,
            StuckKeywordCount = stuckCount
        };
    }

    public class CircularConversationResult
    {
        public bool IsCircular { get; set; }
        public string? DetectionReason { get; set; }
        public double SimilarityScore { get; set; }
        public string? RecommendedAction { get; set; }
    }

    private class ExactRepeatResult
    {
        public bool HasRepeats { get; set; }
        public int RepeatCount { get; set; }
        public string Pattern { get; set; } = string.Empty;
    }

    private class StuckPatternResult
    {
        public bool IsStuck { get; set; }
        public int StuckKeywordCount { get; set; }
    }
}

public class ConversationMessage
{
    public string Content { get; set; } = string.Empty;
    public bool IsUserMessage { get; set; }
    public DateTime Timestamp { get; set; }
}
```

**Integrate with ChatHub:**
```csharp
// src/bmadServer.ApiService/Hubs/ChatHub.cs (modify)
private readonly CircularConversationDetectionService _circularDetectionService;

public async Task SendMessage(string message)
{
    // ... existing code ...

    // Check for circular conversation
    var conversationHistory = await GetConversationHistoryAsync(sessionId);
    var circularResult = _circularDetectionService.AnalyzeConversation(conversationHistory);

    if (circularResult.IsCircular)
    {
        await Clients.Caller.SendAsync("CONVERSATION_OFF_TRACK", new
        {
            Reason = circularResult.DetectionReason,
            SimilarityScore = circularResult.SimilarityScore,
            RecommendedAction = circularResult.RecommendedAction,
            Message = "The conversation seems off track. Would you like to rephrase or restart this step?"
        });
    }

    // ... continue with normal processing ...
}
```

#### 4. Step Context Reset and Restart

**File:** `src/bmadServer.ApiService/Services/WorkflowStepResetService.cs` (NEW)

**Requirements:**
- Clear step context safely
- Generate fresh context for agent
- Update workflow state
- Allow new user input

**Implementation Pattern:**
```csharp
namespace bmadServer.ApiService.Services;

public class WorkflowStepResetService
{
    private readonly ILogger<WorkflowStepResetService> _logger;
    private readonly ApplicationDbContext _dbContext;
    private readonly IHubContext<ChatHub> _hubContext;

    public WorkflowStepResetService(
        ILogger<WorkflowStepResetService> logger,
        ApplicationDbContext dbContext,
        IHubContext<ChatHub> hubContext)
    {
        _logger = logger;
        _dbContext = dbContext;
        _hubContext = hubContext;
    }

    public async Task<StepResetResult> ResetStepAsync(
        Guid workflowId, 
        string currentStepId, 
        string userId)
    {
        var correlationId = Activity.Current?.Id ?? Guid.NewGuid().ToString();

        _logger.LogInformation(
            "Resetting workflow step. WorkflowId: {WorkflowId}, StepId: {StepId}, " +
            "UserId: {UserId}, CorrelationId: {CorrelationId}",
            workflowId, currentStepId, userId, correlationId);

        try
        {
            // Load workflow instance
            var workflow = await _dbContext.WorkflowInstances
                .FirstOrDefaultAsync(w => w.Id == workflowId);

            if (workflow == null)
            {
                return new StepResetResult
                {
                    Success = false,
                    Error = "Workflow not found"
                };
            }

            // Validate ownership
            if (workflow.UserId != userId)
            {
                return new StepResetResult
                {
                    Success = false,
                    Error = "Unauthorized"
                };
            }

            // Clear step-specific context
            var stateJson = JsonSerializer.Deserialize<WorkflowState>(workflow.StateJson);
            if (stateJson != null)
            {
                // Clear current step data
                stateJson.CurrentStepData = null;
                stateJson.CurrentStepContext = new Dictionary<string, object>
                {
                    ["reset_at"] = DateTime.UtcNow,
                    ["reset_by"] = userId,
                    ["correlation_id"] = correlationId
                };

                // Clear conversation history for this step (optional - depends on requirements)
                // stateJson.StepConversationHistory?.Clear();

                workflow.StateJson = JsonSerializer.Serialize(stateJson);
                await _dbContext.SaveChangesAsync();
            }

            // Notify frontend via SignalR
            await _hubContext.Clients.User(userId).SendAsync("STEP_CONTEXT_RESET", new
            {
                WorkflowId = workflowId,
                StepId = currentStepId,
                Message = "Step context has been cleared. You can now provide fresh input.",
                Timestamp = DateTime.UtcNow
            });

            _logger.LogInformation(
                "Step reset completed successfully. WorkflowId: {WorkflowId}, " +
                "CorrelationId: {CorrelationId}",
                workflowId, correlationId);

            return new StepResetResult
            {
                Success = true,
                FreshContext = stateJson?.CurrentStepContext
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to reset workflow step. WorkflowId: {WorkflowId}, " +
                "CorrelationId: {CorrelationId}",
                workflowId, correlationId);

            return new StepResetResult
            {
                Success = false,
                Error = "Failed to reset step context"
            };
        }
    }

    public class StepResetResult
    {
        public bool Success { get; set; }
        public Dictionary<string, object>? FreshContext { get; set; }
        public string? Error { get; set; }
    }

    private class WorkflowState
    {
        public Dictionary<string, object>? CurrentStepData { get; set; }
        public Dictionary<string, object>? CurrentStepContext { get; set; }
        public List<ConversationMessage>? StepConversationHistory { get; set; }
    }
}
```

**Add API Endpoint:**
```csharp
// src/bmadServer.ApiService/Controllers/WorkflowsController.cs (modify)
[HttpPost("{workflowId}/steps/{stepId}/reset")]
[Authorize]
public async Task<ActionResult<StepResetResult>> ResetStep(
    Guid workflowId, 
    string stepId)
{
    var userId = GetUserIdFromClaims();
    var result = await _stepResetService.ResetStepAsync(workflowId, stepId, userId);

    if (!result.Success)
    {
        return BadRequest(new ProblemDetails
        {
            Type = "https://bmadserver.dev/errors/step-reset-failed",
            Title = "Step Reset Failed",
            Status = StatusCodes.Status400BadRequest,
            Detail = result.Error
        });
    }

    return Ok(result);
}
```

**Frontend Integration:**
```typescript
// src/frontend/src/components/CircularConversationPrompt.tsx (NEW)
import React from 'react';
import { Modal, Button } from 'antd';
import { ExclamationCircleOutlined } from '@ant-design/icons';

interface CircularConversationPromptProps {
  visible: boolean;
  reason: string;
  recommendedAction: string;
  onRephrase: () => void;
  onRestart: () => void;
  onDismiss: () => void;
}

export const CircularConversationPrompt: React.FC<CircularConversationPromptProps> = ({
  visible,
  reason,
  recommendedAction,
  onRephrase,
  onRestart,
  onDismiss,
}) => {
  return (
    <Modal
      title={
        <span>
          <ExclamationCircleOutlined style={{ color: '#faad14', marginRight: 8 }} />
          Conversation Off Track
        </span>
      }
      open={visible}
      onCancel={onDismiss}
      footer={[
        <Button key="dismiss" onClick={onDismiss}>
          Continue Anyway
        </Button>,
        <Button key="rephrase" onClick={onRephrase}>
          Rephrase Input
        </Button>,
        <Button key="restart" type="primary" onClick={onRestart}>
          Restart Step
        </Button>,
      ]}
    >
      <p>{reason}</p>
      <p><strong>Recommendation:</strong> {recommendedAction}</p>
    </Modal>
  );
};
```

#### 5. Stall Monitoring and Alerting

**File:** `src/bmadServer.ApiService/Services/StallRateMonitoringService.cs` (NEW)

**Requirements:**
- Calculate stall rate (stalls / total conversations)
- Trigger alerts when threshold exceeded (> 5%)
- Provide investigation data
- Integrate with Grafana alerting

**Implementation Pattern:**
```csharp
using System.Diagnostics.Metrics;

namespace bmadServer.ApiService.Services;

public class StallRateMonitoringService
{
    private readonly ILogger<StallRateMonitoringService> _logger;
    private static readonly Meter Meter = new("bmadServer.StallMonitoring", "1.0.0");
    
    private static readonly Counter<int> TotalConversationsCounter = 
        Meter.CreateCounter<int>("total_conversations");
    
    private static readonly Counter<int> StalledConversationsCounter = 
        Meter.CreateCounter<int>("stalled_conversations");
    
    private static readonly ObservableGauge<double> StallRateGauge = 
        Meter.CreateObservableGauge<double>("stall_rate_percentage", 
            () => CalculateCurrentStallRate());

    private static long _totalConversations = 0;
    private static long _stalledConversations = 0;
    private const double AlertThresholdPercentage = 5.0; // 5%

    public StallRateMonitoringService(ILogger<StallRateMonitoringService> logger)
    {
        _logger = logger;
    }

    public void RecordConversationStart()
    {
        Interlocked.Increment(ref _totalConversations);
        TotalConversationsCounter.Add(1);
    }

    public void RecordConversationStall()
    {
        Interlocked.Increment(ref _stalledConversations);
        StalledConversationsCounter.Add(1);

        // Check if alert threshold exceeded
        var currentRate = CalculateCurrentStallRate();
        if (currentRate > AlertThresholdPercentage)
        {
            TriggerStallAlert(currentRate);
        }
    }

    private static double CalculateCurrentStallRate()
    {
        var total = Interlocked.Read(ref _totalConversations);
        if (total == 0) return 0.0;

        var stalled = Interlocked.Read(ref _stalledConversations);
        return (double)stalled / total * 100.0;
    }

    private void TriggerStallAlert(double currentRate)
    {
        _logger.LogError(
            "⚠️ ALERT: Stall rate threshold exceeded! Current rate: {Rate:F2}%, " +
            "Threshold: {Threshold:F2}%, Total: {Total}, Stalled: {Stalled}",
            currentRate,
            AlertThresholdPercentage,
            _totalConversations,
            _stalledConversations);

        // TODO: Send alert to operator notification system
        // This could be:
        // - Email alert
        // - Slack/Teams webhook
        // - PagerDuty integration
        // - Admin dashboard notification
    }

    public StallRateReport GetStallRateReport()
    {
        var total = Interlocked.Read(ref _totalConversations);
        var stalled = Interlocked.Read(ref _stalledConversations);
        var rate = CalculateCurrentStallRate();

        return new StallRateReport
        {
            TotalConversations = total,
            StalledConversations = stalled,
            StallRatePercentage = rate,
            IsAboveThreshold = rate > AlertThresholdPercentage,
            Threshold = AlertThresholdPercentage,
            ReportedAt = DateTime.UtcNow
        };
    }

    public class StallRateReport
    {
        public long TotalConversations { get; set; }
        public long StalledConversations { get; set; }
        public double StallRatePercentage { get; set; }
        public bool IsAboveThreshold { get; set; }
        public double Threshold { get; set; }
        public DateTime ReportedAt { get; set; }
    }
}
```

**Grafana Alert Configuration:**

Create Grafana alert rule file: `grafana/alerts/stall-rate-alert.yaml`
```yaml
apiVersion: 1
groups:
  - name: conversation_stalls
    interval: 1m
    rules:
      - alert: HighStallRate
        expr: stall_rate_percentage > 5.0
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: "High conversation stall rate detected"
          description: "Stall rate is {{ $value }}% (threshold: 5%)"
      
      - alert: CriticalStallRate
        expr: stall_rate_percentage > 10.0
        for: 2m
        labels:
          severity: critical
        annotations:
          summary: "CRITICAL: Conversation stall rate very high"
          description: "Stall rate is {{ $value }}% (threshold: 10%)"
```

**Prometheus Queries for Dashboard:**
```promql
# Stall Rate (last 5 minutes)
rate(stalled_conversations[5m]) / rate(total_conversations[5m]) * 100

# Total Stalls (last 1 hour)
increase(stalled_conversations[1h])

# Stall Rate Trend (last 24 hours)
rate(stalled_conversations[1h]) / rate(total_conversations[1h]) * 100

# Stalls by Workflow Type
sum by (workflow_type) (conversation_stalls_total)
```

### 🧪 Testing Strategy

#### Unit Tests

**File:** `src/bmadServer.Tests/Services/ConversationStallDetectionServiceTests.cs` (NEW)

Test Cases:
```csharp
[Fact]
public async Task StartTracking_ValidConversation_TracksActivity()
{
    // Arrange
    var service = CreateService();
    var conversationId = Guid.NewGuid();

    // Act
    service.StartTracking(conversationId, Guid.NewGuid(), "user123");

    // Assert
    // Verify tracker created and monitoring started
}

[Fact]
public async Task DetectStall_60SecondsNoActivity_TriggersAutoRetry()
{
    // Arrange
    var service = CreateService();
    var conversationId = Guid.NewGuid();
    service.StartTracking(conversationId, Guid.NewGuid(), "user123");

    // Act
    await Task.Delay(TimeSpan.FromSeconds(65)); // Simulate stall

    // Assert
    // Verify stall detected and auto-retry triggered
    // Verify log contains "stall detected, auto-retry initiated"
}

[Fact]
public void CircularDetection_RepeatedMessages_DetectsCircular()
{
    // Arrange
    var service = new CircularConversationDetectionService(logger);
    var messages = new List<ConversationMessage>
    {
        new() { Content = "Hello", IsUserMessage = true },
        new() { Content = "Response 1", IsUserMessage = false },
        new() { Content = "Hello", IsUserMessage = true },
        new() { Content = "Response 2", IsUserMessage = false },
        new() { Content = "Hello", IsUserMessage = true }
    };

    // Act
    var result = service.AnalyzeConversation(messages);

    // Assert
    Assert.True(result.IsCircular);
    Assert.Contains("repeat", result.DetectionReason.ToLower());
}

[Fact]
public async Task ResetStep_ValidWorkflow_ClearsContextSuccessfully()
{
    // Arrange
    var service = CreateResetService();
    var workflowId = await CreateTestWorkflowAsync();

    // Act
    var result = await service.ResetStepAsync(workflowId, "step-1", "user123");

    // Assert
    Assert.True(result.Success);
    Assert.NotNull(result.FreshContext);
}
```

#### Integration Tests

**File:** `src/bmadServer.ApiService.IntegrationTests/ConversationStallIntegrationTests.cs` (NEW)

Test Cases:
```csharp
[Fact]
public async Task SendMessage_30SecondTimeout_ShowsTimeoutPrompt()
{
    // Test: Frontend timeout detection
    // Verify: Timeout UI appears after 30 seconds
}

[Fact]
public async Task AgentStall_AutoRetrySucceeds_ResumesConversation()
{
    // Test: Auto-retry after 60-second stall
    // Verify: Retry triggered, conversation resumes
}

[Fact]
public async Task CircularConversation_DetectedAndPrompted_UserCanRestart()
{
    // Test: Circular conversation detection
    // Verify: User receives prompt with restart option
}

[Fact]
public async Task POST_StepReset_ValidWorkflow_Returns200()
{
    // Test: Step reset API endpoint
    // Verify: Context cleared, fresh context created
}

[Fact]
public async Task StallRateExceeds5Percent_AlertTriggered()
{
    // Test: Stall monitoring and alerting
    // Verify: Alert triggered when threshold exceeded
}
```

#### BDD Tests (SpecFlow)

**File:** `src/bmadServer.BDD.Tests/Features/ConversationStallRecovery.feature` (NEW)

```gherkin
Feature: Conversation Stall Recovery
  As a user
  I want the system to detect and recover from conversation stalls
  So that I'm not stuck waiting indefinitely

Background:
  Given I am logged in as "Marcus"
  And I have started a workflow conversation

Scenario: Response timeout shows user options
  Given I send a message "Start new project"
  When no response is received within 30 seconds
  Then I should see "This is taking longer than expected..."
  And I should see options "Keep Waiting", "Retry", "Cancel"

Scenario: Agent stall triggers automatic retry
  Given an agent is processing my message
  When 60 seconds pass with no progress
  Then the system should auto-retry with the same input
  And the logs should contain "stall detected, auto-retry initiated"

Scenario: Circular conversation detected
  Given I send message "Show me the dashboard"
  And I receive response "I don't understand"
  And I send message "Show me the dashboard"
  And I receive response "Can you clarify?"
  And I send message "Show me the dashboard"
  When the system detects circular conversation
  Then I should see "The conversation seems off track. Would you like to rephrase or restart this step?"
  And I should see options "Rephrase" and "Restart Step"

Scenario: User restarts step with fresh context
  Given the conversation is off-track
  And I see the off-track prompt
  When I choose to "Restart Step"
  And I confirm the restart
  Then the step context should be cleared
  And I should be able to provide new input
  And the agent should receive fresh context

Scenario: High stall rate triggers operator alert
  Given 100 conversations are active
  When 6 conversations stall (6% stall rate)
  Then an alert should be sent to operators
  And the alert should contain the stall rate percentage
  And operators can investigate the issue
```

### 🔐 Security Considerations

**Authorization:**
- Step reset API: Only workflow owner can reset
- Stall rate reports: Admin-only access
- Validate user ownership before any reset operation

**Data Privacy:**
- Don't log sensitive conversation content
- Redact user input in stall logs
- Correlation IDs for tracing without exposing data

**Rate Limiting:**
- Prevent abuse of retry mechanism (max 3 auto-retries)
- Limit manual retry attempts (e.g., 10 per hour)
- Track reset abuse patterns

### 📊 Metrics and Monitoring

**Key Metrics to Track:**
```
1. conversation_stalls_total (counter)
   - Labels: workflow_type, user_id
   
2. conversation_stall_duration_seconds (histogram)
   - Tracks how long stalls last
   
3. stall_rate_percentage (gauge)
   - Current stall rate (updated every minute)
   
4. timeout_events_total (counter)
   - Labels: action (keep_waiting, retry, cancel)
   
5. circular_conversations_detected_total (counter)
   - Labels: detection_reason
   
6. step_resets_total (counter)
   - Labels: workflow_type, user_id
```

**Grafana Dashboard Panels:**
```
Panel 1: Stall Rate (Line chart)
  Query: stall_rate_percentage
  Alert: Red line at 5%

Panel 2: Stalls by Type (Bar chart)
  Query: sum by (workflow_type) (conversation_stalls_total)

Panel 3: Timeout Actions (Pie chart)
  Query: sum by (action) (timeout_events_total)

Panel 4: Average Stall Duration (Gauge)
  Query: avg(conversation_stall_duration_seconds)

Panel 5: Circular Conversations (Counter)
  Query: increase(circular_conversations_detected_total[24h])
```

### 📂 Files to Create/Modify

**New Files:**
```
Backend:
  src/bmadServer.ApiService/
    Services/
      ConversationStallDetectionService.cs
      CircularConversationDetectionService.cs
      WorkflowStepResetService.cs
      StallRateMonitoringService.cs
    Models/DTOs/
      StallDetectionResult.cs
      CircularConversationResult.cs
      StepResetResult.cs
      StallRateReport.cs

Frontend:
  src/frontend/src/
    components/
      MessageTimeout.tsx
      CircularConversationPrompt.tsx
    hooks/
      useMessageTimeout.ts
    services/
      stallDetectionService.ts

Tests:
  src/bmadServer.Tests/
    Services/
      ConversationStallDetectionServiceTests.cs
      CircularConversationDetectionServiceTests.cs
      WorkflowStepResetServiceTests.cs
      StallRateMonitoringServiceTests.cs
  src/bmadServer.ApiService.IntegrationTests/
    ConversationStallIntegrationTests.cs
  src/bmadServer.BDD.Tests/
    Features/
      ConversationStallRecovery.feature
    StepDefinitions/
      ConversationStallRecoverySteps.cs

Configuration:
  grafana/
    alerts/
      stall-rate-alert.yaml
    dashboards/
      conversation-stall-monitoring.json
```

**Modified Files:**
```
Backend:
  src/bmadServer.ApiService/
    Hubs/
      ChatHub.cs                  # Add stall detection, circular detection
    Controllers/
      WorkflowsController.cs       # Add step reset endpoint
    Program.cs                     # Register new services

Frontend:
  src/frontend/src/
    components/
      ResponsiveChat.tsx          # Integrate timeout and circular detection
```

### 🎓 Learning from Previous Stories

#### Story 10.1 Insights (Graceful Error Handling)

**Apply These Patterns:**
✅ Use ProblemDetails RFC 7807 for API errors
✅ Include correlation IDs in all error logs
✅ Structured logging with OpenTelemetry
✅ Prometheus metrics for tracking
✅ User-friendly error messages (not technical details)

**Example:**
```csharp
return BadRequest(new ProblemDetails
{
    Type = "https://bmadserver.dev/errors/step-reset-failed",
    Title = "Step Reset Failed",
    Status = StatusCodes.Status400BadRequest,
    Detail = "Unable to reset step context",
    Instance = HttpContext.Request.Path
});
```

#### Story 10.2 Insights (Connection Recovery)

**Apply These Patterns:**
✅ Exponential backoff for retries: 2s, 5s, 10s
✅ Max retry attempts: 3-5 (prevent infinite loops)
✅ Queue operations during disconnection
✅ UI indicators for connection state
✅ Local storage for draft preservation

**Example:**
```csharp
// Use same retry pattern
var retryPolicy = Policy
    .Handle<TimeoutException>()
    .WaitAndRetryAsync(3, retryAttempt => 
        TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
```

#### Story 10.3 Insights (Workflow Recovery)

**Apply These Patterns:**
✅ Polly resilience policies for transient failures
✅ Automatic retry with logging
✅ Failed state transition after exhausted retries
✅ Recovery API endpoints with authorization
✅ Background service for startup recovery

**Key Lesson:**
- Don't reinvent the wheel - use existing WorkflowExecutionService patterns
- Leverage checkpoint system for state restoration
- Always log recovery attempts with correlation IDs

### 🔗 Dependencies

**Required NuGet Packages (Backend):**
- `Polly` 8.0+ (already installed from Story 10.2)
- `System.Diagnostics.DiagnosticSource` (OpenTelemetry - already installed)
- All other dependencies already available

**Required NPM Packages (Frontend):**
- `@microsoft/signalr` (already installed from Story 3.1)
- `antd` (already installed)
- `react`, `react-dom` (already installed)

**No New Packages Needed** - Everything required is already available!

**Dependencies on Other Stories:**
- ✅ Story 3.1: SignalR Hub Setup (provides ChatHub infrastructure)
- ✅ Story 3.4: Real-Time Message Streaming (provides streaming infrastructure)
- ✅ Story 10.1: Graceful Error Handling (provides error patterns)
- ✅ Story 10.2: Connection Recovery (provides retry patterns)
- ✅ Story 10.3: Workflow Recovery (provides recovery infrastructure)
- ✅ Epic 4: Workflow Orchestration (provides workflow execution service)

---

## Aspire Development Standards

### Rule 1: Use Aspire Service Defaults

This story leverages Aspire's built-in observability infrastructure:
- `builder.AddServiceDefaults()` provides OpenTelemetry logging and tracing
- Correlation IDs automatically available via `Activity.Current?.Id`
- Structured JSON logging pre-configured
- Prometheus metrics endpoint available at `/metrics`

**Reference:** [PROJECT-WIDE-RULES.md](../../../PROJECT-WIDE-RULES.md) - Rule 4: OpenTelemetry from Day 1

### Rule 2: Documentation Sources

**Primary:** https://aspire.dev/docs/fundamentals/observability/
**Secondary:** https://github.com/microsoft/aspire

### PostgreSQL Connection Pattern

This story uses PostgreSQL configured in Story 1.2 via Aspire:
- Connection string automatically injected from Aspire AppHost
- Pattern: `builder.AddServiceDefaults();` (inherits PostgreSQL reference)
- See Story 1.2 for AppHost configuration pattern

---

## References

- **Epic Context:** [epics.md - Epic 10: Error Handling & Recovery](../planning-artifacts/epics.md#epic-10-error-handling--recovery)
- **Story Source:** [epics.md - Story 10.4](../planning-artifacts/epics.md)
- **Architecture:** [architecture.md](../planning-artifacts/architecture.md)
- **PRD:** [prd.md](../planning-artifacts/prd.md)
- **Previous Stories:**
  - [10.1: Graceful Error Handling](./10-1-graceful-error-handling.md)
  - [10.2: Connection Recovery & Retry](./10-2-connection-recovery-retry.md)
  - [10.3: Workflow Recovery After Failure](./10-3-workflow-recovery-after-failure.md)
- **Foundation Stories:**
  - Epic 3: Real-Time Chat Interface (Stories 3.1-3.6)
  - Epic 4: Workflow Orchestration Engine (Stories 4.1-4.7)
- **Aspire Rules:** [PROJECT-WIDE-RULES.md](../../../PROJECT-WIDE-RULES.md)

---

## Dev Agent Record

### Agent Model Used

_To be filled during implementation_

### Debug Log References

_To be added during implementation_

### Completion Notes List

_To be added during implementation_

### File List

_To be added during implementation

This story should be implemented following the patterns established in the codebase:
- Follow the architecture patterns defined in `architecture.md`
- Use existing service patterns and dependency injection
- Ensure proper error handling and logging
- Add appropriate authorization checks based on user roles
- Follow the coding standards and conventions of the project

### Testing Strategy

- Unit tests should cover business logic and edge cases
- Integration tests should verify API endpoints and database interactions
- Consider performance implications for database queries
- Test error scenarios and validation rules

### Dependencies

Review the acceptance criteria for dependencies on:
- Other stories or epics that must be completed first
- External packages or services that need to be configured
- Database migrations that need to be created

## Files to Create/Modify

Files will be determined during implementation based on:
- Data models and entities needed
- API endpoints required
- Service layer components
- Database migrations
- Test files


---

## Aspire Development Standards

### PostgreSQL Connection Pattern

This story uses PostgreSQL configured in Story 1.2 via Aspire:
- Connection string automatically injected from Aspire AppHost
- Pattern: `builder.AddServiceDefaults();` (inherits PostgreSQL reference)
- See Story 1.2 for AppHost configuration pattern

### Project-Wide Standards

This story follows the Aspire-first development pattern:
- **Reference:** [PROJECT-WIDE-RULES.md](../../../PROJECT-WIDE-RULES.md)
- **Primary Documentation:** https://aspire.dev
- **GitHub:** https://github.com/microsoft/aspire

---


### Future: Redis Caching Pattern

When caching layer needed in Phase 2:
- Command: `aspire add Redis.Distributed`
- Pattern: DI injection via IConnectionMultiplexer
- Also available: Redis backplane for SignalR scaling
- Reference: https://aspire.dev Redis integration

## References
- **Aspire Rules:** [PROJECT-WIDE-RULES.md](../../../PROJECT-WIDE-RULES.md)
- **Aspire Docs:** https://aspire.dev

- Source: [epics.md - Story 10.4](../planning-artifacts/epics.md)
- Architecture: [architecture.md](../planning-artifacts/architecture.md)
- PRD: [prd.md](../planning-artifacts/prd.md)
