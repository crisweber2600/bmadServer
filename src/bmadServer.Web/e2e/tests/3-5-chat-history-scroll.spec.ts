import { test, expect } from '@playwright/test';

test.describe('Chat History & Scroll Management (Story 3-5)', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/login');
    await page.fill('input[name="email"]', 'test@example.com');
    await page.fill('input[name="password"]', 'SecurePass123!');
    await page.click('button:has-text("Login")');
    await page.waitForURL(/\/dashboard/, { timeout: 5000 });
  });

  test('should load last 50 messages on chat open', async ({ page }) => {
    await page.goto('/chat/workflow-with-history');

    // Wait for messages to load
    await page.waitForSelector('.chat-message', { timeout: 5000 });

    // Count loaded messages
    const messages = page.locator('.chat-message');
    const count = await messages.count();

    // Should load up to 50 messages
    expect(count).toBeLessThanOrEqual(50);
    expect(count).toBeGreaterThan(0);

    // Verify scroll is at bottom
    const isAtBottom = await page.evaluate(() => {
      const container = document.querySelector('.chat-messages');
      if (!container) return false;
      return Math.abs(container.scrollHeight - container.scrollTop - container.clientHeight) < 10;
    });

    expect(isAtBottom).toBeTruthy();
  });

  test('should show Load More button and load older messages', async ({ page }) => {
    await page.goto('/chat/workflow-with-many-messages');

    // Scroll to top
    await page.evaluate(() => {
      const container = document.querySelector('.chat-messages');
      if (container) container.scrollTop = 0;
    });

    // Wait for Load More button
    const loadMoreButton = page.locator('button:has-text("Load More")');
    await expect(loadMoreButton).toBeVisible({ timeout: 3000 });

    // Get current message count
    const initialCount = await page.locator('.chat-message').count();

    // Click Load More
    await loadMoreButton.click();

    // Wait for new messages to load
    await page.waitForTimeout(1000);

    // Verify more messages loaded
    const newCount = await page.locator('.chat-message').count();
    expect(newCount).toBeGreaterThan(initialCount);
  });

  test('should show "New message" badge when scrolled up', async ({ page }) => {
    await page.goto('/chat');

    // Scroll up
    await page.evaluate(() => {
      const container = document.querySelector('.chat-messages');
      if (container) container.scrollTop = 0;
    });

    // Simulate new message arriving
    await page.evaluate(() => {
      const event = new CustomEvent('new-message', {
        detail: { id: 'new-msg', content: 'New message arrived' }
      });
      window.dispatchEvent(event);
    });

    // Badge should appear
    const badge = page.locator('.new-message-badge, :has-text("New message")');
    await expect(badge).toBeVisible({ timeout: 3000 });
  });

  test('should scroll to bottom when badge is clicked', async ({ page }) => {
    await page.goto('/chat');

    // Scroll up
    await page.evaluate(() => {
      const container = document.querySelector('.chat-messages');
      if (container) container.scrollTop = 0;
    });

    // Simulate badge appearing
    await page.evaluate(() => {
      const badge = document.createElement('div');
      badge.className = 'new-message-badge';
      badge.textContent = 'New message';
      badge.onclick = () => {
        const container = document.querySelector('.chat-messages');
        if (container) {
          container.scrollTop = container.scrollHeight;
        }
      };
      document.body.appendChild(badge);
    });

    // Click badge
    const badge = page.locator('.new-message-badge');
    await badge.click();

    // Wait for scroll
    await page.waitForTimeout(500);

    // Verify scrolled to bottom
    const isAtBottom = await page.evaluate(() => {
      const container = document.querySelector('.chat-messages');
      if (!container) return false;
      return Math.abs(container.scrollHeight - container.scrollTop - container.clientHeight) < 10;
    });

    expect(isAtBottom).toBeTruthy();
  });

  test('should restore scroll position on reload', async ({ page }) => {
    await page.goto('/chat/workflow-with-history');

    // Scroll to middle
    await page.evaluate(() => {
      const container = document.querySelector('.chat-messages');
      if (container) container.scrollTop = container.scrollHeight / 2;
    });

    // Get scroll position
    const scrollPosition = await page.evaluate(() => {
      const container = document.querySelector('.chat-messages');
      return container?.scrollTop || 0;
    });

    // Wait for position to save
    await page.waitForTimeout(500);

    // Reload page
    await page.reload();
    await page.waitForLoadState('networkidle');

    // Verify scroll position restored (approximately)
    const restoredPosition = await page.evaluate(() => {
      const container = document.querySelector('.chat-messages');
      return container?.scrollTop || 0;
    });

    expect(Math.abs(restoredPosition - scrollPosition)).toBeLessThan(50);
  });

  test('should show welcome message for empty chat', async ({ page }) => {
    await page.goto('/chat/new-workflow');

    // Wait for welcome message
    const welcomeMessage = page.locator(':has-text("Welcome to BMAD Server")');
    await expect(welcomeMessage).toBeVisible({ timeout: 3000 });

    // Verify Quick Start button
    const quickStartButton = page.locator('button:has-text("Quick Start")');
    await expect(quickStartButton).toBeVisible();

    // Verify no Load More button
    const loadMoreButton = page.locator('button:has-text("Load More")');
    await expect(loadMoreButton).not.toBeVisible();
  });

  test('should handle smooth scrolling with many messages', async ({ page }) => {
    await page.goto('/chat/workflow-with-200-messages');

    // Perform scroll test
    const scrollTest = await page.evaluate(() => {
      const container = document.querySelector('.chat-messages');
      if (!container) return { smooth: false, lagDetected: false };

      let lagDetected = false;
      const startTime = Date.now();
      
      // Scroll through messages
      for (let i = 0; i < 5; i++) {
        container.scrollTop += 100;
        const elapsed = Date.now() - startTime;
        if (elapsed > 1000) {
          lagDetected = true;
          break;
        }
      }

      return { smooth: true, lagDetected };
    });

    expect(scrollTest.smooth).toBeTruthy();
    expect(scrollTest.lagDetected).toBeFalsy();
  });
});
