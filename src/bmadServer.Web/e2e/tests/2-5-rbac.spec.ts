import { test, expect } from '@playwright/test';

test.describe('Story 2-5: Role-Based Access Control', () => {
  
  test('Admin can access admin panel', async ({ page }) => {
    await page.goto('/login');
    await page.fill('input[name="email"]', 'admin@example.com');
    await page.fill('input[name="password"]', 'SecurePass123!');
    await page.click('button:has-text("Login")');
    
    await expect(page).toHaveURL(/.*dashboard/);
    await page.goto('/admin/users');
    
    await expect(page).toHaveURL(/.*admin.*users/);
    const title = page.locator('h1');
    await expect(title).toContainText('User Management');
  });

  test('Participant cannot access admin panel', async ({ page }) => {
    await page.goto('/login');
    await page.fill('input[name="email"]', 'participant@example.com');
    await page.fill('input[name="password"]', 'SecurePass123!');
    await page.click('button:has-text("Login")');
    
    await expect(page).toHaveURL(/.*dashboard/);
    await page.goto('/admin/users');
    
    const accessDeniedMessage = page.locator('text=Access Denied');
    await expect(accessDeniedMessage).toBeVisible();
  });

  test('Viewer cannot create or edit resources', async ({ page }) => {
    await page.goto('/login');
    await page.fill('input[name="email"]', 'viewer@example.com');
    await page.fill('input[name="password"]', 'SecurePass123!');
    await page.click('button:has-text("Login")');
    
    await expect(page).toHaveURL(/.*dashboard/);
    
    const createButton = page.locator('button:has-text("Create")');
    await expect(createButton).not.toBeVisible();
    
    const editButtons = page.locator('[data-testid="edit-button"]');
    await expect(editButtons).not.toBeVisible();
  });

  test('Viewer can read resources', async ({ page }) => {
    await page.goto('/login');
    await page.fill('input[name="email"]', 'viewer@example.com');
    await page.fill('input[name="password"]', 'SecurePass123!');
    await page.click('button:has-text("Login")');
    
    await expect(page).toHaveURL(/.*dashboard/);
    
    const resourceList = page.locator('[data-testid="resource-list"]');
    await expect(resourceList).toBeVisible();
    
    const items = resourceList.locator('[data-testid="resource-item"]');
    const count = await items.count();
    expect(count).toBeGreaterThan(0);
  });

  test('Role change applies immediately', async ({ page, context }) => {
    await page.goto('/login');
    await page.fill('input[name="email"]', 'participant@example.com');
    await page.fill('input[name="password"]', 'SecurePass123!');
    await page.click('button:has-text("Login")');
    
    await expect(page).toHaveURL(/.*dashboard/);
    
    const adminLinkBefore = page.locator('[href="/admin"]');
    await expect(adminLinkBefore).not.toBeVisible();
    
    await page.evaluate(async () => {
      await fetch('/api/v1/users/update-role', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ role: 'Admin' })
      });
    });
    
    const adminLinkAfter = page.locator('[href="/admin"]');
    await expect(adminLinkAfter).toBeVisible();
  });
});
