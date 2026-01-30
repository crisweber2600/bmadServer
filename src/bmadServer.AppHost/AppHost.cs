using Microsoft.Extensions.Configuration;

// Create a distributed application builder - this defines all services and their orchestration
// The AppHost is the "conductor" for the entire application - it manages service startup order,
// dependencies, and health checks across all services (API, Web, Database, etc.)
var builder = DistributedApplication.CreateBuilder(args);

// Check if we're running in test mode (no frontend needed)
var isTestMode = builder.Configuration.GetValue<bool>("IsTestMode");

// Check if PgAdmin should use credentials (feature flag for debugging)
// When false (default in Development), PgAdmin will use default/auto-generated credentials
// When true (can be set in Production), PgAdmin will require configured username/password parameters
var usePgAdminCredentials = builder.Configuration.GetValue<bool>("PgAdmin:UseCredentials");

// Configure PostgreSQL database resource via Aspire
// - "pgsql" is the resource name (used for service discovery)
// - WithPgAdmin() adds a pgAdmin UI at https://localhost:5050 for database management
// - AddDatabase("bmadserver", "bmadserver_dev") creates a database with the user credentials
// - WithHealthCheck() enables health check monitoring (CRITICAL for startup validation)
// PostgreSQL will start automatically when 'aspire run' is executed
// Health checks are automatically configured by Aspire and include startup verification
var pgsql = builder.AddPostgres("pgsql");
if (!isTestMode)
{
    if (usePgAdminCredentials)
    {
        // When credentials are enabled, use parameters for username/password
        // These must be configured via user secrets or environment variables:
        // dotnet user-secrets set "Parameters:pgadmin-username" "admin@example.com"
        // dotnet user-secrets set "Parameters:pgadmin-password" "your-password"
        var pgAdminUsername = builder.AddParameter("pgadmin-username");
        var pgAdminPassword = builder.AddParameter("pgadmin-password", secret: true);
        pgsql.WithPgAdmin(container => container
            .WithEnvironment("PGADMIN_DEFAULT_EMAIL", pgAdminUsername)
            .WithEnvironment("PGADMIN_DEFAULT_PASSWORD", pgAdminPassword));
    }
    else
    {
        // When credentials are disabled (default for debugging), PgAdmin uses default credentials
        // This is the recommended setting during development/debugging
        pgsql.WithPgAdmin();
    }
}
var db = pgsql.AddDatabase("bmadserver", "bmadserver_dev");

// Configure the API service (bmadServer.ApiService)
// - WithHttpHealthCheck("/health") enables Aspire health check monitoring at /health endpoint
// - WithReference(db) injects PostgreSQL connection string via service discovery
// - WaitFor(db) ensures API doesn't start until PostgreSQL is healthy
// This implements the Aspire dependency ordering pattern from Microsoft docs
var apiService = builder.AddProject<Projects.bmadServer_ApiService>("apiservice")
    .WithHttpHealthCheck("/health")
    .WithReference(db)
    .WaitFor(db);

// Configure the React frontend (using Vite dev server) - skip in test mode
if (!isTestMode)
{
    // - WithExternalHttpEndpoints() exposes the frontend to the host machine
    // - WithReference(apiService) injects API service endpoint for client calls
    // - WaitFor(apiService) ensures frontend starts after API is healthy
    var frontend = builder.AddViteApp("frontend", "../frontend")
        .WithExternalHttpEndpoints()
        .WithReference(apiService)
        .WaitFor(apiService);
}

// Build and run the distributed application
// This starts all services in dependency order:
// 1. PostgreSQL container starts
// 2. API service starts (after PostgreSQL is healthy)
// 3. React frontend (Vite dev server) starts (after API is healthy) - if not test mode
// The Aspire dashboard opens automatically at https://localhost:17360
builder.Build().Run();
