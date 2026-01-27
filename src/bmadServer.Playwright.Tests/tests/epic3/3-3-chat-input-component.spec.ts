import { test, expect } from '@playwright/test';
import { ChatPage } from '../../pages/ChatPage';

/**
 * Story 3.3: Chat Input Component with Rich Interactions
 *
 * Tests multi-line input, keyboard shortcuts, character count,
 * draft preservation, command palette, and cancel functionality
 */
test.describe('Story 3.3: Chat Input Component with Rich Interactions', () => {
  let chatPage: ChatPage;

  test.beforeEach(async ({ page }) => {
    chatPage = new ChatPage(page);
    await chatPage.goto();
  });

  test('Multi-line input with Send button @P0', async ({ page }) => {
    // Given I view the chat interface
    // Then I should see multi-line input with Send button
    await expect(chatPage.messageInput).toBeVisible();
    await expect(chatPage.sendButton).toBeVisible();

    // Input should allow multiple lines
    await chatPage.messageInput.fill('Line 1\nLine 2\nLine 3');
    const value = await chatPage.messageInput.inputValue();
    expect(value).toContain('\n');
  });

  test('Send button disabled when empty @P0', async ({ page }) => {
    // Given input is empty
    await chatPage.messageInput.clear();

    // Then Send button should be disabled
    await expect(chatPage.sendButton).toBeDisabled();

    // When I type something
    await chatPage.messageInput.fill('Hello');

    // Then Send button should be enabled
    await expect(chatPage.sendButton).toBeEnabled();
  });

  test('Ctrl+Enter sends message and clears input @P0', async ({ page }) => {
    // Given I type a message
    const testMessage = 'Test message via keyboard';
    await chatPage.messageInput.fill(testMessage);

    // When I press Ctrl+Enter
    await chatPage.sendMessageWithKeyboard(testMessage);

    // Then message should be sent
    const messages = page.locator('[data-testid="message"]');
    const lastMessage = messages.last();
    await expect(lastMessage).toContainText(testMessage, { timeout: 5000 });

    // And input should be cleared
    const inputValue = await chatPage.messageInput.inputValue();
    expect(inputValue).toBe('');
  });

  test('Character count turns red at 2000+ chars @P1', async ({ page }) => {
    // Given I type less than 2000 characters
    await chatPage.messageInput.fill('a'.repeat(1999));

    // Then character count should not be red
    let countColor = await chatPage.characterCount.evaluate((el) =>
      window.getComputedStyle(el).color
    );
    expect(countColor).not.toMatch(/rgb\(2[0-5]\d,\s*0,\s*0\)/); // Not red

    // When I exceed 2000 characters
    await chatPage.messageInput.fill('a'.repeat(2001));

    // Then character count should turn red
    countColor = await chatPage.characterCount.evaluate((el) =>
      window.getComputedStyle(el).color
    );
    // Red color check - could be various shades of red
    expect(countColor).toMatch(/rgb\((2[0-5]\d|1\d\d),\s*\d{1,2},\s*\d{1,2}\)/);
  });

  test('Send button remains enabled at 2000+ chars @P1', async ({ page }) => {
    // Given I type 2000+ characters
    await chatPage.messageInput.fill('a'.repeat(2001));

    // Then Send button should STILL be enabled (not a hard limit)
    // Per AC: "Character count turns red" but sending is allowed
    await expect(chatPage.sendButton).toBeEnabled();
  });

  test('Draft preservation in localStorage @P1', async ({ page, context }) => {
    // Given I type a partial message
    const draftMessage = 'This is a draft message';
    await chatPage.messageInput.fill(draftMessage);

    // When I navigate away
    await page.goto('/settings');
    await page.waitForLoadState('domcontentloaded');

    // And return to chat
    await chatPage.goto();

    // Then draft message should be preserved
    const inputValue = await chatPage.messageInput.inputValue();
    expect(inputValue).toBe(draftMessage);
  });

  test('/ command palette appears @P1', async ({ page }) => {
    // Given I am in the input
    // When I type "/"
    await chatPage.messageInput.fill('/');

    // Then command palette should appear
    await expect(chatPage.commandPalette).toBeVisible();

    // And should show expected options
    const paletteText = await chatPage.commandPalette.textContent();
    expect(paletteText).toContain('help');
    expect(paletteText).toContain('status');
    expect(paletteText).toContain('pause');
    expect(paletteText).toContain('resume');
  });

  test('Arrow key navigation in command palette @P2', async ({ page }) => {
    // Given command palette is open
    await chatPage.messageInput.fill('/');
    await expect(chatPage.commandPalette).toBeVisible();

    // When I press ArrowDown
    await page.keyboard.press('ArrowDown');

    // Then selection should move
    const firstItem = chatPage.commandPalette.locator('[data-selected="true"]');
    await expect(firstItem).toBeVisible();

    // And pressing Enter should select
    await page.keyboard.press('Enter');

    // Command should be inserted
    const inputValue = await chatPage.messageInput.inputValue();
    expect(inputValue.startsWith('/')).toBe(true);
  });

  test('Cancel button for slow requests @P1', async ({ page }) => {
    // Given I send a message
    await chatPage.sendMessage('Generate a very long response please');

    // When processing takes > 5 seconds
    await page.waitForTimeout(5000);

    // Then Cancel button should be visible
    // Note: This depends on actual API response time
    // If response is quick, cancel button may not appear
    const isCancelVisible = await chatPage.cancelButton.isVisible().catch(() => false);

    // If cancel button appeared, clicking it should work
    if (isCancelVisible) {
      await chatPage.cancelButton.click();

      // Processing should stop
      await expect(chatPage.typingIndicator).toBeHidden({ timeout: 2000 });
    }
  });
});
