import { test as base, Page } from '@playwright/test';

type AuthFixtures = {
  authenticatedPage: Page;
};

export const test = base.extend<AuthFixtures>({
  authenticatedPage: async ({ page }, use) => {
    // Login with test credentials before test runs
    await page.goto('/login');
    
    // Fill login form
    await page.fill('input[name="email"]', 'test@example.com');
    await page.fill('input[name="password"]', 'SecurePass123!');
    
    // Click login button
    await page.click('button:has-text("Login")');
    
    // Wait for redirect to dashboard
    await page.waitForURL(/\/dashboard/, { timeout: 10000 });
    
    // Provide authenticated page to test
    await use(page);
    
    // Cleanup: logout
    try {
      await page.click('[data-testid="logout-button"]');
    } catch {
      // Logout button may not exist, just clear cookies
      await page.context().clearCookies();
    }
  },
});

export { expect } from '@playwright/test';
