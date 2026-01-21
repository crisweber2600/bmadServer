import type { QuotaResult, QuotaProvider, LLMProvider } from './types.js';
export declare class CopilotQuotaProvider implements QuotaProvider {
    readonly name = "github-copilot";
    checkQuota(): Promise<QuotaResult | null>;
}
export declare function filterCandidatesByQuota(candidates: LLMProvider[], minPercent?: number): Promise<LLMProvider[]>;
export declare function getCopilotQuota(): Promise<QuotaResult | null>;
