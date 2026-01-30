import React, { useState, useEffect, useCallback } from 'react';
import { Drawer, Timeline, Button, Skeleton, Empty, Modal, Typography, Space, Tag } from 'antd';
import { HistoryOutlined, RollbackOutlined, DiffOutlined } from '@ant-design/icons';
import './VersionHistoryPanel.css';

const { Text, Paragraph } = Typography;

/**
 * Represents a single version in the decision history
 */
export interface DecisionVersion {
  VersionNumber: number;
  ModifiedAt: Date | string;
  ModifiedBy: string;
  ChangeReason?: string;
  Changes?: Array<{ Field: string; OldValue: string; NewValue: string }>;
}

export interface VersionHistoryPanelProps {
  /** Decision ID to fetch history for */
  decisionId: string;
  /** Whether the drawer is open */
  open: boolean;
  /** Callback when drawer is closed */
  onClose: () => void;
  /** Callback when user wants to view diff between versions */
  onViewDiff?: (fromVersion: number, toVersion: number) => void;
  /** Callback when user reverts to a previous version */
  onRevert?: (versionNumber: number) => void;
  /** Current version number (defaults to latest) */
  currentVersion?: number;
  /** Maximum versions to show before paginating (default 50) */
  maxVersionsToShow?: number;
  /** Optional: Pre-fetched versions for testing */
  versions?: DecisionVersion[];
  /** Whether data is loading */
  loading?: boolean;
  /** Error message if fetch failed */
  error?: string | null;
}

/**
 * VersionHistoryPanel - Drawer showing decision version history
 * 
 * Displays a timeline of versions with ability to view diffs and revert.
 */
export const VersionHistoryPanel: React.FC<VersionHistoryPanelProps> = ({
  decisionId,
  open,
  onClose,
  onViewDiff,
  onRevert,
  currentVersion,
  maxVersionsToShow = 50,
  versions: propVersions,
  loading: propLoading,
  error: propError,
}) => {
  const [versions, setVersions] = useState<DecisionVersion[]>(propVersions || []);
  const [loading, setLoading] = useState(propLoading ?? true);
  const [error, setError] = useState<string | null>(propError ?? null);
  const [visibleCount, setVisibleCount] = useState(maxVersionsToShow);
  const [revertingVersion, setRevertingVersion] = useState<number | null>(null);

  // Fetch versions when panel opens
  useEffect(() => {
    if (!open || propVersions) return;

    const fetchVersions = async () => {
      setLoading(true);
      setError(null);
      
      try {
        const apiUrl = '';
        const response = await fetch(`${apiUrl}/api/v1/decisions/${decisionId}/versions`);
        
        if (!response.ok) {
          throw new Error(`Failed to fetch versions: ${response.statusText}`);
        }
        
        const data = await response.json();
        setVersions(data);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to fetch version history');
      } finally {
        setLoading(false);
      }
    };

    fetchVersions();
  }, [open, decisionId, propVersions]);

  // Update from props if provided
  useEffect(() => {
    if (propVersions) setVersions(propVersions);
    if (propLoading !== undefined) setLoading(propLoading);
    if (propError !== undefined) setError(propError);
  }, [propVersions, propLoading, propError]);

  // Handle revert action
  const handleRevert = useCallback(async (versionNumber: number) => {
    Modal.confirm({
      title: `Revert to version ${versionNumber}?`,
      content: 'This will create a new version based on the selected version. This action cannot be undone.',
      okText: 'Revert',
      okType: 'danger',
      cancelText: 'Cancel',
      async onOk() {
        setRevertingVersion(versionNumber);
        
        try {
          const apiUrl = '';
          const response = await fetch(`${apiUrl}/api/v1/decisions/${decisionId}/revert`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ versionNumber }),
          });
          
          if (!response.ok) {
            throw new Error('Failed to revert decision');
          }
          
          onRevert?.(versionNumber);
        } catch (err) {
          Modal.error({
            title: 'Revert Failed',
            content: err instanceof Error ? err.message : 'Failed to revert to selected version',
          });
        } finally {
          setRevertingVersion(null);
        }
      },
    });
  }, [decisionId, onRevert]);

  // Handle view diff action
  const handleViewDiff = useCallback((fromVersion: number) => {
    const toVersion = currentVersion ?? versions[0]?.VersionNumber ?? 1;
    onViewDiff?.(fromVersion, toVersion);
  }, [onViewDiff, currentVersion, versions]);

  // Load more versions
  const handleLoadMore = useCallback(() => {
    setVisibleCount(prev => prev + maxVersionsToShow);
  }, [maxVersionsToShow]);

  // Format date for display
  const formatDate = (date: Date | string): string => {
    const d = typeof date === 'string' ? new Date(date) : date;
    return d.toLocaleString();
  };

  // Get current version number
  const latestVersion = currentVersion ?? versions[0]?.VersionNumber ?? 1;

  // Visible versions (paginated)
  const visibleVersions = versions.slice(0, visibleCount);
  const hasMore = versions.length > visibleCount;

  return (
    <Drawer
      title={
        <Space>
          <HistoryOutlined />
          <span>Version History</span>
          {!loading && !error && (
            <Tag color="blue">{versions.length} versions</Tag>
          )}
        </Space>
      }
      placement="right"
      width={400}
      open={open}
      onClose={onClose}
      className="version-history-panel"
      data-testid="version-history-panel"
    >
      {loading ? (
        <div className="version-history-loading" data-testid="version-history-loading">
          <Skeleton active paragraph={{ rows: 2 }} />
          <Skeleton active paragraph={{ rows: 2 }} />
          <Skeleton active paragraph={{ rows: 2 }} />
        </div>
      ) : error ? (
        <Empty
          description={error}
          data-testid="version-history-error"
        />
      ) : versions.length === 0 ? (
        <Empty
          description="No version history available"
          data-testid="version-history-empty"
        />
      ) : (
        <div className="version-history-content">
          <Timeline
            className="version-timeline"
            items={visibleVersions.map((version) => ({
              key: version.VersionNumber,
              color: version.VersionNumber === latestVersion ? 'green' : 'blue',
              children: (
                <div 
                  className="version-item" 
                  data-testid={`version-item-${version.VersionNumber}`}
                >
                  <div className="version-header">
                    <Text strong>
                      v{version.VersionNumber}
                      {version.VersionNumber === latestVersion && (
                        <Tag color="green" className="current-tag">Current</Tag>
                      )}
                    </Text>
                  </div>
                  <div className="version-meta">
                    <Text type="secondary">
                      {formatDate(version.ModifiedAt)} by {version.ModifiedBy}
                    </Text>
                  </div>
                  {version.ChangeReason && (
                    <Paragraph 
                      className="version-reason" 
                      ellipsis={{ rows: 2, expandable: true }}
                    >
                      {version.ChangeReason}
                    </Paragraph>
                  )}
                  {version.VersionNumber !== latestVersion && (
                    <Space className="version-actions">
                      {onViewDiff && (
                        <Button
                          type="link"
                          size="small"
                          icon={<DiffOutlined />}
                          onClick={() => handleViewDiff(version.VersionNumber)}
                          data-testid={`view-diff-${version.VersionNumber}`}
                        >
                          View Diff
                        </Button>
                      )}
                      {onRevert && (
                        <Button
                          type="link"
                          size="small"
                          icon={<RollbackOutlined />}
                          onClick={() => handleRevert(version.VersionNumber)}
                          loading={revertingVersion === version.VersionNumber}
                          data-testid={`revert-${version.VersionNumber}`}
                        >
                          Revert
                        </Button>
                      )}
                    </Space>
                  )}
                </div>
              ),
            }))}
          />
          {hasMore && (
            <div className="load-more-container">
              <Button 
                onClick={handleLoadMore}
                data-testid="load-more-button"
              >
                Load more ({versions.length - visibleCount} remaining)
              </Button>
            </div>
          )}
        </div>
      )}
    </Drawer>
  );
};
