import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { AgentHandoffIndicator } from './AgentHandoffIndicator';

describe('AgentHandoffIndicator Component', () => {
  const baseProps = {
    fromAgentId: 'agent-001',
    fromAgentName: 'BMAD Architect',
    toAgentId: 'agent-002',
    toAgentName: 'BMAD Developer',
    timestamp: new Date('2026-01-26T10:30:00Z'),
  };

  describe('basic rendering', () => {
    it('renders handoff indicator with required props', () => {
      render(<AgentHandoffIndicator {...baseProps} />);

      expect(
        screen.getByText(/Handing off to BMAD Developer/)
      ).toBeInTheDocument();
    });

    it('displays both agent avatars', () => {
      const { container } = render(
        <AgentHandoffIndicator {...baseProps} />
      );

      const avatars = container.querySelectorAll('span.ant-avatar');
      expect(avatars.length).toBeGreaterThanOrEqual(2);
    });

    it('displays swap icon between avatars', () => {
      const { container } = render(
        <AgentHandoffIndicator {...baseProps} />
      );

      const swapIcon = container.querySelector('.swap-icon');
      expect(swapIcon).toBeInTheDocument();
    });

    it('displays formatted timestamp', () => {
      render(<AgentHandoffIndicator {...baseProps} />);

      expect(screen.getByText(/10:30/)).toBeInTheDocument();
    });
  });

  describe('optional content', () => {
    it('displays step name when provided', () => {
      render(
        <AgentHandoffIndicator
          {...baseProps}
          stepName="Design Phase"
        />
      );

      expect(screen.getByText('Design Phase')).toBeInTheDocument();
      expect(screen.getByText(/Step:/)).toBeInTheDocument();
    });

    it('displays reason when provided', () => {
      render(
        <AgentHandoffIndicator
          {...baseProps}
          reason="Architecture review required"
        />
      );

      expect(screen.getByText('Architecture review required')).toBeInTheDocument();
      expect(screen.getByText(/Reason:/)).toBeInTheDocument();
    });

    it('does not display step when not provided', () => {
      const { container } = render(
        <AgentHandoffIndicator {...baseProps} />
      );

      const stepLabels = container.querySelectorAll('.label');
      const hasStepLabel = Array.from(stepLabels).some(
        label => label.textContent === 'Step:'
      );
      expect(hasStepLabel).toBe(false);
    });

    it('does not display reason when not provided', () => {
      const { container } = render(
        <AgentHandoffIndicator {...baseProps} />
      );

      const reasonLabels = container.querySelectorAll('.label');
      const hasReasonLabel = Array.from(reasonLabels).some(
        label => label.textContent === 'Reason:'
      );
      expect(hasReasonLabel).toBe(false);
    });
  });

  describe('avatar colors', () => {
    it('generates consistent colors for same agent IDs', () => {
      const { container: container1 } = render(
        <AgentHandoffIndicator {...baseProps} />
      );
      const { container: container2 } = render(
        <AgentHandoffIndicator {...baseProps} />
      );

      const avatars1 = container1.querySelectorAll('span.ant-avatar');
      const avatars2 = container2.querySelectorAll('span.ant-avatar');

      const color1 = window.getComputedStyle(avatars1[0]).backgroundColor;
      const color2 = window.getComputedStyle(avatars2[0]).backgroundColor;

      expect(color1).toBe(color2);
    });

    it('generates different colors for different agent IDs', () => {
      const { container } = render(
        <AgentHandoffIndicator {...baseProps} />
      );

      const avatars = container.querySelectorAll('span.ant-avatar');
      const fromColor = window.getComputedStyle(avatars[0]).backgroundColor;
      const toColor = window.getComputedStyle(avatars[1]).backgroundColor;

      expect(fromColor).not.toBe(toColor);
    });
  });

  describe('custom avatar URLs', () => {
    it('uses custom avatar URLs when provided', () => {
      const { container } = render(
        <AgentHandoffIndicator
          {...baseProps}
          fromAvatarUrl="https://example.com/agent1.png"
          toAvatarUrl="https://example.com/agent2.png"
        />
      );

      const images = container.querySelectorAll('img');
      expect(images.length).toBeGreaterThanOrEqual(2);
    });
  });

  describe('accessibility', () => {
    it('has proper role attribute', () => {
      const { container } = render(
        <AgentHandoffIndicator {...baseProps} />
      );

      const indicator = container.querySelector(
        '[role="status"]'
      );
      expect(indicator).toBeInTheDocument();
    });

    it('has descriptive aria-label', () => {
      const { container } = render(
        <AgentHandoffIndicator {...baseProps} />
      );

      const indicator = container.querySelector('[role="status"]');
      expect(indicator).toHaveAttribute('aria-label');
      expect(
        indicator?.getAttribute('aria-label')
      ).toContain('BMAD Architect');
      expect(
        indicator?.getAttribute('aria-label')
      ).toContain('BMAD Developer');
    });
  });

  describe('animations', () => {
    it('has slide-in animation class', () => {
      const { container } = render(
        <AgentHandoffIndicator {...baseProps} />
      );

      const indicator = container.querySelector(
        '.agent-handoff-indicator'
      );
      expect(indicator).toHaveClass('agent-handoff-indicator');
    });

    it('applies visible class for animation trigger', () => {
      const { container } = render(
        <AgentHandoffIndicator {...baseProps} />
      );

      const indicator = container.querySelector(
        '.agent-handoff-indicator'
      );
      expect(indicator).toHaveClass('visible');
    });
  });

  describe('timestamp formatting', () => {
    it('formats different times correctly', () => {
      const afternoonTime = new Date('2026-01-26T14:45:00Z');
      render(
        <AgentHandoffIndicator
          {...baseProps}
          timestamp={afternoonTime}
        />
      );

      expect(screen.getByText(/02:45|14:45/)).toBeInTheDocument();
    });
  });

  describe('complete integration', () => {
    it('renders all content together', () => {
      render(
        <AgentHandoffIndicator
          {...baseProps}
          stepName="Implementation Phase"
          reason="Development expertise needed"
          fromAvatarUrl="https://example.com/architect.png"
          toAvatarUrl="https://example.com/developer.png"
        />
      );

      expect(
        screen.getByText(/Handing off to BMAD Developer/)
      ).toBeInTheDocument();
      expect(screen.getByText('Implementation Phase')).toBeInTheDocument();
      expect(
        screen.getByText('Development expertise needed')
      ).toBeInTheDocument();
      expect(screen.getByText(/10:30/)).toBeInTheDocument();
    });
  });
});
