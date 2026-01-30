import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { AgentRosterGrid } from './AgentRosterGrid';
import type { AgentInfo } from './AgentRosterGrid';

const mockAgents: AgentInfo[] = [
  {
    agentId: 'agent-1',
    agentName: 'Winston',
    role: 'Architect',
    capabilities: ['Design', 'Planning', 'Review'],
    relevanceScore: 0.85,
    isActive: true,
  },
  {
    agentId: 'agent-2',
    agentName: 'Sally',
    role: 'UX Designer',
    capabilities: ['Wireframes', 'Prototyping'],
    relevanceScore: 0.5,
  },
  {
    agentId: 'agent-3',
    agentName: 'Murat',
    role: 'Developer',
    capabilities: ['Coding'],
    relevanceScore: 0.3,
  },
];

describe('AgentRosterGrid', () => {
  it('should render a grid of agent cards', () => {
    render(<AgentRosterGrid agents={mockAgents} />);

    expect(screen.getByTestId('agent-roster-grid')).toBeInTheDocument();
    expect(screen.getByTestId('agent-card-agent-1')).toBeInTheDocument();
    expect(screen.getByTestId('agent-card-agent-2')).toBeInTheDocument();
    expect(screen.getByTestId('agent-card-agent-3')).toBeInTheDocument();
  });

  it('should display agent name, role, and capabilities', () => {
    render(<AgentRosterGrid agents={mockAgents} />);

    expect(screen.getByText('Winston')).toBeInTheDocument();
    expect(screen.getByText('Architect')).toBeInTheDocument();
    expect(screen.getByText('Design')).toBeInTheDocument();
    expect(screen.getByText('Planning')).toBeInTheDocument();
  });

  it('should show "Highly Relevant" badge for relevanceScore > 0.7', () => {
    render(<AgentRosterGrid agents={mockAgents} />);

    expect(screen.getByText('Highly Relevant')).toBeInTheDocument();
  });

  it('should show "Relevant" badge for 0.4 < relevanceScore <= 0.7', () => {
    render(<AgentRosterGrid agents={mockAgents} />);

    expect(screen.getByText('Relevant')).toBeInTheDocument();
  });

  it('should show "Low Relevance" badge for relevanceScore <= 0.4', () => {
    render(<AgentRosterGrid agents={mockAgents} />);

    expect(screen.getByText('Low Relevance')).toBeInTheDocument();
  });

  it('should call onAgentSelect when a card is clicked', () => {
    const onAgentSelect = vi.fn();
    render(<AgentRosterGrid agents={mockAgents} onAgentSelect={onAgentSelect} />);

    fireEvent.click(screen.getByTestId('agent-card-agent-1'));
    expect(onAgentSelect).toHaveBeenCalledWith('agent-1');
  });

  it('should call onAgentSelect on Enter key press', () => {
    const onAgentSelect = vi.fn();
    render(<AgentRosterGrid agents={mockAgents} onAgentSelect={onAgentSelect} />);

    const card = screen.getByTestId('agent-card-agent-2');
    fireEvent.keyDown(card, { key: 'Enter' });
    expect(onAgentSelect).toHaveBeenCalledWith('agent-2');
  });

  it('should call onAgentSelect on Space key press', () => {
    const onAgentSelect = vi.fn();
    render(<AgentRosterGrid agents={mockAgents} onAgentSelect={onAgentSelect} />);

    const card = screen.getByTestId('agent-card-agent-3');
    fireEvent.keyDown(card, { key: ' ' });
    expect(onAgentSelect).toHaveBeenCalledWith('agent-3');
  });

  it('should limit displayed capabilities to maxCapabilities', () => {
    render(<AgentRosterGrid agents={mockAgents} maxCapabilities={2} />);

    // Winston has 3 capabilities, should show 2 + "+1" overflow
    expect(screen.getByText('Design')).toBeInTheDocument();
    expect(screen.getByText('Planning')).toBeInTheDocument();
    expect(screen.getByText('+1')).toBeInTheDocument();
  });

  it('should show empty state when no agents', () => {
    render(<AgentRosterGrid agents={[]} />);

    expect(screen.getByTestId('agent-roster-empty')).toBeInTheDocument();
    expect(screen.getByText('No agents available')).toBeInTheDocument();
  });

  it('should not show relevance badges when showRelevanceBadges is false', () => {
    render(<AgentRosterGrid agents={mockAgents} showRelevanceBadges={false} />);

    expect(screen.queryByText('Highly Relevant')).not.toBeInTheDocument();
    expect(screen.queryByText('Relevant')).not.toBeInTheDocument();
    expect(screen.queryByText('Low Relevance')).not.toBeInTheDocument();
  });

  it('should have proper accessibility attributes', () => {
    render(<AgentRosterGrid agents={mockAgents} />);

    const grid = screen.getByTestId('agent-roster-grid');
    expect(grid).toHaveAttribute('role', 'group');
    expect(grid).toHaveAttribute('aria-label', 'Available agents');

    const cards = screen.getAllByTestId(/agent-card-/);
    expect(cards).toHaveLength(3);
  });

  it('should apply active class to active agents', () => {
    render(<AgentRosterGrid agents={mockAgents} />);

    const activeCard = screen.getByTestId('agent-card-agent-1');
    expect(activeCard).toHaveClass('active');
  });

  it('should handle agents without optional properties', () => {
    const minimalAgent: AgentInfo = {
      agentId: 'minimal',
      agentName: 'MinimalAgent',
    };

    render(<AgentRosterGrid agents={[minimalAgent]} />);

    expect(screen.getByText('MinimalAgent')).toBeInTheDocument();
    expect(screen.queryByText('Highly Relevant')).not.toBeInTheDocument();
  });
});
