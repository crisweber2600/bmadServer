import { describe, it, expect, vi, beforeEach } from 'vitest';
import { renderHook, act } from '@testing-library/react';
import { useStreamingMessage } from './useStreamingMessage';

describe('useStreamingMessage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('should initialize with empty state', () => {
    const { result } = renderHook(() => useStreamingMessage());

    expect(result.current.streamingMessages.size).toBe(0);
    expect(result.current.isStreaming).toBe(false);
  });

  it('should handle message chunks and build up content', () => {
    const { result } = renderHook(() => useStreamingMessage());

    act(() => {
      result.current.handleMessageChunk({
        MessageId: 'msg-1',
        Chunk: 'Hello',
        IsComplete: false,
        AgentId: 'agent-1',
        Timestamp: new Date().toISOString(),
      });
    });

    expect(result.current.isStreaming).toBe(true);
    const message = result.current.streamingMessages.get('msg-1');
    expect(message?.content).toBe('Hello');
    expect(message?.isComplete).toBe(false);

    act(() => {
      result.current.handleMessageChunk({
        MessageId: 'msg-1',
        Chunk: ' World',
        IsComplete: false,
        AgentId: 'agent-1',
        Timestamp: new Date().toISOString(),
      });
    });

    const updatedMessage = result.current.streamingMessages.get('msg-1');
    expect(updatedMessage?.content).toBe('Hello World');
  });

  it('should call onComplete when message is complete', async () => {
    const onComplete = vi.fn();
    const { result } = renderHook(() => useStreamingMessage({ onComplete }));

    await act(async () => {
      result.current.handleMessageChunk({
        MessageId: 'msg-1',
        Chunk: 'Complete',
        IsComplete: true,
        AgentId: 'agent-1',
        Timestamp: new Date().toISOString(),
      });
    });

    expect(onComplete).toHaveBeenCalledWith(
      expect.objectContaining({
        messageId: 'msg-1',
        content: 'Complete',
        isComplete: true,
        agentId: 'agent-1',
      })
    );
  });

  it('should call onChunk for each chunk received', () => {
    const onChunk = vi.fn();
    const { result } = renderHook(() => useStreamingMessage({ onChunk }));

    act(() => {
      result.current.handleMessageChunk({
        MessageId: 'msg-1',
        Chunk: 'First',
        IsComplete: false,
        Timestamp: new Date().toISOString(),
      });
    });

    expect(onChunk).toHaveBeenCalledWith('First');

    act(() => {
      result.current.handleMessageChunk({
        MessageId: 'msg-1',
        Chunk: ' Second',
        IsComplete: false,
        Timestamp: new Date().toISOString(),
      });
    });

    expect(onChunk).toHaveBeenCalledWith(' Second');
    expect(onChunk).toHaveBeenCalledTimes(2);
  });

  it('should handle generation stopped event', () => {
    const onComplete = vi.fn();
    const { result } = renderHook(() => useStreamingMessage({ onComplete }));

    act(() => {
      result.current.handleMessageChunk({
        MessageId: 'msg-1',
        Chunk: 'Partial',
        IsComplete: false,
        Timestamp: new Date().toISOString(),
      });
    });

    act(() => {
      result.current.handleGenerationStopped({ MessageId: 'msg-1' });
    });

    const stoppedMessage = result.current.streamingMessages.get('msg-1');
    expect(stoppedMessage?.content).toBe('Partial (Stopped)');
    expect(stoppedMessage?.isComplete).toBe(true);
    expect(onComplete).toHaveBeenCalled();
  });

  it('should ignore chunks for stopped messages', () => {
    const { result } = renderHook(() => useStreamingMessage());

    act(() => {
      result.current.handleMessageChunk({
        MessageId: 'msg-1',
        Chunk: 'First',
        IsComplete: false,
        Timestamp: new Date().toISOString(),
      });
    });

    act(() => {
      result.current.handleGenerationStopped({ MessageId: 'msg-1' });
    });

    const contentBeforeIgnored = result.current.streamingMessages.get('msg-1')?.content;

    act(() => {
      result.current.handleMessageChunk({
        MessageId: 'msg-1',
        Chunk: ' Ignored',
        IsComplete: false,
        Timestamp: new Date().toISOString(),
      });
    });

    const contentAfterIgnored = result.current.streamingMessages.get('msg-1')?.content;
    expect(contentAfterIgnored).toBe(contentBeforeIgnored);
  });

  it('should handle multiple concurrent streaming messages', () => {
    const { result } = renderHook(() => useStreamingMessage());

    act(() => {
      result.current.handleMessageChunk({
        MessageId: 'msg-1',
        Chunk: 'First',
        IsComplete: false,
        Timestamp: new Date().toISOString(),
      });

      result.current.handleMessageChunk({
        MessageId: 'msg-2',
        Chunk: 'Second',
        IsComplete: false,
        Timestamp: new Date().toISOString(),
      });
    });

    expect(result.current.streamingMessages.size).toBe(2);
    expect(result.current.streamingMessages.get('msg-1')?.content).toBe('First');
    expect(result.current.streamingMessages.get('msg-2')?.content).toBe('Second');
  });

  it('should clear all streaming messages', () => {
    const { result } = renderHook(() => useStreamingMessage());

    act(() => {
      result.current.handleMessageChunk({
        MessageId: 'msg-1',
        Chunk: 'Test',
        IsComplete: false,
        Timestamp: new Date().toISOString(),
      });
    });

    expect(result.current.streamingMessages.size).toBe(1);

    act(() => {
      result.current.clearStreaming();
    });

    expect(result.current.streamingMessages.size).toBe(0);
    expect(result.current.isStreaming).toBe(false);
  });

  it('should clean up completed messages after timeout', async () => {
    vi.useFakeTimers();
    const { result } = renderHook(() => useStreamingMessage());

    act(() => {
      result.current.handleMessageChunk({
        MessageId: 'msg-1',
        Chunk: 'Done',
        IsComplete: true,
        Timestamp: new Date().toISOString(),
      });
    });

    expect(result.current.streamingMessages.get('msg-1')).toBeDefined();

    await act(async () => {
      vi.advanceTimersByTime(100);
    });

    expect(result.current.streamingMessages.get('msg-1')).toBeUndefined();
    expect(result.current.isStreaming).toBe(false);

    vi.useRealTimers();
  });
});
