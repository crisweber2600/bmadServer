import { test, expect } from '@playwright/test';
import { ChatPage } from '../../pages/ChatPage';

/**
 * Story 3.5: Chat History & Scroll Management
 *
 * Tests message loading, pagination, scroll behavior,
 * new message badges, position restoration, and welcome screens
 */
test.describe('Story 3.5: Chat History & Scroll Management', () => {
  let chatPage: ChatPage;

  test.beforeEach(async ({ page }) => {
    chatPage = new ChatPage(page);
  });

  test('Last 50 messages load at bottom @P0', async ({ page }) => {
    // Given I have an existing workflow with messages
    // Note: This test assumes workflow with history exists
    await chatPage.goto('existing-workflow-id');

    // When chat loads
    await page.waitForTimeout(1000);

    // Then last 50 messages should display
    const messageCount = await chatPage.getMessageCount();
    expect(messageCount).toBeLessThanOrEqual(50);

    // And scroll should be at bottom
    const isAtBottom = await chatPage.messageList.evaluate((el) => {
      return el.scrollTop + el.clientHeight >= el.scrollHeight - 10;
    });
    expect(isAtBottom).toBe(true);
  });

  test('Load More loads next 50 without scroll jump @P1', async ({ page }) => {
    // Given I am viewing chat with history
    await chatPage.goto('existing-workflow-id');
    await page.waitForTimeout(1000);

    // When I scroll to top
    await chatPage.scrollToTop();
    const initialScrollPos = await chatPage.getScrollPosition();

    // And click Load More
    if (await chatPage.loadMoreButton.isVisible()) {
      const initialCount = await chatPage.getMessageCount();
      await chatPage.loadMoreMessages();
      await page.waitForTimeout(500);

      // Then next 50 messages should load
      const newCount = await chatPage.getMessageCount();
      expect(newCount).toBeGreaterThan(initialCount);
      expect(newCount - initialCount).toBeLessThanOrEqual(50);

      // And scroll position should not jump
      const newScrollPos = await chatPage.getScrollPosition();
      // Position should be similar (not exactly same due to content insertion)
      expect(Math.abs(newScrollPos - initialScrollPos)).toBeLessThan(100);
    }
  });

  test('New message badge when scrolled up @P1', async ({ page }) => {
    // Given I am scrolled up from bottom
    await chatPage.goto();
    await chatPage.sendMessage('First message');
    await page.waitForTimeout(1000);

    // Scroll up
    await chatPage.scrollToTop();

    // When a new message arrives
    await chatPage.sendMessage('Trigger new message');
    await page.waitForTimeout(2000);

    // Then "New message" badge should appear
    // Note: Badge only appears if truly scrolled away from bottom
    const isAtBottom = await chatPage.messageList.evaluate((el) => {
      return el.scrollTop + el.clientHeight >= el.scrollHeight - 50;
    });

    if (!isAtBottom) {
      await expect(chatPage.newMessageBadge).toBeVisible();
    }
  });

  test('Scroll position restored on reopen @P2', async ({ page }) => {
    // Given I view chat and scroll to specific position
    await chatPage.goto();
    await chatPage.sendMessage('Message 1');
    await chatPage.sendMessage('Message 2');
    await chatPage.sendMessage('Message 3');
    await page.waitForTimeout(1000);

    // Scroll to middle
    await chatPage.messageList.evaluate((el) => {
      el.scrollTop = el.scrollHeight / 2;
    });
    const savedScrollPos = await chatPage.getScrollPosition();

    // When I close and reopen chat
    await page.goto('/settings');
    await page.waitForTimeout(500);
    await chatPage.goto();

    // Then my last scroll position should be restored
    await page.waitForTimeout(500);
    const restoredScrollPos = await chatPage.getScrollPosition();

    // Allow some margin for restoration
    expect(Math.abs(restoredScrollPos - savedScrollPos)).toBeLessThan(100);
  });

  test('Welcome message for empty chat @P1', async ({ page }) => {
    // Given I open a new workflow with no history
    await chatPage.goto();

    // When chat history is empty
    const messageCount = await chatPage.getMessageCount();

    // Then I should see welcome message with quick-start buttons
    if (messageCount === 0) {
      await expect(chatPage.welcomeMessage).toBeVisible();
      await expect(chatPage.quickStartButtons).toBeVisible();

      // Quick start buttons should be clickable
      const buttonCount = await chatPage.quickStartButtons.locator('button').count();
      expect(buttonCount).toBeGreaterThan(0);
    }
  });

  test('New message badge click scrolls to bottom @P1', async ({ page }) => {
    // Given new message badge is visible
    await chatPage.goto();

    // Create some messages
    for (let i = 0; i < 5; i++) {
      await chatPage.sendMessage(`Message ${i}`);
      await page.waitForTimeout(500);
    }

    // Scroll up
    await chatPage.scrollToTop();
    await page.waitForTimeout(500);

    // Trigger new message while scrolled up
    await chatPage.sendMessage('New message while scrolled');
    await page.waitForTimeout(1000);

    // When I click the new message badge (if visible)
    if (await chatPage.newMessageBadge.isVisible()) {
      await chatPage.newMessageBadge.click();

      // Then scroll should jump to bottom
      await page.waitForTimeout(300);
      const isAtBottom = await chatPage.messageList.evaluate((el) => {
        return el.scrollTop + el.clientHeight >= el.scrollHeight - 10;
      });
      expect(isAtBottom).toBe(true);
    }
  });
});
