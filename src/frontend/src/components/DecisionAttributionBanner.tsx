import React, { useState } from 'react';
import { CheckCircleOutlined, ChevronDownOutlined, ChevronUpOutlined } from '@ant-design/icons';
import './DecisionAttributionBanner.css';

export interface DecisionAttributionBannerProps {
  agentId: string;
  agentName: string;
  timestamp: Date;
  reasoning?: string;
  confidence?: number;
  decidedAt?: Date;
  avatarUrl?: string;
}

export const DecisionAttributionBanner: React.FC<DecisionAttributionBannerProps> = ({
  agentId,
  agentName,
  timestamp,
  reasoning,
  confidence,
  decidedAt = timestamp,
  avatarUrl,
}) => {
  const [isExpanded, setIsExpanded] = useState(false);

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

  const formatTimestamp = (date: Date): string => {
    const formatter = new Intl.DateTimeFormat('en-US', {
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
      hour12: true,
    });
    return formatter.format(date);
  };

  const agentColor = getColorHash(agentId);

  return (
    <div
      className="decision-attribution-banner"
      role="region"
      aria-label={`Decision by ${agentName} at ${formatTimestamp(decidedAt)}`}
    >
      <div className="decision-header">
        <div className="decision-icon-wrapper">
          <CheckCircleOutlined
            className="decision-icon"
            style={{ color: agentColor }}
          />
        </div>

        <div className="decision-info">
          <div className="decision-text">
            <span className="decision-label">Decided by </span>
            <strong style={{ color: agentColor }}>{agentName}</strong>
            <span className="decision-time">
              {' '}at {formatTimestamp(decidedAt)}
            </span>
          </div>

          {confidence !== undefined && (
            <div className="decision-confidence">
              <span className="confidence-label">Confidence:</span>
              <div className="confidence-bar">
                <div
                  className="confidence-fill"
                  style={{
                    width: `${confidence * 100}%`,
                    backgroundColor: agentColor,
                  }}
                  role="progressbar"
                  aria-valuenow={Math.round(confidence * 100)}
                  aria-valuemin={0}
                  aria-valuemax={100}
                />
              </div>
              <span className="confidence-percentage">
                {Math.round(confidence * 100)}%
              </span>
            </div>
          )}
        </div>

        {reasoning && (
          <button
            className="toggle-reasoning-button"
            onClick={() => setIsExpanded(!isExpanded)}
            aria-expanded={isExpanded}
            aria-label={isExpanded ? 'Hide reasoning' : 'Show reasoning'}
          >
            {isExpanded ? (
              <ChevronUpOutlined />
            ) : (
              <ChevronDownOutlined />
            )}
          </button>
        )}
      </div>

      {reasoning && isExpanded && (
        <div className="decision-reasoning">
          <h4 className="reasoning-title">Decision Reasoning:</h4>
          <p className="reasoning-content">{reasoning}</p>
        </div>
      )}
    </div>
  );
};
