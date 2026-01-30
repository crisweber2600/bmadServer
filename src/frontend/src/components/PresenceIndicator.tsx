import React from 'react';
import { Tooltip, Typography } from 'antd';
import './PresenceIndicator.css';

const { Text } = Typography;

export interface PresenceUser {
  name: string;
  id?: string;
}

export interface PresenceIndicatorProps {
  /** Whether the user(s) are online */
  isOnline: boolean;
  /** List of online users for tooltip display */
  users?: PresenceUser[];
  /** Whether to show tooltip with user names */
  showTooltip?: boolean;
  /** Size of the indicator dot */
  size?: 'small' | 'medium' | 'large';
  /** Optional custom label */
  label?: string;
  /** Whether to show the pulse animation when online */
  showPulse?: boolean;
}

/**
 * Online/offline presence indicator dot
 * 
 * Shows a colored dot indicating online (green, pulsing) or offline (gray) status.
 * Can display a tooltip with list of online user names.
 * 
 * @example
 * ```tsx
 * <PresenceIndicator
 *   isOnline={true}
 *   users={[{ name: 'Alice' }, { name: 'Bob' }]}
 *   showTooltip={true}
 * />
 * ```
 */
export const PresenceIndicator: React.FC<PresenceIndicatorProps> = ({
  isOnline,
  users = [],
  showTooltip = true,
  size = 'medium',
  label,
  showPulse = true,
}) => {
  const getSizeClass = () => {
    switch (size) {
      case 'small': return 'size-small';
      case 'large': return 'size-large';
      default: return 'size-medium';
    }
  };

  const getTooltipContent = () => {
    if (!showTooltip) return null;

    if (!isOnline || users.length === 0) {
      return 'Offline';
    }

    if (users.length === 1) {
      return `${users[0].name} online`;
    }

    if (users.length <= 3) {
      const names = users.map(u => u.name).join(', ');
      return `${names} online`;
    }

    // More than 3 users
    const firstThree = users.slice(0, 3).map(u => u.name).join(', ');
    return `${firstThree} and ${users.length - 3} more online`;
  };

  const indicator = (
    <div 
      className={`presence-indicator ${getSizeClass()}`}
      role="status"
      aria-label={isOnline ? 'Online' : 'Offline'}
      data-testid="presence-indicator"
    >
      <span 
        className={`presence-dot ${isOnline ? 'online' : 'offline'} ${showPulse && isOnline ? 'pulse' : ''}`}
        aria-hidden="true"
      />
      {label && (
        <Text className={`presence-label ${isOnline ? 'online' : 'offline'}`}>
          {label}
        </Text>
      )}
    </div>
  );

  if (showTooltip) {
    return (
      <Tooltip title={getTooltipContent()} placement="top">
        {indicator}
      </Tooltip>
    );
  }

  return indicator;
};

export default PresenceIndicator;
