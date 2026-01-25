import React from 'react';
import { Avatar, Typography } from 'antd';
import { RobotOutlined } from '@ant-design/icons';
import './TypingIndicator.css';

const { Text } = Typography;

export interface TypingIndicatorProps {
  agentName?: string;
}

export const TypingIndicator: React.FC<TypingIndicatorProps> = ({
  agentName = 'BMAD Agent',
}) => {
  return (
    <div
      className="typing-indicator"
      role="status"
      aria-live="polite"
      aria-label={`${agentName} is typing`}
    >
      <Avatar
        icon={<RobotOutlined />}
        className="typing-avatar"
        style={{ backgroundColor: '#1890ff' }}
        aria-hidden="true"
      />
      <div className="typing-content">
        <Text strong className="typing-agent-name">
          {agentName}
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
