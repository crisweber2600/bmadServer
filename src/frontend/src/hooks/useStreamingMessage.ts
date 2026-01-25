import { useState, useCallback, useRef, useEffect } from 'react';

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
  const cleanupTimeoutsRef = useRef<Map<string, number>>(new Map());

  // Cleanup timeouts on unmount
  useEffect(() => {
    return () => {
      cleanupTimeoutsRef.current.forEach((timeout) => clearTimeout(timeout));
      cleanupTimeoutsRef.current.clear();
    };
  }, []);

  const scheduleCleanup = useCallback((messageId: string, cleanupStopped: boolean = false) => {
    // Clear any existing timeout for this message
    const existingTimeout = cleanupTimeoutsRef.current.get(messageId);
    if (existingTimeout) {
      clearTimeout(existingTimeout);
    }

    const timeout = setTimeout(() => {
      setStreamingMessages((current) => {
        const cleaned = new Map(current);
        cleaned.delete(messageId);
        return cleaned;
      });
      setIsStreaming(false);
      if (cleanupStopped) {
        stoppedMessagesRef.current.delete(messageId);
      }
      cleanupTimeoutsRef.current.delete(messageId);
    }, 100) as unknown as number;

    cleanupTimeoutsRef.current.set(messageId, timeout);
  }, []);

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

      setStreamingMessages((prev: Map<string, StreamingMessage>) => {
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
          // Schedule cleanup for completed message
          scheduleCleanup(messageId);
          
          // Check if there are any remaining incomplete messages before setting isStreaming to false
          const hasIncompleteMessages = Array.from(newMap.values()).some((msg: StreamingMessage) => !msg.isComplete);
          if (!hasIncompleteMessages) {
            setIsStreaming(false);
          }
        } else {
          setIsStreaming(true);
        }

        return newMap;
      });
    },
    [onComplete, onChunk, scheduleCleanup]
  );

  const handleGenerationStopped = useCallback((data: { MessageId: string }) => {
    const messageId = data.MessageId;
    stoppedMessagesRef.current.add(messageId);

    setStreamingMessages((prev: Map<string, StreamingMessage>) => {
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

        // Schedule cleanup for stopped message
        scheduleCleanup(messageId, true);
        
        // Check if there are any remaining incomplete messages
        const hasIncompleteMessages = Array.from(newMap.values()).some((msg: StreamingMessage) => !msg.isComplete);
        if (!hasIncompleteMessages) {
          setIsStreaming(false);
        }
      }

      return newMap;
    });
  }, [onComplete, stoppedMessageSuffix, scheduleCleanup]);

  const clearStreaming = useCallback(() => {
    // Clear all pending timeouts
    cleanupTimeoutsRef.current.forEach((timeout: number) => clearTimeout(timeout));
    cleanupTimeoutsRef.current.clear();
    
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
