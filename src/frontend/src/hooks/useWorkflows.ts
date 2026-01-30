import { useState, useCallback } from 'react';
import { notification } from 'antd';
import type { WorkflowDefinition, WorkflowState } from '../types/workflow';

interface UseWorkflowsOptions {
  token: string | null;
  apiUrl: string;
}

export const useWorkflows = ({ token, apiUrl }: UseWorkflowsOptions) => {
  const [definitions, setDefinitions] = useState<WorkflowDefinition[]>([]);
  const [currentWorkflow, setCurrentWorkflow] = useState<WorkflowState | null>(null);
  const [isLoading, setIsLoading] = useState(false);

  const fetchDefinitions = useCallback(async () => {
    if (!token) return;
    setIsLoading(true);
    try {
      const response = await fetch(`${apiUrl}/api/v1/workflows/definitions`, {
        headers: { Authorization: `Bearer ${token}` }
      });
      if (response.ok) {
        const data = await response.json();
        setDefinitions(data);
      }
    } catch (error) {
      console.error('Failed to fetch definitions', error);
    } finally {
      setIsLoading(false);
    }
  }, [apiUrl, token]);

  const createWorkflow = useCallback(async (params: { workflowId: string }) => {
    if (!token) return null;
    setIsLoading(true);
    try {
      const response = await fetch(`${apiUrl}/api/v1/workflows`, {
        method: 'POST',
        headers: { 
          'Content-Type': 'application/json',
          Authorization: `Bearer ${token}` 
        },
        body: JSON.stringify({ workflowId: params.workflowId })
      });
      
      if (!response.ok) throw new Error('Failed to create workflow');
      
      const instance = await response.json();
      // Normalize backend response to match frontend expectations
      const normalizedInstance = {
          ...instance,
          status: instance.status ? instance.status.toLowerCase() : 'created',
          currentStep: instance.currentStep !== undefined ? instance.currentStep.toString() : undefined
      };
      
      setCurrentWorkflow(normalizedInstance);
      return normalizedInstance as WorkflowState;
    } catch (error) {
      notification.error({ message: 'Failed to create workflow' });
      return null;
    } finally {
      setIsLoading(false);
    }
  }, [apiUrl, token]);

  const startWorkflow = useCallback(async (id: string) => {
    if (!token) return false;
    setIsLoading(true);
    try {
      const response = await fetch(`${apiUrl}/api/v1/workflows/${id}/start`, {
        method: 'POST',
        headers: { Authorization: `Bearer ${token}` }
      });
      
      if (!response.ok) throw new Error('Failed to start workflow');
      
      setCurrentWorkflow(prev => prev ? { ...prev, status: 'running' } : null);
      return true;
    } catch (error) {
      notification.error({ message: 'Failed to start workflow' });
      return false;
    } finally {
      setIsLoading(false);
    }
  }, [apiUrl, token]);

  const pauseWorkflow = useCallback(async (id: string) => {
    if (!token) return;
    try {
      await fetch(`${apiUrl}/api/v1/workflows/${id}/pause`, {
        method: 'POST',
        headers: { Authorization: `Bearer ${token}` }
      });
      setCurrentWorkflow(prev => prev ? { ...prev, status: 'paused' } : null);
    } catch (error) {
      notification.error({ message: 'Failed to pause workflow' });
    }
  }, [apiUrl, token]);

  const resumeWorkflow = useCallback(async (id: string) => {
    if (!token) return;
    try {
      await fetch(`${apiUrl}/api/v1/workflows/${id}/resume`, {
        method: 'POST',
        headers: { Authorization: `Bearer ${token}` }
      });
      setCurrentWorkflow(prev => prev ? { ...prev, status: 'running' } : null);
    } catch (error) {
        notification.error({ message: 'Failed to resume workflow' });
    }
  }, [apiUrl, token]);

  const cancelWorkflow = useCallback(async (id: string) => {
    if (!token) return;
    try {
      await fetch(`${apiUrl}/api/v1/workflows/${id}/cancel`, {
        method: 'POST',
        headers: { Authorization: `Bearer ${token}` }
      });
      setCurrentWorkflow(prev => prev ? { ...prev, status: 'cancelled' } : null);
    } catch (error) {
        notification.error({ message: 'Failed to cancel workflow' });
    }
  }, [apiUrl, token]);

  return {
    definitions,
    currentWorkflow,
    isLoading,
    fetchDefinitions,
    createWorkflow,
    startWorkflow,
    pauseWorkflow,
    resumeWorkflow,
    cancelWorkflow
  };
};
