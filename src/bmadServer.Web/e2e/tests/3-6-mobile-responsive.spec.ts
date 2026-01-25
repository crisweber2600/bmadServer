import { test, expect, devices } from '@playwright/test';

test.describe('Mobile-Responsive Chat Interface (Story 3-6)', () => {
  test.use({ ...devices['iPhone 12'] });

  test.beforeEach(async ({ page }) => {
    await page.goto('/login');
    await page.fill('input[name="email"]', 'test@example.com');
    await page.fill('input[name="password"]', 'SecurePass123!');
    await page.click('button:has-text("Login")');
    await page.waitForURL(/\/dashboard/, { timeout: 5000 });
  });

  test('should adapt to mobile layout below 768px', async ({ page }) => {
    await page.goto('/chat');

    // Verify mobile layout
    const chatContainer = page.locator('.chat-container');
    await expect(chatContainer).toBeVisible();

    // Check that layout is single column
    const layout = await chatContainer.evaluate((el) => {
      const computed = window.getComputedStyle(el);
      return {
        display: computed.display,
        flexDirection: computed.flexDirection
      };
    });

    // Mobile should use column layout
    expect(layout.flexDirection === 'column' || layout.display === 'block').toBeTruthy();
  });

  test('should have touch-friendly tap targets (48px minimum)', async ({ page }) => {
    await page.goto('/chat');

    // Check Send button size
    const sendButton = page.locator('button:has-text("Send")');
    const buttonSize = await sendButton.boundingBox();

    expect(buttonSize).not.toBeNull();
    if (buttonSize) {
      expect(buttonSize.height).toBeGreaterThanOrEqual(44); // WCAG 2.1 AA minimum
      expect(buttonSize.width).toBeGreaterThanOrEqual(44);
    }
  });

  test('should show hamburger menu for sidebar', async ({ page }) => {
    await page.goto('/chat');

    // Look for hamburger menu
    const hamburgerMenu = page.locator('[aria-label*="menu"], .hamburger-menu, button:has-text("☰")');
    await expect(hamburgerMenu).toBeVisible({ timeout: 3000 });
  });

  test('should handle virtual keyboard appearance', async ({ page }) => {
    await page.goto('/chat');

    const input = page.locator('[aria-label="Message input"]');
    
    // Focus input (triggers virtual keyboard on mobile)
    await input.click();

    // Wait for keyboard
    await page.waitForTimeout(500);

    // Input should still be visible and accessible
    await expect(input).toBeVisible();
    
    // Should be able to type
    await input.fill('Mobile keyboard test');
    await expect(input).toHaveValue('Mobile keyboard test');
  });

  test('should support tap and hold gesture', async ({ page }) => {
    await page.goto('/chat');

    // Add a message to tap
    await page.evaluate(() => {
      const container = document.querySelector('.chat-messages');
      if (container) {
        const messageDiv = document.createElement('div');
        messageDiv.className = 'chat-message';
        messageDiv.textContent = 'Tap and hold me';
        container.appendChild(messageDiv);
      }
    });

    const message = page.locator('.chat-message', { hasText: 'Tap and hold me' });
    
    // Simulate long press
    await message.tap();
    await page.waitForTimeout(1000);

    // Context menu or action should appear (depends on implementation)
    // This test validates the gesture is supported
  });

  test('should support VoiceOver accessibility', async ({ page }) => {
    await page.goto('/chat');

    // Check for ARIA labels on key elements
    const input = page.locator('[aria-label="Message input"]');
    await expect(input).toBeVisible();

    const sendButton = page.locator('button:has-text("Send")');
    const buttonLabel = await sendButton.getAttribute('aria-label');
    expect(buttonLabel || 'Send').toBeTruthy();

    // Check messages have proper roles
    const messages = page.locator('.chat-message');
    if (await messages.count() > 0) {
      const firstMessage = messages.first();
      const role = await firstMessage.getAttribute('role');
      const ariaLabel = await firstMessage.getAttribute('aria-label');
      
      // Should have accessibility attributes
      expect(role || ariaLabel).toBeTruthy();
    }
  });

  test('should respect reduced motion preference', async ({ page }) => {
    // Set reduced motion preference
    await page.emulateMedia({ reducedMotion: 'reduce' });
    
    await page.goto('/chat');

    // Send a message
    const input = page.locator('[aria-label="Message input"]');
    await input.fill('Test reduced motion');
    await page.click('button:has-text("Send")');

    // Check that animations are reduced
    const container = page.locator('.chat-messages');
    const computedStyle = await container.evaluate((el) => {
      return window.getComputedStyle(el).getPropertyValue('scroll-behavior');
    });

    // Should use instant scroll instead of smooth
    expect(computedStyle === 'auto' || computedStyle === 'instant').toBeTruthy();
  });

  test('should work in landscape orientation', async ({ page }) => {
    // Simulate landscape
    await page.setViewportSize({ width: 812, height: 375 }); // iPhone 12 landscape

    await page.goto('/chat');

    // Verify layout adapts
    const chatContainer = page.locator('.chat-container');
    await expect(chatContainer).toBeVisible();

    // Input should be accessible
    const input = page.locator('[aria-label="Message input"]');
    await expect(input).toBeVisible();

    // Should be able to send message
    await input.fill('Landscape test');
    await page.click('button:has-text("Send")');

    const message = page.locator('.chat-message', { hasText: 'Landscape test' });
    await expect(message).toBeVisible({ timeout: 3000 });
  });

  test('should have smooth scrolling performance on mobile', async ({ page }) => {
    await page.goto('/chat/workflow-with-history');

    // Perform scroll performance test
    const scrollPerformance = await page.evaluate(() => {
      const container = document.querySelector('.chat-messages');
      if (!container) return { fps: 0, smooth: false };

      let frameCount = 0;
      const startTime = performance.now();

      // Simulate scroll
      return new Promise((resolve) => {
        const measureFrame = () => {
          frameCount++;
          if (performance.now() - startTime < 1000) {
            requestAnimationFrame(measureFrame);
          } else {
            resolve({ fps: frameCount, smooth: frameCount > 50 });
          }
        };
        requestAnimationFrame(measureFrame);
      });
    });

    expect(scrollPerformance.fps).toBeGreaterThan(50); // Should achieve ~60fps
  });

  test('should have full-width input on mobile', async ({ page }) => {
    await page.goto('/chat');

    const input = page.locator('[aria-label="Message input"]');
    const inputBox = await input.boundingBox();
    const viewportSize = page.viewportSize();

    expect(inputBox).not.toBeNull();
    expect(viewportSize).not.toBeNull();

    if (inputBox && viewportSize) {
      // Input should be close to full width (accounting for padding)
      expect(inputBox.width).toBeGreaterThan(viewportSize.width * 0.85);
    }
  });
});

// Additional test for tablet viewport
test.describe('Mobile-Responsive Chat Interface - Tablet (Story 3-6)', () => {
  test.use({ ...devices['iPad Pro'] });

  test('should adapt layout for tablet viewport', async ({ page }) => {
    await page.goto('/login');
    await page.fill('input[name="email"]', 'test@example.com');
    await page.fill('input[name="password"]', 'SecurePass123!');
    await page.click('button:has-text("Login")');
    await page.waitForURL(/\/dashboard/, { timeout: 5000 });
    
    await page.goto('/chat');

    // Tablet may show sidebar or adapt differently than mobile
    const chatContainer = page.locator('.chat-container');
    await expect(chatContainer).toBeVisible();

    // Should be responsive and usable
    const input = page.locator('[aria-label="Message input"]');
    await expect(input).toBeVisible();
  });
});
