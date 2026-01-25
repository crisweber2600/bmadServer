import React, { useState, useEffect, useRef, KeyboardEvent } from 'react';
import { Button, Input } from 'antd';
import { SendOutlined, CloseCircleOutlined } from '@ant-design/icons';
import './ChatInput.css';

const { TextArea } = Input;

const MAX_CHARS = 2000;
const DRAFT_KEY = 'bmad-chat-draft';

const COMMANDS = [
  { value: '/help', description: 'Show available commands' },
  { value: '/status', description: 'Check workflow status' },
  { value: '/pause', description: 'Pause current workflow' },
  { value: '/resume', description: 'Resume paused workflow' },
];

export interface ChatInputProps {
  /**
   * Callback when user sends a message
   */
  onSend: (message: string) => void;

  /**
   * Optional callback when user cancels a request
   */
  onCancel?: () => void;

  /**
   * Whether the server is currently processing a request
   */
  isProcessing?: boolean;
}

/**
 * ChatInput component provides a rich text input interface for chat.
 * Features:
 * - Multi-line text input with character count
 * - Send button (disabled when empty or over limit)
 * - Ctrl+Enter/Cmd+Enter keyboard shortcut
 * - Draft message persistence in localStorage
 * - Command palette with /help, /status, /pause, /resume
 * - Cancel button for slow requests
 */
export const ChatInput: React.FC<ChatInputProps> = ({ onSend, onCancel, isProcessing = false }) => {
  const [value, setValue] = useState<string>('');
  const [showCommands, setShowCommands] = useState<boolean>(false);
  const [selectedCommandIndex, setSelectedCommandIndex] = useState<number>(0);
  const textareaRef = useRef<HTMLTextAreaElement>(null);

  // Restore draft from localStorage on mount
  useEffect(() => {
    const savedDraft = localStorage.getItem(DRAFT_KEY);
    if (savedDraft) {
      setValue(savedDraft);
    }
  }, []);

  // Save draft to localStorage on value change
  useEffect(() => {
    if (value) {
      localStorage.setItem(DRAFT_KEY, value);
    } else {
      localStorage.removeItem(DRAFT_KEY);
    }
  }, [value]);

  // Check if value starts with '/' to show command palette
  useEffect(() => {
    const trimmedValue = value.trim();
    if (trimmedValue.startsWith('/') && trimmedValue.length > 0) {
      setShowCommands(true);
      setSelectedCommandIndex(0);
    } else {
      setShowCommands(false);
    }
  }, [value]);

  const characterCount = value.length;
  const isOverLimit = characterCount > MAX_CHARS;
  const isEmpty = value.trim().length === 0;
  const isSendDisabled = isEmpty || isOverLimit || isProcessing;

  const handleSend = () => {
    if (!isSendDisabled) {
      onSend(value.trim());
      setValue('');
      localStorage.removeItem(DRAFT_KEY);
    }
  };

  const handleKeyDown = (e: KeyboardEvent<HTMLTextAreaElement>) => {
    // Handle Ctrl+Enter or Cmd+Enter to send
    if ((e.ctrlKey || e.metaKey) && e.key === 'Enter') {
      e.preventDefault();
      handleSend();
      return;
    }

    // Handle command palette navigation
    if (showCommands) {
      const filteredCommands = getFilteredCommands();
      if (e.key === 'ArrowDown') {
        e.preventDefault();
        setSelectedCommandIndex((prev) => Math.min(prev + 1, filteredCommands.length - 1));
      } else if (e.key === 'ArrowUp') {
        e.preventDefault();
        setSelectedCommandIndex((prev) => Math.max(prev - 1, 0));
      } else if (e.key === 'Enter' && filteredCommands.length > 0) {
        e.preventDefault();
        selectCommand(filteredCommands[selectedCommandIndex].value);
      } else if (e.key === 'Escape') {
        e.preventDefault();
        setShowCommands(false);
      }
    }
  };

  const getFilteredCommands = () => {
    const query = value.trim().toLowerCase();
    if (query === '/') {
      return COMMANDS;
    }
    return COMMANDS.filter((cmd) => cmd.value.toLowerCase().startsWith(query));
  };

  const selectCommand = (command: string) => {
    setValue(command);
    setShowCommands(false);
    onSend(command);
    setValue('');
    localStorage.removeItem(DRAFT_KEY);
  };

  const getShortcutHint = () => {
    const isMac = navigator.platform.toUpperCase().indexOf('MAC') >= 0;
    return isMac ? 'Cmd+Enter to send' : 'Ctrl+Enter to send';
  };

  const filteredCommands = showCommands ? getFilteredCommands() : [];

  return (
    <div className="chat-input-container">
      {showCommands && filteredCommands.length > 0 && (
        <div
          className="command-palette"
          role="listbox"
          aria-label="Available commands"
        >
          {filteredCommands.map((cmd, index) => (
            <div
              key={cmd.value}
              className={`command-option ${index === selectedCommandIndex ? 'command-option-selected' : ''}`}
              role="option"
              aria-selected={index === selectedCommandIndex}
              onClick={() => selectCommand(cmd.value)}
            >
              <span className="command-value">{cmd.value}</span>
              <span className="command-description">{cmd.description}</span>
            </div>
          ))}
        </div>
      )}
      <div className="chat-input-wrapper">
        <TextArea
          ref={textareaRef}
          value={value}
          onChange={(e) => setValue(e.target.value)}
          onKeyDown={handleKeyDown}
          placeholder="Type your message..."
          aria-label="Message input"
          disabled={isProcessing}
          autoSize={{ minRows: 2, maxRows: 6 }}
          className="chat-input-textarea"
        />
        <div className="chat-input-footer">
          <span
            className={`character-count ${isOverLimit ? 'character-count-exceeded' : ''}`}
            aria-live="polite"
            aria-label={`Character count: ${characterCount} of ${MAX_CHARS}`}
          >
            {characterCount} / {MAX_CHARS}
          </span>
          <span className="keyboard-hint">{getShortcutHint()}</span>
        </div>
      </div>
      <div className="chat-input-actions">
        {isProcessing && onCancel ? (
          <Button
            type="default"
            icon={<CloseCircleOutlined />}
            onClick={onCancel}
            aria-label="Cancel request"
          >
            Cancel
          </Button>
        ) : (
          <Button
            type="primary"
            icon={<SendOutlined />}
            onClick={handleSend}
            disabled={isSendDisabled}
            aria-label="Send message"
          >
            Send
          </Button>
        )}
      </div>
    </div>
  );
};
