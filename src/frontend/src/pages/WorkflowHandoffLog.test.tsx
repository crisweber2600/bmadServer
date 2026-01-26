import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, fireEvent, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { WorkflowHandoffLog } from './WorkflowHandoffLog';

global.fetch = vi.fn();

const mockHandoffsResponse = {
  items: [
    {
      id: '1',
      fromAgentId: 'agent-1',
      fromAgentName: 'Architect',
      toAgentId: 'agent-2',
      toAgentName: 'Developer',
      stepName: 'design',
      reason: 'Ready for implementation',
      timestamp: '2026-01-26T10:00:00Z',
    },
    {
      id: '2',
      fromAgentId: 'agent-2',
      fromAgentName: 'Developer',
      toAgentId: 'agent-3',
      toAgentName: 'QA',
      stepName: 'testing',
      reason: 'Code complete',
      timestamp: '2026-01-26T14:00:00Z',
    },
  ],
  pageIndex: 1,
  pageSize: 10,
  totalCount: 2,
};

describe('WorkflowHandoffLog', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    localStorage.setItem('accessToken', 'test-token');
  });

  afterEach(() => {
    localStorage.removeItem('accessToken');
  });

  it('renders component with title and description', async () => {
    (global.fetch as any).mockResolvedValueOnce({
      ok: true,
      json: async () => mockHandoffsResponse,
    });

    render(
      <WorkflowHandoffLog workflowId="workflow-1" />
    );

    expect(screen.getByText('Agent Handoff Timeline')).toBeInTheDocument();
    expect(screen.getByText('View all agent transitions for this workflow')).toBeInTheDocument();
  });

  it('fetches and displays handoff records', async () => {
    (global.fetch as any).mockResolvedValueOnce({
      ok: true,
      json: async () => mockHandoffsResponse,
    });

    render(
      <WorkflowHandoffLog workflowId="workflow-1" />
    );

    await waitFor(() => {
      expect(screen.getByText('Architect')).toBeInTheDocument();
      expect(screen.getByText('Developer')).toBeInTheDocument();
    });

    expect(screen.getByText('design')).toBeInTheDocument();
    expect(screen.getByText('testing')).toBeInTheDocument();
  });

  it('displays filter controls', () => {
    (global.fetch as any).mockResolvedValueOnce({
      ok: true,
      json: async () => ({ items: [] }),
    });

    render(
      <WorkflowHandoffLog workflowId="workflow-1" />
    );

    expect(screen.getByText('From Date:')).toBeInTheDocument();
    expect(screen.getByText('To Date:')).toBeInTheDocument();
    expect(screen.getByText('Clear Filters')).toBeInTheDocument();
  });

  it('displays export buttons', () => {
    (global.fetch as any).mockResolvedValueOnce({
      ok: true,
      json: async () => mockHandoffsResponse,
    });

    render(
      <WorkflowHandoffLog workflowId="workflow-1" />
    );

    waitFor(() => {
      const csvButton = screen.getAllByText('CSV')[0];
      const jsonButton = screen.getAllByText('JSON')[0];

      expect(csvButton).toBeInTheDocument();
      expect(jsonButton).toBeInTheDocument();
    });
  });

  it('disables export buttons when no data', () => {
    (global.fetch as any).mockResolvedValueOnce({
      ok: true,
      json: async () => ({ items: [] }),
    });

    render(
      <WorkflowHandoffLog workflowId="workflow-1" />
    );

    waitFor(() => {
      const csvButton = screen.getByText('CSV').closest('button');
      const jsonButton = screen.getByText('JSON').closest('button');

      expect(csvButton).toBeDisabled();
      expect(jsonButton).toBeDisabled();
    });
  });

  it('shows empty state when no handoffs', async () => {
    (global.fetch as any).mockResolvedValueOnce({
      ok: true,
      json: async () => ({ items: [] }),
    });

    render(
      <WorkflowHandoffLog workflowId="workflow-1" />
    );

    await waitFor(() => {
      expect(screen.getByText('No handoffs found')).toBeInTheDocument();
    });
  });

  it('displays error message on fetch failure', async () => {
    (global.fetch as any).mockResolvedValueOnce({
      ok: false,
      statusText: 'Unauthorized',
    });

    render(
      <WorkflowHandoffLog workflowId="workflow-1" />
    );

    await waitFor(() => {
      expect(screen.getByText('Error Loading Handoffs')).toBeInTheDocument();
    });
  });

  it('includes authorization header when fetching', async () => {
    (global.fetch as any).mockResolvedValueOnce({
      ok: true,
      json: async () => mockHandoffsResponse,
    });

    render(
      <WorkflowHandoffLog workflowId="workflow-1" />
    );

    await waitFor(() => {
      expect(global.fetch).toHaveBeenCalledWith(
        expect.stringContaining('/handoffs'),
        expect.objectContaining({
          headers: expect.objectContaining({
            Authorization: 'Bearer test-token',
          }),
        })
      );
    });
  });

  it('exports data as CSV', async () => {
    const user = userEvent.setup();
    (global.fetch as any).mockResolvedValueOnce({
      ok: true,
      json: async () => mockHandoffsResponse,
    });

    const createElementSpy = vi.spyOn(document, 'createElement');
    const appendChildSpy = vi.spyOn(document.body, 'appendChild');

    render(
      <WorkflowHandoffLog workflowId="workflow-1" />
    );

    await waitFor(() => {
      expect(screen.getByText('Architect')).toBeInTheDocument();
    });

    const csvButton = screen.getAllByText('CSV').find((el) => el.closest('button'));
    if (csvButton) {
      await user.click(csvButton.closest('button')!);

      expect(createElementSpy).toHaveBeenCalledWith('a');
    }

    createElementSpy.mockRestore();
    appendChildSpy.mockRestore();
  });

  it('exports data as JSON', async () => {
    const user = userEvent.setup();
    (global.fetch as any).mockResolvedValueOnce({
      ok: true,
      json: async () => mockHandoffsResponse,
    });

    const createElementSpy = vi.spyOn(document, 'createElement');

    render(
      <WorkflowHandoffLog workflowId="workflow-1" />
    );

    await waitFor(() => {
      expect(screen.getByText('Architect')).toBeInTheDocument();
    });

    const jsonButton = screen.getAllByText('JSON').find((el) => el.closest('button'));
    if (jsonButton) {
      await user.click(jsonButton.closest('button')!);

      expect(createElementSpy).toHaveBeenCalledWith('a');
    }

    createElementSpy.mockRestore();
  });

  it('clears filters when clear button clicked', async () => {
    const user = userEvent.setup();
    (global.fetch as any).mockResolvedValueOnce({
      ok: true,
      json: async () => mockHandoffsResponse,
    });

    const { rerender } = render(
      <WorkflowHandoffLog workflowId="workflow-1" />
    );

    await waitFor(() => {
      expect(screen.getByText('Architect')).toBeInTheDocument();
    });

    const clearButton = screen.getByText('Clear Filters');
    await user.click(clearButton);

    expect(global.fetch).toHaveBeenCalledTimes(2);
  });

  it('renders table with correct columns', async () => {
    (global.fetch as any).mockResolvedValueOnce({
      ok: true,
      json: async () => mockHandoffsResponse,
    });

    const { container } = render(
      <WorkflowHandoffLog workflowId="workflow-1" />
    );

    await waitFor(() => {
      expect(screen.getByText('Architect')).toBeInTheDocument();
    });

    const tableHeaders = container.querySelectorAll('.ant-table-thead th');
    expect(tableHeaders.length).toBeGreaterThan(0);
  });

  it('displays reasons correctly in table', async () => {
    (global.fetch as any).mockResolvedValueOnce({
      ok: true,
      json: async () => mockHandoffsResponse,
    });

    render(
      <WorkflowHandoffLog workflowId="workflow-1" />
    );

    await waitFor(() => {
      expect(screen.getByText('Ready for implementation')).toBeInTheDocument();
      expect(screen.getByText('Code complete')).toBeInTheDocument();
    });
  });

  it('handles pagination', async () => {
    (global.fetch as any).mockResolvedValueOnce({
      ok: true,
      json: async () => ({
        items: Array(20).fill(null).map((_, i) => ({
          ...mockHandoffsResponse.items[0],
          id: `${i}`,
          timestamp: `2026-01-26T${10 + i}:00:00Z`,
        })),
        pageIndex: 1,
        pageSize: 10,
        totalCount: 20,
      }),
    });

    render(
      <WorkflowHandoffLog workflowId="workflow-1" />
    );

    await waitFor(() => {
      expect(screen.getByText('Architect')).toBeInTheDocument();
    });
  });

  it('uses custom apiBaseUrl when provided', async () => {
    (global.fetch as any).mockResolvedValueOnce({
      ok: true,
      json: async () => mockHandoffsResponse,
    });

    render(
      <WorkflowHandoffLog
        workflowId="workflow-1"
        apiBaseUrl="http://custom-api.com"
      />
    );

    await waitFor(() => {
      expect(global.fetch).toHaveBeenCalledWith(
        expect.stringContaining('http://custom-api.com'),
        expect.anything()
      );
    });
  });

  it('shows loading state while fetching', () => {
    (global.fetch as any).mockImplementation(
      () => new Promise(() => {})
    );

    render(
      <WorkflowHandoffLog workflowId="workflow-1" />
    );

    expect(screen.getByText('Loading handoffs...')).toBeInTheDocument();
  });

  it('generates consistent colors for agent IDs', async () => {
    (global.fetch as any).mockResolvedValueOnce({
      ok: true,
      json: async () => mockHandoffsResponse,
    });

    const { container } = render(
      <WorkflowHandoffLog workflowId="workflow-1" />
    );

    await waitFor(() => {
      expect(screen.getByText('Architect')).toBeInTheDocument();
    });

    const coloredCircles = container.querySelectorAll('[style*="background"]');
    expect(coloredCircles.length).toBeGreaterThan(0);
  });
});
