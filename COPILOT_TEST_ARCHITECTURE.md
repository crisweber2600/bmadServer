# Copilot SDK Test Interface - Architecture

## System Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                          Browser / Frontend                                  │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                               │
│  ┌────────────────────────────────────────────────────────────────────────┐  │
│  │ ChatApp Component (main.tsx)                                           │  │
│  │                                                                        │  │
│  │  ┌──────────────────┐                    ┌─────────────────────────┐ │  │
│  │  │ Chat Interface   │                    │ Test SDK Button (NEW)   │ │  │
│  │  │                  │                    │ - Launches Modal        │ │  │
│  │  │ - Messages       │◄──────────────────►│ - Opens Test Interface  │ │  │
│  │  │ - Input          │     Messages       └─────────────────────────┘ │  │
│  │  │ - Workflows      │                              │                  │  │
│  │  └──────────────────┘                              │ Triggers         │  │
│  │                                                    ▼                  │  │
│  │                                    ┌────────────────────────────────┐ │  │
│  │                                    │ CopilotTest Modal (NEW)        │ │  │
│  │                                    │                                │ │  │
│  │                                    │ ┌──────────────────────────┐  │ │  │
│  │                                    │ │ Test Configuration       │  │ │  │
│  │                                    │ │ - Prompt Input           │  │ │  │
│  │                                    │ │ - System Message         │  │ │  │
│  │                                    │ │ - Model Selection        │  │ │  │
│  │                                    │ │ - Timeout Config         │  │ │  │
│  │                                    │ └──────────────────────────┘  │ │  │
│  │                                    │                                │ │  │
│  │                                    │ ┌──────────────────────────┐  │ │  │
│  │                                    │ │ Action Buttons           │  │ │  │
│  │                                    │ │ - Test Copilot           │  │ │  │
│  │                                    │ │ - Health Check           │  │ │  │
│  │                                    │ │ - Clear                  │  │ │  │
│  │                                    │ └──────────────────────────┘  │ │  │
│  │                                    │                                │ │  │
│  │                                    │ ┌──────────────────────────┐  │ │  │
│  │                                    │ │ Results Display          │  │ │  │
│  │                                    │ │ - Status Alert           │  │ │  │
│  │                                    │ │ - Response Content       │  │ │  │
│  │                                    │ │ - Debug Log Expand       │  │ │  │
│  │                                    │ │ - Event Log Expand       │  │ │  │
│  │                                    │ └──────────────────────────┘  │ │  │
│  │                                    └────────────────────────────────┘ │  │
│  └────────────────────────────────────────────────────────────────────────┘  │
│                                                                               │
└───────────────────────────────────────────────────────────────────────────────┘
                                       │
                                       │ HTTP POST/GET
                                       ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                          ASP.NET Core Backend                                │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                               │
│  ┌────────────────────────────────────────────────────────────────────────┐  │
│  │ CopilotTestController (NEW) - Routing Layer                           │  │
│  │                                                                        │  │
│  │  POST /api/copilottest/test                                          │  │
│  │  ├─ Receives: {prompt, systemMessage, model, timeoutSeconds}         │  │
│  │  └─ Returns: CopilotTestResponse                                     │  │
│  │                                                                        │  │
│  │  GET /api/copilottest/health                                         │  │
│  │  ├─ Quick connectivity check                                         │  │
│  │  └─ Returns: {status, success, responseTime, timedOut}               │  │
│  │                                                                        │  │
│  └────────────────────────────────────────────────────────────────────────┘  │
│                                       │                                       │
│                                       │ Injects                               │
│                                       ▼                                       │
│  ┌────────────────────────────────────────────────────────────────────────┐  │
│  │ CopilotTestService (NEW) - Business Logic Layer                       │  │
│  │                                                                        │  │
│  │  ICopilotTestService                                                  │  │
│  │  └─ TestCopilotConnectionAsync()                                     │  │
│  │     ├─ Create CopilotClient                                          │  │
│  │     ├─ Initialize Session with Config                                │  │
│  │     ├─ Send Message                                                  │  │
│  │     ├─ Collect Response (StreamingEvent handling)                    │  │
│  │     ├─ Wait for Session Idle (with timeout)                          │  │
│  │     ├─ Capture Debug Log (timestamps, operations)                    │  │
│  │     ├─ Capture Event Log (all SDK events)                            │  │
│  │     └─ Return CopilotTestResponse                                    │  │
│  │                                                                        │  │
│  │  Response includes:                                                   │  │
│  │  - success: bool                                                      │  │
│  │  - content: string (response from Copilot)                            │  │
│  │  - debugLog: List<string> (detailed flow)                             │  │
│  │  - eventLog: List<string> (SDK events)                                │  │
│  │  - error: string (if failed)                                          │  │
│  │  - timedOut: bool                                                     │  │
│  │                                                                        │  │
│  └────────────────────────────────────────────────────────────────────────┘  │
│                                       │                                       │
│                                       │ Uses                                  │
│                                       ▼                                       │
│  ┌────────────────────────────────────────────────────────────────────────┐  │
│  │ GitHub Copilot SDK (External)                                         │  │
│  │                                                                        │  │
│  │  CopilotClient                                                        │  │
│  │  ├─ StartAsync()                                                      │  │
│  │  └─ CreateSessionAsync(config)                                        │  │
│  │                                                                        │  │
│  │  Session                                                              │  │
│  │  ├─ SendAsync(message)                                                │  │
│  │  ├─ On(eventHandler) - Stream events                                  │  │
│  │  └─ Events: AssistantMessageEvent, SessionIdleEvent                   │  │
│  │                                                                        │  │
│  │  Configuration                                                        │  │
│  │  ├─ Model (gpt-4, gpt-3.5-turbo, etc.)                                │  │
│  │  ├─ SystemMessage                                                     │  │
│  │  └─ Other session options                                             │  │
│  │                                                                        │  │
│  └────────────────────────────────────────────────────────────────────────┘  │
│                                       │                                       │
│                                       │ Network                               │
│                                       ▼                                       │
│  ┌────────────────────────────────────────────────────────────────────────┐  │
│  │ GitHub Copilot API (Cloud)                                            │  │
│  │                                                                        │  │
│  │ - Receives prompts                                                    │  │
│  │ - Processes with LLM                                                  │  │
│  │ - Streams responses back                                              │  │
│  │                                                                        │  │
│  └────────────────────────────────────────────────────────────────────────┘  │
│                                                                               │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Data Flow - Test Request

```
1. USER ACTION
   └─ Click "Test Copilot" button

2. FRONTEND
   └─ CopilotTest.tsx: handleTest()
      └─ POST /api/copilottest/test
         {prompt, systemMessage, model, timeoutSeconds}

3. BACKEND - ROUTING
   └─ CopilotTestController.TestCopilot()
      └─ Input validation
      └─ Call service

4. BACKEND - BUSINESS LOGIC
   └─ CopilotTestService.TestCopilotConnectionAsync()
      │
      ├─ [LOG] Start operation, timestamp
      │
      ├─ Create CopilotClient
      │  └─ [LOG] Client created
      │
      ├─ client.StartAsync()
      │  └─ [LOG] Client started
      │
      ├─ Create SessionConfig
      │
      ├─ client.CreateSessionAsync(config)
      │  └─ [LOG] Session created
      │
      ├─ Register event handlers
      │  ├─ On(AssistantMessageEvent) → Append to responseBuilder
      │  │  └─ [EVENT LOG] Event type + content
      │  └─ On(SessionIdleEvent) → Set completion signal
      │     └─ [EVENT LOG] Session idle
      │
      ├─ session.SendAsync(prompt)
      │  └─ [LOG] Message sent
      │
      ├─ Wait for SessionIdleEvent or Timeout
      │  ├─ Timeout triggered → [LOG] Timed out
      │  └─ Session complete → [LOG] Complete
      │
      ├─ Collect response from responseBuilder
      │
      └─ Return CopilotTestResponse
         {success, content, debugLog, eventLog, error, timedOut}

5. BACKEND - RESPONSE
   └─ Return 200 OK + JSON response

6. FRONTEND - DISPLAY
   └─ CopilotTest.tsx: setTestResponse()
      └─ Render results
         ├─ Status alert (✅ Success or ❌ Failed)
         ├─ Response content box
         ├─ Debug log (expandable)
         └─ Event log (expandable)
```

## Debug Log Example

```
[2026-01-29T12:34:56.123Z] Starting Copilot SDK test...
Model: gpt-4
Prompt: Tell me about the GitHub Copilot SDK.
System Message: (default)

[2026-01-29T12:34:56.124Z] CopilotClient created
[2026-01-29T12:34:56.125Z] Copilot client started
[2026-01-29T12:34:56.126Z] Creating session with config...
[2026-01-29T12:34:57.456Z] Session created successfully
[2026-01-29T12:34:57.457Z] Event handlers registered, sending message...
[2026-01-29T12:34:57.458Z] Message sent, waiting for response...
[2026-01-29T12:34:58.789Z] Event: AssistantMessageEvent
  Content: The GitHub Copilot SDK provides...
[2026-01-29T12:34:58.890Z] Event: SessionIdleEvent
  Session is now idle
[2026-01-29T12:34:58.891Z] Test completed successfully
[2026-01-29T12:34:58.892Z] Response length: 450 characters

Total Time: ~2.8 seconds
```

## Integration Points

### 1. Authentication
- Tests require authenticated user
- Token extracted from JWT claims
- Authorization header passed to controller

### 2. Dependency Injection
- `CopilotTestService` registered in Program.cs
- Scoped lifetime (new instance per request)
- ILogger<CopilotTestService> automatically injected
- IOptions<CopilotOptions> automatically injected

### 3. Configuration
- Copilot options read from `appsettings.json`
- Model, timeout, and other settings configurable
- Override with UI parameters

### 4. Error Handling
- Try-catch in service
- Full exception details logged
- Stack traces included in debug log
- User-friendly error messages returned

## Performance Characteristics

| Component | Typical Time | Notes |
|-----------|--------------|-------|
| Client creation | 10-100ms | Very fast |
| Session init | 500-1000ms | Depends on API |
| Message send | 10-100ms | Network latency |
| Response wait | 2-10s | LLM processing |
| Total | 2-13s | 30s timeout default |

## Security Considerations

✅ **Authorized Access**: Must be authenticated
✅ **Read-Only**: No workflow modifications
✅ **Isolated**: No impact on active workflows
✅ **Auditable**: All tests logged with timestamps
✅ **Error Handling**: No credential leakage
✅ **Input Validation**: Prompt validation before sending
✅ **Rate Limiting**: Can be added if needed

## Deployment Notes

### Prerequisites
- GitHub Copilot SDK NuGet package installed
- Copilot API credentials configured
- appsettings.json with CopilotOptions
- .NET 10+ runtime

### Configuration Example
```json
{
  "Copilot": {
    "Model": "gpt-4",
    "TimeoutSeconds": 30,
    "SystemPrompt": "You are a helpful assistant."
  }
}
```

### Production Considerations
- Enable structured logging
- Monitor Copilot API quota usage
- Set up alerts for failed tests
- Track response time metrics
- Consider rate limiting for test endpoint
