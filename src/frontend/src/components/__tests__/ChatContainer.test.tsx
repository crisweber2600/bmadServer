import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import '@testing-library/jest-dom';
import { ChatContainer } from '../ChatContainer';

// Mock SignalR connection
const mockConnection = {
  on: vi.fn(),
  off: vi.fn(),
  invoke: vi.fn(),
  start: vi.fn().mockResolvedValue(undefined),
  stop: vi.fn().mockResolvedValue(undefined),
  state: 'Connected',
};

vi.mock('@microsoft/signalr', () => ({
  HubConnectionBuilder: class MockHubConnectionBuilder {
    withUrl = vi.fn().mockReturnThis();
    withAutomaticReconnect = vi.fn().mockReturnThis();
    configureLogging = vi.fn().mockReturnThis();
    build = vi.fn(() => mockConnection);
  },
  LogLevel: {
    Information: 1,
  },
}));

describe('ChatContainer - History & Scroll Management', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    // Mock scrollIntoView
    Element.prototype.scrollIntoView = vi.fn();
  });

  it('should load last 50 messages on mount', async () => {
    const mockMessages = Array.from({ length: 50 }, (_, i) => ({
      id: `msg-${i}`,
      role: i % 2 === 0 ? 'user' : 'agent',
      content: `Message ${i + 1}`,
      timestamp: new Date(Date.now() - (50 - i) * 60000).toISOString(),
    }));

    mockConnection.invoke.mockResolvedValueOnce({
      messages: mockMessages,
      totalCount: 100,
      hasMore: true,
      offset: 0,
      pageSize: 50,
    });

    render(<ChatContainer />);

    await waitFor(() => {
      expect(mockConnection.invoke).toHaveBeenCalledWith('GetChatHistory', 50, 0);
    });

    // Should display messages
    expect(screen.getByText('Message 1')).toBeInTheDocument();
    expect(screen.getByText('Message 50')).toBeInTheDocument();
  });

  it('should show "Load More" button when hasMore is true', async () => {
    mockConnection.invoke.mockResolvedValueOnce({
      messages: [],
      totalCount: 100,
      hasMore: true,
      offset: 0,
      pageSize: 50,
    });

    render(<ChatContainer />);

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /load more/i })).toBeInTheDocument();
    });
  });

  it('should load more messages when "Load More" is clicked', async () => {
    const user = userEvent.setup();

    // Initial load
    mockConnection.invoke.mockResolvedValueOnce({
      messages: [{ id: '1', role: 'user', content: 'Recent', timestamp: new Date().toISOString() }],
      totalCount: 100,
      hasMore: true,
      offset: 0,
      pageSize: 50,
    });

    render(<ChatContainer />);

    await waitFor(() => {
      expect(screen.getByText('Recent')).toBeInTheDocument();
    });

    // Load more
    mockConnection.invoke.mockResolvedValueOnce({
      messages: [{ id: '2', role: 'user', content: 'Older', timestamp: new Date(Date.now() - 60000).toISOString() }],
      totalCount: 100,
      hasMore: false,
      offset: 50,
      pageSize: 50,
    });

    const loadMoreButton = screen.getByRole('button', { name: /load more/i });
    await user.click(loadMoreButton);

    await waitFor(() => {
      expect(mockConnection.invoke).toHaveBeenCalledWith('GetChatHistory', 50, 50);
      expect(screen.getByText('Older')).toBeInTheDocument();
    });
  });

  it('should scroll to bottom on initial load', async () => {
    mockConnection.invoke.mockResolvedValueOnce({
      messages: [{ id: '1', role: 'user', content: 'Test', timestamp: new Date().toISOString() }],
      totalCount: 1,
      hasMore: false,
      offset: 0,
      pageSize: 50,
    });

    render(<ChatContainer />);

    await waitFor(() => {
      expect(Element.prototype.scrollIntoView).toHaveBeenCalled();
    });
  });

  it('should show "New message" badge when scrolled up and new message arrives', async () => {
    mockConnection.invoke.mockResolvedValueOnce({
      messages: [],
      totalCount: 0,
      hasMore: false,
      offset: 0,
      pageSize: 50,
    });

    render(<ChatContainer />);

    // Simulate scrolled up state (mock scrollTop not at bottom)
    Object.defineProperty(HTMLElement.prototype, 'scrollTop', {
      configurable: true,
      get: () => 0,
    });
    Object.defineProperty(HTMLElement.prototype, 'scrollHeight', {
      configurable: true,
      get: () => 1000,
    });
    Object.defineProperty(HTMLElement.prototype, 'clientHeight', {
      configurable: true,
      get: () => 500,
    });

    // Trigger new message event
    const receiveMessageCallback = mockConnection.on.mock.calls.find(
      (call) => call[0] === 'ReceiveMessage'
    )?.[1];

    if (receiveMessageCallback) {
      receiveMessageCallback({
        role: 'agent',
        content: 'New message',
        timestamp: new Date().toISOString(),
      });
    }

    await waitFor(() => {
      expect(screen.getByText(/new message/i)).toBeInTheDocument();
    });
  });

  it('should show welcome message for empty chat history', async () => {
    mockConnection.invoke.mockResolvedValueOnce({
      messages: [],
      totalCount: 0,
      hasMore: false,
      offset: 0,
      pageSize: 50,
    });

    render(<ChatContainer />);

    await waitFor(() => {
      expect(screen.getByText(/welcome/i)).toBeInTheDocument();
    });
  });
});
