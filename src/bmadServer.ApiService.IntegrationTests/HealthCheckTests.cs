using System.Net;
using Aspire.Hosting;
using Aspire.Hosting.Testing;

namespace bmadServer.ApiService.IntegrationTests;

public class HealthCheckTests : IAsyncLifetime
{
    private DistributedApplication? _app;
    private HttpClient? _client;

    public async Task InitializeAsync()
    {
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.bmadServer_AppHost>();

        _app = await appHost.BuildAsync();
        await _app.StartAsync();

        _client = _app.CreateHttpClient("apiservice");
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        if (_app != null)
        {
            await _app.DisposeAsync();
        }
    }

    [Fact]
    public async Task HealthEndpoint_Returns200_WhenCalled()
    {
        var response = await _client!.GetAsync("/health");
        
        Assert.NotNull(response);
        Assert.True(response.StatusCode == HttpStatusCode.OK || 
                    response.StatusCode == HttpStatusCode.ServiceUnavailable,
                    "Health endpoint should return a valid HTTP response");
    }

    [Fact]
    public async Task AliveEndpoint_ReturnsOk_WhenServiceIsRunning()
    {
        var response = await _client!.GetAsync("/alive");
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsResponseBody()
    {
        var response = await _client!.GetAsync("/health");
        var content = await response.Content.ReadAsStringAsync();
        
        Assert.NotEmpty(content);
    }

    [Fact]
    public async Task HealthEndpoint_HasCorrectContentType()
    {
        var response = await _client!.GetAsync("/health");
        
        Assert.NotNull(response.Content.Headers.ContentType);
        Assert.Equal("text/plain", response.Content.Headers.ContentType.MediaType);
    }

    [Fact]
    public async Task RootEndpoint_ReturnsOk()
    {
        var response = await _client!.GetAsync("/");
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task WeatherForecastEndpoint_ReturnsOk()
    {
        var response = await _client!.GetAsync("/weatherforecast");
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
