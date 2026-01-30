import { describe, it, expect, vi, beforeEach, type Mock } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { ApprovalPrompt, type ApprovalRequestDto } from './ApprovalPrompt';

describe('ApprovalPrompt Component', () => {
  const mockApprovalRequest: ApprovalRequestDto = {
    id: 'approval-1',
    workflowInstanceId: 'workflow-1',
    agentId: 'architect',
    stepId: 'design-system',
    proposedResponse: 'This is the proposed response from the agent.',
    confidenceScore: 0.65,
    reasoning: 'Requirements are ambiguous regarding the authentication method.',
    status: 'Pending',
    requestedAt: '2026-01-25T10:30:00Z',
  };

  const mockOnClose = vi.fn();
  const mockOnApproved = vi.fn();

  beforeEach(() => {
    vi.clearAllMocks();
    global.fetch = vi.fn();
  });

  it('renders the approval prompt modal', () => {
    render(
      <ApprovalPrompt
        approvalRequest={mockApprovalRequest}
        onClose={mockOnClose}
        onApproved={mockOnApproved}
      />
    );

    expect(screen.getByText('Agent Approval Required')).toBeInTheDocument();
    expect(screen.getByText(/architect needs your approval/)).toBeInTheDocument();
  });

  it('displays the confidence score with correct visualization', () => {
    render(
      <ApprovalPrompt
        approvalRequest={mockApprovalRequest}
        onClose={mockOnClose}
        onApproved={mockOnApproved}
      />
    );

    expect(screen.getByText('Confidence Score')).toBeInTheDocument();
    expect(screen.getByText('65%')).toBeInTheDocument();
  });

  it('displays the agent reasoning', () => {
    render(
      <ApprovalPrompt
        approvalRequest={mockApprovalRequest}
        onClose={mockOnClose}
        onApproved={mockOnApproved}
      />
    );

    expect(screen.getByText("Agent's Reasoning")).toBeInTheDocument();
    expect(screen.getByText(/Requirements are ambiguous/)).toBeInTheDocument();
  });

  it('displays the proposed response in a text area', () => {
    render(
      <ApprovalPrompt
        approvalRequest={mockApprovalRequest}
        onClose={mockOnClose}
        onApproved={mockOnApproved}
      />
    );

    expect(screen.getByText('Proposed Response')).toBeInTheDocument();
    const textarea = screen.getByDisplayValue('This is the proposed response from the agent.');
    expect(textarea).toBeDisabled();
  });

  it('renders Approve, Modify, and Reject buttons initially', () => {
    render(
      <ApprovalPrompt
        approvalRequest={mockApprovalRequest}
        onClose={mockOnClose}
        onApproved={mockOnApproved}
      />
    );

    expect(screen.getByRole('button', { name: /Approve/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Modify/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Reject/i })).toBeInTheDocument();
  });

  it('allows selecting Modify action and editing the response', async () => {
    const user = userEvent.setup();
    render(
      <ApprovalPrompt
        approvalRequest={mockApprovalRequest}
        onClose={mockOnClose}
        onApproved={mockOnApproved}
      />
    );

    const modifyButton = screen.getByRole('button', { name: /Modify/i });
    await user.click(modifyButton);

    const textareas = screen.getAllByDisplayValue('This is the proposed response from the agent.');
    const editableTextarea = textareas.find((ta) => !ta.hasAttribute('disabled'));

    expect(editableTextarea).not.toBeDisabled();
  });

  it('allows selecting Reject action and entering rejection reason', async () => {
    const user = userEvent.setup();
    render(
      <ApprovalPrompt
        approvalRequest={mockApprovalRequest}
        onClose={mockOnClose}
        onApproved={mockOnApproved}
      />
    );

    const rejectButton = screen.getByRole('button', { name: /Reject/i });
    await user.click(rejectButton);

    expect(screen.getByPlaceholderText(/Why are you rejecting/)).toBeInTheDocument();
  });

  it('handles approve action with API call', async () => {
    const mockFetch = global.fetch as Mock;
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => ({}),
    });

    const user = userEvent.setup();
    render(
      <ApprovalPrompt
        approvalRequest={mockApprovalRequest}
        onClose={mockOnClose}
        onApproved={mockOnApproved}
      />
    );

    const approveButton = screen.getByRole('button', { name: /Approve/i });
    await user.click(approveButton);

    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalledWith(
        `/api/v1/workflows/approvals/${mockApprovalRequest.id}/approve`,
        expect.objectContaining({
          method: 'POST',
        })
      );
    });

    expect(mockOnApproved).toHaveBeenCalled();
    expect(mockOnClose).toHaveBeenCalled();
  });

  it('handles modify action with API call', async () => {
    const mockFetch = global.fetch as Mock;
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => ({}),
    });

    const user = userEvent.setup();
    render(
      <ApprovalPrompt
        approvalRequest={mockApprovalRequest}
        onClose={mockOnClose}
        onApproved={mockOnApproved}
      />
    );

    const modifyButton = screen.getByRole('button', { name: /Modify/i });
    await user.click(modifyButton);

    const textareas = screen.getAllByDisplayValue('This is the proposed response from the agent.');
    const editableTextarea = textareas.find((ta) => !ta.hasAttribute('disabled')) as HTMLTextAreaElement;

    await user.clear(editableTextarea);
    await user.type(editableTextarea, 'Modified response from user');

    const confirmButton = screen.getByRole('button', { name: /Confirm Modification/i });
    await user.click(confirmButton);

    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalledWith(
        `/api/v1/workflows/approvals/${mockApprovalRequest.id}/modify`,
        expect.objectContaining({
          method: 'POST',
          body: JSON.stringify({ modifiedResponse: 'Modified response from user' }),
        })
      );
    });

    expect(mockOnApproved).toHaveBeenCalled();
    expect(mockOnClose).toHaveBeenCalled();
  });

  it('handles reject action with API call', async () => {
    const mockFetch = global.fetch as Mock;
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => ({}),
    });

    const user = userEvent.setup();
    render(
      <ApprovalPrompt
        approvalRequest={mockApprovalRequest}
        onClose={mockOnClose}
        onApproved={mockOnApproved}
      />
    );

    const rejectButton = screen.getByRole('button', { name: /Reject/i });
    await user.click(rejectButton);

    const rejectionInput = screen.getByPlaceholderText(/Why are you rejecting/);
    await user.type(rejectionInput, 'This does not meet requirements');

    const confirmButton = screen.getByRole('button', { name: /Confirm Rejection/i });
    await user.click(confirmButton);

    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalledWith(
        `/api/v1/workflows/approvals/${mockApprovalRequest.id}/reject`,
        expect.objectContaining({
          method: 'POST',
          body: JSON.stringify({ rejectionReason: 'This does not meet requirements' }),
        })
      );
    });

    expect(mockOnApproved).toHaveBeenCalled();
    expect(mockOnClose).toHaveBeenCalled();
  });

  it('displays error message when API call fails', async () => {
    const mockFetch = global.fetch as Mock;
    mockFetch.mockResolvedValueOnce({
      ok: false,
      json: async () => ({ detail: 'Server error' }),
    });

    const user = userEvent.setup();
    render(
      <ApprovalPrompt
        approvalRequest={mockApprovalRequest}
        onClose={mockOnClose}
        onApproved={mockOnApproved}
      />
    );

    const approveButton = screen.getByRole('button', { name: /Approve/i });
    await user.click(approveButton);

    await waitFor(() => {
      expect(screen.getByText('Error')).toBeInTheDocument();
      expect(screen.getByText('Server error')).toBeInTheDocument();
    });

    expect(mockOnApproved).not.toHaveBeenCalled();
    expect(mockOnClose).not.toHaveBeenCalled();
  });

  it('shows warning for low confidence score', () => {
    const lowConfidenceRequest: ApprovalRequestDto = {
      ...mockApprovalRequest,
      confidenceScore: 0.45,
    };

    render(
      <ApprovalPrompt
        approvalRequest={lowConfidenceRequest}
        onClose={mockOnClose}
        onApproved={mockOnApproved}
      />
    );

    expect(screen.getByText(/45%/)).toBeInTheDocument();
  });
});
