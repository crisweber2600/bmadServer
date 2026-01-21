import { tool } from '@opencode-ai/plugin';
import { detectCurrentPhase } from './workflow.js';
import { filterCandidatesByPhase, isDevPhase } from './rules.js';
import { filterCandidatesByQuota, getCopilotQuota } from './quota.js';
import { filterCandidatesByRateLimit, getAllRateLimitStatuses, updateRateLimitFromHeaders } from './ratelimit.js';
import { mapProviderToOpenCode } from './model-mapping.js';
import { routeModel } from './router.js';
globalThis.__bmadRouterUpdateRateLimit = updateRateLimitFromHeaders;
const DEFAULT_CANDIDATES = [
    // Premium GitHub Copilot models
    { provider: 'github-copilot', model: 'gpt-5.2' },
    { provider: 'github-copilot', model: 'gpt-5.1' },
    { provider: 'github-copilot', model: 'gpt-5' },
    { provider: 'github-copilot', model: 'gpt-4.1' },
    { provider: 'github-copilot', model: 'claude-opus-4.5' },
    { provider: 'github-copilot', model: 'claude-sonnet-4.5' },
    { provider: 'github-copilot', model: 'claude-sonnet-4' },
    { provider: 'github-copilot', model: 'claude-haiku-4.5' },
    { provider: 'github-copilot', model: 'gemini-3-pro-preview' },
    { provider: 'github-copilot', model: 'gemini-3-flash-preview' },
    { provider: 'github-copilot', model: 'gemini-2.5-pro' },
    { provider: 'github-copilot', model: 'grok-code-fast-1' },
    // Standard GitHub Copilot models
    { provider: 'github-copilot', model: 'gpt-4o' },
    { provider: 'github-copilot', model: 'gpt-5-mini' },
    { provider: 'github-copilot', model: 'gpt-4o-mini' },
    // Direct Anthropic models (when available)
    { provider: 'anthropic', model: 'claude-opus-4-1-20250805' },
    { provider: 'anthropic', model: 'claude-opus-4-20250514' },
    { provider: 'anthropic', model: 'claude-sonnet-4-5-20250929' },
    { provider: 'anthropic', model: 'claude-sonnet-4-20250514' },
    { provider: 'anthropic', model: 'claude-haiku-4-5-20251001' },
    { provider: 'anthropic', model: 'claude-3-7-sonnet-20250219' },
    { provider: 'anthropic', model: 'claude-3-5-haiku-20241022' },
    { provider: 'anthropic', model: 'claude-3-haiku-20240307' },
    // Direct OpenAI models (when available) 
    { provider: 'openai', model: 'gpt-5.2' },
    { provider: 'openai', model: 'gpt-5.1-codex-max' },
    { provider: 'openai', model: 'gpt-5.2-codex' },
];
const KEEP_MODEL_PATTERNS = [/^!km\b/i, /^!keep-model\b/i];
function shouldKeepModel(text) {
    for (const pattern of KEEP_MODEL_PATTERNS) {
        if (pattern.test(text)) {
            return {
                keep: true,
                cleanedText: text.replace(pattern, '').trimStart()
            };
        }
    }
    return { keep: false, cleanedText: text };
}
function getTextFromParts(parts) {
    const textPart = parts.find(p => p.type === 'text' && p.text);
    return textPart?.type === 'text' ? textPart.text : null;
}
function buildRouteReason(phase, selected, copilotFiltered, rateLimitedProviders) {
    const reasons = [];
    if (!isDevPhase(phase)) {
        reasons.push(`non-dev phase (${phase})`);
    }
    if (copilotFiltered) {
        reasons.push('copilot quota low');
    }
    if (rateLimitedProviders.length > 0) {
        reasons.push(`rate limited: ${rateLimitedProviders.join(', ')}`);
    }
    reasons.push(`selected: ${selected.provider}/${selected.model}`);
    return reasons.join(', ');
}
export const BmadRouterPlugin = async ({ client, directory }) => {
    const hooks = {
        'chat.message': async (input, output) => {
            const textPart = output.parts.find(p => p.type === 'text');
            if (textPart?.type === 'text') {
                const { keep, cleanedText } = shouldKeepModel(textPart.text);
                if (keep) {
                    textPart.text = cleanedText;
                    return;
                }
            }
            const phase = await detectCurrentPhase(directory);
            let candidates = filterCandidatesByPhase(DEFAULT_CANDIDATES, phase);
            const candidatesBeforeQuota = candidates.length;
            candidates = await filterCandidatesByQuota(candidates);
            const copilotFiltered = candidates.length < candidatesBeforeQuota;
            const { filtered: rateLimitFiltered, limitedProviders } = filterCandidatesByRateLimit(candidates);
            candidates = rateLimitFiltered;
            if (candidates.length === 0) {
                return;
            }
            const selected = await routeModel(candidates, output.parts);
            // Map NotDiamond model name back to OpenCode-compatible model
            const opencodeModel = mapProviderToOpenCode(selected);
            output.message.model = {
                providerID: opencodeModel.provider,
                modelID: opencodeModel.model,
            };
            const reason = buildRouteReason(phase, selected, copilotFiltered, limitedProviders);
            try {
                await client.tui.publish({
                    body: {
                        type: 'tui.toast.show',
                        properties: {
                            message: `Model: ${opencodeModel.provider}/${opencodeModel.model}`,
                            variant: 'info',
                            duration: 3000,
                        },
                    },
                });
            }
            catch {
            }
        },
        tool: {
            bmad_route_info: tool({
                description: 'Show current BMAD routing status and recommendations',
                args: {},
                async execute() {
                    const phase = await detectCurrentPhase(directory);
                    const quota = await getCopilotQuota();
                    const copilotAllowed = isDevPhase(phase);
                    const rateLimits = getAllRateLimitStatuses();
                    return JSON.stringify({
                        phase,
                        copilotAllowed,
                        quota: quota ? {
                            percentRemaining: Math.round(quota.percentRemaining),
                            unlimited: quota.unlimited ?? false,
                        } : null,
                        rateLimits: Object.fromEntries(Object.entries(rateLimits).map(([k, v]) => [k, {
                                percentRemaining: Math.round(v.percentRemaining),
                                isLimited: v.isLimited,
                                remainingRequests: v.remainingRequests,
                            }])),
                        candidates: filterCandidatesByPhase(DEFAULT_CANDIDATES, phase),
                    }, null, 2);
                },
            }),
        },
    };
    return hooks;
};
export default BmadRouterPlugin;
