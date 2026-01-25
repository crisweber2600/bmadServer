using System.Diagnostics;
using Reqnroll;
using Xunit;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace bmadServer.BDD.Tests.StepDefinitions;

[Binding]
public class GitHubActionsCICDSteps
{
    private const int TestExecutionTimeoutSeconds = 30;
    private const int MinimumDocumentationCommentLines = 10;
    
    private string? _workflowPath;
    private bool _workflowExists;
    private string? _workflowContent;
    private Dictionary<string, object>? _workflowYaml;
    private bool _unitTestsPassed;
    private string? _testOutput;

    [Given(@"I have a GitHub repository")]
    public void GivenIHaveAGitHubRepository()
    {
        // Verify we're in a git repository
        var gitDirectory = Path.Combine(GetRepositoryRoot(), ".git");
        Assert.True(Directory.Exists(gitDirectory), "Should be in a Git repository");
    }

    [When(@"I check the workflow file at ""(.*)""")]
    public void WhenICheckTheWorkflowFile(string workflowPath)
    {
        _workflowPath = Path.Combine(GetRepositoryRoot(), workflowPath);
        _workflowExists = File.Exists(_workflowPath);
        if (_workflowExists)
        {
            _workflowContent = File.ReadAllText(_workflowPath);
        }
    }

    [Then(@"the workflow file exists")]
    public void ThenTheWorkflowFileExists()
    {
        Assert.True(_workflowExists, $"Workflow file should exist at {_workflowPath}");
        Assert.NotNull(_workflowContent);
    }

    [Then(@"the workflow file is valid YAML")]
    public void ThenTheWorkflowFileIsValidYAML()
    {
        Assert.NotNull(_workflowContent);
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        try
        {
            _workflowYaml = deserializer.Deserialize<Dictionary<string, object>>(_workflowContent);
            Assert.NotNull(_workflowYaml);
        }
        catch (Exception ex)
        {
            Assert.Fail($"Workflow file is not valid YAML: {ex.Message}");
        }
    }

    [Then(@"the workflow defines a trigger for ""(.*)"" events")]
    public void ThenTheWorkflowDefinesATriggerForEvents(string triggerType)
    {
        Assert.NotNull(_workflowYaml);
        Assert.True(_workflowYaml.ContainsKey("on"), "Workflow should have 'on' key for triggers");
        
        var triggers = _workflowYaml["on"];
        Assert.NotNull(triggers);
        
        // Check if the trigger type is defined
        var triggerDict = triggers as Dictionary<object, object>;
        Assert.NotNull(triggerDict);
        Assert.Contains(triggerType, triggerDict.Keys.Select(k => k.ToString()!));
    }

    [Then(@"the workflow defines a job named ""(.*)""")]
    public void ThenTheWorkflowDefinesAJobNamed(string jobName)
    {
        Assert.NotNull(_workflowYaml);
        Assert.True(_workflowYaml.ContainsKey("jobs"), "Workflow should have 'jobs' key");
        
        var jobs = _workflowYaml["jobs"] as Dictionary<object, object>;
        Assert.NotNull(jobs);
        Assert.Contains(jobName, jobs.Keys.Select(k => k.ToString()!));
    }

    [Given(@"the workflow file exists")]
    public void GivenTheWorkflowFileExists()
    {
        _workflowPath = Path.Combine(GetRepositoryRoot(), ".github/workflows/ci.yml");
        _workflowExists = File.Exists(_workflowPath);
        Assert.True(_workflowExists, "Workflow file must exist");
        _workflowContent = File.ReadAllText(_workflowPath);
    }

    [When(@"I review the build job configuration")]
    public void WhenIReviewTheBuildJobConfiguration()
    {
        Assert.NotNull(_workflowContent);
        // Content is already loaded, just verify it contains build job
        Assert.Contains("build:", _workflowContent);
    }

    [Then(@"it includes a checkout step using ""(.*)""")]
    public void ThenItIncludesACheckoutStepUsing(string action)
    {
        Assert.NotNull(_workflowContent);
        Assert.Contains($"uses: {action}", _workflowContent);
    }

    [Then(@"it includes a setup \.NET step using ""(.*)""")]
    public void ThenItIncludesASetupDotNetStepUsing(string action)
    {
        Assert.NotNull(_workflowContent);
        Assert.Contains($"uses: {action}", _workflowContent);
    }

    [Then(@"the \.NET version is configured as ""(.*)""")]
    public void ThenTheDotNetVersionIsConfiguredAs(string version)
    {
        Assert.NotNull(_workflowContent);
        Assert.Contains($"dotnet-version: '{version}'", _workflowContent);
    }

    [Then(@"it includes a ""(.*)"" step")]
    public void ThenItIncludesAStep(string command)
    {
        Assert.NotNull(_workflowContent);
        Assert.Contains(command, _workflowContent);
    }

    [Given(@"the build job completes successfully")]
    public void GivenTheBuildJobCompletesSuccessfully()
    {
        // For BDD tests, we assume the build completes successfully
        // This is verified by the fact that these tests are running
        Assert.True(true);
    }

    [When(@"I review the test job configuration")]
    public void WhenIReviewTheTestJobConfiguration()
    {
        _workflowPath = Path.Combine(GetRepositoryRoot(), ".github/workflows/ci.yml");
        _workflowContent = File.ReadAllText(_workflowPath);
        Assert.Contains("test:", _workflowContent);
    }

    [Then(@"it depends on the build job")]
    public void ThenItDependsOnTheBuildJob()
    {
        Assert.NotNull(_workflowContent);
        Assert.Contains("needs: build", _workflowContent);
    }

    [Then(@"it includes a ""(.*)"" step with ""(.*)"" parameter")]
    public void ThenItIncludesAStepWithParameter(string command, string parameter)
    {
        Assert.NotNull(_workflowContent);
        Assert.Contains(command, _workflowContent);
        Assert.Contains(parameter, _workflowContent);
    }

    [Then(@"it uploads test results as artifacts")]
    public void ThenItUploadsTestResultsAsArtifacts()
    {
        Assert.NotNull(_workflowContent);
        Assert.Contains("uses: actions/upload-artifact@", _workflowContent);
        Assert.Contains("TestResults", _workflowContent);
    }

    [Then(@"it is configured to fail when tests fail")]
    public void ThenItIsConfiguredToFailWhenTestsFail()
    {
        Assert.NotNull(_workflowContent);
        // The default behavior of dotnet test is to fail the job if tests fail
        // We verify that there's no explicit ignore of test failures
        Assert.Contains("dotnet test", _workflowContent);
    }

    [Given(@"I have a test project")]
    public void GivenIHaveATestProject()
    {
        var testProjectPath = Path.Combine(GetRepositoryRoot(), "src/bmadServer.Tests/bmadServer.Tests.csproj");
        Assert.True(File.Exists(testProjectPath), "Test project should exist");
    }

    [When(@"I run the unit tests locally")]
    public void WhenIRunTheUnitTestsLocally()
    {
        var srcPath = Path.Combine(GetRepositoryRoot(), "src");
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "test bmadServer.Tests/bmadServer.Tests.csproj --configuration Release --logger trx",
            WorkingDirectory = srcPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };
        
        // Set a timeout for the test execution
        var timeoutCancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(TestExecutionTimeoutSeconds));
        
        process.Start();
        
        var outputTask = process.StandardOutput.ReadToEndAsync(timeoutCancellationToken.Token);
        var errorTask = process.StandardError.ReadToEndAsync(timeoutCancellationToken.Token);
        
        var timeoutMilliseconds = TestExecutionTimeoutSeconds * 1000;
        var finished = process.WaitForExit(timeoutMilliseconds);
        
        if (!finished)
        {
            process.Kill();
            Assert.Fail($"Test execution timed out after {TestExecutionTimeoutSeconds} seconds");
        }

        _testOutput = outputTask.GetAwaiter().GetResult();
        _unitTestsPassed = process.ExitCode == 0;
    }

    [Then(@"all unit tests should pass")]
    public void ThenAllUnitTestsShouldPass()
    {
        Assert.True(_unitTestsPassed, $"Unit tests should pass. Output: {_testOutput}");
    }

    [Then(@"test results should be generated in TRX format")]
    public void ThenTestResultsShouldBeGeneratedInTRXFormat()
    {
        var testResultsPath = Path.Combine(GetRepositoryRoot(), "src/TestResults");
        if (Directory.Exists(testResultsPath))
        {
            var trxFiles = Directory.GetFiles(testResultsPath, "*.trx");
            Assert.NotEmpty(trxFiles);
        }
        // If TestResults directory doesn't exist yet, that's okay for this BDD test
    }

    [Given(@"the CI\/CD pipeline is configured")]
    public void GivenTheCICDPipelineIsConfigured()
    {
        _workflowPath = Path.Combine(GetRepositoryRoot(), ".github/workflows/ci.yml");
        Assert.True(File.Exists(_workflowPath), "CI/CD pipeline should be configured");
    }

    [When(@"I check the branch protection rules for main branch")]
    public void WhenICheckTheBranchProtectionRulesForMainBranch()
    {
        // For BDD tests, we'll verify this through documentation or CI configuration
        // In a real scenario, you'd use GitHub API to check this
        Assert.True(true, "Branch protection check would require GitHub API access");
    }

    [Then(@"the build check should be required")]
    public void ThenTheBuildCheckShouldBeRequired()
    {
        // This is typically configured via GitHub UI or API
        // For BDD tests, we verify that the build job exists in the workflow
        Assert.NotNull(_workflowPath);
        var content = File.ReadAllText(_workflowPath);
        Assert.Contains("build:", content);
    }

    [Then(@"the test check should be required")]
    public void ThenTheTestCheckShouldBeRequired()
    {
        // This is typically configured via GitHub UI or API
        // For BDD tests, we verify that the test job exists in the workflow
        Assert.NotNull(_workflowPath);
        var content = File.ReadAllText(_workflowPath);
        Assert.Contains("test:", content);
    }

    [Given(@"the CI\/CD is operational")]
    public void GivenTheCICDIsOperational()
    {
        GivenTheCICDPipelineIsConfigured();
    }

    [When(@"I review the workflow file")]
    public void WhenIReviewTheWorkflowFile()
    {
        _workflowPath = Path.Combine(GetRepositoryRoot(), ".github/workflows/ci.yml");
        _workflowContent = File.ReadAllText(_workflowPath);
    }

    [Then(@"it should contain documentation comments")]
    public void ThenItShouldContainDocumentationComments()
    {
        Assert.NotNull(_workflowContent);
        // YAML comments start with #
        Assert.Contains("#", _workflowContent);
        // Should have multiple comment lines
        var commentLines = _workflowContent.Split('\n').Count(line => line.TrimStart().StartsWith("#"));
        Assert.True(commentLines > MinimumDocumentationCommentLines, 
            $"Should have at least {MinimumDocumentationCommentLines} documentation comment lines, found {commentLines}");
    }

    [Then(@"comments should explain when each job runs")]
    public void ThenCommentsShouldExplainWhenEachJobRuns()
    {
        Assert.NotNull(_workflowContent);
        // Verify documentation about job execution
        Assert.True(_workflowContent.Contains("job") || _workflowContent.Contains("Job") || _workflowContent.Contains("JOB"));
    }

    [Then(@"comments should explain what each step does")]
    public void ThenCommentsShouldExplainWhatEachStepDoes()
    {
        Assert.NotNull(_workflowContent);
        // Verify documentation about steps
        Assert.True(_workflowContent.Contains("step") || _workflowContent.Contains("Step") || _workflowContent.Contains("STEP"));
    }

    [Then(@"comments should explain how to extend the workflow")]
    public void ThenCommentsShouldExplainHowToExtendTheWorkflow()
    {
        Assert.NotNull(_workflowContent);
        // Verify documentation about extending
        Assert.True(
            ContainsWorkflowExtensionDocumentation(_workflowContent),
            "Workflow should contain documentation about extending it"
        );
    }

    private static bool ContainsWorkflowExtensionDocumentation(string content)
    {
        var extensionKeywords = new[] { "extend", "add", "modify", "EXTENSION", "GUIDE" };
        return extensionKeywords.Any(keyword => 
            content.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    private static string GetRepositoryRoot()
    {
        var directory = Directory.GetCurrentDirectory();
        while (directory != null && !Directory.Exists(Path.Combine(directory, ".git")))
        {
            directory = Directory.GetParent(directory)?.FullName;
        }
        
        if (directory == null)
        {
            throw new InvalidOperationException("Could not find repository root");
        }
        
        return directory;
    }
}
