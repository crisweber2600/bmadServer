import React, { useState, useEffect, useCallback } from 'react';
import { Button, Empty } from 'antd';
import { MenuOutlined, StopOutlined, DownOutlined } from '@ant-design/icons';
import { ChatMessage } from './ChatMessage';
import { TypingIndicator } from './TypingIndicator';
import { useStreamingMessage } from '../hooks/useStreamingMessage';
import { useScrollManagement } from '../hooks/useScrollManagement';
import { useTouchGestures } from '../hooks/useTouchGestures';
import '../styles/responsive-chat.css';

interface Message {
  id: string;
  content: string;
  isUser: boolean;
  timestamp: Date;
  agentName?: string;
}

export interface ResponsiveChatProps {
  messages: Message[];
  onSendMessage: (message: string) => void;
  onLoadMore?: () => void;
  onStopGenerating?: (messageId: string) => void;
  hasMore?: boolean;
  isLoading?: boolean;
}

export const ResponsiveChat: React.FC<ResponsiveChatProps> = ({
  messages,
  onSendMessage,
  onLoadMore,
  onStopGenerating,
  hasMore = false,
  isLoading = false,
}) => {
  const [inputValue, setInputValue] = useState('');
  const [isSidebarOpen, setIsSidebarOpen] = useState(false);
  const [currentStreamingMessageId, setCurrentStreamingMessageId] = useState<string | null>(null);

  const {
    streamingMessages,
    isStreaming,
    handleMessageChunk: _handleMessageChunk,
    handleGenerationStopped: _handleGenerationStopped,
  } = useStreamingMessage({
    onComplete: (message) => {
      console.log('Message complete:', message);
      setCurrentStreamingMessageId(null);
    },
  });

  const {
    scrollContainerRef,
    showNewMessageBadge,
    scrollToBottom,
    handleScroll,
    onNewMessage,
    saveScrollPosition,
    restoreScrollPosition,
    dismissNewMessageBadge,
  } = useScrollManagement({
    onScrollToTop: () => {
      if (hasMore && onLoadMore && !isLoading) {
        const position = saveScrollPosition();
        onLoadMore();
        if (position) {
          setTimeout(() => restoreScrollPosition(position), 100);
        }
      }
    },
  });

  const { attachGestureListeners, copyToClipboard } = useTouchGestures({
    onLongPress: async (element) => {
      const messageElement = element.closest('.message-bubble');
      if (messageElement) {
        const textContent = messageElement.textContent || '';
        const copied = await copyToClipboard(textContent);
        if (copied) {
          // Show visual feedback
          element.style.backgroundColor = 'rgba(24, 144, 255, 0.2)';
          setTimeout(() => {
            element.style.backgroundColor = '';
          }, 300);
        }
      }
    },
    onSwipeDown: () => {
      if (hasMore && onLoadMore && !isLoading) {
        onLoadMore();
      }
    },
  });

  // Attach gesture listeners to container
  useEffect(() => {
    const container = scrollContainerRef.current;
    if (container) {
      return attachGestureListeners(container);
    }
  }, [scrollContainerRef, attachGestureListeners]);

  // Scroll to bottom on new message
  useEffect(() => {
    if (messages.length > 0) {
      onNewMessage();
    }
  }, [messages.length, onNewMessage]);

  // Initial scroll to bottom
  useEffect(() => {
    scrollToBottom('auto');
  }, [scrollToBottom]);

  const handleSend = useCallback(() => {
    if (!inputValue.trim() || isStreaming) return;

    onSendMessage(inputValue);
    setInputValue('');
  }, [inputValue, isStreaming, onSendMessage]);

  const handleKeyPress = useCallback(
    (e: React.KeyboardEvent) => {
      if (e.key === 'Enter' && !e.shiftKey) {
        e.preventDefault();
        handleSend();
      }
    },
    [handleSend]
  );

  const renderMessages = () => {
    const allMessages = [...messages];
    let streamingMessageId: string | null = null;

    // Add streaming messages
    streamingMessages.forEach((streamingMsg) => {
      allMessages.push({
        id: streamingMsg.messageId,
        content: streamingMsg.content,
        isUser: false,
        timestamp: streamingMsg.timestamp,
        agentName: 'BMAD Agent',
      });
      if (!streamingMsg.isComplete) {
        streamingMessageId = streamingMsg.messageId;
      }
    });

    // Update current streaming message ID outside of render
    if (streamingMessageId !== currentStreamingMessageId) {
      setCurrentStreamingMessageId(streamingMessageId);
    }

    return allMessages.map((msg) => (
      <ChatMessage
        key={msg.id}
        content={msg.content}
        isUser={msg.isUser}
        timestamp={msg.timestamp}
        agentName={msg.agentName}
      />
    ));
  };

  return (
    <div className="chat-container">
      {/* Sidebar overlay for mobile */}
      <div
        className={`sidebar-overlay ${isSidebarOpen ? 'visible' : ''}`}
        onClick={() => setIsSidebarOpen(false)}
        aria-hidden="true"
      />

      {/* Sidebar - would contain workflow list, etc. */}
      <div className={`chat-sidebar ${isSidebarOpen ? 'open' : ''}`}>
        <div style={{ padding: '16px' }}>
          <h3>Workflows</h3>
          {/* Sidebar content would go here */}
        </div>
      </div>

      {/* Main chat area */}
      <div className="chat-main">
        {/* Header */}
        <div className="chat-header">
          <button
            className="hamburger-menu"
            onClick={() => setIsSidebarOpen(!isSidebarOpen)}
            aria-label="Toggle menu"
            aria-expanded={isSidebarOpen}
          >
            <MenuOutlined style={{ fontSize: '20px' }} />
          </button>
          <h1 className="chat-header-title">BMAD Chat</h1>
        </div>

        {/* Messages */}
        <div
          ref={scrollContainerRef}
          className="chat-messages swipeable"
          onScroll={handleScroll}
          role="log"
          aria-live="polite"
          aria-label="Chat messages"
        >
          {/* Load more button */}
          {hasMore && !isLoading && (
            <button
              className="load-more-button"
              onClick={onLoadMore}
              aria-label="Load more messages"
            >
              Load More
            </button>
          )}

          {/* Empty state */}
          {messages.length === 0 && !isStreaming && (
            <div className="welcome-message">
              <Empty
                description="No messages yet"
                image={Empty.PRESENTED_IMAGE_SIMPLE}
              >
                <p>Start a conversation to begin your workflow.</p>
                <div className="welcome-quick-actions">
                  <Button
                    type="primary"
                    size="large"
                    className="quick-action-button"
                    onClick={() => onSendMessage('Help me get started')}
                  >
                    Get Started
                  </Button>
                  <Button
                    size="large"
                    className="quick-action-button"
                    onClick={() => onSendMessage('Show me an example')}
                  >
                    Show Example
                  </Button>
                </div>
              </Empty>
            </div>
          )}

          {/* Messages */}
          {renderMessages()}

          {/* Typing indicator */}
          {isStreaming && <TypingIndicator agentName="BMAD Agent" />}
        </div>

        {/* New message badge */}
        {showNewMessageBadge && (
          <button
            className="new-message-badge"
            onClick={dismissNewMessageBadge}
            aria-label="New message, click to scroll to bottom"
          >
            <DownOutlined /> New message
          </button>
        )}

        {/* Input area */}
        <div className="chat-input-area">
          {isStreaming && currentStreamingMessageId && (
            <Button
              type="primary"
              danger
              icon={<StopOutlined />}
              className="stop-generating-button"
              onClick={() => {
                if (currentStreamingMessageId && onStopGenerating) {
                  onStopGenerating(currentStreamingMessageId);
                }
              }}
              style={{ marginBottom: '8px', width: '100%' }}
            >
              Stop Generating
            </Button>
          )}

          <div className="chat-input-wrapper">
            <textarea
              className="chat-input-field"
              placeholder="Type your message..."
              value={inputValue}
              onChange={(e) => setInputValue(e.target.value)}
              onKeyPress={handleKeyPress}
              disabled={isStreaming}
              aria-label="Message input"
              rows={1}
            />
            <button
              className="chat-send-button"
              onClick={handleSend}
              disabled={isStreaming || !inputValue.trim()}
              aria-label="Send message"
            >
              Send
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};
