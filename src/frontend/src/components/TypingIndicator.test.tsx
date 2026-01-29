import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import { TypingIndicator } from './TypingIndicator';

describe('TypingIndicator', () => {
  let consoleWarnSpy: ReturnType<typeof vi.spyOn>;

  beforeEach(() => {
    consoleWarnSpy = vi.spyOn(console, 'warn').mockImplementation(() => {});
  });

  afterEach(() => {
    consoleWarnSpy.mockRestore();
  });

  describe('rendering', () => {
    it('does not render when no users are typing', () => {
      const { container } = render(<TypingIndicator />);

      const indicator = container.querySelector('.typing-indicator');
      expect(indicator).not.toBeInTheDocument();
    });

    it('renders typing indicator with single user', () => {
      render(<TypingIndicator typingUsers={['Test Agent']} />);

      const indicator = screen.getByRole('status');
      expect(indicator).toBeInTheDocument();
      expect(indicator).toHaveClass('typing-indicator');
    });

    it('renders avatar with robot icon', () => {
      const { container } = render(<TypingIndicator typingUsers={['Agent']} />);

      const avatar = container.querySelector('.typing-avatar');
      expect(avatar).toBeInTheDocument();
    });

    it('renders three animated dots', () => {
      const { container } = render(<TypingIndicator typingUsers={['Agent']} />);

      const dots = container.querySelectorAll('.dot');
      expect(dots).toHaveLength(3);
    });

    it('has typing animation container', () => {
      const { container } = render(<TypingIndicator typingUsers={['Agent']} />);

      const typingBubble = container.querySelector('.typing-bubble');
      expect(typingBubble).toBeInTheDocument();

      const typingDots = container.querySelector('.typing-dots');
      expect(typingDots).toBeInTheDocument();
    });
  });

  describe('backward compatibility (agentName prop)', () => {
    it('displays agent name when using deprecated agentName prop', () => {
      render(<TypingIndicator agentName="Test Agent" />);

      expect(screen.getByText('Test Agent')).toBeInTheDocument();
    });

    it('has proper ARIA attributes with agentName', () => {
      render(<TypingIndicator agentName="Assistant" />);

      const indicator = screen.getByRole('status');
      expect(indicator).toHaveAttribute('aria-live', 'polite');
      expect(indicator).toHaveAttribute('aria-label', 'Assistant is typing...');
    });
  });

  describe('multi-user support (typingUsers prop)', () => {
    it('displays single user name', () => {
      render(<TypingIndicator typingUsers={['Alice']} />);

      expect(screen.getByText('Alice')).toBeInTheDocument();
      expect(screen.getByRole('status')).toHaveAttribute(
        'aria-label',
        'Alice is typing...'
      );
    });

    it('displays two users with proper formatting', () => {
      render(<TypingIndicator typingUsers={['Alice', 'Bob']} />);

      expect(screen.getByText('Alice, Bob')).toBeInTheDocument();
      expect(screen.getByRole('status')).toHaveAttribute(
        'aria-label',
        'Alice, Bob are typing...'
      );
    });

    it('displays three users with proper formatting', () => {
      render(<TypingIndicator typingUsers={['Alice', 'Bob', 'Charlie']} />);

      expect(screen.getByText('Alice, Bob, Charlie')).toBeInTheDocument();
      expect(screen.getByRole('status')).toHaveAttribute(
        'aria-label',
        'Alice, Bob, Charlie are typing...'
      );
    });

    it('displays count when 4+ users are typing', () => {
      render(
        <TypingIndicator typingUsers={['Alice', 'Bob', 'Charlie', 'Dave']} />
      );

      expect(screen.getByText('4 people')).toBeInTheDocument();
      expect(screen.getByRole('status')).toHaveAttribute(
        'aria-label',
        '4 people are typing...'
      );
    });

    it('displays count when many users are typing', () => {
      render(
        <TypingIndicator
          typingUsers={['A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J']}
        />
      );

      expect(screen.getByText('10 people')).toBeInTheDocument();
      expect(screen.getByRole('status')).toHaveAttribute(
        'aria-label',
        '10 people are typing...'
      );
    });

    it('does not render with empty typingUsers array', () => {
      const { container } = render(<TypingIndicator typingUsers={[]} />);

      const indicator = container.querySelector('.typing-indicator');
      expect(indicator).not.toBeInTheDocument();
    });

    it('prefers typingUsers over agentName when both provided', () => {
      render(<TypingIndicator agentName="Agent" typingUsers={['Alice', 'Bob']} />);

      expect(screen.getByText('Alice, Bob')).toBeInTheDocument();
      expect(screen.queryByText('Agent')).not.toBeInTheDocument();
    });
  });

  describe('accessibility', () => {
    it('has role status', () => {
      render(<TypingIndicator typingUsers={['Agent']} />);

      expect(screen.getByRole('status')).toBeInTheDocument();
    });

    it('has aria-live polite for non-intrusive announcements', () => {
      render(<TypingIndicator typingUsers={['Agent']} />);

      expect(screen.getByRole('status')).toHaveAttribute('aria-live', 'polite');
    });

    it('has data-testid for testing', () => {
      render(<TypingIndicator typingUsers={['Agent']} />);

      expect(screen.getByTestId('typing-indicator')).toBeInTheDocument();
    });

    it('hides avatar from assistive technology', () => {
      const { container } = render(<TypingIndicator typingUsers={['Agent']} />);

      const avatar = container.querySelector('.typing-avatar');
      expect(avatar).toHaveAttribute('aria-hidden', 'true');
    });
  });
});
