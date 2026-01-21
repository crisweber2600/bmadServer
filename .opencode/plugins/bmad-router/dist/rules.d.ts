import type { BmadPhase, LLMProvider } from './types.js';
export declare function isDevPhase(phase: BmadPhase): boolean;
export declare function isCopilotModel(provider: string, model: string): boolean;
export declare function isCopilotProvider(provider: string): boolean;
export declare function filterCandidatesByPhase(candidates: LLMProvider[], phase: BmadPhase): LLMProvider[];
