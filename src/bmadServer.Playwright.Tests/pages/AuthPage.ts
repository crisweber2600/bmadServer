import { Page, Locator } from '@playwright/test';

/**
 * Page Object Model for Authentication pages
 * Covers Epic 2 UI elements for login/register flows
 */
export class AuthPage {
  readonly page: Page;

  // Login form
  readonly loginForm: Locator;
  readonly emailInput: Locator;
  readonly passwordInput: Locator;
  readonly loginButton: Locator;
  readonly loginError: Locator;

  // Register form
  readonly registerForm: Locator;
  readonly displayNameInput: Locator;
  readonly confirmPasswordInput: Locator;
  readonly registerButton: Locator;
  readonly registerError: Locator;

  // Navigation
  readonly switchToRegisterLink: Locator;
  readonly switchToLoginLink: Locator;

  constructor(page: Page) {
    this.page = page;

    // Login form
    this.loginForm = page.locator('[data-testid="login-form"]');
    this.emailInput = page.getByLabel('Email');
    this.passwordInput = page.getByLabel('Password');
    this.loginButton = page.getByRole('button', { name: /sign in|login/i });
    this.loginError = page.locator('[data-testid="login-error"]');

    // Register form
    this.registerForm = page.locator('[data-testid="register-form"]');
    this.displayNameInput = page.getByLabel('Display Name');
    this.confirmPasswordInput = page.getByLabel('Confirm Password');
    this.registerButton = page.getByRole('button', { name: /register|sign up/i });
    this.registerError = page.locator('[data-testid="register-error"]');

    // Navigation
    this.switchToRegisterLink = page.getByRole('link', { name: /register|sign up/i });
    this.switchToLoginLink = page.getByRole('link', { name: /login|sign in/i });
  }

  async gotoLogin() {
    await this.page.goto('/login');
    await this.loginForm.waitFor({ state: 'visible' });
  }

  async gotoRegister() {
    await this.page.goto('/register');
    await this.registerForm.waitFor({ state: 'visible' });
  }

  async login(email: string, password: string) {
    await this.emailInput.fill(email);
    await this.passwordInput.fill(password);
    await this.loginButton.click();
  }

  async register(email: string, password: string, displayName: string) {
    await this.emailInput.fill(email);
    await this.displayNameInput.fill(displayName);
    await this.passwordInput.fill(password);
    await this.confirmPasswordInput.fill(password);
    await this.registerButton.click();
  }

  async getErrorMessage() {
    const loginError = await this.loginError.textContent();
    const registerError = await this.registerError.textContent();
    return loginError || registerError || null;
  }
}
