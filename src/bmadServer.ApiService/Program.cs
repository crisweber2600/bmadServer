using FluentValidation;
using System.Reflection;
using System.Text;
using bmadServer.ApiService.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

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

// Register session cleanup background service
builder.Services.AddHostedService<bmadServer.ApiService.BackgroundServices.SessionCleanupService>();

// Register workflow registry as singleton (shared in-memory registry)
builder.Services.AddSingleton<bmadServer.ServiceDefaults.Services.Workflows.IWorkflowRegistry, bmadServer.ServiceDefaults.Services.Workflows.WorkflowRegistry>();

// Register workflow instance service
builder.Services.AddScoped<bmadServer.ApiService.Services.Workflows.IWorkflowInstanceService, bmadServer.ApiService.Services.Workflows.WorkflowInstanceService>();

// Register agent router as singleton (shared agent handler registry)
builder.Services.AddSingleton<bmadServer.ApiService.Services.Workflows.IAgentRouter, bmadServer.ApiService.Services.Workflows.AgentRouter>();

// Register step executor
builder.Services.AddScoped<bmadServer.ApiService.Services.Workflows.IStepExecutor, bmadServer.ApiService.Services.Workflows.StepExecutor>();

// Register participant service
builder.Services.AddScoped<bmadServer.ApiService.Services.IParticipantService, bmadServer.ApiService.Services.ParticipantService>();

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
builder.Services.AddControllers();

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

// Configure the HTTP request pipeline with middleware

// Use exception handler middleware to catch unhandled exceptions and return problem details
app.UseExceptionHandler();

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

// Make Program class accessible to tests
public partial class Program { }
