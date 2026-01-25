# Playwright E2E Testing Guide

## Overview

This guide explains how to write and run end-to-end tests using Playwright for the bmadServer Web application.

## Running Tests

### Run all tests
```bash
npm run test:e2e
```

### Run tests in UI mode
```bash
npm run test:e2e:ui
```

### Run tests in debug mode
```bash
npm run test:e2e:debug
```

### Run tests with headed browser (visible)
```bash
npm run test:e2e:headed
```

### Run a specific test file
```bash
npx playwright test e2e/tests/2-1-user-registration.spec.ts
```

### Run tests matching a pattern
```bash
npx playwright test --grep "login"
```

## Project Structure

```
e2e/
├── tests/              # Test specification files
│   ├── 2-1-user-registration.spec.ts
│   ├── 2-2-jwt-login.spec.ts
│   └── ... (more spec files)
├── fixtures/           # Custom test fixtures for reusability
│   ├── auth.fixture.ts
│   └── api.fixture.ts
├── pages/              # Page Object Pattern implementations
│   ├── LoginPage.ts
│   └── ... (more page objects)
├── helpers/            # Utility helpers for tests
│   ├── api-helper.ts
│   ├── database-helper.ts
│   └── auth-helper.ts
├── playwright.config.ts # Playwright configuration
└── test-results/       # Generated test reports
```

## Writing Tests

### Basic Test Structure

```typescript
import { test, expect } from '@playwright/test';

test.describe('Feature Name', () => {
  test('should do something', async ({ page }) => {
    await page.goto('/login');
    
    const element = page.locator('h1');
    await expect(element).toContainText('Login');
  });
});
```

### Using Page Objects

Create a Page Object to encapsulate page-specific selectors and actions:

```typescript
// e2e/pages/LoginPage.ts
import { Page } from '@playwright/test';

export class LoginPage {
  constructor(private page: Page) {}

  async goto() {
    await this.page.goto('/login');
  }

  async fillEmail(email: string) {
    await this.page.fill('input[name="email"]', email);
  }

  async fillPassword(password: string) {
    await this.page.fill('input[name="password"]', password);
  }

  async clickLogin() {
    await this.page.click('button:has-text("Login")');
  }

  async getErrorMessage() {
    return this.page.locator('[role="alert"]').textContent();
  }
}
```

Then use in tests:

```typescript
import { test } from '@playwright/test';
import { LoginPage } from '../pages/LoginPage';

test('login flow', async ({ page }) => {
  const loginPage = new LoginPage(page);
  await loginPage.goto();
  await loginPage.fillEmail('test@example.com');
  await loginPage.fillPassword('password123');
  await loginPage.clickLogin();
});
```

### Using Fixtures

Fixtures provide reusable test setup. Use the `auth.fixture.ts` for authenticated tests:

```typescript
import { test } from '../fixtures/auth.fixture';

test('should load dashboard when authenticated', async ({ authenticatedPage }) => {
  await authenticatedPage.goto('/dashboard');
  
  const title = authenticatedPage.locator('h1');
  await expect(title).toContainText('Dashboard');
});
```

### Using Helper Classes

```typescript
import { test } from '@playwright/test';
import { AuthHelper } from '../helpers/auth-helper';

test('logout flow', async ({ page }) => {
  const auth = new AuthHelper(page);
  
  await auth.loginAsUser('test@example.com', 'password123');
  
  const isLoggedIn = await auth.isLoggedIn();
  expect(isLoggedIn).toBeTruthy();
  
  await auth.logout();
  
  const isStillLoggedIn = await auth.isLoggedIn();
  expect(isStillLoggedIn).toBeFalsy();
});
```

## Common Assertions

```typescript
// URL assertions
await expect(page).toHaveURL('http://example.com/dashboard');
await expect(page).toHaveURL(/.*dashboard/);

// Text assertions
await expect(element).toContainText('Hello');
await expect(element).toHaveText('Exact text');

// Visibility assertions
await expect(element).toBeVisible();
await expect(element).toBeHidden();

// Disabled assertions
await expect(button).toBeDisabled();
await expect(button).toBeEnabled();

// Count assertions
const items = page.locator('.item');
await expect(items).toHaveCount(3);
```

## Best Practices

1. **Use data-testid attributes** for reliable selectors
   ```typescript
   // Prefer this
   await page.click('[data-testid="login-button"]');
   
   // Avoid this
   await page.click('.btn.btn-primary');
   ```

2. **Use Page Objects** for maintainability
   - Encapsulates selectors and actions
   - Makes tests more readable
   - Easier to update when UI changes

3. **Use Fixtures** for setup/teardown
   - Reduces code duplication
   - Automatic cleanup
   - Better test isolation

4. **Wait for elements properly**
   ```typescript
   // Wait for element to appear
   await expect(element).toBeVisible({ timeout: 5000 });
   
   // Wait for URL change
   await page.waitForURL('**/dashboard');
   
   // Wait for network idle
   await page.waitForLoadState('networkidle');
   ```

5. **Use meaningful test names**
   ```typescript
   // Good
   test('should show error when logging in with invalid password', async => {});
   
   // Poor
   test('login test', async => {});
   ```

6. **Keep tests independent**
   - Each test should be able to run in isolation
   - Use fixtures for setup
   - Clean up after tests

## Configuration

The `playwright.config.ts` file includes:

- **Multiple browsers**: Chromium, Firefox, WebKit
- **Screenshots on failure**: Automatically captured
- **Video recording**: Retained on failure
- **HTML reports**: Generated after test runs
- **Retries**: 2 retries in CI, 0 locally
- **Base URL**: http://localhost:3000

## Debugging Tests

### Using Playwright Inspector

```bash
npx playwright test --debug
```

This opens the Playwright Inspector where you can:
- Step through tests
- Hover over elements
- See selectors
- View console messages

### Viewing Reports

```bash
npx playwright show-report
```

Opens the HTML report showing:
- Test results
- Screenshots
- Videos (for failed tests)
- Timeline

### Checking a Single Test

```bash
npx playwright test e2e/tests/2-1-user-registration.spec.ts --debug
```

## CI/CD Integration

Tests run automatically in GitHub Actions on:
- Every push to any branch
- All pull requests

The CI/CD workflow:
1. Installs dependencies
2. Builds the application
3. Starts the API server
4. Runs Playwright tests
5. Uploads test artifacts
6. Generates HTML report

## Troubleshooting

### Tests timeout
- Increase timeout in test: `{ timeout: 60000 }`
- Check if server is running
- Verify base URL in config

### Element not found
- Check data-testid attribute exists in DOM
- Use `page.pause()` to debug
- Verify selector with `--debug` mode

### Tests pass locally but fail in CI
- Check environment variables
- Verify base URL matches
- Check for timing issues (use `waitForLoadState`)

### Flaky tests
- Increase waits with explicit conditions
- Avoid hard timeouts
- Use `waitForURL` instead of waiting for time

## Best Test Patterns

### Wait for navigation
```typescript
await Promise.all([
  page.waitForNavigation(),
  page.click('button')
]);
```

### Wait for API response
```typescript
const response = await page.waitForResponse(
  response => response.url().includes('/api/users') && response.status() === 200
);
```

### Handle dialog boxes
```typescript
page.on('dialog', dialog => dialog.accept());
await page.click('button-that-triggers-dialog');
```

### Fill forms
```typescript
await page.fill('input[type="email"]', 'test@example.com');
await page.check('input[type="checkbox"]');
await page.selectOption('select', 'option-value');
```

## Performance Testing

Check page load performance:

```typescript
test('page loads quickly', async ({ page }) => {
  const startTime = Date.now();
  
  await page.goto('/dashboard');
  
  const loadTime = Date.now() - startTime;
  expect(loadTime).toBeLessThan(3000);
});
```

## Accessibility Testing

Install `@axe-core/playwright`:

```bash
npm install --save-dev @axe-core/playwright
```

```typescript
import { injectAxe, checkA11y } from 'axe-playwright';

test('page should be accessible', async ({ page }) => {
  await page.goto('/dashboard');
  await injectAxe(page);
  await checkA11y(page);
});
```

## References

- [Playwright Documentation](https://playwright.dev)
- [Best Practices](https://playwright.dev/docs/best-practices)
- [API Reference](https://playwright.dev/docs/api/class-page)
