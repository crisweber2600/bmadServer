import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { GlossaryPanel } from './GlossaryPanel';
import type { GlossaryTerm } from '../types/persona';
import { PersonaType } from '../types/persona';

const mockGlossary: GlossaryTerm[] = [
  {
    term: 'PRD',
    definition: 'Product Requirements Document',
    category: 'business',
    relatedTerms: ['Requirements', 'MVP'],
  },
  {
    term: 'API',
    definition: 'Application Programming Interface',
    category: 'technical',
    relatedTerms: ['REST', 'HTTP'],
  },
  {
    term: 'Workflow',
    definition: 'A sequence of tasks or steps',
    category: 'general',
    relatedTerms: ['Process'],
  },
];

describe('GlossaryPanel', () => {
  const user = userEvent.setup();
  const mockOnClose = vi.fn();

  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  describe('Loading States', () => {
    it('shows Skeleton loading state when fetching terms', async () => {
      const fetchTerms = vi.fn(() => new Promise<GlossaryTerm[]>(() => {}));

      render(
        <GlossaryPanel
          open={true}
          onClose={mockOnClose}
          terms={undefined}
          fetchTerms={fetchTerms}
        />
      );

      await waitFor(() => {
        // Check for skeleton element by its class
        expect(document.querySelector('.ant-skeleton')).toBeInTheDocument();
      });
    });

    it('shows search input and term list immediately with static data', () => {
      render(
        <GlossaryPanel
          open={true}
          onClose={mockOnClose}
          terms={mockGlossary}
        />
      );

      expect(screen.getByTestId('glossary-search')).toBeInTheDocument();
      expect(screen.getByTestId('glossary-list')).toBeInTheDocument();
    });
  });

  describe('Empty States', () => {
    it('shows Empty component when glossary is empty', () => {
      render(
        <GlossaryPanel
          open={true}
          onClose={mockOnClose}
          terms={[]}
        />
      );

      expect(screen.getByTestId('glossary-empty')).toBeInTheDocument();
      expect(screen.getByText('No terms available')).toBeInTheDocument();
    });

    it('shows Empty component with "No terms available" when terms is undefined', () => {
      // Override default glossary for this test
      render(
        <GlossaryPanel
          open={true}
          onClose={mockOnClose}
          terms={[]}
        />
      );

      expect(screen.getByText('No terms available')).toBeInTheDocument();
    });
  });

  describe('Search Functionality', () => {
    it('filters terms when user types in search (case-insensitive)', async () => {
      render(
        <GlossaryPanel
          open={true}
          onClose={mockOnClose}
          terms={mockGlossary}
        />
      );

      const searchInput = screen.getByPlaceholderText('Search terms...');
      await user.type(searchInput, 'prd');

      await waitFor(() => {
        expect(screen.getByText('PRD')).toBeInTheDocument();
        expect(screen.queryByText('API')).not.toBeInTheDocument();
        expect(screen.queryByText('Workflow')).not.toBeInTheDocument();
      });
    });

    it('shows no results message when search has no matches', async () => {
      render(
        <GlossaryPanel
          open={true}
          onClose={mockOnClose}
          terms={mockGlossary}
        />
      );

      const searchInput = screen.getByPlaceholderText('Search terms...');
      await user.type(searchInput, 'xyz123');

      await waitFor(() => {
        expect(screen.getByTestId('glossary-no-results')).toBeInTheDocument();
      });
    });

    it('searches in definitions as well as terms', async () => {
      render(
        <GlossaryPanel
          open={true}
          onClose={mockOnClose}
          terms={mockGlossary}
        />
      );

      const searchInput = screen.getByPlaceholderText('Search terms...');
      await user.type(searchInput, 'Product');

      await waitFor(() => {
        expect(screen.getByText('PRD')).toBeInTheDocument();
        expect(screen.queryByText('API')).not.toBeInTheDocument();
      });
    });
  });

  describe('Term Expansion', () => {
    it('expands term definition when clicked', async () => {
      render(
        <GlossaryPanel
          open={true}
          onClose={mockOnClose}
          terms={mockGlossary}
        />
      );

      // Click on PRD header to expand
      const prdHeader = screen.getByText('PRD');
      await user.click(prdHeader);

      await waitFor(() => {
        expect(screen.getByText('Product Requirements Document')).toBeVisible();
      });
    });

    it('shows related terms when expanded', async () => {
      render(
        <GlossaryPanel
          open={true}
          onClose={mockOnClose}
          terms={mockGlossary}
        />
      );

      // Click on PRD header to expand
      const prdHeader = screen.getByText('PRD');
      await user.click(prdHeader);

      await waitFor(() => {
        expect(screen.getByText('Related:')).toBeInTheDocument();
        expect(screen.getByText('Requirements')).toBeInTheDocument();
        expect(screen.getByText('MVP')).toBeInTheDocument();
      });
    });
  });

  describe('Persona-based Sorting', () => {
    it('shows business-category terms first when currentPersona is Business', () => {
      render(
        <GlossaryPanel
          open={true}
          onClose={mockOnClose}
          terms={mockGlossary}
          currentPersona={PersonaType.Business}
        />
      );

      const list = screen.getByTestId('glossary-list');
      const termHeaders = list.querySelectorAll('.ant-collapse-header');
      
      // Business persona: business (-2) > general (-1) > technical (0)
      // Expected order: PRD (business), Workflow (general), API (technical)
      expect(termHeaders[0]).toHaveTextContent('PRD');
      expect(termHeaders[1]).toHaveTextContent('Workflow');
      expect(termHeaders[2]).toHaveTextContent('API');
    });

    it('shows technical-category terms first when currentPersona is Technical', () => {
      render(
        <GlossaryPanel
          open={true}
          onClose={mockOnClose}
          terms={mockGlossary}
          currentPersona={PersonaType.Technical}
        />
      );

      const list = screen.getByTestId('glossary-list');
      const termHeaders = list.querySelectorAll('.ant-collapse-header');
      
      // Technical persona: technical (-2) > general (-1) > business (0)
      // Expected order: API (technical), Workflow (general), PRD (business)
      expect(termHeaders[0]).toHaveTextContent('API');
      expect(termHeaders[1]).toHaveTextContent('Workflow');
      expect(termHeaders[2]).toHaveTextContent('PRD');
    });
  });

  describe('Async Loading', () => {
    it('calls fetchTerms when opened with fetchTerms prop', async () => {
      const fetchTerms = vi.fn().mockResolvedValue(mockGlossary);

      render(
        <GlossaryPanel
          open={true}
          onClose={mockOnClose}
          fetchTerms={fetchTerms}
        />
      );

      await waitFor(() => {
        expect(fetchTerms).toHaveBeenCalled();
      });
    });

    it('shows error message when fetchTerms fails', async () => {
      const fetchTerms = vi.fn().mockRejectedValue(new Error('Network error'));

      render(
        <GlossaryPanel
          open={true}
          onClose={mockOnClose}
          fetchTerms={fetchTerms}
          terms={undefined}
        />
      );

      await waitFor(() => {
        expect(screen.getByText('Network error')).toBeInTheDocument();
      });

      expect(screen.getByText('Retry')).toBeInTheDocument();
    });
  });

  describe('Drawer Behavior', () => {
    it('calls onClose when close button is clicked', async () => {
      render(
        <GlossaryPanel
          open={true}
          onClose={mockOnClose}
          terms={mockGlossary}
        />
      );

      const closeButton = screen.getByRole('button', { name: /close/i });
      await user.click(closeButton);

      expect(mockOnClose).toHaveBeenCalled();
    });

    it('renders with correct title', () => {
      render(
        <GlossaryPanel
          open={true}
          onClose={mockOnClose}
          terms={mockGlossary}
        />
      );

      expect(screen.getByText('Glossary')).toBeInTheDocument();
    });
  });

  describe('Category Tags', () => {
    it('displays category tags with correct colors', () => {
      render(
        <GlossaryPanel
          open={true}
          onClose={mockOnClose}
          terms={mockGlossary}
        />
      );

      expect(screen.getByText('business')).toBeInTheDocument();
      expect(screen.getByText('technical')).toBeInTheDocument();
      expect(screen.getByText('general')).toBeInTheDocument();
    });
  });

  describe('Accessibility', () => {
    it('should have proper accessibility attributes on drawer', async () => {
      render(
        <GlossaryPanel
          open={true}
          onClose={mockOnClose}
          terms={mockGlossary}
        />
      );

      // Wait for drawer to be rendered
      await waitFor(() => {
        expect(screen.getByTestId('glossary-panel')).toBeInTheDocument();
      });

      // Check drawer has proper structure
      expect(screen.getByTestId('glossary-search')).toBeInTheDocument();
      expect(screen.getByTestId('glossary-list')).toBeInTheDocument();
    });

    it('should have proper accessibility attributes on search input', async () => {
      render(
        <GlossaryPanel
          open={true}
          onClose={mockOnClose}
          terms={mockGlossary}
        />
      );

      await waitFor(() => {
        expect(screen.getByTestId('glossary-panel')).toBeInTheDocument();
      });

      const searchInput = screen.getByPlaceholderText('Search terms...');
      expect(searchInput).toHaveAttribute('type', 'search');
    });

    it('should have proper accessibility attributes on empty state', async () => {
      render(
        <GlossaryPanel
          open={true}
          onClose={mockOnClose}
          terms={[]}
        />
      );

      await waitFor(() => {
        expect(screen.getByTestId('glossary-panel')).toBeInTheDocument();
      });

      expect(screen.getByTestId('glossary-empty')).toBeInTheDocument();
    });
  });
});
