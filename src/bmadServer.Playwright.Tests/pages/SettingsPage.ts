import { Page, Locator } from '@playwright/test';

/**
 * Page Object Model for Settings/Persona page
 * Covers Epic 8 stories: Persona configuration and switching
 */
export class SettingsPage {
  readonly page: Page;

  // Settings container
  readonly settingsContainer: Locator;

  // Persona section (Story 8.1, 8.4)
  readonly personaSection: Locator;
  readonly personaSwitcher: Locator;
  readonly currentPersona: Locator;
  readonly businessOption: Locator;
  readonly technicalOption: Locator;
  readonly hybridOption: Locator;
  readonly personaDescription: Locator;

  // Notification
  readonly notification: Locator;

  constructor(page: Page) {
    this.page = page;

    // Settings container
    this.settingsContainer = page.locator('[data-testid="settings-container"]');

    // Persona section
    this.personaSection = page.locator('[data-testid="persona-section"]');
    this.personaSwitcher = page.locator('[data-testid="persona-switcher"]');
    this.currentPersona = page.locator('[data-testid="current-persona"]');
    this.businessOption = page.locator('[data-testid="persona-business"]');
    this.technicalOption = page.locator('[data-testid="persona-technical"]');
    this.hybridOption = page.locator('[data-testid="persona-hybrid"]');
    this.personaDescription = page.locator('[data-testid="persona-description"]');

    // Notification
    this.notification = page.locator('[data-testid="notification"]');
  }

  async goto() {
    await this.page.goto('/settings');
    await this.settingsContainer.waitFor({ state: 'visible' });
  }

  async openPersonaSwitcher() {
    await this.personaSwitcher.click();
  }

  async selectPersona(persona: 'business' | 'technical' | 'hybrid') {
    await this.openPersonaSwitcher();
    switch (persona) {
      case 'business':
        await this.businessOption.click();
        break;
      case 'technical':
        await this.technicalOption.click();
        break;
      case 'hybrid':
        await this.hybridOption.click();
        break;
    }
  }

  async getCurrentPersona() {
    return await this.currentPersona.textContent();
  }

  async openPersonaSwitcherWithKeyboard() {
    await this.page.keyboard.press('Control+Shift+P');
    await this.personaSwitcher.waitFor({ state: 'visible' });
  }

  async selectPersonaWithArrowKeys(index: number) {
    for (let i = 0; i < index; i++) {
      await this.page.keyboard.press('ArrowDown');
    }
    await this.page.keyboard.press('Enter');
  }

  async hoverOverPersonaOption(persona: 'business' | 'technical' | 'hybrid') {
    const option =
      persona === 'business'
        ? this.businessOption
        : persona === 'technical'
          ? this.technicalOption
          : this.hybridOption;
    await option.hover();
    return this.personaDescription;
  }

  async waitForNotification(text?: string, timeout = 5000) {
    await this.notification.waitFor({ state: 'visible', timeout });
    if (text) {
      await this.notification.filter({ hasText: text }).waitFor({ state: 'visible', timeout });
    }
  }
}
