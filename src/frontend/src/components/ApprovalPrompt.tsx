import React, { useState } from 'react';
import {
  Modal,
  Button,
  Alert,
  Space,
  Typography,
  Input,
  Spin,
  Tag,
  Row,
  Col,
  Progress,
} from 'antd';
import {
  CheckCircleOutlined,
  DeleteOutlined,
  EditOutlined,
  CloseCircleOutlined,
} from '@ant-design/icons';
import './ApprovalPrompt.css';

const { Text, Paragraph } = Typography;
const { TextArea } = Input;

export interface ApprovalRequestDto {
  id: string;
  workflowInstanceId: string;
  agentId: string;
  stepId?: string;
  proposedResponse: string;
  confidenceScore: number;
  reasoning?: string;
  status: string;
  requestedAt: string;
  resolvedAt?: string;
  resolvedBy?: string;
  modifiedResponse?: string;
  rejectionReason?: string;
}

export interface ApprovalPromptProps {
  approvalRequest: ApprovalRequestDto;
  onClose: () => void;
  onApproved?: () => void;
}

export const ApprovalPrompt: React.FC<ApprovalPromptProps> = ({
  approvalRequest,
  onClose,
  onApproved,
}) => {
  const [action, setAction] = useState<'approve' | 'modify' | 'reject' | null>(null);
  const [modifiedResponse, setModifiedResponse] = useState(approvalRequest.proposedResponse);
  const [rejectionReason, setRejectionReason] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const getConfidenceColor = (score: number): string => {
    if (score >= 0.9) return 'green';
    if (score >= 0.7) return 'orange';
    return 'red';
  };

  const getConfidenceStatus = (score: number): 'success' | 'normal' | 'exception' => {
    if (score >= 0.9) return 'success';
    if (score >= 0.7) return 'normal';
    return 'exception';
  };

  const handleApprove = async () => {
    setLoading(true);
    setError(null);
    try {
      const response = await fetch(
        `/api/v1/workflows/approvals/${approvalRequest.id}/approve`,
        {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
          },
        }
      );

      if (!response.ok) {
        const errorData = await response.json();
        throw new Error(errorData.detail || 'Failed to approve request');
      }

      onApproved?.();
      onClose();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unknown error occurred');
    } finally {
      setLoading(false);
    }
  };

  const handleModify = async () => {
    if (!modifiedResponse.trim()) {
      setError('Modified response cannot be empty');
      return;
    }

    setLoading(true);
    setError(null);
    try {
      const response = await fetch(
        `/api/v1/workflows/approvals/${approvalRequest.id}/modify`,
        {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
          },
          body: JSON.stringify({ modifiedResponse }),
        }
      );

      if (!response.ok) {
        const errorData = await response.json();
        throw new Error(errorData.detail || 'Failed to modify request');
      }

      onApproved?.();
      onClose();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unknown error occurred');
    } finally {
      setLoading(false);
    }
  };

  const handleReject = async () => {
    if (!rejectionReason.trim()) {
      setError('Rejection reason cannot be empty');
      return;
    }

    setLoading(true);
    setError(null);
    try {
      const response = await fetch(
        `/api/v1/workflows/approvals/${approvalRequest.id}/reject`,
        {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
          },
          body: JSON.stringify({ rejectionReason }),
        }
      );

      if (!response.ok) {
        const errorData = await response.json();
        throw new Error(errorData.detail || 'Failed to reject request');
      }

      onApproved?.();
      onClose();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unknown error occurred');
    } finally {
      setLoading(false);
    }
  };

  const confidencePercentage = Math.round(approvalRequest.confidenceScore * 100);
  const confidenceColor = getConfidenceColor(approvalRequest.confidenceScore);
  const confidenceStatus = getConfidenceStatus(approvalRequest.confidenceScore);

  return (
    <Modal
      title="Agent Approval Required"
      open={true}
      onCancel={onClose}
      width={700}
      closable={!loading}
      footer={null}
      className="approval-prompt-modal"
    >
      <Spin spinning={loading}>
        <Space direction="vertical" size="large" style={{ width: '100%' }}>
          {/* Error Alert */}
          {error && (
            <Alert
              message="Error"
              description={error}
              type="error"
              showIcon
              closable
              onClose={() => setError(null)}
            />
          )}

          {/* Agent Info */}
          <Alert
            message={`${approvalRequest.agentId} needs your approval`}
            description={`This agent has low confidence about its response and requires your review`}
            type="warning"
            showIcon
          />

          {/* Confidence Score */}
          <Row gutter={16}>
            <Col span={12}>
              <div className="confidence-section">
                <Text strong>Confidence Score</Text>
                <div className="confidence-score-display">
                  <Progress
                    type="circle"
                    percent={confidencePercentage}
                    status={confidenceStatus}
                    width={100}
                  />
                  <Text className="confidence-percentage">{confidencePercentage}%</Text>
                </div>
              </div>
            </Col>
            <Col span={12}>
              <div className="info-section">
                <Text strong>Request Info</Text>
                <div className="info-item">
                  <Text type="secondary">Step:</Text>
                  <Text>{approvalRequest.stepId || 'N/A'}</Text>
                </div>
                <div className="info-item">
                  <Text type="secondary">Status:</Text>
                  <Tag color={confidenceColor}>{approvalRequest.status}</Tag>
                </div>
              </div>
            </Col>
          </Row>

          {/* Reasoning */}
          {approvalRequest.reasoning && (
            <div className="reasoning-section">
              <Text strong>Agent's Reasoning</Text>
              <Paragraph className="reasoning-text">{approvalRequest.reasoning}</Paragraph>
            </div>
          )}

          {/* Proposed Response */}
          <div className="response-section">
            <Text strong>Proposed Response</Text>
            <TextArea
              rows={8}
              value={action === 'modify' ? modifiedResponse : approvalRequest.proposedResponse}
              disabled={action !== 'modify'}
              onChange={(e) => setModifiedResponse(e.target.value)}
              className="response-textarea"
              placeholder="Agent's proposed response will appear here"
            />
          </div>

          {/* Rejection Reason (if rejecting) */}
          {action === 'reject' && (
            <div className="rejection-section">
              <Text strong>Rejection Reason</Text>
              <TextArea
                rows={4}
                placeholder="Why are you rejecting this response? This will be used to guide the agent's regeneration."
                value={rejectionReason}
                onChange={(e) => setRejectionReason(e.target.value)}
                className="rejection-textarea"
              />
            </div>
          )}

          {/* Action Buttons */}
          <div className="action-buttons">
            {action === null ? (
              <Space>
                <Button
                  type="primary"
                  icon={<CheckCircleOutlined />}
                  onClick={handleApprove}
                  loading={loading}
                  className="approve-button"
                >
                  Approve
                </Button>
                <Button
                  icon={<EditOutlined />}
                  onClick={() => setAction('modify')}
                  disabled={loading}
                  className="modify-button"
                >
                  Modify
                </Button>
                <Button
                  danger
                  icon={<CloseCircleOutlined />}
                  onClick={() => setAction('reject')}
                  disabled={loading}
                  className="reject-button"
                >
                  Reject
                </Button>
              </Space>
            ) : action === 'modify' ? (
              <Space>
                <Button
                  type="primary"
                  icon={<CheckCircleOutlined />}
                  onClick={handleModify}
                  loading={loading}
                >
                  Confirm Modification
                </Button>
                <Button onClick={() => setAction(null)} disabled={loading}>
                  Cancel
                </Button>
              </Space>
            ) : action === 'reject' ? (
              <Space>
                <Button
                  danger
                  type="primary"
                  icon={<DeleteOutlined />}
                  onClick={handleReject}
                  loading={loading}
                >
                  Confirm Rejection
                </Button>
                <Button onClick={() => setAction(null)} disabled={loading}>
                  Cancel
                </Button>
              </Space>
            ) : null}
          </div>
        </Space>
      </Spin>
    </Modal>
  );
};
