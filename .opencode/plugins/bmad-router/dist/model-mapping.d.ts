import type { LLMProvider } from './types.js';
/**
 * Map NotDiamond-selected model back to OpenCode-compatible model
 *
 * NOTE: Currently disabled - pass through as-is since candidates already
 * contain valid OpenCode model names. The mapping tables above are preserved
 * for future use if NotDiamond returns different model identifiers.
 */
export declare function mapToOpenCodeModel(provider: string, model: string): string;
/**
 * Map LLMProvider to OpenCode-compatible model
 */
export declare function mapProviderToOpenCode(provider: LLMProvider): LLMProvider;
