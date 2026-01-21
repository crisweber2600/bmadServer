import type { LLMProvider, NotDiamondTradeoff } from './types.js';
export declare class NotDiamondRouter {
    private client;
    private timeoutMs;
    constructor(apiKey?: string, timeoutMs?: number);
    selectModel(messageContext: string, candidates: LLMProvider[], tradeoff?: NotDiamondTradeoff): Promise<LLMProvider>;
}
export declare function routeModel(candidates: LLMProvider[], parts: Array<{
    type: string;
    text?: string;
}>, tradeoff?: NotDiamondTradeoff): Promise<LLMProvider>;
