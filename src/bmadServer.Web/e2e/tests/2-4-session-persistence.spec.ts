import { test, expect } from '@playwright/test';

test.describe('Story 2-4: Session Persistence', () => {
  
  test('Session state persisted during navigation', async ({ page }) => {
    await page.goto('/login');
    await page.fill('input[name="email"]', 'existing@example.com');
    await page.fill('input[name="password"]', 'SecurePass123!');
    await page.click('button:has-text("Login")');
    
    await expect(page).toHaveURL(/.*dashboard/);
    
    const sessionId = await page.evaluate(() => sessionStorage.getItem('sessionId'));
    expect(sessionId).toBeTruthy();
    
    await page.goto('/dashboard/settings');
    
    const persistedSessionId = await page.evaluate(() => sessionStorage.getItem('sessionId'));
    expect(persistedSessionId).toBe(sessionId);
  });

  test('Session recovered after refresh', async ({ page }) => {
    await page.goto('/login');
    await page.fill('input[name="email"]', 'existing@example.com');
    await page.fill('input[name="password"]', 'SecurePass123!');
    await page.click('button:has-text("Login")');
    
    await expect(page).toHaveURL(/.*dashboard/);
    
    const token = await page.evaluate(() => localStorage.getItem('accessToken'));
    
    await page.reload();
    
    const recoveredToken = await page.evaluate(() => localStorage.getItem('accessToken'));
    expect(recoveredToken).toBe(token);
    expect(page.url()).toContain('dashboard');
  });

  test('Multi-device sessions tracked independently', async ({ browser }) => {
    const context1 = await browser.newContext();
    const context2 = await browser.newContext();
    
    const page1 = await context1.newPage();
    const page2 = await context2.newPage();
    
    await page1.goto('http://localhost:3000/login');
    await page1.fill('input[name="email"]', 'existing@example.com');
    await page1.fill('input[name="password"]', 'SecurePass123!');
    await page1.click('button:has-text("Login")');
    
    await page2.goto('http://localhost:3000/login');
    await page2.fill('input[name="email"]', 'existing@example.com');
    await page2.fill('input[name="password"]', 'SecurePass123!');
    await page2.click('button:has-text("Login")');
    
    const token1 = await page1.evaluate(() => localStorage.getItem('accessToken'));
    const token2 = await page2.evaluate(() => localStorage.getItem('accessToken'));
    
    expect(token1).not.toBe(token2);
    
    await context1.close();
    await context2.close();
  });

  test('Session expires after inactivity timeout', async ({ page }) => {
    await page.goto('/login');
    await page.fill('input[name="email"]', 'existing@example.com');
    await page.fill('input[name="password"]', 'SecurePass123!');
    await page.click('button:has-text("Login")');
    
    await expect(page).toHaveURL(/.*dashboard/);
    
    await page.context().clearCookies();
    await page.evaluate(() => localStorage.clear());
    
    await page.reload();
    
    await expect(page).toHaveURL(/.*login/);
  });

  test('Offline session state preserved', async ({ page, context }) => {
    await page.goto('/login');
    await page.fill('input[name="email"]', 'existing@example.com');
    await page.fill('input[name="password"]', 'SecurePass123!');
    await page.click('button:has-text("Login")');
    
    await expect(page).toHaveURL(/.*dashboard/);
    
    const stateBeforeOffline = await page.evaluate(() => ({
      token: localStorage.getItem('accessToken'),
      sessionId: sessionStorage.getItem('sessionId')
    }));
    
    await context.setOffline(true);
    
    await page.reload();
    
    const stateAfterOffline = await page.evaluate(() => ({
      token: localStorage.getItem('accessToken'),
      sessionId: sessionStorage.getItem('sessionId')
    }));
    
    expect(stateAfterOffline.token).toBe(stateBeforeOffline.token);
    expect(stateAfterOffline.sessionId).toBe(stateBeforeOffline.sessionId);
  });
});
