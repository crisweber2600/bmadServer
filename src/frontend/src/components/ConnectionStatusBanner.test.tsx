import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, fireEvent, act, waitFor } from '@testing-library/react';
import { ConnectionStatusBanner } from './ConnectionStatusBanner';

describe('ConnectionStatusBanner', () => {
  beforeEach(() => {
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.useRealTimers();
    vi.clearAllMocks();
  });

  it('should be hidden when connectionState is "connected" initially', async () => {
    render(<ConnectionStatusBanner connectionState="connected" />);
    
    // Wait for debounce
    await act(async () => {
      vi.advanceTimersByTime(150);
    });

    const wrapper = screen.getByTestId('connection-status-banner');
    expect(wrapper).toHaveClass('hidden');
  });

  it('should show amber warning when connectionState is "disconnected"', async () => {
    render(<ConnectionStatusBanner connectionState="disconnected" />);
    
    // Wait for debounce
    await act(async () => {
      vi.advanceTimersByTime(150);
    });

    expect(screen.getByTestId('connection-banner-disconnected')).toBeInTheDocument();
    expect(screen.getByText(/Connection lost/i)).toBeInTheDocument();
  });

  it('should show reconnecting state with attempt count', async () => {
    render(
      <ConnectionStatusBanner 
        connectionState="reconnecting" 
        attemptNumber={3}
        maxAttempts={10}
      />
    );
    
    await act(async () => {
      vi.advanceTimersByTime(150);
    });

    expect(screen.getByTestId('connection-banner-reconnecting')).toBeInTheDocument();
    expect(screen.getByText(/Attempt 3 of 10/i)).toBeInTheDocument();
  });

  it('should show "Reconnected" message for 2 seconds after reconnecting', async () => {
    const { rerender } = render(
      <ConnectionStatusBanner connectionState="disconnected" />
    );
    
    // Wait for debounce
    await act(async () => {
      vi.advanceTimersByTime(150);
    });

    expect(screen.getByTestId('connection-banner-disconnected')).toBeInTheDocument();

    // Reconnect
    rerender(<ConnectionStatusBanner connectionState="connected" />);
    
    await act(async () => {
      vi.advanceTimersByTime(150);
    });

    // Should show reconnected
    expect(screen.getByTestId('connection-banner-reconnected')).toBeInTheDocument();
    expect(screen.getByText(/Reconnected/i)).toBeInTheDocument();

    // After 2 seconds, should hide
    await act(async () => {
      vi.advanceTimersByTime(2000);
    });

    const wrapper = screen.getByTestId('connection-status-banner');
    expect(wrapper).toHaveClass('hidden');
  });

  it('should immediately switch to disconnected if disconnect occurs during reconnected display', async () => {
    const { rerender } = render(
      <ConnectionStatusBanner connectionState="disconnected" />
    );
    
    await act(async () => {
      vi.advanceTimersByTime(150);
    });

    // Reconnect
    rerender(<ConnectionStatusBanner connectionState="connected" />);
    
    await act(async () => {
      vi.advanceTimersByTime(150);
    });

    expect(screen.getByTestId('connection-banner-reconnected')).toBeInTheDocument();

    // Disconnect again before 2s timer
    rerender(<ConnectionStatusBanner connectionState="disconnected" />);
    
    await act(async () => {
      vi.advanceTimersByTime(150);
    });

    // Should immediately show disconnected
    expect(screen.getByTestId('connection-banner-disconnected')).toBeInTheDocument();
    expect(screen.queryByTestId('connection-banner-reconnected')).not.toBeInTheDocument();
  });

  it('should call onRetryClick when retry button is clicked', async () => {
    const onRetryClick = vi.fn();
    render(
      <ConnectionStatusBanner 
        connectionState="disconnected" 
        onRetryClick={onRetryClick}
      />
    );
    
    await act(async () => {
      vi.advanceTimersByTime(150);
    });

    fireEvent.click(screen.getByRole('button', { name: /retry now/i }));
    expect(onRetryClick).toHaveBeenCalledTimes(1);
  });

  it('should not show retry button when showRetryButton is false', async () => {
    const onRetryClick = vi.fn();
    render(
      <ConnectionStatusBanner 
        connectionState="disconnected" 
        onRetryClick={onRetryClick}
        showRetryButton={false}
      />
    );
    
    await act(async () => {
      vi.advanceTimersByTime(150);
    });

    expect(screen.queryByRole('button', { name: /retry/i })).not.toBeInTheDocument();
  });

  it('should have proper accessibility attributes on disconnected banner', async () => {
    render(<ConnectionStatusBanner connectionState="disconnected" />);
    
    await act(async () => {
      vi.advanceTimersByTime(150);
    });

    const banner = screen.getByTestId('connection-banner-disconnected');
    expect(banner).toHaveAttribute('role', 'alert');
    expect(banner).toHaveAttribute('aria-live', 'assertive');
  });

  it('should have proper accessibility attributes on reconnecting banner', async () => {
    render(<ConnectionStatusBanner connectionState="reconnecting" />);
    
    await act(async () => {
      vi.advanceTimersByTime(150);
    });

    const banner = screen.getByTestId('connection-banner-reconnecting');
    expect(banner).toHaveAttribute('role', 'status');
    expect(banner).toHaveAttribute('aria-live', 'polite');
  });

  it('should use custom reconnectedDisplayMs value', async () => {
    const { rerender } = render(
      <ConnectionStatusBanner 
        connectionState="disconnected" 
        reconnectedDisplayMs={5000}
      />
    );
    
    await act(async () => {
      vi.advanceTimersByTime(150);
    });

    // Reconnect
    rerender(
      <ConnectionStatusBanner 
        connectionState="connected" 
        reconnectedDisplayMs={5000}
      />
    );
    
    await act(async () => {
      vi.advanceTimersByTime(150);
    });

    // After 2 seconds, should still show reconnected
    await act(async () => {
      vi.advanceTimersByTime(2000);
    });

    expect(screen.getByTestId('connection-banner-reconnected')).toBeInTheDocument();

    // After 5 seconds total, should hide
    await act(async () => {
      vi.advanceTimersByTime(3000);
    });

    const wrapper = screen.getByTestId('connection-status-banner');
    expect(wrapper).toHaveClass('hidden');
  });

  it('should debounce rapid state changes', async () => {
    const { rerender } = render(
      <ConnectionStatusBanner connectionState="connected" />
    );

    // Rapid changes within 100ms
    rerender(<ConnectionStatusBanner connectionState="disconnected" />);
    rerender(<ConnectionStatusBanner connectionState="reconnecting" />);
    rerender(<ConnectionStatusBanner connectionState="connected" />);
    rerender(<ConnectionStatusBanner connectionState="disconnected" />);

    // Before debounce completes
    await act(async () => {
      vi.advanceTimersByTime(50);
    });

    // Should not have updated yet (debouncing)
    const wrapper = screen.getByTestId('connection-status-banner');
    expect(wrapper).toHaveClass('hidden'); // Initial state

    // After debounce completes
    await act(async () => {
      vi.advanceTimersByTime(100);
    });

    // Should show final state (disconnected)
    expect(screen.getByTestId('connection-banner-disconnected')).toBeInTheDocument();
  });

  it('should allow input during disconnected state per UX feedback', async () => {
    render(<ConnectionStatusBanner connectionState="disconnected" />);
    
    await act(async () => {
      vi.advanceTimersByTime(150);
    });

    // Banner is informational only - it doesn't block interaction
    // This test verifies the banner doesn't have any blocking attributes
    const banner = screen.getByTestId('connection-banner-disconnected');
    expect(banner).not.toHaveAttribute('aria-modal');
    expect(banner).not.toHaveStyle({ pointerEvents: 'none' });
  });
});
