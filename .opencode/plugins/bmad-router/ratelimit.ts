import type { RateLimitStatus, LLMProvider } from './types.js';

const DEFAULT_MIN_PERCENT = 15;
const RATE_LIMIT_CACHE_TTL_MS = 60000;

interface CachedRateLimit {
  status: RateLimitStatus;
  cachedAt: number;
}

const rateLimitCache = new Map<string, CachedRateLimit>();

function parseRateLimitHeaders(headers: Headers, provider: string): RateLimitStatus | null {
  if (provider === 'openai') {
    const remaining = headers.get('x-ratelimit-remaining-requests');
    const limit = headers.get('x-ratelimit-limit-requests');
    const resetMs = headers.get('x-ratelimit-reset-requests');
    
    if (remaining && limit) {
      const remainingNum = parseInt(remaining, 10);
      const limitNum = parseInt(limit, 10);
      const percentRemaining = limitNum > 0 ? (remainingNum / limitNum) * 100 : 100;
      
      return {
        provider,
        remainingRequests: remainingNum,
        percentRemaining,
        resetAt: resetMs ? new Date(Date.now() + parseInt(resetMs, 10)) : undefined,
        isLimited: percentRemaining < DEFAULT_MIN_PERCENT,
      };
    }
  }
  
  if (provider === 'anthropic') {
    const remaining = headers.get('anthropic-ratelimit-requests-remaining');
    const limit = headers.get('anthropic-ratelimit-requests-limit');
    const reset = headers.get('anthropic-ratelimit-requests-reset');
    
    if (remaining && limit) {
      const remainingNum = parseInt(remaining, 10);
      const limitNum = parseInt(limit, 10);
      const percentRemaining = limitNum > 0 ? (remainingNum / limitNum) * 100 : 100;
      
      return {
        provider,
        remainingRequests: remainingNum,
        percentRemaining,
        resetAt: reset ? new Date(reset) : undefined,
        isLimited: percentRemaining < DEFAULT_MIN_PERCENT,
      };
    }
  }
  
  return null;
}

export function updateRateLimitFromHeaders(headers: Headers, provider: string): void {
  const status = parseRateLimitHeaders(headers, provider);
  if (status) {
    rateLimitCache.set(provider, {
      status,
      cachedAt: Date.now(),
    });
    
    if (status.isLimited) {
    }
  }
}

export function getRateLimitStatus(provider: string): RateLimitStatus | null {
  const cached = rateLimitCache.get(provider);
  if (!cached) return null;
  
  if (Date.now() - cached.cachedAt > RATE_LIMIT_CACHE_TTL_MS) {
    rateLimitCache.delete(provider);
    return null;
  }
  
  return cached.status;
}

export function isProviderRateLimited(provider: string, minPercent: number = DEFAULT_MIN_PERCENT): boolean {
  const status = getRateLimitStatus(provider);
  if (!status) return false;
  return status.percentRemaining < minPercent;
}

export function filterCandidatesByRateLimit(
  candidates: LLMProvider[],
  minPercent: number = DEFAULT_MIN_PERCENT
): { filtered: LLMProvider[]; limitedProviders: string[] } {
  const limitedProviders: string[] = [];
  
  const filtered = candidates.filter(c => {
    if (isProviderRateLimited(c.provider, minPercent)) {
      if (!limitedProviders.includes(c.provider)) {
        limitedProviders.push(c.provider);
      }
      return false;
    }
    return true;
  });
  
  if (filtered.length === 0) {
    return { filtered: candidates, limitedProviders: [] };
  }
  
  return { filtered, limitedProviders };
}

export function getAllRateLimitStatuses(): Record<string, RateLimitStatus> {
  const result: Record<string, RateLimitStatus> = {};
  
  for (const [provider, cached] of rateLimitCache.entries()) {
    if (Date.now() - cached.cachedAt <= RATE_LIMIT_CACHE_TTL_MS) {
      result[provider] = cached.status;
    }
  }
  
  return result;
}
