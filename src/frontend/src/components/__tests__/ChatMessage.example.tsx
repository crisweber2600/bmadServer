import { ChatMessage, TypingIndicator } from '../ChatMessage';

/**
 * Example usage of ChatMessage component
 * Demonstrates user and agent messages with markdown support
 */
export function ChatMessageExample() {
  return (
    <div style={{ padding: '20px', maxWidth: '800px', margin: '0 auto' }}>
      <h2>Chat Message Examples</h2>
      
      {/* User Message */}
      <ChatMessage
        id="msg-1"
        role="user"
        content="Hello! Can you help me create a PRD for my project?"
        timestamp={new Date()}
      />

      {/* Agent Message with Markdown */}
      <ChatMessage
        id="msg-2"
        role="agent"
        content="Of course! I'd be happy to help you create a **Product Requirements Document**. Here's what we'll cover:

1. **Project Overview**
2. **User Stories**
3. **Functional Requirements**
4. **Non-Functional Requirements**

Let's start with your project overview. What is your project about?"
        timestamp={new Date()}
        agentName="Planning Agent"
      />

      {/* User Message with Code */}
      <ChatMessage
        id="msg-3"
        role="user"
        content="It's a chat application using React and SignalR. Here's the basic setup:

```typescript
const connection = new HubConnectionBuilder()
  .withUrl('/hubs/chat')
  .withAutomaticReconnect()
  .build();
```"
        timestamp={new Date()}
      />

      {/* Agent Response with Link */}
      <ChatMessage
        id="msg-4"
        role="agent"
        content="Great! A real-time chat application is an excellent project. I can see you're using SignalR for WebSocket connections.

For reference, you might want to check out the [official SignalR documentation](https://learn.microsoft.com/en-us/aspnet/core/signalr/introduction).

Now, let's define your key user stories..."
        timestamp={new Date()}
        agentName="Planning Agent"
      />

      {/* Typing Indicator */}
      <TypingIndicator agentName="Planning Agent" />
    </div>
  );
}
