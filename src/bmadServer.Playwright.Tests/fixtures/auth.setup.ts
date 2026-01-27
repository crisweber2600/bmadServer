import { test as setup, expect } from '@playwright/test';
import path from 'path';

const authFile = path.join(__dirname, '../playwright/.auth/user.json');

/**
 * Authentication setup for all tests
 * Runs once before test suite to establish authenticated session
 */
setup('authenticate', async ({ page }) => {
  // Navigate to login page
  await page.goto('/login');

  // Fill in test credentials
  await page.getByLabel('Email').fill(process.env.TEST_USER_EMAIL || 'test@example.com');
  await page.getByLabel('Password').fill(process.env.TEST_USER_PASSWORD || 'TestPassword123!');

  // Submit login form
  await page.getByRole('button', { name: /sign in|login/i }).click();

  // Wait for successful redirect (dashboard or home)
  await page.waitForURL(/\/(dashboard|home|chat)?$/);

  // Verify authentication succeeded
  await expect(page).not.toHaveURL(/\/login/);

  // Save authentication state
  await page.context().storageState({ path: authFile });
});
