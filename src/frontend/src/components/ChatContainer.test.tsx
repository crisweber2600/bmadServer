import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { ChatContainer } from './ChatContainer';

describe('ChatContainer', () => {
  it('renders children correctly', () => {
    render(
      <ChatContainer>
        <div>Test Message 1</div>
        <div>Test Message 2</div>
      </ChatContainer>
    );

    expect(screen.getByText('Test Message 1')).toBeInTheDocument();
    expect(screen.getByText('Test Message 2')).toBeInTheDocument();
  });

  it('has proper ARIA attributes', () => {
    const { container } = render(
      <ChatContainer>
        <div>Content</div>
      </ChatContainer>
    );

    const chatContainer = container.querySelector('.chat-container');
    expect(chatContainer).toHaveAttribute('role', 'log');
    expect(chatContainer).toHaveAttribute('aria-live', 'polite');
    expect(chatContainer).toHaveAttribute('aria-label', 'Chat messages');
  });

  it('applies chat-container class', () => {
    const { container } = render(
      <ChatContainer>
        <div>Content</div>
      </ChatContainer>
    );

    const chatContainer = container.querySelector('.chat-container');
    expect(chatContainer).toBeInTheDocument();
  });

  it('renders multiple children', () => {
    render(
      <ChatContainer>
        <div>Message 1</div>
        <div>Message 2</div>
        <div>Message 3</div>
      </ChatContainer>
    );

    expect(screen.getByText('Message 1')).toBeInTheDocument();
    expect(screen.getByText('Message 2')).toBeInTheDocument();
    expect(screen.getByText('Message 3')).toBeInTheDocument();
  });

  it('calls scrollTo when children change', () => {
    const scrollToMock = vi.fn();
    
    HTMLElement.prototype.scrollTo = scrollToMock;

    const { rerender } = render(
      <ChatContainer autoScroll={true}>
        <div>Message 1</div>
      </ChatContainer>
    );

    // Clear initial scroll calls
    scrollToMock.mockClear();

    // Trigger rerender with new children
    rerender(
      <ChatContainer autoScroll={true}>
        <div>Message 1</div>
        <div>Message 2</div>
      </ChatContainer>
    );

    // Scroll should be called with smooth behavior
    expect(scrollToMock).toHaveBeenCalled();
  });

  it('does not auto-scroll when autoScroll is false', () => {
    const scrollToMock = vi.fn();
    
    HTMLElement.prototype.scrollTo = scrollToMock;

    const { rerender } = render(
      <ChatContainer autoScroll={false}>
        <div>Message 1</div>
      </ChatContainer>
    );

    scrollToMock.mockClear();

    rerender(
      <ChatContainer autoScroll={false}>
        <div>Message 1</div>
        <div>Message 2</div>
      </ChatContainer>
    );

    expect(scrollToMock).not.toHaveBeenCalled();
  });
});
