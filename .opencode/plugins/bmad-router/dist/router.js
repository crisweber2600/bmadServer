import NotDiamond from 'notdiamond';
const DEFAULT_TIMEOUT_MS = 5000;
function mapToNotDiamondFormat(candidates) {
    return candidates.map(c => ({
        provider: mapProviderName(c.provider),
        model: c.model,
    }));
}
function mapProviderName(provider) {
    const mapping = {
        'github-copilot': 'openai',
        'copilot': 'openai',
    };
    return mapping[provider] ?? provider;
}
function extractMessageText(parts) {
    return parts
        .filter(p => p.type === 'text' && p.text)
        .map(p => p.text)
        .join('\n')
        .slice(0, 4000);
}
function getApiKey() {
    return process.env.NOT_DIAMOND_API_KEY ?? process.env.NOTDIAMOND_API_KEY;
}
export class NotDiamondRouter {
    constructor(apiKey, timeoutMs = DEFAULT_TIMEOUT_MS) {
        this.client = null;
        this.timeoutMs = timeoutMs;
        const key = apiKey ?? getApiKey();
        if (key) {
            try {
                this.client = new NotDiamond({ apiKey: key });
            }
            catch {
                this.client = null;
            }
        }
    }
    async selectModel(messageContext, candidates, tradeoff = 'quality') {
        if (candidates.length === 0) {
            throw new Error('No candidates provided for routing');
        }
        if (candidates.length === 1) {
            return candidates[0];
        }
        if (!this.client) {
            return candidates[0];
        }
        try {
            const ndCandidates = mapToNotDiamondFormat(candidates);
            const controller = new AbortController();
            const timeoutId = setTimeout(() => controller.abort(), this.timeoutMs);
            try {
                const result = await this.client.modelRouter.selectModel({
                    messages: [{ role: 'user', content: messageContext }],
                    llm_providers: ndCandidates,
                    tradeoff,
                });
                clearTimeout(timeoutId);
                if (result?.providers?.[0]) {
                    const selected = result.providers[0];
                    // Prefer exact provider match first.
                    // This avoids provider alias collisions where github-copilot is mapped to openai
                    // and accidentally wins due to candidate ordering.
                    const exact = candidates.find(c => c.provider === selected.provider && c.model === selected.model);
                    if (exact)
                        return exact;
                    const mapped = candidates.find(c => mapProviderName(c.provider) === selected.provider && c.model === selected.model);
                    if (mapped)
                        return mapped;
                }
            }
            catch (err) {
                clearTimeout(timeoutId);
                if (err.name === 'AbortError') {
                    console.warn('[bmad-router] NotDiamond API timeout, using fallback');
                }
                else {
                    console.warn('[bmad-router] NotDiamond API error:', err);
                }
            }
        }
        catch (err) {
            console.warn('[bmad-router] NotDiamond routing failed:', err);
        }
        return candidates[0];
    }
}
export async function routeModel(candidates, parts, tradeoff = 'quality') {
    const router = new NotDiamondRouter();
    const messageText = extractMessageText(parts);
    return router.selectModel(messageText, candidates, tradeoff);
}
