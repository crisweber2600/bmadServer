// Create a distributed application builder - this defines all services and their orchestration
// The AppHost is the "conductor" for the entire application - it manages service startup order,
// dependencies, and health checks across all services (API, Web, Database, etc.)
var builder = DistributedApplication.CreateBuilder(args);

// Configure PostgreSQL database resource via Aspire
// - "pgsql" is the resource name (used for service discovery)
// - WithPgAdmin() adds a pgAdmin UI at https://localhost:5050 for database management
// - AddDatabase("bmadserver", "bmadserver_dev") creates a database with the user credentials
// - WithHealthCheck() enables health check monitoring (CRITICAL for startup validation)
// PostgreSQL will start automatically when 'aspire run' is executed
// Health checks are automatically configured by Aspire and include startup verification
var db = builder.AddPostgres("pgsql")
    .WithPgAdmin()
    .AddDatabase("bmadserver", "bmadserver_dev");

// Configure the API service (bmadServer.ApiService)
// - WithHttpHealthCheck("/health") enables Aspire health check monitoring at /health endpoint
// - WithReference(db) injects PostgreSQL connection string via service discovery
// - WaitFor(db) ensures API doesn't start until PostgreSQL is healthy
// This implements the Aspire dependency ordering pattern from Microsoft docs
var apiService = builder.AddProject<Projects.bmadServer_ApiService>("apiservice")
    .WithHttpHealthCheck("/health")
    .WithReference(db)
    .WaitFor(db);

// Configure the React Web frontend (Vite-based)
// - AddViteApp() creates a Node.js/Vite development server for the React app
// - WithReference(apiService) injects API service endpoint via environment variables (APISERVICE_HTTP/HTTPS)
// - WaitFor(apiService) ensures frontend starts after API is healthy
// - Vite proxy configured in vite.config.ts routes /api/* to the API service
var webfrontend = builder.AddViteApp("webfrontend", "../frontend")
    .WithReference(apiService)
    .WaitFor(apiService);

// For production deployment, publish the static frontend with the API service
apiService.PublishWithContainerFiles(webfrontend, "wwwroot");

// Build and run the distributed application
// This starts all services in dependency order:
// 1. PostgreSQL container starts
// 2. API service starts (after PostgreSQL is healthy)
// 3. Web frontend starts (after API is healthy)
// The Aspire dashboard opens automatically at https://localhost:17360
builder.Build().Run();
