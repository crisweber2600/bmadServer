# GitHub Copilot SDK Test Interface - Quick Start Guide

## Getting Started in 5 Minutes

### Step 1: Start the Application
```bash
cd d:\bmadServer\src
dotnet run --project bmadServer.AppHost
```

Wait for the application to fully start and show "Application started" in the console.

### Step 2: Open the Chat Application
- Open your browser and navigate to `http://localhost:5173` (default Vite dev server)
- You should see the BMAD Chat login screen

### Step 3: Create an Account or Sign In
1. **First time?** Click "Don't have an account? Register"
2. Enter:
   - Email: `test@example.com`
   - Display Name: `Test User`
   - Password: `TestPassword123!`
3. Click "Register"
4. Now click "Sign In" and enter your credentials
5. You should see the chat interface

### Step 4: Access the Test Interface
1. Look at the top header with "BMAD Chat"
2. Click the **"Test Copilot SDK"** button (it has a bug icon 🐞)
3. A modal dialog will open showing the Copilot test interface

### Step 5: Run Your First Test
1. The prompt field is pre-filled with: `"Tell me about the GitHub Copilot SDK."`
2. Click the **"Test Copilot"** button (play icon)
3. Wait for the test to complete
4. You should see:
   - ✅ Success status (green alert)
   - Response content from Copilot
   - Debug log entries showing the flow
   - Event log showing SDK events

### Step 6: Examine the Debug Output
1. **Debug Log**: Shows operation flow with timestamps
   - Client creation time
   - Session initialization
   - Message sending
   - Response completion time
   
2. **Event Log**: Shows Copilot SDK events
   - AssistantMessageEvent (content received)
   - SessionIdleEvent (session complete)

### Step 7: Try Different Prompts
1. Clear the current test
2. Enter a new prompt, e.g.:
   - `"What are the benefits of using GitHub Copilot?"`
   - `"Explain how to integrate Copilot SDK in a .NET application"`
   - `"How does Copilot handle context?"`
3. Click **"Test Copilot"** again

### Step 8: Run a Health Check
1. Click the **"Health Check"** button (heart icon)
2. This performs a quick connectivity test
3. You should see response time and status information

## Common Issues & Solutions

### Issue: Button Not Visible
**Solution**: Make sure you're signed in. The button only appears after authentication.

### Issue: "Failed" Status
**Solution**: 
1. Check your internet connection
2. Verify Copilot SDK is configured in `appsettings.json`
3. Check if Copilot API is accessible
4. Look at the debug log for specific error details

### Issue: Timeout Error
**Solution**:
1. The test timed out waiting for Copilot response
2. Try increasing the timeout value (e.g., from 30 to 60 seconds)
3. Try again - Copilot API might be slow

### Issue: Empty Response
**Solution**:
1. Check the event log - did you get an AssistantMessageEvent?
2. Try the Health Check to verify connectivity
3. Check your system prompt isn't conflicting

## Understanding the Response

### Success Response Shows:
```
✅ Success alert
Response Content: [Copilot's answer here]
Model: gpt-4
Timestamp: [ISO 8601 timestamp]

Debug Log:
[2026-01-29T12:34:56Z] Starting Copilot SDK test...
[2026-01-29T12:34:56Z] CopilotClient created
[2026-01-29T12:34:57Z] Copilot client started
[2026-01-29T12:34:57Z] Creating session with config...
[2026-01-29T12:34:58Z] Session created successfully
[2026-01-29T12:34:58Z] Event handlers registered, sending message...
[2026-01-29T12:34:58Z] Message sent, waiting for response...
[2026-01-29T12:34:59Z] Session completed successfully
[2026-01-29T12:34:59Z] Response length: 250 characters

Event Log:
[2026-01-29T12:34:59Z] Event: AssistantMessageEvent
  Content: [Response text here]
[2026-01-29T12:34:59Z] Event: SessionIdleEvent
  Session is now idle
```

## Key Features

| Feature | Purpose | How to Use |
|---------|---------|-----------|
| **Test Button** | Send a test prompt to Copilot | Enter prompt, click "Test Copilot" |
| **Health Check** | Quick connectivity test | Click "Health Check" button |
| **Debug Log** | See operation flow with timing | Expand "Debug Log" section |
| **Event Log** | See Copilot SDK events | Expand "Event Log" section |
| **Copy Button** | Copy log entries to clipboard | Click copy icon on any log entry |
| **Clear Button** | Reset the form | Click "Clear" button |

## What's Being Tested?

The test interface verifies:
1. ✅ Copilot client can be created
2. ✅ Client can connect to Copilot service
3. ✅ Session can be established with configuration
4. ✅ Message can be sent to Copilot
5. ✅ Response is received and parsed
6. ✅ Session completes cleanly
7. ✅ No timeout occurs within configured timeout

## Next Steps

Once you've verified the Copilot SDK is working:

1. **Run Integration Tests**:
   ```bash
   dotnet test bmadServer.ApiService.IntegrationTests
   ```

2. **Test with Workflows**:
   - Create a new chat session
   - Start a workflow
   - Chat naturally with the system

3. **Monitor Production**:
   - Check logs for any Copilot SDK errors
   - Monitor response times
   - Track success/failure rates

## Tips & Tricks

- **Longer Prompts**: The interface supports up to 2000 characters
- **Custom System Message**: Override the default system prompt for different behavior
- **Change Model**: Test with different Copilot models (gpt-4, gpt-3.5-turbo, etc.)
- **Adjust Timeout**: Increase timeout if Copilot is slow or responses are long
- **Copy Logs**: Click the copy icon to copy individual log entries for sharing/debugging

## Performance Expectations

**Typical Response Times:**
- Connection: 0-2 seconds
- Session creation: 0.5-1 second
- API response: 2-10 seconds
- Total: 2-13 seconds

**Debug Log Timing:**
Each log entry shows ISO 8601 timestamp for precise timing analysis.

## Need Help?

1. Check the **error type** in the failure alert
2. Review the **debug log** for specific error messages
3. Check the **event log** to see what SDK events occurred
4. Verify network connectivity with **Health Check**
5. Check `appsettings.json` for Copilot configuration
