import type { RateLimitStatus, LLMProvider } from './types.js';
export declare function updateRateLimitFromHeaders(headers: Headers, provider: string): void;
export declare function getRateLimitStatus(provider: string): RateLimitStatus | null;
export declare function isProviderRateLimited(provider: string, minPercent?: number): boolean;
export declare function filterCandidatesByRateLimit(candidates: LLMProvider[], minPercent?: number): {
    filtered: LLMProvider[];
    limitedProviders: string[];
};
export declare function getAllRateLimitStatuses(): Record<string, RateLimitStatus>;
