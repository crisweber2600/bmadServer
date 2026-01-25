import React from 'react';
import { Avatar, Typography } from 'antd';
import { RobotOutlined, UserOutlined } from '@ant-design/icons';
import ReactMarkdown from 'react-markdown';
import remarkGfm from 'remark-gfm';
import { Prism as SyntaxHighlighter } from 'react-syntax-highlighter';
import { vscDarkPlus } from 'react-syntax-highlighter/dist/esm/styles/prism';
import './ChatMessage.css';

const { Text } = Typography;

export interface ChatMessageProps {
  content: string;
  isUser: boolean;
  timestamp: Date;
  agentName?: string;
}

export const ChatMessage: React.FC<ChatMessageProps> = ({
  content,
  isUser,
  timestamp,
  agentName = 'BMAD Agent',
}) => {
  const formattedTime = new Intl.DateTimeFormat('en-US', {
    hour: '2-digit',
    minute: '2-digit',
    hour12: true,
  }).format(timestamp);

  const ariaLabel = isUser
    ? `You said: ${content} at ${formattedTime}`
    : `${agentName} said: ${content} at ${formattedTime}`;

  return (
    <div
      className={`chat-message ${isUser ? 'user-message' : 'agent-message'}`}
      role="article"
      aria-label={ariaLabel}
    >
      {!isUser && (
        <Avatar
          icon={<RobotOutlined />}
          className="message-avatar"
          style={{ backgroundColor: '#1890ff' }}
          aria-hidden="true"
        />
      )}
      <div className="message-content-wrapper">
        {!isUser && (
          <Text strong className="agent-name">
            {agentName}
          </Text>
        )}
        <div className={`message-bubble ${isUser ? 'user-bubble' : 'agent-bubble'}`}>
          <div className="message-text">
            <ReactMarkdown
              remarkPlugins={[remarkGfm]}
              components={{
                // eslint-disable-next-line @typescript-eslint/no-explicit-any
                code(props: any) {
                  const { inline, className, children } = props;
                  const match = /language-(\w+)/.exec(className || '');
                  return !inline && match ? (
                    <SyntaxHighlighter
                      style={vscDarkPlus as { [key: string]: React.CSSProperties }}
                      language={match[1]}
                      PreTag="div"
                    >
                      {String(children).replace(/\n$/, '')}
                    </SyntaxHighlighter>
                  ) : (
                    <code className={className}>
                      {children}
                    </code>
                  );
                },
                // eslint-disable-next-line @typescript-eslint/no-explicit-any
                a(props: any) {
                  const { children, ...rest } = props;
                  return (
                    <a {...rest} target="_blank" rel="noopener noreferrer">
                      {children}
                    </a>
                  );
                },
              }}
            >
              {content}
            </ReactMarkdown>
          </div>
          <Text className="message-timestamp" type="secondary">
            {formattedTime}
          </Text>
        </div>
      </div>
      {isUser && (
        <Avatar
          icon={<UserOutlined />}
          className="message-avatar"
          style={{ backgroundColor: '#1890ff' }}
          aria-hidden="true"
        />
      )}
    </div>
  );
};
