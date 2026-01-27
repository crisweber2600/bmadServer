import { Page } from '@playwright/test';

/**
 * SignalR helper utilities for testing real-time communication
 * Supports Epic 3.1 (connection), 3.4 (streaming) tests
 */
export interface SignalRMessage {
  type: string;
  messageId?: string;
  chunk?: string;
  isComplete?: boolean;
  agentId?: string;
  content?: string;
  timestamp?: string;
}

export class SignalRHelper {
  private page: Page;
  private messages: SignalRMessage[] = [];
  private connectionState: 'disconnected' | 'connecting' | 'connected' | 'reconnecting' = 'disconnected';

  constructor(page: Page) {
    this.page = page;
  }

  /**
   * Inject SignalR message interceptor into page
   * @throws Error if SignalR is not loaded after retries
   */
  async setupInterceptor(): Promise<void> {
    // First, ensure page is loaded enough for SignalR
    await this.page.waitForLoadState('domcontentloaded');
    
    await this.page.addInitScript(() => {
      // Flag to track if we've set up interceptors
      (window as any).__signalrInterceptorReady = false;
      (window as any).__signalrMessages = [];
      (window as any).__signalrConnectionState = 'disconnected';
      
      // Function to set up interceptors once SignalR is available
      const setupInterceptors = () => {
        const originalHubConnection = (window as any).signalR?.HubConnectionBuilder;
        if (!originalHubConnection) {
          return false;
        }

        const originalBuild = originalHubConnection.prototype.build;
        originalHubConnection.prototype.build = function () {
          const connection = originalBuild.call(this);

          // Intercept on method
          const originalOn = connection.on;
          connection.on = function (methodName: string, callback: Function) {
            return originalOn.call(this, methodName, (...args: any[]) => {
              (window as any).__signalrMessages.push({
                type: methodName,
                data: args,
                timestamp: new Date().toISOString(),
              });
              return callback(...args);
            });
          };

          // Track connection state
          const originalStart = connection.start;
          connection.start = function () {
            (window as any).__signalrConnectionState = 'connecting';
            return originalStart.call(this).then(() => {
              (window as any).__signalrConnectionState = 'connected';
            });
          };

          return connection;
        };
        
        (window as any).__signalrInterceptorReady = true;
        return true;
      };
      
      // Try to set up immediately
      if (!setupInterceptors()) {
        // If SignalR not ready, poll until it is
        const pollInterval = setInterval(() => {
          if (setupInterceptors()) {
            clearInterval(pollInterval);
          }
        }, 100);
        
        // Stop polling after 10 seconds
        setTimeout(() => clearInterval(pollInterval), 10000);
      }
    });
  }
  
  /**
   * Verify interceptor is ready, with retry logic
   */
  async ensureInterceptorReady(timeout = 5000): Promise<boolean> {
    const startTime = Date.now();
    while (Date.now() - startTime < timeout) {
      const ready = await this.page.evaluate(() => (window as any).__signalrInterceptorReady === true);
      if (ready) return true;
      await this.page.waitForTimeout(100);
    }
    console.warn('SignalR interceptor not ready - SignalR may not be used on this page');
    return false;
  }

  /**
   * Get all intercepted SignalR messages
   */
  async getMessages(): Promise<SignalRMessage[]> {
    return await this.page.evaluate(() => (window as any).__signalrMessages || []);
  }

  /**
   * Get messages of a specific type
   */
  async getMessagesByType(type: string): Promise<SignalRMessage[]> {
    const messages = await this.getMessages();
    return messages.filter((m) => m.type === type);
  }

  /**
   * Wait for a specific message type
   */
  async waitForMessage(type: string, timeout = 5000): Promise<SignalRMessage> {
    const startTime = Date.now();
    while (Date.now() - startTime < timeout) {
      const messages = await this.getMessagesByType(type);
      if (messages.length > 0) {
        return messages[messages.length - 1];
      }
      await this.page.waitForTimeout(100);
    }
    throw new Error(`Timeout waiting for SignalR message: ${type}`);
  }

  /**
   * Get current connection state
   */
  async getConnectionState(): Promise<string> {
    return await this.page.evaluate(() => (window as any).__signalrConnectionState || 'disconnected');
  }

  /**
   * Wait for connection to be established
   */
  async waitForConnection(timeout = 10000): Promise<void> {
    const startTime = Date.now();
    while (Date.now() - startTime < timeout) {
      const state = await this.getConnectionState();
      if (state === 'connected') return;
      await this.page.waitForTimeout(100);
    }
    throw new Error('Timeout waiting for SignalR connection');
  }

  /**
   * Simulate connection drop (for reconnection testing)
   */
  async simulateDisconnect(): Promise<void> {
    await this.page.evaluate(() => {
      // Trigger offline event
      window.dispatchEvent(new Event('offline'));
    });
  }

  /**
   * Simulate reconnection
   */
  async simulateReconnect(): Promise<void> {
    await this.page.evaluate(() => {
      window.dispatchEvent(new Event('online'));
    });
  }

  /**
   * Clear intercepted messages
   */
  async clearMessages(): Promise<void> {
    await this.page.evaluate(() => {
      (window as any).__signalrMessages = [];
    });
  }

  /**
   * Measure message latency
   */
  async measureLatency(sendAction: () => Promise<void>, responseType: string): Promise<number> {
    const startTime = Date.now();
    await sendAction();
    await this.waitForMessage(responseType);
    return Date.now() - startTime;
  }
}
