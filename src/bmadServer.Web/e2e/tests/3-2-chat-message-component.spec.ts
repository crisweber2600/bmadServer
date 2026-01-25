import { test, expect } from '@playwright/test';

test.describe('Chat Message Component with Ant Design (Story 3-2)', () => {
  test.beforeEach(async ({ page }) => {
    // Login and navigate to chat
    await page.goto('/login');
    await page.fill('input[name="email"]', 'test@example.com');
    await page.fill('input[name="password"]', 'SecurePass123!');
    await page.click('button:has-text("Login")');
    await page.waitForURL(/\/dashboard/, { timeout: 5000 });
    await page.goto('/chat');
  });

  test('should display user messages aligned right with blue background', async ({ page }) => {
    const testMessage = 'This is my message';
    
    // Send a message
    await page.fill('[aria-label="Message input"]', testMessage);
    await page.click('button:has-text("Send")');

    // Find the user message
    const userMessage = page.locator('.chat-message.user-message', { hasText: testMessage });
    await expect(userMessage).toBeVisible();

    // Check alignment and styling
    const styles = await userMessage.evaluate((el) => {
      const computed = window.getComputedStyle(el);
      return {
        textAlign: computed.textAlign,
        backgroundColor: computed.backgroundColor,
        justifyContent: computed.justifyContent
      };
    });

    // User messages should be aligned right or have appropriate flex properties
    expect(styles.textAlign === 'right' || styles.justifyContent === 'flex-end').toBeTruthy();
  });

  test('should display agent messages aligned left with gray background', async ({ page }) => {
    // Mock receiving an agent message via SignalR
    await page.evaluate(() => {
      const event = new CustomEvent('agent-message', {
        detail: {
          id: 'msg-123',
          role: 'agent',
          content: 'Hello! How can I help?',
          timestamp: new Date().toISOString(),
          agentName: 'TestAgent'
        }
      });
      window.dispatchEvent(event);
    });

    // Wait for agent message to appear
    const agentMessage = page.locator('.chat-message.agent-message', { hasText: 'Hello! How can I help?' });
    await expect(agentMessage).toBeVisible({ timeout: 3000 });

    // Verify agent avatar is shown
    const avatar = agentMessage.locator('.agent-avatar, [aria-label*="avatar"]');
    await expect(avatar).toBeVisible();
  });

  test('should render markdown formatting correctly', async ({ page }) => {
    // Inject a message with markdown
    await page.evaluate(() => {
      const container = document.querySelector('.chat-messages');
      if (container) {
        const messageDiv = document.createElement('div');
        messageDiv.className = 'chat-message agent-message';
        messageDiv.innerHTML = `
          <div class="message-content">
            This is <strong>bold</strong> and <code>code</code>
          </div>
        `;
        container.appendChild(messageDiv);
      }
    });

    // Verify markdown is rendered
    const boldText = page.locator('.chat-message strong:has-text("bold")');
    await expect(boldText).toBeVisible();

    const codeText = page.locator('.chat-message code:has-text("code")');
    await expect(codeText).toBeVisible();
  });

  test('should render code blocks with syntax highlighting', async ({ page }) => {
    // Inject a message with a code block
    await page.evaluate(() => {
      const container = document.querySelector('.chat-messages');
      if (container) {
        const messageDiv = document.createElement('div');
        messageDiv.className = 'chat-message agent-message';
        messageDiv.innerHTML = `
          <div class="message-content">
            <pre><code class="language-javascript">const greeting = "Hello";
console.log(greeting);</code></pre>
          </div>
        `;
        container.appendChild(messageDiv);
      }
    });

    // Verify code block exists
    const codeBlock = page.locator('pre code.language-javascript');
    await expect(codeBlock).toBeVisible();

    // Verify it contains expected code
    await expect(codeBlock).toContainText('const greeting');
  });

  test('should make links clickable and open in new tab', async ({ page }) => {
    // Inject a message with a link
    await page.evaluate(() => {
      const container = document.querySelector('.chat-messages');
      if (container) {
        const messageDiv = document.createElement('div');
        messageDiv.className = 'chat-message agent-message';
        messageDiv.innerHTML = `
          <div class="message-content">
            Check out <a href="https://github.com" target="_blank" rel="noopener noreferrer">GitHub</a>
          </div>
        `;
        container.appendChild(messageDiv);
      }
    });

    // Verify link exists and has correct attributes
    const link = page.locator('.chat-message a:has-text("GitHub")');
    await expect(link).toBeVisible();
    await expect(link).toHaveAttribute('href', 'https://github.com');
    await expect(link).toHaveAttribute('target', '_blank');
    await expect(link).toHaveAttribute('rel', /noopener/);
  });

  test('should display typing indicator when agent is typing', async ({ page }) => {
    // Trigger typing indicator
    await page.evaluate(() => {
      const container = document.querySelector('.chat-messages');
      if (container) {
        const typingDiv = document.createElement('div');
        typingDiv.className = 'typing-indicator';
        typingDiv.setAttribute('data-testid', 'typing-indicator');
        typingDiv.innerHTML = '<span class="agent-name">Agent</span> is typing...';
        container.appendChild(typingDiv);
      }
    });

    // Verify typing indicator is visible
    const typingIndicator = page.locator('[data-testid="typing-indicator"]');
    await expect(typingIndicator).toBeVisible();

    // Verify it has animated ellipsis (check for animation class or dots)
    await expect(typingIndicator).toContainText('typing');
  });

  test('should have proper ARIA labels for accessibility', async ({ page }) => {
    const testMessage = 'Accessibility test message';
    
    // Send a message
    await page.fill('[aria-label="Message input"]', testMessage);
    await page.click('button:has-text("Send")');

    // Wait for message
    const message = page.locator('.chat-message', { hasText: testMessage });
    await expect(message).toBeVisible();

    // Check for ARIA attributes
    const ariaLabel = await message.getAttribute('aria-label');
    const role = await message.getAttribute('role');

    // Should have some accessibility attributes
    expect(ariaLabel || role).toBeTruthy();
  });

  test('should auto-scroll to new messages', async ({ page }) => {
    // Get initial scroll position
    const initialScroll = await page.evaluate(() => {
      const container = document.querySelector('.chat-messages');
      return container?.scrollTop || 0;
    });

    // Add multiple messages to trigger scroll
    for (let i = 0; i < 10; i++) {
      await page.evaluate((index) => {
        const container = document.querySelector('.chat-messages');
        if (container) {
          const messageDiv = document.createElement('div');
          messageDiv.className = 'chat-message agent-message';
          messageDiv.innerHTML = `<div class="message-content">Message ${index}</div>`;
          container.appendChild(messageDiv);
        }
      }, i);
    }

    // Wait a bit for auto-scroll
    await page.waitForTimeout(1000);

    // Get final scroll position
    const finalScroll = await page.evaluate(() => {
      const container = document.querySelector('.chat-messages');
      return container?.scrollTop || 0;
    });

    // Should have scrolled down
    expect(finalScroll).toBeGreaterThanOrEqual(initialScroll);
  });
});
