import React from 'react';
import { Avatar, Badge, Tag, Tooltip, Typography, Card } from 'antd';
import { RobotOutlined, AimOutlined } from '@ant-design/icons';
import './AgentRosterGrid.css';

const { Text } = Typography;

export interface AgentInfo {
  agentId: string;
  agentName: string;
  role?: string;
  capabilities?: string[];
  avatarUrl?: string;
  relevanceScore?: number;
  isActive?: boolean;
}

export interface AgentRosterGridProps {
  /** List of agents to display */
  agents: AgentInfo[];
  /** Callback when an agent card is clicked */
  onAgentSelect?: (agentId: string) => void;
  /** Maximum capabilities to show per agent */
  maxCapabilities?: number;
  /** Whether to show relevance badges */
  showRelevanceBadges?: boolean;
}

/**
 * Responsive grid displaying party mode agents
 * 
 * Shows agents in a 3-column grid on desktop, single column on mobile.
 * Each card displays avatar, name, role, capabilities, and relevance badge.
 * 
 * @example
 * ```tsx
 * <AgentRosterGrid
 *   agents={[
 *     { agentId: '1', agentName: 'Winston', role: 'Architect', relevanceScore: 0.85 }
 *   ]}
 *   onAgentSelect={(id) => console.log(`Selected: ${id}`)}
 * />
 * ```
 */
export const AgentRosterGrid: React.FC<AgentRosterGridProps> = ({
  agents,
  onAgentSelect,
  maxCapabilities = 2,
  showRelevanceBadges = true,
}) => {
  // Generate avatar color based on agent ID hash
  const getColorHash = (id: string): string => {
    let hash = 0;
    for (let i = 0; i < id.length; i++) {
      const char = id.charCodeAt(i);
      hash = (hash << 5) - hash + char;
      hash = hash & hash;
    }
    const hue = Math.abs(hash) % 360;
    return `hsl(${hue}, 70%, 60%)`;
  };

  // Get relevance badge based on score
  const getRelevanceBadge = (score?: number) => {
    if (score === undefined || !showRelevanceBadges) return null;

    if (score > 0.7) {
      return (
        <Tag color="green" className="relevance-badge">
          <AimOutlined /> Highly Relevant
        </Tag>
      );
    }
    if (score > 0.4) {
      return (
        <Tag color="blue" className="relevance-badge">
          Relevant
        </Tag>
      );
    }
    return (
      <Tag color="default" className="relevance-badge">
        Low Relevance
      </Tag>
    );
  };

  const handleCardClick = (agentId: string) => {
    onAgentSelect?.(agentId);
  };

  const handleKeyDown = (e: React.KeyboardEvent, agentId: string) => {
    if (e.key === 'Enter' || e.key === ' ') {
      e.preventDefault();
      onAgentSelect?.(agentId);
    }
  };

  if (agents.length === 0) {
    return (
      <div className="agent-roster-empty" data-testid="agent-roster-empty">
        <Text type="secondary">No agents available</Text>
      </div>
    );
  }

  return (
    <div 
      className="agent-roster-grid"
      role="group"
      aria-label="Available agents"
      data-testid="agent-roster-grid"
    >
      {agents.map((agent) => (
        <Card
          key={agent.agentId}
          className={`agent-card ${agent.isActive ? 'active' : ''}`}
          onClick={() => handleCardClick(agent.agentId)}
          onKeyDown={(e) => handleKeyDown(e, agent.agentId)}
          hoverable
          tabIndex={0}
          aria-label={`Agent ${agent.agentName}${agent.role ? `, ${agent.role}` : ''}`}
          data-testid={`agent-card-${agent.agentId}`}
        >
          <div className="agent-card-content">
            <div className="agent-header">
              <Badge dot={agent.isActive} status="success" offset={[-4, 28]}>
                <Avatar
                  icon={<RobotOutlined />}
                  src={agent.avatarUrl}
                  style={{ backgroundColor: getColorHash(agent.agentId) }}
                  size={48}
                  aria-hidden="true"
                />
              </Badge>
              <div className="agent-info">
                <Text strong className="agent-name">{agent.agentName}</Text>
                {agent.role && (
                  <Text type="secondary" className="agent-role">{agent.role}</Text>
                )}
              </div>
            </div>

            {showRelevanceBadges && getRelevanceBadge(agent.relevanceScore)}

            {agent.capabilities && agent.capabilities.length > 0 && (
              <div className="agent-capabilities">
                {agent.capabilities.slice(0, maxCapabilities).map((cap, idx) => (
                  <Tag key={idx} className="capability-tag">{cap}</Tag>
                ))}
                {agent.capabilities.length > maxCapabilities && (
                  <Tooltip title={agent.capabilities.slice(maxCapabilities).join(', ')}>
                    <Tag className="capability-more">
                      +{agent.capabilities.length - maxCapabilities}
                    </Tag>
                  </Tooltip>
                )}
              </div>
            )}
          </div>
        </Card>
      ))}
    </div>
  );
};

export default AgentRosterGrid;
