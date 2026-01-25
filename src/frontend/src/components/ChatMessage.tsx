import React from 'react';
import { Avatar, Typography } from 'antd';
import { RobotOutlined, UserOutlined } from '@ant-design/icons';
import ReactMarkdown from 'react-markdown';
import remarkGfm from 'remark-gfm';
import rehypeHighlight from 'rehype-highlight';
import 'highlight.js/styles/github-dark.css';
import './ChatMessage.css';

const { Text } = Typography;

export interface ChatMessageProps {
  /**
   * Unique identifier for the message
   */
  id: string;

  /**
   * Role of the message sender: 'user' or 'agent'
   */
  role: 'user' | 'agent';

  /**
   * Content of the message (supports markdown)
   */
  content: string;

  /**
   * Timestamp when the message was sent
   */
  timestamp: Date;

  /**
   * Agent name (only for agent messages)
   */
  agentName?: string;
}

/**
 * ChatMessage component displays individual chat messages with proper formatting.
 * - User messages: aligned right, blue background
 * - Agent messages: aligned left, gray background, with avatar
 * - Markdown support with syntax highlighting
 * - Accessible with ARIA labels and screen reader support
 */
export const ChatMessage: React.FC<ChatMessageProps> = ({
  id,
  role,
  content,
  timestamp,
  agentName = 'BMAD Agent',
}) => {
  const isUser = role === 'user';
  const formattedTime = timestamp.toLocaleTimeString([], {
    hour: '2-digit',
    minute: '2-digit',
  });

  return (
    <div
      className={`chat-message ${isUser ? 'chat-message-user' : 'chat-message-agent'}`}
      role="article"
      aria-label={`${isUser ? 'You' : agentName} said: ${content}`}
      data-message-id={id}
    >
      <div className="chat-message-content">
        {!isUser && (
          <Avatar
            className="chat-message-avatar"
            icon={<RobotOutlined />}
            style={{ backgroundColor: '#87d068' }}
            aria-label={`${agentName} avatar`}
          />
        )}
        <div className="chat-message-bubble">
          {!isUser && (
            <Text className="chat-message-agent-name" strong aria-label="Agent name">
              {agentName}
            </Text>
          )}
          <div className="chat-message-text">
            <ReactMarkdown
              remarkPlugins={[remarkGfm]}
              rehypePlugins={[rehypeHighlight]}
              components={{
                // Make links open in new tab
                a: ({ node, ...props }) => (
                  <a {...props} target="_blank" rel="noopener noreferrer" />
                ),
              }}
            >
              {content}
            </ReactMarkdown>
          </div>
          <Text className="chat-message-timestamp" type="secondary" aria-label="Message time">
            {formattedTime}
          </Text>
        </div>
        {isUser && (
          <Avatar
            className="chat-message-avatar"
            icon={<UserOutlined />}
            style={{ backgroundColor: '#1890ff' }}
            aria-label="Your avatar"
          />
        )}
      </div>
    </div>
  );
};

/**
 * TypingIndicator component shows animated ellipsis when agent is typing.
 * Displays within 500ms of agent starting to type.
 */
export interface TypingIndicatorProps {
  agentName?: string;
}

export const TypingIndicator: React.FC<TypingIndicatorProps> = ({ agentName = 'BMAD Agent' }) => {
  return (
    <div
      className="chat-message chat-message-agent"
      role="status"
      aria-label={`${agentName} is typing`}
      aria-live="polite"
    >
      <div className="chat-message-content">
        <Avatar
          className="chat-message-avatar"
          icon={<RobotOutlined />}
          style={{ backgroundColor: '#87d068' }}
          aria-label={`${agentName} avatar`}
        />
        <div className="chat-message-bubble typing-indicator">
          <Text className="chat-message-agent-name" strong>
            {agentName}
          </Text>
          <div className="typing-dots">
            <span className="dot"></span>
            <span className="dot"></span>
            <span className="dot"></span>
          </div>
        </div>
      </div>
    </div>
  );
};
