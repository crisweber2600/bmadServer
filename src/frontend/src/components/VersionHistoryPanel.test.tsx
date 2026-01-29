import { describe, it, expect, vi } from 'vitest';
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { VersionHistoryPanel, DecisionVersion } from './VersionHistoryPanel';

// Mock Ant Design Modal
vi.mock('antd', async () => {
  const actual = await vi.importActual('antd');
  return {
    ...actual,
    Modal: {
      ...((actual as Record<string, unknown>).Modal as Record<string, unknown>),
      confirm: vi.fn(({ onOk }) => {
        // Simulate immediate confirmation
        onOk?.();
      }),
      error: vi.fn(),
    },
  };
});

const mockVersions: DecisionVersion[] = [
  {
    VersionNumber: 3,
    ModifiedAt: '2026-01-26T15:00:00Z',
    ModifiedBy: 'Alice',
    ChangeReason: 'Updated decision value to reflect new requirements',
  },
  {
    VersionNumber: 2,
    ModifiedAt: '2026-01-25T10:00:00Z',
    ModifiedBy: 'Bob',
    ChangeReason: 'Fixed typo in decision text',
  },
  {
    VersionNumber: 1,
    ModifiedAt: '2026-01-24T09:00:00Z',
    ModifiedBy: 'Charlie',
    ChangeReason: 'Initial decision',
  },
];

describe('VersionHistoryPanel', () => {
  const defaultProps = {
    decisionId: 'decision-123',
    open: true,
    onClose: vi.fn(),
  };

  describe('loading state', () => {
    it('shows skeleton loading when loading prop is true', () => {
      render(
        <VersionHistoryPanel
          {...defaultProps}
          loading={true}
          versions={[]}
        />
      );

      expect(screen.getByTestId('version-history-loading')).toBeInTheDocument();
    });
  });

  describe('empty state', () => {
    it('shows empty message when no versions exist', () => {
      render(
        <VersionHistoryPanel
          {...defaultProps}
          loading={false}
          versions={[]}
        />
      );

      expect(screen.getByTestId('version-history-empty')).toBeInTheDocument();
      expect(screen.getByText('No version history available')).toBeInTheDocument();
    });
  });

  describe('error state', () => {
    it('shows error message when error prop is set', () => {
      render(
        <VersionHistoryPanel
          {...defaultProps}
          loading={false}
          versions={[]}
          error="Failed to fetch versions"
        />
      );

      expect(screen.getByTestId('version-history-error')).toBeInTheDocument();
      expect(screen.getByText('Failed to fetch versions')).toBeInTheDocument();
    });
  });

  describe('with versions', () => {
    it('renders version timeline with all versions', () => {
      render(
        <VersionHistoryPanel
          {...defaultProps}
          loading={false}
          versions={mockVersions}
        />
      );

      expect(screen.getByTestId('version-item-3')).toBeInTheDocument();
      expect(screen.getByTestId('version-item-2')).toBeInTheDocument();
      expect(screen.getByTestId('version-item-1')).toBeInTheDocument();
    });

    it('shows version number and modified by info', () => {
      render(
        <VersionHistoryPanel
          {...defaultProps}
          loading={false}
          versions={mockVersions}
        />
      );

      expect(screen.getByText('v3')).toBeInTheDocument();
      expect(screen.getByText(/Alice/)).toBeInTheDocument();
    });

    it('shows change reason for each version', () => {
      render(
        <VersionHistoryPanel
          {...defaultProps}
          loading={false}
          versions={mockVersions}
        />
      );

      expect(screen.getByText('Updated decision value to reflect new requirements')).toBeInTheDocument();
      expect(screen.getByText('Fixed typo in decision text')).toBeInTheDocument();
    });

    it('marks current version with tag', () => {
      render(
        <VersionHistoryPanel
          {...defaultProps}
          loading={false}
          versions={mockVersions}
          currentVersion={3}
        />
      );

      expect(screen.getByText('Current')).toBeInTheDocument();
    });

    it('shows version count in header', () => {
      render(
        <VersionHistoryPanel
          {...defaultProps}
          loading={false}
          versions={mockVersions}
        />
      );

      expect(screen.getByText('3 versions')).toBeInTheDocument();
    });
  });

  describe('view diff', () => {
    it('shows view diff button for non-current versions', () => {
      const onViewDiff = vi.fn();
      render(
        <VersionHistoryPanel
          {...defaultProps}
          loading={false}
          versions={mockVersions}
          currentVersion={3}
          onViewDiff={onViewDiff}
        />
      );

      // Versions 1 and 2 should have diff buttons
      expect(screen.getByTestId('view-diff-2')).toBeInTheDocument();
      expect(screen.getByTestId('view-diff-1')).toBeInTheDocument();
    });

    it('calls onViewDiff when button is clicked', () => {
      const onViewDiff = vi.fn();
      render(
        <VersionHistoryPanel
          {...defaultProps}
          loading={false}
          versions={mockVersions}
          currentVersion={3}
          onViewDiff={onViewDiff}
        />
      );

      fireEvent.click(screen.getByTestId('view-diff-2'));

      expect(onViewDiff).toHaveBeenCalledWith(2, 3);
    });
  });

  describe('revert', () => {
    it('shows revert button for non-current versions', () => {
      const onRevert = vi.fn();
      render(
        <VersionHistoryPanel
          {...defaultProps}
          loading={false}
          versions={mockVersions}
          currentVersion={3}
          onRevert={onRevert}
        />
      );

      expect(screen.getByTestId('revert-2')).toBeInTheDocument();
      expect(screen.getByTestId('revert-1')).toBeInTheDocument();
    });
  });

  describe('pagination', () => {
    it('shows load more button when more versions exist', () => {
      const manyVersions = Array.from({ length: 60 }, (_, i) => ({
        VersionNumber: 60 - i,
        ModifiedAt: new Date().toISOString(),
        ModifiedBy: 'User',
        ChangeReason: `Change ${60 - i}`,
      }));

      render(
        <VersionHistoryPanel
          {...defaultProps}
          loading={false}
          versions={manyVersions}
          maxVersionsToShow={50}
        />
      );

      expect(screen.getByTestId('load-more-button')).toBeInTheDocument();
      expect(screen.getByText(/10 remaining/)).toBeInTheDocument();
    });

    it('loads more versions when button is clicked', async () => {
      const manyVersions = Array.from({ length: 60 }, (_, i) => ({
        VersionNumber: 60 - i,
        ModifiedAt: new Date().toISOString(),
        ModifiedBy: 'User',
        ChangeReason: `Change ${60 - i}`,
      }));

      render(
        <VersionHistoryPanel
          {...defaultProps}
          loading={false}
          versions={manyVersions}
          maxVersionsToShow={50}
        />
      );

      // Initially only first 50 are visible
      expect(screen.queryByTestId('version-item-5')).not.toBeInTheDocument();

      fireEvent.click(screen.getByTestId('load-more-button'));

      // After loading more, all should be visible
      await waitFor(() => {
        expect(screen.queryByTestId('load-more-button')).not.toBeInTheDocument();
      });
    });
  });

  describe('drawer behavior', () => {
    it('calls onClose when drawer is closed', () => {
      const onClose = vi.fn();
      render(
        <VersionHistoryPanel
          {...defaultProps}
          onClose={onClose}
          loading={false}
          versions={mockVersions}
        />
      );

      // Find and click the close button
      const closeButton = screen.getByRole('button', { name: /close/i });
      if (closeButton) {
        fireEvent.click(closeButton);
        expect(onClose).toHaveBeenCalled();
      }
    });
  });
});
