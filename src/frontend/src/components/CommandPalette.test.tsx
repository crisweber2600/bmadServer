import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { CommandPalette } from './CommandPalette';

describe('CommandPalette', () => {
  let mockOnSelect: ReturnType<typeof vi.fn>;
  let mockOnClose: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    mockOnSelect = vi.fn();
    mockOnClose = vi.fn();
  });

  describe('Basic Rendering', () => {
    it('renders all available commands', () => {
      render(<CommandPalette onSelect={mockOnSelect} onClose={mockOnClose} />);

      expect(screen.getByText('/help')).toBeInTheDocument();
      expect(screen.getByText('/status')).toBeInTheDocument();
      expect(screen.getByText('/pause')).toBeInTheDocument();
      expect(screen.getByText('/resume')).toBeInTheDocument();
    });

    it('displays command descriptions', () => {
      render(<CommandPalette onSelect={mockOnSelect} onClose={mockOnClose} />);

      expect(screen.getByText('Show available commands')).toBeInTheDocument();
      expect(screen.getByText('Check agent status')).toBeInTheDocument();
      expect(screen.getByText('Pause agent execution')).toBeInTheDocument();
      expect(screen.getByText('Resume agent execution')).toBeInTheDocument();
    });

    it('has proper ARIA attributes', () => {
      render(<CommandPalette onSelect={mockOnSelect} onClose={mockOnClose} />);

      const listbox = screen.getByRole('listbox', { name: /command palette/i });
      expect(listbox).toBeInTheDocument();

      const options = screen.getAllByRole('option');
      expect(options).toHaveLength(4);
    });

    it('applies custom position', () => {
      const { container } = render(
        <CommandPalette
          onSelect={mockOnSelect}
          onClose={mockOnClose}
          position={{ top: 100, left: 200 }}
        />
      );

      const palette = container.querySelector('.command-palette');
      expect(palette).toHaveStyle({ top: '100px', left: '200px' });
    });
  });

  describe('Command Selection', () => {
    it('calls onSelect when command is clicked', async () => {
      const user = userEvent.setup();
      render(<CommandPalette onSelect={mockOnSelect} onClose={mockOnClose} />);

      const helpCommand = screen.getByText('/help');
      await user.click(helpCommand);

      expect(mockOnSelect).toHaveBeenCalledWith('/help');
    });

    it('first command is selected by default', () => {
      render(<CommandPalette onSelect={mockOnSelect} onClose={mockOnClose} />);

      const options = screen.getAllByRole('option');
      expect(options[0]).toHaveAttribute('aria-selected', 'true');
    });

    it('calls onSelect on Enter key', () => {
      render(<CommandPalette onSelect={mockOnSelect} onClose={mockOnClose} />);

      fireEvent.keyDown(document, { key: 'Enter' });

      expect(mockOnSelect).toHaveBeenCalledWith('/help');
    });
  });

  describe('Keyboard Navigation', () => {
    it('navigates down with ArrowDown key', () => {
      render(<CommandPalette onSelect={mockOnSelect} onClose={mockOnClose} />);

      let options = screen.getAllByRole('option');
      expect(options[0]).toHaveAttribute('aria-selected', 'true');

      fireEvent.keyDown(document, { key: 'ArrowDown' });

      options = screen.getAllByRole('option');
      expect(options[1]).toHaveAttribute('aria-selected', 'true');
    });

    it('navigates up with ArrowUp key', () => {
      render(<CommandPalette onSelect={mockOnSelect} onClose={mockOnClose} />);

      fireEvent.keyDown(document, { key: 'ArrowDown' });
      fireEvent.keyDown(document, { key: 'ArrowDown' });

      let options = screen.getAllByRole('option');
      expect(options[2]).toHaveAttribute('aria-selected', 'true');

      fireEvent.keyDown(document, { key: 'ArrowUp' });

      options = screen.getAllByRole('option');
      expect(options[1]).toHaveAttribute('aria-selected', 'true');
    });

    it('wraps around to first item when navigating down from last item', () => {
      render(<CommandPalette onSelect={mockOnSelect} onClose={mockOnClose} />);

      // Navigate to last item
      fireEvent.keyDown(document, { key: 'ArrowDown' });
      fireEvent.keyDown(document, { key: 'ArrowDown' });
      fireEvent.keyDown(document, { key: 'ArrowDown' });

      let options = screen.getAllByRole('option');
      expect(options[3]).toHaveAttribute('aria-selected', 'true');

      // Wrap around to first
      fireEvent.keyDown(document, { key: 'ArrowDown' });

      options = screen.getAllByRole('option');
      expect(options[0]).toHaveAttribute('aria-selected', 'true');
    });

    it('wraps around to last item when navigating up from first item', () => {
      render(<CommandPalette onSelect={mockOnSelect} onClose={mockOnClose} />);

      const options = screen.getAllByRole('option');
      expect(options[0]).toHaveAttribute('aria-selected', 'true');

      fireEvent.keyDown(document, { key: 'ArrowUp' });

      const updatedOptions = screen.getAllByRole('option');
      expect(updatedOptions[3]).toHaveAttribute('aria-selected', 'true');
    });

    it('closes on Escape key', () => {
      render(<CommandPalette onSelect={mockOnSelect} onClose={mockOnClose} />);

      fireEvent.keyDown(document, { key: 'Escape' });

      expect(mockOnClose).toHaveBeenCalled();
    });
  });

  describe('Command Filtering', () => {
    it('filters commands based on filter prop', () => {
      render(
        <CommandPalette
          onSelect={mockOnSelect}
          onClose={mockOnClose}
          filter="help"
        />
      );

      expect(screen.getByText('/help')).toBeInTheDocument();
      expect(screen.queryByText('/status')).not.toBeInTheDocument();
      expect(screen.queryByText('/pause')).not.toBeInTheDocument();
      expect(screen.queryByText('/resume')).not.toBeInTheDocument();
    });

    it('shows all commands when filter is empty', () => {
      render(
        <CommandPalette
          onSelect={mockOnSelect}
          onClose={mockOnClose}
          filter=""
        />
      );

      expect(screen.getByText('/help')).toBeInTheDocument();
      expect(screen.getByText('/status')).toBeInTheDocument();
      expect(screen.getByText('/pause')).toBeInTheDocument();
      expect(screen.getByText('/resume')).toBeInTheDocument();
    });

    it('returns null when no commands match filter', () => {
      const { container } = render(
        <CommandPalette
          onSelect={mockOnSelect}
          onClose={mockOnClose}
          filter="nonexistent"
        />
      );

      expect(container.firstChild).toBeNull();
    });

    it('resets selected index when filter changes', () => {
      const { rerender } = render(
        <CommandPalette
          onSelect={mockOnSelect}
          onClose={mockOnClose}
          filter=""
        />
      );

      // Navigate to second item
      fireEvent.keyDown(document, { key: 'ArrowDown' });

      let options = screen.getAllByRole('option');
      expect(options[1]).toHaveAttribute('aria-selected', 'true');

      // Change filter
      rerender(
        <CommandPalette
          onSelect={mockOnSelect}
          onClose={mockOnClose}
          filter="status"
        />
      );

      // Should reset to first item
      options = screen.getAllByRole('option');
      expect(options[0]).toHaveAttribute('aria-selected', 'true');
    });
  });

  describe('Click Outside', () => {
    it('calls onClose when clicking outside', () => {
      render(
        <div>
          <div data-testid="outside">Outside</div>
          <CommandPalette onSelect={mockOnSelect} onClose={mockOnClose} />
        </div>
      );

      const outside = screen.getByTestId('outside');
      fireEvent.mouseDown(outside);

      expect(mockOnClose).toHaveBeenCalled();
    });

    it('does not call onClose when clicking inside', () => {
      render(<CommandPalette onSelect={mockOnSelect} onClose={mockOnClose} />);

      const palette = screen.getByRole('listbox', { name: /command palette/i });
      fireEvent.mouseDown(palette);

      expect(mockOnClose).not.toHaveBeenCalled();
    });
  });

  describe('Visual States', () => {
    it('highlights selected command', () => {
      render(<CommandPalette onSelect={mockOnSelect} onClose={mockOnClose} />);

      const options = screen.getAllByRole('option');
      const firstOption = options[0];

      expect(firstOption).toHaveClass('command-item-selected');
    });

    it('updates highlight when navigating', () => {
      render(<CommandPalette onSelect={mockOnSelect} onClose={mockOnClose} />);

      fireEvent.keyDown(document, { key: 'ArrowDown' });

      const options = screen.getAllByRole('option');
      expect(options[0]).not.toHaveClass('command-item-selected');
      expect(options[1]).toHaveClass('command-item-selected');
    });
  });
});
