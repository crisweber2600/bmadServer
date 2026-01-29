import { describe, it, expect, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { ConflictResolutionPanel, type ConflictData } from './ConflictResolutionPanel';

describe('ConflictResolutionPanel', () => {
  const mockConflict: ConflictData = {
    id: 'conflict-1',
    type: 'Contradicting decisions',
    description: 'Two decisions have conflicting information about the architecture approach.',
    severity: 'medium',
    decision1: {
      id: 'decision-1',
      title: 'Use Microservices',
      content: 'The architecture should use microservices for scalability.',
      author: 'Alice',
      timestamp: new Date('2024-01-15T10:00:00Z'),
    },
    decision2: {
      id: 'decision-2',
      title: 'Use Monolith',
      content: 'The architecture should use a monolithic approach for simplicity.',
      author: 'Bob',
      timestamp: new Date('2024-01-15T11:00:00Z'),
    },
    createdAt: new Date('2024-01-15T12:00:00Z'),
  };

  const defaultProps = {
    conflict: mockConflict,
    open: true,
    onClose: vi.fn(),
    onResolved: vi.fn(),
    onOverride: vi.fn(),
  };

  it('renders when open is true', () => {
    render(<ConflictResolutionPanel {...defaultProps} />);
    
    expect(screen.getByTestId('conflict-resolution-panel')).toBeInTheDocument();
  });

  it('does not render when open is false', () => {
    render(<ConflictResolutionPanel {...defaultProps} open={false} />);
    
    expect(screen.queryByTestId('conflict-resolution-panel')).not.toBeInTheDocument();
  });

  it('displays conflict information', () => {
    render(<ConflictResolutionPanel {...defaultProps} />);
    
    expect(screen.getByText('Contradicting decisions')).toBeInTheDocument();
    expect(screen.getByText(/Two decisions have conflicting information/)).toBeInTheDocument();
  });

  it('shows side-by-side decision comparison in DiffViewer', () => {
    render(<ConflictResolutionPanel {...defaultProps} />);
    
    expect(screen.getByText(/Decision 1: Use Microservices/)).toBeInTheDocument();
    expect(screen.getByText(/Decision 2: Use Monolith/)).toBeInTheDocument();
  });

  it('displays decision metadata (authors and timestamps)', () => {
    render(<ConflictResolutionPanel {...defaultProps} />);
    
    expect(screen.getByText(/Decision 1 by Alice/)).toBeInTheDocument();
    expect(screen.getByText(/Decision 2 by Bob/)).toBeInTheDocument();
  });

  it('shows resolution options by default', () => {
    render(<ConflictResolutionPanel {...defaultProps} />);
    
    expect(screen.getByTestId('resolution-options')).toBeInTheDocument();
    expect(screen.getByTestId('choice-decision1')).toBeInTheDocument();
    expect(screen.getByTestId('choice-decision2')).toBeInTheDocument();
    expect(screen.getByTestId('choice-merge')).toBeInTheDocument();
  });

  it('disables resolve button when no choice is selected', () => {
    render(<ConflictResolutionPanel {...defaultProps} />);
    
    expect(screen.getByTestId('resolve-button')).toBeDisabled();
  });

  it('enables resolve button when a choice is selected', async () => {
    const user = userEvent.setup();
    render(<ConflictResolutionPanel {...defaultProps} />);
    
    await user.click(screen.getByTestId('choice-decision1'));
    
    expect(screen.getByTestId('resolve-button')).not.toBeDisabled();
  });

  it('calls onResolved with decision1 when Accept Decision 1 is selected', async () => {
    const user = userEvent.setup();
    const onResolved = vi.fn();
    render(<ConflictResolutionPanel {...defaultProps} onResolved={onResolved} />);
    
    await user.click(screen.getByTestId('choice-decision1'));
    await user.click(screen.getByTestId('resolve-button'));
    
    expect(onResolved).toHaveBeenCalledWith('conflict-1', {
      selectedDecisionId: 'decision-1',
      resolutionNotes: '',
    });
  });

  it('calls onResolved with decision2 when Accept Decision 2 is selected', async () => {
    const user = userEvent.setup();
    const onResolved = vi.fn();
    render(<ConflictResolutionPanel {...defaultProps} onResolved={onResolved} />);
    
    await user.click(screen.getByTestId('choice-decision2'));
    await user.click(screen.getByTestId('resolve-button'));
    
    expect(onResolved).toHaveBeenCalledWith('conflict-1', {
      selectedDecisionId: 'decision-2',
      resolutionNotes: '',
    });
  });

  it('includes resolution notes when provided', async () => {
    const user = userEvent.setup();
    const onResolved = vi.fn();
    render(<ConflictResolutionPanel {...defaultProps} onResolved={onResolved} />);
    
    await user.click(screen.getByTestId('choice-decision1'));
    await user.type(screen.getByTestId('resolution-notes-input'), 'Microservices are better for this use case');
    await user.click(screen.getByTestId('resolve-button'));
    
    expect(onResolved).toHaveBeenCalledWith('conflict-1', {
      selectedDecisionId: 'decision-1',
      resolutionNotes: 'Microservices are better for this use case',
    });
  });

  it('shows override section when Override Warning is clicked', async () => {
    const user = userEvent.setup();
    render(<ConflictResolutionPanel {...defaultProps} />);
    
    await user.click(screen.getByTestId('show-override-button'));
    
    await waitFor(() => {
      expect(screen.getByTestId('override-section')).toBeInTheDocument();
      expect(screen.queryByTestId('resolution-options')).not.toBeInTheDocument();
    });
  });

  it('disables override button when justification is empty', async () => {
    const user = userEvent.setup();
    render(<ConflictResolutionPanel {...defaultProps} />);
    
    await user.click(screen.getByTestId('show-override-button'));
    
    expect(screen.getByTestId('confirm-override-button')).toBeDisabled();
  });

  it('enables override button when justification is provided', async () => {
    const user = userEvent.setup();
    render(<ConflictResolutionPanel {...defaultProps} />);
    
    await user.click(screen.getByTestId('show-override-button'));
    await user.type(screen.getByTestId('override-justification-input'), 'Team decided to proceed');
    
    expect(screen.getByTestId('confirm-override-button')).not.toBeDisabled();
  });

  it('calls onOverride with justification when confirmed', async () => {
    const user = userEvent.setup();
    const onOverride = vi.fn();
    render(<ConflictResolutionPanel {...defaultProps} onOverride={onOverride} />);
    
    await user.click(screen.getByTestId('show-override-button'));
    await user.type(screen.getByTestId('override-justification-input'), 'Team decided to proceed anyway');
    await user.click(screen.getByTestId('confirm-override-button'));
    
    expect(onOverride).toHaveBeenCalledWith('conflict-1', 'Team decided to proceed anyway');
  });

  it('returns to resolution options when back button is clicked', async () => {
    const user = userEvent.setup();
    render(<ConflictResolutionPanel {...defaultProps} />);
    
    await user.click(screen.getByTestId('show-override-button'));
    expect(screen.getByTestId('override-section')).toBeInTheDocument();
    
    await user.click(screen.getByTestId('back-to-resolution-button'));
    
    await waitFor(() => {
      expect(screen.getByTestId('resolution-options')).toBeInTheDocument();
      expect(screen.queryByTestId('override-section')).not.toBeInTheDocument();
    });
  });

  it('calls onClose when cancel button is clicked', async () => {
    const user = userEvent.setup();
    const onClose = vi.fn();
    render(<ConflictResolutionPanel {...defaultProps} onClose={onClose} />);
    
    await user.click(screen.getByTestId('cancel-button'));
    
    expect(onClose).toHaveBeenCalled();
  });

  it('shows loading state on resolve button', () => {
    render(<ConflictResolutionPanel {...defaultProps} isLoading={true} />);
    
    const resolveButton = screen.getByTestId('resolve-button');
    expect(resolveButton).toHaveClass('ant-btn-loading');
  });

  describe('severity display', () => {
    it('displays conflict info for high severity conflicts', () => {
      const highSeverityConflict = { ...mockConflict, severity: 'high' as const };
      render(<ConflictResolutionPanel {...defaultProps} conflict={highSeverityConflict} />);
      
      expect(screen.getByTestId('conflict-info')).toBeInTheDocument();
      expect(screen.getByText('Contradicting decisions')).toBeInTheDocument();
    });

    it('displays conflict info for medium severity conflicts', () => {
      render(<ConflictResolutionPanel {...defaultProps} />);
      
      expect(screen.getByTestId('conflict-info')).toBeInTheDocument();
    });

    it('displays conflict info for low severity conflicts', () => {
      const lowSeverityConflict = { ...mockConflict, severity: 'low' as const };
      render(<ConflictResolutionPanel {...defaultProps} conflict={lowSeverityConflict} />);
      
      expect(screen.getByTestId('conflict-info')).toBeInTheDocument();
    });
  });
});
