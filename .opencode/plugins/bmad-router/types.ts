/**
 * BMAD-Aware Model Router - Type Definitions
 * 
 * Shared TypeScript interfaces for the bmad-router plugin.
 */

/**
 * Represents an LLM provider and model combination
 */
export interface LLMProvider {
  provider: string;
  model: string;
}

/**
 * Result from quota checking
 */
export interface QuotaResult {
  provider: string;
  percentRemaining: number;
  tier?: string;
  unlimited?: boolean;
}

/**
 * BMAD workflow phases
 * 
 * Dev phases (Copilot allowed): quick-dev, code-review, in-progress, review
 * Non-dev phases (no Copilot): quick-spec, prd, architecture, brainstorm, research, unknown
 */
export type BmadPhase = 
  | 'quick-spec'
  | 'quick-dev'
  | 'code-review'
  | 'prd'
  | 'architecture'
  | 'brainstorm'
  | 'research'
  | 'in-progress'
  | 'review'
  | 'done'
  | 'backlog'
  | 'unknown';

/**
 * Result from model routing
 */
export interface RouteResult {
  provider: string;
  model: string;
  reason: string;
}

/**
 * Structure of OpenCode auth.json file
 */
export interface AuthData {
  'github-copilot'?: {
    type: 'oauth';
    refresh: string;
    access: string;
    expires: number;
  };
  [key: string]: {
    type: string;
    refresh?: string;
    access?: string;
    expires?: number;
    key?: string;
  } | undefined;
}

/**
 * BMAD workflow status YAML structure (Quick Flow)
 */
export interface BmmWorkflowStatus {
  generated?: string;
  project?: string;
  selected_track?: string;
  workflow_status?: {
    'quick-spec'?: string;
    'quick-dev'?: string;
    'code-review'?: string;
    [key: string]: string | undefined;
  };
}

/**
 * Sprint status YAML structure (full BMAD Method)
 */
export interface SprintStatus {
  development_status?: {
    [key: string]: string;
  };
}

/**
 * Copilot token exchange response
 */
export interface CopilotTokenResponse {
  token: string;
  expires_at: number;
}

/**
 * Copilot quota API response
 */
export interface CopilotQuotaResponse {
  copilot_ide_chat?: {
    chat_quota?: {
      premium_requests_remaining?: number;
      premium_requests_limit?: number;
      overage_limit?: number;
      unlimited?: boolean;
    };
  };
}

/**
 * Provider for checking quota
 */
export interface QuotaProvider {
  readonly name: string;
  checkQuota(): Promise<QuotaResult | null>;
}

/**
 * NotDiamond tradeoff preference
 */
export type NotDiamondTradeoff = 'cost' | 'latency' | 'quality';

/**
 * Rate limit status for a provider
 */
export interface RateLimitStatus {
  provider: string;
  remainingRequests?: number;
  remainingTokens?: number;
  resetAt?: Date;
  percentRemaining: number;
  isLimited: boolean;
}
