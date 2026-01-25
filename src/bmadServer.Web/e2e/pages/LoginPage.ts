import { Page, expect } from '@playwright/test';

export class LoginPage {
  readonly page: Page;
  readonly emailInput = 'input[name="email"]';
  readonly passwordInput = 'input[name="password"]';
  readonly loginButton = 'button:has-text("Login")';
  readonly errorMessage = '[role="alert"]';

  constructor(page: Page) {
    this.page = page;
  }

  async goto() {
    await this.page.goto('/login');
  }

  async fillEmail(email: string) {
    await this.page.fill(this.emailInput, email);
  }

  async fillPassword(password: string) {
    await this.page.fill(this.passwordInput, password);
  }

  async clickLogin() {
    await this.page.click(this.loginButton);
  }

  async getErrorMessage(): Promise<string | null> {
    try {
      return await this.page.textContent(this.errorMessage);
    } catch {
      return null;
    }
  }

  async login(email: string, password: string) {
    await this.goto();
    await this.fillEmail(email);
    await this.fillPassword(password);
    await this.clickLogin();
  }

  async loginAndWaitForDashboard(email: string, password: string) {
    await this.login(email, password);
    await this.page.waitForURL(/\/dashboard/);
  }
}
