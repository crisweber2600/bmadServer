const { chromium } = require('playwright');

(async () => {
    const browser = await chromium.launch({ headless: true });
    const page = await browser.newPage();
    
    // Capture console logs
    page.on('console', msg => console.log(`[Browser Console] ${msg.type()}: ${msg.text()}`));

    try {
        await page.goto('http://localhost:5173');
        
        // Wait for potential auto-login or redirect
        await page.waitForTimeout(2000);

        // Check if we need to login/register
        if (await page.isVisible('input[placeholder="Email"]')) {
            console.log('On login/register page.');
            
            // Switch to register
            await page.click('text=Don\'t have an account? Register');
            await page.waitForTimeout(500);

            // Register
            const timestamp = Date.now();
            await page.fill('input[placeholder="Display Name"]', `Test User ${timestamp}`);
            await page.fill('input[placeholder="Email"]', `test${timestamp}@example.com`);
            await page.fill('input[placeholder="Password"]', 'Password123!');
            await page.click('button:has-text("Register")');
            
            // Wait for registration to complete and sign-in to be available (or auto-login)
            // Some apps redirect to login, some auto-login.
            // Based on previous logs, we might need to manually sign in or it might auto-sign in? 
            // Previous code clicked "Sign in" after registration. The notification says "You can now sign in".
            // So we need to switch back to login.
            
            await page.waitForTimeout(2000);
            
            // Assuming we are back at login form or need to click "Already have an account? Sign in"
            // Wait, the registration success notification appears.
            // Check if we are still on register form (Register button visible?)
            if (await page.isVisible('button:has-text("Register")')) {
                 await page.click('text=Already have an account? Sign in');
            }

            // Login
            await page.fill('input[placeholder="Email"]', `test${timestamp}@example.com`);
            await page.fill('input[placeholder="Password"]', 'Password123!');
            await page.click('button:has-text("Sign in")');
        }

        // Wait for Chat UI
        console.log('Waiting for Chat UI...');
        await page.waitForSelector('.workflow-selector-container', { timeout: 10000 });
        console.log('Chat UI loaded.');

        // Wait for SignalR connection (check logs for validation)
        await page.waitForTimeout(3000);

        // Select workflow
        console.log('Selecting workflow...');
        await page.click('.workflow-selector-container .ant-select');
        await page.waitForSelector('.ant-select-dropdown:not(.ant-select-dropdown-hidden)');
        
        // Click the first option
        await page.click('.ant-select-item-option');
        
        // Wait for state update
        await page.waitForTimeout(2000);

        // Check button state
        const isStartEnabled = await page.evaluate(() => {
            const btn = document.querySelector('.workflow-selector-container button');
            return btn && !btn.disabled;
        });
        
        console.log('Start Button Enabled:', isStartEnabled);

        // Helper to dump button attributes
        const btnAttrs = await page.evaluate(() => {
             const btn = document.querySelector('.workflow-selector-container button');
             return {
                 disabled: btn.disabled,
                 className: btn.className,
                 outerHTML: btn.outerHTML
             };
        });
        console.log('Button attributes:', btnAttrs);

    } catch (error) {
        console.error('Error:', error);
    } finally {
        await browser.close();
    }
})();
