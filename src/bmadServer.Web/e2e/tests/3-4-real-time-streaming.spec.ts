import { test, expect } from '@playwright/test';

test.describe('Real-Time Message Streaming (Story 3-4)', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/login');
    await page.fill('input[name="email"]', 'test@example.com');
    await page.fill('input[name="password"]', 'SecurePass123!');
    await page.click('button:has-text("Login")');
    await page.waitForURL(/\/dashboard/, { timeout: 5000 });
    await page.goto('/chat');
  });

  test('should start streaming within 5 seconds (NFR2)', async ({ page }) => {
    const input = page.locator('[aria-label="Message input"]');
    
    // Record start time
    const startTime = Date.now();
    
    // Send message
    await input.fill('Tell me a story');
    await page.click('button:has-text("Send")');

    // Wait for first token/chunk to appear
    const agentMessage = page.locator('.chat-message.agent-message').last();
    await expect(agentMessage).toBeVisible({ timeout: 5000 });

    const elapsedTime = Date.now() - startTime;
    
    // Verify first token appeared within 5 seconds
    expect(elapsedTime).toBeLessThan(5000);
  });

  test('should stream tokens smoothly without flickering', async ({ page }) => {
    const input = page.locator('[aria-label="Message input"]');
    
    await input.fill('Stream test message');
    await page.click('button:has-text("Send")');

    // Wait for streaming to start
    const streamingMessage = page.locator('.chat-message.agent-message').last();
    await expect(streamingMessage).toBeVisible({ timeout: 5000 });

    // Monitor for flickering (content should only grow, not replace)
    let previousLength = 0;
    for (let i = 0; i < 5; i++) {
      await page.waitForTimeout(200);
      const currentText = await streamingMessage.textContent() || '';
      const currentLength = currentText.length;
      
      // Content should grow or stay same, never shrink (which would indicate flickering)
      expect(currentLength).toBeGreaterThanOrEqual(previousLength);
      previousLength = currentLength;
    }
  });

  test('should show typing indicator during streaming', async ({ page }) => {
    const input = page.locator('[aria-label="Message input"]');
    
    await input.fill('Show typing indicator');
    await page.click('button:has-text("Send")');

    // Typing indicator should appear
    const typingIndicator = page.locator('.typing-indicator');
    await expect(typingIndicator).toBeVisible({ timeout: 5000 });
  });

  test('should stop showing typing indicator when complete', async ({ page }) => {
    const input = page.locator('[aria-label="Message input"]');
    
    await input.fill('Complete message test');
    await page.click('button:has-text("Send")');

    // Wait for streaming to complete (mock or wait for actual completion)
    await page.waitForTimeout(3000);

    // Typing indicator should disappear
    const typingIndicator = page.locator('.typing-indicator');
    await expect(typingIndicator).not.toBeVisible();
  });

  test('should allow canceling streaming with Stop button', async ({ page }) => {
    const input = page.locator('[aria-label="Message input"]');
    
    await input.fill('Long streaming message');
    await page.click('button:has-text("Send")');

    // Wait for streaming to start
    await page.waitForTimeout(1000);

    // Click Stop Generating button
    const stopButton = page.locator('button:has-text("Stop")');
    await expect(stopButton).toBeVisible({ timeout: 3000 });
    await stopButton.click();

    // Verify streaming stopped
    const stoppedIndicator = page.locator(':has-text("(Stopped)")');
    await expect(stoppedIndicator).toBeVisible({ timeout: 2000 });

    // Input should be re-enabled
    await expect(input).toBeEnabled();
  });

  test('should preserve partial message on network interruption', async ({ page }) => {
    const input = page.locator('[aria-label="Message input"]');
    
    await input.fill('Interruption test');
    await page.click('button:has-text("Send")');

    // Wait for streaming to start
    await page.waitForTimeout(1000);

    // Get current partial message
    const message = page.locator('.chat-message.agent-message').last();
    const partialText = await message.textContent();

    // Simulate network interruption
    await page.context().setOffline(true);
    await page.waitForTimeout(500);
    await page.context().setOffline(false);

    // Verify partial message is still visible
    await expect(message).toContainText(partialText || '');
  });

  test('should handle multiple concurrent streams', async ({ page }) => {
    // Send two messages quickly
    const input = page.locator('[aria-label="Message input"]');
    
    await input.fill('First message');
    await page.click('button:has-text("Send")');
    
    await page.waitForTimeout(100);
    
    await input.fill('Second message');
    await page.click('button:has-text("Send")');

    // Verify both messages appear
    const messages = page.locator('.chat-message.agent-message');
    await expect(messages).toHaveCount(2, { timeout: 10000 });
  });
});
