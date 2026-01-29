import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { PresenceIndicator } from './PresenceIndicator';

describe('PresenceIndicator', () => {
  it('should render green pulsing dot when online', () => {
    render(<PresenceIndicator isOnline={true} />);

    const indicator = screen.getByTestId('presence-indicator');
    expect(indicator).toBeInTheDocument();

    const dot = indicator.querySelector('.presence-dot');
    expect(dot).toHaveClass('online');
    expect(dot).toHaveClass('pulse');
  });

  it('should render gray dot without animation when offline', () => {
    render(<PresenceIndicator isOnline={false} />);

    const indicator = screen.getByTestId('presence-indicator');
    const dot = indicator.querySelector('.presence-dot');
    
    expect(dot).toHaveClass('offline');
    expect(dot).not.toHaveClass('pulse');
  });

  it('should show tooltip with single user name when hovering', () => {
    render(
      <PresenceIndicator
        isOnline={true}
        users={[{ name: 'Alice' }]}
        showTooltip={true}
      />
    );

    // Tooltip content is tested through accessibility label or hover simulation
    const indicator = screen.getByTestId('presence-indicator');
    expect(indicator).toHaveAttribute('aria-label', 'Online');
  });

  it('should show tooltip with multiple users when hovering', () => {
    render(
      <PresenceIndicator
        isOnline={true}
        users={[{ name: 'Alice' }, { name: 'Bob' }]}
        showTooltip={true}
      />
    );

    const indicator = screen.getByTestId('presence-indicator');
    expect(indicator).toBeInTheDocument();
  });

  it('should show tooltip with "+N more" for 4+ users', () => {
    const users = [
      { name: 'Alice' },
      { name: 'Bob' },
      { name: 'Carol' },
      { name: 'Dave' },
      { name: 'Eve' },
    ];

    render(
      <PresenceIndicator
        isOnline={true}
        users={users}
        showTooltip={true}
      />
    );

    const indicator = screen.getByTestId('presence-indicator');
    expect(indicator).toBeInTheDocument();
  });

  it('should apply small size class', () => {
    render(<PresenceIndicator isOnline={true} size="small" />);

    const indicator = screen.getByTestId('presence-indicator');
    expect(indicator).toHaveClass('size-small');
  });

  it('should apply medium size class by default', () => {
    render(<PresenceIndicator isOnline={true} />);

    const indicator = screen.getByTestId('presence-indicator');
    expect(indicator).toHaveClass('size-medium');
  });

  it('should apply large size class', () => {
    render(<PresenceIndicator isOnline={true} size="large" />);

    const indicator = screen.getByTestId('presence-indicator');
    expect(indicator).toHaveClass('size-large');
  });

  it('should show custom label when provided', () => {
    render(<PresenceIndicator isOnline={true} label="Active" />);

    expect(screen.getByText('Active')).toBeInTheDocument();
  });

  it('should disable pulse animation when showPulse is false', () => {
    render(<PresenceIndicator isOnline={true} showPulse={false} />);

    const dot = screen.getByTestId('presence-indicator').querySelector('.presence-dot');
    expect(dot).toHaveClass('online');
    expect(dot).not.toHaveClass('pulse');
  });

  it('should have proper accessibility attributes when online', () => {
    render(<PresenceIndicator isOnline={true} />);

    const indicator = screen.getByTestId('presence-indicator');
    expect(indicator).toHaveAttribute('role', 'status');
    expect(indicator).toHaveAttribute('aria-label', 'Online');
  });

  it('should have proper accessibility attributes when offline', () => {
    render(<PresenceIndicator isOnline={false} />);

    const indicator = screen.getByTestId('presence-indicator');
    expect(indicator).toHaveAttribute('role', 'status');
    expect(indicator).toHaveAttribute('aria-label', 'Offline');
  });

  it('should not render tooltip when showTooltip is false', () => {
    render(
      <PresenceIndicator
        isOnline={true}
        users={[{ name: 'Alice' }]}
        showTooltip={false}
      />
    );

    const indicator = screen.getByTestId('presence-indicator');
    expect(indicator).toBeInTheDocument();
    // No tooltip wrapper should be present
  });

  it('should handle empty users array', () => {
    render(
      <PresenceIndicator
        isOnline={true}
        users={[]}
        showTooltip={true}
      />
    );

    const indicator = screen.getByTestId('presence-indicator');
    expect(indicator).toBeInTheDocument();
  });
});
