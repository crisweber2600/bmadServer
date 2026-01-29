import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { CheckpointList, CheckpointResponse } from './CheckpointList';

// Mock antd message
vi.mock('antd', async () => {
  const actual = await vi.importActual<typeof import('antd')>('antd');
  return {
    ...actual,
    message: {
      success: vi.fn(),
      error: vi.fn(),
    },
  };
});

const mockCheckpoints: CheckpointResponse[] = [
  {
    Id: 'cp-1',
    WorkflowId: 'wf-1',
    StepId: 'step-1',
    CheckpointType: 'ExplicitSave',
    Version: 1,
    CreatedAt: '2025-01-20T10:00:00Z',
    TriggeredBy: 'user-1',
    Name: 'Initial Checkpoint',
    Description: 'First save point',
  },
  {
    Id: 'cp-2',
    WorkflowId: 'wf-1',
    StepId: 'step-2',
    CheckpointType: 'AutomaticSave',
    Version: 2,
    CreatedAt: '2025-01-20T11:00:00Z',
    TriggeredBy: 'system',
  },
  {
    Id: 'cp-3',
    WorkflowId: 'wf-1',
    StepId: 'phase-boundary',
    CheckpointType: 'PhaseBoundary',
    Version: 3,
    CreatedAt: '2025-01-20T12:00:00Z',
    TriggeredBy: 'system',
  },
];

describe('CheckpointList', () => {
  const user = userEvent.setup();

  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  describe('Rendering States', () => {
    it('renders loading state when fetching checkpoints', () => {
      render(
        <CheckpointList
          workflowId="wf-1"
          fetchCheckpoints={() => new Promise(() => {})}
        />
      );

      expect(screen.getByTestId('checkpoint-list')).toBeInTheDocument();
      expect(screen.getByText('Checkpoints')).toBeInTheDocument();
      expect(document.querySelector('.ant-skeleton')).toBeInTheDocument();
    });

    it('renders loading state when loading prop is true', () => {
      render(
        <CheckpointList
          workflowId="wf-1"
          checkpoints={mockCheckpoints}
          loading={true}
        />
      );

      expect(document.querySelector('.ant-skeleton')).toBeInTheDocument();
    });

    it('renders Timeline with checkpoint entries when data loads', () => {
      render(
        <CheckpointList
          workflowId="wf-1"
          checkpoints={mockCheckpoints}
        />
      );

      expect(screen.getByTestId('checkpoint-timeline')).toBeInTheDocument();
      expect(screen.getByText('Initial Checkpoint')).toBeInTheDocument();
      expect(screen.getByText('Checkpoint v2')).toBeInTheDocument();
      expect(screen.getByText('Checkpoint v3')).toBeInTheDocument();
    });

    it('shows Empty component when checkpoints array is empty', () => {
      render(
        <CheckpointList
          workflowId="wf-1"
          checkpoints={[]}
        />
      );

      expect(screen.getByTestId('empty-checkpoints')).toBeInTheDocument();
      expect(screen.getByText('No checkpoints saved')).toBeInTheDocument();
    });

    it('renders checkpoint type tags correctly', () => {
      render(
        <CheckpointList
          workflowId="wf-1"
          checkpoints={mockCheckpoints}
        />
      );

      expect(screen.getByText('Manual Save')).toBeInTheDocument();
      expect(screen.getByText('Auto Save')).toBeInTheDocument();
      expect(screen.getByText('Phase Boundary')).toBeInTheDocument();
    });

    it('renders checkpoint description when provided', () => {
      render(
        <CheckpointList
          workflowId="wf-1"
          checkpoints={mockCheckpoints}
        />
      );

      expect(screen.getByText('First save point')).toBeInTheDocument();
    });
  });

  describe('Async Loading', () => {
    it('fetches checkpoints using fetchCheckpoints prop', async () => {
      const fetchCheckpoints = vi.fn().mockResolvedValue(mockCheckpoints);

      render(
        <CheckpointList
          workflowId="wf-1"
          fetchCheckpoints={fetchCheckpoints}
        />
      );

      await waitFor(() => {
        expect(fetchCheckpoints).toHaveBeenCalled();
      });

      await waitFor(() => {
        expect(screen.getByText('Initial Checkpoint')).toBeInTheDocument();
      });
    });

    it('shows error message and retry button when fetch fails', async () => {
      const fetchCheckpoints = vi.fn().mockRejectedValue(new Error('Network error'));

      render(
        <CheckpointList
          workflowId="wf-1"
          fetchCheckpoints={fetchCheckpoints}
        />
      );

      await waitFor(() => {
        expect(screen.getByText('Network error')).toBeInTheDocument();
      });

      expect(screen.getByText('Retry')).toBeInTheDocument();
    });

    it('retries loading when retry button is clicked', async () => {
      const fetchCheckpoints = vi
        .fn()
        .mockRejectedValueOnce(new Error('Network error'))
        .mockResolvedValueOnce(mockCheckpoints);

      render(
        <CheckpointList
          workflowId="wf-1"
          fetchCheckpoints={fetchCheckpoints}
        />
      );

      await waitFor(() => {
        expect(screen.getByText('Retry')).toBeInTheDocument();
      });

      await user.click(screen.getByText('Retry'));

      await waitFor(() => {
        expect(fetchCheckpoints).toHaveBeenCalledTimes(2);
      });
    });
  });

  describe('Restore Functionality', () => {
    it('shows Restore buttons when onRestore is provided', () => {
      render(
        <CheckpointList
          workflowId="wf-1"
          checkpoints={mockCheckpoints}
          onRestore={vi.fn()}
        />
      );

      expect(screen.getByTestId('restore-cp-1')).toBeInTheDocument();
      expect(screen.getByTestId('restore-cp-2')).toBeInTheDocument();
      expect(screen.getByTestId('restore-cp-3')).toBeInTheDocument();
    });

    it('does not show Restore buttons when onRestore is not provided', () => {
      render(
        <CheckpointList
          workflowId="wf-1"
          checkpoints={mockCheckpoints}
        />
      );

      expect(screen.queryByTestId('restore-cp-1')).not.toBeInTheDocument();
    });

    it('shows confirmation modal when Restore is clicked', async () => {
      const onRestore = vi.fn();

      render(
        <CheckpointList
          workflowId="wf-1"
          checkpoints={mockCheckpoints}
          onRestore={onRestore}
        />
      );

      await user.click(screen.getByTestId('restore-cp-1'));

      await waitFor(() => {
        // Check that the confirmation modal is shown by looking for the modal structure
        expect(screen.getByRole('dialog')).toBeInTheDocument();
        expect(screen.getByText(/cannot be undone/i)).toBeInTheDocument();
      });
    });
  });

  describe('Create Checkpoint', () => {
    it('shows Create Checkpoint button when onCreateCheckpoint is provided', () => {
      render(
        <CheckpointList
          workflowId="wf-1"
          checkpoints={mockCheckpoints}
          onCreateCheckpoint={vi.fn()}
        />
      );

      expect(screen.getByTestId('create-checkpoint-button')).toBeInTheDocument();
    });

    it('does not show Create Checkpoint button when onCreateCheckpoint is not provided', () => {
      render(
        <CheckpointList
          workflowId="wf-1"
          checkpoints={mockCheckpoints}
        />
      );

      expect(screen.queryByTestId('create-checkpoint-button')).not.toBeInTheDocument();
    });

    it('opens modal when Create Checkpoint button is clicked', async () => {
      render(
        <CheckpointList
          workflowId="wf-1"
          checkpoints={mockCheckpoints}
          onCreateCheckpoint={vi.fn()}
        />
      );

      await user.click(screen.getByTestId('create-checkpoint-button'));

      await waitFor(() => {
        // Check that the create checkpoint modal is shown by looking for the input
        expect(screen.getByTestId('checkpoint-name-input')).toBeInTheDocument();
      });
    });

    it('validates name field is required', async () => {
      render(
        <CheckpointList
          workflowId="wf-1"
          checkpoints={mockCheckpoints}
          onCreateCheckpoint={vi.fn()}
        />
      );

      await user.click(screen.getByTestId('create-checkpoint-button'));

      await waitFor(() => {
        expect(screen.getByTestId('checkpoint-name-input')).toBeInTheDocument();
      });

      // Submit form without entering name
      await user.click(screen.getByText('Create Checkpoint', { selector: 'button[type="submit"] span' }));

      await waitFor(() => {
        expect(screen.getByText('Please enter a name for this checkpoint')).toBeInTheDocument();
      });
    });
  });

  describe('Accessibility', () => {
    it('should have proper accessibility attributes on main container', () => {
      render(
        <CheckpointList
          workflowId="wf-1"
          checkpoints={mockCheckpoints}
          onRestore={vi.fn()}
          onCreateCheckpoint={vi.fn()}
        />
      );

      const container = screen.getByTestId('checkpoint-list');
      expect(container).toBeInTheDocument();
      // Timeline has implicit list semantics
      expect(screen.getByTestId('checkpoint-timeline')).toBeInTheDocument();
    });

    it('should have proper accessibility on empty state', () => {
      render(
        <CheckpointList
          workflowId="wf-1"
          checkpoints={[]}
        />
      );

      const emptyState = screen.getByTestId('empty-checkpoints');
      expect(emptyState).toBeInTheDocument();
    });

    it('should have proper accessibility on buttons', () => {
      render(
        <CheckpointList
          workflowId="wf-1"
          checkpoints={mockCheckpoints}
          onRestore={vi.fn()}
          onCreateCheckpoint={vi.fn()}
        />
      );

      // Create Checkpoint button
      const createButton = screen.getByTestId('create-checkpoint-button');
      expect(createButton).toHaveAttribute('type', 'button');
      
      // Restore buttons
      const restoreButton = screen.getByTestId('restore-cp-1');
      expect(restoreButton).toHaveAttribute('type', 'button');
    });
  });
});
