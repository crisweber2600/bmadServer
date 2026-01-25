# Prep Sprint: BDD & Playwright Framework Initialization

**Sprint Name:** Test Framework Foundation  
**Duration:** 1 week (5 business days)  
**Start Date:** January 27, 2026 (after Epic 2 retrospective)  
**End Date:** January 31, 2026  
**Goal:** Initialize production-ready BDD and E2E testing frameworks before Epic 3 begins

---

## 📋 Sprint Objectives

1. **Initialize SpecFlow BDD Framework** - Executable acceptance criteria for all stories
2. **Initialize Playwright E2E Framework** - Automated UI testing with Page Objects
3. **Retrofit Epic 2 Tests** - Write .feature files for completed stories
4. **Update Story Template** - Lock in BDD + Playwright requirements
5. **Team Training** - Patterns, best practices, CI/CD integration

---

## 🎯 Day-by-Day Breakdown

### Day 1: BDD Framework Setup & Epic 2 Retrospective Tests

**Goal:** SpecFlow initialized, first .feature files written for Epic 2

#### Morning (3 hours)

**Task 1.1: Initialize SpecFlow Project** (1 hour)
- Create `src/bmadServer.BDD.Tests/` project
- Add NuGet packages:
  - `SpecFlow`
  - `SpecFlow.NUnit`
  - `Gherkin`
  - `BoDi` (dependency injection)
- Configure feature file discovery

**Task 1.2: Create Hooks & Step Definitions Framework** (1.5 hours)
- Create `Hooks/AuthenticationHooks.cs` - Setup/teardown for auth tests
- Create `Hooks/DatabaseHooks.cs` - Database reset between scenarios
- Create `Steps/AuthenticationSteps.cs` - Implement Given/When/Then steps for Story 2-1
- Create `StepDefinitions/` directory structure

**Task 1.3: Write First Feature File** (0.5 hours)
- Create `Features/2-1-user-registration.feature`
- Map all Story 2-1 acceptance criteria to Gherkin
- Example:
```gherkin
Feature: User Registration & Local Database Authentication
  As a new user (Sarah, non-technical co-founder)
  I want to create an account with email and password
  So that I can securely access bmadServer and start using BMAD workflows

  Scenario: Register with valid email and password
    Given no user exists with email "sarah@example.com"
    When I send a POST request to "/api/v1/auth/register" with:
      | email       | sarah@example.com      |
      | password    | SecurePass123!         |
      | displayName | Sarah Johnson          |
    Then the response status is 201 Created
    And the response includes a user with email "sarah@example.com"
    And the password is hashed using bcrypt
    And the Email column has a unique constraint

  Scenario: Register with duplicate email
    Given a user exists with email "existing@example.com"
    When I send a POST request to "/api/v1/auth/register" with:
      | email       | existing@example.com   |
      | password    | SecurePass123!         |
      | displayName | New User               |
    Then the response status is 409 Conflict
    And the error type is "user-exists"

  Scenario: Register with weak password
    Given no user exists with email "test@example.com"
    When I send a POST request to "/api/v1/auth/register" with:
      | email       | test@example.com       |
      | password    | weak                   |
      | displayName | Test User              |
    Then the response status is 400 Bad Request
    And the error mentions "password must be at least 8 characters"
```

#### Afternoon (3 hours)

**Task 1.4: Implement Step Definitions for Story 2-1** (2 hours)
```csharp
[Given("no user exists with email \"([^\"]+)\"")]
public void GivenNoUserExists(string email)
{
    var user = _dbContext.Users.FirstOrDefault(u => u.Email == email);
    user.Should().BeNull($"User with email {email} should not exist");
}

[When("I send a POST request to \"([^\"]+)\" with:")]
public void WhenISendPostRequest(string endpoint, Table table)
{
    var request = table.Rows.ToDictionary(r => r["Key"], r => r["Value"]);
    _httpClient.DefaultRequestHeaders.Clear();
    var content = new StringContent(JsonSerializer.Serialize(request), 
        Encoding.UTF8, "application/json");
    _response = _httpClient.PostAsync($"http://localhost:5000{endpoint}", content).Result;
}

[Then("the response status is (\\d+) (.+)")]
public void ThenResponseStatus(int statusCode, string statusDescription)
{
    _response.StatusCode.Should().Be((HttpStatusCode)statusCode);
}
```

**Task 1.5: Create Test Configuration** (1 hour)
- Create `app.config` with test database connection string
- Create `Fixtures/HttpClientFixture.cs` - Shared HTTP client for API tests
- Create `Fixtures/DatabaseFixture.cs` - Database setup/teardown
- Configure SpecFlow runners for NUnit

#### Deliverables (Day 1):
- ✅ SpecFlow project initialized
- ✅ `Features/2-1-user-registration.feature` written
- ✅ Step definitions for 2-1 implemented
- ✅ First test scenario running (even if some steps pending)

---

### Day 2: Complete Epic 2 BDD Tests

**Goal:** Write .feature files for remaining 5 Epic 2 stories

#### Morning (4 hours)

**Task 2.1-2.2: Write Features for Stories 2-2 & 2-3** (2 hours)
- `Features/2-2-jwt-token-generation.feature`
  - Scenario: Login and receive JWT token
  - Scenario: Login with wrong password
  - Scenario: Access protected endpoint with valid token
  - Scenario: Access protected endpoint with expired token
  
- `Features/2-3-refresh-token-flow.feature`
  - Scenario: Refresh token generates new access token
  - Scenario: Refresh token rotation is atomic
  - Scenario: Reuse of refresh token revokes all user tokens

**Task 2.3: Implement Step Definitions for 2-2 & 2-3** (1 hour)
- Add JWT assertion helpers
- Add token extraction from response
- Add cookie validation steps

**Task 2.4: Write Features for Stories 2-4, 2-5, 2-6** (1 hour)
- `Features/2-4-session-persistence.feature`
- `Features/2-5-rbac-implementation.feature`
- `Features/2-6-idle-timeout-security.feature`

#### Afternoon (3 hours)

**Task 2.5: Implement Step Definitions for 2-4, 2-5, 2-6** (2 hours)
- Session state verification
- RBAC authorization checks
- Idle timeout timer simulation

**Task 2.6: Add BDD Tests to CI/CD Pipeline** (1 hour)
- Update `.github/workflows/ci.yml`:
```yaml
- name: Run BDD Tests
  run: |
    cd src
    dotnet test bmadServer.BDD.Tests --configuration Release --logger trx
```

#### Deliverables (Day 2):
- ✅ 6 .feature files written for all Epic 2 stories
- ✅ 30+ Gherkin scenarios (5 per story average)
- ✅ Step definitions for all scenarios
- ✅ BDD tests integrated into CI/CD

---

### Day 3: Playwright Framework Setup

**Goal:** Playwright initialized with Page Objects and sample tests

#### Morning (4 hours)

**Task 3.1: Initialize Playwright Project** (1 hour)
```bash
cd UI
npm install -D @playwright/test
npx playwright install
```

**Task 3.2: Create playwright.config.ts** (1 hour)
```typescript
import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './e2e/tests',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: 'html',
  use: {
    baseURL: 'http://localhost:3000',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
  },

  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
    {
      name: 'firefox',
      use: { ...devices['Desktop Firefox'] },
    },
  ],

  webServer: {
    command: 'npm run dev',
    url: 'http://localhost:3000',
    reuseExistingServer: !process.env.CI,
  },
});
```

**Task 3.3: Create Fixtures** (1.5 hours)

`e2e/fixtures/auth.fixture.ts`:
```typescript
import { test as base, expect } from '@playwright/test';

type AuthFixtures = {
  authenticatedPage: Page;
  user: { email: string; password: string };
};

export const test = base.extend<AuthFixtures>({
  user: {
    email: 'test@example.com',
    password: 'TestPass123!',
  },

  authenticatedPage: async ({ page, user }, use) => {
    // Navigate to login
    await page.goto('/login');
    
    // Fill in credentials
    await page.fill('[data-testid="email-input"]', user.email);
    await page.fill('[data-testid="password-input"]', user.password);
    
    // Click login
    await page.click('[data-testid="login-button"]');
    
    // Wait for navigation to dashboard
    await page.waitForURL('/dashboard');
    
    // Use the authenticated page
    await use(page);
  },
});

export { expect };
```

`e2e/fixtures/api.fixture.ts`:
```typescript
import { test as base } from '@playwright/test';

type ApiFixtures = {
  apiToken: string;
};

export const test = base.extend<ApiFixtures>({
  apiToken: async ({}, use) => {
    // Get auth token via API
    const response = await fetch('http://localhost:5000/api/v1/auth/login', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        email: 'test@example.com',
        password: 'TestPass123!',
      }),
    });
    
    const data = await response.json();
    await use(data.accessToken);
  },
});
```

**Task 3.4: Create Page Objects** (0.5 hours)

`e2e/pages/login.page.ts`:
```typescript
import { Page } from '@playwright/test';

export class LoginPage {
  constructor(private page: Page) {}

  async goto() {
    await this.page.goto('/login');
  }

  async login(email: string, password: string) {
    await this.page.fill('[data-testid="email-input"]', email);
    await this.page.fill('[data-testid="password-input"]', password);
    await this.page.click('[data-testid="login-button"]');
  }

  async getErrorMessage() {
    return await this.page.textContent('[data-testid="error-message"]');
  }
}
```

#### Afternoon (3 hours)

**Task 3.5: Write Sample E2E Tests for Story 2-1** (1.5 hours)

`e2e/tests/2-1-user-registration.spec.ts`:
```typescript
import { test, expect } from '../fixtures/auth.fixture';

test.describe('User Registration', () => {
  test('should register with valid credentials', async ({ page }) => {
    await page.goto('/register');
    
    await page.fill('[data-testid="email-input"]', 'newuser@example.com');
    await page.fill('[data-testid="password-input"]', 'SecurePass123!');
    await page.fill('[data-testid="displayName-input"]', 'New User');
    
    await page.click('[data-testid="register-button"]');
    
    // Wait for success message or redirect
    await page.waitForURL('/login');
    await expect(page).toHaveURL('/login');
  });

  test('should show error for duplicate email', async ({ page }) => {
    // Pre-create user
    await page.goto('/register');
    await page.fill('[data-testid="email-input"]', 'existing@example.com');
    await page.fill('[data-testid="password-input"]', 'SecurePass123!');
    await page.fill('[data-testid="displayName-input"]', 'Existing User');
    await page.click('[data-testid="register-button"]');
    
    // Try to register again with same email
    await page.goto('/register');
    await page.fill('[data-testid="email-input"]', 'existing@example.com');
    await page.fill('[data-testid="password-input"]', 'SecurePass123!');
    await page.fill('[data-testid="displayName-input"]', 'Another User');
    await page.click('[data-testid="register-button"]');
    
    // Expect error
    const error = await page.textContent('[data-testid="error-message"]');
    expect(error).toContain('already exists');
  });
});
```

**Task 3.6: Configure package.json Scripts** (1 hour)
```json
{
  "scripts": {
    "test:e2e": "playwright test",
    "test:e2e:ui": "playwright test --ui",
    "test:e2e:debug": "playwright test --debug",
    "test:e2e:report": "playwright show-report"
  }
}
```

#### Deliverables (Day 3):
- ✅ Playwright project initialized
- ✅ Configuration with multi-browser support
- ✅ Fixtures for auth, API, database
- ✅ Page Objects for main pages
- ✅ Sample E2E tests for Story 2-1

---

### Day 4: Complete Playwright Tests & CI/CD Integration

**Goal:** Write E2E tests for remaining Epic 2 stories, integrate into CI/CD

#### Morning (4 hours)

**Task 4.1: Write E2E Tests for Stories 2-2 to 2-6** (3 hours)
- `2-2-jwt-login.spec.ts` - Happy path, wrong password, token validation
- `2-3-refresh-token.spec.ts` - Token refresh, automatic refresh, logout
- `2-4-session-recovery.spec.ts` - Reconnection, state restore, multi-device
- `2-5-rbac-roles.spec.ts` - Role-based UI visibility (Admin vs Participant vs Viewer)
- `2-6-idle-timeout.spec.ts` - Warning modal, extend session, auto-logout

**Task 4.2: Create Shared Test Utilities** (1 hour)
- `e2e/helpers/api.helper.ts` - API calls for test setup/teardown
- `e2e/helpers/database.helper.ts` - Direct DB access for test data
- `e2e/helpers/auth.helper.ts` - Token generation, user creation

#### Afternoon (3 hours)

**Task 4.3: Integrate Playwright into CI/CD** (1.5 hours)

Update `.github/workflows/ci.yml`:
```yaml
- name: Start API Server
  run: |
    cd src
    dotnet run --project bmadServer.ApiService &
    sleep 5  # Wait for API to start

- name: Install UI Dependencies
  run: |
    cd UI
    npm install

- name: Run Playwright Tests
  run: |
    cd UI
    npm run test:e2e

- name: Upload Playwright Report
  if: always()
  uses: actions/upload-artifact@v4
  with:
    name: playwright-report
    path: UI/playwright-report/
    retention-days: 30
```

**Task 4.4: Document E2E Testing Guide** (1.5 hours)

Create `UI/E2E-TESTING.md`:
```markdown
# Playwright E2E Testing Guide

## Running Tests Locally

```bash
# Run all tests
npm run test:e2e

# Run specific test file
npx playwright test 2-1-user-registration.spec.ts

# Run in UI mode (interactive)
npm run test:e2e:ui

# Debug mode
npm run test:e2e:debug
```

## Writing New Tests

1. Create test file in `e2e/tests/`
2. Use fixtures (authenticatedPage, apiToken, etc.)
3. Use Page Objects for UI interactions
4. Run locally to verify
5. Commit to git

## Fixtures

- `authenticatedPage` - Page with logged-in user
- `apiToken` - JWT token for API calls
- `user` - Test user credentials

## Best Practices

- Use data-testid attributes for element selection
- Use Page Objects to encapsulate UI interactions
- Keep tests focused on user workflows
- Use fixtures for setup/teardown
```

#### Deliverables (Day 4):
- ✅ 6 E2E test files written (one per Epic 2 story)
- ✅ 30+ test scenarios covering happy path + error cases
- ✅ Playwright integrated into GitHub Actions CI/CD
- ✅ Documentation for E2E testing workflow

---

### Day 5: Story Template Update & Validation

**Goal:** Update story template, validate frameworks work, team training

#### Morning (3 hours)

**Task 5.1: Update Story Template** (1.5 hours)

Add new sections to story template:

```markdown
## BDD Tests

**Status:** [Not Started | In Progress | Complete]

**Feature File:** `src/bmadServer.BDD.Tests/Features/{story-number}-{story-name}.feature`

**Scenarios Covered:**
- [ ] Happy path (main workflow)
- [ ] Error cases (validation, edge cases)
- [ ] Security scenarios (if applicable)

**Run Tests:**
```bash
cd src
dotnet test bmadServer.BDD.Tests --filter "{story-number}" --configuration Release
```

---

## Playwright E2E Tests

**Status:** [Not Applicable | Not Started | In Progress | Complete]

**Test File:** `UI/e2e/tests/{story-number}-{story-name}.spec.ts`

**Scenarios Covered:**
- [ ] Happy path (user workflow)
- [ ] Error handling (UI feedback)
- [ ] Edge cases (timing, network, state)

**Run Tests:**
```bash
cd UI
npm run test:e2e -- {story-number}
```

---

## Definition of Done (Updated)

- [ ] All acceptance criteria implemented
- [ ] Unit tests written (API layer)
- [ ] Integration tests written (database layer)
- [ ] **BDD tests written (.feature file)**
- [ ] **Playwright E2E tests written (if UI story)**
- [ ] Code review passed
- [ ] All tests passing
- [ ] Documentation complete
```

**Task 5.2: Create PR Template with Test Requirements** (0.5 hours)

Update `.github/pull_request_template.md`:
```markdown
## Story

Closes #[story-number]

## Changes

- [ ] API implementation
- [ ] Database migrations
- [ ] Unit tests
- [ ] Integration tests
- [ ] BDD tests (.feature file)
- [ ] Playwright E2E tests (if applicable)

## Test Results

- [ ] All unit tests passing: `dotnet test`
- [ ] All BDD tests passing: `dotnet test bmadServer.BDD.Tests`
- [ ] All E2E tests passing (if applicable): `npm run test:e2e`
```

**Task 5.3: Run All Tests End-to-End** (1 hour)
```bash
# BDD tests
cd src && dotnet test bmadServer.BDD.Tests

# Playwright tests
cd UI && npm run test:e2e

# API integration tests
cd src && dotnet test bmadServer.ApiService.IntegrationTests
```

#### Afternoon (2 hours)

**Task 5.4: Team Training & Documentation** (1.5 hours)

Create `TESTING-GUIDE.md`:
```markdown
# Testing Strategy for bmadServer

## Three Levels of Testing

### 1. Unit Tests
- **Framework:** xUnit + Moq
- **Coverage:** Individual methods, logic
- **Example:** `PasswordHasher.Hash()` produces valid bcrypt hash
- **Run:** `dotnet test bmadServer.Tests`

### 2. BDD Tests (NEW - Mandatory)
- **Framework:** SpecFlow + NUnit
- **Coverage:** Acceptance criteria from stories
- **Example:** Story 2-1 "Register with valid email" scenario
- **Run:** `dotnet test bmadServer.BDD.Tests`

### 3. E2E Tests (NEW - Mandatory for UI)
- **Framework:** Playwright
- **Coverage:** Complete user workflows in real browser
- **Example:** User registration → login → see dashboard
- **Run:** `npm run test:e2e`

## Story Workflow (Updated)

1. Implement feature (code)
2. Write unit tests (test business logic)
3. Write integration tests (test with database)
4. Write BDD tests (test acceptance criteria)
5. Write Playwright E2E tests (test UI workflow)
6. Code review
7. Merge when all tests pass

## CI/CD Pipeline

GitHub Actions runs:
1. Build solution
2. Run unit tests
3. Run BDD tests
4. Run integration tests
5. Run Playwright E2E tests
6. Upload reports

Merge only succeeds if ALL tests pass.
```

**Task 5.5: Create Examples for Epic 3 Stories** (0.5 hours)

Prepare template examples:
- Example .feature file for chat interface
- Example Playwright test for message sending
- Example step definitions

#### Deliverables (Day 5):
- ✅ Story template updated with BDD + Playwright sections
- ✅ PR template requires test coverage
- ✅ All frameworks validated (tests passing)
- ✅ Team training documentation complete
- ✅ Examples prepared for Epic 3

---

## 📊 Sprint Summary

### Hours by Task

| Task | Hours | Owner |
|------|-------|-------|
| SpecFlow Setup | 4 | QA Agent |
| Epic 2 BDD Tests (5 stories) | 8 | QA Agent |
| Playwright Setup | 7 | QA Agent + Dev |
| Epic 2 E2E Tests (5 stories) | 8 | QA Agent + Dev |
| CI/CD Integration | 4 | Dev Agent |
| Documentation & Training | 4 | SM + QA |
| **TOTAL** | **35 hours** | **1 week** |

### Deliverables Checklist

- [ ] SpecFlow project initialized
- [ ] 6 .feature files written (30+ Gherkin scenarios)
- [ ] BDD step definitions for all scenarios
- [ ] Playwright project initialized
- [ ] 6 E2E test files written (30+ Playwright scenarios)
- [ ] Page Objects created
- [ ] Fixtures for auth, API, database
- [ ] BDD tests in CI/CD pipeline
- [ ] Playwright tests in CI/CD pipeline
- [ ] Story template updated
- [ ] Testing guide documented
- [ ] Team training complete

### Success Criteria

- ✅ All Epic 2 BDD tests passing (6/6 scenarios green)
- ✅ All Epic 2 Playwright tests passing (6/6 scenarios green)
- ✅ CI/CD pipeline includes both test types
- ✅ Story template locked with test requirements
- ✅ Team trained on new testing frameworks
- ✅ Epic 3 stories ready to include BDD + Playwright tests

---

## 🚀 Post-Sprint: Ready for Epic 3

After completing this prep sprint:

1. **BDD Framework is production-ready** → Every Epic 3 story will have executable .feature files
2. **Playwright Framework is production-ready** → Every Epic 3 UI story will have automated E2E tests
3. **Story Template is locked** → Developers know to write BDD + Playwright tests
4. **Team is trained** → QA + Dev agents understand new workflow
5. **CI/CD validates everything** → No story merges without full test coverage

**Epic 3 Kickoff Ready:** ✅ YES

---

## 📝 Notes

- **BDD Tests:** Map acceptance criteria → executable scenarios
- **Playwright Tests:** User workflows → automated browser testing
- **CI/CD Integration:** Both test types run on every PR
- **Living Documentation:** Feature files = behavior documentation
- **Quality Gate:** All tests must pass before merge

---

**Assigned to:** QA Agent + Dev Agent  
**Start Date:** Monday, January 27, 2026  
**End Date:** Friday, January 31, 2026  
**Status:** Ready to Schedule

