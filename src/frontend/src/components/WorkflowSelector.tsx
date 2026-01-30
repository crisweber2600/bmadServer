import React from 'react';
import { Select, Button, Space, Card, Typography } from 'antd';
import { PlayCircleOutlined, SettingOutlined } from '@ant-design/icons';
import type { WorkflowDefinition } from '../types/workflow';

const { Option } = Select;
const { Text } = Typography;

export interface WorkflowSelectorProps {
  definitions: WorkflowDefinition[];
  selectedWorkflowId?: string;
  hasActiveWorkflow: boolean;
  isLoading: boolean;
  onSelect: (id: string) => void;
  onStart: (id: string) => void;
  disabled: boolean;
}

export const WorkflowSelector: React.FC<WorkflowSelectorProps> = ({
  definitions,
  selectedWorkflowId,
  hasActiveWorkflow,
  isLoading,
  onSelect,
  onStart,
  disabled
}) => {
  return (
    <Card size="small" className="workflow-selector-card" bordered={false}>
      <Space className="workflow-selector-container" style={{ width: '100%', justifyContent: 'space-between' }}>
        <Space>
          <SettingOutlined />
          <Text strong>Workflow:</Text>
          <Select
            placeholder="Select a workflow"
            style={{ width: 250 }}
            value={selectedWorkflowId}
            onChange={onSelect}
            loading={isLoading}
            disabled={disabled || hasActiveWorkflow}
          >
            {definitions.map(def => (
              <Option key={def.workflowId || def.id} value={def.workflowId || def.id}>{def.name}</Option>
            ))}
          </Select>
        </Space>
        
        <Button 
          type="primary" 
          icon={<PlayCircleOutlined />}
          onClick={() => selectedWorkflowId && onStart(selectedWorkflowId)}
          disabled={disabled || !selectedWorkflowId || hasActiveWorkflow}
          loading={isLoading && !hasActiveWorkflow}
        >
          Start Workflow
        </Button>
      </Space>
    </Card>
  );
};
