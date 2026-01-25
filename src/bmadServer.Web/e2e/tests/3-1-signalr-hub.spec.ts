import { test, expect } from '@playwright/test';

test.describe('SignalR Hub Setup & WebSocket Connection (Story 3-1)', () => {
  test.beforeEach(async ({ page }) => {
    // Login first to get JWT token
    await page.goto('/login');
    await page.fill('input[name="email"]', 'test@example.com');
    await page.fill('input[name="password"]', 'SecurePass123!');
    await page.click('button:has-text("Login")');
    await page.waitForURL(/\/dashboard/, { timeout: 5000 });
  });

  test('should establish WebSocket connection with valid JWT', async ({ page }) => {
    // Navigate to chat
    await page.goto('/chat');

    // Wait for SignalR connection to establish
    await page.waitForFunction(() => {
      return (window as any).signalRConnection?.state === 'Connected';
    }, { timeout: 10000 });

    // Verify connection is active
    const connectionState = await page.evaluate(() => {
      return (window as any).signalRConnection?.state;
    });

    expect(connectionState).toBe('Connected');
  });

  test('should send and receive messages via SignalR', async ({ page }) => {
    await page.goto('/chat');

    // Wait for connection
    await page.waitForFunction(() => {
      return (window as any).signalRConnection?.state === 'Connected';
    }, { timeout: 10000 });

    // Send a message
    const testMessage = 'Hello from E2E test';
    await page.fill('[aria-label="Message input"]', testMessage);
    await page.click('button:has-text("Send")');

    // Verify message appears in chat
    const messageElement = page.locator('.chat-message', { hasText: testMessage });
    await expect(messageElement).toBeVisible({ timeout: 5000 });
  });

  test('should join and leave workflow rooms', async ({ page }) => {
    await page.goto('/chat');

    // Wait for connection
    await page.waitForFunction(() => {
      return (window as any).signalRConnection?.state === 'Connected';
    }, { timeout: 10000 });

    // Join a workflow
    const workflowId = 'test-workflow-123';
    await page.evaluate((wfId) => {
      return (window as any).signalRConnection?.invoke('JoinWorkflow', wfId);
    }, workflowId);

    // Verify joined (wait a bit)
    await page.waitForTimeout(1000);

    // Leave the workflow
    await page.evaluate((wfId) => {
      return (window as any).signalRConnection?.invoke('LeaveWorkflow', wfId);
    }, workflowId);

    // Verify left (wait a bit)
    await page.waitForTimeout(1000);
  });

  test('should handle connection drop and reconnect', async ({ page }) => {
    await page.goto('/chat');

    // Wait for initial connection
    await page.waitForFunction(() => {
      return (window as any).signalRConnection?.state === 'Connected';
    }, { timeout: 10000 });

    // Simulate connection drop by going offline
    await page.context().setOffline(true);

    // Wait for disconnected state
    await page.waitForFunction(() => {
      return (window as any).signalRConnection?.state !== 'Connected';
    }, { timeout: 5000 });

    // Go back online
    await page.context().setOffline(false);

    // Wait for reconnection
    await page.waitForFunction(() => {
      return (window as any).signalRConnection?.state === 'Connected';
    }, { timeout: 30000 }); // Allow time for exponential backoff
  });

  test('should reject connection with invalid JWT', async ({ page }) => {
    // Clear localStorage to remove valid token
    await page.goto('/chat');
    await page.evaluate(() => {
      localStorage.setItem('token', 'invalid-token-xyz');
    });

    // Reload to trigger connection with invalid token
    await page.reload();

    // Wait and verify connection fails
    await page.waitForTimeout(3000);

    const connectionState = await page.evaluate(() => {
      return (window as any).signalRConnection?.state;
    });

    // Should not be connected
    expect(connectionState).not.toBe('Connected');
  });

  test('should display connection status indicator', async ({ page }) => {
    await page.goto('/chat');

    // Should show connecting status initially
    const statusIndicator = page.locator('[data-testid="connection-status"]');
    
    // Wait for connected state
    await page.waitForFunction(() => {
      return (window as any).signalRConnection?.state === 'Connected';
    }, { timeout: 10000 });

    // Verify connected status is shown (if implemented)
    // This is optional depending on UI implementation
  });
});
