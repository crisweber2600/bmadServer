import React from 'react';
import { Space, Button, Typography, Card, Tag } from 'antd';
import { PauseCircleOutlined, PlayCircleOutlined, StopOutlined } from '@ant-design/icons';
import type { WorkflowState } from '../types/workflow';

const { Text } = Typography;

export interface WorkflowStatusBarProps {
  status: WorkflowState;
  isLoading: boolean;
  onPause: () => void;
  onResume: () => void;
  onCancel: () => void;
  disabled: boolean;
}

export const WorkflowStatusBar: React.FC<WorkflowStatusBarProps> = ({
  status,
  isLoading,
  onPause,
  onResume,
  onCancel,
  disabled
}) => {
  const getStatusColor = (s: string) => {
    switch (s) {
      case 'running': return 'processing';
      case 'paused': return 'warning';
      case 'completed': return 'success';
      case 'failed': return 'error';
      case 'cancelled': return 'default';
      default: return 'default';
    }
  };

  return (
    <Card size="small" className="workflow-status-bar" style={{ marginTop: 8 }} type="inner">
      <Space style={{ width: '100%', justifyContent: 'space-between' }}>
        <Space>
          <Text strong>Active Workflow:</Text>
          <Tag color={getStatusColor(status.status)}>{status.status.toUpperCase()}</Tag>
          {status.currentStep && <Text type="secondary">Step: {status.currentStep}</Text>}
        </Space>

        <Space>
          {status.status === 'running' && (
            <Button
              size="small"
              icon={<PauseCircleOutlined />}
              onClick={onPause}
              disabled={disabled || isLoading}
            >
              Pause
            </Button>
          )}

          {status.status === 'paused' && (
            <Button
              size="small"
              icon={<PlayCircleOutlined />}
              onClick={onResume}
              disabled={disabled || isLoading}
            >
              Resume
            </Button>
          )}

          <Button
            size="small"
            danger
            icon={<StopOutlined />}
            onClick={onCancel}
            disabled={disabled || isLoading || ['completed', 'failed', 'cancelled'].includes(status.status)}
          >
            Stop
          </Button>
        </Space>
      </Space>
    </Card>
  );
};
