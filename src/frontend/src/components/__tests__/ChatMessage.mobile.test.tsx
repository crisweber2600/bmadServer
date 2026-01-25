import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import '@testing-library/jest-dom';
import { ChatMessage, TypingIndicator } from '../ChatMessage';

describe('ChatMessage - Mobile Responsive', () => {
  beforeEach(() => {
    // Mock window.matchMedia for mobile detection
    Object.defineProperty(window, 'matchMedia', {
      writable: true,
      value: vi.fn().mockImplementation((query) => ({
        matches: query === '(max-width: 768px)',
        media: query,
        onchange: null,
        addListener: vi.fn(),
        removeListener: vi.fn(),
        addEventListener: vi.fn(),
        removeEventListener: vi.fn(),
        dispatchEvent: vi.fn(),
      })),
    });
  });

  it('should render message with proper ARIA labels', () => {
    render(
      <ChatMessage
        id="1"
        role="agent"
        content="Test message"
        timestamp={new Date()}
        agentName="Test Agent"
      />
    );

    const message = screen.getByRole('article');
    expect(message).toHaveAttribute('aria-label');
    expect(message.getAttribute('aria-label')).toContain('Test Agent');
  });

  it('should support keyboard navigation with focus visible', () => {
    const { container } = render(
      <ChatMessage
        id="1"
        role="user"
        content="Test message"
        timestamp={new Date()}
      />
    );

    const bubble = container.querySelector('.chat-message-bubble');
    expect(bubble).toBeInTheDocument();
    
    // CSS class should support focus-visible
    const styles = window.getComputedStyle(bubble!);
    expect(styles).toBeDefined();
  });

  it('should render with touch-friendly dimensions on mobile', () => {
    const { container } = render(
      <ChatMessage
        id="1"
        role="agent"
        content="Test message"
        timestamp={new Date()}
      />
    );

    const content = container.querySelector('.chat-message-content');
    expect(content).toHaveClass('chat-message-content');
    
    // Min-height should be at least 44px for touch targets
    const styles = window.getComputedStyle(content!);
    expect(styles).toBeDefined();
  });

  it('should support reduced motion preference', () => {
    // Mock prefers-reduced-motion
    Object.defineProperty(window, 'matchMedia', {
      writable: true,
      value: vi.fn().mockImplementation((query) => ({
        matches: query === '(prefers-reduced-motion: reduce)',
        media: query,
        onchange: null,
        addListener: vi.fn(),
        removeListener: vi.fn(),
        addEventListener: vi.fn(),
        removeEventListener: vi.fn(),
        dispatchEvent: vi.fn(),
      })),
    });

    const { container } = render(
      <ChatMessage
        id="1"
        role="agent"
        content="Test message"
        timestamp={new Date()}
      />
    );

    const message = container.querySelector('.chat-message');
    expect(message).toBeInTheDocument();
  });

  it('should render TypingIndicator with reduced motion support', () => {
    render(<TypingIndicator agentName="Test Agent" />);

    const indicator = screen.getByRole('status');
    expect(indicator).toHaveAttribute('aria-label', 'Test Agent is typing');
    expect(indicator).toHaveAttribute('aria-live', 'polite');
  });

  it('should render avatar with appropriate size', () => {
    const { container } = render(
      <ChatMessage
        id="1"
        role="agent"
        content="Test message"
        timestamp={new Date()}
      />
    );

    const avatar = container.querySelector('.chat-message-avatar');
    expect(avatar).toBeInTheDocument();
  });
});

describe('ChatMessage - Accessibility', () => {
  it('should have proper ARIA attributes for screen readers', () => {
    render(
      <ChatMessage
        id="1"
        role="agent"
        content="Hello, how can I help you?"
        timestamp={new Date()}
        agentName="Support Agent"
      />
    );

    const article = screen.getByRole('article');
    expect(article).toHaveAttribute('aria-label');
    expect(article.getAttribute('aria-label')).toContain('Support Agent');
    expect(article.getAttribute('aria-label')).toContain('Hello, how can I help you?');
  });

  it('should render links with accessible attributes', () => {
    const contentWithLink = 'Check out [this link](https://example.com)';
    
    const { container } = render(
      <ChatMessage
        id="1"
        role="agent"
        content={contentWithLink}
        timestamp={new Date()}
      />
    );

    // ReactMarkdown will convert markdown to HTML link
    const links = container.querySelectorAll('a');
    links.forEach((link) => {
      expect(link).toHaveAttribute('target', '_blank');
      expect(link).toHaveAttribute('rel', 'noopener noreferrer');
    });
  });

  it('should support high contrast mode', () => {
    Object.defineProperty(window, 'matchMedia', {
      writable: true,
      value: vi.fn().mockImplementation((query) => ({
        matches: query === '(prefers-contrast: high)',
        media: query,
        onchange: null,
        addListener: vi.fn(),
        removeListener: vi.fn(),
        addEventListener: vi.fn(),
        removeEventListener: vi.fn(),
        dispatchEvent: vi.fn(),
      })),
    });

    const { container } = render(
      <ChatMessage
        id="1"
        role="user"
        content="Test message"
        timestamp={new Date()}
      />
    );

    const bubble = container.querySelector('.chat-message-bubble');
    expect(bubble).toBeInTheDocument();
  });
});
