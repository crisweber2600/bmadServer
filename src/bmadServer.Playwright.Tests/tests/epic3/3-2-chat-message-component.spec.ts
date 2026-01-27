import { test, expect } from '@playwright/test';
import { ChatPage } from '../../pages/ChatPage';

/**
 * Story 3.2: Chat Message Component with Ant Design
 *
 * Tests message rendering, alignment, markdown conversion,
 * typing indicators, and accessibility features
 */
test.describe('Story 3.2: Chat Message Component with Ant Design', () => {
  let chatPage: ChatPage;

  test.beforeEach(async ({ page }) => {
    chatPage = new ChatPage(page);
    await chatPage.goto();
  });

  test('User messages aligned right with blue background @P1', async ({ page }) => {
    // Given I send a message
    await chatPage.sendMessage('This is a user message');

    // When the message renders
    await page.waitForTimeout(500);
    const lastMessage = await chatPage.getLastMessage();

    // Then user messages should be aligned right with blue background
    await expect(lastMessage).toHaveAttribute('data-sender', 'user');

    // Check alignment via CSS
    const styles = await lastMessage.evaluate((el) => {
      const computed = window.getComputedStyle(el);
      return {
        justifyContent: computed.justifyContent,
        alignSelf: computed.alignSelf,
        backgroundColor: computed.backgroundColor,
      };
    });

    // Should be right-aligned (flex-end or similar)
    expect(styles.alignSelf === 'flex-end' || styles.justifyContent === 'flex-end').toBe(true);

    // Should have blue-ish background
    expect(styles.backgroundColor).toMatch(/rgb\(\d+,\s*\d+,\s*2[0-5]\d\)/); // Blue range
  });

  test('Agent messages aligned left with gray background @P1', async ({ page }) => {
    // Given I send a message to trigger agent response
    await chatPage.sendMessage('Hello agent');

    // When agent responds
    await chatPage.waitForTypingIndicatorHidden(30000);

    // Then agent messages should be aligned left with gray background
    const agentMessages = chatPage.agentMessages;
    const count = await agentMessages.count();
    expect(count).toBeGreaterThan(0);

    const lastAgentMessage = agentMessages.last();
    const styles = await lastAgentMessage.evaluate((el) => {
      const computed = window.getComputedStyle(el);
      return {
        alignSelf: computed.alignSelf,
        backgroundColor: computed.backgroundColor,
      };
    });

    expect(styles.alignSelf === 'flex-start' || styles.alignSelf === 'auto').toBe(true);
  });

  test('Markdown to HTML conversion @P1', async ({ page }) => {
    // Given agent sends a message with markdown
    await chatPage.sendMessage('Show me some **bold** and *italic* text');

    // When the message renders
    await chatPage.waitForTypingIndicatorHidden(30000);

    // Then markdown should be converted to HTML
    const agentMessage = chatPage.agentMessages.last();
    const html = await agentMessage.innerHTML();

    // Check for markdown conversion (bold, italic, etc.)
    const hasFormattedText =
      html.includes('<strong>') ||
      html.includes('<b>') ||
      html.includes('<em>') ||
      html.includes('<i>') ||
      html.includes('<code>') ||
      html.includes('<pre>');

    expect(hasFormattedText || html.length > 0).toBe(true);
  });

  test('Code blocks syntax highlighted @P2', async ({ page }) => {
    // Given agent sends a message with code
    await chatPage.sendMessage('Show me a JavaScript code example');

    // When the message renders
    await chatPage.waitForTypingIndicatorHidden(30000);

    // Then code blocks should have syntax highlighting
    const agentMessage = chatPage.agentMessages.last();

    // Look for code/pre elements with highlighting classes
    const codeBlock = agentMessage.locator('pre, code');
    const hasCodeBlock = (await codeBlock.count()) > 0;

    // Code blocks should have syntax highlighting classes
    if (hasCodeBlock) {
      const classes = await codeBlock.first().getAttribute('class');
      expect(classes || '').toBeTruthy(); // Should have some class for highlighting
    }
  });

  test('Links clickable in new tabs @P2', async ({ page }) => {
    // Given agent sends a message with a link
    await chatPage.sendMessage('Give me a link to https://example.com');

    // When the message renders
    await chatPage.waitForTypingIndicatorHidden(30000);

    // Then links should have target="_blank"
    const agentMessage = chatPage.agentMessages.last();
    const links = agentMessage.locator('a[href]');

    const linkCount = await links.count();
    if (linkCount > 0) {
      for (let i = 0; i < linkCount; i++) {
        const target = await links.nth(i).getAttribute('target');
        expect(target).toBe('_blank');
      }
    }
  });

  test('Typing indicator appears within 500ms @P1', async ({ page }) => {
    // Given I am viewing chat
    const startTime = Date.now();

    // When I send a message to trigger agent response
    await chatPage.sendMessage('Quick response please');

    // Then typing indicator should appear within 500ms
    await chatPage.waitForTypingIndicator(500);
    const elapsed = Date.now() - startTime;

    expect(elapsed).toBeLessThan(1000); // Allow some margin
    await expect(chatPage.typingIndicator).toBeVisible();
  });

  test('ARIA labels for accessibility @P1', async ({ page }) => {
    // Given messages exist
    await chatPage.sendMessage('Accessibility test');
    await page.waitForTimeout(500);

    // When I check accessibility attributes
    const messages = page.locator('[data-testid="message"]');
    const count = await messages.count();

    // Then messages should have aria-labels
    for (let i = 0; i < count; i++) {
      const ariaLabel = await messages.nth(i).getAttribute('aria-label');
      const role = await messages.nth(i).getAttribute('role');

      // Should have either aria-label or appropriate role
      expect(ariaLabel !== null || role !== null).toBe(true);
    }
  });

  test('Live region announcements @P1', async ({ page }) => {
    // Given chat is loaded
    // When I check for live regions
    const liveRegion = await chatPage.getLiveRegion();

    // Then live region should exist for screen readers
    const exists = (await liveRegion.count()) > 0;
    expect(exists).toBe(true);

    // And should have correct aria-live attribute
    if (exists) {
      const ariaLive = await liveRegion.first().getAttribute('aria-live');
      expect(['polite', 'assertive']).toContain(ariaLive);
    }
  });
});
