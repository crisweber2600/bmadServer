import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { ChatMessage } from './ChatMessage';

describe('ChatMessage', () => {
  const mockDate = new Date('2024-01-15T10:30:00');

  describe('User Messages', () => {
    it('renders user message with correct styling', () => {
      render(
        <ChatMessage
          content="Hello, world!"
          isUser={true}
          timestamp={mockDate}
        />
      );

      const message = screen.getByRole('article');
      expect(message).toHaveClass('user-message');
      expect(screen.getByText('Hello, world!')).toBeInTheDocument();
    });

    it('displays timestamp for user messages', () => {
      render(
        <ChatMessage
          content="Test message"
          isUser={true}
          timestamp={mockDate}
        />
      );

      expect(screen.getByText(/10:30/)).toBeInTheDocument();
    });

    it('does not show avatar for user messages in content', () => {
      const { container } = render(
        <ChatMessage
          content="User message"
          isUser={true}
          timestamp={mockDate}
        />
      );

      const avatars = container.querySelectorAll('.message-avatar');
      expect(avatars).toHaveLength(1); // User avatar on the right
    });

    it('has proper ARIA label for user messages', () => {
      render(
        <ChatMessage
          content="Test content"
          isUser={true}
          timestamp={mockDate}
        />
      );

      const message = screen.getByRole('article');
      expect(message).toHaveAttribute('aria-label');
      expect(message.getAttribute('aria-label')).toContain('You said');
    });
  });

  describe('Agent Messages', () => {
    it('renders agent message with correct styling', () => {
      render(
        <ChatMessage
          content="Hello from agent"
          isUser={false}
          timestamp={mockDate}
          agentName="Test Agent"
        />
      );

      const message = screen.getByRole('article');
      expect(message).toHaveClass('agent-message');
      expect(screen.getByText('Hello from agent')).toBeInTheDocument();
    });

    it('displays agent name', () => {
      render(
        <ChatMessage
          content="Agent message"
          isUser={false}
          timestamp={mockDate}
          agentName="BMAD Assistant"
        />
      );

      expect(screen.getByText('BMAD Assistant')).toBeInTheDocument();
    });

    it('uses default agent name if not provided', () => {
      render(
        <ChatMessage
          content="Agent message"
          isUser={false}
          timestamp={mockDate}
        />
      );

      expect(screen.getByText('BMAD Agent')).toBeInTheDocument();
    });

    it('shows agent avatar', () => {
      const { container } = render(
        <ChatMessage
          content="Agent message"
          isUser={false}
          timestamp={mockDate}
        />
      );

      const avatars = container.querySelectorAll('.message-avatar');
      expect(avatars).toHaveLength(1); // Agent avatar on the left
    });

    it('has proper ARIA label for agent messages', () => {
      render(
        <ChatMessage
          content="Test content"
          isUser={false}
          timestamp={mockDate}
          agentName="Test Agent"
        />
      );

      const message = screen.getByRole('article');
      expect(message).toHaveAttribute('aria-label');
      expect(message.getAttribute('aria-label')).toContain('Test Agent said');
    });
  });

  describe('Markdown Rendering', () => {
    it('renders bold text', () => {
      render(
        <ChatMessage
          content="**Bold text**"
          isUser={false}
          timestamp={mockDate}
        />
      );

      const strong = screen.getByText('Bold text');
      expect(strong.tagName).toBe('STRONG');
    });

    it('renders italic text', () => {
      render(
        <ChatMessage
          content="*Italic text*"
          isUser={false}
          timestamp={mockDate}
        />
      );

      const em = screen.getByText('Italic text');
      expect(em.tagName).toBe('EM');
    });

    it('renders links with target="_blank"', () => {
      render(
        <ChatMessage
          content="[Link text](https://example.com)"
          isUser={false}
          timestamp={mockDate}
        />
      );

      const link = screen.getByText('Link text') as HTMLAnchorElement;
      expect(link.tagName).toBe('A');
      expect(link.href).toBe('https://example.com/');
      expect(link.target).toBe('_blank');
      expect(link.rel).toBe('noopener noreferrer');
    });

    it('renders inline code', () => {
      render(
        <ChatMessage
          content="`inline code`"
          isUser={false}
          timestamp={mockDate}
        />
      );

      const code = screen.getByText('inline code');
      expect(code.tagName).toBe('CODE');
    });

    it('renders lists', () => {
      const content = `
- Item 1
- Item 2
- Item 3
      `;

      render(
        <ChatMessage
          content={content}
          isUser={false}
          timestamp={mockDate}
        />
      );

      expect(screen.getByText('Item 1')).toBeInTheDocument();
      expect(screen.getByText('Item 2')).toBeInTheDocument();
      expect(screen.getByText('Item 3')).toBeInTheDocument();
    });
  });

  describe('Timestamp Formatting', () => {
    it('formats timestamp correctly for morning', () => {
      const morning = new Date('2024-01-15T09:15:00');
      render(
        <ChatMessage
          content="Morning message"
          isUser={false}
          timestamp={morning}
        />
      );

      expect(screen.getByText(/09:15/)).toBeInTheDocument();
    });

    it('formats timestamp correctly for afternoon', () => {
      const afternoon = new Date('2024-01-15T14:30:00');
      render(
        <ChatMessage
          content="Afternoon message"
          isUser={false}
          timestamp={afternoon}
        />
      );

      expect(screen.getByText(/02:30|14:30/)).toBeInTheDocument();
    });
  });
});
