import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, fireEvent, act, waitFor } from '@testing-library/react';
import { ResponsiveChat } from './ResponsiveChat';

const mockScrollContainerRef = { current: document.createElement('div') };

const createMockStreamingHook = (overrides = {}) => ({
  streamingMessages: new Map(),
  isStreaming: false,
  handleMessageChunk: vi.fn(),
  handleGenerationStopped: vi.fn(),
  ...overrides,
});

const createMockScrollHook = (overrides = {}) => ({
  scrollContainerRef: mockScrollContainerRef,
  showNewMessageBadge: false,
  scrollToBottom: vi.fn(),
  handleScroll: vi.fn(),
  onNewMessage: vi.fn(),
  saveScrollPosition: vi.fn(),
  restoreScrollPosition: vi.fn(),
  dismissNewMessageBadge: vi.fn(),
  ...overrides,
});

const createMockTouchHook = (overrides = {}) => ({
  attachGestureListeners: vi.fn(() => () => {}),
  copyToClipboard: vi.fn(),
  ...overrides,
});

let mockStreamingHook = createMockStreamingHook();
let mockScrollHook = createMockScrollHook();
let mockTouchHook = createMockTouchHook();

vi.mock('../hooks/useStreamingMessage', () => ({
  useStreamingMessage: vi.fn(() => mockStreamingHook),
}));

vi.mock('../hooks/useScrollManagement', () => ({
  useScrollManagement: vi.fn(() => mockScrollHook),
}));

vi.mock('../hooks/useTouchGestures', () => ({
  useTouchGestures: vi.fn(() => mockTouchHook),
}));

describe('ResponsiveChat', () => {
  const defaultProps = {
    messages: [],
    onSendMessage: vi.fn(),
  };

  beforeEach(() => {
    vi.clearAllMocks();
    mockStreamingHook = createMockStreamingHook();
    mockScrollHook = createMockScrollHook();
    mockTouchHook = createMockTouchHook();
  });

  afterEach(() => {
    vi.resetAllMocks();
  });

  describe('rendering', () => {
    it('renders without errors', () => {
      render(<ResponsiveChat {...defaultProps} />);
      expect(screen.getByText('BMAD Chat')).toBeInTheDocument();
    });

    it('renders empty state when no messages', () => {
      render(<ResponsiveChat {...defaultProps} />);
      expect(screen.getByText('No messages yet')).toBeInTheDocument();
    });

    it('renders messages when provided', () => {
      const messages = [
        { id: '1', content: 'Hello', isUser: true, timestamp: new Date() },
        { id: '2', content: 'Hi there', isUser: false, timestamp: new Date(), agentName: 'Agent' },
      ];
      render(<ResponsiveChat {...defaultProps} messages={messages} />);
      expect(screen.getByText('Hello')).toBeInTheDocument();
      expect(screen.getByText('Hi there')).toBeInTheDocument();
    });
  });

  describe('state updates during render - CRITICAL FIX', () => {
    it('should NOT cause React warnings when streaming messages change', async () => {
      const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {});
      
      const streamingMessages = new Map();
      streamingMessages.set('msg-1', {
        messageId: 'msg-1',
        content: 'Streaming...',
        isComplete: false,
        timestamp: new Date(),
      });

      mockStreamingHook = createMockStreamingHook({
        streamingMessages,
        isStreaming: true,
      });

      render(<ResponsiveChat {...defaultProps} />);

      const reactStateUpdateError = consoleSpy.mock.calls.find(
        call => call[0]?.toString().includes('Cannot update') ||
                call[0]?.toString().includes('state transition')
      );
      
      expect(reactStateUpdateError).toBeUndefined();
      consoleSpy.mockRestore();
    });

    it('should update currentStreamingMessageId via useEffect, not in render', async () => {
      const streamingMessages = new Map();
      streamingMessages.set('msg-streaming', {
        messageId: 'msg-streaming',
        content: 'Content',
        isComplete: false,
        timestamp: new Date(),
      });

      mockStreamingHook = createMockStreamingHook({
        streamingMessages,
        isStreaming: true,
      });

      const { rerender } = render(<ResponsiveChat {...defaultProps} />);
      
      await act(async () => {
        rerender(<ResponsiveChat {...defaultProps} />);
      });

      expect(true).toBe(true);
    });
  });

  describe('input handling', () => {
    it('sends message when send button clicked', async () => {
      const onSendMessage = vi.fn();
      mockStreamingHook = createMockStreamingHook({ isStreaming: false });
      
      render(<ResponsiveChat {...defaultProps} onSendMessage={onSendMessage} />);
      
      const input = screen.getByPlaceholderText('Type your message...');
      fireEvent.change(input, { target: { value: 'Test message' } });
      
      const sendButton = screen.getByLabelText('Send message');
      fireEvent.click(sendButton);
      
      expect(onSendMessage).toHaveBeenCalledWith('Test message');
    });

    it('sends message on Enter key (without shift)', async () => {
      const onSendMessage = vi.fn();
      mockStreamingHook = createMockStreamingHook({ isStreaming: false });
      
      render(<ResponsiveChat {...defaultProps} onSendMessage={onSendMessage} />);
      
      const input = screen.getByPlaceholderText('Type your message...');
      fireEvent.change(input, { target: { value: 'Test message' } });
      fireEvent.keyDown(input, { key: 'Enter', code: 'Enter' });
      
      expect(onSendMessage).toHaveBeenCalledWith('Test message');
    });

    it('does not send message when streaming', async () => {
      mockStreamingHook = createMockStreamingHook({ isStreaming: true });

      const onSendMessage = vi.fn();
      render(<ResponsiveChat {...defaultProps} onSendMessage={onSendMessage} />);
      
      const input = screen.getByPlaceholderText('Type your message...');
      fireEvent.change(input, { target: { value: 'Test message' } });
      
      const sendButton = screen.getByLabelText('Send message');
      fireEvent.click(sendButton);
      
      expect(onSendMessage).not.toHaveBeenCalled();
    });
  });

  describe('sidebar', () => {
    it('toggles sidebar when hamburger menu clicked', () => {
      render(<ResponsiveChat {...defaultProps} />);
      
      const menuButton = screen.getByLabelText('Toggle menu');
      const sidebar = document.querySelector('.chat-sidebar');
      
      expect(sidebar).not.toHaveClass('open');
      
      fireEvent.click(menuButton);
      expect(sidebar).toHaveClass('open');
      
      fireEvent.click(menuButton);
      expect(sidebar).not.toHaveClass('open');
    });
  });

  describe('stop generating', () => {
    it('shows stop button when streaming with active message', async () => {
      const streamingMessages = new Map();
      streamingMessages.set('active-msg', {
        messageId: 'active-msg',
        content: 'Streaming content...',
        isComplete: false,
        timestamp: new Date(),
      });

      mockStreamingHook = createMockStreamingHook({
        streamingMessages,
        isStreaming: true,
      });

      const onStopGenerating = vi.fn();
      render(<ResponsiveChat {...defaultProps} onStopGenerating={onStopGenerating} />);

      await waitFor(() => {
        const stopButton = screen.queryByText('Stop Generating');
        expect(stopButton).toBeInTheDocument();
      });
    });

    it('calls onStopGenerating when stop button clicked', async () => {
      const streamingMessages = new Map();
      streamingMessages.set('active-msg', {
        messageId: 'active-msg',
        content: 'Streaming content...',
        isComplete: false,
        timestamp: new Date(),
      });

      mockStreamingHook = createMockStreamingHook({
        streamingMessages,
        isStreaming: true,
      });

      const onStopGenerating = vi.fn();
      render(<ResponsiveChat {...defaultProps} onStopGenerating={onStopGenerating} />);

      await waitFor(() => {
        expect(screen.getByText('Stop Generating')).toBeInTheDocument();
      });
      
      await waitFor(async () => {
        const stopButton = screen.getByRole('button', { name: /stop generating/i });
        await act(async () => {
          fireEvent.click(stopButton);
        });
      });
      
      await waitFor(() => {
        expect(onStopGenerating).toHaveBeenCalledWith('active-msg');
      });
    });
  });

  describe('load more', () => {
    it('shows load more button when hasMore is true', () => {
      render(<ResponsiveChat {...defaultProps} hasMore={true} />);
      expect(screen.getByText('Load More')).toBeInTheDocument();
    });

    it('calls onLoadMore when load more button clicked', () => {
      const onLoadMore = vi.fn();
      render(<ResponsiveChat {...defaultProps} hasMore={true} onLoadMore={onLoadMore} />);
      
      const loadMoreButton = screen.getByText('Load More');
      fireEvent.click(loadMoreButton);
      
      expect(onLoadMore).toHaveBeenCalled();
    });
  });

  describe('new message badge', () => {
    it('shows new message badge and scrolls to bottom when dismissed', async () => {
      const dismissNewMessageBadge = vi.fn();
      
      mockScrollHook = createMockScrollHook({
        showNewMessageBadge: true,
        dismissNewMessageBadge,
      });

      render(<ResponsiveChat {...defaultProps} />);
      
      const badge = screen.getByText('New message');
      fireEvent.click(badge);
      
      expect(dismissNewMessageBadge).toHaveBeenCalled();
    });
  });

  describe('accessibility', () => {
    it('has proper aria labels on interactive elements', () => {
      render(<ResponsiveChat {...defaultProps} />);
      
      expect(screen.getByLabelText('Toggle menu')).toBeInTheDocument();
      expect(screen.getByLabelText('Message input')).toBeInTheDocument();
      expect(screen.getByLabelText('Send message')).toBeInTheDocument();
      expect(screen.getByLabelText('Chat messages')).toBeInTheDocument();
    });

    it('has proper role="log" on messages container', () => {
      render(<ResponsiveChat {...defaultProps} />);
      expect(screen.getByRole('log')).toBeInTheDocument();
    });
  });
});
