import React, { useMemo } from 'react';
import { Avatar, Typography } from 'antd';
import { RobotOutlined } from '@ant-design/icons';
import './TypingIndicator.css';

const { Text } = Typography;

export interface TypingIndicatorProps {
  /**
   * @deprecated Use typingUsers instead. Will be removed in v2.0.
   * Single agent name for backward compatibility.
   */
  agentName?: string;
  /**
   * Array of typing user/agent names for multi-user support.
   * Takes precedence over agentName if provided.
   */
  typingUsers?: string[];
}

export const TypingIndicator: React.FC<TypingIndicatorProps> = ({
  agentName,
  typingUsers,
}) => {
  // Normalize to array with backward compatibility
  const normalizedUsers = useMemo(() => {
    if (typingUsers && typingUsers.length > 0) {
      return typingUsers;
    }
    if (agentName) {
      // Log deprecation warning in development
      if (process.env.NODE_ENV === 'development') {
        console.warn('[TypingIndicator] agentName prop is deprecated. Use typingUsers instead.');
      }
      return [agentName];
    }
    return [];
  }, [agentName, typingUsers]);

  // Don't render if no one is typing
  if (normalizedUsers.length === 0) {
    return null;
  }

  // Format typing message based on number of users
  const getTypingMessage = (): string => {
    if (normalizedUsers.length === 1) {
      return `${normalizedUsers[0]} is typing...`;
    }
    if (normalizedUsers.length === 2) {
      return `${normalizedUsers[0]}, ${normalizedUsers[1]} are typing...`;
    }
    if (normalizedUsers.length === 3) {
      return `${normalizedUsers[0]}, ${normalizedUsers[1]}, ${normalizedUsers[2]} are typing...`;
    }
    // 4 or more users
    return `${normalizedUsers.length} people are typing...`;
  };

  // Display name (first user's name or count)
  const displayName = normalizedUsers.length <= 3 
    ? normalizedUsers.join(', ') 
    : `${normalizedUsers.length} people`;

  return (
    <div
      className="typing-indicator"
      role="status"
      aria-live="polite"
      aria-label={getTypingMessage()}
      data-testid="typing-indicator"
    >
      <Avatar
        icon={<RobotOutlined />}
        className="typing-avatar"
        style={{ backgroundColor: '#1890ff' }}
        aria-hidden="true"
      />
      <div className="typing-content">
        <Text strong className="typing-agent-name">
          {displayName}
        </Text>
        <div className="typing-bubble">
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
