import React, { useState, useEffect, useRef, useCallback } from 'react';
import { Input, Button, Space, Typography } from 'antd';
import { SendOutlined, StopOutlined } from '@ant-design/icons';
import { CommandPalette } from './CommandPalette';
import './ChatInput.css';

const { Text } = Typography;
const { TextArea } = Input;

const MAX_CHARS = 2000;
const DRAFT_STORAGE_KEY = 'bmad-chat-draft';
const DEBOUNCE_DELAY = 500;
const CANCEL_BUTTON_DELAY = 5000;

export interface ChatInputProps {
  onSend: (message: string, abortSignal?: AbortSignal) => Promise<void>;
  disabled?: boolean;
  placeholder?: string;
}

export const ChatInput: React.FC<ChatInputProps> = ({
  onSend,
  disabled = false,
  placeholder = 'Type a message... (Ctrl+Enter to send)',
}) => {
  const [message, setMessage] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [showCancelButton, setShowCancelButton] = useState(false);
  const [showCommandPalette, setShowCommandPalette] = useState(false);
  const [commandPalettePosition, setCommandPalettePosition] = useState({ top: 0, left: 0 });
  const [error, setError] = useState<string | null>(null);
  
  const textAreaRef = useRef<HTMLTextAreaElement>(null);
  const debounceTimerRef = useRef<NodeJS.Timeout | null>(null);
  const cancelTimerRef = useRef<NodeJS.Timeout | null>(null);
  const abortControllerRef = useRef<AbortController | null>(null);

  // Load draft from localStorage on mount
  useEffect(() => {
    try {
      const draft = localStorage.getItem(DRAFT_STORAGE_KEY);
      if (draft) {
        setMessage(draft);
      }
    } catch (err) {
      console.error('Failed to load draft from localStorage:', err);
    }
  }, []);

  // Save draft to localStorage (debounced)
  const saveDraft = useCallback((text: string) => {
    if (debounceTimerRef.current) {
      clearTimeout(debounceTimerRef.current);
    }

    debounceTimerRef.current = setTimeout(() => {
      try {
        if (text) {
          localStorage.setItem(DRAFT_STORAGE_KEY, text);
        } else {
          localStorage.removeItem(DRAFT_STORAGE_KEY);
        }
      } catch (err) {
        console.error('Failed to save draft to localStorage:', err);
      }
    }, DEBOUNCE_DELAY);
  }, []);

  // Clear draft from localStorage
  const clearDraft = useCallback(() => {
    try {
      localStorage.removeItem(DRAFT_STORAGE_KEY);
    } catch (err) {
      console.error('Failed to clear draft from localStorage:', err);
    }
  }, []);

  // Handle message change
  const handleMessageChange = (e: React.ChangeEvent<HTMLTextAreaElement>) => {
    const newMessage = e.target.value;
    setMessage(newMessage);
    saveDraft(newMessage);
    setError(null);

    // Check if command palette should be shown
    if (newMessage.endsWith('/')) {
      const textarea = textAreaRef.current;
      if (textarea && typeof textarea.getBoundingClientRect === 'function') {
        const { selectionStart } = textarea;
        const rect = textarea.getBoundingClientRect();
        // Simple position calculation - in a real app, you'd calculate exact caret position
        setCommandPalettePosition({
          top: rect.top - 200,
          left: rect.left,
        });
        setShowCommandPalette(true);
      }
    } else {
      setShowCommandPalette(false);
    }
  };

  // Handle command selection
  const handleCommandSelect = (command: string) => {
    setMessage((prev) => {
      const newMessage = prev.slice(0, -1) + command;
      saveDraft(newMessage);
      return newMessage;
    });
    setShowCommandPalette(false);
    textAreaRef.current?.focus();
  };

  // Handle send
  const handleSend = async () => {
    if (!message.trim() || message.length > MAX_CHARS || isLoading || disabled) {
      return;
    }

    const messageToSend = message.trim();
    setMessage('');
    clearDraft();
    setIsLoading(true);
    setError(null);
    setShowCancelButton(false);

    // Create AbortController for request cancellation
    const abortController = new AbortController();
    abortControllerRef.current = abortController;

    // Show cancel button after 5 seconds
    cancelTimerRef.current = setTimeout(() => {
      setShowCancelButton(true);
    }, CANCEL_BUTTON_DELAY);

    try {
      await onSend(messageToSend, abortController.signal);
    } catch (err) {
      if (err instanceof Error) {
        if (err.name === 'AbortError') {
          setError('Request cancelled');
        } else {
          setError(err.message || 'Failed to send message');
        }
      } else {
        setError('An unexpected error occurred');
      }
    } finally {
      setIsLoading(false);
      setShowCancelButton(false);
      if (cancelTimerRef.current) {
        clearTimeout(cancelTimerRef.current);
        cancelTimerRef.current = null;
      }
      abortControllerRef.current = null;
    }
  };

  // Handle cancel
  const handleCancel = () => {
    if (abortControllerRef.current) {
      abortControllerRef.current.abort();
      setShowCancelButton(false);
      if (cancelTimerRef.current) {
        clearTimeout(cancelTimerRef.current);
        cancelTimerRef.current = null;
      }
    }
  };

  // Handle keyboard shortcuts
  const handleKeyDown = (e: React.KeyboardEvent<HTMLTextAreaElement>) => {
    // Ctrl+Enter or Cmd+Enter to send
    if ((e.ctrlKey || e.metaKey) && e.key === 'Enter') {
      e.preventDefault();
      handleSend();
    }

    // Close command palette on Escape
    if (e.key === 'Escape' && showCommandPalette) {
      setShowCommandPalette(false);
    }
  };

  // Cleanup on unmount
  useEffect(() => {
    return () => {
      if (debounceTimerRef.current) {
        clearTimeout(debounceTimerRef.current);
      }
      if (cancelTimerRef.current) {
        clearTimeout(cancelTimerRef.current);
      }
    };
  }, []);

  const charCount = message.length;
  const isOverLimit = charCount > MAX_CHARS;
  const isSendDisabled = !message.trim() || isOverLimit || isLoading || disabled;

  return (
    <div className="chat-input-container" role="region" aria-label="Message input">
      <Space vertical style={{ width: '100%' }} size="small">
        {error && (
          <Text type="danger" role="alert" aria-live="assertive">
            {error}
          </Text>
        )}
        
        <div className="chat-input-wrapper">
          <TextArea
            ref={textAreaRef}
            value={message}
            onChange={handleMessageChange}
            onKeyDown={handleKeyDown}
            placeholder={placeholder}
            disabled={disabled || isLoading}
            autoSize={{ minRows: 2, maxRows: 6 }}
            maxLength={undefined}
            className={isOverLimit ? 'chat-input-over-limit' : ''}
            aria-label="Message input"
            aria-describedby="char-count-display keyboard-hint-display"
            aria-invalid={isOverLimit}
          />
          
          {showCommandPalette && (
            <CommandPalette
              onSelect={handleCommandSelect}
              onClose={() => setShowCommandPalette(false)}
              position={commandPalettePosition}
            />
          )}
        </div>

        <div className="chat-input-footer">
          <Space size="small">
            <Text
              id="char-count-display"
              type={isOverLimit ? 'danger' : 'secondary'}
              className={isOverLimit ? 'char-count-over-limit' : 'char-count'}
              role="status"
              aria-live="polite"
            >
              {charCount}/{MAX_CHARS}
            </Text>
            
            <Text
              id="keyboard-hint-display"
              type="secondary"
              className="keyboard-hint"
            >
              {navigator.platform.toLowerCase().includes('mac') ? '⌘' : 'Ctrl'}+Enter to send
            </Text>
          </Space>

          <Space size="small">
            {showCancelButton && (
              <Button
                icon={<StopOutlined />}
                onClick={handleCancel}
                danger
                aria-label="Cancel request"
              >
                Cancel
              </Button>
            )}
            
            <Button
              type="primary"
              icon={<SendOutlined />}
              onClick={handleSend}
              disabled={isSendDisabled}
              loading={isLoading}
              aria-label="Send message"
            >
              Send
            </Button>
          </Space>
        </div>
      </Space>
    </div>
  );
};
