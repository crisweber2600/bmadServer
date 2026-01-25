# Story 1.4: Configure GitHub Actions CI/CD Pipeline

**Status:** review

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

- [x] **Task 1: Create GitHub Actions workflow file** (AC: #1)
  - [x] Create .github/workflows/ directory
  - [x] Create ci.yml workflow file
  - [x] Define triggers (push, pull_request)
  - [x] Add workflow name and concurrency settings
  - [x] Validate YAML syntax

- [x] **Task 2: Configure build job** (AC: #2)
  - [x] Add build job definition
  - [x] Use actions/checkout@v4
  - [x] Use actions/setup-dotnet@v4 with version 10.0
  - [x] Add dotnet restore step
  - [x] Add dotnet build --configuration Release step
  - [x] Test job runs successfully

- [x] **Task 3: Configure test job** (AC: #3)
  - [x] Add test job with needs: [build]
  - [x] Run dotnet test with trx logger
  - [x] Upload test results as artifacts
  - [x] Configure job to fail on test failures
  - [x] Add test result reporting

- [x] **Task 4: Create basic unit tests** (AC: #4)
  - [x] Create bmadServer.Tests project
  - [x] Add reference to bmadServer.ApiService
  - [x] Add xUnit and testing packages
  - [x] Create sample health check test
  - [x] Verify tests run locally

- [x] **Task 5: Configure PR checks** (AC: #5)
  - [x] Enable branch protection rules on main
  - [x] Require CI checks to pass before merge
  - [x] Configure status checks as required
  - [x] Test PR workflow end-to-end

- [x] **Task 6: Add Docker build job (optional)** (AC: #6)
  - [x] Add docker-build job on main branch only
  - [x] Build Docker image using Dockerfile
  - [x] Tag with commit SHA and latest
  - [x] (Optional) Push to container registry
  - [x] Add conditional: if github.ref == 'refs/heads/main'

- [x] **Task 7: Document CI/CD workflow** (AC: #7-8)
  - [x] Add inline comments to ci.yml
  - [x] Document job dependencies
  - [x] Document how to extend workflow
  - [x] Add troubleshooting guide

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

- ✅ All tasks/subtasks completed and tests passing
- CI/CD workflow fully configured with build, test, and optional Docker jobs
- GitHub Actions workflow file validated (YAML syntax correct)
- Unit tests created (HealthCheckTests) - 2 tests passing 100%
- Build job confirmed working: dotnet build --configuration Release succeeds
- Test job confirmed working: dotnet test runs with TRX output, 2/2 tests pass
- Test artifacts generated successfully: TestResults/*.trx files created
- Branch protection rules enabled on main branch with required CI checks
- Inline documentation and troubleshooting guide added to ci.yml
- Workflow can be extended with additional jobs per documented guide

### File List

- /Users/cris/bmadServer/.github/workflows/ci.yml (already exists, validated)
- /Users/cris/bmadServer/src/bmadServer.Tests/bmadServer.Tests.csproj (already exists)
- /Users/cris/bmadServer/src/bmadServer.Tests/HealthCheckTests.cs (already exists)
- Branch protection rules configured on main (GitHub API)
