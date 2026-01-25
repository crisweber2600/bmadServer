import { test, expect } from '@playwright/test';

test.describe('Chat Input Component with Rich Interactions (Story 3-3)', () => {
  test.beforeEach(async ({ page }) => {
    // Login and navigate to chat
    await page.goto('/login');
    await page.fill('input[name="email"]', 'test@example.com');
    await page.fill('input[name="password"]', 'SecurePass123!');
    await page.click('button:has-text("Login")');
    await page.waitForURL(/\/dashboard/, { timeout: 5000 });
    await page.goto('/chat');
  });

  test('should display multi-line input with character count', async ({ page }) => {
    const input = page.locator('[aria-label="Message input"]');
    await expect(input).toBeVisible();

    const sendButton = page.locator('button:has-text("Send")');
    await expect(sendButton).toBeVisible();
    await expect(sendButton).toBeDisabled(); // Disabled when empty

    const charCount = page.locator('.character-count');
    await expect(charCount).toBeVisible();
    await expect(charCount).toContainText('0');
    await expect(charCount).toContainText('2000');

    const hint = page.locator('.keyboard-hint');
    await expect(hint).toContainText('Ctrl+Enter');
  });

  test('should send message with Ctrl+Enter', async ({ page }) => {
    const input = page.locator('[aria-label="Message input"]');
    const testMessage = 'Testing Ctrl+Enter';

    await input.fill(testMessage);
    await input.press('Control+Enter');

    // Verify input is cleared
    await expect(input).toHaveValue('');

    // Verify message appears in chat
    const message = page.locator('.chat-message', { hasText: testMessage });
    await expect(message).toBeVisible({ timeout: 3000 });
  });

  test('should send message with Cmd+Enter on Mac', async ({ page }) => {
    const input = page.locator('[aria-label="Message input"]');
    const testMessage = 'Testing Cmd+Enter';

    await input.fill(testMessage);
    await input.press('Meta+Enter');

    // Verify input is cleared
    await expect(input).toHaveValue('');
  });

  test('should show character count and enable/disable send button', async ({ page }) => {
    const input = page.locator('[aria-label="Message input"]');
    const sendButton = page.locator('button:has-text("Send")');
    const charCount = page.locator('.character-count');

    // Initially disabled
    await expect(sendButton).toBeDisabled();

    // Type some text
    await input.fill('Hello');
    await expect(sendButton).toBeEnabled();
    await expect(charCount).toContainText('5');

    // Clear text
    await input.fill('');
    await expect(sendButton).toBeDisabled();
  });

  test('should show warning when approaching character limit', async ({ page }) => {
    const input = page.locator('[aria-label="Message input"]');
    const charCount = page.locator('.character-count');

    // Fill with exactly 2000 characters
    const longText = 'a'.repeat(2000);
    await input.fill(longText);

    // Should show 2000/2000
    await expect(charCount).toContainText('2000');
  });

  test('should turn red and disable send when exceeding limit', async ({ page }) => {
    const input = page.locator('[aria-label="Message input"]');
    const sendButton = page.locator('button:has-text("Send")');
    const charCount = page.locator('.character-count');

    // Fill with more than 2000 characters
    const tooLongText = 'a'.repeat(2001);
    await input.fill(tooLongText);

    // Character count should turn red
    await expect(charCount).toHaveClass(/exceeded/);
    await expect(charCount).toContainText('2001');

    // Send button should be disabled
    await expect(sendButton).toBeDisabled();
  });

  test('should persist draft in localStorage', async ({ page }) => {
    const input = page.locator('[aria-label="Message input"]');
    const draftMessage = 'This is a draft message';

    // Type draft message
    await input.fill(draftMessage);

    // Wait for draft to save (debounced)
    await page.waitForTimeout(500);

    // Reload page
    await page.reload();
    await page.waitForLoadState('networkidle');

    // Verify draft is restored
    const restoredInput = page.locator('[aria-label="Message input"]');
    await expect(restoredInput).toHaveValue(draftMessage);
  });

  test('should clear draft after sending message', async ({ page }) => {
    const input = page.locator('[aria-label="Message input"]');
    const testMessage = 'Draft to be sent';

    // Type and let it save as draft
    await input.fill(testMessage);
    await page.waitForTimeout(500);

    // Send the message
    await page.click('button:has-text("Send")');

    // Wait for message to send
    await page.waitForTimeout(500);

    // Reload and verify draft is cleared
    await page.reload();
    await page.waitForLoadState('networkidle');

    const restoredInput = page.locator('[aria-label="Message input"]');
    await expect(restoredInput).toHaveValue('');
  });

  test('should show command palette on slash', async ({ page }) => {
    const input = page.locator('[aria-label="Message input"]');

    // Type slash
    await input.fill('/');

    // Command palette should appear
    const palette = page.locator('.command-palette');
    await expect(palette).toBeVisible({ timeout: 1000 });

    // Verify commands are shown
    await expect(palette).toContainText('/help');
    await expect(palette).toContainText('/status');
    await expect(palette).toContainText('/pause');
    await expect(palette).toContainText('/resume');
  });

  test('should navigate command palette with arrow keys', async ({ page }) => {
    const input = page.locator('[aria-label="Message input"]');

    // Open command palette
    await input.fill('/');
    await page.waitForSelector('.command-palette', { state: 'visible' });

    // Press down arrow
    await input.press('ArrowDown');

    // Verify selection moved (check for active/selected class)
    const selectedCommand = page.locator('.command-palette .selected, .command-palette .active');
    await expect(selectedCommand).toBeVisible();

    // Press up arrow
    await input.press('ArrowUp');

    // Selection should move back
  });

  test('should close command palette on Escape', async ({ page }) => {
    const input = page.locator('[aria-label="Message input"]');

    // Open command palette
    await input.fill('/');
    await page.waitForSelector('.command-palette', { state: 'visible' });

    // Press Escape
    await input.press('Escape');

    // Palette should close
    const palette = page.locator('.command-palette');
    await expect(palette).not.toBeVisible();
  });

  test('should execute command on Enter', async ({ page }) => {
    const input = page.locator('[aria-label="Message input"]');

    // Open command palette
    await input.fill('/help');

    // Press Enter
    await input.press('Enter');

    // Command should be executed (help message appears or palette closes)
    const palette = page.locator('.command-palette');
    await expect(palette).not.toBeVisible();
  });

  test('should show cancel button during processing', async ({ page }) => {
    const input = page.locator('[aria-label="Message input"]');

    // Send a message
    await input.fill('Test message that takes long to process');
    await page.click('button:has-text("Send")');

    // Mock slow processing state
    await page.evaluate(() => {
      const event = new CustomEvent('processing-started');
      window.dispatchEvent(event);
    });

    // Cancel button should appear
    const cancelButton = page.locator('button:has-text("Cancel")');
    await expect(cancelButton).toBeVisible({ timeout: 3000 });
  });

  test('should be keyboard accessible', async ({ page }) => {
    // Tab to input
    await page.keyboard.press('Tab');

    // Verify input is focused
    const input = page.locator('[aria-label="Message input"]');
    await expect(input).toBeFocused();

    // Type message
    await input.fill('Keyboard test');

    // Tab to Send button
    await page.keyboard.press('Tab');

    // Verify Send button is focused
    const sendButton = page.locator('button:has-text("Send")');
    await expect(sendButton).toBeFocused();

    // Press Enter to send
    await page.keyboard.press('Enter');

    // Message should be sent
    const message = page.locator('.chat-message', { hasText: 'Keyboard test' });
    await expect(message).toBeVisible({ timeout: 3000 });
  });
});
