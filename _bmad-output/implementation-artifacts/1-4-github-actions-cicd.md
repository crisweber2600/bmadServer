# Story 1.4: Configure GitHub Actions CI/CD Pipeline

**Status:** ready-for-dev

## Story

As a developer,
I want automated CI/CD,
so that every commit triggers build, test, and deployment checks.

## Acceptance Criteria

**Given** I have a GitHub repository  
**When** I create `.github/workflows/ci.yml`  
**Then** the workflow file is valid YAML and defines:
  - Trigger: on: [push, pull_request] to all branches
  - Jobs: build, test, (deploy on main only)

**Given** the workflow file exists  
**When** I review the build job  
**Then** it includes:
  - Checkout code: uses: actions/checkout@v4
  - Setup .NET: uses: actions/setup-dotnet@v4 with dotnet-version: 10.0
  - Restore dependencies: `dotnet restore`
  - Build project: `dotnet build --configuration Release`  
**And** the job succeeds or fails with clear error messages

**Given** the build job completes  
**When** I review the test job  
**Then** it includes:
  - Run unit tests: `dotnet test --configuration Release --logger trx`
  - Report test results: upload .trx files as artifacts
  - Fail job if any tests fail
  - Job runs only if build succeeds (depends_on: build)

**Given** the pipeline is configured  
**When** I push a commit to a branch  
**Then** GitHub Actions automatically:
  - Checks out the code
  - Builds the solution (passes/fails)
  - Runs all unit tests (passes/fails)
  - Reports results in the PR

**Given** a PR is created  
**When** I review the PR checks section  
**Then** I see:
  - build job status (passed/failed)
  - test job status (passed/failed)  
**And** merge is blocked if any checks fail

**Given** a commit is merged to main  
**When** the workflow completes  
**Then** the build succeeds  
**And** the tests pass  
**And** (optional) Docker image is built and tagged with commit SHA  
**And** (optional) image is pushed to container registry

**Given** the CI/CD is operational  
**When** I review `.github/workflows/` directory  
**Then** I find documentation comments explaining:
  - When each job runs
  - What each step does
  - How to modify triggers or add new jobs

**Given** a developer makes a breaking change  
**When** they push to a branch  
**Then** the CI/CD catches the error and reports it in the PR  
**And** they can fix and re-push without manual intervention

## Tasks / Subtasks

- [ ] **Task 1: Create GitHub Actions workflow file** (AC: #1)
  - [ ] Create .github/workflows/ directory
  - [ ] Create ci.yml workflow file
  - [ ] Define triggers (push, pull_request)
  - [ ] Add workflow name and concurrency settings
  - [ ] Validate YAML syntax

- [ ] **Task 2: Configure build job** (AC: #2)
  - [ ] Add build job definition
  - [ ] Use actions/checkout@v4
  - [ ] Use actions/setup-dotnet@v4 with version 10.0
  - [ ] Add dotnet restore step
  - [ ] Add dotnet build --configuration Release step
  - [ ] Test job runs successfully

- [ ] **Task 3: Configure test job** (AC: #3)
  - [ ] Add test job with needs: [build]
  - [ ] Run dotnet test with trx logger
  - [ ] Upload test results as artifacts
  - [ ] Configure job to fail on test failures
  - [ ] Add test result reporting

- [ ] **Task 4: Create basic unit tests** (AC: #4)
  - [ ] Create bmadServer.Tests project
  - [ ] Add reference to bmadServer.ApiService
  - [ ] Add xUnit and testing packages
  - [ ] Create sample health check test
  - [ ] Verify tests run locally

- [ ] **Task 5: Configure PR checks** (AC: #5)
  - [ ] Enable branch protection rules on main
  - [ ] Require CI checks to pass before merge
  - [ ] Configure status checks as required
  - [ ] Test PR workflow end-to-end

- [ ] **Task 6: Add Docker build job (optional)** (AC: #6)
  - [ ] Add docker-build job on main branch only
  - [ ] Build Docker image using Dockerfile
  - [ ] Tag with commit SHA and latest
  - [ ] (Optional) Push to container registry
  - [ ] Add conditional: if github.ref == 'refs/heads/main'

- [ ] **Task 7: Document CI/CD workflow** (AC: #7-8)
  - [ ] Add inline comments to ci.yml
  - [ ] Document job dependencies
  - [ ] Document how to extend workflow
  - [ ] Add troubleshooting guide

## Dev Notes

### GitHub Actions Workflow Template

```yaml
name: CI/CD Pipeline

on:
  push:
    branches: ['**']
  pull_request:
    branches: ['**']

concurrency:
  group: ${{ github.workflow }}-${{ github.ref }}
  cancel-in-progress: true

jobs:
  build:
    name: Build
    runs-on: ubuntu-latest
    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Restore dependencies
        run: dotnet restore

      - name: Build
        run: dotnet build --configuration Release --no-restore

  test:
    name: Test
    runs-on: ubuntu-latest
    needs: build
    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Restore dependencies
        run: dotnet restore

      - name: Run tests
        run: dotnet test --configuration Release --no-restore --logger trx --results-directory TestResults

      - name: Upload test results
        uses: actions/upload-artifact@v4
        if: always()
        with:
          name: test-results
          path: TestResults/*.trx

  docker:
    name: Docker Build
    runs-on: ubuntu-latest
    needs: [build, test]
    if: github.ref == 'refs/heads/main'
    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Build Docker image
        run: |
          docker build -t bmadserver:${{ github.sha }} -f bmadServer.ApiService/Dockerfile .
          docker tag bmadserver:${{ github.sha }} bmadserver:latest
```

### Test Project Setup

```bash
# Create test project
dotnet new xunit -n bmadServer.Tests
dotnet sln add bmadServer.Tests/bmadServer.Tests.csproj
dotnet add bmadServer.Tests/bmadServer.Tests.csproj reference bmadServer.ApiService/bmadServer.ApiService.csproj
```

### Architecture Alignment

Per architecture.md requirements:
- CI/CD: GitHub Actions + Docker Build + Push ✅
- Build triggers on push and PR ✅
- Test reporting with artifacts ✅

### Dependencies

- **Depends on**: Story 1-1 (project structure), Story 1-3 (Dockerfile)
- **Enables**: Automated quality gates for all future stories

### References

- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [actions/setup-dotnet](https://github.com/actions/setup-dotnet)
- [Epic 1 Story 1.4](../../planning-artifacts/epics.md#story-14-configure-github-actions-cicd-pipeline)

## Dev Agent Record

### Agent Model Used

Claude 3.5 Sonnet

### Completion Notes List

- Story created with full acceptance criteria
- Complete workflow template included
- Test project setup documented

### File List

- /Users/cris/bmadServer/.github/workflows/ci.yml (create)
- /Users/cris/bmadServer/bmadServer.Tests/bmadServer.Tests.csproj (create)
- /Users/cris/bmadServer/bmadServer.Tests/HealthCheckTests.cs (create)
