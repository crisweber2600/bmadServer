import React, { useState } from 'react';
import { Avatar, Tooltip, Typography, Divider } from 'antd';
import { RobotOutlined } from '@ant-design/icons';
import './AgentAttribution.css';

const { Text } = Typography;

export interface AgentAttributionProps {
  agentId: string;
  agentName: string;
  agentDescription?: string;
  capabilities?: string[];
  currentStepResponsibility?: string;
  avatarUrl?: string;
  timestamp: Date;
  size?: 'small' | 'large';
  variant?: 'inline' | 'block';
}

export const AgentAttribution: React.FC<AgentAttributionProps> = ({
  agentId,
  agentName,
  agentDescription,
  capabilities,
  currentStepResponsibility,
  avatarUrl,
  timestamp,
  size = 'large',
  variant = 'block',
}) => {
  const [hovering, setHovering] = useState(false);

  const formattedTime = new Intl.DateTimeFormat('en-US', {
    hour: '2-digit',
    minute: '2-digit',
    month: 'short',
    day: 'numeric',
    hour12: true,
  }).format(timestamp);

  // Generate avatar color based on agent ID hash
  const getAvatarColor = (id: string): string => {
    const colors = [
      '#f5222d', '#fa541c', '#fa8c16', '#faad14', '#ffc53d',
      '#fadb14', '#d4af37', '#a0d911', '#52c41a', '#13c2c2',
      '#1890ff', '#2f54eb', '#722ed1', '#eb2f96', '#fa8c16',
    ];
    const hash = id.split('').reduce((acc, char) => acc + char.charCodeAt(0), 0);
    return colors[hash % colors.length];
  };

  const tooltipContent = (
    <div className="agent-attribution-tooltip">
      <div className="tooltip-header">
        <strong>{agentName}</strong>
        <Text type="secondary" className="tooltip-id">
          ({agentId})
        </Text>
      </div>

      {agentDescription && (
        <>
          <Divider style={{ margin: '8px 0' }} />
          <div className="tooltip-section">
            <Text type="secondary">Description:</Text>
            <Text>{agentDescription}</Text>
          </div>
        </>
      )}

      {capabilities && capabilities.length > 0 && (
        <>
          <Divider style={{ margin: '8px 0' }} />
          <div className="tooltip-section">
            <Text type="secondary">Capabilities:</Text>
            <div className="capabilities-list">
              {capabilities.map((capability, idx) => (
                <span key={idx} className="capability-badge">
                  {capability}
                </span>
              ))}
            </div>
          </div>
        </>
      )}

      {currentStepResponsibility && (
        <>
          <Divider style={{ margin: '8px 0' }} />
          <div className="tooltip-section">
            <Text type="secondary">Current Step Responsibility:</Text>
            <Text>{currentStepResponsibility}</Text>
          </div>
        </>
      )}

      <Divider style={{ margin: '8px 0' }} />
      <Text type="secondary" className="tooltip-timestamp">
        {formattedTime}
      </Text>
    </div>
  );

  const avatar = (
    <Avatar
      icon={<RobotOutlined />}
      style={{ backgroundColor: getAvatarColor(agentId) }}
      src={avatarUrl}
      size={size === 'small' ? 24 : 32}
      className={`agent-avatar ${size}`}
      aria-label={agentName}
    />
  );

  if (variant === 'inline') {
    return (
      <Tooltip title={tooltipContent} placement="top">
        <span className="agent-attribution-inline" onMouseEnter={() => setHovering(true)} onMouseLeave={() => setHovering(false)}>
          {avatar}
          <Text strong className="agent-name">
            {agentName}
          </Text>
        </span>
      </Tooltip>
    );
  }

  return (
    <Tooltip title={tooltipContent} placement="top">
      <div
        className={`agent-attribution-block ${hovering ? 'hovering' : ''}`}
        onMouseEnter={() => setHovering(true)}
        onMouseLeave={() => setHovering(false)}
      >
        <div className="attribution-content">
          {avatar}
          <div className="attribution-text">
            <Text strong className="agent-name">
              {agentName}
            </Text>
            <Text type="secondary" className="agent-timestamp">
              {formattedTime}
            </Text>
          </div>
        </div>
      </div>
    </Tooltip>
  );
};
