import { useState } from 'react';
import { Button, Input, Space } from 'antd';
import { ChatContainer, ChatMessage, TypingIndicator } from './components';
import './ChatDemo.css';

interface Message {
  id: string;
  content: string;
  isUser: boolean;
  timestamp: Date;
  agentName?: string;
}

export function ChatDemo() {
  const [messages, setMessages] = useState<Message[]>([
    {
      id: '1',
      content: 'Hello! I\'m BMAD Agent. How can I help you today?',
      isUser: false,
      timestamp: new Date(Date.now() - 60000),
      agentName: 'BMAD Agent',
    },
  ]);
  const [inputValue, setInputValue] = useState('');
  const [isTyping, setIsTyping] = useState(false);

  const handleSend = () => {
    if (!inputValue.trim()) return;

    // Add user message
    const userMessage: Message = {
      id: Date.now().toString(),
      content: inputValue,
      isUser: true,
      timestamp: new Date(),
    };

    setMessages((prev) => [...prev, userMessage]);
    setInputValue('');

    // Simulate agent typing
    setIsTyping(true);

    // Simulate agent response after 2 seconds
    setTimeout(() => {
      const agentMessage: Message = {
        id: (Date.now() + 1).toString(),
        content: generateResponse(inputValue),
        isUser: false,
        timestamp: new Date(),
        agentName: 'BMAD Agent',
      };

      setMessages((prev) => [...prev, agentMessage]);
      setIsTyping(false);
    }, 2000);
  };

  const generateResponse = (userInput: string): string => {
    const lower = userInput.toLowerCase();

    if (lower.includes('help')) {
      return `I'd be happy to help! Here are some things I can do:

- Answer questions about **BMAD**
- Provide code examples
- Explain concepts
- And much more!

What would you like to know?`;
    }

    if (lower.includes('code')) {
      return `Here's a simple code example:

\`\`\`javascript
function greet(name) {
  return \`Hello, \${name}!\`;
}

console.log(greet('World'));
\`\`\`

This function takes a name and returns a greeting. Pretty simple, right?`;
    }

    if (lower.includes('link')) {
      return `Sure! Here are some useful links:

- [React Documentation](https://react.dev)
- [Ant Design](https://ant.design)
- [TypeScript Handbook](https://www.typescriptlang.org/docs/)

Click any of these to learn more!`;
    }

    return `You said: "${userInput}"

That's interesting! I can help you with various tasks. Try asking about:
- **Help** - See what I can do
- **Code** - Get code examples
- **Link** - Get useful links`;
  };

  return (
    <div className="chat-demo">
      <div className="chat-header">
        <h2>BMAD Chat Demo</h2>
        <p>Try sending messages to see the components in action!</p>
      </div>

      <div className="chat-messages">
        <ChatContainer>
          {messages.map((msg) => (
            <ChatMessage
              key={msg.id}
              content={msg.content}
              isUser={msg.isUser}
              timestamp={msg.timestamp}
              agentName={msg.agentName}
            />
          ))}
          {isTyping && <TypingIndicator agentName="BMAD Agent" />}
        </ChatContainer>
      </div>

      <div className="chat-input">
        <Space.Compact style={{ width: '100%' }}>
          <Input
            placeholder="Type your message..."
            value={inputValue}
            onChange={(e) => setInputValue(e.target.value)}
            onPressEnter={handleSend}
            disabled={isTyping}
          />
          <Button type="primary" onClick={handleSend} disabled={isTyping}>
            Send
          </Button>
        </Space.Compact>
      </div>
    </div>
  );
}
