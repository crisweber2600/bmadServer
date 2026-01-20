import { readFile } from 'fs/promises';
import { join } from 'path';
import { homedir } from 'os';
import type { 
  AuthData, 
  QuotaResult, 
  QuotaProvider, 
  LLMProvider,
  CopilotTokenResponse,
  CopilotQuotaResponse 
} from './types';
import { isCopilotProvider } from './rules';

const AUTH_FILE_PATH = join(homedir(), '.local/share/opencode/auth.json');
const COPILOT_TOKEN_URL = 'https://api.github.com/copilot_internal/v2/token';
const COPILOT_MODELS_URL = 'https://api.individual.githubcopilot.com/models';
const DEFAULT_TIMEOUT_MS = 5000;
const DEFAULT_MIN_QUOTA_PERCENT = 10;

async function fetchWithTimeout(
  url: string, 
  options: RequestInit, 
  timeoutMs: number = DEFAULT_TIMEOUT_MS
): Promise<Response> {
  const controller = new AbortController();
  const timeoutId = setTimeout(() => controller.abort(), timeoutMs);
  
  try {
    const response = await fetch(url, {
      ...options,
      signal: controller.signal,
    });
    return response;
  } finally {
    clearTimeout(timeoutId);
  }
}

async function loadAuthData(): Promise<AuthData | null> {
  try {
    const content = await readFile(AUTH_FILE_PATH, 'utf-8');
    return JSON.parse(content) as AuthData;
  } catch {
    return null;
  }
}

async function exchangeForCopilotToken(oauthToken: string): Promise<string | null> {
  try {
    const response = await fetchWithTimeout(COPILOT_TOKEN_URL, {
      method: 'GET',
      headers: {
        'Authorization': `token ${oauthToken}`,
        'Accept': 'application/json',
      },
    });
    
    if (!response.ok) return null;
    
    const data = await response.json() as CopilotTokenResponse;
    return data.token;
  } catch {
    return null;
  }
}

async function fetchCopilotQuota(copilotToken: string): Promise<CopilotQuotaResponse | null> {
  try {
    const response = await fetchWithTimeout(COPILOT_MODELS_URL, {
      method: 'GET',
      headers: {
        'Authorization': `Bearer ${copilotToken}`,
        'Accept': 'application/json',
        'Copilot-Integration-Id': 'vscode-chat',
      },
    });
    
    if (!response.ok) return null;
    
    return await response.json() as CopilotQuotaResponse;
  } catch {
    return null;
  }
}

export class CopilotQuotaProvider implements QuotaProvider {
  readonly name = 'github-copilot';

  async checkQuota(): Promise<QuotaResult | null> {
    const authData = await loadAuthData();
    const copilotAuth = authData?.['github-copilot'];
    
    if (!copilotAuth || copilotAuth.type !== 'oauth') {
      return null;
    }

    const copilotToken = await exchangeForCopilotToken(copilotAuth.access);
    if (!copilotToken) {
      return null;
    }

    const quotaData = await fetchCopilotQuota(copilotToken);
    if (!quotaData?.copilot_ide_chat?.chat_quota) {
      return null;
    }

    const quota = quotaData.copilot_ide_chat.chat_quota;
    
    if (quota.unlimited) {
      return {
        provider: this.name,
        percentRemaining: 100,
        unlimited: true,
      };
    }

    const limit = quota.premium_requests_limit ?? 0;
    const remaining = quota.premium_requests_remaining ?? 0;
    
    if (limit === 0) {
      return {
        provider: this.name,
        percentRemaining: 0,
      };
    }

    return {
      provider: this.name,
      percentRemaining: (remaining / limit) * 100,
    };
  }
}

export async function filterCandidatesByQuota(
  candidates: LLMProvider[],
  minPercent: number = DEFAULT_MIN_QUOTA_PERCENT
): Promise<LLMProvider[]> {
  const copilotCandidates = candidates.filter(c => isCopilotProvider(c.provider));
  
  if (copilotCandidates.length === 0) {
    return candidates;
  }

  const quotaProvider = new CopilotQuotaProvider();
  const quota = await quotaProvider.checkQuota();
  
  if (quota === null) {
    return candidates;
  }

  if (quota.unlimited || quota.percentRemaining >= minPercent) {
    return candidates;
  }

  return candidates.filter(c => !isCopilotProvider(c.provider));
}

export async function getCopilotQuota(): Promise<QuotaResult | null> {
  const provider = new CopilotQuotaProvider();
  return provider.checkQuota();
}
