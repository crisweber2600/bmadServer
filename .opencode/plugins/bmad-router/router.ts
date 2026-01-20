import NotDiamond from 'notdiamond';
import type { LLMProvider, NotDiamondTradeoff } from './types';

const DEFAULT_TIMEOUT_MS = 5000;

function mapToNotDiamondFormat(candidates: LLMProvider[]): Array<{ provider: string; model: string }> {
  return candidates.map(c => ({
    provider: mapProviderName(c.provider),
    model: c.model,
  }));
}

function mapProviderName(provider: string): string {
  const mapping: Record<string, string> = {
    'github-copilot': 'openai',
    'copilot': 'openai',
  };
  return mapping[provider] ?? provider;
}

function extractMessageText(parts: Array<{ type: string; text?: string }>): string {
  return parts
    .filter(p => p.type === 'text' && p.text)
    .map(p => p.text!)
    .join('\n')
    .slice(0, 4000);
}

function getApiKey(): string | undefined {
  return process.env.NOT_DIAMOND_API_KEY ?? process.env.NOTDIAMOND_API_KEY;
}

export class NotDiamondRouter {
  private client: NotDiamond | null = null;
  private timeoutMs: number;

  constructor(apiKey?: string, timeoutMs: number = DEFAULT_TIMEOUT_MS) {
    this.timeoutMs = timeoutMs;
    const key = apiKey ?? getApiKey();
    if (key) {
      try {
        this.client = new NotDiamond({ apiKey: key });
      } catch {
        this.client = null;
      }
    }
  }

  async selectModel(
    messageContext: string,
    candidates: LLMProvider[],
    tradeoff: NotDiamondTradeoff = 'quality'
  ): Promise<LLMProvider> {
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
          const match = candidates.find(
            c => mapProviderName(c.provider) === selected.provider && c.model === selected.model
          );
          if (match) return match;
        }
      } catch (err) {
        clearTimeout(timeoutId);
        if ((err as Error).name === 'AbortError') {
          console.warn('[bmad-router] NotDiamond API timeout, using fallback');
        } else {
          console.warn('[bmad-router] NotDiamond API error:', err);
        }
      }
    } catch (err) {
      console.warn('[bmad-router] NotDiamond routing failed:', err);
    }

    return candidates[0];
  }
}

export async function routeModel(
  candidates: LLMProvider[],
  parts: Array<{ type: string; text?: string }>,
  tradeoff: NotDiamondTradeoff = 'quality'
): Promise<LLMProvider> {
  const router = new NotDiamondRouter();
  const messageText = extractMessageText(parts);
  return router.selectModel(messageText, candidates, tradeoff);
}
