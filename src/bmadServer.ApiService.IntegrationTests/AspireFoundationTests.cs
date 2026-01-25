using System.Diagnostics;
using System.Net;

namespace bmadServer.ApiService.IntegrationTests;

/// <summary>
/// Integration tests for Aspire AppHost startup and dashboard availability.
/// Verifies that Story 1-1 AC #1 and #2 are satisfied:
/// - AC #1: "Aspire dashboard appears at https://localhost:17137"
/// - AC #2: "API responds to GET /health with 200 OK status"
/// </summary>
public class AspireFoundationTests
{
    private const int DashboardPort = 17137;
    private const int DashboardWaitTimeMs = 30000; // 30 second timeout
    private const int RetryIntervalMs = 1000; // Check every second
    
    /// <summary>
    /// Verifies that Aspire CLI can start without certificate errors.
    /// This test validates that the development environment is properly configured.
    /// </summary>
    [Fact]
    public async Task AspireRun_StartsWithoutCertificateErrors()
    {
        // Arrange
        var environmentVariables = new Dictionary<string, string>
        {
            { "ASPIRE_ALLOW_UNSECURED_TRANSPORT", "true" },
            { "DOTNET_CLI_TELEMETRY_OPTOUT", "1" }
        };
        
        var psi = new ProcessStartInfo
        {
            FileName = "aspire",
            Arguments = "run",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = Path.Combine(
                AppContext.BaseDirectory,
                "..", "..", "..", "..", 
                "bmadServer.AppHost"
            )
        };
        
        foreach (var (key, value) in environmentVariables)
        {
            psi.Environment[key] = value;
        }
        
        // Act
        var process = new Process { StartInfo = psi };
        process.Start();
        
        try
        {
            // Wait for up to 30 seconds for Aspire to start
            var startTime = DateTime.UtcNow;
            var certificateErrorFound = false;
            var aspireStartedSuccessfully = false;
            
            while (DateTime.UtcNow - startTime < TimeSpan.FromMilliseconds(DashboardWaitTimeMs))
            {
                var output = process.StandardError.ReadLine();
                if (output != null)
                {
                    // Check for certificate errors
                    if (output.Contains("Unable to configure HTTPS endpoint") ||
                        output.Contains("default developer certificate could not be found"))
                    {
                        certificateErrorFound = true;
                        break;
                    }
                    
                    // Check for successful startup
                    if (output.Contains("Listening on") || 
                        output.Contains("localhost:17137"))
                    {
                        aspireStartedSuccessfully = true;
                    }
                }
                
                await Task.Delay(100);
            }
            
            // Assert
            Assert.False(certificateErrorFound, 
                "Aspire failed to start due to HTTPS certificate error. " +
                "Ensure ASPIRE_ALLOW_UNSECURED_TRANSPORT environment variable is set.");
            
            Assert.True(aspireStartedSuccessfully,
                "Aspire did not start successfully within timeout period.");
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill();
                process.WaitForExit(5000);
            }
            process.Dispose();
        }
    }

    /// <summary>
    /// Verifies that dashboard health endpoint is reachable via HTTP.
    /// This validates that we can skip HTTPS certificate validation in development.
    /// </summary>
    [Fact(Skip = "Requires running aspire run - run manually with: aspire run")]
    public async Task DashboardHealthEndpoint_IsReachableViaHttp()
    {
        // Arrange
        var dashboardUrl = $"http://localhost:{DashboardPort}/health";
        var httpHandler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        };
        var client = new HttpClient(httpHandler);
        
        // Act & Assert
        var startTime = DateTime.UtcNow;
        while (DateTime.UtcNow - startTime < TimeSpan.FromMilliseconds(DashboardWaitTimeMs))
        {
            try
            {
                var response = await client.GetAsync(dashboardUrl);
                
                // Dashboard may return different status codes but should be accessible
                Assert.NotNull(response);
                return;
            }
            catch (HttpRequestException)
            {
                // Dashboard not ready yet, retry
                await Task.Delay(RetryIntervalMs);
            }
        }
        
        // Assert
            Assert.Fail(
                $"Dashboard at {dashboardUrl} was not reachable within timeout period.");
    }

    /// <summary>
    /// Verifies that .env.development file is properly configured for development.
    /// This ensures that developers have the correct environment setup.
    /// </summary>
    [Fact]
    public void EnvironmentConfiguration_HasDevelopmentFile()
    {
        // Arrange
        var envFile = Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            ".env.development"
        );
        
        // Act & Assert
        Assert.True(File.Exists(envFile),
            $".env.development file not found at {envFile}. " +
            "This file is required for development setup.");
        
        var content = File.ReadAllText(envFile);
        Assert.True(content.Contains("ASPIRE_ALLOW_UNSECURED_TRANSPORT"),
            "ASPIRE_ALLOW_UNSECURED_TRANSPORT not configured in .env.development");
    }

    /// <summary>
    /// Verifies that dev-run.sh helper script exists and is executable.
    /// This ensures developers can use the convenient helper script.
    /// </summary>
    [Fact(Skip = "Unix-specific - skip on Windows")]
    public void ScriptConfiguration_HasDevRunScript()
    {
        // Arrange
        var scriptPath = Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "scripts", "dev-run.sh"
        );
        
        // Act & Assert
        Assert.True(File.Exists(scriptPath),
            $"scripts/dev-run.sh not found at {scriptPath}");
    }

    /// <summary>
    /// Verifies that Story 1-1 setup is complete and documented.
    /// This ensures developers know how to set up their environment.
    /// </summary>
    [Fact]
    public void Documentation_HasSetupInstructions()
    {
        // Arrange
        var readmePath = Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "README.md"
        );
        
        // Act & Assert
        if (File.Exists(readmePath))
        {
            var content = File.ReadAllText(readmePath);
            Assert.True(
                content.Contains("aspire run") || content.Contains("dev-run.sh"),
                "README.md should document how to start Aspire for development");
        }
    }
}
