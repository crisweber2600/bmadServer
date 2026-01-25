import { Page } from '@playwright/test';

export class AuthHelper {
  constructor(private page: Page) {}

  async loginAsUser(email: string, password: string) {
    await this.page.goto('/login');
    await this.page.fill('input[name="email"]', email);
    await this.page.fill('input[name="password"]', password);
    await this.page.click('button:has-text("Login")');
    await this.page.waitForURL(/.*dashboard/);
  }

  async logout() {
    const logoutButton = this.page.locator('[data-testid="logout-button"]');
    await logoutButton.click();
    await this.page.waitForURL(/.*login/);
  }

  async getAccessToken(): Promise<string | null> {
    return this.page.evaluate(() => localStorage.getItem('accessToken'));
  }

  async getRefreshToken(): Promise<string | null> {
    const cookies = await this.page.context().cookies();
    const refreshCookie = cookies.find(c => c.name === 'refreshToken');
    return refreshCookie?.value || null;
  }

  async setAccessToken(token: string) {
    await this.page.evaluate((t) => {
      localStorage.setItem('accessToken', t);
    }, token);
  }

  async clearTokens() {
    await this.page.evaluate(() => {
      localStorage.removeItem('accessToken');
      sessionStorage.clear();
    });
    await this.page.context().clearCookies();
  }

  async isLoggedIn(): Promise<boolean> {
    const token = await this.getAccessToken();
    return token !== null && token !== '';
  }

  async waitForTimeout(ms: number) {
    await this.page.waitForTimeout(ms);
  }

  async dismissTimeoutWarning() {
    const dismissButton = this.page.locator('[data-testid="timeout-dismiss"]');
    if (await dismissButton.isVisible()) {
      await dismissButton.click();
    }
  }

  async extendSessionFromWarning() {
    const extendButton = this.page.locator('button:has-text("Extend Session")');
    await extendButton.click();
  }
}
