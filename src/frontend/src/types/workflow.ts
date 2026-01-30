export interface WorkflowDefinition {
  id: string;
  workflowId: string;
  name: string;
  description: string;
  parameters?: Record<string, string>;
}

export interface WorkflowState {
  id: string;
  definitionId: string;
  status: 'running' | 'paused' | 'completed' | 'failed' | 'cancelled';
  currentStep?: string;
  startedAt: string;
  lastUpdatedAt: string;
}

export interface ConversationAction {
  label: string;
  action: string; // The message to send
  description?: string;
  icon?: string;
}
