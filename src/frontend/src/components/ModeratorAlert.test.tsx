import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, fireEvent, act } from '@testing-library/react';
import { ModeratorAlert } from './ModeratorAlert';

describe('ModeratorAlert', () => {
  beforeEach(() => {
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.useRealTimers();
    vi.clearAllMocks();
  });

  it('should render alert with message', () => {
    render(<ModeratorAlert message="Discussion appears to be going in circles" />);

    expect(screen.getByTestId('moderator-alert')).toBeInTheDocument();
    expect(screen.getByText('Discussion appears to be going in circles')).toBeInTheDocument();
  });

  it('should show warning style by default', () => {
    render(<ModeratorAlert message="Test message" />);

    expect(screen.getByTestId('moderator-alert')).toHaveClass('moderator-alert-warning');
  });

  it('should apply info severity styling', () => {
    render(<ModeratorAlert message="Info message" severity="info" />);

    expect(screen.getByTestId('moderator-alert')).toHaveClass('moderator-alert-info');
  });

  it('should apply error severity styling', () => {
    render(<ModeratorAlert message="Error message" severity="error" />);

    expect(screen.getByTestId('moderator-alert')).toHaveClass('moderator-alert-error');
  });

  it('should show Acknowledge button by default', () => {
    render(<ModeratorAlert message="Test" />);

    expect(screen.getByRole('button', { name: /acknowledge/i })).toBeInTheDocument();
  });

  it('should use custom acknowledgeText', () => {
    render(<ModeratorAlert message="Test" acknowledgeText="Got it" />);

    expect(screen.getByRole('button', { name: /got it/i })).toBeInTheDocument();
  });

  it('should call onAcknowledge and dismiss with animation when clicked', async () => {
    const onAcknowledge = vi.fn();
    render(<ModeratorAlert message="Test" onAcknowledge={onAcknowledge} />);

    fireEvent.click(screen.getByTestId('moderator-alert-acknowledge'));

    // Should have dismissing class immediately
    expect(screen.getByTestId('moderator-alert')).toHaveClass('dismissing');

    // Wait for animation timeout
    await act(async () => {
      vi.advanceTimersByTime(300);
    });

    // Callback should be called
    expect(onAcknowledge).toHaveBeenCalledTimes(1);

    // Alert should be removed from DOM
    expect(screen.queryByTestId('moderator-alert')).not.toBeInTheDocument();
  });

  it('should not show button when dismissible is false', () => {
    render(<ModeratorAlert message="Test" dismissible={false} />);

    expect(screen.queryByRole('button')).not.toBeInTheDocument();
  });

  it('should show details when provided', () => {
    render(
      <ModeratorAlert 
        message="Main message" 
        details="Additional context about the alert"
      />
    );

    expect(screen.getByText('Additional context about the alert')).toBeInTheDocument();
  });

  it('should have proper accessibility attributes', () => {
    render(<ModeratorAlert message="Test alert" />);

    const alert = screen.getByTestId('moderator-alert');
    expect(alert).toHaveAttribute('role', 'alert');
    expect(alert).toHaveAttribute('aria-live', 'assertive');
  });

  it('should not call onAcknowledge if not provided', async () => {
    render(<ModeratorAlert message="Test" />);

    fireEvent.click(screen.getByTestId('moderator-alert-acknowledge'));

    await act(async () => {
      vi.advanceTimersByTime(300);
    });

    // Should dismiss without error
    expect(screen.queryByTestId('moderator-alert')).not.toBeInTheDocument();
  });
});
