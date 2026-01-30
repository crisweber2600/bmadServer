import React, { useState, useCallback } from 'react';
import { Button, Typography } from 'antd';
import { WarningOutlined, InfoCircleOutlined, CheckOutlined } from '@ant-design/icons';
import './ModeratorAlert.css';

const { Text } = Typography;

export type AlertSeverity = 'info' | 'warning' | 'error';

export interface ModeratorAlertProps {
  /** Alert message to display */
  message: string;
  /** Severity level affects styling */
  severity?: AlertSeverity;
  /** Callback when user acknowledges the alert */
  onAcknowledge?: () => void;
  /** Custom action button text */
  acknowledgeText?: string;
  /** Whether the alert can be dismissed */
  dismissible?: boolean;
  /** Optional additional details */
  details?: string;
}

/**
 * Moderator alert banner for workflow warnings
 * 
 * Displays warnings about circular discussions, long-running operations,
 * or other moderator-level concerns that require user acknowledgment.
 * 
 * @example
 * ```tsx
 * <ModeratorAlert
 *   message="Discussion appears to be going in circles"
 *   severity="warning"
 *   onAcknowledge={() => setShowAlert(false)}
 * />
 * ```
 */
export const ModeratorAlert: React.FC<ModeratorAlertProps> = ({
  message,
  severity = 'warning',
  onAcknowledge,
  acknowledgeText = 'Acknowledge',
  dismissible = true,
  details,
}) => {
  const [isVisible, setIsVisible] = useState(true);
  const [isDismissing, setIsDismissing] = useState(false);

  const handleAcknowledge = useCallback(() => {
    setIsDismissing(true);
    // Wait for fade-out animation
    setTimeout(() => {
      setIsVisible(false);
      onAcknowledge?.();
    }, 300);
  }, [onAcknowledge]);

  if (!isVisible) {
    return null;
  }

  const getIcon = () => {
    switch (severity) {
      case 'error':
        return <WarningOutlined className="alert-icon" />;
      case 'info':
        return <InfoCircleOutlined className="alert-icon" />;
      case 'warning':
      default:
        return <WarningOutlined className="alert-icon" />;
    }
  };

  return (
    <div
      className={`moderator-alert moderator-alert-${severity} ${isDismissing ? 'dismissing' : ''}`}
      role="alert"
      aria-live="assertive"
      data-testid="moderator-alert"
    >
      <div className="alert-content">
        {getIcon()}
        <div className="alert-text">
          <Text strong className="alert-message">{message}</Text>
          {details && (
            <Text type="secondary" className="alert-details">{details}</Text>
          )}
        </div>
      </div>
      
      {dismissible && (
        <Button
          type="primary"
          size="small"
          icon={<CheckOutlined />}
          onClick={handleAcknowledge}
          className="alert-button"
          data-testid="moderator-alert-acknowledge"
        >
          {acknowledgeText}
        </Button>
      )}
    </div>
  );
};

export default ModeratorAlert;
