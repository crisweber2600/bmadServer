import React, { useState, useEffect } from 'react';
import { Table, DatePicker, Button, Space, Empty, Spin, Alert } from 'antd';
import { DownloadOutlined } from '@ant-design/icons';
import type { ColumnsType } from 'antd/es/table';
import dayjs from 'dayjs';
import './WorkflowHandoffLog.css';

interface AgentHandoffRecord {
  id: string;
  fromAgentId: string;
  fromAgentName: string;
  toAgentId: string;
  toAgentName: string;
  stepName?: string;
  reason?: string;
  timestamp: Date;
}

interface WorkflowHandoffLogProps {
  workflowId: string;
  apiBaseUrl?: string;
  onExport?: (data: AgentHandoffRecord[]) => void;
}

export const WorkflowHandoffLog: React.FC<WorkflowHandoffLogProps> = ({
  workflowId,
  apiBaseUrl = 'http://localhost:8080',
  onExport,
}) => {
  const [handoffs, setHandoffs] = useState<AgentHandoffRecord[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [fromDate, setFromDate] = useState<dayjs.Dayjs | null>(null);
  const [toDate, setToDate] = useState<dayjs.Dayjs | null>(null);
  const [selectedAgent, setSelectedAgent] = useState<string | null>(null);
  const [pageSize, setPageSize] = useState(10);
  const [currentPage, setCurrentPage] = useState(1);

  const fetchHandoffs = async () => {
    setLoading(true);
    setError(null);

    try {
      const params = new URLSearchParams();
      params.append('page', currentPage.toString());
      params.append('pageSize', pageSize.toString());

      if (fromDate) {
        params.append('fromDate', fromDate.toISOString());
      }
      if (toDate) {
        params.append('toDate', toDate.toISOString());
      }

      const token = localStorage.getItem('accessToken');
      const response = await fetch(
        `${apiBaseUrl}/api/v1/workflows/${workflowId}/handoffs?${params.toString()}`,
        {
          headers: {
            Authorization: `Bearer ${token}`,
          },
        }
      );

      if (!response.ok) {
        throw new Error(`Failed to fetch handoffs: ${response.statusText}`);
      }

      const data = await response.json();
      const records = data.items?.map((item: any) => ({
        id: `${item.id}`,
        fromAgentId: item.fromAgentId,
        fromAgentName: item.fromAgentName,
        toAgentId: item.toAgentId,
        toAgentName: item.toAgentName,
        stepName: item.stepName,
        reason: item.reason,
        timestamp: new Date(item.timestamp),
      })) || [];

      setHandoffs(records);
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Unknown error';
      setError(errorMessage);
      setHandoffs([]);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (workflowId) {
      fetchHandoffs();
    }
  }, [workflowId, fromDate, toDate, currentPage, pageSize]);

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
    return new Intl.DateTimeFormat('en-US', {
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit',
      hour12: true,
    }).format(date);
  };

  const handleExportCSV = () => {
    const csv = [
      ['From Agent', 'To Agent', 'Step', 'Reason', 'Timestamp'],
      ...handoffs.map((h) => [
        h.fromAgentName,
        h.toAgentName,
        h.stepName || '-',
        h.reason || '-',
        formatTimestamp(h.timestamp),
      ]),
    ]
      .map((row) => row.map((cell) => `"${cell}"`).join(','))
      .join('\n');

    const blob = new Blob([csv], { type: 'text/csv' });
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `workflow-${workflowId}-handoffs.csv`;
    document.body.appendChild(a);
    a.click();
    window.URL.revokeObjectURL(url);
    document.body.removeChild(a);
  };

  const handleExportJSON = () => {
    const json = JSON.stringify(handoffs, null, 2);
    const blob = new Blob([json], { type: 'application/json' });
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `workflow-${workflowId}-handoffs.json`;
    document.body.appendChild(a);
    a.click();
    window.URL.revokeObjectURL(url);
    document.body.removeChild(a);
  };

  const columns: ColumnsType<AgentHandoffRecord> = [
    {
      title: 'From Agent',
      dataIndex: 'fromAgentName',
      key: 'fromAgentName',
      width: 150,
      render: (text: string, record: AgentHandoffRecord) => (
        <span
          style={{
            display: 'inline-flex',
            alignItems: 'center',
            gap: '6px',
          }}
        >
          <div
            style={{
              width: '12px',
              height: '12px',
              borderRadius: '50%',
              backgroundColor: getColorHash(record.fromAgentId),
            }}
          />
          {text}
        </span>
      ),
    },
    {
      title: 'To Agent',
      dataIndex: 'toAgentName',
      key: 'toAgentName',
      width: 150,
      render: (text: string, record: AgentHandoffRecord) => (
        <span
          style={{
            display: 'inline-flex',
            alignItems: 'center',
            gap: '6px',
          }}
        >
          <div
            style={{
              width: '12px',
              height: '12px',
              borderRadius: '50%',
              backgroundColor: getColorHash(record.toAgentId),
            }}
          />
          {text}
        </span>
      ),
    },
    {
      title: 'Step',
      dataIndex: 'stepName',
      key: 'stepName',
      width: 120,
      render: (text?: string) => text ? <code>{text}</code> : '-',
    },
    {
      title: 'Reason',
      dataIndex: 'reason',
      key: 'reason',
      width: 200,
      render: (text?: string) => text || '-',
      ellipsis: true,
    },
    {
      title: 'Timestamp',
      dataIndex: 'timestamp',
      key: 'timestamp',
      width: 180,
      render: (date: Date) => formatTimestamp(date),
      sorter: (a: AgentHandoffRecord, b: AgentHandoffRecord) =>
        a.timestamp.getTime() - b.timestamp.getTime(),
    },
  ];

  return (
    <div className="workflow-handoff-log">
      <div className="handoff-log-header">
        <h2>Agent Handoff Timeline</h2>
        <p className="handoff-log-description">
          View all agent transitions for this workflow
        </p>
      </div>

      {error && (
        <Alert
          message="Error Loading Handoffs"
          description={error}
          type="error"
          showIcon
          closable
          style={{ marginBottom: '16px' }}
        />
      )}

      <div className="handoff-log-filters">
        <Space wrap align="center">
          <div className="date-filter">
            <label>From Date:</label>
            <DatePicker
              value={fromDate}
              onChange={(date) => {
                setFromDate(date);
                setCurrentPage(1);
              }}
              placeholder="Select start date"
            />
          </div>

          <div className="date-filter">
            <label>To Date:</label>
            <DatePicker
              value={toDate}
              onChange={(date) => {
                setToDate(date);
                setCurrentPage(1);
              }}
              placeholder="Select end date"
            />
          </div>

          <Button
            onClick={() => {
              setFromDate(null);
              setToDate(null);
              setSelectedAgent(null);
              setCurrentPage(1);
            }}
          >
            Clear Filters
          </Button>
        </Space>
      </div>

      <div className="handoff-log-export">
        <Space>
          <span>Export:</span>
          <Button
            icon={<DownloadOutlined />}
            onClick={handleExportCSV}
            disabled={handoffs.length === 0}
          >
            CSV
          </Button>
          <Button
            icon={<DownloadOutlined />}
            onClick={handleExportJSON}
            disabled={handoffs.length === 0}
          >
            JSON
          </Button>
        </Space>
      </div>

      {loading ? (
        <div className="handoff-log-loading">
          <Spin tip="Loading handoffs..." />
        </div>
      ) : handoffs.length === 0 ? (
        <Empty
          description="No handoffs found"
          style={{ marginTop: '32px' }}
        />
      ) : (
        <Table
          columns={columns}
          dataSource={handoffs}
          rowKey="id"
          loading={loading}
          pagination={{
            current: currentPage,
            pageSize: pageSize,
            showSizeChanger: true,
            showTotal: (total) => `Total ${total} handoffs`,
          }}
          onChange={(pagination) => {
            setCurrentPage(pagination.current || 1);
            setPageSize(pagination.pageSize || 10);
          }}
        />
      )}
    </div>
  );
};
