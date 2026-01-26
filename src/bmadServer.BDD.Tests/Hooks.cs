using Reqnroll;
using Xunit;

namespace bmadServer.BDD.Tests;

[Binding]
public class Hooks
{
    private readonly ScenarioContext _scenarioContext;

    public Hooks(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    [BeforeTestRun]
    public static void BeforeTestRun()
    {
    }

    [BeforeScenario]
    public void BeforeScenario()
    {
        if (_scenarioContext.ScenarioInfo.Tags.Contains("skip"))
        {
            Skip.If(true, "Scenario marked as @skip - requires full step execution implementation");
        }
    }

    [AfterScenario]
    public void AfterScenario()
    {
    }
}
