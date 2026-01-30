import React from 'react';
import { Alert, Button, Space, Tag } from 'antd';
import { WarningOutlined, ExclamationCircleOutlined, InfoCircleOutlined } from '@ant-design/icons';
import './ConflictAlert.css';

/**
 * Severity levels for conflicts
 */
export type ConflictSeverity = 'high' | 'medium' | 'low' | 'info';

export interface ConflictAlertProps {
  /** Unique conflict identifier */
  conflictId: string;
  /** Severity of the conflict */
  severity: ConflictSeverity;
  /** Type of conflict (e.g., "Contradicting decisions") */
  conflictType: string;
  /** Optional description of the conflict */
  description?: string;
  /** Callback when user clicks resolve */
  onResolve?: (conflictId: string) => void;
  /** Callback when user dismisses the alert */
  onDismiss?: () => void;
  /** Whether to show the resolve button */
  showResolve?: boolean;
  /** Whether to show the dismiss button */
  showDismiss?: boolean;
}

/**
 * ConflictAlert - Banner showing conflict warnings
 * 
 * Displays conflicts with severity-based styling and resolution actions.
 */
export const ConflictAlert: React.FC<ConflictAlertProps> = ({
  conflictId,
  severity,
  conflictType,
  description,
  onResolve,
  onDismiss,
  showResolve = true,
  showDismiss = true,
}) => {
  // Map severity to alert type
  const getAlertType = (): 'error' | 'warning' | 'info' => {
    switch (severity) {
      case 'high':
        return 'error';
      case 'medium':
        return 'warning';
      case 'low':
      case 'info':
      default:
        return 'info';
    }
  };

  // Get severity icon
  const getSeverityIcon = (): React.ReactNode => {
    switch (severity) {
      case 'high':
        return <WarningOutlined />;
      case 'medium':
        return <ExclamationCircleOutlined />;
      case 'low':
      case 'info':
      default:
        return <InfoCircleOutlined />;
    }
  };

  // Get severity tag color
  const getSeverityColor = (): string => {
    switch (severity) {
      case 'high':
        return 'red';
      case 'medium':
        return 'orange';
      case 'low':
        return 'blue';
      case 'info':
      default:
        return 'default';
    }
  };

  // Format severity for display
  const formatSeverity = (s: ConflictSeverity): string => {
    return s.charAt(0).toUpperCase() + s.slice(1);
  };

  return (
    <Alert
      type={getAlertType()}
      className={`conflict-alert severity-${severity}`}
      data-testid="conflict-alert"
      icon={getSeverityIcon()}
      message={
        <span className="conflict-message" data-testid="conflict-message">
          <Tag color={getSeverityColor()} className="severity-tag">
            {formatSeverity(severity)} Severity
          </Tag>
          {conflictType}
        </span>
      }
      description={description}
      action={
        <Space>
          {showResolve && onResolve && (
            <Button
              type="primary"
              size="small"
              onClick={() => onResolve(conflictId)}
              data-testid="resolve-button"
            >
              Resolve
            </Button>
          )}
          {showDismiss && onDismiss && (
            <Button
              size="small"
              onClick={onDismiss}
              data-testid="dismiss-button"
            >
              Dismiss
            </Button>
          )}
        </Space>
      }
      showIcon
      closable={false}
    />
  );
};
