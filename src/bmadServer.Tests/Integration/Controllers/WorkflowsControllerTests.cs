using bmadServer.ApiService.Controllers;
using bmadServer.ApiService.Data;
using bmadServer.ApiService.Data.Entities;
using bmadServer.ApiService.Models.Workflows;
using bmadServer.ApiService.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace bmadServer.Tests.Integration.Controllers;

public class WorkflowsControllerTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private static readonly string _databaseName = "TestDb_Workflows_" + Guid.NewGuid();

    public WorkflowsControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase(_databaseName);
                });
            });

            builder.UseEnvironment("Test");
        });

        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
    }

    [Fact]
    public async Task CreateWorkflow_WithoutAuthentication_ShouldReturn401()
    {
        // Arrange
        var request = new CreateWorkflowRequest
        {
            WorkflowId = "create-prd"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/workflows", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateWorkflow_WithInvalidWorkflowId_ShouldReturn400()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new CreateWorkflowRequest
        {
            WorkflowId = "invalid-workflow"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/workflows", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateWorkflow_WithValidRequest_ShouldReturn201()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new CreateWorkflowRequest
        {
            WorkflowId = "create-prd",
            InitialContext = new Dictionary<string, object>
            {
                ["projectName"] = "Test Project",
                ["description"] = "Test Description"
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/workflows", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var instance = await response.Content.ReadFromJsonAsync<WorkflowInstance>();
        instance.Should().NotBeNull();
        instance!.WorkflowDefinitionId.Should().Be("create-prd");
        instance.Status.Should().Be(WorkflowStatus.Created);
    }

    [Fact]
    public async Task GetWorkflow_WithValidId_ShouldReturn200()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create a workflow first
        var createRequest = new CreateWorkflowRequest
        {
            WorkflowId = "create-prd"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/workflows", createRequest);
        var createdInstance = await createResponse.Content.ReadFromJsonAsync<WorkflowInstance>();

        // Act
        var response = await _client.GetAsync($"/api/v1/workflows/{createdInstance!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var instance = await response.Content.ReadFromJsonAsync<WorkflowInstance>();
        instance.Should().NotBeNull();
        instance!.Id.Should().Be(createdInstance.Id);
    }

    [Fact]
    public async Task GetWorkflow_WithInvalidId_ShouldReturn404()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync($"/api/v1/workflows/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task StartWorkflow_WithValidId_ShouldReturn204()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create a workflow first
        var createRequest = new CreateWorkflowRequest
        {
            WorkflowId = "create-prd"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/workflows", createRequest);
        var createdInstance = await createResponse.Content.ReadFromJsonAsync<WorkflowInstance>();

        // Act
        var response = await _client.PostAsync($"/api/v1/workflows/{createdInstance!.Id}/start", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify workflow status changed
        var getResponse = await _client.GetAsync($"/api/v1/workflows/{createdInstance.Id}");
        var instance = await getResponse.Content.ReadFromJsonAsync<WorkflowInstance>();
        instance!.Status.Should().Be(WorkflowStatus.Running);
        instance.CurrentStep.Should().Be(1);
    }

    private async Task<string> GetAuthTokenAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var jwtTokenService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();

        var user = new User
        {
            Email = $"test-{Guid.NewGuid()}@example.com",
            PasswordHash = "hash",
            DisplayName = "Test User",
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        return jwtTokenService.GenerateAccessToken(user);
    }
}
