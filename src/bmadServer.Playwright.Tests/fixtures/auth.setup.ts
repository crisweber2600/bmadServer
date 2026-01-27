import { test as setup, expect } from '@playwright/test';
import path from 'path';

const authFile = path.join(__dirname, '../playwright/.auth/user.json');

const TEST_USER_EMAIL = process.env.TEST_USER_EMAIL || 'test@example.com';
const TEST_USER_PASSWORD = process.env.TEST_USER_PASSWORD || 'TestPassword123!';
const TEST_USER_DISPLAY_NAME = process.env.TEST_USER_DISPLAY_NAME || 'Test User';

/**
 * Authentication setup for all tests
 * Runs once before test suite to establish authenticated session
 * Will attempt to register user if login fails (first run scenario)
 */
setup('authenticate', async ({ page, request }) => {
  // First, try to register the test user (idempotent - 409 if exists is OK)
  try {
    const registerResponse = await request.post('/api/v1/auth/register', {
      data: {
        email: TEST_USER_EMAIL,
        password: TEST_USER_PASSWORD,
        displayName: TEST_USER_DISPLAY_NAME,
      },
    });
    
    // 201 = created, 409 = already exists (both are acceptable)
    if (registerResponse.status() !== 201 && registerResponse.status() !== 409) {
      console.warn(`User registration returned ${registerResponse.status()}`);
    }
  } catch (error) {
    // Registration endpoint may not exist in all environments
    console.warn('Could not pre-register test user:', error);
  }

  // Navigate to login page
  await page.goto('/login');

  // Fill in test credentials
  await page.getByLabel('Email').fill(TEST_USER_EMAIL);
  await page.getByLabel('Password').fill(TEST_USER_PASSWORD);

  // Submit login form
  await page.getByRole('button', { name: /sign in|login/i }).click();

  // Wait for successful redirect (dashboard or home)
  await page.waitForURL(/\/(dashboard|home|chat)?$/, { timeout: 10000 });

  // Verify authentication succeeded
  await expect(page).not.toHaveURL(/\/login/);

  // Save authentication state
  await page.context().storageState({ path: authFile });
});
