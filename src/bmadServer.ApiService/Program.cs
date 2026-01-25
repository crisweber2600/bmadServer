using FluentValidation;
using System.Reflection;

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

// Register password hashing service
builder.Services.AddScoped<bmadServer.ApiService.Services.IPasswordHasher, bmadServer.ApiService.Services.PasswordHasher>();

// Register FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<bmadServer.ApiService.Validators.RegisterRequestValidator>();

// Add controllers
builder.Services.AddControllers();

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

// Sample data for demonstration purposes
string[] summaries = ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];

// Root endpoint - simple health status message
app.MapGet("/", () => "API service is running. Navigate to /weatherforecast to see sample data.");

// Sample weather forecast endpoint - demonstrates basic endpoint structure
// Remove this in production and replace with domain-specific endpoints
// Future integration points (from Architecture.md):
//   - POST /api/workflows - Create workflow (Epic 4)
//   - POST /api/chat - Send chat message (Epic 3 with SignalR hub at /hubs/chat)
//   - POST /api/auth/login - User authentication (Epic 2)
//   - GET /api/sessions - Retrieve user sessions (Epic 2)
app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

// Map default Aspire endpoints (health checks)
// Calls MapHealthChecks("/health") and MapHealthChecks("/alive") from ServiceDefaults
// These endpoints indicate:
//   - /health: All health checks pass, service is ready for traffic
//   - /alive: Service is running (liveness probe, subset of health checks)
app.MapDefaultEndpoints();

// Map controllers
app.MapControllers();

// Start the application
app.Run();

// Make Program class accessible to tests
public partial class Program { }


// Simple record for demonstration - remove in production
record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
