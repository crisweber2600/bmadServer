# BDD Tests with Reqnroll

This directory contains Behavior-Driven Development (BDD) tests using **Reqnroll** (the modern fork of SpecFlow) and xUnit.

## Overview

BDD tests are written in Gherkin syntax using Given-When-Then format to describe acceptance criteria in a human-readable way.

### Why Reqnroll?

- **Modern & Actively Maintained**: Reqnroll is the actively maintained open-source fork of SpecFlow
- **Better .NET Support**: Full support for .NET 10.0 and latest .NET features
- **xUnit Integration**: Seamless integration with xUnit test runner
- **Timeout Support**: Built-in support for test timeouts to prevent hanging tests

## Running Tests

### Locally

```bash
# Run all BDD tests
cd src
dotnet test bmadServer.BDD.Tests/bmadServer.BDD.Tests.csproj --configuration Release

# Run with verbose output
dotnet test bmadServer.BDD.Tests/bmadServer.BDD.Tests.csproj --configuration Release --logger "console;verbosity=detailed"

# Run with timeout (60 seconds)
timeout 60 dotnet test bmadServer.BDD.Tests/bmadServer.BDD.Tests.csproj --configuration Release
```

### In CI/CD

The BDD tests are automatically run as part of the GitHub Actions CI/CD pipeline in the `test` job. The workflow includes:
- **5-minute timeout** at the workflow level (`timeout-minutes: 5`)
- **300-second timeout** at the command level (`timeout 300`)
- **Per-scenario timeouts** defined in feature files using `@timeout` tags

## Test Structure

```
bmadServer.BDD.Tests/
├── Features/                    # Gherkin feature files (.feature)
│   └── GitHubActionsCICD.feature
├── StepDefinitions/             # C# step definition implementations
│   └── GitHubActionsCICDSteps.cs
├── Hooks.cs                     # Test lifecycle hooks
├── reqnroll.json                # Reqnroll configuration
└── bmadServer.BDD.Tests.csproj  # Project file
```

## Writing New Tests

### 1. Create a Feature File

Create a new `.feature` file in the `Features/` directory:

```gherkin
Feature: My New Feature
  As a user
  I want some functionality
  So that I can achieve a goal

  @timeout:30000
  Scenario: Basic scenario
    Given I have a precondition
    When I perform an action
    Then I should see the expected result
```

### 2. Implement Step Definitions

Create or update a step definition class in `StepDefinitions/`:

```csharp
using Reqnroll;
using Xunit;

namespace bmadServer.BDD.Tests.StepDefinitions;

[Binding]
public class MyFeatureSteps
{
    [Given(@"I have a precondition")]
    public void GivenIHaveAPrecondition()
    {
        // Setup code
    }

    [When(@"I perform an action")]
    public void WhenIPerformAnAction()
    {
        // Action code
    }

    [Then(@"I should see the expected result")]
    public void ThenIShouldSeeTheExpectedResult()
    {
        // Assertion code
        Assert.True(true);
    }
}
```

### 3. Important: Escaping Special Characters

When your scenarios contain special regex characters (like `/` in "CI/CD"), **you must escape them** in the step definition:

```csharp
// ❌ Wrong - will not match
[Given(@"the CI/CD pipeline is configured")]

// ✅ Correct - escapes the forward slash
[Given(@"the CI\/CD pipeline is configured")]
```

## Timeout Configuration

### Per-Scenario Timeouts

Use the `@timeout` tag in feature files (milliseconds):

```gherkin
@timeout:30000
Scenario: My scenario that might take longer
  Given...
```

### Test-Level Timeouts

In step definitions, use `CancellationToken` for async operations:

```csharp
[When(@"I run a long operation")]
public void WhenIRunALongOperation()
{
    var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
    // Use timeout.Token in async operations
}
```

### CI/CD Timeouts

The CI/CD pipeline has multiple timeout layers:
1. **Job-level**: 5 minutes (`timeout-minutes: 5`)
2. **Command-level**: 5 minutes (`timeout 300`)
3. **Per-test**: Defined in feature files

## Test Execution Flow

1. **Build**: Reqnroll generates `.feature.cs` files from `.feature` files during build
2. **Discovery**: xUnit discovers the generated test methods
3. **Execution**: Tests run with timeout enforcement at multiple levels
4. **Reporting**: Results output in TRX format and console

## Troubleshooting

### Tests Timeout

- Check `@timeout` tags in feature files
- Verify step definitions don't have infinite loops
- Look for missing `WaitForExit` timeouts in Process calls

### Step Definition Not Found

- Ensure `[Binding]` attribute is on the step definition class
- Check for special characters that need escaping in regex
- Verify the step text exactly matches between feature and definition
- Rebuild to regenerate `.feature.cs` files

### Build Failures

```bash
# Clean and rebuild
cd src/bmadServer.BDD.Tests
dotnet clean
rm -rf bin obj Features/*.cs
cd ..
dotnet build bmadServer.BDD.Tests/bmadServer.BDD.Tests.csproj
```

## Dependencies

- **Reqnroll**: v2.2.0 - Core BDD framework
- **Reqnroll.xUnit**: v2.2.0 - xUnit integration
- **xUnit**: v2.9.3 - Test runner
- **YamlDotNet**: v16.2.1 - YAML parsing for workflow validation

## Best Practices

1. ✅ **Use timeout tags** on all scenarios
2. ✅ **Escape special characters** in regex patterns
3. ✅ **Keep steps focused** - one assertion per Then step
4. ✅ **Make scenarios independent** - don't rely on execution order
5. ✅ **Use descriptive names** - scenarios should read like documentation
6. ✅ **Timeout long operations** - prevent hanging tests
7. ✅ **Handle cleanup** - use hooks for teardown

## References

- [Reqnroll Documentation](https://docs.reqnroll.net/)
- [Gherkin Syntax](https://cucumber.io/docs/gherkin/reference/)
- [xUnit Documentation](https://xunit.net/)
