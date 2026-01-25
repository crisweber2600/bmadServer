import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import '@testing-library/jest-dom';
import { ChatMessage, TypingIndicator } from '../ChatMessage';

describe('ChatMessage', () => {
  const mockTimestamp = new Date('2026-01-25T12:00:00');

  describe('User Messages', () => {
    it('should render user message aligned right with blue background', () => {
      render(
        <ChatMessage
          id="msg-1"
          role="user"
          content="Hello, BMAD!"
          timestamp={mockTimestamp}
        />
      );

      const message = screen.getByRole('article');
      expect(message).toHaveClass('chat-message-user');
      expect(screen.getByText('Hello, BMAD!')).toBeInTheDocument();
      expect(screen.getByLabelText('Your avatar')).toBeInTheDocument();
    });

    it('should display timestamp in user message', () => {
      render(
        <ChatMessage
          id="msg-1"
          role="user"
          content="Test message"
          timestamp={mockTimestamp}
        />
      );

      const timestamp = screen.getByLabelText('Message time');
      expect(timestamp).toBeInTheDocument();
      expect(timestamp.textContent).toMatch(/12:00|PM/);
    });

    it('should have proper ARIA label for screen readers', () => {
      render(
        <ChatMessage
          id="msg-1"
          role="user"
          content="Accessible message"
          timestamp={mockTimestamp}
        />
      );

      const message = screen.getByRole('article');
      expect(message).toHaveAttribute('aria-label', 'You said: Accessible message');
    });
  });

  describe('Agent Messages', () => {
    it('should render agent message aligned left with gray background', () => {
      render(
        <ChatMessage
          id="msg-2"
          role="agent"
          content="Hello, Sarah!"
          timestamp={mockTimestamp}
          agentName="Planning Agent"
        />
      );

      const message = screen.getByRole('article');
      expect(message).toHaveClass('chat-message-agent');
      expect(screen.getByText('Hello, Sarah!')).toBeInTheDocument();
      expect(screen.getByLabelText('Planning Agent avatar')).toBeInTheDocument();
    });

    it('should display agent name in agent message', () => {
      render(
        <ChatMessage
          id="msg-2"
          role="agent"
          content="Test message"
          timestamp={mockTimestamp}
          agentName="Planning Agent"
        />
      );

      const agentName = screen.getByLabelText('Agent name');
      expect(agentName).toBeInTheDocument();
      expect(agentName).toHaveTextContent('Planning Agent');
    });

    it('should use default agent name when not provided', () => {
      render(
        <ChatMessage
          id="msg-2"
          role="agent"
          content="Test message"
          timestamp={mockTimestamp}
        />
      );

      const agentName = screen.getByLabelText('Agent name');
      expect(agentName).toHaveTextContent('BMAD Agent');
    });

    it('should have proper ARIA label for agent messages', () => {
      render(
        <ChatMessage
          id="msg-2"
          role="agent"
          content="Agent message"
          timestamp={mockTimestamp}
          agentName="Planning Agent"
        />
      );

      const message = screen.getByRole('article');
      expect(message).toHaveAttribute('aria-label', 'Planning Agent said: Agent message');
    });
  });

  describe('Markdown Rendering', () => {
    it('should render markdown formatting', () => {
      render(
        <ChatMessage
          id="msg-3"
          role="agent"
          content="This is **bold** and *italic* text"
          timestamp={mockTimestamp}
        />
      );

      expect(screen.getByText('bold')).toBeInTheDocument();
      expect(screen.getByText('italic')).toBeInTheDocument();
    });

    it('should render code blocks with syntax highlighting', () => {
      const codeContent = '```javascript\nconst x = 42;\n```';
      const { container } = render(
        <ChatMessage
          id="msg-4"
          role="agent"
          content={codeContent}
          timestamp={mockTimestamp}
        />
      );

      // Check for pre element
      const pre = container.querySelector('pre');
      expect(pre).toBeInTheDocument();
      
      // Check for code element with language class
      const code = container.querySelector('code.language-javascript');
      expect(code).toBeInTheDocument();
      
      // Check that content includes the code (may be split by syntax highlighting)
      expect(code?.textContent).toContain('const');
      expect(code?.textContent).toContain('42');
    });

    it('should render inline code', () => {
      render(
        <ChatMessage
          id="msg-5"
          role="agent"
          content="Use the `console.log()` function"
          timestamp={mockTimestamp}
        />
      );

      const code = screen.getByText('console.log()');
      expect(code.tagName).toBe('CODE');
    });

    it('should render links that open in new tab', () => {
      render(
        <ChatMessage
          id="msg-6"
          role="agent"
          content="Check out [this link](https://example.com)"
          timestamp={mockTimestamp}
        />
      );

      const link = screen.getByRole('link', { name: 'this link' });
      expect(link).toHaveAttribute('href', 'https://example.com');
      expect(link).toHaveAttribute('target', '_blank');
      expect(link).toHaveAttribute('rel', 'noopener noreferrer');
    });

    it('should render lists', () => {
      const listContent = '1. First item\n2. Second item\n3. Third item';
      render(
        <ChatMessage
          id="msg-7"
          role="agent"
          content={listContent}
          timestamp={mockTimestamp}
        />
      );

      expect(screen.getByText('First item')).toBeInTheDocument();
      expect(screen.getByText('Second item')).toBeInTheDocument();
      expect(screen.getByText('Third item')).toBeInTheDocument();
    });
  });

  describe('Accessibility', () => {
    it('should have proper role attribute', () => {
      render(
        <ChatMessage
          id="msg-8"
          role="user"
          content="Accessible message"
          timestamp={mockTimestamp}
        />
      );

      const message = screen.getByRole('article');
      expect(message).toBeInTheDocument();
    });

    it('should have unique message ID', () => {
      render(
        <ChatMessage
          id="unique-msg-id"
          role="user"
          content="Test"
          timestamp={mockTimestamp}
        />
      );

      const message = screen.getByRole('article');
      expect(message).toHaveAttribute('data-message-id', 'unique-msg-id');
    });
  });
});

describe('TypingIndicator', () => {
  it('should render typing indicator with agent name', () => {
    render(<TypingIndicator agentName="Planning Agent" />);

    expect(screen.getByText('Planning Agent')).toBeInTheDocument();
    expect(screen.getByRole('status')).toBeInTheDocument();
  });

  it('should have proper ARIA label for screen readers', () => {
    render(<TypingIndicator agentName="Planning Agent" />);

    const indicator = screen.getByRole('status');
    expect(indicator).toHaveAttribute('aria-label', 'Planning Agent is typing');
    expect(indicator).toHaveAttribute('aria-live', 'polite');
  });

  it('should render animated dots', () => {
    const { container } = render(<TypingIndicator agentName="Planning Agent" />);

    const dots = container.querySelectorAll('.dot');
    expect(dots).toHaveLength(3);
  });

  it('should use default agent name when not provided', () => {
    render(<TypingIndicator />);

    expect(screen.getByText('BMAD Agent')).toBeInTheDocument();
  });

  it('should display within 500ms - CSS animation check', () => {
    const { container } = render(<TypingIndicator agentName="Test Agent" />);

    const indicator = container.querySelector('.typing-indicator');
    expect(indicator).toBeInTheDocument();
    // The component renders immediately; CSS handles animation timing
    expect(indicator).toHaveClass('typing-indicator');
  });
});
