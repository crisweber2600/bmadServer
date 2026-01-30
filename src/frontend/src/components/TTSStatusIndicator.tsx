import React, { useEffect, useCallback } from 'react';
import { Typography, Tooltip } from 'antd';
import { SoundOutlined, AudioMutedOutlined } from '@ant-design/icons';
import './TTSStatusIndicator.css';

const { Text } = Typography;

export interface TTSStatusIndicatorProps {
  /** Whether TTS is currently playing */
  isPlaying: boolean;
  /** Name of the agent currently speaking */
  agentName?: string;
  /** Callback to stop TTS playback */
  onStop?: () => void;
  /** Timeout in ms before auto-stopping stale playback state - default 30000 */
  staleTimeoutMs?: number;
}

/**
 * TTS playback status indicator
 * 
 * Shows pulsing sound icon when playing, muted icon when idle.
 * Includes 30-second stale state recovery to handle TTS crashes.
 * 
 * @example
 * ```tsx
 * <TTSStatusIndicator
 *   isPlaying={ttsState.isPlaying}
 *   agentName={ttsState.agentName}
 *   onStop={handleStopTts}
 * />
 * ```
 */
export const TTSStatusIndicator: React.FC<TTSStatusIndicatorProps> = ({
  isPlaying,
  agentName,
  onStop,
  staleTimeoutMs = 30000,
}) => {
  // Stale state recovery - auto-stop after timeout
  useEffect(() => {
    if (isPlaying && staleTimeoutMs > 0) {
      const timeout = setTimeout(() => {
        console.warn('[TTSStatusIndicator] Stale playing state detected, auto-stopping');
        onStop?.();
      }, staleTimeoutMs);
      
      return () => clearTimeout(timeout);
    }
  }, [isPlaying, staleTimeoutMs, onStop]);

  const handleClick = useCallback(() => {
    if (isPlaying && onStop) {
      onStop();
    }
  }, [isPlaying, onStop]);

  const handleKeyDown = useCallback((e: React.KeyboardEvent) => {
    if ((e.key === 'Enter' || e.key === ' ') && isPlaying && onStop) {
      e.preventDefault();
      onStop();
    }
  }, [isPlaying, onStop]);

  const statusText = isPlaying
    ? `${agentName || 'Agent'} speaking...`
    : 'TTS idle';

  const tooltipText = isPlaying
    ? `Click to stop ${agentName || 'agent'}`
    : 'Text-to-speech is idle';

  return (
    <Tooltip title={tooltipText} placement="top">
      <div
        className={`tts-status-indicator ${isPlaying ? 'playing' : 'idle'}`}
        onClick={handleClick}
        onKeyDown={handleKeyDown}
        tabIndex={isPlaying ? 0 : -1}
        role="button"
        aria-label={statusText}
        aria-pressed={isPlaying ? 'true' : 'false'}
        data-testid="tts-status-indicator"
      >
        {isPlaying ? (
          <SoundOutlined className="tts-icon playing" aria-hidden="true" />
        ) : (
          <AudioMutedOutlined className="tts-icon idle" aria-hidden="true" />
        )}
        <Text className={`tts-text ${isPlaying ? 'playing' : 'idle'}`}>
          {statusText}
        </Text>
      </div>
    </Tooltip>
  );
};

export default TTSStatusIndicator;
