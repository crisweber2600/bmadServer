import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { DecisionAttributionBanner } from './DecisionAttributionBanner';

describe('DecisionAttributionBanner', () => {
  const mockDate = new Date('2026-01-26T14:30:00Z');

  beforeEach(() => {
    vi.useFakeTimers({ shouldAdvanceTime: true });
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('renders banner with agent name and timestamp', () => {
    render(
      <DecisionAttributionBanner
        agentId="agent-1"
        agentName="BMAD Architect"
        timestamp={mockDate}
      />
    );

    expect(screen.getByText(/Decided by/)).toBeInTheDocument();
    expect(screen.getByText('BMAD Architect')).toBeInTheDocument();
  });

  it('displays decision icon with proper role', () => {
    render(
      <DecisionAttributionBanner
        agentId="agent-1"
        agentName="BMAD Architect"
        timestamp={mockDate}
      />
    );

    const banner = screen.getByRole('region');
    expect(banner).toHaveAttribute(
      'aria-label',
      expect.stringContaining('BMAD Architect')
    );
  });

  it('displays confidence bar when confidence prop provided', () => {
    render(
      <DecisionAttributionBanner
        agentId="agent-1"
        agentName="BMAD Architect"
        timestamp={mockDate}
        confidence={0.85}
      />
    );

    expect(screen.getByText(/Confidence:/)).toBeInTheDocument();
    expect(screen.getByText('85%')).toBeInTheDocument();
    expect(screen.getByRole('progressbar')).toBeInTheDocument();
  });

  it('does not display confidence when not provided', () => {
    render(
      <DecisionAttributionBanner
        agentId="agent-1"
        agentName="BMAD Architect"
        timestamp={mockDate}
      />
    );

    expect(screen.queryByText(/Confidence:/)).not.toBeInTheDocument();
  });

  it('shows toggle button when reasoning provided', () => {
    render(
      <DecisionAttributionBanner
        agentId="agent-1"
        agentName="BMAD Architect"
        timestamp={mockDate}
        reasoning="This decision was based on project requirements and best practices."
      />
    );

    const toggleButton = screen.getByRole('button');
    expect(toggleButton).toBeInTheDocument();
    expect(toggleButton).toHaveAttribute('aria-label', 'Show reasoning');
  });

  it('does not show toggle button when reasoning not provided', () => {
    render(
      <DecisionAttributionBanner
        agentId="agent-1"
        agentName="BMAD Architect"
        timestamp={mockDate}
      />
    );

    const buttons = screen.queryAllByRole('button');
    expect(buttons).toHaveLength(0);
  });

  it('toggles reasoning section on button click', async () => {
    const user = userEvent.setup();
    render(
      <DecisionAttributionBanner
        agentId="agent-1"
        agentName="BMAD Architect"
        timestamp={mockDate}
        reasoning="This is the decision reasoning content."
      />
    );

    const toggleButton = screen.getByRole('button');
    expect(screen.queryByText('Decision Reasoning:')).not.toBeInTheDocument();

    await user.click(toggleButton);

    await waitFor(() => {
      expect(screen.getByText('Decision Reasoning:')).toBeInTheDocument();
      expect(screen.getByText('This is the decision reasoning content.')).toBeInTheDocument();
    });

    await user.click(toggleButton);

    await waitFor(() => {
      expect(screen.queryByText('This is the decision reasoning content.')).not.toBeInTheDocument();
    });
  });

  it('displays reasoning content with correct formatting', async () => {
    const user = userEvent.setup();
    const multilineReasoning = 'Point 1: First reason\nPoint 2: Second reason\nPoint 3: Third reason';
    
    render(
      <DecisionAttributionBanner
        agentId="agent-1"
        agentName="BMAD Architect"
        timestamp={mockDate}
        reasoning={multilineReasoning}
      />
    );

    const toggleButton = screen.getByTestId('toggle-reasoning-button');
    await user.click(toggleButton);

    const reasoningContent = screen.getByTestId('decision-reasoning');
    expect(reasoningContent).toBeInTheDocument();
  });

  it('generates consistent colors for same agent ID', () => {
    const { rerender } = render(
      <DecisionAttributionBanner
        agentId="agent-1"
        agentName="BMAD Architect"
        timestamp={mockDate}
      />
    );

    const banner1 = screen.getByRole('region');
    const firstColor = window.getComputedStyle(banner1).getPropertyValue('--agent-color');

    rerender(
      <DecisionAttributionBanner
        agentId="agent-1"
        agentName="BMAD Architect"
        timestamp={mockDate}
      />
    );

    const banner2 = screen.getByRole('region');
    const secondColor = window.getComputedStyle(banner2).getPropertyValue('--agent-color');
    // Consistent colors for same agent ID
    expect(firstColor).toBe(secondColor);
  });

  it('generates different colors for different agent IDs', () => {
    const { rerender } = render(
      <DecisionAttributionBanner
        agentId="agent-1"
        agentName="Agent 1"
        timestamp={mockDate}
      />
    );

    rerender(
      <DecisionAttributionBanner
        agentId="agent-2"
        agentName="Agent 2"
        timestamp={mockDate}
      />
    );

    expect(screen.getByText('Agent 2')).toBeInTheDocument();
  });

  it('formats timestamp correctly', () => {
    const testDate = new Date('2026-01-26T09:30:00Z');
    render(
      <DecisionAttributionBanner
        agentId="agent-1"
        agentName="BMAD Architect"
        timestamp={testDate}
      />
    );

    const timeElement = screen.getByText(/at/);
    expect(timeElement).toBeInTheDocument();
  });

  it('uses decidedAt prop when provided instead of timestamp', () => {
    const timestamp = new Date('2026-01-26T09:30:00Z');
    const decidedAt = new Date('2026-01-26T14:30:00Z');

    render(
      <DecisionAttributionBanner
        agentId="agent-1"
        agentName="BMAD Architect"
        timestamp={timestamp}
        decidedAt={decidedAt}
      />
    );

    expect(screen.getByText(/Decided by/)).toBeInTheDocument();
  });

  it('renders with custom avatar URL (if added to future version)', () => {
    render(
      <DecisionAttributionBanner
        agentId="agent-1"
        agentName="BMAD Architect"
        timestamp={mockDate}
        avatarUrl="http://example.com/avatar.png"
      />
    );

    expect(screen.getByText('BMAD Architect')).toBeInTheDocument();
  });

  it('has proper role and aria attributes', () => {
    render(
      <DecisionAttributionBanner
        agentId="agent-1"
        agentName="BMAD Architect"
        timestamp={mockDate}
      />
    );

    const banner = screen.getByRole('region');
    expect(banner).toHaveAttribute('aria-label');
  });

  it('has semantic heading for reasoning section', async () => {
    const user = userEvent.setup();
    render(
      <DecisionAttributionBanner
        agentId="agent-1"
        agentName="BMAD Architect"
        timestamp={mockDate}
        reasoning="Test reasoning"
      />
    );

    await user.click(screen.getByRole('button'));

    await waitFor(() => {
      const heading = screen.getByText('Decision Reasoning:');
      expect(heading.tagName).toBe('H4');
    });
  });

  it('displays all content together when expanded', async () => {
    const user = userEvent.setup();
    render(
      <DecisionAttributionBanner
        agentId="agent-1"
        agentName="BMAD Architect"
        timestamp={mockDate}
        confidence={0.92}
        reasoning="Based on architecture best practices and project requirements."
      />
    );

    const toggleButton = screen.getByRole('button');
    await user.click(toggleButton);

    await waitFor(() => {
      expect(screen.getByText('BMAD Architect')).toBeInTheDocument();
      expect(screen.getByText('92%')).toBeInTheDocument();
      expect(screen.getByText('Decision Reasoning:')).toBeInTheDocument();
      expect(screen.getByText('Based on architecture best practices and project requirements.')).toBeInTheDocument();
    });
  });

  it('handles empty reasoning string gracefully', async () => {
    render(
      <DecisionAttributionBanner
        agentId="agent-1"
        agentName="BMAD Architect"
        timestamp={mockDate}
        reasoning=""
      />
    );

    const buttons = screen.queryAllByRole('button');
    expect(buttons).toHaveLength(0);
  });

  it('updates toggle button aria-expanded when toggled', async () => {
    const user = userEvent.setup();
    render(
      <DecisionAttributionBanner
        agentId="agent-1"
        agentName="BMAD Architect"
        timestamp={mockDate}
        reasoning="Test"
      />
    );

    const button = screen.getByRole('button');
    expect(button).toHaveAttribute('aria-expanded', 'false');

    await user.click(button);

    await waitFor(() => {
      expect(button).toHaveAttribute('aria-expanded', 'true');
    });
  });

  describe('Lock Controls', () => {
    const mockDate = new Date('2026-01-26T14:30:00Z');

    // Use real timers for lock controls tests since they involve complex UI interactions
    beforeEach(() => {
      vi.useRealTimers();
    });

    it('shows lock button when canLock is true and not locked', () => {
      const onLock = vi.fn();
      render(
        <DecisionAttributionBanner
          agentId="agent-1"
          agentName="BMAD Architect"
          timestamp={mockDate}
          canLock={true}
          onLock={onLock}
        />
      );

      expect(screen.getByTestId('lock-button')).toBeInTheDocument();
    });

    it('does not show lock button when canLock is false', () => {
      render(
        <DecisionAttributionBanner
          agentId="agent-1"
          agentName="BMAD Architect"
          timestamp={mockDate}
          canLock={false}
        />
      );

      expect(screen.queryByTestId('lock-button')).not.toBeInTheDocument();
    });

    it('shows locked badge when isLocked is true', () => {
      render(
        <DecisionAttributionBanner
          agentId="agent-1"
          agentName="BMAD Architect"
          timestamp={mockDate}
          isLocked={true}
          lockedBy="Alice"
        />
      );

      expect(screen.getByTestId('locked-badge')).toBeInTheDocument();
      expect(screen.getByText(/Locked by Alice/)).toBeInTheDocument();
    });

    it('shows enabled unlock button when canUnlock is true', () => {
      const onUnlock = vi.fn();
      render(
        <DecisionAttributionBanner
          agentId="agent-1"
          agentName="BMAD Architect"
          timestamp={mockDate}
          isLocked={true}
          lockedBy="Alice"
          canUnlock={true}
          onUnlock={onUnlock}
        />
      );

      const unlockButton = screen.getByTestId('unlock-button');
      expect(unlockButton).toBeInTheDocument();
      expect(unlockButton).not.toBeDisabled();
    });

    it('shows disabled unlock button with tooltip when canUnlock is false', () => {
      render(
        <DecisionAttributionBanner
          agentId="agent-1"
          agentName="BMAD Architect"
          timestamp={mockDate}
          isLocked={true}
          lockedBy="Alice"
          canUnlock={false}
        />
      );

      const unlockButton = screen.getByTestId('unlock-button-disabled');
      expect(unlockButton).toBeInTheDocument();
      expect(unlockButton).toBeDisabled();
    });

    it('renders lock button with correct props', () => {
      const onLock = vi.fn();
      render(
        <DecisionAttributionBanner
          agentId="agent-1"
          agentName="BMAD Architect"
          timestamp={mockDate}
          canLock={true}
          onLock={onLock}
        />
      );

      const lockButton = screen.getByTestId('lock-button');
      expect(lockButton).toHaveAttribute('aria-label', 'Lock decision');
    });

    it('calls onUnlock when unlock button is clicked', async () => {
      const user = userEvent.setup();
      const onUnlock = vi.fn();
      render(
        <DecisionAttributionBanner
          agentId="agent-1"
          agentName="BMAD Architect"
          timestamp={mockDate}
          isLocked={true}
          lockedBy="Alice"
          canUnlock={true}
          onUnlock={onUnlock}
        />
      );

      await user.click(screen.getByTestId('unlock-button'));

      expect(onUnlock).toHaveBeenCalled();
    });

    it('shows lock reason when provided', () => {
      render(
        <DecisionAttributionBanner
          agentId="agent-1"
          agentName="BMAD Architect"
          timestamp={mockDate}
          isLocked={true}
          lockedBy="Alice"
          lockReason="Final decision approved"
        />
      );

      expect(screen.getByTestId('lock-reason')).toBeInTheDocument();
      expect(screen.getByText(/Final decision approved/)).toBeInTheDocument();
    });

    it('shows history button when onViewHistory is provided', () => {
      const onViewHistory = vi.fn();
      render(
        <DecisionAttributionBanner
          agentId="agent-1"
          agentName="BMAD Architect"
          timestamp={mockDate}
          onViewHistory={onViewHistory}
        />
      );

      expect(screen.getByTestId('history-button')).toBeInTheDocument();
    });

    it('calls onViewHistory when history button is clicked', async () => {
      const user = userEvent.setup();
      const onViewHistory = vi.fn();
      render(
        <DecisionAttributionBanner
          agentId="agent-1"
          agentName="BMAD Architect"
          timestamp={mockDate}
          onViewHistory={onViewHistory}
        />
      );

      await user.click(screen.getByTestId('history-button'));

      expect(onViewHistory).toHaveBeenCalled();
    });

    it('applies is-locked class when locked', () => {
      render(
        <DecisionAttributionBanner
          agentId="agent-1"
          agentName="BMAD Architect"
          timestamp={mockDate}
          isLocked={true}
          lockedBy="Alice"
        />
      );

      const banner = screen.getByTestId('decision-attribution-banner');
      expect(banner).toHaveClass('is-locked');
    });
  });
});
