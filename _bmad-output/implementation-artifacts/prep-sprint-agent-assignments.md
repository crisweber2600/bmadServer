# Prep Sprint Agent Assignments (Jan 27-31, 2026)

**Objective:** Initialize BDD (SpecFlow) and Playwright E2E test frameworks
**Duration:** 1 week (5 business days)
**Total Effort:** 35 hours across QA Agent + Dev Agent
**Success Criteria:** 
- ✅ SpecFlow initialized with 6 .feature files + step definitions
- ✅ Playwright initialized with 6 .spec.ts files + Page Objects
- ✅ Both frameworks integrated into CI/CD pipeline
- ✅ All tests passing in automated workflow
- ✅ Templates updated with test requirements
- ✅ Team trained on new frameworks

---

## 📋 AGENT ROLES

### QA Agent
**Responsibility:** Test framework setup, test design, automation
**Daily Role:** Lead Days 1-2 (BDD), Lead Days 3-4 (Playwright), Co-lead Day 5
**Assigned Tasks:** All BDD feature files, test design patterns, CI/CD test step integration

### Dev Agent
**Responsibility:** Implementation, step definition code, code quality
**Daily Role:** Co-lead Days 1-2, Co-lead Days 3-4, Lead Day 5 (templates + validation)
**Assigned Tasks:** Step definition implementation, Playwright page objects, template updates

---

## 🗓️ DAILY BREAKDOWN WITH AGENT ASSIGNMENTS

### DAY 1 (Monday, Jan 27): BDD Framework Setup & First Tests
**Duration:** 6 hours | **Lead Agent:** QA Agent | **Support:** Dev Agent

#### Task 1.1: Initialize SpecFlow Project (1 hour)
**Assigned to:** QA Agent
**Deliverables:**
- [ ] Create `src/bmadServer.BDD.Tests/` directory structure
- [ ] Create `bmadServer.BDD.Tests.csproj` with references to:
  - SpecFlow 3.9+
  - SpecFlow.NUnit 3.9+
  - Gherkin 26+
  - BoDi (dependency injection)
- [ ] Add `specflow.json` configuration
- [ ] Verify project builds without errors

**Acceptance Criteria:**
- ✅ Project compiles
- ✅ NuGet packages installed
- ✅ Directory structure created: Features/, Steps/, Hooks/, Support/

#### Task 1.2: Create Hooks & Step Definitions Framework (1.5 hours)
**Assigned to:** Dev Agent
**Deliverables:**
- [ ] Create `Hooks/AuthenticationHooks.cs`
  - BeforeScenario: Set up test database context
  - AfterScenario: Clean up test data
- [ ] Create `Hooks/DatabaseHooks.cs`
  - SetupDatabase: Create test tables
  - TeardownDatabase: Drop test tables
- [ ] Create `Steps/AuthenticationSteps.cs` (empty template)
  - [Given] pattern methods (user registration, login)
  - [When] pattern methods (perform actions)
  - [Then] pattern methods (verify state)

**Acceptance Criteria:**
- ✅ All hooks compile
- ✅ Test database can be created and destroyed
- ✅ Step definition class has proper attributes

#### Task 1.3: Write First Feature File (0.5 hours)
**Assigned to:** QA Agent
**Deliverable:**
- [ ] Create `Features/2-1-user-registration.feature`
  - Scenario: Valid user registration
  - Scenario: Duplicate email registration
  - Scenario: Invalid password registration
  - All acceptance criteria from Story 2-1 mapped to Gherkin language

**Example Structure:**
```gherkin
Feature: User Registration
  As a new user
  I want to create an account with email and password
  So that I can access bmadServer

  Scenario: Valid user registration
    Given the API is running
    When I register with email "test@example.com" and password "SecurePass123!"
    Then the user is created successfully
    And the response contains user details
    And the password is hashed with bcrypt
```

**Acceptance Criteria:**
- ✅ Feature file is valid Gherkin syntax
- ✅ 5+ scenarios covering all acceptance criteria from Story 2-1
- ✅ File located at `Features/2-1-user-registration.feature`

#### Task 1.4: Implement Step Definitions for Story 2-1 (2 hours)
**Assigned to:** Dev Agent
**Deliverables:**
- [ ] Implement all [Given] steps for registration scenario
- [ ] Implement all [When] steps (POST to /api/v1/auth/register)
- [ ] Implement all [Then] steps (verify response, check database)
- [ ] Add API client helper class for HTTP calls
- [ ] Add database helper class for user lookup

**Code Pattern:**
```csharp
[Given(@"the API is running")]
public void ApiIsRunning()
{
    // Verify API is accessible
}

[When(@"I register with email ""(.*)"" and password ""(.*)""")]
public void RegisterUser(string email, string password)
{
    // Call POST /api/v1/auth/register with credentials
}

[Then(@"the user is created successfully")]
public void VerifyUserCreated()
{
    // Check database for user record
    // Verify password is hashed
}
```

**Acceptance Criteria:**
- ✅ All steps execute without errors
- ✅ First SpecFlow test runs successfully
- ✅ `dotnet test` command finds and executes tests

#### Task 1.5: Create Test Configuration & Runners (1 hour)
**Assigned to:** QA Agent
**Deliverables:**
- [ ] Create `appsettings.Development.json` (test database connection)
- [ ] Create `nunit.framework.json` with test configuration
- [ ] Create `hooks.xml` for test runner configuration
- [ ] Add `.github/workflows/bdd-tests.yml` (CI/CD step)

**Acceptance Criteria:**
- ✅ Tests can connect to test database
- ✅ Test runner discovers all SpecFlow tests
- ✅ CI/CD workflow file created

---

### DAY 2 (Tuesday, Jan 28): Complete Epic 2 BDD Tests
**Duration:** 7 hours | **Lead Agent:** QA Agent | **Support:** Dev Agent

#### Task 2.1: Write .feature Files for Stories 2-2 & 2-3 (2 hours)
**Assigned to:** QA Agent
**Deliverables:**
- [ ] Create `Features/2-2-jwt-login.feature`
  - Scenario: Valid login generates JWT
  - Scenario: Invalid credentials rejected
  - Scenario: JWT token validation
  - Scenario: Token expiration
  - Scenario: Tampered token rejected
- [ ] Create `Features/2-3-refresh-token.feature`
  - Scenario: Refresh token issued on login
  - Scenario: Token refresh generates new access token
  - Scenario: Expired refresh token rejected
  - Scenario: Revoked token rejected
  - Scenario: Concurrent refresh requests handled safely

**Acceptance Criteria:**
- ✅ 10+ combined scenarios for 2-2 and 2-3
- ✅ All acceptance criteria from stories mapped to Gherkin
- ✅ Feature files compile in SpecFlow

#### Task 2.2: Implement Step Definitions for Stories 2-2 & 2-3 (1 hour)
**Assigned to:** Dev Agent
**Deliverables:**
- [ ] Implement JWT validation steps in `Steps/AuthenticationSteps.cs`
- [ ] Implement refresh token steps
- [ ] Add JWT helper class for token parsing and validation
- [ ] Add cookie handling for refresh tokens

**Acceptance Criteria:**
- ✅ All 10+ steps execute
- ✅ JWT tokens can be generated and validated
- ✅ Refresh token flow works end-to-end

#### Task 2.3: Write .feature Files for Stories 2-4, 2-5, 2-6 (1 hour)
**Assigned to:** QA Agent
**Deliverables:**
- [ ] Create `Features/2-4-session-persistence.feature` (Session recovery, multi-device)
- [ ] Create `Features/2-5-rbac.feature` (Role assignment, authorization)
- [ ] Create `Features/2-6-idle-timeout.feature` (Idle detection, session extension)

**Acceptance Criteria:**
- ✅ 15+ combined scenarios
- ✅ All acceptance criteria mapped
- ✅ Session recovery scenarios include database persistence

#### Task 2.4: Implement Step Definitions for Stories 2-4, 2-5, 2-6 (2 hours)
**Assigned to:** Dev Agent
**Deliverables:**
- [ ] Implement session management steps
- [ ] Implement role assignment and verification steps
- [ ] Implement idle timeout and extension steps
- [ ] Add SignalR connection helper for session testing
- [ ] Add role assignment helper

**Acceptance Criteria:**
- ✅ All 15+ steps execute
- ✅ Session state can be persisted and recovered
- ✅ Role-based access works in tests

#### Task 2.5: Add BDD Tests to CI/CD Pipeline (1 hour)
**Assigned to:** QA Agent
**Deliverables:**
- [ ] Update `.github/workflows/ci.yml` to include BDD test step:
  ```yaml
  - name: Run BDD Tests (SpecFlow)
    run: dotnet test src/bmadServer.BDD.Tests --logger trx
  ```
- [ ] Configure test result reporting
- [ ] Add BDD test artifacts to CI/CD

**Acceptance Criteria:**
- ✅ CI/CD workflow includes BDD test execution
- ✅ BDD tests run after unit tests
- ✅ Workflow fails if BDD tests fail

---

### DAY 3 (Wednesday, Jan 29): Playwright Framework Setup
**Duration:** 7 hours | **Lead Agent:** QA Agent | **Support:** Dev Agent

#### Task 3.1: Initialize Playwright Project (1 hour)
**Assigned to:** QA Agent
**Deliverables:**
- [ ] Create `src/bmadServer.Web/e2e/` directory
- [ ] Run `npm install -D @playwright/test`
- [ ] Run `npx playwright install` (browser binaries)
- [ ] Create `package.json` scripts for E2E testing:
  ```json
  {
    "test:e2e": "playwright test",
    "test:e2e:ui": "playwright test --ui",
    "test:e2e:debug": "playwright test --debug"
  }
  ```

**Acceptance Criteria:**
- ✅ Playwright installed
- ✅ Browser binaries available
- ✅ npm scripts work

#### Task 3.2: Create playwright.config.ts Configuration (1 hour)
**Assigned to:** Dev Agent
**Deliverables:**
- [ ] Create `src/bmadServer.Web/playwright.config.ts` with:
  - Multi-browser configuration (Chromium, Firefox, Safari)
  - Base URL pointing to localhost development server
  - Screenshots on failure
  - Video recording for failed tests
  - Retries on failure (2 retries)
  - Test timeout (30 seconds)
  - Parallel execution settings
- [ ] Configure web server launch:
  ```typescript
  webServer: {
    command: 'npm run start',
    url: 'http://localhost:3000',
    reuseExistingServer: !process.env.CI
  }
  ```

**Acceptance Criteria:**
- ✅ Config file is valid TypeScript
- ✅ Browsers download successfully
- ✅ Config file has proper reporters (html, json)

#### Task 3.3: Create Fixtures for Test Reusability (1.5 hours)
**Assigned to:** Dev Agent
**Deliverables:**
- [ ] Create `e2e/fixtures/auth.fixture.ts`
  - authenticatedPage fixture
  - Login with test credentials automatically
  - Return page object with user context
- [ ] Create `e2e/fixtures/api.fixture.ts`
  - Token fixture for API calls
  - Generate valid JWT for requests
- [ ] Create `fixtures/index.ts` exporting all fixtures
- [ ] Document fixture usage

**Example Code:**
```typescript
// fixtures/auth.fixture.ts
import { test as base, Page } from '@playwright/test';

type AuthFixtures = {
  authenticatedPage: Page;
};

export const test = base.extend<AuthFixtures>({
  authenticatedPage: async ({ page }, use) => {
    // Login with test credentials
    await page.goto('http://localhost:3000/login');
    await page.fill('input[name="email"]', 'test@example.com');
    await page.fill('input[name="password"]', 'SecurePass123!');
    await page.click('button:has-text("Login")');
    
    // Wait for redirect to dashboard
    await page.waitForURL('**/dashboard');
    
    // Provide authenticated page to test
    await use(page);
    
    // Cleanup
    await page.context().clearCookies();
  }
});
```

**Acceptance Criteria:**
- ✅ Fixtures are properly typed in TypeScript
- ✅ Test can use authenticatedPage fixture
- ✅ Fixtures handle cleanup

#### Task 3.4: Create Page Objects for Maintainability (0.5 hours)
**Assigned to:** Dev Agent
**Deliverables:**
- [ ] Create `e2e/pages/login.page.ts` (example Page Object)
  ```typescript
  export class LoginPage {
    constructor(private page: Page) {}
    
    async goto() { await this.page.goto('http://localhost:3000/login'); }
    async fillEmail(email: string) { /* ... */ }
    async fillPassword(password: string) { /* ... */ }
    async clickLogin() { /* ... */ }
    async getErrorMessage() { /* ... */ }
  }
  ```
- [ ] Create `e2e/pages/index.ts` exporting all page objects
- [ ] Add documentation on Page Object pattern

**Acceptance Criteria:**
- ✅ Page Objects are properly structured
- ✅ Example Page Object works with fixtures
- ✅ Other team members can extend pattern

#### Task 3.5: Write Sample E2E Tests (1.5 hours)
**Assigned to:** QA Agent
**Deliverables:**
- [ ] Create `e2e/tests/2-1-user-registration.spec.ts`
  ```typescript
  import { test, expect } from '@playwright/test';
  
  test.describe('User Registration', () => {
    test('should register a new user', async ({ page }) => {
      await page.goto('http://localhost:3000/register');
      await page.fill('input[name="email"]', 'newuser@example.com');
      await page.fill('input[name="password"]', 'SecurePass123!');
      await page.click('button:has-text("Register")');
      
      await expect(page).toHaveURL('**/dashboard');
    });
  });
  ```
- [ ] Create tests for registration success and failure scenarios
- [ ] Test validation error messages

**Acceptance Criteria:**
- ✅ Tests are properly structured with describe blocks
- ✅ Tests use proper assertions (expect)
- ✅ At least 1 test passes when run

#### Task 3.6: Configure npm Test Scripts (1 hour)
**Assigned to:** QA Agent
**Deliverables:**
- [ ] Add to `package.json`:
  ```json
  {
    "test:e2e": "playwright test",
    "test:e2e:ui": "playwright test --ui",
    "test:e2e:debug": "playwright test --debug",
    "test:e2e:headed": "playwright test --headed"
  }
  ```
- [ ] Verify `npm run test:e2e` executes tests
- [ ] Create `.gitignore` entries for test artifacts

**Acceptance Criteria:**
- ✅ npm scripts are properly configured
- ✅ Tests run successfully via npm
- ✅ Test artifacts ignored by git

---

### DAY 4 (Thursday, Jan 30): Complete Playwright E2E Tests & CI/CD
**Duration:** 7 hours | **Lead Agent:** QA Agent | **Support:** Dev Agent

#### Task 4.1: Write E2E Tests for Stories 2-2 to 2-6 (3 hours)
**Assigned to:** QA Agent
**Deliverables:**
- [ ] Create `e2e/tests/2-2-jwt-login.spec.ts`
  - Test valid login flow
  - Test invalid credentials error
  - Test JWT token in response
- [ ] Create `e2e/tests/2-3-refresh-token.spec.ts`
  - Test token refresh after expiry
  - Test cookie handling
- [ ] Create `e2e/tests/2-4-session-recovery.spec.ts`
  - Test session persistence across refresh
  - Test multi-device sessions
- [ ] Create `e2e/tests/2-5-rbac-roles.spec.ts`
  - Test role-based access to endpoints
  - Test permission denied message
- [ ] Create `e2e/tests/2-6-idle-timeout.spec.ts`
  - Test idle timeout warning modal
  - Test extend session action
  - Test logout on timeout

**Total:** 5 spec files with 30+ test cases

**Acceptance Criteria:**
- ✅ All 5 spec files created
- ✅ Each file has 6+ test cases
- ✅ Tests use Page Objects and fixtures
- ✅ Tests pass when run locally

#### Task 4.2: Create Shared Test Utilities (1 hour)
**Assigned to:** Dev Agent
**Deliverables:**
- [ ] Create `e2e/helpers/api-helper.ts`
  - Helper to make authenticated API calls
  - Helper to generate JWT tokens
  - Helper to manage test data
- [ ] Create `e2e/helpers/database-helper.ts`
  - Helper to create test users
  - Helper to clean up test data
- [ ] Create `e2e/helpers/auth-helper.ts`
  - Helper for login/logout flows
  - Helper for token management

**Acceptance Criteria:**
- ✅ Helpers are properly exported
- ✅ Helpers are used in test files
- ✅ Code is DRY (no duplication)

#### Task 4.3: Integrate Playwright into CI/CD (1.5 hours)
**Assigned to:** QA Agent
**Deliverables:**
- [ ] Update `.github/workflows/ci.yml` to:
  ```yaml
  - name: Start API server
    run: npm start &
    
  - name: Wait for API to be ready
    run: npx wait-on http://localhost:8080/health
    
  - name: Run Playwright E2E Tests
    run: npm run test:e2e
    
  - name: Upload test artifacts
    if: always()
    uses: actions/upload-artifact@v3
    with:
      name: playwright-report
      path: playwright-report/
  ```
- [ ] Configure test artifact storage
- [ ] Add HTML report generation

**Acceptance Criteria:**
- ✅ CI/CD workflow includes E2E test step
- ✅ API server starts before tests
- ✅ Test artifacts uploaded on failure

#### Task 4.4: Document E2E Testing Guide (1.5 hours)
**Assigned to:** Dev Agent
**Deliverables:**
- [ ] Create `src/bmadServer.Web/E2E-TESTING.md` with:
  - Setup instructions
  - How to write new tests
  - Page Object pattern explanation
  - Fixture usage guide
  - Debugging tips
  - Common assertions reference

**Acceptance Criteria:**
- ✅ Documentation is clear and complete
- ✅ Examples work as written
- ✅ Team can follow guide to write new tests

---

### DAY 5 (Friday, Jan 31): Story Template Update & Validation
**Duration:** 5 hours | **Lead Agent:** Dev Agent | **Support:** QA Agent

#### Task 5.1: Update Story Template (1.5 hours)
**Assigned to:** Dev Agent
**Deliverables:**
- [ ] Update `_bmad-output/implementation-artifacts/story-template.md` to include:
  ```markdown
  ## BDD Tests
  - Feature file: `src/bmadServer.BDD.Tests/Features/{story-id}-{title}.feature`
  - Step definitions in: `src/bmadServer.BDD.Tests/Steps/`
  - Scenarios: [List 5+ scenarios]
  
  ## Playwright E2E Tests (if UI story)
  - Test file: `src/bmadServer.Web/e2e/tests/{story-id}-{title}.spec.ts`
  - Page Objects: [List Page Objects used]
  - Test cases: [List 5+ test cases]
  
  ## Definition of Done
  - [ ] All acceptance criteria implemented
  - [ ] Unit tests passing (90%+ coverage)
  - [ ] BDD tests passing (all scenarios)
  - [ ] Playwright E2E tests passing (all scenarios) - if UI story
  - [ ] Code reviewed and approved
  - [ ] CI/CD passing (all checks)
  ```
- [ ] Update PR template to include test checkboxes

**Acceptance Criteria:**
- ✅ Template is updated and saved
- ✅ New stories will use updated template
- ✅ PR template includes test requirements

#### Task 5.2: Create PR Template Update (0.5 hours)
**Assigned to:** QA Agent
**Deliverables:**
- [ ] Update `.github/pull_request_template.md` to require:
  ```markdown
  ## Testing
  - [ ] BDD tests written and passing
  - [ ] Playwright E2E tests written and passing (if UI)
  - [ ] Unit tests passing
  - [ ] CI/CD workflow passed
  
  ## Checklist
  - [ ] Code follows project conventions
  - [ ] Tests added/updated
  - [ ] Documentation updated
  ```

**Acceptance Criteria:**
- ✅ PR template updated
- ✅ All new PRs will include test requirements

#### Task 5.3: Run All Tests End-to-End Validation (1 hour)
**Assigned to:** QA Agent
**Deliverables:**
- [ ] Run all unit tests: `dotnet test` (should pass ~150+ tests)
- [ ] Run all BDD tests: `dotnet test src/bmadServer.BDD.Tests` (should pass 30+ scenarios)
- [ ] Run all E2E tests: `npm run test:e2e` (should pass 30+ tests)
- [ ] Document test execution results

**Acceptance Criteria:**
- ✅ All unit tests pass
- ✅ All BDD tests pass
- ✅ All Playwright tests pass
- ✅ No flaky tests
- ✅ Results documented

#### Task 5.4: Team Training & Documentation (1.5 hours)
**Assigned to:** Dev Agent
**Deliverables:**
- [ ] Create `TESTING-GUIDE.md` with three-level testing explanation:
  - Level 1: Unit tests (individual functions)
  - Level 2: BDD tests (acceptance criteria)
  - Level 3: Playwright E2E tests (user workflows)
- [ ] Prepare training examples for Epic 3 stories
- [ ] Create test checklist for future stories
- [ ] Document common test patterns

**Acceptance Criteria:**
- ✅ Guide is comprehensive
- ✅ Team understands three-level testing
- ✅ Examples are ready for Epic 3

#### Task 5.5: Create Examples for Epic 3 (0.5 hours)
**Assigned to:** QA Agent
**Deliverables:**
- [ ] Create template examples for Epic 3 Story 3.1 (SignalR setup):
  - Example BDD feature file structure
  - Example Playwright test structure
  - Example Page Object for chat interface
- [ ] Store in `_bmad-output/examples/` for reference

**Acceptance Criteria:**
- ✅ Examples are clear
- ✅ Ready for Epic 3 story generation

---

## ✅ PREP SPRINT SUCCESS CRITERIA

### BDD Framework ✅
- [ ] SpecFlow initialized
- [ ] 6 .feature files created (30+ scenarios total)
- [ ] All step definitions implemented
- [ ] Tests pass locally
- [ ] Integrated into CI/CD

### Playwright Framework ✅
- [ ] Playwright installed and configured
- [ ] 6 .spec.ts files created (30+ test cases total)
- [ ] Fixtures created for reusability
- [ ] Page Objects created for maintainability
- [ ] Tests pass locally
- [ ] Integrated into CI/CD

### Documentation & Training ✅
- [ ] Story template updated
- [ ] PR template updated
- [ ] TESTING-GUIDE.md created
- [ ] E2E-TESTING.md created
- [ ] Team trained
- [ ] Examples created for Epic 3

### Quality Gates ✅
- [ ] All unit tests pass (~150+)
- [ ] All BDD tests pass (30+ scenarios)
- [ ] All Playwright tests pass (30+ test cases)
- [ ] No flaky tests
- [ ] CI/CD passing

---

## 🚀 DELIVERABLES SUMMARY

| Artifact | Count | Location |
|----------|-------|----------|
| BDD Feature Files | 6 | `src/bmadServer.BDD.Tests/Features/` |
| BDD Scenarios | 30+ | In .feature files |
| Playwright Test Files | 6 | `src/bmadServer.Web/e2e/tests/` |
| Playwright Test Cases | 30+ | In .spec.ts files |
| Page Objects | 6+ | `e2e/pages/` |
| Fixtures | 3 | `e2e/fixtures/` |
| Test Utilities | 3 | `e2e/helpers/` |
| Documentation Files | 3 | TESTING-GUIDE.md, E2E-TESTING.md, updated templates |
| CI/CD Workflows Updated | 1 | `.github/workflows/ci.yml` |

**Total Test Coverage:** 60+ tests + 60+ BDD scenarios = **120+ automated test assertions**

---

## 📝 NOTES FOR AGENTS

### For QA Agent
- Lead test design and BDD structure
- Ensure all acceptance criteria are covered
- Write Gherkin scenarios clearly for developers
- Validate all tests pass before handoff
- Create comprehensive test documentation

### For Dev Agent
- Implement step definitions to match QA's Gherkin
- Write clean, maintainable Playwright tests
- Use Page Objects and fixtures properly
- Document test utilities for team
- Update templates and configuration files

### For Both Agents
- Communicate status daily
- Flag blockers immediately
- Ensure CI/CD integration works
- Validate no flaky tests
- Prepare Epic 3 for immediate handoff on Feb 3

