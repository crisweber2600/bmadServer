# Three-Level Testing Guide

## Overview

bmadServer uses a **three-level testing pyramid** to ensure quality:

1. **Unit Tests** (Fast, Isolated)
2. **BDD Tests** (Acceptance Criteria)
3. **Playwright E2E Tests** (User Workflows)

---

## Level 1: Unit Tests

### Purpose
Test individual functions and classes in isolation.

### Location
- `src/bmadServer.Tests/` - .NET unit tests (xUnit)
- `src/bmadServer.ApiService.IntegrationTests/` - API integration tests

### Running Unit Tests
```bash
cd src
dotnet test
```

### Coverage Requirement
Minimum **90% code coverage** for new code.

### Example Test
```csharp
[Fact]
public void HashPassword_WithValidPassword_ReturnsHashedPassword()
{
    var password = "SecurePass123!";
    var hashedPassword = PasswordHasher.Hash(password);
    
    Assert.NotEmpty(hashedPassword);
    Assert.True(PasswordHasher.Verify(password, hashedPassword));
}
```

### Best Practices
- One assertion per test (focus on single behavior)
- Use meaningful test names (should describe expected behavior)
- Mock external dependencies
- Keep tests fast (< 1 second each)
- Use AAA pattern (Arrange, Act, Assert)

---

## Level 2: BDD Tests (Behavior-Driven Development)

### Purpose
Test that features work according to acceptance criteria (business requirements in Gherkin language).

### Framework
**Reqnroll** (open-source fork of SpecFlow) with xUnit

### Location
- Feature files: `src/bmadServer.BDD.Tests/Features/`
- Step definitions: `src/bmadServer.BDD.Tests/Steps/`
- Hooks: `src/bmadServer.BDD.Tests/Hooks/`
- Support: `src/bmadServer.BDD.Tests/Support/`

### Running BDD Tests
```bash
cd src
dotnet test bmadServer.BDD.Tests/bmadServer.BDD.Tests.csproj
```

### Writing a BDD Test

**Step 1: Create Feature File** (`Features/2-1-user-registration.feature`)
```gherkin
Feature: User Registration
  As a new user
  I want to register with email and password
  So that I can access the system

  Scenario: Valid user registration
    Given the API is running
    When I register with email "test@example.com" and password "SecurePass123!"
    Then the user is created successfully
    And I receive a confirmation email
```

**Step 2: Implement Step Definitions** (`Steps/AuthenticationSteps.cs`)
```csharp
[Binding]
public class AuthenticationSteps
{
    private readonly TestContext _context;

    [Given("the API is running")]
    public async Task GivenApiIsRunning()
    {
        var response = await _context.ApiClient.GetAsync("/health");
        Assert.True(response.IsSuccessStatusCode);
    }

    [When("I register with email \"(.*)\" and password \"(.*)\"")]
    public async Task WhenIRegisterWithCredentials(string email, string password)
    {
        var request = new { email, password };
        _context.LastResponse = await _context.ApiClient.PostAsJsonAsync(
            "/api/v1/auth/register",
            request
        );
    }

    [Then("the user is created successfully")]
    public void ThenUserIsCreated()
    {
        Assert.Equal(HttpStatusCode.Created, _context.LastResponse.StatusCode);
    }
}
```

### Requirements
- **Minimum 5 scenarios per story** (covers happy path + error cases)
- **All acceptance criteria must be testable** in Gherkin
- **Step definitions must be DRY** (no code duplication)
- **Feature files must be self-documenting** (readable by non-technical stakeholders)

### Best Practices
- Write Feature file first (BDD-style)
- Use Background for common setup
- One scenario per user workflow/behavior
- Use meaningful scenario names
- Reuse step definitions across scenarios

---

## Level 3: Playwright E2E (End-to-End) Tests

### Purpose
Test complete user workflows from UI perspective (browser automation).

### Framework
**Playwright** with TypeScript

### Location
- Test files: `src/bmadServer.Web/e2e/tests/`
- Page Objects: `src/bmadServer.Web/e2e/pages/`
- Fixtures: `src/bmadServer.Web/e2e/fixtures/`
- Helpers: `src/bmadServer.Web/e2e/helpers/`

### Running E2E Tests
```bash
cd src/bmadServer.Web
npm run test:e2e

# UI mode (visual debugging)
npm run test:e2e:ui

# Debug mode (step through)
npm run test:e2e:debug

# Specific test file
npx playwright test e2e/tests/2-1-user-registration.spec.ts
```

### Writing an E2E Test

**Step 1: Create Test File** (`e2e/tests/2-1-user-registration.spec.ts`)
```typescript
import { test, expect } from '@playwright/test';
import { AuthHelper } from '../helpers/auth-helper';

test.describe('User Registration (Story 2-1)', () => {
  
  test('should register with valid credentials', async ({ page }) => {
    const auth = new AuthHelper(page);
    
    await page.goto('/register');
    
    await page.fill('input[name="email"]', 'newuser@example.com');
    await page.fill('input[name="password"]', 'SecurePass123!');
    await page.click('button:has-text("Register")');
    
    await expect(page).toHaveURL(/.*dashboard/);
    expect(await auth.isLoggedIn()).toBeTruthy();
  });

  test('should show error for duplicate email', async ({ page }) => {
    await page.goto('/register');
    
    await page.fill('input[name="email"]', 'existing@example.com');
    await page.fill('input[name="password"]', 'SecurePass123!');
    await page.click('button:has-text("Register")');
    
    const error = page.locator('[role="alert"]');
    await expect(error).toContainText('Email already exists');
  });
});
```

**Step 2: Use Page Objects for Maintainability**
```typescript
// e2e/pages/RegisterPage.ts
export class RegisterPage {
  constructor(private page: Page) {}

  async goto() { await this.page.goto('/register'); }
  
  async fillEmail(email: string) {
    await this.page.fill('input[name="email"]', email);
  }
  
  async fillPassword(password: string) {
    await this.page.fill('input[name="password"]', password);
  }
  
  async clickRegister() {
    await this.page.click('button:has-text("Register")');
  }
}
```

### Requirements
- **Minimum 5 test cases per UI story**
- **Page Objects required** for maintainability
- **Use data-testid attributes** for reliable selectors
- **Tests must be independent** (can run in any order)
- **Happy path + error cases** must be covered

### Best Practices
- Use `data-testid` attributes (not class-based selectors)
- Encapsulate selectors in Page Objects
- Use custom fixtures for setup/teardown
- Wait for elements explicitly (not hard timeouts)
- Keep tests focused (one user action per test)

---

## Test Execution Flow

### Development (Local)
```bash
# Run all tests at once
npm run test:e2e           # Playwright E2E
dotnet test                # All unit + BDD tests

# Run specific level
dotnet test src/bmadServer.Tests  # Unit tests only
dotnet test src/bmadServer.BDD.Tests  # BDD tests only
npx playwright test         # E2E tests only
```

### CI/CD Pipeline
1. **Build job** - Compiles .NET and Node.js
2. **Test job** - Unit + BDD tests (5 min timeout)
3. **E2E Tests job** - Playwright tests (10 min timeout)
4. **Artifacts** - TRX reports, HTML Playwright report

---

## Coverage Requirements

| Level | Minimum Coverage | Location | Requirements |
|-------|------------------|----------|--------------|
| **Unit Tests** | 90% | `Tests/` | Fast, isolated, <1s each |
| **BDD Tests** | 100% of acceptance criteria | `BDD.Tests/` | 5+ scenarios per story |
| **E2E Tests** | Happy path + errors | `e2e/tests/` | 5+ test cases per UI story |

---

## Story Template Reference

When creating stories in Epic 3+, follow this structure:

```markdown
# Story 3-1: Signal Real-Time Connection

## Acceptance Criteria
1. User can establish WebSocket connection to /hubs/chat
2. Connection shows online status in UI
3. Connection persists for 24 hours or until user disconnects
4. Disconnection shows clear error message

## Unit Tests
- [ ] WebSocketManager establishes connection correctly
- [ ] Connection heartbeat sends every 30 seconds
- [ ] Reconnection logic works after network loss

## BDD Tests
- Feature file: Features/3-1-signal-connection.feature
- [ ] 5+ scenarios covering all acceptance criteria
- [ ] Test database setup/cleanup
- [ ] SignalR client connection tests

## Playwright E2E Tests (UI Story)
- Test file: e2e/tests/3-1-signal-connection.spec.ts
- Page Object: e2e/pages/ChatPage.ts
- [ ] 5+ test cases: connection, reconnection, error states
- [ ] Visual regression checks (optional)
- [ ] Performance baseline (connection establishes <2s)

## Definition of Done
- [ ] All acceptance criteria implemented
- [ ] Unit tests: 90%+ coverage
- [ ] BDD tests: All 5+ scenarios passing
- [ ] E2E tests: All 5+ test cases passing
- [ ] Code reviewed and approved
- [ ] CI/CD pipeline passing
- [ ] Documentation updated
```

---

## Debugging Tips

### Unit Tests
```bash
# Run with verbose output
dotnet test --logger "console;verbosity=detailed"

# Run specific test
dotnet test --filter "TestName"
```

### BDD Tests
```bash
# Run with full trace
dotnet test src/bmadServer.BDD.Tests --logger "console;verbosity=detailed"

# Run specific feature
dotnet test --filter "Feature"
```

### E2E Tests
```bash
# Debug mode (step through)
npx playwright test --debug

# UI mode (visual)
npm run test:e2e:ui

# Specific test
npx playwright test -g "should register"

# View report
npx playwright show-report
```

---

## Quality Gates

### Must Pass Before Merge
- ✅ All unit tests passing (90%+ coverage)
- ✅ All BDD tests passing
- ✅ All E2E tests passing (if UI story)
- ✅ CI/CD pipeline fully passing
- ✅ Code review approved
- ✅ Acceptance criteria met

### PR Checklist
- [ ] Unit tests written and passing
- [ ] BDD tests written and passing
- [ ] E2E tests written and passing (if UI)
- [ ] All acceptance criteria covered
- [ ] CI/CD passing
- [ ] Code reviewed
- [ ] Definition of Done met

---

## References

- [Unit Testing Best Practices](https://docs.microsoft.com/en-us/dotnet/core/testing/)
- [Reqnroll/SpecFlow Documentation](https://docs.reqnroll.net/)
- [Playwright Documentation](https://playwright.dev/)
- [BDD Cucumber Guide](https://cucumber.io/docs/gherkin/reference/)
