import { useState, useCallback } from 'react';
import type {
  DecisionResponse,
  DecisionVersionResponse,
  DecisionVersionDiffResponse,
  ApiError,
  ApiResult,
  ReviewRequestResponse,
} from '../types';

/**
 * Hook options for decision management
 */
export interface UseDecisionsOptions {
  /** Workflow ID to scope decisions */
  workflowId: string;
  /** Base API URL - defaults to environment variable or localhost */
  apiUrl?: string;
  /** Enable debug logging */
  debug?: boolean;
  /** Callback when a decision is updated */
  onDecisionUpdated?: (decision: DecisionResponse) => void;
  /** Callback when a decision is locked */
  onDecisionLocked?: (decision: DecisionResponse) => void;
  /** Callback when a decision is unlocked */
  onDecisionUnlocked?: (decision: DecisionResponse) => void;
  /** Callback when a decision is reverted */
  onDecisionReverted?: (decision: DecisionResponse) => void;
}

/**
 * Return type for useDecisions hook
 */
export interface UseDecisionsReturn {
  /** Current list of decisions */
  decisions: DecisionResponse[];
  /** Loading state */
  loading: boolean;
  /** Current error state */
  error: ApiError | null;
  /** Fetch all decisions for the workflow */
  getDecisions: () => Promise<ApiResult<DecisionResponse[]>>;
  /** Fetch a single decision by ID */
  getDecision: (decisionId: string) => Promise<ApiResult<DecisionResponse>>;
  /** Update a decision (creates new version) */
  updateDecision: (decisionId: string, value: unknown, changeReason?: string) => Promise<ApiResult<DecisionResponse>>;
  /** Get version history for a decision */
  getVersionHistory: (decisionId: string) => Promise<ApiResult<DecisionVersionResponse[]>>;
  /** Get diff between two versions */
  getDiff: (decisionId: string, fromVersion: number, toVersion: number) => Promise<ApiResult<DecisionVersionDiffResponse>>;
  /** Revert to a previous version */
  revertToVersion: (decisionId: string, versionNumber: number) => Promise<ApiResult<DecisionResponse>>;
  /** Lock a decision */
  lockDecision: (decisionId: string, reason?: string) => Promise<ApiResult<DecisionResponse>>;
  /** Unlock a decision */
  unlockDecision: (decisionId: string) => Promise<ApiResult<DecisionResponse>>;
  /** Request review for a decision */
  requestReview: (decisionId: string, reviewerIds: string[], deadline?: string, notes?: string) => Promise<ApiResult<ReviewRequestResponse>>;
  /** Clear current error */
  clearError: () => void;
  /** Number of retry attempts for current operation */
  retryCount: number;
}

/**
 * Hook for managing decisions with full CRUD operations
 * 
 * Provides comprehensive error handling with error codes for specific scenarios:
 * - CONFLICT (409): Resource already locked or version conflict
 * - FORBIDDEN (403): Permission denied
 * - NOT_FOUND (404): Resource not found
 * - NETWORK: Connection issues (retryable)
 * - UNKNOWN: Unexpected errors
 * 
 * @example
 * ```tsx
 * const { decisions, getDecisions, lockDecision, error } = useDecisions({
 *   workflowId: 'workflow-123',
 *   onDecisionLocked: (d) => toast.success(`Locked decision ${d.Id}`),
 * });
 * 
 * useEffect(() => {
 *   getDecisions();
 * }, [getDecisions]);
 * 
 * const handleLock = async (id: string) => {
 *   const result = await lockDecision(id, 'Final review');
 *   if (!result.success && result.error.code === 'CONFLICT') {
 *     toast.error(result.error.message);
 *   }
 * };
 * ```
 */
export function useDecisions(options: UseDecisionsOptions): UseDecisionsReturn {
  const {
    workflowId,
    apiUrl = import.meta.env.VITE_API_URL || 'http://localhost:8080',
    debug = false,
    onDecisionUpdated,
    onDecisionLocked,
    onDecisionUnlocked,
    onDecisionReverted,
  } = options;

  const [decisions, setDecisions] = useState<DecisionResponse[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<ApiError | null>(null);
  const [retryCount, setRetryCount] = useState(0);

  // Debug logging helper
  const log = useCallback((message: string, data?: unknown) => {
    if (debug) {
      console.log(`[useDecisions] ${message}`, data || '');
    }
  }, [debug]);

  // API call wrapper with error handling
  const apiCall = useCallback(async <T>(
    fn: () => Promise<Response>,
    operation: string
  ): Promise<ApiResult<T>> => {
    try {
      setLoading(true);
      setError(null);
      
      const response = await fn();
      
      if (!response.ok) {
        let errorData: { message?: string } = {};
        try {
          errorData = await response.json();
        } catch {
          // Response may not have JSON body
        }
        
        let apiError: ApiError;
        
        switch (response.status) {
          case 409:
            apiError = {
              code: 'CONFLICT',
              message: errorData.message || 'Resource conflict - item may already be locked or modified',
              isRetryable: false,
            };
            break;
          case 403:
            apiError = {
              code: 'FORBIDDEN',
              message: errorData.message || 'Permission denied',
              isRetryable: false,
            };
            break;
          case 404:
            apiError = {
              code: 'NOT_FOUND',
              message: errorData.message || 'Resource not found',
              isRetryable: false,
            };
            break;
          case 400:
            apiError = {
              code: 'VALIDATION',
              message: errorData.message || 'Invalid request data',
              isRetryable: false,
            };
            break;
          default:
            apiError = {
              code: 'UNKNOWN',
              message: errorData.message || `Request failed with status ${response.status}`,
              isRetryable: response.status >= 500,
            };
        }
        
        log(`${operation} failed`, apiError);
        setError(apiError);
        return { success: false, error: apiError };
      }
      
      const data = await response.json() as T;
      log(`${operation} succeeded`, data);
      return { success: true, data };
      
    } catch (err) {
      const isNetworkError = err instanceof TypeError && err.message.includes('fetch');
      const apiError: ApiError = {
        code: isNetworkError ? 'NETWORK' : 'UNKNOWN',
        message: isNetworkError 
          ? 'Network error - please check your connection'
          : (err instanceof Error ? err.message : String(err)),
        isRetryable: isNetworkError,
      };
      
      if (isNetworkError) {
        setRetryCount((prev) => prev + 1);
      }
      
      log(`${operation} threw error`, apiError);
      setError(apiError);
      return { success: false, error: apiError };
      
    } finally {
      setLoading(false);
    }
  }, [log]);

  // Get auth headers
  const getHeaders = useCallback((): HeadersInit => {
    const token = localStorage.getItem('accessToken');
    return {
      'Content-Type': 'application/json',
      ...(token ? { 'Authorization': `Bearer ${token}` } : {}),
    };
  }, []);

  // Fetch all decisions for workflow
  const getDecisions = useCallback(async (): Promise<ApiResult<DecisionResponse[]>> => {
    const result = await apiCall<DecisionResponse[]>(
      () => fetch(`${apiUrl}/api/v1/workflows/${workflowId}/decisions`, {
        method: 'GET',
        headers: getHeaders(),
      }),
      'getDecisions'
    );
    
    if (result.success) {
      setDecisions(result.data);
    }
    
    return result;
  }, [apiCall, apiUrl, workflowId, getHeaders]);

  // Fetch single decision
  const getDecision = useCallback(async (decisionId: string): Promise<ApiResult<DecisionResponse>> => {
    return apiCall<DecisionResponse>(
      () => fetch(`${apiUrl}/api/v1/decisions/${decisionId}`, {
        method: 'GET',
        headers: getHeaders(),
      }),
      'getDecision'
    );
  }, [apiCall, apiUrl, getHeaders]);

  // Update decision (creates new version)
  const updateDecision = useCallback(async (
    decisionId: string,
    value: unknown,
    changeReason?: string
  ): Promise<ApiResult<DecisionResponse>> => {
    const result = await apiCall<DecisionResponse>(
      () => fetch(`${apiUrl}/api/v1/decisions/${decisionId}`, {
        method: 'PUT',
        headers: getHeaders(),
        body: JSON.stringify({ value, changeReason }),
      }),
      'updateDecision'
    );
    
    if (result.success) {
      setDecisions((prev) =>
        prev.map((d) => d.Id === decisionId ? result.data : d)
      );
      onDecisionUpdated?.(result.data);
    }
    
    return result;
  }, [apiCall, apiUrl, getHeaders, onDecisionUpdated]);

  // Get version history
  const getVersionHistory = useCallback(async (
    decisionId: string
  ): Promise<ApiResult<DecisionVersionResponse[]>> => {
    return apiCall<DecisionVersionResponse[]>(
      () => fetch(`${apiUrl}/api/v1/decisions/${decisionId}/versions`, {
        method: 'GET',
        headers: getHeaders(),
      }),
      'getVersionHistory'
    );
  }, [apiCall, apiUrl, getHeaders]);

  // Get diff between versions
  const getDiff = useCallback(async (
    decisionId: string,
    fromVersion: number,
    toVersion: number
  ): Promise<ApiResult<DecisionVersionDiffResponse>> => {
    return apiCall<DecisionVersionDiffResponse>(
      () => fetch(`${apiUrl}/api/v1/decisions/${decisionId}/versions/${fromVersion}/diff/${toVersion}`, {
        method: 'GET',
        headers: getHeaders(),
      }),
      'getDiff'
    );
  }, [apiCall, apiUrl, getHeaders]);

  // Revert to version
  const revertToVersion = useCallback(async (
    decisionId: string,
    versionNumber: number
  ): Promise<ApiResult<DecisionResponse>> => {
    const result = await apiCall<DecisionResponse>(
      () => fetch(`${apiUrl}/api/v1/decisions/${decisionId}/revert`, {
        method: 'POST',
        headers: getHeaders(),
        body: JSON.stringify({ versionNumber }),
      }),
      'revertToVersion'
    );
    
    if (result.success) {
      setDecisions((prev) =>
        prev.map((d) => d.Id === decisionId ? result.data : d)
      );
      onDecisionReverted?.(result.data);
    }
    
    return result;
  }, [apiCall, apiUrl, getHeaders, onDecisionReverted]);

  // Lock decision
  const lockDecision = useCallback(async (
    decisionId: string,
    reason?: string
  ): Promise<ApiResult<DecisionResponse>> => {
    const result = await apiCall<DecisionResponse>(
      () => fetch(`${apiUrl}/api/v1/decisions/${decisionId}/lock`, {
        method: 'POST',
        headers: getHeaders(),
        body: JSON.stringify({ reason }),
      }),
      'lockDecision'
    );
    
    if (result.success) {
      setDecisions((prev) =>
        prev.map((d) => d.Id === decisionId ? result.data : d)
      );
      onDecisionLocked?.(result.data);
    }
    
    return result;
  }, [apiCall, apiUrl, getHeaders, onDecisionLocked]);

  // Unlock decision
  const unlockDecision = useCallback(async (
    decisionId: string
  ): Promise<ApiResult<DecisionResponse>> => {
    const result = await apiCall<DecisionResponse>(
      () => fetch(`${apiUrl}/api/v1/decisions/${decisionId}/unlock`, {
        method: 'POST',
        headers: getHeaders(),
      }),
      'unlockDecision'
    );
    
    if (result.success) {
      setDecisions((prev) =>
        prev.map((d) => d.Id === decisionId ? result.data : d)
      );
      onDecisionUnlocked?.(result.data);
    }
    
    return result;
  }, [apiCall, apiUrl, getHeaders, onDecisionUnlocked]);

  // Request review
  const requestReview = useCallback(async (
    decisionId: string,
    reviewerIds: string[],
    deadline?: string,
    notes?: string
  ): Promise<ApiResult<ReviewRequestResponse>> => {
    return apiCall<ReviewRequestResponse>(
      () => fetch(`${apiUrl}/api/v1/decisions/${decisionId}/reviews`, {
        method: 'POST',
        headers: getHeaders(),
        body: JSON.stringify({ reviewerIds, deadline, notes }),
      }),
      'requestReview'
    );
  }, [apiCall, apiUrl, getHeaders]);

  // Clear error
  const clearError = useCallback(() => {
    setError(null);
    setRetryCount(0);
  }, []);

  return {
    decisions,
    loading,
    error,
    getDecisions,
    getDecision,
    updateDecision,
    getVersionHistory,
    getDiff,
    revertToVersion,
    lockDecision,
    unlockDecision,
    requestReview,
    clearError,
    retryCount,
  };
}
