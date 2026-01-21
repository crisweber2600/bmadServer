const OPENAI_MODEL_MAPPING = {
    // GPT-5 series
    'gpt-5.2-2025-12-11': 'gpt-4o',
    'gpt-5.2-pro-2025-12-11': 'gpt-4o',
    'gpt-5.1-2025-11-13': 'gpt-4o',
    'gpt-5-2025-08-07': 'gpt-4o',
    'gpt-5-mini-2025-08-07': 'gpt-4o-mini',
    'gpt-5-nano-2025-08-07': 'gpt-4o-mini',
    // GPT-4.1 series
    'gpt-4.1-2025-04-14': 'gpt-4o',
    'gpt-4.1-mini-2025-04-14': 'gpt-4o-mini',
    'gpt-4.1-nano-2025-04-14': 'gpt-4o-mini',
    // GPT-4o series
    'gpt-4o-2024-11-20': 'gpt-4o',
    'gpt-4o-2024-08-06': 'gpt-4o',
    'gpt-4o-2024-05-13': 'gpt-4o',
    'gpt-4o-mini-2024-07-18': 'gpt-4o-mini',
    'chatgpt-4o-latest': 'gpt-4o',
    // GPT-4-turbo
    'gpt-4-turbo-2024-04-09': 'gpt-4-turbo',
    'gpt-4-0125-preview': 'gpt-4-turbo',
    'gpt-4-1106-preview': 'gpt-4-turbo',
    'gpt-4-0613': 'gpt-4-turbo',
    // GPT-3.5
    'gpt-3.5-turbo-0125': 'gpt-3.5-turbo',
};
const ANTHROPIC_MODEL_MAPPING = {
    // Claude Opus 4
    'claude-opus-4-1-20250805': 'claude-opus-4-1-20250805',
    'claude-opus-4-20250514': 'claude-opus-4-20250514',
    // Claude Sonnet 4
    'claude-sonnet-4-5-20250929': 'claude-sonnet-4-5-20250929',
    'claude-sonnet-4-20250514': 'claude-sonnet-4-20250514',
    // Claude Haiku 4
    'claude-haiku-4-5-20251001': 'claude-haiku-4-5-20251001',
    // Claude 3.7 Sonnet
    'claude-3-7-sonnet-20250219': 'claude-3-7-sonnet-20250219',
    // Claude 3.5 Haiku
    'claude-3-5-haiku-20241022': 'claude-3-5-haiku-20241022',
    // Claude 3 Haiku
    'claude-3-haiku-20240307': 'claude-3-haiku-20240307',
};
const GITHUB_COPILOT_MODEL_MAPPING = {
    // OpenAI models in Copilot
    'gpt-4.1': 'gpt-4o',
    'gpt-5-mini': 'gpt-4o-mini',
    'gpt-5.1': 'gpt-4o',
    'gpt-5.1-codex': 'gpt-4o',
    'gpt-5.1-codex-mini': 'gpt-4o-mini',
    'gpt-5.1-codex-max': 'gpt-4o',
    'gpt-5.2': 'gpt-4o',
    'gpt-5.2-codex': 'gpt-4o',
    // Anthropic models in Copilot
    'claude-haiku-4.5': 'claude-haiku-4-5-20251001',
    'claude-sonnet-4': 'claude-sonnet-4-20250514',
    'claude-sonnet-4.5': 'claude-sonnet-4-5-20250929',
    'claude-opus-4.5': 'claude-opus-4-20250514',
    // Google models in Copilot
    'gemini-3-flash': 'gemini-2.0-flash-001',
    'gemini-3-pro': 'gemini-2.5-pro',
    // xAI models in Copilot
    'grok-code-fast-1': 'grok-4',
};
/**
 * Map NotDiamond-selected model back to OpenCode-compatible model
 *
 * NOTE: Currently disabled - pass through as-is since candidates already
 * contain valid OpenCode model names. The mapping tables above are preserved
 * for future use if NotDiamond returns different model identifiers.
 */
export function mapToOpenCodeModel(provider, model) {
    return model;
}
/**
 * Map LLMProvider to OpenCode-compatible model
 */
export function mapProviderToOpenCode(provider) {
    return {
        provider: provider.provider,
        model: mapToOpenCodeModel(provider.provider, provider.model),
    };
}
