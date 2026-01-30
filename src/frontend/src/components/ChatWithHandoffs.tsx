import React, { useState, useCallback } from 'react';
import { ResponsiveChat } from './ResponsiveChat';
import { AgentHandoffIndicator } from './AgentHandoffIndicator';
import { useSignalRHandoffs } from '../hooks/useSignalRHandoffs';
import type { AgentHandoffEvent } from '../hooks/useSignalRHandoffs';

interface HandoffMessage {
  type: 'handoff';
  id: string;
  handoffEvent: AgentHandoffEvent;
  timestamp: Date;
}

interface RegularMessage {
  type: 'regular';
  id: string;
  content: string;
  isUser: boolean;
  timestamp: Date;
  agentName?: string;
}

export interface ChatWithHandoffsProps {
  messages: RegularMessage[];
  onSendMessage: (message: string) => void;
  onLoadMore?: () => void;
  onStopGenerating?: (messageId: string) => void;
  hasMore?: boolean;
  isLoading?: boolean;
  debug?: boolean;
}

export const ChatWithHandoffs: React.FC<ChatWithHandoffsProps> = ({
  messages,
  onSendMessage,
  onLoadMore,
  onStopGenerating,
  hasMore = false,
  isLoading = false,
  debug = false,
}) => {
  const [handoffMessages, setHandoffMessages] = useState<Map<string, HandoffMessage>>(new Map());

  const handleHandoffEvent = useCallback((event: AgentHandoffEvent) => {
    if (debug) {
      console.log('[ChatWithHandoffs] Handoff event received:', event);
    }

    const handoffId = `handoff-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
    const handoffMessage: HandoffMessage = {
      type: 'handoff',
      id: handoffId,
      handoffEvent: event,
      timestamp: new Date(),
    };

    setHandoffMessages((prev) => new Map(prev).set(handoffId, handoffMessage));
  }, [debug]);

  const { connectionState: signalRConnectionState, error: signalRError } = useSignalRHandoffs({
    onHandoff: handleHandoffEvent,
    debug,
  });

  /* Removed unused mergedMessages and renderMessages logic */

  return (
    <div data-testid="chat-with-handoffs">
      {signalRConnectionState === 'disconnected' && (
        <div
          className="signalr-status-warning"
          role="alert"
          aria-live="polite"
          style={{
            padding: '12px 16px',
            backgroundColor: '#fff7e6',
            borderLeft: '4px solid #faad14',
            marginBottom: '16px',
            fontSize: '14px',
            color: '#8c6e00',
          }}
        >
          ⚠️ Connection to real-time service is not available
          {signalRError && ` - ${signalRError}`}
        </div>
      )}

      {signalRConnectionState === 'reconnecting' && (
        <div
          className="signalr-status-warning"
          role="status"
          aria-live="polite"
          style={{
            padding: '12px 16px',
            backgroundColor: '#e6f7ff',
            borderLeft: '4px solid #1890ff',
            marginBottom: '16px',
            fontSize: '14px',
            color: '#0050b3',
          }}
        >
          ⟳ Reconnecting to real-time service...
        </div>
      )}

      <ResponsiveChat
        messages={messages}
        onSendMessage={onSendMessage}
        onLoadMore={onLoadMore}
        onStopGenerating={onStopGenerating}
        hasMore={hasMore}
        isLoading={isLoading}
      />

      {handoffMessages.size > 0 && (
        <div
          className="handoff-messages-container"
          style={{
            marginTop: '16px',
            paddingTop: '16px',
            borderTop: '1px solid #f0f0f0',
          }}
          data-testid="handoff-messages"
        >
          {Array.from(handoffMessages.values()).map((handoffMsg) => (
            <AgentHandoffIndicator
              key={handoffMsg.id}
              fromAgentId={handoffMsg.handoffEvent.FromAgentId}
              fromAgentName={handoffMsg.handoffEvent.FromAgentName}
              toAgentId={handoffMsg.handoffEvent.ToAgentId}
              toAgentName={handoffMsg.handoffEvent.ToAgentName}
              timestamp={handoffMsg.handoffEvent.Timestamp}
              reason={handoffMsg.handoffEvent.Reason}
              stepName={handoffMsg.handoffEvent.StepName}
              fromAvatarUrl={handoffMsg.handoffEvent.FromAvatarUrl}
              toAvatarUrl={handoffMsg.handoffEvent.ToAvatarUrl}
            />
          ))}
        </div>
      )}
    </div>
  );
};
