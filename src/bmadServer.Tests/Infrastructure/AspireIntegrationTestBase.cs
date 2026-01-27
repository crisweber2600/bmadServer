using Aspire.Hosting;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace bmadServer.Tests.Infrastructure;

/// <summary>
/// Base class for Aspire-based integration tests.
/// Spins up the full AppHost with real PostgreSQL for accurate testing.
/// </summary>
public abstract class AspireIntegrationTestBase : IAsyncLifetime
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(3);
    
    protected DistributedApplication App { get; private set; } = null!;
    protected HttpClient ApiClient { get; private set; } = null!;
    protected IServiceProvider Services => App.Services;
    
    // Test user credentials
    protected string TestUserEmail { get; } = $"testuser_{Guid.NewGuid():N}@test.com";
    protected string TestUserPassword { get; } = "TestPassword123!";
    protected Guid TestUserId { get; private set; }
    protected string? AuthToken { get; private set; }
    
    public virtual async Task InitializeAsync()
    {
        var cancellationToken = CancellationToken.None;
        
        // Create the AppHost testing builder with test mode enabled
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.bmadServer_AppHost>(
                args: ["--IsTestMode=true"],
                cancellationToken: cancellationToken);
        
        // Configure logging for debugging
        appHost.Services.AddLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Information);
            logging.AddFilter("Aspire.", LogLevel.Warning);
            logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
        });
        
        // Add standard resilience handler for HTTP clients
        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });
        
        // Build and start the application
        App = await appHost.BuildAsync(cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);
        
        await App.StartAsync(cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);
        
        // Wait for the API service to be healthy
        await App.ResourceNotifications.WaitForResourceHealthyAsync(
            "apiservice", cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);
        
        // Create HTTP client for the API service
        ApiClient = App.CreateHttpClient("apiservice");
        
        // Setup test user if needed
        if (RequiresAuthentication)
        {
            await SetupTestUserAsync(cancellationToken);
        }
    }
    
    /// <summary>
    /// Override to false if test doesn't need authentication setup
    /// </summary>
    protected virtual bool RequiresAuthentication => true;
    
    /// <summary>
    /// Creates a test user and obtains auth token
    /// </summary>
    protected virtual async Task SetupTestUserAsync(CancellationToken cancellationToken = default)
    {
        // Register test user
        var registerResponse = await ApiClient.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email = TestUserEmail,
            password = TestUserPassword,
            displayName = "Test User"
        }, cancellationToken);
        
        if (!registerResponse.IsSuccessStatusCode)
        {
            var error = await registerResponse.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Failed to register test user: {registerResponse.StatusCode} - {error}");
        }
        
        // Login to get token
        var loginResponse = await ApiClient.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = TestUserEmail,
            password = TestUserPassword
        }, cancellationToken);
        
        if (!loginResponse.IsSuccessStatusCode)
        {
            var error = await loginResponse.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Failed to login test user: {loginResponse.StatusCode} - {error}");
        }
        
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken);
        AuthToken = loginResult?.Token;
        TestUserId = loginResult?.UserId ?? Guid.Empty;
        
        // Set authorization header for subsequent requests
        ApiClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", AuthToken);
    }
    
    /// <summary>
    /// Creates a new HTTP client with auth header for parallel requests
    /// </summary>
    protected HttpClient CreateAuthenticatedClient()
    {
        var client = App.CreateHttpClient("apiservice");
        if (!string.IsNullOrEmpty(AuthToken))
        {
            client.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", AuthToken);
        }
        return client;
    }
    
    public virtual async Task DisposeAsync()
    {
        ApiClient?.Dispose();
        if (App != null)
        {
            await App.DisposeAsync();
        }
    }
    
    private record LoginResponse(string Token, Guid UserId);
}

/// <summary>
/// Collection fixture for shared Aspire app instance across tests in a class
/// </summary>
[CollectionDefinition("Aspire")]
public class AspireTestCollection : ICollectionFixture<AspireAppFixture>
{
}

/// <summary>
/// Shared fixture that maintains a single AppHost instance across all tests in a collection
/// </summary>
public class AspireAppFixture : IAsyncLifetime
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(60);
    
    public DistributedApplication App { get; private set; } = null!;
    
    public async Task InitializeAsync()
    {
        var cancellationToken = CancellationToken.None;
        
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.bmadServer_AppHost>(
                args: ["--IsTestMode=true"],
                cancellationToken: cancellationToken);
        
        appHost.Services.AddLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Warning);
        });
        
        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });
        
        App = await appHost.BuildAsync(cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);
        
        await App.StartAsync(cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);
        
        await App.ResourceNotifications.WaitForResourceHealthyAsync(
            "apiservice", cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);
    }
    
    public HttpClient CreateApiClient() => App.CreateHttpClient("apiservice");
    
    public async Task DisposeAsync()
    {
        if (App != null)
        {
            await App.DisposeAsync();
        }
    }
}
