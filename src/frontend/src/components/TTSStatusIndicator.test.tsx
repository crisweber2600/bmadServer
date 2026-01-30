import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, fireEvent, act } from '@testing-library/react';
import { TTSStatusIndicator } from './TTSStatusIndicator';

describe('TTSStatusIndicator', () => {
  beforeEach(() => {
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.useRealTimers();
    vi.clearAllMocks();
  });

  it('should show muted icon and "TTS idle" when not playing', () => {
    render(<TTSStatusIndicator isPlaying={false} />);

    expect(screen.getByText('TTS idle')).toBeInTheDocument();
    expect(screen.getByTestId('tts-status-indicator')).toHaveClass('idle');
  });

  it('should show sound icon and agent name when playing', () => {
    render(<TTSStatusIndicator isPlaying={true} agentName="Winston" />);

    expect(screen.getByText('Winston speaking...')).toBeInTheDocument();
    expect(screen.getByTestId('tts-status-indicator')).toHaveClass('playing');
  });

  it('should show generic "Agent speaking..." when playing without agentName', () => {
    render(<TTSStatusIndicator isPlaying={true} />);

    expect(screen.getByText('Agent speaking...')).toBeInTheDocument();
  });

  it('should call onStop when clicked while playing', () => {
    const onStop = vi.fn();
    render(<TTSStatusIndicator isPlaying={true} agentName="Winston" onStop={onStop} />);

    fireEvent.click(screen.getByTestId('tts-status-indicator'));
    expect(onStop).toHaveBeenCalledTimes(1);
  });

  it('should not call onStop when clicked while idle', () => {
    const onStop = vi.fn();
    render(<TTSStatusIndicator isPlaying={false} onStop={onStop} />);

    fireEvent.click(screen.getByTestId('tts-status-indicator'));
    expect(onStop).not.toHaveBeenCalled();
  });

  it('should call onStop on Enter key while playing', () => {
    const onStop = vi.fn();
    render(<TTSStatusIndicator isPlaying={true} onStop={onStop} />);

    fireEvent.keyDown(screen.getByTestId('tts-status-indicator'), { key: 'Enter' });
    expect(onStop).toHaveBeenCalledTimes(1);
  });

  it('should call onStop on Space key while playing', () => {
    const onStop = vi.fn();
    render(<TTSStatusIndicator isPlaying={true} onStop={onStop} />);

    fireEvent.keyDown(screen.getByTestId('tts-status-indicator'), { key: ' ' });
    expect(onStop).toHaveBeenCalledTimes(1);
  });

  it('should auto-stop after stale timeout (30s default)', async () => {
    const onStop = vi.fn();
    render(<TTSStatusIndicator isPlaying={true} onStop={onStop} />);

    // Fast forward 29 seconds - should not stop yet
    await act(async () => {
      vi.advanceTimersByTime(29000);
    });
    expect(onStop).not.toHaveBeenCalled();

    // Fast forward 1 more second - should trigger stale recovery
    await act(async () => {
      vi.advanceTimersByTime(1000);
    });
    expect(onStop).toHaveBeenCalledTimes(1);
  });

  it('should use custom staleTimeoutMs', async () => {
    const onStop = vi.fn();
    render(<TTSStatusIndicator isPlaying={true} onStop={onStop} staleTimeoutMs={5000} />);

    await act(async () => {
      vi.advanceTimersByTime(5000);
    });
    expect(onStop).toHaveBeenCalledTimes(1);
  });

  it('should not trigger stale timeout when not playing', async () => {
    const onStop = vi.fn();
    render(<TTSStatusIndicator isPlaying={false} onStop={onStop} />);

    await act(async () => {
      vi.advanceTimersByTime(35000);
    });
    expect(onStop).not.toHaveBeenCalled();
  });

  it('should clear stale timeout when isPlaying changes to false', async () => {
    const onStop = vi.fn();
    const { rerender } = render(<TTSStatusIndicator isPlaying={true} onStop={onStop} />);

    // Advance time partially
    await act(async () => {
      vi.advanceTimersByTime(15000);
    });

    // Stop playing
    rerender(<TTSStatusIndicator isPlaying={false} onStop={onStop} />);

    // Continue advancing time
    await act(async () => {
      vi.advanceTimersByTime(20000);
    });

    // onStop should not have been called (timeout was cleared)
    expect(onStop).not.toHaveBeenCalled();
  });

  it('should have proper accessibility attributes when playing', () => {
    render(<TTSStatusIndicator isPlaying={true} agentName="Winston" onStop={vi.fn()} />);

    const indicator = screen.getByTestId('tts-status-indicator');
    expect(indicator).toHaveAttribute('role', 'button');
    expect(indicator).toHaveAttribute('aria-label', 'Winston speaking...');
    expect(indicator).toHaveAttribute('aria-pressed', 'true');
    expect(indicator).toHaveAttribute('tabIndex', '0');
  });

  it('should have proper accessibility attributes when idle', () => {
    render(<TTSStatusIndicator isPlaying={false} />);

    const indicator = screen.getByTestId('tts-status-indicator');
    expect(indicator).toHaveAttribute('role', 'button');
    expect(indicator).toHaveAttribute('aria-label', 'TTS idle');
    expect(indicator).toHaveAttribute('aria-pressed', 'false');
    expect(indicator).toHaveAttribute('tabIndex', '-1');
  });

  it('should disable stale timeout when staleTimeoutMs is 0', async () => {
    const onStop = vi.fn();
    render(<TTSStatusIndicator isPlaying={true} onStop={onStop} staleTimeoutMs={0} />);

    await act(async () => {
      vi.advanceTimersByTime(60000);
    });
    expect(onStop).not.toHaveBeenCalled();
  });
});
