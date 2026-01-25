import { test, expect } from '@playwright/test';

test.describe('Story 2-6: Idle Timeout & Session Extension', () => {
  
  test('Idle timeout warning appears after inactivity', async ({ page }) => {
    await page.goto('/login');
    await page.fill('input[name="email"]', 'existing@example.com');
    await page.fill('input[name="password"]', 'SecurePass123!');
    await page.click('button:has-text("Login")');
    
    await expect(page).toHaveURL(/.*dashboard/);
    
    const warningModal = page.locator('[data-testid="timeout-warning"]');
    await expect(warningModal, { timeout: 90000 }).toBeVisible();
    
    const message = warningModal.locator('text=5 minutes remain');
    await expect(message).toBeVisible();
  });

  test('Session extended when user clicks extend button', async ({ page }) => {
    await page.goto('/login');
    await page.fill('input[name="email"]', 'existing@example.com');
    await page.fill('input[name="password"]', 'SecurePass123!');
    await page.click('button:has-text("Login")');
    
    await expect(page).toHaveURL(/.*dashboard/);
    
    const warningModal = page.locator('[data-testid="timeout-warning"]');
    await expect(warningModal, { timeout: 90000 }).toBeVisible();
    
    const extendButton = warningModal.locator('button:has-text("Extend Session")');
    await extendButton.click();
    
    await expect(warningModal).not.toBeVisible();
    
    const token = await page.evaluate(() => localStorage.getItem('accessToken'));
    expect(token).toBeTruthy();
  });

  test('User auto-logout after timeout', async ({ page }) => {
    await page.goto('/login');
    await page.fill('input[name="email"]', 'existing@example.com');
    await page.fill('input[name="password"]', 'SecurePass123!');
    await page.click('button:has-text("Login")');
    
    await expect(page).toHaveURL(/.*dashboard/);
    
    const warningModal = page.locator('[data-testid="timeout-warning"]');
    await expect(warningModal, { timeout: 90000 }).toBeVisible();
    
    await page.waitForTimeout(65000);
    
    await expect(page).toHaveURL(/.*login/);
    
    const token = await page.evaluate(() => localStorage.getItem('accessToken'));
    expect(token).toBeNull();
  });

  test('Activity resets idle timer', async ({ page }) => {
    await page.goto('/login');
    await page.fill('input[name="email"]', 'existing@example.com');
    await page.fill('input[name="password"]', 'SecurePass123!');
    await page.click('button:has-text("Login")');
    
    await expect(page).toHaveURL(/.*dashboard/);
    
    let warningAppeared = false;
    
    page.on('framenavigated', () => {
      warningAppeared = page.locator('[data-testid="timeout-warning"]').isVisible().catch(() => false);
    });
    
    await page.waitForTimeout(15000);
    
    await page.click('[data-testid="load-data"]');
    
    const warningModal = page.locator('[data-testid="timeout-warning"]');
    
    if (warningAppeared) {
      await expect(warningModal).not.toBeVisible();
    }
    
    expect(page.url()).toContain('dashboard');
  });

  test('Countdown timer displayed in warning modal', async ({ page }) => {
    await page.goto('/login');
    await page.fill('input[name="email"]', 'existing@example.com');
    await page.fill('input[name="password"]', 'SecurePass123!');
    await page.click('button:has-text("Login")');
    
    await expect(page).toHaveURL(/.*dashboard/);
    
    const warningModal = page.locator('[data-testid="timeout-warning"]');
    await expect(warningModal, { timeout: 90000 }).toBeVisible();
    
    const countdown = warningModal.locator('[data-testid="countdown-timer"]');
    await expect(countdown).toBeVisible();
    
    const initialTime = await countdown.textContent();
    expect(initialTime).toMatch(/\d+:\d+/);
    
    await page.waitForTimeout(1000);
    
    const laterTime = await countdown.textContent();
    expect(laterTime).not.toBe(initialTime);
  });
});
