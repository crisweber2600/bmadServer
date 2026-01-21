import type { RateLimitStatus, LLMProvider } from './types.js';
export declare function updateRateLimitFromHeaders(headers: Headers, provider: string): void;
/**
 * Mark a provider as rate-limited when receiving a 429 response
 * This bypasses header parsing and immediately marks the provider as unavailable
 */
export declare function markProviderRateLimited(provider: string, resetAt?: Date): void;
export declare function getRateLimitStatus(provider: string): RateLimitStatus | null;
export declare function isProviderRateLimited(provider: string, minPercent?: number): boolean;
export declare function filterCandidatesByRateLimit(candidates: LLMProvider[], minPercent?: number): {
    filtered: LLMProvider[];
    limitedProviders: string[];
};
export declare function getAllRateLimitStatuses(): Record<string, RateLimitStatus>;
