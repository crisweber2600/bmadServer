import type { Plugin } from '@opencode-ai/plugin';
declare global {
    var __bmadRouterUpdateRateLimit: ((headers: Headers, provider: string) => void) | undefined;
}
export declare const BmadRouterPlugin: Plugin;
export default BmadRouterPlugin;
