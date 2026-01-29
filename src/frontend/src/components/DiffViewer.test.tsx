import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { DiffViewer } from './DiffViewer';
import type { FieldChange } from './DiffViewer';

const mockChanges: FieldChange[] = [
  { Field: 'Value', OldValue: 'A', NewValue: 'B' },
  { Field: 'Description', OldValue: 'Old description', NewValue: 'New description' },
];

describe('DiffViewer', () => {
  describe('with changes', () => {
    it('renders diff viewer container', () => {
      render(<DiffViewer changes={mockChanges} fromVersion={1} toVersion={2} />);

      expect(screen.getByTestId('diff-viewer')).toBeInTheDocument();
    });

    it('shows version headers', () => {
      render(<DiffViewer changes={mockChanges} fromVersion={1} toVersion={2} />);

      expect(screen.getByText('Version 1')).toBeInTheDocument();
      expect(screen.getByText('Version 2')).toBeInTheDocument();
    });

    it('shows Previous and Current tags', () => {
      render(<DiffViewer changes={mockChanges} fromVersion={1} toVersion={2} />);

      expect(screen.getByText('Previous')).toBeInTheDocument();
      expect(screen.getByText('Current')).toBeInTheDocument();
    });

    it('renders all changed fields', () => {
      render(<DiffViewer changes={mockChanges} fromVersion={1} toVersion={2} />);

      expect(screen.getByText('Value')).toBeInTheDocument();
      expect(screen.getByText('Description')).toBeInTheDocument();
    });

    it('shows old and new values for each field', () => {
      render(<DiffViewer changes={mockChanges} fromVersion={1} toVersion={2} />);

      expect(screen.getByText('A')).toBeInTheDocument();
      expect(screen.getByText('B')).toBeInTheDocument();
      expect(screen.getByText('Old description')).toBeInTheDocument();
      expect(screen.getByText('New description')).toBeInTheDocument();
    });

    it('shows diff rows with correct test ids', () => {
      render(<DiffViewer changes={mockChanges} fromVersion={1} toVersion={2} />);

      expect(screen.getByTestId('diff-row-0')).toBeInTheDocument();
      expect(screen.getByTestId('diff-row-1')).toBeInTheDocument();
    });

    it('shows change summary', () => {
      render(<DiffViewer changes={mockChanges} fromVersion={1} toVersion={2} />);

      expect(screen.getByText('2 fields changed')).toBeInTheDocument();
    });

    it('shows singular form for single change', () => {
      render(
        <DiffViewer 
          changes={[{ Field: 'Status', OldValue: 'Draft', NewValue: 'Final' }]} 
          fromVersion={1} 
          toVersion={2} 
        />
      );

      expect(screen.getByText('1 field changed')).toBeInTheDocument();
    });
  });

  describe('with title', () => {
    it('renders title when provided', () => {
      render(
        <DiffViewer 
          changes={mockChanges} 
          fromVersion={1} 
          toVersion={2} 
          title="Changes between versions" 
        />
      );

      expect(screen.getByText('Changes between versions')).toBeInTheDocument();
    });
  });

  describe('empty state', () => {
    it('shows empty message when no changes', () => {
      render(<DiffViewer changes={[]} fromVersion={1} toVersion={2} />);

      expect(screen.getByTestId('diff-viewer-empty')).toBeInTheDocument();
      expect(screen.getByText('No differences found')).toBeInTheDocument();
    });
  });

  describe('empty values', () => {
    it('shows (empty) for null/empty old values', () => {
      render(
        <DiffViewer 
          changes={[{ Field: 'NewField', OldValue: '', NewValue: 'Added' }]} 
          fromVersion={1} 
          toVersion={2} 
        />
      );

      expect(screen.getByText('(empty)')).toBeInTheDocument();
      expect(screen.getByText('Added')).toBeInTheDocument();
    });

    it('shows (empty) for null/empty new values', () => {
      render(
        <DiffViewer 
          changes={[{ Field: 'RemovedField', OldValue: 'Removed', NewValue: '' }]} 
          fromVersion={1} 
          toVersion={2} 
        />
      );

      expect(screen.getByText('(empty)')).toBeInTheDocument();
      expect(screen.getByText('Removed')).toBeInTheDocument();
    });
  });

  describe('accessibility', () => {
    it('has proper test ids for old and new values', () => {
      render(<DiffViewer changes={mockChanges} fromVersion={1} toVersion={2} />);

      expect(screen.getByTestId('old-value-0')).toBeInTheDocument();
      expect(screen.getByTestId('new-value-0')).toBeInTheDocument();
      expect(screen.getByTestId('old-value-1')).toBeInTheDocument();
      expect(screen.getByTestId('new-value-1')).toBeInTheDocument();
    });
  });

  describe('content comparison mode', () => {
    it('renders diff viewer in content mode', () => {
      render(
        <DiffViewer 
          originalContent="Original text content"
          modifiedContent="Modified text content"
        />
      );

      expect(screen.getByTestId('diff-viewer')).toBeInTheDocument();
    });

    it('shows original and modified content', () => {
      render(
        <DiffViewer 
          originalContent="Original text content"
          modifiedContent="Modified text content"
        />
      );

      expect(screen.getByText('Original text content')).toBeInTheDocument();
      expect(screen.getByText('Modified text content')).toBeInTheDocument();
    });

    it('shows custom labels when provided', () => {
      render(
        <DiffViewer 
          originalContent="First content"
          modifiedContent="Second content"
          originalLabel="Decision 1: Use Microservices"
          modifiedLabel="Decision 2: Use Monolith"
        />
      );

      expect(screen.getByText('Decision 1: Use Microservices')).toBeInTheDocument();
      expect(screen.getByText('Decision 2: Use Monolith')).toBeInTheDocument();
    });

    it('shows default labels when not provided', () => {
      render(
        <DiffViewer 
          originalContent="Content A"
          modifiedContent="Content B"
        />
      );

      // Check that Original and Modified card headers exist
      expect(screen.getByTestId('original-content')).toBeInTheDocument();
      expect(screen.getByTestId('modified-content')).toBeInTheDocument();
    });

    it('shows (empty) for empty original content', () => {
      render(
        <DiffViewer 
          originalContent=""
          modifiedContent="New content"
        />
      );

      expect(screen.getByText('(empty)')).toBeInTheDocument();
      expect(screen.getByText('New content')).toBeInTheDocument();
    });

    it('shows (empty) for empty modified content', () => {
      render(
        <DiffViewer 
          originalContent="Old content"
          modifiedContent=""
        />
      );

      expect(screen.getByText('Old content')).toBeInTheDocument();
      expect(screen.getByText('(empty)')).toBeInTheDocument();
    });

    it('has content-mode class', () => {
      const { container } = render(
        <DiffViewer 
          originalContent="Content A"
          modifiedContent="Content B"
        />
      );

      expect(container.querySelector('.content-mode')).toBeInTheDocument();
    });

    it('has correct test ids for content areas', () => {
      render(
        <DiffViewer 
          originalContent="Content A"
          modifiedContent="Content B"
        />
      );

      expect(screen.getByTestId('original-content')).toBeInTheDocument();
      expect(screen.getByTestId('modified-content')).toBeInTheDocument();
    });
  });
});
