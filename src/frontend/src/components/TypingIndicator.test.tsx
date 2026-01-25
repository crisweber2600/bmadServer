import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { TypingIndicator } from './TypingIndicator';

describe('TypingIndicator', () => {
  it('renders typing indicator', () => {
    render(<TypingIndicator />);

    const indicator = screen.getByRole('status');
    expect(indicator).toBeInTheDocument();
    expect(indicator).toHaveClass('typing-indicator');
  });

  it('displays agent name', () => {
    render(<TypingIndicator agentName="Test Agent" />);

    expect(screen.getByText('Test Agent')).toBeInTheDocument();
  });

  it('uses default agent name if not provided', () => {
    render(<TypingIndicator />);

    expect(screen.getByText('BMAD Agent')).toBeInTheDocument();
  });

  it('has proper ARIA attributes', () => {
    render(<TypingIndicator agentName="Assistant" />);

    const indicator = screen.getByRole('status');
    expect(indicator).toHaveAttribute('aria-live', 'polite');
    expect(indicator).toHaveAttribute('aria-label', 'Assistant is typing');
  });

  it('renders avatar with robot icon', () => {
    const { container } = render(<TypingIndicator />);

    const avatar = container.querySelector('.typing-avatar');
    expect(avatar).toBeInTheDocument();
  });

  it('renders three animated dots', () => {
    const { container } = render(<TypingIndicator />);

    const dots = container.querySelectorAll('.dot');
    expect(dots).toHaveLength(3);
  });

  it('has typing animation container', () => {
    const { container } = render(<TypingIndicator />);

    const typingBubble = container.querySelector('.typing-bubble');
    expect(typingBubble).toBeInTheDocument();

    const typingDots = container.querySelector('.typing-dots');
    expect(typingDots).toBeInTheDocument();
  });
});
