import React, { useState, useEffect, useRef, useCallback } from 'react';
import { Typography, Button } from 'antd';
import {
  DisconnectOutlined,
  SyncOutlined,
  CheckCircleOutlined,
  ReloadOutlined,
} from '@ant-design/icons';
import './ConnectionStatusBanner.css';

const { Text } = Typography;

export type ConnectionState = 'connected' | 'disconnected' | 'reconnecting';

export interface ConnectionStatusBannerProps {
  /** Current connection state */
  connectionState: ConnectionState;
  /** Current reconnection attempt number */
  attemptNumber?: number;
  /** Maximum reconnection attempts before giving up */
  maxAttempts?: number;
  /** How long to show "Reconnected" message in ms */
  reconnectedDisplayMs?: number;
  /** Callback for manual retry button */
  onRetryClick?: () => void;
  /** Whether to show manual retry button when disconnected */
  showRetryButton?: boolean;
}

type DisplayState = 'hidden' | 'disconnected' | 'reconnecting' | 'reconnected';

/**
 * Connection status banner with state machine
 * 
 * States:
 * - connected: hidden (height: 0)
 * - disconnected: amber warning
 * - reconnecting: yellow with attempt count
 * - reconnected: green success (auto-hides after 2s)
 * 
 * Includes debounce to prevent flicker on rapid state changes.
 * 
 * @example
 * ```tsx
 * <ConnectionStatusBanner
 *   connectionState={signalRState}
 *   attemptNumber={reconnectAttempts}
 *   onRetryClick={handleManualRetry}
 * />
 * ```
 */
export const ConnectionStatusBanner: React.FC<ConnectionStatusBannerProps> = ({
  connectionState,
  attemptNumber = 0,
  maxAttempts = 10,
  reconnectedDisplayMs = 2000,
  onRetryClick,
  showRetryButton = true,
}) => {
  const [displayState, setDisplayState] = useState<DisplayState>('hidden');
  const [previousState, setPreviousState] = useState<ConnectionState>(connectionState);
  const reconnectedTimerRef = useRef<NodeJS.Timeout | null>(null);
  const debounceTimerRef = useRef<NodeJS.Timeout | null>(null);

  // Clear reconnected timer
  const clearReconnectedTimer = useCallback(() => {
    if (reconnectedTimerRef.current) {
      clearTimeout(reconnectedTimerRef.current);
      reconnectedTimerRef.current = null;
    }
  }, []);

  // Clear debounce timer
  const clearDebounceTimer = useCallback(() => {
    if (debounceTimerRef.current) {
      clearTimeout(debounceTimerRef.current);
      debounceTimerRef.current = null;
    }
  }, []);

  // Handle state transitions with debounce
  useEffect(() => {
    clearDebounceTimer();

    // Debounce state transitions by 100ms to prevent flicker
    debounceTimerRef.current = setTimeout(() => {
      // Determine new display state based on transition
      if (connectionState === 'connected') {
        // Was previously disconnected/reconnecting - show "reconnected" briefly
        if (previousState === 'disconnected' || previousState === 'reconnecting') {
          clearReconnectedTimer();
          setDisplayState('reconnected');

          // Auto-hide after reconnectedDisplayMs
          reconnectedTimerRef.current = setTimeout(() => {
            setDisplayState('hidden');
          }, reconnectedDisplayMs);
        } else {
          setDisplayState('hidden');
        }
      } else if (connectionState === 'disconnected') {
        // Immediately show disconnected - cancel any reconnected display
        clearReconnectedTimer();
        setDisplayState('disconnected');
      } else if (connectionState === 'reconnecting') {
        // Show reconnecting state
        clearReconnectedTimer();
        setDisplayState('reconnecting');
      }

      setPreviousState(connectionState);
    }, 100);

    return clearDebounceTimer;
  }, [
    connectionState,
    previousState,
    reconnectedDisplayMs,
    clearReconnectedTimer,
    clearDebounceTimer,
  ]);

  // Cleanup on unmount
  useEffect(() => {
    return () => {
      clearReconnectedTimer();
      clearDebounceTimer();
    };
  }, [clearReconnectedTimer, clearDebounceTimer]);

  // Render based on display state
  const renderContent = () => {
    switch (displayState) {
      case 'hidden':
        return null;

      case 'disconnected':
        return (
          <div 
            className="connection-banner connection-banner-disconnected"
            role="alert"
            aria-live="assertive"
            data-testid="connection-banner-disconnected"
          >
            <DisconnectOutlined className="banner-icon" />
            <Text className="banner-text">
              ⚠️ Connection lost. Retrying...
            </Text>
            {showRetryButton && onRetryClick && (
              <Button
                type="link"
                size="small"
                icon={<ReloadOutlined />}
                onClick={onRetryClick}
                className="banner-action"
              >
                Retry Now
              </Button>
            )}
          </div>
        );

      case 'reconnecting':
        return (
          <div 
            className="connection-banner connection-banner-reconnecting"
            role="status"
            aria-live="polite"
            data-testid="connection-banner-reconnecting"
          >
            <SyncOutlined spin className="banner-icon" />
            <Text className="banner-text">
              🔄 Reconnecting... Attempt {attemptNumber} of {maxAttempts}
            </Text>
          </div>
        );

      case 'reconnected':
        return (
          <div 
            className="connection-banner connection-banner-reconnected"
            role="status"
            aria-live="polite"
            data-testid="connection-banner-reconnected"
          >
            <CheckCircleOutlined className="banner-icon" />
            <Text className="banner-text">
              ✓ Reconnected
            </Text>
          </div>
        );

      default:
        return null;
    }
  };

  return (
    <div 
      className={`connection-status-wrapper ${displayState === 'hidden' ? 'hidden' : 'visible'}`}
      data-testid="connection-status-banner"
    >
      {renderContent()}
    </div>
  );
};

export default ConnectionStatusBanner;
