import { useCallback, useEffect, useState } from 'react';
import {
  Timeline,
  Skeleton,
  Empty,
  Button,
  Modal,
  Form,
  Input,
  Typography,
  Space,
  Tag,
  Tooltip,
  message,
} from 'antd';
import {
  SaveOutlined,
  HistoryOutlined,
  UndoOutlined,
  ExclamationCircleFilled,
} from '@ant-design/icons';
import './CheckpointList.css';

const { Text, Title } = Typography;
const { TextArea } = Input;

export interface CheckpointResponse {
  Id: string;
  WorkflowId: string;
  StepId: string;
  CheckpointType: string;
  Version: number;
  CreatedAt: string;
  TriggeredBy: string;
  Metadata?: Record<string, unknown> | null;
  Name?: string;
  Description?: string;
}

export interface CheckpointListProps {
  workflowId: string;
  checkpoints?: CheckpointResponse[];
  fetchCheckpoints?: () => Promise<CheckpointResponse[]>;
  onRestore?: (checkpointId: string) => Promise<void>;
  onCreateCheckpoint?: (name: string, description?: string) => Promise<void>;
  loading?: boolean;
}

export function CheckpointList({
  workflowId,
  checkpoints: initialCheckpoints,
  fetchCheckpoints,
  onRestore,
  onCreateCheckpoint,
  loading: externalLoading,
}: CheckpointListProps): JSX.Element {
  const [checkpoints, setCheckpoints] = useState<CheckpointResponse[]>(initialCheckpoints || []);
  const [loading, setLoading] = useState<boolean>(!initialCheckpoints && !!fetchCheckpoints);
  const [error, setError] = useState<string | null>(null);
  const [createModalOpen, setCreateModalOpen] = useState<boolean>(false);
  const [restoring, setRestoring] = useState<string | null>(null);
  const [creating, setCreating] = useState<boolean>(false);
  const [form] = Form.useForm();

  const loadCheckpoints = useCallback(async () => {
    if (!fetchCheckpoints) return;
    
    setLoading(true);
    setError(null);
    
    try {
      const data = await fetchCheckpoints();
      setCheckpoints(data);
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to load checkpoints';
      setError(errorMessage);
    } finally {
      setLoading(false);
    }
  }, [fetchCheckpoints]);

  useEffect(() => {
    if (!initialCheckpoints && fetchCheckpoints) {
      loadCheckpoints();
    }
  }, [initialCheckpoints, fetchCheckpoints, loadCheckpoints]);

  useEffect(() => {
    if (initialCheckpoints) {
      setCheckpoints(initialCheckpoints);
    }
  }, [initialCheckpoints]);

  const handleRestore = useCallback(async (checkpointId: string) => {
    if (!onRestore) return;

    Modal.confirm({
      title: 'Restore to Checkpoint',
      icon: <ExclamationCircleFilled />,
      content: 'Are you sure you want to restore the workflow to this checkpoint? This action cannot be undone.',
      okText: 'Restore',
      okType: 'danger',
      cancelText: 'Cancel',
      onOk: async () => {
        setRestoring(checkpointId);
        try {
          await onRestore(checkpointId);
          message.success('Workflow restored successfully');
        } catch (err) {
          const errorMessage = err instanceof Error ? err.message : 'Failed to restore checkpoint';
          message.error(errorMessage);
        } finally {
          setRestoring(null);
        }
      },
    });
  }, [onRestore]);

  const handleCreateCheckpoint = useCallback(async (values: { name: string; description?: string }) => {
    if (!onCreateCheckpoint) return;

    setCreating(true);
    try {
      await onCreateCheckpoint(values.name, values.description);
      message.success('Checkpoint created successfully');
      setCreateModalOpen(false);
      form.resetFields();
      // Reload checkpoints if we have a fetch function
      if (fetchCheckpoints) {
        await loadCheckpoints();
      }
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to create checkpoint';
      message.error(errorMessage);
    } finally {
      setCreating(false);
    }
  }, [onCreateCheckpoint, form, fetchCheckpoints, loadCheckpoints]);

  const formatDate = (dateString: string): string => {
    const date = new Date(dateString);
    return date.toLocaleString();
  };

  const getCheckpointTypeColor = (type: string): string => {
    switch (type.toLowerCase()) {
      case 'explicitsave':
        return 'blue';
      case 'automaticsave':
        return 'green';
      case 'phaseboundary':
        return 'purple';
      default:
        return 'default';
    }
  };

  const getCheckpointTypeLabel = (type: string): string => {
    switch (type.toLowerCase()) {
      case 'explicitsave':
        return 'Manual Save';
      case 'automaticsave':
        return 'Auto Save';
      case 'phaseboundary':
        return 'Phase Boundary';
      default:
        return type;
    }
  };

  const isLoading = externalLoading ?? loading;

  if (isLoading) {
    return (
      <div className="checkpoint-list" data-testid="checkpoint-list">
        <div className="checkpoint-list-header">
          <Title level={4}>
            <HistoryOutlined /> Checkpoints
          </Title>
        </div>
        <Skeleton active paragraph={{ rows: 4 }} />
      </div>
    );
  }

  if (error) {
    return (
      <div className="checkpoint-list" data-testid="checkpoint-list">
        <div className="checkpoint-list-header">
          <Title level={4}>
            <HistoryOutlined /> Checkpoints
          </Title>
        </div>
        <Empty
          description={
            <Space direction="vertical">
              <Text type="danger">{error}</Text>
              <Button onClick={loadCheckpoints}>Retry</Button>
            </Space>
          }
        />
      </div>
    );
  }

  return (
    <div className="checkpoint-list" data-testid="checkpoint-list">
      <div className="checkpoint-list-header">
        <Title level={4}>
          <HistoryOutlined /> Checkpoints
        </Title>
        {onCreateCheckpoint && (
          <Button
            type="primary"
            icon={<SaveOutlined />}
            onClick={() => setCreateModalOpen(true)}
            data-testid="create-checkpoint-button"
          >
            Create Checkpoint
          </Button>
        )}
      </div>

      {checkpoints.length === 0 ? (
        <Empty 
          description="No checkpoints saved" 
          data-testid="empty-checkpoints"
        />
      ) : (
        <Timeline
          className="checkpoint-timeline"
          data-testid="checkpoint-timeline"
          items={checkpoints.map((checkpoint) => ({
            key: checkpoint.Id,
            dot: <HistoryOutlined />,
            children: (
              <div className="checkpoint-item" data-testid={`checkpoint-${checkpoint.Id}`}>
                <div className="checkpoint-item-header">
                  <Space>
                    <Text strong>
                      {checkpoint.Name || `Checkpoint v${checkpoint.Version}`}
                    </Text>
                    <Tag color={getCheckpointTypeColor(checkpoint.CheckpointType)}>
                      {getCheckpointTypeLabel(checkpoint.CheckpointType)}
                    </Tag>
                  </Space>
                  {onRestore && (
                    <Tooltip title="Restore to this checkpoint">
                      <Button
                        size="small"
                        icon={<UndoOutlined />}
                        loading={restoring === checkpoint.Id}
                        onClick={() => handleRestore(checkpoint.Id)}
                        data-testid={`restore-${checkpoint.Id}`}
                      >
                        Restore
                      </Button>
                    </Tooltip>
                  )}
                </div>
                <div className="checkpoint-item-meta">
                  <Text type="secondary">{formatDate(checkpoint.CreatedAt)}</Text>
                  <Text type="secondary"> • Step: {checkpoint.StepId}</Text>
                </div>
                {checkpoint.Description && (
                  <div className="checkpoint-item-description">
                    <Text>{checkpoint.Description}</Text>
                  </div>
                )}
              </div>
            ),
          }))}
        />
      )}

      <Modal
        title="Create Checkpoint"
        open={createModalOpen}
        onCancel={() => {
          setCreateModalOpen(false);
          form.resetFields();
        }}
        footer={null}
        data-testid="create-checkpoint-modal"
      >
        <Form
          form={form}
          layout="vertical"
          onFinish={handleCreateCheckpoint}
        >
          <Form.Item
            name="name"
            label="Checkpoint Name"
            rules={[{ required: true, message: 'Please enter a name for this checkpoint' }]}
          >
            <Input 
              placeholder="e.g., Before major refactor" 
              data-testid="checkpoint-name-input"
            />
          </Form.Item>
          <Form.Item
            name="description"
            label="Description (optional)"
          >
            <TextArea 
              rows={3} 
              placeholder="Describe what this checkpoint represents..."
              data-testid="checkpoint-description-input"
            />
          </Form.Item>
          <Form.Item>
            <Space>
              <Button type="primary" htmlType="submit" loading={creating}>
                Create Checkpoint
              </Button>
              <Button onClick={() => setCreateModalOpen(false)}>
                Cancel
              </Button>
            </Space>
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
}

export default CheckpointList;
