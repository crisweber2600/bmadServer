import React, { useState } from 'react';
import { 
  CheckCircleOutlined, 
  DownOutlined, 
  UpOutlined, 
  LockOutlined, 
  UnlockOutlined,
  HistoryOutlined 
} from '@ant-design/icons';
import { Button, Modal, Input, Tooltip, Space, Tag, message } from 'antd';
import './DecisionAttributionBanner.css';

const { TextArea } = Input;

export interface DecisionAttributionBannerProps {
  agentId: string;
  agentName: string;
  timestamp: Date;
  reasoning?: string;
  confidence?: number;
  decidedAt?: Date;
  avatarUrl?: string;
  /** Lock state props */
  isLocked?: boolean;
  lockedBy?: string;
  lockedAt?: Date;
  lockReason?: string;
  canLock?: boolean;
  canUnlock?: boolean;
  onLock?: (reason: string) => void;
  onUnlock?: () => void;
  onViewHistory?: () => void;
}

export const DecisionAttributionBanner: React.FC<DecisionAttributionBannerProps> = ({
  agentId,
  agentName,
  timestamp,
  reasoning,
  confidence,
  decidedAt = timestamp,
  isLocked = false,
  lockedBy,
  lockedAt,
  lockReason,
  canLock = false,
  canUnlock = false,
  onLock,
  onUnlock,
  onViewHistory,
}) => {
  const [isExpanded, setIsExpanded] = useState(false);
  const [isLockModalOpen, setIsLockModalOpen] = useState(false);
  const [lockReasonInput, setLockReasonInput] = useState('');

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

  const handleLockClick = () => {
    setIsLockModalOpen(true);
  };

  const handleLockConfirm = () => {
    if (onLock && lockReasonInput.trim()) {
      onLock(lockReasonInput.trim());
      setIsLockModalOpen(false);
      setLockReasonInput('');
      message.success('Decision locked successfully');
    }
  };

  const handleUnlockClick = () => {
    if (onUnlock) {
      onUnlock();
      message.success('Decision unlocked successfully');
    }
  };

  const handleViewHistory = () => {
    if (onViewHistory) {
      onViewHistory();
    }
  };

  const agentColor = getColorHash(agentId);

  // CSS variable for dynamic agent color
  const agentColorStyle = { '--agent-color': agentColor } as React.CSSProperties;

  return (
    <div
      className={`decision-attribution-banner ${isLocked ? 'is-locked' : ''}`}
      role="region"
      aria-label={`Decision by ${agentName} at ${formatTimestamp(decidedAt)}`}
      data-testid="decision-attribution-banner"
      style={agentColorStyle}
    >
      <div className="decision-header">
        <div className="decision-icon-wrapper">
          <CheckCircleOutlined className="decision-icon" />
        </div>

        <div className="decision-info">
          <div className="decision-text">
            <span className="decision-label">Decided by </span>
            <strong className="agent-name">{agentName}</strong>
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
                  style={{ width: `${confidence * 100}%` }}
                  role="progressbar"
                  aria-valuenow={Math.round(confidence * 100)}
                  aria-valuemin={0}
                  aria-valuemax={100}
                  aria-label={`Confidence ${Math.round(confidence * 100)}%`}
                  title={`Confidence: ${Math.round(confidence * 100)}%`}
                />
              </div>
              <span className="confidence-percentage">
                {Math.round(confidence * 100)}%
              </span>
            </div>
          )}
        </div>

        <div className="decision-actions">
          <Space size="small">
            {/* Lock Status / Controls */}
            {isLocked ? (
              <>
                <Tag
                  icon={<LockOutlined />}
                  color="gold"
                  className="lock-status-tag"
                  data-testid="locked-badge"
                >
                  Locked by {lockedBy}
                </Tag>
                {canUnlock && onUnlock ? (
                  <Button
                    size="small"
                    icon={<UnlockOutlined />}
                    onClick={handleUnlockClick}
                    data-testid="unlock-button"
                    aria-label="Unlock decision"
                  >
                    Unlock
                  </Button>
                ) : (
                  <Tooltip title={`Only ${lockedBy} or admins can unlock`}>
                    <Button
                      size="small"
                      icon={<UnlockOutlined />}
                      disabled
                      data-testid="unlock-button-disabled"
                      aria-label="Unlock decision (disabled)"
                    >
                      Unlock
                    </Button>
                  </Tooltip>
                )}
              </>
            ) : (
              canLock && onLock && (
                <Button
                  size="small"
                  icon={<UnlockOutlined />}
                  onClick={handleLockClick}
                  data-testid="lock-button"
                  aria-label="Lock decision"
                >
                  Lock
                </Button>
              )
            )}

            {/* History Button */}
            {onViewHistory && (
              <Button
                size="small"
                icon={<HistoryOutlined />}
                onClick={handleViewHistory}
                data-testid="history-button"
                aria-label="View decision history"
              >
                History
              </Button>
            )}

            {/* Expand/Collapse Reasoning */}
            {reasoning && (
              <button
                className="toggle-reasoning-button"
                onClick={() => setIsExpanded(!isExpanded)}
                aria-expanded={isExpanded ? 'true' : 'false'}
                aria-label={isExpanded ? 'Hide reasoning' : 'Show reasoning'}
                data-testid="toggle-reasoning-button"
              >
                {isExpanded ? <UpOutlined /> : <DownOutlined />}
              </button>
            )}
          </Space>
        </div>
      </div>

      {/* Lock Reason Display */}
      {isLocked && lockReason && (
        <div className="lock-reason" data-testid="lock-reason">
          <strong>Lock reason:</strong> {lockReason}
          {lockedAt && (
            <span className="lock-time"> (locked {formatTimestamp(lockedAt)})</span>
          )}
        </div>
      )}

      {/* Reasoning Section */}
      {reasoning && isExpanded && (
        <div className="decision-reasoning" data-testid="decision-reasoning">
          <h4 className="reasoning-title">Decision Reasoning:</h4>
          <p className="reasoning-content">{reasoning}</p>
        </div>
      )}

      {/* Lock Modal */}
      <Modal
        title="Lock Decision"
        open={isLockModalOpen}
        onOk={handleLockConfirm}
        onCancel={() => {
          setIsLockModalOpen(false);
          setLockReasonInput('');
        }}
        okText="Lock Decision"
        okButtonProps={{ disabled: !lockReasonInput.trim() }}
        data-testid="lock-modal"
      >
        <div className="lock-modal-content">
          <p>Locking this decision will prevent further changes until unlocked.</p>
          <label htmlFor="lock-reason-input">
            Please provide a reason for locking:
          </label>
          <TextArea
            id="lock-reason-input"
            rows={3}
            value={lockReasonInput}
            onChange={(e) => setLockReasonInput(e.target.value)}
            placeholder="e.g., Final decision approved by team"
            data-testid="lock-reason-input"
          />
        </div>
      </Modal>
    </div>
  );
};
