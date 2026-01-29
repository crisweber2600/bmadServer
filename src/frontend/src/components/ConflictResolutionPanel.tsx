import React, { useState } from 'react';
import { Modal, Button, Radio, Input, Space, Typography, Alert, Divider } from 'antd';
import { CheckCircleOutlined, WarningOutlined, SwapOutlined } from '@ant-design/icons';
import { DiffViewer } from './DiffViewer';
import './ConflictResolutionPanel.css';

const { TextArea } = Input;
const { Text, Title } = Typography;

export interface ConflictDecision {
  id: string;
  title: string;
  content: string;
  author: string;
  timestamp: Date;
  metadata?: Record<string, unknown>;
}

export interface ConflictData {
  id: string;
  type: string;
  description: string;
  severity: 'high' | 'medium' | 'low';
  decision1: ConflictDecision;
  decision2: ConflictDecision;
  createdAt: Date;
}

export interface ConflictResolutionPanelProps {
  /** The conflict to resolve */
  conflict: ConflictData;
  /** Whether the panel is open */
  open: boolean;
  /** Callback when panel is closed */
  onClose: () => void;
  /** Callback when conflict is resolved */
  onResolved?: (conflictId: string, resolutionData: ResolutionData) => void;
  /** Callback when conflict is overridden */
  onOverride?: (conflictId: string, justification: string) => void;
  /** Whether resolution is in progress */
  isLoading?: boolean;
}

export interface ResolutionData {
  selectedDecisionId: string;
  resolutionNotes: string;
}

type ResolutionChoice = 'decision1' | 'decision2' | 'merge' | null;

/**
 * ConflictResolutionPanel - Modal for resolving conflicts between decisions
 * 
 * Shows side-by-side comparison of conflicting decisions with options to:
 * - Accept one decision over the other
 * - Merge the decisions
 * - Override the warning with justification
 */
export const ConflictResolutionPanel: React.FC<ConflictResolutionPanelProps> = ({
  conflict,
  open,
  onClose,
  onResolved,
  onOverride,
  isLoading = false,
}) => {
  const [selectedChoice, setSelectedChoice] = useState<ResolutionChoice>(null);
  const [resolutionNotes, setResolutionNotes] = useState('');
  const [overrideJustification, setOverrideJustification] = useState('');
  const [showOverrideSection, setShowOverrideSection] = useState(false);

  const handleResolve = () => {
    if (!selectedChoice || !onResolved) return;

    const selectedDecisionId = selectedChoice === 'decision1' 
      ? conflict.decision1.id 
      : selectedChoice === 'decision2'
        ? conflict.decision2.id
        : 'merged'; // For merge case, backend handles this

    onResolved(conflict.id, {
      selectedDecisionId,
      resolutionNotes: resolutionNotes.trim(),
    });
  };

  const handleOverride = () => {
    if (!overrideJustification.trim() || !onOverride) return;
    onOverride(conflict.id, overrideJustification.trim());
  };

  const handleClose = () => {
    // Reset state on close
    setSelectedChoice(null);
    setResolutionNotes('');
    setOverrideJustification('');
    setShowOverrideSection(false);
    onClose();
  };

  const formatTimestamp = (date: Date): string => {
    return new Intl.DateTimeFormat('en-US', {
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    }).format(date);
  };

  return (
    <Modal
      title={
        <Space>
          <SwapOutlined />
          <span>Resolve Conflict</span>
        </Space>
      }
      open={open}
      onCancel={handleClose}
      width={900}
      className="conflict-resolution-modal"
      data-testid="conflict-resolution-panel"
      footer={
        <div className="modal-footer">
          <div className="footer-left">
            {!showOverrideSection && (
              <Button 
                type="link" 
                danger
                onClick={() => setShowOverrideSection(true)}
                data-testid="show-override-button"
              >
                Override Warning
              </Button>
            )}
          </div>
          <div className="footer-right">
            <Button onClick={handleClose} data-testid="cancel-button">
              Cancel
            </Button>
            {!showOverrideSection ? (
              <Button
                type="primary"
                icon={<CheckCircleOutlined />}
                onClick={handleResolve}
                disabled={!selectedChoice}
                loading={isLoading}
                data-testid="resolve-button"
              >
                Resolve Conflict
              </Button>
            ) : (
              <Button
                type="primary"
                danger
                icon={<WarningOutlined />}
                onClick={handleOverride}
                disabled={!overrideJustification.trim()}
                loading={isLoading}
                data-testid="confirm-override-button"
              >
                Confirm Override
              </Button>
            )}
          </div>
        </div>
      }
    >
      {/* Conflict Info Header */}
      <Alert
        type={conflict.severity === 'high' ? 'error' : conflict.severity === 'medium' ? 'warning' : 'info'}
        message={conflict.type}
        description={conflict.description}
        showIcon
        className="conflict-info-alert"
        data-testid="conflict-info"
      />

      {/* Decision Comparison */}
      <div className="comparison-section">
        <Title level={5}>Compare Decisions</Title>
        
        <DiffViewer
          originalContent={conflict.decision1.content}
          modifiedContent={conflict.decision2.content}
          originalLabel={`Decision 1: ${conflict.decision1.title}`}
          modifiedLabel={`Decision 2: ${conflict.decision2.title}`}
        />

        <div className="decision-metadata">
          <div className="decision-meta-item">
            <Text type="secondary">Decision 1 by {conflict.decision1.author}</Text>
            <Text type="secondary" className="meta-timestamp">
              {formatTimestamp(conflict.decision1.timestamp)}
            </Text>
          </div>
          <div className="decision-meta-item">
            <Text type="secondary">Decision 2 by {conflict.decision2.author}</Text>
            <Text type="secondary" className="meta-timestamp">
              {formatTimestamp(conflict.decision2.timestamp)}
            </Text>
          </div>
        </div>
      </div>

      <Divider />

      {/* Resolution Options - Show only if not in override mode */}
      {!showOverrideSection && (
        <div className="resolution-section" data-testid="resolution-options">
          <Title level={5}>Choose Resolution</Title>
          
          <Radio.Group 
            value={selectedChoice} 
            onChange={(e) => setSelectedChoice(e.target.value)}
            className="resolution-choices"
          >
            <Space direction="vertical" style={{ width: '100%' }}>
              <Radio value="decision1" data-testid="choice-decision1">
                <strong>Accept Decision 1</strong> - Keep "{conflict.decision1.title}"
              </Radio>
              <Radio value="decision2" data-testid="choice-decision2">
                <strong>Accept Decision 2</strong> - Keep "{conflict.decision2.title}"
              </Radio>
              <Radio value="merge" data-testid="choice-merge">
                <strong>Merge Decisions</strong> - Combine both decisions (manual review required)
              </Radio>
            </Space>
          </Radio.Group>

          <div className="resolution-notes">
            <Text strong>Resolution Notes (optional)</Text>
            <TextArea
              value={resolutionNotes}
              onChange={(e) => setResolutionNotes(e.target.value)}
              placeholder="Add any notes about why this resolution was chosen..."
              rows={3}
              data-testid="resolution-notes-input"
            />
          </div>
        </div>
      )}

      {/* Override Section */}
      {showOverrideSection && (
        <div className="override-section" data-testid="override-section">
          <Alert
            type="warning"
            message="Override Conflict Warning"
            description="By overriding this warning, you acknowledge the conflict but choose to proceed without resolution. This requires a justification."
            showIcon
          />
          
          <div className="override-justification">
            <Text strong>Justification (required)</Text>
            <TextArea
              value={overrideJustification}
              onChange={(e) => setOverrideJustification(e.target.value)}
              placeholder="Explain why you are overriding this conflict warning..."
              rows={4}
              data-testid="override-justification-input"
            />
          </div>

          <Button 
            type="link" 
            onClick={() => setShowOverrideSection(false)}
            data-testid="back-to-resolution-button"
          >
            ← Back to resolution options
          </Button>
        </div>
      )}
    </Modal>
  );
};
