using Reqnroll;

namespace bmadServer.BDD.Tests;

[Binding]
public class Hooks
{
    [BeforeTestRun]
    public static void BeforeTestRun()
    {
        // Test run initialization
    }

    [BeforeScenario]
    public void BeforeScenario()
    {
        // Scenario initialization
    }

    [AfterScenario]
    public void AfterScenario()
    {
        // Scenario cleanup
    }
}
