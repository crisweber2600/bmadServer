import type { Plugin } from '@opencode-ai/plugin';
declare global {
    var __bmadRouterUpdateRateLimit: ((headers: Headers, provider: string) => void) | undefined;
    var __bmadRouterMarkRateLimited: ((provider: string, resetAt?: Date) => void) | undefined;
}
export declare const BmadRouterPlugin: Plugin;
export default BmadRouterPlugin;
