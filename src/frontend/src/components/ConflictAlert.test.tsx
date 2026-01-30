import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { ConflictAlert, type ConflictSeverity } from './ConflictAlert';

describe('ConflictAlert', () => {
  const defaultProps = {
    conflictId: 'conflict-1',
    severity: 'medium' as ConflictSeverity,
    conflictType: 'Contradicting decisions',
    description: 'Two decisions have conflicting information',
    onResolve: vi.fn(),
    onDismiss: vi.fn(),
  };

  it('renders conflict type message', () => {
    render(<ConflictAlert {...defaultProps} />);
    
    expect(screen.getByText(/Contradicting decisions/)).toBeInTheDocument();
  });

  it('displays severity tag', () => {
    render(<ConflictAlert {...defaultProps} />);
    
    expect(screen.getByText(/Medium Severity/)).toBeInTheDocument();
  });

  it('renders description when provided', () => {
    render(<ConflictAlert {...defaultProps} />);
    
    expect(screen.getByText(/Two decisions have conflicting information/)).toBeInTheDocument();
  });

  it('calls onResolve when resolve button is clicked', () => {
    const onResolve = vi.fn();
    render(<ConflictAlert {...defaultProps} onResolve={onResolve} />);
    
    fireEvent.click(screen.getByTestId('resolve-button'));
    
    expect(onResolve).toHaveBeenCalledWith('conflict-1');
  });

  it('calls onDismiss when dismiss button is clicked', () => {
    const onDismiss = vi.fn();
    render(<ConflictAlert {...defaultProps} onDismiss={onDismiss} />);
    
    fireEvent.click(screen.getByTestId('dismiss-button'));
    
    expect(onDismiss).toHaveBeenCalled();
  });

  it('applies correct CSS class for high severity', () => {
    const { container } = render(
      <ConflictAlert {...defaultProps} severity="high" />
    );
    
    expect(container.querySelector('.severity-high')).toBeInTheDocument();
  });

  it('applies correct CSS class for low severity', () => {
    const { container } = render(
      <ConflictAlert {...defaultProps} severity="low" />
    );
    
    expect(container.querySelector('.severity-low')).toBeInTheDocument();
  });

  it('applies correct CSS class for info severity', () => {
    const { container } = render(
      <ConflictAlert {...defaultProps} severity="info" />
    );
    
    expect(container.querySelector('.severity-info')).toBeInTheDocument();
  });

  it('renders without resolve button when showResolve is false', () => {
    render(<ConflictAlert {...defaultProps} showResolve={false} />);
    
    expect(screen.queryByTestId('resolve-button')).not.toBeInTheDocument();
  });

  it('renders without dismiss button when showDismiss is false', () => {
    render(<ConflictAlert {...defaultProps} showDismiss={false} />);
    
    expect(screen.queryByTestId('dismiss-button')).not.toBeInTheDocument();
  });

  it('renders without resolve button when onResolve is not provided', () => {
    render(<ConflictAlert {...defaultProps} onResolve={undefined} />);
    
    expect(screen.queryByTestId('resolve-button')).not.toBeInTheDocument();
  });

  it('renders without dismiss button when onDismiss is not provided', () => {
    render(<ConflictAlert {...defaultProps} onDismiss={undefined} />);
    
    expect(screen.queryByTestId('dismiss-button')).not.toBeInTheDocument();
  });

  it('has accessible alert role', () => {
    render(<ConflictAlert {...defaultProps} />);
    
    expect(screen.getByRole('alert')).toBeInTheDocument();
  });

  it('renders with correct data-testid', () => {
    render(<ConflictAlert {...defaultProps} />);
    
    expect(screen.getByTestId('conflict-alert')).toBeInTheDocument();
  });

  describe('severity display', () => {
    it('displays High Severity tag for high severity', () => {
      render(<ConflictAlert {...defaultProps} severity="high" />);
      
      expect(screen.getByText(/High Severity/)).toBeInTheDocument();
    });

    it('displays Low Severity tag for low severity', () => {
      render(<ConflictAlert {...defaultProps} severity="low" />);
      
      expect(screen.getByText(/Low Severity/)).toBeInTheDocument();
    });

    it('displays Info Severity tag for info severity', () => {
      render(<ConflictAlert {...defaultProps} severity="info" />);
      
      expect(screen.getByText(/Info Severity/)).toBeInTheDocument();
    });
  });

  describe('alert type mapping', () => {
    it('renders error alert for high severity', () => {
      const { container } = render(
        <ConflictAlert {...defaultProps} severity="high" />
      );
      
      expect(container.querySelector('.ant-alert-error')).toBeInTheDocument();
    });

    it('renders warning alert for medium severity', () => {
      const { container } = render(
        <ConflictAlert {...defaultProps} severity="medium" />
      );
      
      expect(container.querySelector('.ant-alert-warning')).toBeInTheDocument();
    });

    it('renders info alert for low severity', () => {
      const { container } = render(
        <ConflictAlert {...defaultProps} severity="low" />
      );
      
      expect(container.querySelector('.ant-alert-info')).toBeInTheDocument();
    });
  });
});
