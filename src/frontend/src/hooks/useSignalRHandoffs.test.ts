import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { renderHook, waitFor, act } from '@testing-library/react';
import { useSignalRHandoffs, AgentHandoffEvent } from './useSignalRHandoffs';
import * as signalR from '@microsoft/signalr';

// Mock @microsoft/signalr
vi.mock('@microsoft/signalr', () => {
  const mockConnection = {
    start: vi.fn().mockResolvedValue(undefined),
    stop: vi.fn().mockResolvedValue(undefined),
    on: vi.fn(),
    off: vi.fn(),
    invoke: vi.fn(),
    onreconnecting: vi.fn(),
    onreconnected: vi.fn(),
    onclose: vi.fn(),
    connectionId: 'mock-connection-id',
    state: 1, // HubConnectionState.Connected
  };

  const mockBuilder = {
    withUrl: vi.fn().mockReturnThis(),
    withAutomaticReconnect: vi.fn().mockReturnThis(),
    configureLogging: vi.fn().mockReturnThis(),
    build: vi.fn().mockReturnValue(mockConnection),
  };

  return {
    HubConnectionBuilder: vi.fn(() => mockBuilder),
    LogLevel: {
      Information: 2,
      Error: 5,
    },
    HubConnectionState: {
      Connected: 1,
      Reconnecting: 2,
      Disconnected: 3,
    },
  };
});

// Mock localStorage
const localStorageMock = {
  getItem: vi.fn().mockReturnValue('mock-token'),
  setItem: vi.fn(),
  removeItem: vi.fn(),
  clear: vi.fn(),
};

Object.defineProperty(window, 'localStorage', {
  value: localStorageMock,
});

// Mock import.meta.env
Object.defineProperty(import.meta, 'env', {
  value: {
    VITE_API_URL: 'http://localhost:8080',
  },
});

describe('useSignalRHandoffs', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  it('should initialize with disconnected state', () => {
    const { result } = renderHook(() => useSignalRHandoffs());

    expect(result.current.connectionState).toBe('disconnected');
    expect(result.current.handoffHistory).toEqual([]);
    expect(result.current.error).toBeNull();
  });

  it('should connect to SignalR hub on mount', async () => {
    const { result } = renderHook(() => useSignalRHandoffs());

    await waitFor(() => {
      expect(result.current.connectionState).toBe('connected');
    });

    expect(result.current.error).toBeNull();
  });

  it('should register AGENT_HANDOFF event handler', async () => {
    const mockConnection = (signalR.HubConnectionBuilder as any)().build();
    const { result } = renderHook(() => useSignalRHandoffs());

    await waitFor(() => {
      expect(result.current.connectionState).toBe('connected');
    });

    expect(mockConnection.on).toHaveBeenCalledWith(
      'AGENT_HANDOFF',
      expect.any(Function)
    );
  });

  it('should handle incoming handoff events', async () => {
    const onHandoff = vi.fn();
    const mockConnection = (signalR.HubConnectionBuilder as any)().build();
    const { result } = renderHook(() => useSignalRHandoffs({ onHandoff }));

    await waitFor(() => {
      expect(result.current.connectionState).toBe('connected');
    });

    // Simulate incoming handoff event
    const handoffEventHandler = mockConnection.on.mock.calls.find(
      (call: any[]) => call[0] === 'AGENT_HANDOFF'
    )?.[1];

    const mockPayload = {
      FromAgentId: 'agent-1',
      FromAgentName: 'BMAD Architect',
      ToAgentId: 'agent-2',
      ToAgentName: 'BMAD Dev',
      StepName: 'implementation',
      Reason: 'Handing off to implementation phase',
      Timestamp: new Date().toISOString(),
      FromAvatarUrl: 'http://example.com/avatar1.png',
      ToAvatarUrl: 'http://example.com/avatar2.png',
    };

    act(() => {
      handoffEventHandler(mockPayload);
    });

    // Wait for state update
    await waitFor(() => {
      expect(result.current.handoffHistory).toHaveLength(1);
    });

    const event = result.current.handoffHistory[0];
    expect(event.FromAgentId).toBe('agent-1');
    expect(event.FromAgentName).toBe('BMAD Architect');
    expect(event.ToAgentId).toBe('agent-2');
    expect(event.ToAgentName).toBe('BMAD Dev');
    expect(event.StepName).toBe('implementation');
    expect(event.Reason).toBe('Handing off to implementation phase');

    expect(onHandoff).toHaveBeenCalledWith(expect.objectContaining({
      FromAgentId: 'agent-1',
      ToAgentId: 'agent-2',
    }));
  });

  it('should track multiple handoff events in order', async () => {
    const mockConnection = (signalR.HubConnectionBuilder as any)().build();
    const { result } = renderHook(() => useSignalRHandoffs());

    await waitFor(() => {
      expect(result.current.connectionState).toBe('connected');
    });

    const handoffEventHandler = mockConnection.on.mock.calls.find(
      (call: any[]) => call[0] === 'AGENT_HANDOFF'
    )?.[1];

    const event1 = {
      FromAgentId: 'agent-1',
      FromAgentName: 'Architect',
      ToAgentId: 'agent-2',
      ToAgentName: 'Dev',
      Timestamp: new Date().toISOString(),
    };

    const event2 = {
      FromAgentId: 'agent-2',
      FromAgentName: 'Dev',
      ToAgentId: 'agent-3',
      ToAgentName: 'QA',
      Timestamp: new Date().toISOString(),
    };

    act(() => {
      handoffEventHandler(event1);
      handoffEventHandler(event2);
    });

    await waitFor(() => {
      expect(result.current.handoffHistory).toHaveLength(2);
    });

    expect(result.current.handoffHistory[0].FromAgentName).toBe('Architect');
    expect(result.current.handoffHistory[1].FromAgentName).toBe('Dev');
  });

  it('should return current agent from most recent handoff', async () => {
    const mockConnection = (signalR.HubConnectionBuilder as any)().build();
    const { result } = renderHook(() => useSignalRHandoffs());

    await waitFor(() => {
      expect(result.current.connectionState).toBe('connected');
    });

    const handoffEventHandler = mockConnection.on.mock.calls.find(
      (call: any[]) => call[0] === 'AGENT_HANDOFF'
    )?.[1];

    act(() => {
      handoffEventHandler({
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
    const mockConnection = (signalR.HubConnectionBuilder as any)().build();
    const { result } = renderHook(() => useSignalRHandoffs());

    await waitFor(() => {
      expect(result.current.connectionState).toBe('connected');
    });

    const handoffEventHandler = mockConnection.on.mock.calls.find(
      (call: any[]) => call[0] === 'AGENT_HANDOFF'
    )?.[1];

    act(() => {
      handoffEventHandler({
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

  it('should call onConnectionStateChange callback', async () => {
    const onConnectionStateChange = vi.fn();
    const { result } = renderHook(() =>
      useSignalRHandoffs({ onConnectionStateChange })
    );

    await waitFor(() => {
      expect(result.current.connectionState).toBe('connected');
    });

    expect(onConnectionStateChange).toHaveBeenCalledWith('connected');
  });

  it('should parse timestamp correctly', async () => {
    const mockConnection = (signalR.HubConnectionBuilder as any)().build();
    const { result } = renderHook(() => useSignalRHandoffs());

    await waitFor(() => {
      expect(result.current.connectionState).toBe('connected');
    });

    const handoffEventHandler = mockConnection.on.mock.calls.find(
      (call: any[]) => call[0] === 'AGENT_HANDOFF'
    )?.[1];

    const timestamp = new Date('2026-01-26T14:30:00Z');
    act(() => {
      handoffEventHandler({
        FromAgentId: 'agent-1',
        FromAgentName: 'Architect',
        ToAgentId: 'agent-2',
        ToAgentName: 'Dev',
        Timestamp: timestamp.toISOString(),
      });
    });

    await waitFor(() => {
      expect(result.current.handoffHistory).toHaveLength(1);
    });

    const event = result.current.handoffHistory[0];
    expect(event.Timestamp).toBeInstanceOf(Date);
    expect(event.Timestamp.getTime()).toBe(timestamp.getTime());
  });

  it('should disconnect on unmount', async () => {
    const mockConnection = (signalR.HubConnectionBuilder as any)().build();
    const { unmount } = renderHook(() => useSignalRHandoffs());

    await waitFor(() => {
      expect(mockConnection.start).toHaveBeenCalled();
    });

    unmount();

    expect(mockConnection.stop).toHaveBeenCalled();
  });
});
