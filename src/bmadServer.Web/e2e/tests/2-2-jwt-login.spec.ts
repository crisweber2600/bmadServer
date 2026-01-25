import { test, expect } from '@playwright/test';

test.describe('Story 2-2: JWT Token Generation & Login', () => {
  
  test('Valid login generates access token', async ({ page }) => {
    await page.goto('/login');
    
    await page.fill('input[name="email"]', 'existing@example.com');
    await page.fill('input[name="password"]', 'SecurePass123!');
    await page.click('button:has-text("Login")');
    
    await expect(page).toHaveURL(/.*dashboard/);
    
    const token = await page.evaluate(() => localStorage.getItem('accessToken'));
    expect(token).toBeTruthy();
    expect(token).toMatch(/^[\w\-]*\.[\w\-]*\.[\w\-]*$/);
  });

  test('Invalid password shows error', async ({ page }) => {
    await page.goto('/login');
    
    await page.fill('input[name="email"]', 'existing@example.com');
    await page.fill('input[name="password"]', 'WrongPassword123!');
    await page.click('button:has-text("Login")');
    
    const errorMessage = page.locator('[role="alert"]');
    await expect(errorMessage).toContainText('Invalid credentials');
    
    expect(page.url()).not.toMatch(/.*dashboard/);
  });

  test('Non-existent email shows error', async ({ page }) => {
    await page.goto('/login');
    
    await page.fill('input[name="email"]', 'nonexistent@example.com');
    await page.fill('input[name="password"]', 'SecurePass123!');
    await page.click('button:has-text("Login")');
    
    const errorMessage = page.locator('[role="alert"]');
    await expect(errorMessage).toContainText('Invalid credentials');
  });

  test('JWT token included in API requests', async ({ page }) => {
    await page.goto('/login');
    await page.fill('input[name="email"]', 'existing@example.com');
    await page.fill('input[name="password"]', 'SecurePass123!');
    await page.click('button:has-text("Login")');
    
    await expect(page).toHaveURL(/.*dashboard/);
    
    const apiRequests: any[] = [];
    page.on('request', (request) => {
      if (request.url().includes('/api/')) {
        apiRequests.push(request);
      }
    });
    
    await page.click('[data-testid="load-data"]');
    await page.waitForLoadState('networkidle');
    
    const authRequest = apiRequests.find(r => 
      r.headers()['authorization']?.startsWith('Bearer ')
    );
    expect(authRequest).toBeTruthy();
  });

  test('Empty credentials shows validation error', async ({ page }) => {
    await page.goto('/login');
    
    await page.click('button:has-text("Login")');
    
    const emailError = page.locator('text=Email is required');
    const passwordError = page.locator('text=Password is required');
    
    await expect(emailError).toBeVisible();
    await expect(passwordError).toBeVisible();
  });
});
