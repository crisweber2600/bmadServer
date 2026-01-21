# BMAD Router Rate Limit Detection Fix

## Problem
The bmad-router plugin was not detecting when Anthropic (or other providers) returned 429 rate limit errors, causing it to continue selecting rate-limited providers for subsequent requests.

## Solution
Added comprehensive rate limit detection that handles:
1. **429 error responses** - Direct marking of providers as rate-limited
2. **retry-after headers** - Parsing of rate limit reset times from both Anthropic and OpenAI
3. **Automatic filtering** - Rate-limited providers are automatically excluded from candidate selection
4. **Fallback behavior** - If all candidates are rate-limited, returns all candidates to prevent complete failure

## Changes Made

### 1. New Function: `markProviderRateLimited()`
Immediately marks a provider as rate-limited when a 429 error occurs:

```javascript
// When OpenCode receives a 429 error from a provider:
globalThis.__bmadRouterMarkRateLimited('anthropic', resetDate);
```

### 2. Enhanced Header Parsing
Now parses `retry-after` headers from both OpenAI and Anthropic 429 responses:
- Supports both ISO date format and seconds-since-now format
- Automatically sets rate limit status based on retry-after value

### 3. Automatic Provider Filtering
The router now automatically excludes rate-limited providers when selecting models:
```javascript
// In the routing pipeline:
candidates = await filterCandidatesByQuota(candidates);
const { filtered, limitedProviders } = filterCandidatesByRateLimit(candidates);
candidates = filtered;
```

## Integration Guide for OpenCode

OpenCode should call these functions when handling API responses:

### On Successful Response (200-299)
```javascript
if (response.ok) {
  // Existing behavior - update rate limits from response headers
  globalThis.__bmadRouterUpdateRateLimit?.(response.headers, provider);
}
```

### On 429 Rate Limit Error
```javascript
if (response.status === 429) {
  // NEW: Mark provider as rate-limited
  const retryAfter = response.headers.get('retry-after');
  const resetDate = retryAfter 
    ? new Date(Date.now() + (parseInt(retryAfter) * 1000))
    : undefined;
  
  globalThis.__bmadRouterMarkRateLimited?.(provider, resetDate);
  
  // Also parse headers if available (fallback)
  globalThis.__bmadRouterUpdateRateLimit?.(response.headers, provider);
}
```

## Global Functions Exposed

### `__bmadRouterUpdateRateLimit(headers, provider)`
- **Purpose**: Parse rate limit info from successful response headers
- **When to call**: After every successful API response (200-299)
- **Parameters**:
  - `headers`: Response Headers object
  - `provider`: Provider name ('anthropic', 'openai', 'github-copilot')

### `__bmadRouterMarkRateLimited(provider, resetAt?)` ⭐ NEW
- **Purpose**: Immediately mark a provider as rate-limited (e.g., after 429 error)
- **When to call**: When receiving a 429 error response
- **Parameters**:
  - `provider`: Provider name ('anthropic', 'openai', 'github-copilot')
  - `resetAt`: Optional Date when rate limit resets (parsed from retry-after)

## Testing

Run the test suite to verify the fix:
```bash
node test-rate-limit-fix.js
```

Expected output:
- ✅ 429 responses mark providers as rate-limited
- ✅ Rate-limited providers are filtered from candidates
- ✅ retry-after headers are parsed correctly
- ✅ Fallback behavior prevents empty candidate list

## Behavior

### Before Fix
1. Anthropic returns 429
2. Router continues selecting Anthropic models
3. Repeated 429 errors occur
4. User experience degrades

### After Fix
1. Anthropic returns 429
2. OpenCode calls `__bmadRouterMarkRateLimited('anthropic')`
3. Router automatically excludes Anthropic from next selection
4. Router selects alternative provider (e.g., github-copilot)
5. Requests succeed with different provider
6. After cache TTL (60s) or reset time, Anthropic becomes available again

## Rate Limit Cache
- **TTL**: 60 seconds (default)
- **Storage**: In-memory Map (clears on restart)
- **Eviction**: Automatic after TTL or when reset time is reached
- **Threshold**: Providers marked as limited when <15% quota remaining

## Notes
- The fix is backward compatible - if OpenCode doesn't call the new function, existing behavior continues
- Rate limit status is shared across all router instances (global cache)
- If all providers are rate-limited, the router returns all candidates to allow the request to proceed (with expected failure)
