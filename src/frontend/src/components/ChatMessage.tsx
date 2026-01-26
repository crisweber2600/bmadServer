import React from 'react';
import { Avatar, Typography } from 'antd';
import { RobotOutlined, UserOutlined } from '@ant-design/icons';
import ReactMarkdown from 'react-markdown';
import type { Components } from 'react-markdown';
import remarkGfm from 'remark-gfm';
import { Prism as SyntaxHighlighter } from 'react-syntax-highlighter';
import { vscDarkPlus } from 'react-syntax-highlighter/dist/esm/styles/prism';
import { AgentAttribution, type AgentAttributionProps } from './AgentAttribution';
import './ChatMessage.css';

const { Text } = Typography;

export interface ChatMessageProps {
  content: string;
  isUser: boolean;
  timestamp: Date;
  agentName?: string;
  agentAttribution?: Omit<AgentAttributionProps, 'timestamp' | 'variant'>;
}

export const ChatMessage: React.FC<ChatMessageProps> = ({
  content,
  isUser,
  timestamp,
  agentName = 'BMAD Agent',
  agentAttribution,
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
        {!isUser && agentAttribution ? (
          <AgentAttribution
            {...agentAttribution}
            timestamp={timestamp}
            variant="inline"
            size="small"
          />
        ) : !isUser ? (
          <Text strong className="agent-name">
            {agentName}
          </Text>
        ) : null}
        <div className={`message-bubble ${isUser ? 'user-bubble' : 'agent-bubble'}`}>
          <div className="message-text">
            <ReactMarkdown
              remarkPlugins={[remarkGfm]}
              components={{
                code: ({ inline, className, children, ...props }: { inline?: boolean; className?: string; children?: React.ReactNode }) => {
                  const match = /language-(\w+)/.exec(className || '');
                  return !inline && match ? (
                    <SyntaxHighlighter
                      style={vscDarkPlus as { [key: string]: React.CSSProperties }}
                      language={match[1]}
                      PreTag="div"
                      {...props}
                    >
                      {String(children).replace(/\n$/, '')}
                    </SyntaxHighlighter>
                  ) : (
                    <code className={className} {...props}>
                      {children}
                    </code>
                  );
                },
                a: ({ children, ...rest }: React.AnchorHTMLAttributes<HTMLAnchorElement> & { children?: React.ReactNode }) => (
                  <a {...rest} target="_blank" rel="noopener noreferrer">
                    {children}
                  </a>
                ),
              } as Components}
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
