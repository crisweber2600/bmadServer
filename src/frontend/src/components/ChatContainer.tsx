import React, { useState, useEffect, useRef, useCallback } from 'react';
import { Button, Badge, Empty, Spin } from 'antd';
import { ArrowDownOutlined, DownOutlined } from '@ant-design/icons';
import { ChatMessage, TypingIndicator } from './ChatMessage';
import { ChatInput } from './ChatInput';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import './ChatContainer.css';

interface Message {
  id: string;
  role: 'user' | 'agent';
  content: string;
  timestamp: string;
  agentName?: string;
}

interface ChatHistoryResponse {
  messages: Message[];
  totalCount: number;
  hasMore: boolean;
  offset: number;
  pageSize: number;
}

const SCROLL_POSITION_KEY = 'bmad-chat-scroll-position';
const PAGE_SIZE = 50;

export const ChatContainer: React.FC = () => {
  const [messages, setMessages] = useState<Message[]>([]);
  const [connection, setConnection] = useState<HubConnection | null>(null);
  const [isConnected, setIsConnected] = useState(false);
  const [isProcessing, setIsProcessing] = useState(false);
  const [isLoadingHistory, setIsLoadingHistory] = useState(false);
  const [hasMore, setHasMore] = useState(false);
  const [offset, setOffset] = useState(0);
  const [totalCount, setTotalCount] = useState(0);
  const [showNewMessageBadge, setShowNewMessageBadge] = useState(false);
  const [unreadCount, setUnreadCount] = useState(0);
  const [isUserScrolling, setIsUserScrolling] = useState(false);
  const [currentMessageId, setCurrentMessageId] = useState<string | null>(null);

  const messagesEndRef = useRef<HTMLDivElement>(null);
  const messagesContainerRef = useRef<HTMLDivElement>(null);
  const previousScrollHeightRef = useRef<number>(0);

  // Scroll to bottom smoothly
  const scrollToBottom = useCallback((smooth = true) => {
    messagesEndRef.current?.scrollIntoView({ behavior: smooth ? 'smooth' : 'auto' });
    setShowNewMessageBadge(false);
    setUnreadCount(0);
    setIsUserScrolling(false);
  }, []);

  // Check if user is at bottom of chat
  const isAtBottom = useCallback(() => {
    if (!messagesContainerRef.current) return true;
    const { scrollTop, scrollHeight, clientHeight } = messagesContainerRef.current;
    return scrollHeight - scrollTop - clientHeight < 100; // 100px threshold
  }, []);

  // Handle scroll position persistence
  const saveScrollPosition = useCallback(() => {
    if (messagesContainerRef.current) {
      sessionStorage.setItem(
        SCROLL_POSITION_KEY,
        messagesContainerRef.current.scrollTop.toString()
      );
    }
  }, []);

  const restoreScrollPosition = useCallback(() => {
    const savedPosition = sessionStorage.getItem(SCROLL_POSITION_KEY);
    if (savedPosition && messagesContainerRef.current) {
      messagesContainerRef.current.scrollTop = parseInt(savedPosition, 10);
    }
  }, []);

  // Load chat history
  const loadChatHistory = useCallback(
    async (loadOffset: number = 0) => {
      if (!connection || isLoadingHistory) return;

      setIsLoadingHistory(true);
      try {
        const response: ChatHistoryResponse = await connection.invoke(
          'GetChatHistory',
          PAGE_SIZE,
          loadOffset
        );

        if (loadOffset === 0) {
          // Initial load - newest messages
          setMessages(response.messages.reverse()); // Reverse to show oldest first
          setOffset(response.pageSize);
          scrollToBottom(false); // Instant scroll on initial load
        } else {
          // Load more - older messages
          const container = messagesContainerRef.current;
          if (container) {
            previousScrollHeightRef.current = container.scrollHeight;
          }

          setMessages((prev) => [...response.messages.reverse(), ...prev]);
          setOffset(loadOffset + response.pageSize);

          // Restore scroll position after loading more (prevent jump)
          setTimeout(() => {
            if (container) {
              const newScrollHeight = container.scrollHeight;
              const scrollDiff = newScrollHeight - previousScrollHeightRef.current;
              container.scrollTop = scrollDiff;
            }
          }, 0);
        }

        setHasMore(response.hasMore);
        setTotalCount(response.totalCount);
      } catch (error) {
        console.error('Failed to load chat history:', error);
      } finally {
        setIsLoadingHistory(false);
      }
    },
    [connection, isLoadingHistory, scrollToBottom]
  );

  // Handle scroll events
  const handleScroll = useCallback(() => {
    if (isAtBottom()) {
      setShowNewMessageBadge(false);
      setUnreadCount(0);
      setIsUserScrolling(false);
    } else {
      setIsUserScrolling(true);
    }
    saveScrollPosition();
  }, [isAtBottom, saveScrollPosition]);

  // Setup SignalR connection
  useEffect(() => {
    const newConnection = new HubConnectionBuilder()
      .withUrl('/chathub', {
        accessTokenFactory: () => localStorage.getItem('token') || '',
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Information)
      .build();

    setConnection(newConnection);

    return () => {
      newConnection.stop();
    };
  }, []);

  // Start connection and load history
  useEffect(() => {
    if (connection) {
      connection
        .start()
        .then(() => {
          setIsConnected(true);
          loadChatHistory(0);
        })
        .catch((err) => console.error('Connection failed:', err));

      // Listen for new messages
      connection.on('ReceiveMessage', (message: Message) => {
        setMessages((prev) => [...prev, message]);
        
        if (isAtBottom()) {
          scrollToBottom();
        } else {
          setShowNewMessageBadge(true);
          setUnreadCount((count) => count + 1);
        }
      });

      // Listen for message chunks (streaming)
      connection.on('MESSAGE_CHUNK', (data: any) => {
        // Track current streaming message
        if (!data.IsComplete) {
          setCurrentMessageId(data.MessageId);
        } else {
          setCurrentMessageId(null);
        }
        
        // Handle streaming messages
        setMessages((prev) => {
          const existing = prev.find((m) => m.id === data.MessageId);
          if (existing) {
            return prev.map((m) =>
              m.id === data.MessageId
                ? { ...m, content: m.content + data.Chunk }
                : m
            );
          } else {
            return [
              ...prev,
              {
                id: data.MessageId,
                role: 'agent',
                content: data.Chunk,
                timestamp: data.Timestamp,
                agentName: data.AgentId,
              },
            ];
          }
        });

        if (isAtBottom()) {
          scrollToBottom();
        }
      });

      return () => {
        connection.off('ReceiveMessage');
        connection.off('MESSAGE_CHUNK');
      };
    }
  }, [connection, loadChatHistory, isAtBottom, scrollToBottom]);

  // Handle send message
  const handleSendMessage = async (message: string) => {
    if (!connection || !isConnected) return;

    setIsProcessing(true);
    try {
      await connection.invoke('SendMessageStreaming', message);
      scrollToBottom();
    } catch (error) {
      console.error('Failed to send message:', error);
    } finally {
      setIsProcessing(false);
    }
  };

  // Handle cancel
  const handleCancel = async () => {
    if (!connection || !currentMessageId) return;
    
    try {
      await connection.invoke('StopGenerating', currentMessageId);
      setIsProcessing(false);
      setCurrentMessageId(null);
    } catch (error) {
      console.error('Failed to cancel message:', error);
    }
  };

  // Welcome message for empty chat
  const renderWelcomeMessage = () => (
    <Empty
      description={
        <div>
          <h2>Welcome to BMAD Server! 👋</h2>
          <p>Start a conversation to begin your workflow journey.</p>
        </div>
      }
    >
      <div style={{ marginTop: '16px' }}>
        <Button type="primary" onClick={() => handleSendMessage('/help')}>
          Quick Start
        </Button>
      </div>
    </Empty>
  );

  return (
    <div className="chat-container">
      <div
        className="chat-messages"
        ref={messagesContainerRef}
        onScroll={handleScroll}
      >
        {/* Load More Button */}
        {hasMore && (
          <div className="load-more-container">
            <Button
              type="link"
              icon={<DownOutlined />}
              loading={isLoadingHistory}
              onClick={() => loadChatHistory(offset)}
            >
              Load More ({totalCount - messages.length} older messages)
            </Button>
          </div>
        )}

        {/* Welcome Message */}
        {messages.length === 0 && !isLoadingHistory && renderWelcomeMessage()}

        {/* Messages */}
        {messages.map((msg) => (
          <ChatMessage
            key={msg.id}
            id={msg.id}
            role={msg.role}
            content={msg.content}
            timestamp={new Date(msg.timestamp)}
            agentName={msg.agentName}
          />
        ))}

        {/* Typing Indicator */}
        {isProcessing && <TypingIndicator />}

        {/* Scroll anchor */}
        <div ref={messagesEndRef} />
      </div>

      {/* New Message Badge */}
      {showNewMessageBadge && (
        <div className="new-message-badge" onClick={() => scrollToBottom()}>
          <Badge count={unreadCount} offset={[-5, 5]}>
            <Button
              type="primary"
              shape="circle"
              icon={<ArrowDownOutlined />}
              aria-label={`${unreadCount} new message${unreadCount > 1 ? 's' : ''}`}
            />
          </Badge>
          <span className="badge-text">New message{unreadCount > 1 ? 's' : ''}</span>
        </div>
      )}

      {/* Chat Input */}
      <ChatInput
        onSend={handleSendMessage}
        onCancel={handleCancel}
        isProcessing={isProcessing}
      />
    </div>
  );
};
