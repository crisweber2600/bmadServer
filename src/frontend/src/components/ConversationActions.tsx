import React from 'react';
import { Button, Space, Tooltip } from 'antd';
import { 
  ArrowRightOutlined, 
  CheckCircleOutlined, 
  QuestionCircleOutlined, 
  StopOutlined 
} from '@ant-design/icons';
import type { ConversationAction } from '../types/workflow';

export interface ConversationActionsProps {
  onAction: (message: string) => void;
  isProcessing: boolean;
  hasActiveWorkflow: boolean;
  disabled: boolean;
}

export const ConversationActions: React.FC<ConversationActionsProps> = ({
  onAction,
  isProcessing,
  hasActiveWorkflow: _hasActiveWorkflow,
  disabled
}) => {
  const actions: ConversationAction[] = [
    { label: 'Next Step', action: 'Proceed to the next step', icon: 'next' },
    { label: 'Summarize', action: 'Please summarize our conversation so far', icon: 'summary' },
    { label: 'Clarify', action: 'I need clarification on the last point', icon: 'question' },
    { label: 'Finish', action: 'I would like to conclude this session', icon: 'stop' }
  ];

  const getIcon = (icon?: string) => {
    switch(icon) {
      case 'next': return <ArrowRightOutlined />;
      case 'summary': return <CheckCircleOutlined />;
      case 'question': return <QuestionCircleOutlined />;
      case 'stop': return <StopOutlined />;
      default: return undefined;
    }
  };

  return (
    <Space className="conversation-actions" size="small" wrap>
      {actions.map((action, index) => (
        <Tooltip title={action.description || action.action} key={index}>
          <Button
            size="small"
            icon={getIcon(action.icon)}
            onClick={() => onAction(action.action)}
            disabled={disabled || isProcessing}
          >
            {action.label}
          </Button>
        </Tooltip>
      ))}
    </Space>
  );
};
