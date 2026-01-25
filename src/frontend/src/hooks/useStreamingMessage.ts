import { useState, useCallback, useRef } from 'react';

export interface StreamingMessage {
  messageId: string;
  content: string;
  isComplete: boolean;
  agentId?: string;
  timestamp: Date;
}

export interface UseStreamingMessageOptions {
  onComplete?: (message: StreamingMessage) => void;
  onChunk?: (chunk: string) => void;
  stoppedMessageSuffix?: string;
}

export function useStreamingMessage(options?: UseStreamingMessageOptions) {
  const {
    onComplete,
    onChunk,
    stoppedMessageSuffix = ' (Stopped)',
  } = options || {};
  
  const [streamingMessages, setStreamingMessages] = useState<Map<string, StreamingMessage>>(
    new Map()
  );
  const [isStreaming, setIsStreaming] = useState(false);
  const stoppedMessagesRef = useRef<Set<string>>(new Set());

  const handleMessageChunk = useCallback(
    (data: {
      MessageId: string;
      Chunk: string;
      IsComplete: boolean;
      AgentId?: string;
      Timestamp: string;
    }) => {
      const messageId = data.MessageId;

      // Ignore chunks for stopped messages
      if (stoppedMessagesRef.current.has(messageId)) {
        return;
      }

      setStreamingMessages((prev) => {
        const newMap = new Map(prev);
        const existing = newMap.get(messageId);

        const updated: StreamingMessage = {
          messageId,
          content: (existing?.content || '') + data.Chunk,
          isComplete: data.IsComplete,
          agentId: data.AgentId,
          timestamp: new Date(data.Timestamp),
        };

        newMap.set(messageId, updated);

        // Call callbacks
        onChunk?.(data.Chunk);
        if (data.IsComplete) {
          onComplete?.(updated);
          // Clean up completed message after callback
          setTimeout(() => {
            setStreamingMessages((current) => {
              const cleaned = new Map(current);
              cleaned.delete(messageId);
              return cleaned;
            });
            setIsStreaming(false);
          }, 100);
        } else {
          setIsStreaming(true);
        }

        return newMap;
      });
    },
    [onComplete, onChunk]
  );

  const handleGenerationStopped = useCallback((data: { MessageId: string }) => {
    const messageId = data.MessageId;
    stoppedMessagesRef.current.add(messageId);

    setStreamingMessages((prev) => {
      const newMap = new Map(prev);
      const existing = newMap.get(messageId);

      if (existing) {
        const stopped: StreamingMessage = {
          ...existing,
          content: existing.content + stoppedMessageSuffix,
          isComplete: true,
        };
        newMap.set(messageId, stopped);

        // Call onComplete callback
        onComplete?.(stopped);

        // Clean up stopped message
        setTimeout(() => {
          setStreamingMessages((current) => {
            const cleaned = new Map(current);
            cleaned.delete(messageId);
            return cleaned;
          });
          setIsStreaming(false);
          stoppedMessagesRef.current.delete(messageId);
        }, 100);
      }

      return newMap;
    });
  }, [onComplete, stoppedMessageSuffix]);

  const clearStreaming = useCallback(() => {
    setStreamingMessages(new Map());
    setIsStreaming(false);
    stoppedMessagesRef.current.clear();
  }, []);

  return {
    streamingMessages,
    isStreaming,
    handleMessageChunk,
    handleGenerationStopped,
    clearStreaming,
  };
}
