import React, { useEffect, useState } from 'react';
import { Avatar, Typography } from 'antd';
import { RobotOutlined, SwapRightOutlined } from '@ant-design/icons';
import './AgentHandoffIndicator.css';

const { Text } = Typography;

export interface AgentHandoffIndicatorProps {
  fromAgentId: string;
  fromAgentName: string;
  toAgentId: string;
  toAgentName: string;
  timestamp: Date;
  reason?: string;
  stepName?: string;
  fromAvatarUrl?: string;
  toAvatarUrl?: string;
}

export const AgentHandoffIndicator: React.FC<AgentHandoffIndicatorProps> = ({
  fromAgentId,
  fromAgentName,
  toAgentId,
  toAgentName,
  timestamp,
  reason,
  stepName,
  fromAvatarUrl,
  toAvatarUrl,
}) => {
  const [isVisible, setIsVisible] = useState(false);

  useEffect(() => {
    setIsVisible(true);
  }, []);

  const getAvatarColor = (id: string): string => {
    const colors = [
      '#f5222d', '#fa541c', '#fa8c16', '#faad14', '#ffc53d',
      '#fadb14', '#d4af37', '#a0d911', '#52c41a', '#13c2c2',
      '#1890ff', '#2f54eb', '#722ed1', '#eb2f96', '#fa8c16',
    ];
    const hash = id.split('').reduce((acc, char) => acc + char.charCodeAt(0), 0);
    return colors[hash % colors.length];
  };

  const formattedTime = new Intl.DateTimeFormat('en-US', {
    hour: '2-digit',
    minute: '2-digit',
    hour12: true,
  }).format(timestamp);

  return (
    <div
      className={`agent-handoff-indicator ${isVisible ? 'visible' : ''}`}
      role="status"
      aria-label={`Handoff from ${fromAgentName} to ${toAgentName}`}
    >
      <div className="handoff-container">
        <div className="handoff-avatars">
          <Avatar
            icon={<RobotOutlined />}
            style={{ backgroundColor: getAvatarColor(fromAgentId) }}
            src={fromAvatarUrl}
            size={32}
            className="from-avatar"
          />
          <div className="swap-icon">
            <SwapRightOutlined />
          </div>
          <Avatar
            icon={<RobotOutlined />}
            style={{ backgroundColor: getAvatarColor(toAgentId) }}
            src={toAvatarUrl}
            size={32}
            className="to-avatar"
          />
        </div>

        <div className="handoff-content">
          <div className="handoff-message">
            <Text strong className="handoff-text">
              Handing off to <span className="agent-highlight">{toAgentName}</span>...
            </Text>
          </div>

          <div className="handoff-details">
            {stepName && (
              <Text type="secondary" className="detail-item">
                <span className="label">Step:</span> {stepName}
              </Text>
            )}

            {reason && (
              <Text type="secondary" className="detail-item">
                <span className="label">Reason:</span> {reason}
              </Text>
            )}

            <Text type="secondary" className="detail-item">
              <span className="label">Time:</span> {formattedTime}
            </Text>
          </div>
        </div>
      </div>
    </div>
  );
};
