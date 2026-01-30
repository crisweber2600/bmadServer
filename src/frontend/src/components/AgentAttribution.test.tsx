import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { AgentAttribution } from './AgentAttribution';

describe('AgentAttribution Component', () => {
  const testProps = {
    agentId: 'agent-001',
    agentName: 'BMAD Architect',
    agentDescription: 'Designs and reviews system architecture',
    capabilities: ['System Design', 'Code Review', 'Architecture'],
    currentStepResponsibility: 'Designing the system architecture',
    timestamp: new Date('2026-01-26T10:30:00Z'),
  };

  describe('inline variant', () => {
    it('renders agent name and avatar in inline layout', () => {
      render(<AgentAttribution {...testProps} variant="inline" />);
      
      expect(screen.getByText('BMAD Architect')).toBeInTheDocument();
      expect(screen.getByLabelText('BMAD Architect')).toBeInTheDocument();
    });

    it('applies inline styling classes', () => {
      const { container } = render(
        <AgentAttribution {...testProps} variant="inline" />
      );
      
      expect(container.querySelector('.agent-attribution-inline')).toBeInTheDocument();
    });
  });

  describe('block variant', () => {
    it('renders with block layout and all attribution details', () => {
      render(<AgentAttribution {...testProps} variant="block" />);
      
      expect(screen.getByText('BMAD Architect')).toBeInTheDocument();
      expect(screen.getByText('Jan 26, 10:30 AM')).toBeInTheDocument();
    });

    it('applies block styling classes', () => {
      const { container } = render(
        <AgentAttribution {...testProps} variant="block" />
      );
      
      expect(container.querySelector('.agent-attribution-block')).toBeInTheDocument();
    });
  });

  describe('tooltip', () => {
    it('displays tooltip content on hover', async () => {
      const user = userEvent.setup();
      const { container } = render(
        <AgentAttribution {...testProps} variant="block" />
      );
      
      const element = container.querySelector('.agent-attribution-block');
      expect(element).toBeInTheDocument();
      
      await user.hover(element!);
      
      expect(screen.getByText('BMAD Architect')).toBeInTheDocument();
      expect(screen.getByText('(agent-001)')).toBeInTheDocument();
      expect(
        screen.getByText('Designs and reviews system architecture')
      ).toBeInTheDocument();
    });

    it('shows capabilities in tooltip', async () => {
      const user = userEvent.setup();
      const { container } = render(
        <AgentAttribution {...testProps} variant="block" />
      );
      
      const element = container.querySelector('.agent-attribution-block');
      await user.hover(element!);
      
      expect(screen.getByText('System Design')).toBeInTheDocument();
      expect(screen.getByText('Code Review')).toBeInTheDocument();
      expect(screen.getByText('Architecture')).toBeInTheDocument();
    });

    it('shows current step responsibility in tooltip', async () => {
      const user = userEvent.setup();
      const { container } = render(
        <AgentAttribution {...testProps} variant="block" />
      );
      
      const element = container.querySelector('.agent-attribution-block');
      await user.hover(element!);
      
      expect(
        screen.getByText('Designing the system architecture')
      ).toBeInTheDocument();
    });
  });

  describe('avatar sizing', () => {
    it('renders small avatar when size prop is small', () => {
      const { container } = render(
        <AgentAttribution {...testProps} size="small" variant="inline" />
      );
      
      const avatar = container.querySelector('.agent-avatar.small');
      expect(avatar).toBeInTheDocument();
    });

    it('renders large avatar when size prop is large', () => {
      const { container } = render(
        <AgentAttribution {...testProps} size="large" variant="block" />
      );
      
      const avatar = container.querySelector('.agent-avatar.large');
      expect(avatar).toBeInTheDocument();
    });
  });

  describe('timestamp formatting', () => {
    it('formats timestamp in 12-hour format with month and day', () => {
      render(<AgentAttribution {...testProps} variant="block" />);
      
      expect(screen.getByText('Jan 26, 10:30 AM')).toBeInTheDocument();
    });

    it('handles different times correctly', () => {
      const afternoonTime = new Date('2026-01-26T16:45:30Z');
      render(
        <AgentAttribution
          {...testProps}
          timestamp={afternoonTime}
          variant="block"
        />
      );
      
      expect(screen.getByText('Jan 26, 04:45 PM')).toBeInTheDocument();
    });
  });

  describe('optional props', () => {
    it('renders without description when not provided', () => {
      const { agentDescription, ...propsWithoutDescription } = testProps;
      render(
        <AgentAttribution {...propsWithoutDescription} variant="block" />
      );
      
      expect(screen.getByText('BMAD Architect')).toBeInTheDocument();
    });

    it('renders without capabilities when empty array', () => {
      render(
        <AgentAttribution
          {...testProps}
          capabilities={[]}
          variant="block"
        />
      );
      
      expect(screen.getByText('BMAD Architect')).toBeInTheDocument();
    });

    it('renders with custom avatar URL when provided', () => {
      const { container } = render(
        <AgentAttribution
          {...testProps}
          avatarUrl="https://example.com/avatar.png"
          variant="block"
        />
      );
      
      const img = container.querySelector('img[src="https://example.com/avatar.png"]');
      expect(img).toBeInTheDocument();
    });
  });

  describe('accessibility', () => {
    it('has aria-label on avatar', () => {
      render(<AgentAttribution {...testProps} variant="block" />);
      
      expect(screen.getByLabelText('BMAD Architect')).toBeInTheDocument();
    });

    it('provides proper semantic structure', () => {
      const { container } = render(
        <AgentAttribution {...testProps} variant="block" />
      );
      
      const attributionBlock = container.querySelector('.agent-attribution-block');
      expect(attributionBlock).toBeInTheDocument();
    });
  });

  describe('avatar color generation', () => {
    it('generates consistent color for same agent ID', () => {
      const { container: container1 } = render(
        <AgentAttribution {...testProps} variant="block" />
      );
      const { container: container2 } = render(
        <AgentAttribution {...testProps} variant="block" />
      );
      
      const avatar1 = container1.querySelector('.agent-avatar');
      const avatar2 = container2.querySelector('.agent-avatar');
      
      const color1 = window.getComputedStyle(avatar1!).backgroundColor;
      const color2 = window.getComputedStyle(avatar2!).backgroundColor;
      
      expect(color1).toBe(color2);
    });

    it('generates different color for different agent IDs', () => {
      const { container: container1 } = render(
        <AgentAttribution {...testProps} variant="block" />
      );
      const { container: container2 } = render(
        <AgentAttribution
          {...testProps}
          agentId="different-agent-id"
          variant="block"
        />
      );
      
      const avatar1 = container1.querySelector('.agent-avatar');
      const avatar2 = container2.querySelector('.agent-avatar');
      
      const color1 = window.getComputedStyle(avatar1!).backgroundColor;
      const color2 = window.getComputedStyle(avatar2!).backgroundColor;
      
      expect(color1).not.toBe(color2);
    });
  });
});
