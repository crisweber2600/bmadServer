const DEV_PHASES = new Set([
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
export function isDevPhase(phase) {
    return DEV_PHASES.has(phase);
}
export function isCopilotModel(provider, model) {
    if (provider !== 'github-copilot' && provider !== 'copilot') {
        return false;
    }
    return true;
}
export function isCopilotProvider(provider) {
    return provider === 'github-copilot' || provider === 'copilot';
}
export function filterCandidatesByPhase(candidates, phase) {
    // Be conservative: if phase can't be determined, treat as non-dev.
    // (Prevents unintended Copilot selection during planning/spec workflows.)
    if (isDevPhase(phase)) {
        return candidates;
    }
    return candidates.filter(c => !isCopilotProvider(c.provider));
}
