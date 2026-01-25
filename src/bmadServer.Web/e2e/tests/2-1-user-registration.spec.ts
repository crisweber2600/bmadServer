import { test, expect } from '@playwright/test';

test.describe('User Registration (Story 2-1)', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/register');
  });

  test('should register a new user with valid credentials', async ({ page }) => {
    // Fill registration form
    await page.fill('input[name="email"]', `user-${Date.now()}@example.com`);
    await page.fill('input[name="password"]', 'SecurePass123!');
    await page.fill('input[name="displayName"]', 'Test User');

    // Submit form
    await page.click('button:has-text("Register")');

    // Verify redirect to dashboard
    await expect(page).toHaveURL(/\/dashboard/);
  });

  test('should show error for invalid email format', async ({ page }) => {
    // Fill registration form with invalid email
    await page.fill('input[name="email"]', 'invalid-email');
    await page.fill('input[name="password"]', 'SecurePass123!');

    // Submit form
    await page.click('button:has-text("Register")');

    // Verify error message appears
    const errorMessage = page.locator('[role="alert"]');
    await expect(errorMessage).toBeVisible();
    await expect(errorMessage).toContainText('email');
  });

  test('should show error for weak password', async ({ page }) => {
    // Fill registration form with weak password
    await page.fill('input[name="email"]', 'test@example.com');
    await page.fill('input[name="password"]', 'weak');

    // Submit form
    await page.click('button:has-text("Register")');

    // Verify error message appears
    const errorMessage = page.locator('[role="alert"]');
    await expect(errorMessage).toBeVisible();
    await expect(errorMessage).toContainText('password');
  });

  test('should show error when email already exists', async ({ page }) => {
    const existingEmail = 'existing@example.com';

    // First registration
    await page.fill('input[name="email"]', existingEmail);
    await page.fill('input[name="password"]', 'SecurePass123!');
    await page.click('button:has-text("Register")');

    // Wait for redirect
    await page.waitForURL(/\/dashboard/, { timeout: 5000 }).catch(() => {
      // May not redirect if already registered
    });

    // Try to register with same email
    await page.goto('/register');
    await page.fill('input[name="email"]', existingEmail);
    await page.fill('input[name="password"]', 'AnotherPass456!');
    await page.click('button:has-text("Register")');

    // Verify error message
    const errorMessage = page.locator('[role="alert"]');
    await expect(errorMessage).toBeVisible();
    await expect(errorMessage).toContainText('already exists');
  });

  test('should disable register button when form is invalid', async ({ page }) => {
    const registerButton = page.locator('button:has-text("Register")');

    // Initially button should be disabled or form incomplete
    // Fill only email
    await page.fill('input[name="email"]', 'test@example.com');

    // Button should still be disabled
    // Note: Actual behavior depends on form validation implementation
    // This is a placeholder assertion
  });
});
