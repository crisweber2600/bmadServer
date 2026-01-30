/**
 * Decision Management Types
 * Matches backend DTOs from IDecisionService
 */

/** Response from GET /api/v1/decisions/{id} */
export interface DecisionResponse {
  Id: string;
  WorkflowInstanceId: string;
  StepId: string;
  DecisionType: string;
  Value: unknown;
  DecidedBy: string;
  DecidedAt: string;
  CurrentVersion: number;
  IsLocked: boolean;
  LockedBy?: string;
  LockedAt?: string;
  LockReason?: string;
  Status: string;
}

/** Response from GET /api/v1/decisions/{id}/versions */
export interface DecisionVersionResponse {
  Id: string;
  VersionNumber: number;
  Value: unknown;
  ModifiedBy: string;
  ModifiedAt: string;
  ChangeReason?: string;
  Question?: string;
  Options?: string[];
  Reasoning?: string;
  Context?: string;
}

/** Response from GET /api/v1/decisions/{id}/versions/{v1}/diff/{v2} */
export interface DecisionVersionDiffResponse {
  FromVersion: number;
  ToVersion: number;
  Changes: DiffChange[];
}

/** Single change in a diff */
export interface DiffChange {
  Field: string;
  OldValue: unknown;
  NewValue: unknown;
}

/** Response from GET /api/v1/workflows/{id}/conflicts */
export interface ConflictResponse {
  Id: string;
  DecisionId1: string;
  DecisionId2: string;
  ConflictType: string;
  Description: string;
  Severity: 'Low' | 'Medium' | 'High' | 'Critical';
  Status: 'Open' | 'Resolved' | 'Overridden' | 'Dismissed';
  DetectedAt: string;
  ResolvedAt?: string;
  Resolution?: string;
}

/** Request body for POST /api/v1/decisions/{id}/lock */
export interface LockDecisionRequest {
  reason?: string;
}

/** Request body for POST /api/v1/decisions/{id}/revert */
export interface RevertDecisionRequest {
  versionNumber: number;
}

/** Request body for POST /api/v1/decisions/{id}/reviews */
export interface ReviewRequestRequest {
  reviewerIds: string[];
  deadline?: string;
  notes?: string;
}

/** Response from POST /api/v1/decisions/{id}/reviews */
export interface ReviewRequestResponse {
  Id: string;
  DecisionId: string;
  ReviewerIds: string[];
  RequestedBy: string;
  RequestedAt: string;
  Deadline?: string;
  Status: 'pending' | 'in_review' | 'approved' | 'rejected';
}

/** Request body for POST /api/v1/conflicts/{id}/resolve */
export interface ResolveConflictRequest {
  resolution: string;
  selectedDecisionId: string;
  notes?: string;
}

/** Request body for POST /api/v1/conflicts/{id}/override */
export interface OverrideConflictRequest {
  justification: string;
}

/** Workflow member for reviewer selection */
export interface WorkflowMember {
  Id: string;
  DisplayName: string;
  Email: string;
  Role?: string;
}

/** Checkpoint for workflow state preservation */
export interface CheckpointResponse {
  Id: string;
  Name: string;
  Description?: string;
  CreatedAt: string;
  CreatedBy: string;
  WorkflowState: unknown;
}

/** API error response */
export interface ApiError {
  code: 'CONFLICT' | 'FORBIDDEN' | 'NOT_FOUND' | 'NETWORK' | 'VALIDATION' | 'UNKNOWN';
  message: string;
  details?: Record<string, unknown>;
  isRetryable?: boolean;
}

/** Generic API result wrapper */
export type ApiResult<T> = 
  | { success: true; data: T }
  | { success: false; error: ApiError };
