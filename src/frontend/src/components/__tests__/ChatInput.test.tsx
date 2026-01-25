import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import '@testing-library/jest-dom';
import { ChatInput } from '../ChatInput';

describe('ChatInput', () => {
  const mockOnSend = vi.fn();
  const mockOnCancel = vi.fn();

  beforeEach(() => {
    vi.clearAllMocks();
    localStorage.clear();
  });

  describe('Basic Rendering', () => {
    it('should render multi-line text input', () => {
      render(<ChatInput onSend={mockOnSend} />);
      
      const textarea = screen.getByRole('textbox');
      expect(textarea).toBeInTheDocument();
      expect(textarea.tagName).toBe('TEXTAREA');
    });

    it('should render Send button disabled when empty', () => {
      render(<ChatInput onSend={mockOnSend} />);
      
      const sendButton = screen.getByRole('button', { name: /send/i });
      expect(sendButton).toBeInTheDocument();
      expect(sendButton).toBeDisabled();
    });

    it('should show character count', () => {
      render(<ChatInput onSend={mockOnSend} />);
      
      expect(screen.getByText(/0\s*\/\s*2000/)).toBeInTheDocument();
    });

    it('should show keyboard shortcut hint', () => {
      render(<ChatInput onSend={mockOnSend} />);
      
      // Check for hint about Ctrl+Enter or Cmd+Enter
      const hint = screen.getByText(/Ctrl\+Enter|Cmd\+Enter/i);
      expect(hint).toBeInTheDocument();
    });
  });

  describe('Send Button State', () => {
    it('should enable Send button when text is entered', async () => {
      const user = userEvent.setup();
      render(<ChatInput onSend={mockOnSend} />);
      
      const textarea = screen.getByRole('textbox');
      const sendButton = screen.getByRole('button', { name: /send/i });
      
      await user.type(textarea, 'Hello BMAD');
      
      expect(sendButton).not.toBeDisabled();
    });

    it('should disable Send button when text is cleared', async () => {
      const user = userEvent.setup();
      render(<ChatInput onSend={mockOnSend} />);
      
      const textarea = screen.getByRole('textbox');
      const sendButton = screen.getByRole('button', { name: /send/i });
      
      await user.type(textarea, 'Hello');
      expect(sendButton).not.toBeDisabled();
      
      await user.clear(textarea);
      expect(sendButton).toBeDisabled();
    });

    it('should call onSend with message text when Send is clicked', async () => {
      const user = userEvent.setup();
      render(<ChatInput onSend={mockOnSend} />);
      
      const textarea = screen.getByRole('textbox');
      const sendButton = screen.getByRole('button', { name: /send/i });
      
      await user.type(textarea, 'Test message');
      await user.click(sendButton);
      
      expect(mockOnSend).toHaveBeenCalledWith('Test message');
    });

    it('should clear input field after sending', async () => {
      const user = userEvent.setup();
      render(<ChatInput onSend={mockOnSend} />);
      
      const textarea = screen.getByRole('textbox') as HTMLTextAreaElement;
      const sendButton = screen.getByRole('button', { name: /send/i });
      
      await user.type(textarea, 'Test message');
      await user.click(sendButton);
      
      expect(textarea.value).toBe('');
    });
  });

  describe('Character Count', () => {
    it('should update character count as user types', async () => {
      const user = userEvent.setup();
      render(<ChatInput onSend={mockOnSend} />);
      
      const textarea = screen.getByRole('textbox');
      
      await user.type(textarea, 'Hello');
      
      expect(screen.getByText(/5\s*\/\s*2000/)).toBeInTheDocument();
    });

    it('should turn character count red when exceeding 2000 characters', async () => {
      render(<ChatInput onSend={mockOnSend} />);
      
      const textarea = screen.getByRole('textbox') as HTMLTextAreaElement;
      const longText = 'a'.repeat(2001);
      
      // Directly set value to avoid slow typing
      fireEvent.change(textarea, { target: { value: longText } });
      
      await waitFor(() => {
        const counter = screen.getByText(/2001\s*\/\s*2000/);
        expect(counter).toHaveClass('character-count-exceeded');
      });
    }, 10000);

    it('should disable Send button when exceeding 2000 characters', async () => {
      render(<ChatInput onSend={mockOnSend} />);
      
      const textarea = screen.getByRole('textbox') as HTMLTextAreaElement;
      const sendButton = screen.getByRole('button', { name: /send/i });
      const longText = 'a'.repeat(2001);
      
      // Directly set value to avoid slow typing
      fireEvent.change(textarea, { target: { value: longText } });
      
      await waitFor(() => {
        expect(sendButton).toBeDisabled();
      });
    }, 10000);
  });

  describe('Keyboard Shortcuts', () => {
    it('should send message on Ctrl+Enter', async () => {
      render(<ChatInput onSend={mockOnSend} />);
      
      const textarea = screen.getByRole('textbox') as HTMLTextAreaElement;
      
      fireEvent.change(textarea, { target: { value: 'Test message' } });
      fireEvent.keyDown(textarea, { key: 'Enter', ctrlKey: true });
      
      await waitFor(() => {
        expect(mockOnSend).toHaveBeenCalledWith('Test message');
      });
    });

    it('should send message on Cmd+Enter (Mac)', async () => {
      render(<ChatInput onSend={mockOnSend} />);
      
      const textarea = screen.getByRole('textbox') as HTMLTextAreaElement;
      
      fireEvent.change(textarea, { target: { value: 'Test message' } });
      fireEvent.keyDown(textarea, { key: 'Enter', metaKey: true });
      
      await waitFor(() => {
        expect(mockOnSend).toHaveBeenCalledWith('Test message');
      });
    });

    it('should not send on Enter without modifier key', async () => {
      render(<ChatInput onSend={mockOnSend} />);
      
      const textarea = screen.getByRole('textbox');
      
      await userEvent.type(textarea, 'Test message{Enter}');
      
      expect(mockOnSend).not.toHaveBeenCalled();
    });
  });

  describe('Draft Message Persistence', () => {
    it('should save draft to localStorage on input change', async () => {
      const user = userEvent.setup();
      render(<ChatInput onSend={mockOnSend} />);
      
      const textarea = screen.getByRole('textbox');
      
      await user.type(textarea, 'Draft message');
      
      await waitFor(() => {
        const savedDraft = localStorage.getItem('bmad-chat-draft');
        expect(savedDraft).toBe('Draft message');
      });
    });

    it('should restore draft from localStorage on mount', () => {
      localStorage.setItem('bmad-chat-draft', 'Restored draft');
      
      render(<ChatInput onSend={mockOnSend} />);
      
      const textarea = screen.getByRole('textbox') as HTMLTextAreaElement;
      expect(textarea.value).toBe('Restored draft');
    });

    it('should clear draft from localStorage after sending', async () => {
      const user = userEvent.setup();
      localStorage.setItem('bmad-chat-draft', 'Draft to send');
      
      render(<ChatInput onSend={mockOnSend} />);
      
      const sendButton = screen.getByRole('button', { name: /send/i });
      await user.click(sendButton);
      
      expect(localStorage.getItem('bmad-chat-draft')).toBeNull();
    });
  });

  describe('Command Palette', () => {
    it('should show command palette when typing /', async () => {
      const user = userEvent.setup();
      render(<ChatInput onSend={mockOnSend} />);
      
      const textarea = screen.getByRole('textbox');
      
      await user.type(textarea, '/');
      
      expect(screen.getByRole('listbox')).toBeInTheDocument();
    });

    it('should show /help command in palette', async () => {
      const user = userEvent.setup();
      render(<ChatInput onSend={mockOnSend} />);
      
      const textarea = screen.getByRole('textbox');
      
      await user.type(textarea, '/');
      
      expect(screen.getByText('/help')).toBeInTheDocument();
    });

    it('should show /status command in palette', async () => {
      const user = userEvent.setup();
      render(<ChatInput onSend={mockOnSend} />);
      
      const textarea = screen.getByRole('textbox');
      
      await user.type(textarea, '/');
      
      expect(screen.getByText('/status')).toBeInTheDocument();
    });

    it('should show /pause command in palette', async () => {
      const user = userEvent.setup();
      render(<ChatInput onSend={mockOnSend} />);
      
      const textarea = screen.getByRole('textbox');
      
      await user.type(textarea, '/');
      
      expect(screen.getByText('/pause')).toBeInTheDocument();
    });

    it('should show /resume command in palette', async () => {
      const user = userEvent.setup();
      render(<ChatInput onSend={mockOnSend} />);
      
      const textarea = screen.getByRole('textbox');
      
      await user.type(textarea, '/');
      
      expect(screen.getByText('/resume')).toBeInTheDocument();
    });

    it('should filter commands based on input', async () => {
      const user = userEvent.setup();
      render(<ChatInput onSend={mockOnSend} />);
      
      const textarea = screen.getByRole('textbox');
      
      await user.type(textarea, '/hel');
      
      expect(screen.getByText('/help')).toBeInTheDocument();
      expect(screen.queryByText('/status')).not.toBeInTheDocument();
    });

    it('should navigate commands with arrow keys', async () => {
      const user = userEvent.setup();
      render(<ChatInput onSend={mockOnSend} />);
      
      const textarea = screen.getByRole('textbox');
      
      await user.type(textarea, '/');
      
      const listbox = screen.getByRole('listbox');
      fireEvent.keyDown(listbox, { key: 'ArrowDown' });
      
      const options = screen.getAllByRole('option');
      expect(options[0]).toHaveClass('command-option-selected');
    });

    it('should select command on Enter key', async () => {
      const user = userEvent.setup();
      render(<ChatInput onSend={mockOnSend} />);
      
      const textarea = screen.getByRole('textbox');
      
      await user.type(textarea, '/help');
      
      // Fire Enter key on textarea
      fireEvent.keyDown(textarea, { key: 'Enter' });
      
      await waitFor(() => {
        expect(mockOnSend).toHaveBeenCalledWith('/help');
      });
    });

    it('should hide palette when input does not start with /', async () => {
      const user = userEvent.setup();
      render(<ChatInput onSend={mockOnSend} />);
      
      const textarea = screen.getByRole('textbox');
      
      await user.type(textarea, '/');
      expect(screen.getByRole('listbox')).toBeInTheDocument();
      
      await user.clear(textarea);
      await user.type(textarea, 'regular message');
      
      expect(screen.queryByRole('listbox')).not.toBeInTheDocument();
    });
  });

  describe('Cancel Button', () => {
    it('should show Cancel button when processing', () => {
      render(<ChatInput onSend={mockOnSend} isProcessing={true} onCancel={mockOnCancel} />);
      
      const cancelButton = screen.getByRole('button', { name: /cancel|stop/i });
      expect(cancelButton).toBeInTheDocument();
    });

    it('should not show Cancel button when not processing', () => {
      render(<ChatInput onSend={mockOnSend} isProcessing={false} />);
      
      const cancelButton = screen.queryByRole('button', { name: /cancel|stop/i });
      expect(cancelButton).not.toBeInTheDocument();
    });

    it('should call onCancel when Cancel button is clicked', async () => {
      const user = userEvent.setup();
      render(<ChatInput onSend={mockOnSend} isProcessing={true} onCancel={mockOnCancel} />);
      
      const cancelButton = screen.getByRole('button', { name: /cancel|stop/i });
      await user.click(cancelButton);
      
      expect(mockOnCancel).toHaveBeenCalled();
    });

    it('should disable input when processing', () => {
      render(<ChatInput onSend={mockOnSend} isProcessing={true} />);
      
      const textarea = screen.getByRole('textbox');
      expect(textarea).toBeDisabled();
    });
  });

  describe('Accessibility', () => {
    it('should have proper ARIA labels', () => {
      render(<ChatInput onSend={mockOnSend} />);
      
      const textarea = screen.getByRole('textbox');
      expect(textarea).toHaveAttribute('aria-label');
    });

    it('should announce character count to screen readers', () => {
      render(<ChatInput onSend={mockOnSend} />);
      
      const counter = screen.getByText(/0\s*\/\s*2000/);
      expect(counter).toHaveAttribute('aria-live', 'polite');
    });

    it('should announce command palette to screen readers', async () => {
      const user = userEvent.setup();
      render(<ChatInput onSend={mockOnSend} />);
      
      const textarea = screen.getByRole('textbox');
      await user.type(textarea, '/');
      
      const listbox = screen.getByRole('listbox');
      expect(listbox).toHaveAttribute('aria-label');
    });
  });
});
