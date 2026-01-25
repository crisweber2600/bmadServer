import { test, expect } from '@playwright/test';

test.describe('Story 2-3: Refresh Token Flow', () => {
  
  test('Refresh token issued on login', async ({ page }) => {
    await page.goto('/login');
    
    await page.fill('input[name="email"]', 'existing@example.com');
    await page.fill('input[name="password"]', 'SecurePass123!');
    await page.click('button:has-text("Login")');
    
    await expect(page).toHaveURL(/.*dashboard/);
    
    const refreshToken = await page.evaluate(() => 
      document.cookie.split('; ').find(c => c.startsWith('refreshToken='))
    );
    expect(refreshToken).toBeTruthy();
    expect(refreshToken).toContain('HttpOnly');
  });

  test('Token refresh maintains session', async ({ page }) => {
    await page.goto('/login');
    await page.fill('input[name="email"]', 'existing@example.com');
    await page.fill('input[name="password"]', 'SecurePass123!');
    await page.click('button:has-text("Login")');
    
    await expect(page).toHaveURL(/.*dashboard/);
    
    const initialToken = await page.evaluate(() => localStorage.getItem('accessToken'));
    expect(initialToken).toBeTruthy();
    
    await page.context().addInitScript(() => {
      const event = new Event('sessionextend');
      window.dispatchEvent(event);
    });
    
    await page.waitForLoadState('networkidle');
    
    const newToken = await page.evaluate(() => localStorage.getItem('accessToken'));
    expect(newToken).toBeTruthy();
    expect(newToken).not.toBe(initialToken);
  });

  test('Invalid refresh token shows error', async ({ page }) => {
    await page.goto('/login');
    
    await page.evaluate(() => {
      document.cookie = 'refreshToken=invalid-token; path=/';
    });
    
    const result = await page.evaluate(async () => {
      const response = await fetch('/api/v1/auth/refresh', {
        method: 'POST'
      });
      return response.status;
    });
    
    expect(result).toBe(401);
  });

  test('Multiple concurrent refreshes handled safely', async ({ page }) => {
    await page.goto('/login');
    await page.fill('input[name="email"]', 'existing@example.com');
    await page.fill('input[name="password"]', 'SecurePass123!');
    await page.click('button:has-text("Login")');
    
    await expect(page).toHaveURL(/.*dashboard/);
    
    const refreshResults = await page.evaluate(async () => {
      const promises = Array(5).fill(null).map(() =>
        fetch('/api/v1/auth/refresh', { method: 'POST' })
      );
      const responses = await Promise.all(promises);
      return responses.map(r => r.status);
    });
    
    const successCount = refreshResults.filter(status => status === 200).length;
    expect(successCount).toBeGreaterThanOrEqual(1);
  });

  test('Refresh token rotates on use', async ({ page }) => {
    await page.goto('/login');
    await page.fill('input[name="email"]', 'existing@example.com');
    await page.fill('input[name="password"]', 'SecurePass123!');
    await page.click('button:has-text("Login")');
    
    await expect(page).toHaveURL(/.*dashboard/);
    
    const oldCookie = await page.context().cookies();
    
    await page.evaluate(async () => {
      await fetch('/api/v1/auth/refresh', { method: 'POST' });
    });
    
    const newCookie = await page.context().cookies();
    
    const oldRefresh = oldCookie.find(c => c.name === 'refreshToken');
    const newRefresh = newCookie.find(c => c.name === 'refreshToken');
    
    expect(oldRefresh?.value).not.toBe(newRefresh?.value);
  });
});
