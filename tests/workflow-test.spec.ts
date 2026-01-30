
import { test, expect } from '@playwright/test';

test('Workflow Selection and Start', async ({ page }) => {
    // 1. Navigate
    await page.goto('http://localhost:50589/');
    
    // 2. Handle Login/Register
    // Check if we are on login page
    const loginButton = page.getByRole('button', { name: /log in/i });
    if (await loginButton.isVisible()) {
        console.log("On Login Page");
        
        // Try Login first
        await page.getByPlaceholder('Email').fill('test2@example.com');
        await page.getByPlaceholder('Password').fill('Password123!');
        await loginButton.click();
        
        // Check for error or success
        try {
            await expect(page.locator('.ant-layout-header')).toBeVisible({ timeout: 3000 });
            console.log("Login Successful");
        } catch (e) {
            console.log("Login failed, trying registration");
            // If login failed, likely need to register
            await page.getByText('Register now').click();
            await page.getByPlaceholder('Email').fill('test2@example.com');
            await page.getByPlaceholder('Password').fill('Password123!');
            await page.getByPlaceholder('Confirm Password').fill('Password123!');
            await page.getByRole('button', { name: /register/i }).click();
            
            await expect(page.locator('.ant-layout-header')).toBeVisible({ timeout: 10000 });
             console.log("Registration Successful");
        }
    } else {
        console.log("Already Logged In");
    }

    // 3. Workflow Selection
    await expect(page.getByText('Select a workflow')).toBeVisible();
    
    // Open dropdown
    await page.locator('.ant-select-selector').click();
    
    // Select the item
    // "Create Product Requirements Document"
    await page.getByText('Create Product Requirements Document').click();
    
    // 4. Click Start
    // Wait for button to be enabled - this was the failure point before
    const startButton = page.locator('button.ant-btn-primary').filter({ hasText: /start/i });
    // Or just the button next to the select
    
    // Debug: print button state
    const isDisabled = await startButton.isDisabled();
    console.log(`Start button disabled: ${isDisabled}`);
    
    await expect(startButton).toBeEnabled({ timeout: 5000 });
    await startButton.click();
    console.log("Clicked Start");

    // 5. Observe Status
    // "Workflow Status" bar should appear
    await expect(page.getByText('Workflow Status')).toBeVisible({ timeout: 5000 });
    console.log("Workflow Status Visible");
    
    // Take screenshot
    await page.screenshot({ path: 'workflow-started.png' });
});
