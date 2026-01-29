import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { ReviewRequestForm, type Reviewer } from './ReviewRequestForm';

describe('ReviewRequestForm', () => {
  const mockReviewers: Reviewer[] = [
    { Id: 'user-1', DisplayName: 'Alice Smith', Email: 'alice@example.com' },
    { Id: 'user-2', DisplayName: 'Bob Johnson', Email: 'bob@example.com' },
    { Id: 'user-3', DisplayName: 'Carol Williams', Email: 'carol@example.com' },
  ];

  const defaultProps = {
    decisionId: 'decision-1',
    workflowId: 'workflow-1',
    open: true,
    onClose: vi.fn(),
    onSuccess: vi.fn(),
    onSubmit: vi.fn().mockResolvedValue(undefined),
    availableReviewers: mockReviewers,
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders when open is true', () => {
    render(<ReviewRequestForm {...defaultProps} />);
    
    expect(screen.getByTestId('review-request-form')).toBeInTheDocument();
  });

  it('does not render when open is false', () => {
    render(<ReviewRequestForm {...defaultProps} open={false} />);
    
    expect(screen.queryByTestId('review-request-form')).not.toBeInTheDocument();
  });

  it('shows reviewer select populated with availableReviewers', () => {
    render(<ReviewRequestForm {...defaultProps} />);
    
    expect(screen.getByTestId('reviewer-select')).toBeInTheDocument();
  });

  it('shows deadline picker', () => {
    render(<ReviewRequestForm {...defaultProps} />);
    
    expect(screen.getByTestId('deadline-picker')).toBeInTheDocument();
  });

  it('shows loading spinner when fetching reviewers', async () => {
    const fetchReviewers = vi.fn().mockImplementation(() => new Promise(() => {})); // Never resolves
    render(
      <ReviewRequestForm 
        {...defaultProps} 
        availableReviewers={undefined}
        fetchReviewers={fetchReviewers}
      />
    );
    
    await waitFor(() => {
      expect(screen.getByTestId('loading-reviewers')).toBeInTheDocument();
    });
  });

  it('shows error message when fetch fails', async () => {
    const fetchReviewers = vi.fn().mockRejectedValue(new Error('Network error'));
    render(
      <ReviewRequestForm 
        {...defaultProps} 
        availableReviewers={undefined}
        fetchReviewers={fetchReviewers}
      />
    );
    
    await waitFor(() => {
      expect(screen.getByTestId('load-error')).toBeInTheDocument();
      expect(screen.getByText('Failed to load reviewers')).toBeInTheDocument();
    });
  });

  it('shows retry button when fetch fails', async () => {
    const fetchReviewers = vi.fn().mockRejectedValue(new Error('Network error'));
    render(
      <ReviewRequestForm 
        {...defaultProps} 
        availableReviewers={undefined}
        fetchReviewers={fetchReviewers}
      />
    );
    
    await waitFor(() => {
      expect(screen.getByTestId('retry-button')).toBeInTheDocument();
    });
  });

  it('retries fetch when retry button is clicked', async () => {
    const user = userEvent.setup();
    const fetchReviewers = vi.fn()
      .mockRejectedValueOnce(new Error('Network error'))
      .mockResolvedValueOnce(mockReviewers);
    
    render(
      <ReviewRequestForm 
        {...defaultProps} 
        availableReviewers={undefined}
        fetchReviewers={fetchReviewers}
      />
    );
    
    await waitFor(() => {
      expect(screen.getByTestId('retry-button')).toBeInTheDocument();
    });

    await user.click(screen.getByTestId('retry-button'));

    expect(fetchReviewers).toHaveBeenCalledTimes(2);
  });

  it('populates select when fetch completes', async () => {
    const fetchReviewers = vi.fn().mockResolvedValue(mockReviewers);
    render(
      <ReviewRequestForm 
        {...defaultProps} 
        availableReviewers={undefined}
        fetchReviewers={fetchReviewers}
      />
    );
    
    await waitFor(() => {
      expect(screen.getByTestId('review-form')).toBeInTheDocument();
    });
  });

  it('shows validation error when submitting without selecting reviewers', async () => {
    const user = userEvent.setup();
    render(<ReviewRequestForm {...defaultProps} />);
    
    await user.click(screen.getByTestId('submit-button'));
    
    await waitFor(() => {
      expect(screen.getByText('Select at least one reviewer')).toBeInTheDocument();
    });
  });

  it('calls onSubmit with reviewer IDs when form is submitted', async () => {
    const user = userEvent.setup();
    const onSubmit = vi.fn().mockResolvedValue(undefined);
    render(<ReviewRequestForm {...defaultProps} onSubmit={onSubmit} />);
    
    // Open dropdown and select reviewer
    const select = screen.getByTestId('reviewer-select');
    await user.click(select);
    
    // Wait for dropdown options
    await waitFor(() => {
      expect(screen.getByText('Alice Smith')).toBeInTheDocument();
    });
    
    await user.click(screen.getByText('Alice Smith'));
    await user.click(screen.getByTestId('submit-button'));
    
    await waitFor(() => {
      expect(onSubmit).toHaveBeenCalledWith(expect.objectContaining({
        decisionId: 'decision-1',
        reviewerIds: ['user-1'],
      }));
    });
  });

  it('calls onSuccess after successful submission', async () => {
    const user = userEvent.setup();
    const onSuccess = vi.fn();
    const onSubmit = vi.fn().mockResolvedValue(undefined);
    render(<ReviewRequestForm {...defaultProps} onSubmit={onSubmit} onSuccess={onSuccess} />);
    
    const select = screen.getByTestId('reviewer-select');
    await user.click(select);
    
    await waitFor(() => {
      expect(screen.getByText('Alice Smith')).toBeInTheDocument();
    });
    
    await user.click(screen.getByText('Alice Smith'));
    await user.click(screen.getByTestId('submit-button'));
    
    await waitFor(() => {
      expect(onSuccess).toHaveBeenCalled();
    });
  });

  it('calls onClose when cancel button is clicked', async () => {
    const user = userEvent.setup();
    const onClose = vi.fn();
    render(<ReviewRequestForm {...defaultProps} onClose={onClose} />);
    
    await user.click(screen.getByTestId('cancel-button'));
    
    expect(onClose).toHaveBeenCalled();
  });

  it('closes form after successful submission', async () => {
    const user = userEvent.setup();
    const onClose = vi.fn();
    const onSubmit = vi.fn().mockResolvedValue(undefined);
    render(<ReviewRequestForm {...defaultProps} onSubmit={onSubmit} onClose={onClose} />);
    
    const select = screen.getByTestId('reviewer-select');
    await user.click(select);
    
    await waitFor(() => {
      expect(screen.getByText('Alice Smith')).toBeInTheDocument();
    });
    
    await user.click(screen.getByText('Alice Smith'));
    await user.click(screen.getByTestId('submit-button'));
    
    await waitFor(() => {
      expect(onClose).toHaveBeenCalled();
    });
  });
});
