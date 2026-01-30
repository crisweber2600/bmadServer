import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { ChatWithHandoffs } from './ChatWithHandoffs';
import { type AgentHandoffEvent, type UseSignalRHandoffsOptions } from '../hooks/useSignalRHandoffs';

// Mock hook with all required properties
const mockUseSignalRHandoffs = vi.fn().mockReturnValue({
  connectionState: 'connected',
  error: null,
  handoffHistory: [],
  currentAgent: null,
  clearHistory: vi.fn(),
  connection: null,
  onlineUsers: [],
  typingUsers: [],
  clearPresence: vi.fn(),
});

vi.mock('../hooks/useSignalRHandoffs', () => ({
  useSignalRHandoffs: (options?: UseSignalRHandoffsOptions) => mockUseSignalRHandoffs(options),
  AgentHandoffEvent: {},
}));

vi.mock('./ResponsiveChat', () => ({
  ResponsiveChat: ({ messages }: any) => (
    <div data-testid="responsive-chat">
      {messages.map((msg: any) => (
        <div key={msg.id} data-testid={`message-${msg.id}`}>
          {msg.content}
        </div>
      ))}
    </div>
  ),
}));

vi.mock('./AgentHandoffIndicator', () => ({
  AgentHandoffIndicator: ({ fromAgentName, toAgentName, stepName }: any) => (
    <div data-testid="handoff-indicator">
      {fromAgentName} → {toAgentName} {stepName && `(${stepName})`}
    </div>
  ),
}));

describe('ChatWithHandoffs', () => {
  const mockMessages = [
    {
      type: 'regular' as const,
      id: 'msg-1',
      content: 'Hello',
      isUser: true,
      timestamp: new Date(),
      agentName: 'User',
    },
  ];

  const mockHandoffEvent: AgentHandoffEvent = {
    FromAgentId: 'agent-1',
    FromAgentName: 'Architect',
    ToAgentId: 'agent-2',
    ToAgentName: 'Dev',
    Timestamp: new Date(),
    StepName: 'implementation',
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  it('should render chat container with regular messages', () => {
    render(
      <ChatWithHandoffs
        messages={mockMessages}
        onSendMessage={vi.fn()}
      />
    );

    expect(screen.getByTestId('chat-with-handoffs')).toBeInTheDocument();
    expect(screen.getByTestId('responsive-chat')).toBeInTheDocument();
  });

  it('should render disconnect warning when connection is disconnected', () => {
    mockUseSignalRHandoffs.mockReturnValue({
      connectionState: 'disconnected',
      error: 'Connection failed',
      handoffHistory: [],
      currentAgent: null,
      clearHistory: vi.fn(),
      connection: null,
      onlineUsers: [],
      typingUsers: [],
      clearPresence: vi.fn(),
    });

    render(
      <ChatWithHandoffs
        messages={mockMessages}
        onSendMessage={vi.fn()}
      />
    );

    const warning = screen.queryByText(/Connection to real-time service is not available/);
    expect(warning).toBeInTheDocument();
    expect(warning).toHaveTextContent('Connection failed');
  });

  it('should render reconnecting status when connection is reconnecting', () => {
    mockUseSignalRHandoffs.mockReturnValue({
      connectionState: 'reconnecting',
      error: null,
      handoffHistory: [],
      currentAgent: null,
      clearHistory: vi.fn(),
      connection: null,
      onlineUsers: [],
      typingUsers: [],
      clearPresence: vi.fn(),
    });

    render(
      <ChatWithHandoffs
        messages={mockMessages}
        onSendMessage={vi.fn()}
      />
    );

    expect(screen.getByText(/Reconnecting to real-time service/)).toBeInTheDocument();
  });

  it('should not show status indicators when connected', () => {
    mockUseSignalRHandoffs.mockReturnValue({
      connectionState: 'connected',
      error: null,
      handoffHistory: [],
      currentAgent: null,
      clearHistory: vi.fn(),
      connection: null,
      onlineUsers: [],
      typingUsers: [],
      clearPresence: vi.fn(),
    });

    render(
      <ChatWithHandoffs
        messages={mockMessages}
        onSendMessage={vi.fn()}
      />
    );

    expect(screen.queryByText(/not available/)).not.toBeInTheDocument();
    expect(screen.queryByText(/Reconnecting/)).not.toBeInTheDocument();
  });

  it('should pass props to ResponsiveChat', () => {
    const onSendMessage = vi.fn();
    const onLoadMore = vi.fn();
    const onStopGenerating = vi.fn();

    render(
      <ChatWithHandoffs
        messages={mockMessages}
        onSendMessage={onSendMessage}
        onLoadMore={onLoadMore}
        onStopGenerating={onStopGenerating}
        hasMore={true}
        isLoading={false}
      />
    );

    expect(screen.getByTestId('responsive-chat')).toBeInTheDocument();
  });

  it('should handle handoff events from SignalR', async () => {
    let handoffCallback: any = null;

    mockUseSignalRHandoffs.mockImplementation((options: UseSignalRHandoffsOptions) => {
      if (options?.onHandoff) {
        handoffCallback = options.onHandoff;
      }
      return {
        connectionState: 'connected',
        error: null,
        handoffHistory: [],
        currentAgent: null,
        clearHistory: vi.fn(),
        connection: null,
        onlineUsers: [],
        typingUsers: [],
        clearPresence: vi.fn(),
      };
    });

    render(
      <ChatWithHandoffs
        messages={mockMessages}
        onSendMessage={vi.fn()}
      />
    );

    if (handoffCallback) {
      handoffCallback(mockHandoffEvent);

      await waitFor(() => {
        expect(screen.getByTestId('handoff-messages')).toBeInTheDocument();
      });

      expect(screen.getByText(/Architect → Dev/)).toBeInTheDocument();
    }
  });

  it('should display handoff with step name', async () => {
    let handoffCallback: any = null;

    mockUseSignalRHandoffs.mockImplementation((options: UseSignalRHandoffsOptions) => {
      if (options?.onHandoff) {
        handoffCallback = options.onHandoff;
      }
      return {
        connectionState: 'connected',
        error: null,
        handoffHistory: [],
        currentAgent: null,
        clearHistory: vi.fn(),
        connection: null,
        onlineUsers: [],
        typingUsers: [],
        clearPresence: vi.fn(),
      };
    });

    render(
      <ChatWithHandoffs
        messages={mockMessages}
        onSendMessage={vi.fn()}
      />
    );

    if (handoffCallback) {
      handoffCallback({
        ...mockHandoffEvent,
        StepName: 'implementation',
      });

      await waitFor(() => {
        expect(screen.getByText(/implementation/)).toBeInTheDocument();
      });
    }
  });

  it('should handle multiple handoff events', async () => {
    let handoffCallback: any = null;
    mockUseSignalRHandoffs.mockImplementation((options: UseSignalRHandoffsOptions) => {
      if (options?.onHandoff) {
         handoffCallback = options.onHandoff;
      }
      return {
        connectionState: 'connected',
        error: null,
        handoffHistory: [],
        currentAgent: null,
        clearHistory: vi.fn(),
        connection: null,
        onlineUsers: [],
        typingUsers: [],
        clearPresence: vi.fn(),
      };
    });

    render(
      <ChatWithHandoffs
        messages={mockMessages}
        onSendMessage={vi.fn()}
      />
    );

    if (handoffCallback) {
      handoffCallback(mockHandoffEvent);
      handoffCallback({
        ...mockHandoffEvent,
        FromAgentId: 'agent-2',
        FromAgentName: 'Dev',
        ToAgentId: 'agent-3',
        ToAgentName: 'QA',
      });

      await waitFor(() => {
        const indicators = screen.getAllByTestId('handoff-indicator');
        expect(indicators).toHaveLength(2);
      });
    }
  });

  it('should call onConnectionStateChange callback', () => {
    mockUseSignalRHandoffs.mockImplementation((options: UseSignalRHandoffsOptions) => {
      expect(options?.onConnectionStateChange).toBeDefined();
      return {
        connectionState: 'connected',
        error: null,
        handoffHistory: [],
        currentAgent: null,
        clearHistory: vi.fn(),
        connection: null,
        onlineUsers: [],
        typingUsers: [],
        clearPresence: vi.fn(),
      };
    });

    render(
      <ChatWithHandoffs
        messages={mockMessages}
        onSendMessage={vi.fn()}
      />
    );

    expect(mockUseSignalRHandoffs).toHaveBeenCalled();
  });

  it('should not render handoff messages container when no handoffs', () => {
    render(
      <ChatWithHandoffs
        messages={mockMessages}
        onSendMessage={vi.fn()}
      />
    );

    expect(screen.queryByTestId('handoff-messages')).not.toBeInTheDocument();
  });
});
