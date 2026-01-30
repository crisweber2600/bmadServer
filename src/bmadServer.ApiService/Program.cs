using FluentValidation;
using System.Reflection;
using System.Text;
using AspNetCoreRateLimit;
using bmadServer.ApiService.Configuration;
using bmadServer.ApiService.Services.Workflows.Agents;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Configure BMAD options for agent/workflow integration
builder.Services.Configure<BmadOptions>(builder.Configuration.GetSection(BmadOptions.SectionName));
builder.Services.Configure<OpenCodeOptions>(builder.Configuration.GetSection(OpenCodeOptions.SectionName));
builder.Services.Configure<CopilotOptions>(builder.Configuration.GetSection(CopilotOptions.SectionName));

// Add Aspire service defaults: health checks, telemetry (logging + tracing), service discovery, and resilience patterns
// See ServiceDefaults/Extensions.cs for detailed configuration of:
//   - OpenTelemetry with structured JSON logging
//   - Distributed tracing with trace ID support
//   - Health check endpoints at /health and /alive
//   - Service discovery for inter-service communication
//   - HTTP client resilience (retries, circuit breakers, timeouts)
builder.AddServiceDefaults();

// Register Entity Framework Core DbContext for PostgreSQL using Aspire Npgsql integration
// This automatically:
// - Injects the connection string from AppHost service discovery (resource named "pgsql" → database "bmadserver")
// - Configures health checks for the database
// - Sets up OpenTelemetry tracing for database queries
// - Enables connection pooling and resilience patterns
// The connection name "bmadserver" matches the database name in AppHost.cs
// See https://aspire.dev/integrations/databases/efcore/postgresql/ for more details
// Skip database registration in test environments where PostgreSQL is not available
if (!builder.Environment.IsEnvironment("Test"))
{
    builder.AddNpgsqlDbContext<bmadServer.ApiService.Data.ApplicationDbContext>("bmadserver", configureDbContextOptions: options =>
    {
        // Enable sensitive data logging only in development for debugging
        if (builder.Environment.IsDevelopment())
        {
            options.EnableSensitiveDataLogging();
        }
    });
}

// Add structured exception handling with RFC 7807 problem details format
// This provides consistent error responses across all API endpoints
// Errors will be returned as JSON with status code, title, detail, and trace ID
builder.Services.AddProblemDetails();

// Configure JWT settings from appsettings.json
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));

// Register JWT token service
builder.Services.AddScoped<bmadServer.ApiService.Services.IJwtTokenService, bmadServer.ApiService.Services.JwtTokenService>();

// Register password hashing service
builder.Services.AddScoped<bmadServer.ApiService.Services.IPasswordHasher, bmadServer.ApiService.Services.PasswordHasher>();

// Register refresh token service
builder.Services.AddScoped<bmadServer.ApiService.Services.IRefreshTokenService, bmadServer.ApiService.Services.RefreshTokenService>();

// Register session service
builder.Services.AddScoped<bmadServer.ApiService.Services.ISessionService, bmadServer.ApiService.Services.SessionService>();

// Register translation service
builder.Services.AddScoped<bmadServer.ApiService.Services.ITranslationService, bmadServer.ApiService.Services.TranslationService>();

// Register context analysis service
builder.Services.AddScoped<bmadServer.ApiService.Services.IContextAnalysisService, bmadServer.ApiService.Services.ContextAnalysisService>();

// Register response metadata service
builder.Services.AddScoped<bmadServer.ApiService.Services.IResponseMetadataService, bmadServer.ApiService.Services.ResponseMetadataService>();

// Register session cleanup background service
builder.Services.AddHostedService<bmadServer.ApiService.BackgroundServices.SessionCleanupService>();

// Register workflow registry based on test mode configuration
// In Mock mode: use hardcoded WorkflowRegistry (POC/fast tests)
// In Live/Replay mode: use BmadWorkflowRegistry (loads from BMAD files)
var bmadOptions = builder.Configuration.GetSection(BmadOptions.SectionName).Get<BmadOptions>() ?? new BmadOptions();

// Log BMAD configuration at startup (deferred logging via LoggerMessage)
var testModeDescription = bmadOptions.TestMode switch
{
    AgentTestMode.Live => $"Live - Using Copilot SDK, DefaultModel: {bmadOptions.OpenCode.DefaultModel}",
    AgentTestMode.Replay => "Replay - responses will be cached/replayed",
    AgentTestMode.Mock => "Mock - using MockAgentHandler (no LLM calls)",
    _ => "Unknown mode"
};
Console.WriteLine($"[BMAD] TestMode: {testModeDescription}");

if (bmadOptions.TestMode == AgentTestMode.Mock)
{
    builder.Services.AddSingleton<bmadServer.ServiceDefaults.Services.Workflows.IWorkflowRegistry, bmadServer.ServiceDefaults.Services.Workflows.WorkflowRegistry>();
}
else
{
    builder.Services.Configure<bmadServer.ServiceDefaults.Services.Workflows.BmadWorkflowOptions>(options =>
    {
        options.ManifestPath = bmadOptions.WorkflowManifestPath;
        options.EnabledModules = bmadOptions.EnabledModules;
        options.BasePath = bmadOptions.BasePath;
    });
    builder.Services.AddSingleton<bmadServer.ServiceDefaults.Services.Workflows.IWorkflowRegistry, bmadServer.ServiceDefaults.Services.Workflows.BmadWorkflowRegistry>();
}

// Register workflow instance service
builder.Services.AddScoped<bmadServer.ApiService.Services.Workflows.IWorkflowInstanceService, bmadServer.ApiService.Services.Workflows.WorkflowInstanceService>();

// Register agent router as singleton (shared agent handler registry)
builder.Services.AddSingleton<bmadServer.ApiService.Services.Workflows.IAgentRouter, bmadServer.ApiService.Services.Workflows.AgentRouter>();

// Register agent registry based on test mode configuration
// In Mock mode: use hardcoded AgentRegistry (POC/fast tests)
// In Live/Replay mode: use BmadAgentRegistry (loads from BMAD manifest CSV)
if (bmadOptions.TestMode == AgentTestMode.Mock)
{
    builder.Services.AddSingleton<IAgentRegistry, AgentRegistry>();
}
else
{
    builder.Services.AddSingleton<IAgentRegistry, BmadAgentRegistry>();
}

// Register agent handler factory for creating handlers based on test mode
builder.Services.AddSingleton<IAgentHandlerFactory, AgentHandlerFactory>();

// Register agent messaging service for agent-to-agent communication
builder.Services.AddScoped<bmadServer.ApiService.Services.Workflows.Agents.IAgentMessaging, bmadServer.ApiService.Services.Workflows.Agents.AgentMessaging>();

// Register step executor
builder.Services.AddScoped<bmadServer.ApiService.Services.Workflows.IStepExecutor, bmadServer.ApiService.Services.Workflows.StepExecutor>();

// Register approval service (Epic 5 - Multi-Agent Collaboration)
builder.Services.AddScoped<bmadServer.ApiService.Services.Workflows.IApprovalService, bmadServer.ApiService.Services.Workflows.ApprovalService>();

// Register shared context service (Story 5-3)
builder.Services.AddScoped<bmadServer.ApiService.Services.Workflows.ISharedContextService, bmadServer.ApiService.Services.Workflows.SharedContextService>();

// Register agent handoff service (Story 5-4)
builder.Services.AddScoped<bmadServer.ApiService.Services.Workflows.IAgentHandoffService, bmadServer.ApiService.Services.Workflows.AgentHandoffService>();

// Register participant service
builder.Services.AddScoped<bmadServer.ApiService.Services.IParticipantService, bmadServer.ApiService.Services.ParticipantService>();

// Register checkpoint services
builder.Services.AddScoped<bmadServer.ApiService.Services.Checkpoints.ICheckpointService, bmadServer.ApiService.Services.Checkpoints.CheckpointService>();
builder.Services.AddScoped<bmadServer.ApiService.Services.Checkpoints.IInputQueueService, bmadServer.ApiService.Services.Checkpoints.InputQueueService>();

// Register memory cache for user profile caching (Story 7.3)
builder.Services.AddMemoryCache();

// Register distributed cache for contribution metrics (Story 7.3)
// Using in-memory distributed cache for MVP (replace with Redis in production)
builder.Services.AddDistributedMemoryCache();

// Register contribution metrics service (Story 7.3)
builder.Services.AddScoped<bmadServer.ApiService.Services.IContributionMetricsService, bmadServer.ApiService.Services.ContributionMetricsService>();

// Register decision services (Epic 6 - Decision Management)
builder.Services.AddScoped<bmadServer.ApiService.Services.Decisions.IDecisionService, bmadServer.ApiService.Services.Decisions.DecisionService>();

// Register conflict detection services (Story 7.4)
builder.Services.AddScoped<bmadServer.ApiService.Services.IConflictDetectionService, bmadServer.ApiService.Services.ConflictDetectionService>();
builder.Services.AddScoped<bmadServer.ApiService.Services.IConflictResolutionService, bmadServer.ApiService.Services.ConflictResolutionService>();
builder.Services.AddHostedService<bmadServer.ApiService.BackgroundServices.ConflictEscalationJob>();

// Register real-time collaboration services (Story 7.5)
builder.Services.AddSingleton<bmadServer.ApiService.Services.IPresenceTrackingService, bmadServer.ApiService.Services.PresenceTrackingService>();
builder.Services.AddSingleton<bmadServer.ApiService.Services.IUpdateBatchingService, bmadServer.ApiService.Services.UpdateBatchingService>();

// Configure IP rate limiting to prevent brute-force attacks on auth endpoints
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.Configure<IpRateLimitPolicies>(builder.Configuration.GetSection("IpRateLimitPolicies"));
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

// Register FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<bmadServer.ApiService.Validators.RegisterRequestValidator>();

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()!;
jwtSettings.Validate();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
            ClockSkew = TimeSpan.Zero // No tolerance for expiry
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) &&
                    (path.StartsWithSegments("/hubs/chat")))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                if (context.Exception is SecurityTokenExpiredException)
                {
                    context.Response.Headers.Append("Token-Expired", "true");
                }
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                // Suppress default challenge response to allow ProblemDetails
                context.HandleResponse();

                if (!context.Response.HasStarted)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/problem+json";

                    var problemDetails = new Microsoft.AspNetCore.Mvc.ProblemDetails
                    {
                        Type = "https://bmadserver.dev/errors/unauthorized",
                        Title = "Unauthorized",
                        Status = StatusCodes.Status401Unauthorized,
                        Detail = context.AuthenticateFailure?.Message.Contains("expired") == true
                            ? "Access token has expired. Please refresh your token."
                            : "Invalid or missing authentication token."
                    };

                    return context.Response.WriteAsJsonAsync(problemDetails);
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// Add controllers
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// Add SignalR for real-time communication
builder.Services.AddSignalR();

// Add OpenAPI/Swagger documentation (available in development environment)
// Provides automatic API documentation at /openapi/v1.json in development
// Remove or conditionally add in production environments
builder.Services.AddOpenApi();

// Add Swagger/OpenAPI with XML documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Include XML documentation
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();

// Apply EF Core migrations automatically on startup (for development/Aspire orchestration)
// This ensures the database schema is up-to-date when the application starts
if (!app.Environment.IsEnvironment("Test"))
{
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<bmadServer.ApiService.Data.ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        try
        {
            logger.LogInformation("Applying database migrations...");
            dbContext.Database.Migrate();
            logger.LogInformation("Database migrations applied successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while applying database migrations.");
            throw;
        }
    }
}

// Configure the HTTP request pipeline with middleware

// Use exception handler middleware to catch unhandled exceptions and return problem details
app.UseExceptionHandler();

// Apply rate limiting before authentication to protect auth endpoints
app.UseIpRateLimiting();

// Add authentication and authorization middleware (order matters!)
app.UseAuthentication();
app.UseAuthorization();

// Map OpenAPI documentation endpoints (development only)
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "bmadServer API v1");
        options.RoutePrefix = "swagger";
    });
}

app.MapGet("/", () => "bmadServer API service is running. See /swagger for API documentation.");

app.MapDefaultEndpoints();

// Map controllers
app.MapControllers();

// Map SignalR hub endpoint
app.MapHub<bmadServer.ApiService.Hubs.ChatHub>("/hubs/chat");

// Start the application
app.Run();

// Make Program class accessible to tests in bmadServer.ApiService namespace
namespace bmadServer.ApiService
{
    public partial class Program { }
}
