Feature: GitHub Actions CI/CD Pipeline Configuration
  As a developer
  I want automated CI/CD
  So that every commit triggers build, test, and deployment checks

  @timeout:30000
  Scenario: Workflow file is valid YAML with correct structure
    Given I have a GitHub repository
    When I check the workflow file at ".github/workflows/ci.yml"
    Then the workflow file exists
    And the workflow file is valid YAML
    And the workflow defines a trigger for "push" events
    And the workflow defines a trigger for "pull_request" events
    And the workflow defines a job named "build"
    And the workflow defines a job named "test"

  @timeout:30000
  Scenario: Build job is configured correctly
    Given the workflow file exists
    When I review the build job configuration
    Then it includes a checkout step using "actions/checkout@v4"
    And it includes a setup .NET step using "actions/setup-dotnet@v4"
    And the .NET version is configured as "10.0.x"
    And it includes a "dotnet restore" step
    And it includes a "dotnet build --configuration Release" step

  @timeout:30000
  Scenario: Test job is configured correctly
    Given the build job completes successfully
    When I review the test job configuration
    Then it depends on the build job
    And it includes a "dotnet test" step with "--logger trx" parameter
    And it uploads test results as artifacts
    And it is configured to fail when tests fail

  @timeout:45000
  Scenario: Unit tests exist and pass locally
    Given I have a test project
    When I run the unit tests locally
    Then all unit tests should pass
    And test results should be generated in TRX format

  @timeout:30000
  Scenario: Branch protection requires CI checks
    Given the CI/CD pipeline is configured
    When I check the branch protection rules for main branch
    Then the build check should be required
    And the test check should be required

  @timeout:30000
  Scenario: Workflow documentation is comprehensive
    Given the CI/CD is operational
    When I review the workflow file
    Then it should contain documentation comments
    And comments should explain when each job runs
    And comments should explain what each step does
    And comments should explain how to extend the workflow
