import { Page, Locator } from '@playwright/test';

/**
 * Page Object Model for Chat interface
 * Covers Epic 3 stories: SignalR, messages, input, streaming, history, mobile
 */
export class ChatPage {
  readonly page: Page;

  // Main containers
  readonly chatContainer: Locator;
  readonly messageList: Locator;
  readonly inputArea: Locator;

  // Input components (Story 3.3)
  readonly messageInput: Locator;
  readonly sendButton: Locator;
  readonly characterCount: Locator;
  readonly commandPalette: Locator;
  readonly cancelButton: Locator;

  // Message components (Story 3.2)
  readonly userMessages: Locator;
  readonly agentMessages: Locator;
  readonly typingIndicator: Locator;

  // History components (Story 3.5)
  readonly loadMoreButton: Locator;
  readonly newMessageBadge: Locator;
  readonly welcomeMessage: Locator;
  readonly quickStartButtons: Locator;

  // Streaming components (Story 3.4)
  readonly stopGeneratingButton: Locator;

  // Mobile components (Story 3.6)
  readonly hamburgerMenu: Locator;
  readonly sidebar: Locator;

  constructor(page: Page) {
    this.page = page;

    // Main containers
    this.chatContainer = page.locator('[data-testid="chat-container"]');
    this.messageList = page.locator('[data-testid="message-list"]');
    this.inputArea = page.locator('[data-testid="input-area"]');

    // Input components
    this.messageInput = page.locator('[data-testid="message-input"]');
    this.sendButton = page.locator('[data-testid="send-button"]');
    this.characterCount = page.locator('[data-testid="character-count"]');
    this.commandPalette = page.locator('[data-testid="command-palette"]');
    this.cancelButton = page.locator('[data-testid="cancel-button"]');

    // Message components
    this.userMessages = page.locator('[data-testid="message"][data-sender="user"]');
    this.agentMessages = page.locator('[data-testid="message"][data-sender="agent"]');
    this.typingIndicator = page.locator('[data-testid="typing-indicator"]');

    // History components
    this.loadMoreButton = page.locator('[data-testid="load-more-button"]');
    this.newMessageBadge = page.locator('[data-testid="new-message-badge"]');
    this.welcomeMessage = page.locator('[data-testid="welcome-message"]');
    this.quickStartButtons = page.locator('[data-testid="quick-start-buttons"]');

    // Streaming components
    this.stopGeneratingButton = page.locator('[data-testid="stop-generating-button"]');

    // Mobile components
    this.hamburgerMenu = page.locator('[data-testid="hamburger-menu"]');
    this.sidebar = page.locator('[data-testid="sidebar"]');
  }

  // Navigation
  async goto(workflowId?: string) {
    const url = workflowId ? `/chat/${workflowId}` : '/chat';
    await this.page.goto(url);
    await this.chatContainer.waitFor({ state: 'visible' });
  }

  // Input actions (Story 3.3)
  async sendMessage(message: string) {
    await this.messageInput.fill(message);
    await this.sendButton.click();
  }

  async sendMessageWithKeyboard(message: string) {
    await this.messageInput.fill(message);
    await this.messageInput.press('Control+Enter');
  }

  async typeSlashCommand(command: string) {
    await this.messageInput.fill('/');
    await this.commandPalette.waitFor({ state: 'visible' });
    await this.page.keyboard.type(command);
  }

  async selectCommandFromPalette(index: number) {
    for (let i = 0; i < index; i++) {
      await this.page.keyboard.press('ArrowDown');
    }
    await this.page.keyboard.press('Enter');
  }

  // Message assertions (Story 3.2)
  async getMessageCount() {
    return await this.page.locator('[data-testid="message"]').count();
  }

  async getLastMessage() {
    return this.page.locator('[data-testid="message"]').last();
  }

  async waitForTypingIndicator(timeout = 500) {
    await this.typingIndicator.waitFor({ state: 'visible', timeout });
  }

  async waitForTypingIndicatorHidden(timeout = 30000) {
    await this.typingIndicator.waitFor({ state: 'hidden', timeout });
  }

  // Streaming actions (Story 3.4)
  async waitForStreamingToStart(timeout = 5000) {
    await this.typingIndicator.waitFor({ state: 'visible', timeout });
  }

  async stopStreaming() {
    await this.stopGeneratingButton.click();
  }

  async waitForStreamingComplete(timeout = 30000) {
    await this.typingIndicator.waitFor({ state: 'hidden', timeout });
  }

  // History actions (Story 3.5)
  async scrollToTop() {
    await this.messageList.evaluate((el) => el.scrollTo(0, 0));
  }

  async scrollToBottom() {
    await this.messageList.evaluate((el) => el.scrollTo(0, el.scrollHeight));
  }

  async getScrollPosition() {
    return await this.messageList.evaluate((el) => el.scrollTop);
  }

  async loadMoreMessages() {
    await this.loadMoreButton.click();
  }

  // Mobile actions (Story 3.6)
  async openSidebar() {
    await this.hamburgerMenu.click();
    await this.sidebar.waitFor({ state: 'visible' });
  }

  async closeSidebar() {
    // Swipe or click outside
    await this.chatContainer.click();
    await this.sidebar.waitFor({ state: 'hidden' });
  }

  // Accessibility helpers
  async getAriaLabel(locator: Locator) {
    return await locator.getAttribute('aria-label');
  }

  async getLiveRegion() {
    return this.page.locator('[aria-live="polite"]');
  }
}
