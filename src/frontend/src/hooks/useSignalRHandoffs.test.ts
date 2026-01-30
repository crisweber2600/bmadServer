import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { renderHook, waitFor, act } from '@testing-library/react';
import { useSignalRHandoffs } from './useSignalRHandoffs';

// Create a mock connection that we can control
const mockConnectionInstance = {
  start: vi.fn().mockResolvedValue(undefined),
  stop: vi.fn().mockResolvedValue(undefined),
  on: vi.fn(),
  off: vi.fn(),
  invoke: vi.fn(),
  onreconnecting: vi.fn(),
  onreconnected: vi.fn(),
  onclose: vi.fn(),
  connectionId: 'mock-connection-id',
  state: 1,
};

// Mock @microsoft/signalr
vi.mock('@microsoft/signalr', () => ({
  HubConnectionBuilder: vi.fn().mockImplementation(() => ({
    withUrl: vi.fn().mockReturnThis(),
    withAutomaticReconnect: vi.fn().mockReturnThis(),
    configureLogging: vi.fn().mockReturnThis(),
    build: vi.fn().mockReturnValue(mockConnectionInstance),
  })),
  LogLevel: {
    Information: 2,
    Error: 5,
  },
  HubConnectionState: {
    Connected: 1,
    Reconnecting: 2,
    Disconnected: 3,
  },
}));

// Mock localStorage
Object.defineProperty(window, 'localStorage', {
  value: {
    getItem: vi.fn().mockReturnValue('mock-token'),
    setItem: vi.fn(),
    removeItem: vi.fn(),
    clear: vi.fn(),
  },
  writable: true,
});

// Mock import.meta.env
Object.defineProperty(import.meta, 'env', {
  value: {
    VITE_API_URL: 'http://localhost:8080',
  },
  writable: true,
});

describe('useSignalRHandoffs', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    // Reset mock connection methods
    mockConnectionInstance.start.mockResolvedValue(undefined);
    mockConnectionInstance.stop.mockResolvedValue(undefined);
    mockConnectionInstance.on.mockClear();
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  it('should initialize with disconnected state', () => {
    const { result } = renderHook(() => useSignalRHandoffs());

    // Initial state before connection
    expect(result.current.handoffHistory).toEqual([]);
    expect(result.current.onlineUsers).toEqual([]);
    expect(result.current.typingUsers).toEqual([]);
  });

  it('should connect to SignalR hub on mount', async () => {
    const { result } = renderHook(() => useSignalRHandoffs());

    await waitFor(() => {
      expect(result.current.connectionState).toBe('connected');
    });

    expect(result.current.error).toBeNull();
  });

  it('should register all event handlers', async () => {
    const { result } = renderHook(() => useSignalRHandoffs());

    await waitFor(() => {
      expect(result.current.connectionState).toBe('connected');
    });

    // Verify all handlers are registered
    const registeredEvents = mockConnectionInstance.on.mock.calls.map(call => call[0]);
    expect(registeredEvents).toContain('AGENT_HANDOFF');
    expect(registeredEvents).toContain('USER_ONLINE');
    expect(registeredEvents).toContain('USER_OFFLINE');
    expect(registeredEvents).toContain('USER_TYPING');
  });

  it('should handle incoming handoff events', async () => {
    const onHandoff = vi.fn();
    const { result } = renderHook(() => useSignalRHandoffs({ onHandoff }));

    await waitFor(() => {
      expect(result.current.connectionState).toBe('connected');
    });

    // Get the handler
    const handoffEventHandler = mockConnectionInstance.on.mock.calls.find(
      (call: unknown[]) => call[0] === 'AGENT_HANDOFF'
    )?.[1] as ((payload: unknown) => void) | undefined;

    expect(handoffEventHandler).toBeDefined();

    const mockPayload = {
      FromAgentId: 'agent-1',
      FromAgentName: 'BMAD Architect',
      ToAgentId: 'agent-2',
      ToAgentName: 'BMAD Dev',
      StepName: 'implementation',
      Reason: 'Handing off to implementation phase',
      Timestamp: new Date().toISOString(),
    };

    act(() => {
      handoffEventHandler!(mockPayload);
    });

    await waitFor(() => {
      expect(result.current.handoffHistory).toHaveLength(1);
    });

    const event = result.current.handoffHistory[0];
    expect(event.FromAgentId).toBe('agent-1');
    expect(event.FromAgentName).toBe('BMAD Architect');
    expect(event.ToAgentId).toBe('agent-2');
    expect(event.ToAgentName).toBe('BMAD Dev');

    expect(onHandoff).toHaveBeenCalledWith(expect.objectContaining({
      FromAgentId: 'agent-1',
      ToAgentId: 'agent-2',
    }));
  });

  it('should return current agent from most recent handoff', async () => {
    const { result } = renderHook(() => useSignalRHandoffs());

    await waitFor(() => {
      expect(result.current.connectionState).toBe('connected');
    });

    const handoffEventHandler = mockConnectionInstance.on.mock.calls.find(
      (call: unknown[]) => call[0] === 'AGENT_HANDOFF'
    )?.[1] as ((payload: unknown) => void) | undefined;

    act(() => {
      handoffEventHandler!({
        FromAgentId: 'agent-1',
        FromAgentName: 'Architect',
        ToAgentId: 'agent-2',
        ToAgentName: 'Dev',
        Timestamp: new Date().toISOString(),
      });
    });

    await waitFor(() => {
      expect(result.current.currentAgent).not.toBeNull();
    });

    expect(result.current.currentAgent).toEqual({
      agentId: 'agent-2',
      agentName: 'Dev',
    });
  });

  it('should return null for current agent when no handoffs', () => {
    const { result } = renderHook(() => useSignalRHandoffs());

    expect(result.current.currentAgent).toBeNull();
  });

  it('should clear handoff history', async () => {
    const { result } = renderHook(() => useSignalRHandoffs());

    await waitFor(() => {
      expect(result.current.connectionState).toBe('connected');
    });

    const handoffEventHandler = mockConnectionInstance.on.mock.calls.find(
      (call: unknown[]) => call[0] === 'AGENT_HANDOFF'
    )?.[1] as ((payload: unknown) => void) | undefined;

    act(() => {
      handoffEventHandler!({
        FromAgentId: 'agent-1',
        FromAgentName: 'Architect',
        ToAgentId: 'agent-2',
        ToAgentName: 'Dev',
        Timestamp: new Date().toISOString(),
      });
    });

    await waitFor(() => {
      expect(result.current.handoffHistory).toHaveLength(1);
    });

    act(() => {
      result.current.clearHistory();
    });

    expect(result.current.handoffHistory).toHaveLength(0);
  });

  describe('USER_ONLINE events', () => {
    it('should handle incoming USER_ONLINE events', async () => {
      const onUserOnline = vi.fn();
      const { result } = renderHook(() => useSignalRHandoffs({ onUserOnline }));

      await waitFor(() => {
        expect(result.current.connectionState).toBe('connected');
      });

      const userOnlineHandler = mockConnectionInstance.on.mock.calls.find(
        (call: unknown[]) => call[0] === 'USER_ONLINE'
      )?.[1] as ((payload: unknown) => void) | undefined;

      act(() => {
        userOnlineHandler!({
          UserId: 'user-1',
          DisplayName: 'Alice',
          IsOnline: true,
          LastSeen: new Date().toISOString(),
        });
      });

      await waitFor(() => {
        expect(result.current.onlineUsers).toHaveLength(1);
      });

      expect(result.current.onlineUsers[0].UserId).toBe('user-1');
      expect(result.current.onlineUsers[0].DisplayName).toBe('Alice');
      expect(result.current.onlineUsers[0].IsOnline).toBe(true);
      expect(onUserOnline).toHaveBeenCalled();
    });

    it('should update existing user on duplicate USER_ONLINE', async () => {
      const { result } = renderHook(() => useSignalRHandoffs());

      await waitFor(() => {
        expect(result.current.connectionState).toBe('connected');
      });

      const userOnlineHandler = mockConnectionInstance.on.mock.calls.find(
        (call: unknown[]) => call[0] === 'USER_ONLINE'
      )?.[1] as ((payload: unknown) => void) | undefined;

      act(() => {
        userOnlineHandler!({ UserId: 'user-1', DisplayName: 'Alice' });
      });

      await waitFor(() => {
        expect(result.current.onlineUsers).toHaveLength(1);
      });

      act(() => {
        userOnlineHandler!({ UserId: 'user-1', DisplayName: 'Alice Updated' });
      });

      await waitFor(() => {
        expect(result.current.onlineUsers[0].DisplayName).toBe('Alice Updated');
      });

      // Should still be 1 user, not 2
      expect(result.current.onlineUsers).toHaveLength(1);
    });
  });

  describe('USER_OFFLINE events', () => {
    it('should remove user on USER_OFFLINE event', async () => {
      const onUserOffline = vi.fn();
      const { result } = renderHook(() => useSignalRHandoffs({ onUserOffline }));

      await waitFor(() => {
        expect(result.current.connectionState).toBe('connected');
      });

      const userOnlineHandler = mockConnectionInstance.on.mock.calls.find(
        (call: unknown[]) => call[0] === 'USER_ONLINE'
      )?.[1] as ((payload: unknown) => void) | undefined;
      const userOfflineHandler = mockConnectionInstance.on.mock.calls.find(
        (call: unknown[]) => call[0] === 'USER_OFFLINE'
      )?.[1] as ((payload: unknown) => void) | undefined;

      act(() => {
        userOnlineHandler!({ UserId: 'user-1', DisplayName: 'Alice' });
        userOnlineHandler!({ UserId: 'user-2', DisplayName: 'Bob' });
      });

      await waitFor(() => {
        expect(result.current.onlineUsers).toHaveLength(2);
      });

      act(() => {
        userOfflineHandler!({ UserId: 'user-1', DisplayName: 'Alice' });
      });

      await waitFor(() => {
        expect(result.current.onlineUsers).toHaveLength(1);
      });

      expect(result.current.onlineUsers[0].UserId).toBe('user-2');
      expect(onUserOffline).toHaveBeenCalled();
    });
  });

  describe('USER_TYPING events', () => {
    it('should add user to typingUsers on USER_TYPING event', async () => {
      const onUserTyping = vi.fn();
      const { result } = renderHook(() => useSignalRHandoffs({ onUserTyping }));

      await waitFor(() => {
        expect(result.current.connectionState).toBe('connected');
      });

      const userTypingHandler = mockConnectionInstance.on.mock.calls.find(
        (call: unknown[]) => call[0] === 'USER_TYPING'
      )?.[1] as ((payload: unknown) => void) | undefined;

      act(() => {
        userTypingHandler!({
          UserId: 'user-1',
          DisplayName: 'Alice',
          WorkflowId: 'workflow-123',
        });
      });

      await waitFor(() => {
        expect(result.current.typingUsers).toHaveLength(1);
      });

      expect(result.current.typingUsers[0]).toBe('Alice');
      expect(onUserTyping).toHaveBeenCalled();
    });

    it('should not duplicate typing users', async () => {
      const { result } = renderHook(() => useSignalRHandoffs());

      await waitFor(() => {
        expect(result.current.connectionState).toBe('connected');
      });

      const userTypingHandler = mockConnectionInstance.on.mock.calls.find(
        (call: unknown[]) => call[0] === 'USER_TYPING'
      )?.[1] as ((payload: unknown) => void) | undefined;

      act(() => {
        userTypingHandler!({ UserId: 'user-1', DisplayName: 'Alice' });
        userTypingHandler!({ UserId: 'user-1', DisplayName: 'Alice' });
      });

      await waitFor(() => {
        expect(result.current.typingUsers.length).toBeLessThanOrEqual(1);
      });
    });

    it('should remove typing user when USER_OFFLINE received', async () => {
      const { result } = renderHook(() => useSignalRHandoffs());

      await waitFor(() => {
        expect(result.current.connectionState).toBe('connected');
      });

      const userTypingHandler = mockConnectionInstance.on.mock.calls.find(
        (call: unknown[]) => call[0] === 'USER_TYPING'
      )?.[1] as ((payload: unknown) => void) | undefined;
      const userOfflineHandler = mockConnectionInstance.on.mock.calls.find(
        (call: unknown[]) => call[0] === 'USER_OFFLINE'
      )?.[1] as ((payload: unknown) => void) | undefined;

      act(() => {
        userTypingHandler!({ UserId: 'user-1', DisplayName: 'Alice' });
      });

      await waitFor(() => {
        expect(result.current.typingUsers).toHaveLength(1);
      });

      act(() => {
        userOfflineHandler!({ UserId: 'user-1', DisplayName: 'Alice' });
      });

      await waitFor(() => {
        expect(result.current.typingUsers).toHaveLength(0);
      });
    });
  });

  describe('clearPresence', () => {
    it('should clear all presence data', async () => {
      const { result } = renderHook(() => useSignalRHandoffs());

      await waitFor(() => {
        expect(result.current.connectionState).toBe('connected');
      });

      const userOnlineHandler = mockConnectionInstance.on.mock.calls.find(
        (call: unknown[]) => call[0] === 'USER_ONLINE'
      )?.[1] as ((payload: unknown) => void) | undefined;
      const userTypingHandler = mockConnectionInstance.on.mock.calls.find(
        (call: unknown[]) => call[0] === 'USER_TYPING'
      )?.[1] as ((payload: unknown) => void) | undefined;

      act(() => {
        userOnlineHandler!({ UserId: 'user-1', DisplayName: 'Alice' });
        userTypingHandler!({ UserId: 'user-1', DisplayName: 'Alice' });
      });

      await waitFor(() => {
        expect(result.current.onlineUsers).toHaveLength(1);
        expect(result.current.typingUsers).toHaveLength(1);
      });

      act(() => {
        result.current.clearPresence();
      });

      expect(result.current.onlineUsers).toHaveLength(0);
      expect(result.current.typingUsers).toHaveLength(0);
    });
  });

  it('should disconnect on unmount', async () => {
    const { result, unmount } = renderHook(() => useSignalRHandoffs());

    await waitFor(() => {
      expect(result.current.connectionState).toBe('connected');
    });

    unmount();

    expect(mockConnectionInstance.stop).toHaveBeenCalled();
  });
});
