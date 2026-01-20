import type { BmadPhase, LLMProvider } from './types';

const DEV_PHASES: Set<BmadPhase> = new Set([
  'quick-dev',
  'code-review',
  'in-progress',
  'review',
]);

const COPILOT_MODEL_PATTERNS = [
  'gpt-5.2-codex',
  'gpt-5.1-codex-max',
  'gpt-5.2',
  'claude-sonnet-4',
];

export function isDevPhase(phase: BmadPhase): boolean {
  return DEV_PHASES.has(phase);
}

export function isCopilotModel(provider: string, model: string): boolean {
  if (provider !== 'github-copilot' && provider !== 'copilot') {
    return false;
  }
  return true;
}

export function isCopilotProvider(provider: string): boolean {
  return provider === 'github-copilot' || provider === 'copilot';
}

export function filterCandidatesByPhase(
  candidates: LLMProvider[],
  phase: BmadPhase
): LLMProvider[] {
  if (isDevPhase(phase)) {
    return candidates;
  }
  return candidates.filter(c => !isCopilotProvider(c.provider));
}
