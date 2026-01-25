# Aspire Best Practices & Development Standards

**Purpose:** Establish consistent patterns for using .NET Aspire across all stories to avoid rework and ensure quality observability from day 1.

**Last Updated:** 2026-01-25  
**Applies To:** Epic 2+

---

## 🔍 Research-First Approach

### Before Implementing Any Feature
1. **Check Official Aspire Packages First**
   - Visit: https://learn.microsoft.com/en-us/dotnet/aspire/components
   - Search NuGet.org: `aspire.*` or `aspire-*`
   - Examples: `Aspire.Hosting.PostgreSQL`, `Aspire.Hosting.Redis`, `Aspire.Hosting.MongoDB`

2. **Verify .NET & Framework Versions**
   - Don't assume versions are unsupported
   - Check: https://dotnet.microsoft.com/download/dotnet
   - Current project: **.NET 10** (stable LTS)
   - Aspire support: Full support across all .NET versions

3. **Research Official Documentation**
   - Aspire docs: https://learn.microsoft.com/en-us/dotnet/aspire
   - EF Core: https://learn.microsoft.com/en-us/ef/core
   - ASP.NET Core: https://learn.microsoft.com/en-us/aspnet/core
   - Don't guess—look it up!

---

## 📦 Official Aspire Components

### Database Components
```
Aspire.Hosting.PostgreSQL       → PostgreSQL container orchestration
Aspire.Hosting.MongoDB          → MongoDB container orchestration
Aspire.Hosting.SqlServer        → SQL Server container orchestration
Aspire.Hosting.Sqlite           → SQLite local development
```

**Pattern:** Always use Aspire component instead of manual Docker configuration.

```csharp
// ✅ GOOD: Use Aspire component
var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin()
    .AddDatabase("mydb", "user", "password");

// ❌ BAD: Manual Docker configuration (don't do this)
// var containerName = "postgres-13";
// var dockerRunCommand = "docker run ...";
```

### Cache Components
```
Aspire.Hosting.Redis            → Redis cache orchestration
Aspire.Hosting.Memcached        → Memcached orchestration
```

### Message Components
```
Aspire.Hosting.RabbitMQ         → RabbitMQ message broker
Aspire.Hosting.Kafka            → Apache Kafka
```

### Observability Components
```
Aspire.Hosting.OpenTelemetry    → Distributed tracing & metrics
Aspire.Hosting.Prometheus       → Metrics collection
Aspire.Hosting.Grafana          → Metrics visualization
Aspire.Hosting.Seq              → Centralized logging
Aspire.Hosting.Jaeger           → Distributed tracing UI
```

---

## 🔬 Observability: Tracing & Logging as Default

### ✅ Requirement: Every Service Must Have

1. **Structured Logging (JSON)**
   ```json
   {
     "timestamp": "2026-01-25T14:30:45.123Z",
     "level": "Information",
     "message": "User registered successfully",
     "user_id": "550e8400-e29b-41d4-a716-446655440000",
     "trace_id": "550e8400-e29b-41d4-a716-446655440000",
     "span_id": "b9c7c3f5e8a1d2b4",
     "service": "ApiService",
     "duration_ms": 145
   }
   ```

2. **Distributed Tracing (W3C Trace Context)**
   - Trace ID: Unique per request across all services
   - Span ID: Individual operation within trace
   - Visible in Aspire Dashboard automatically

3. **Performance Metrics**
   - Request duration
   - Database query time
   - Error rates
   - Custom business metrics

### Implementation Pattern

**ServiceDefaults/Extensions.cs (Shared Pattern)**
```csharp
public static IServiceCollection AddServiceDefaults(
    this IServiceCollection services) =>
    services
        .ConfigureOpenTelemetry()      // ← Tracing + Logging
        .AddDefaultHealthChecks()
        .AddServiceDiscovery()
        .ConfigureHttpClientDefaults();

private static IServiceCollection ConfigureOpenTelemetry(
    this IServiceCollection services) =>
    services
        .AddOpenTelemetry()
        .WithMetrics(metrics => metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation())
        .WithTracing(tracing => tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddEntityFrameworkCoreInstrumentation())
        .Build();
```

### Story Template Requirement

**Every story implementation must include:**
- [ ] OpenTelemetry tracing configured
- [ ] Structured JSON logging
- [ ] Health checks for all components
- [ ] Visible in Aspire Dashboard
- [ ] Metrics for key operations

---

## 🚀 AppHost.cs Pattern (Aspire Orchestration)

### Standard Service Definition

```csharp
// 1. Database resource
var db = builder.AddPostgres("postgres")
    .WithPgAdmin()
    .AddDatabase("mydb", "user");

// 2. Service definition with health checks
var apiService = builder.AddProject<Projects.MyService>("api")
    .WithHttpHealthCheck("/health")
    .WithReference(db)
    .WaitFor(db);

// 3. Frontend with API dependency
var web = builder.AddProject<Projects.MyWeb>("web")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService);

// 4. Cache service (if needed)
var cache = builder.AddRedis("cache");

// 5. Build & run
builder.Build().Run();
```

### Key Practices

1. **Use `WithReference()` for service discovery**
   - Automatically injects connection strings
   - Services find each other by name
   - No manual configuration needed

2. **Use `WaitFor()` for startup ordering**
   - API waits for database to be healthy
   - Frontend waits for API to be healthy
   - Prevents cascade failures

3. **Use `WithHttpHealthCheck()` for monitoring**
   - Health endpoint automatically monitored
   - Dashboard shows service status
   - Aspire knows when service is ready

---

## 🧪 Testing Pattern

### Test Environment Configuration

**Program.cs (Standard Pattern)**
```csharp
// Skip database registration in test environments
if (!builder.Environment.IsEnvironment("Test"))
{
    builder.AddNpgsqlDbContext<ApplicationDbContext>("postgres");
}

// Add in-memory database for tests
if (builder.Environment.IsEnvironment("Test"))
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseInMemoryDatabase("TestDb"));
}
```

### Test Assertions

```csharp
[Fact]
public void Service_HasRequiredObservability()
{
    // Verify OpenTelemetry is configured
    Assert.NotNull(context.ServiceProvider.GetService<TracerProvider>());
    
    // Verify structured logging
    Assert.NotNull(context.ServiceProvider.GetService<ILogger>());
    
    // Verify health checks
    Assert.NotNull(context.ServiceProvider.GetService<HealthCheckService>());
}
```

---

## 📊 Aspire Dashboard Integration

### Automatic Features
- ✅ Service status and health
- ✅ Real-time logs with trace IDs
- ✅ Performance metrics
- ✅ Container management
- ✅ Endpoint routing

### No Manual Configuration Needed
- Aspire Dashboard starts automatically: `aspire run`
- Available at: https://localhost:17360
- Shows all services configured in AppHost.cs
- Logs updated in real-time

### What You'll See
```
ServiceDefaults: /health endpoint
├─ ApiService
│  ├─ Status: Running
│  ├─ Port: 8080
│  ├─ Health: Healthy
│  └─ Logs: [Structured JSON entries with trace IDs]
│
├─ PostgreSQL
│  ├─ Status: Running
│  ├─ Port: 5432
│  ├─ Health: Healthy
│  └─ Logs: [Container logs]
│
└─ Web
   ├─ Status: Running
   ├─ Port: 5173
   ├─ Health: Healthy
   └─ Logs: [Structured JSON entries]
```

---

## 🔗 Common Aspire Packages Checklist

Use this before implementing any feature:

### Authentication & Security
- [ ] `Aspire.Hosting.AppConfiguration` - Configuration management
- [ ] `Aspire.Hosting.KeyVault` - Azure Key Vault (production secrets)

### Data & Storage
- [ ] `Aspire.Hosting.PostgreSQL` - PostgreSQL (✅ using)
- [ ] `Aspire.Hosting.Redis` - Redis cache
- [ ] `Aspire.Hosting.MongoDB` - NoSQL
- [ ] `Aspire.Hosting.SqlServer` - SQL Server

### Messaging
- [ ] `Aspire.Hosting.RabbitMQ` - Message broker
- [ ] `Aspire.Hosting.Kafka` - Event streaming

### Observability
- [ ] `Aspire.Hosting.OpenTelemetry` - Tracing (✅ using)
- [ ] `Aspire.Hosting.Prometheus` - Metrics collection
- [ ] `Aspire.Hosting.Grafana` - Metrics visualization
- [ ] `Aspire.Hosting.Seq` - Log aggregation
- [ ] `Aspire.Hosting.Jaeger` - Distributed tracing UI

### Recommended Setup for New Stories
```bash
# Always start with these core packages
dotnet add package Aspire.Hosting
dotnet add package Aspire.ServiceDefaults
dotnet add package OpenTelemetry.Exporter.Console
dotnet add package OpenTelemetry.Extensions.Hosting
```

---

## 📝 Story Checklist: Aspire Best Practices

Before marking a story done, verify:

### 1. AppHost Configuration
- [ ] All services defined in AppHost.cs
- [ ] Health checks configured
- [ ] Service dependencies ordered with WaitFor()
- [ ] Service references configured with WithReference()
- [ ] Verified in Aspire Dashboard

### 2. Observability
- [ ] OpenTelemetry configured in ServiceDefaults
- [ ] Structured JSON logging enabled
- [ ] Trace IDs visible in logs
- [ ] Health checks at /health endpoint
- [ ] Metrics collected for key operations

### 3. Testing
- [ ] Unit tests use in-memory database
- [ ] Test environment properly configured
- [ ] Health check tests verify observability
- [ ] 10+ tests minimum per story

### 4. Documentation
- [ ] AppHost pattern documented
- [ ] Service registration explained
- [ ] Health check endpoints listed
- [ ] Trace/logging examples provided

### 5. Local Development
- [ ] `aspire run` starts all services
- [ ] Dashboard shows all services as "running"
- [ ] API responds on configured port
- [ ] Logs visible in dashboard
- [ ] Health endpoint returns 200 OK

---

## 🚨 Anti-Patterns to Avoid

### ❌ Don't Do This

1. **Manual Docker configuration instead of Aspire components**
   ```csharp
   // ❌ BAD
   var dockerCommand = "docker run -d postgres:17 ...";
   System.Diagnostics.Process.Start("bash", "-c", dockerCommand);
   ```

2. **Skip observability (add it later)**
   ```csharp
   // ❌ BAD
   // No logging configured initially
   // "We'll add tracing in Epic 5..."
   ```

3. **Assume framework capabilities without research**
   ```csharp
   // ❌ BAD
   // "I think .NET 10 doesn't exist, use .NET 8"
   // Always verify: https://dotnet.microsoft.com/download
   ```

4. **Custom service discovery instead of WithReference()**
   ```csharp
   // ❌ BAD
   var connectionString = "Server=localhost;...";
   
   // ✅ GOOD
   builder.AddServiceDefaults();
   builder.Services.AddHttpClient<IMyService>(...);
   ```

5. **Sidestep health checks**
   ```csharp
   // ❌ BAD
   var service = builder.AddProject<MyService>("api");
   // Missing: .WithHttpHealthCheck("/health")
   ```

---

## 📚 Resources

### Official Microsoft Aspire Documentation
- Main docs: https://learn.microsoft.com/en-us/dotnet/aspire
- Components: https://learn.microsoft.com/en-us/dotnet/aspire/components
- Orchestration: https://learn.microsoft.com/en-us/dotnet/aspire/components/orchclientgov

### .NET Versions & LTS
- .NET Download: https://dotnet.microsoft.com/download/dotnet
- Current: **.NET 10** (latest stable)
- LTS versions available

### OpenTelemetry & Observability
- OpenTelemetry: https://opentelemetry.io/docs
- Aspire tracing: https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/telemetry

### EF Core
- Entity Framework Core: https://learn.microsoft.com/en-us/ef/core
- Migrations: https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations

---

## ✅ Implementation Example: Story 2-1

**User Registration with Aspire Best Practices:**

### AppHost.cs
```csharp
var db = builder.AddPostgres("postgres")
    .AddDatabase("bmadserver", "user");

var api = builder.AddProject<Projects.ApiService>("api")
    .WithHttpHealthCheck("/health")
    .WithReference(db)
    .WaitFor(db);
```

### Program.cs
```csharp
builder.AddServiceDefaults();  // ← Tracing + Logging + Health

if (!builder.Environment.IsEnvironment("Test"))
{
    builder.AddNpgsqlDbContext<ApplicationDbContext>("postgres");
}

builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
```

### AuthenticationController.cs
```csharp
[HttpPost("/api/auth/register")]
public async Task<IActionResult> Register([FromBody] RegisterRequest request)
{
    using (var activity = _activitySource.StartActivity("UserRegistration"))
    {
        _logger.LogInformation("User registration requested: {Email}", request.Email);
        
        // Implementation with automatic tracing
        var user = await _authService.RegisterAsync(request);
        
        _logger.LogInformation("User registered: {UserId}", user.Id, 
            new { TraceId = activity?.Id });
        
        return Ok(user);
    }
}
```

### HealthCheck
```csharp
[HttpGet("/health")]
public async Task<IActionResult> Health()
{
    var dbHealthy = await _context.Database.CanConnectAsync();
    return dbHealthy ? Ok("Healthy") : StatusCode(503, "Unhealthy");
}
```

### Tests
```csharp
[Fact]
public void RegistrationService_HasObservability()
{
    Assert.NotNull(_provider.GetService<TracerProvider>());
    Assert.NotNull(_provider.GetService<ILogger>());
}
```

---

## 🎓 Summary

**Aspire Best Practices for bmadServer:**

1. ✅ **Research first** - Check official packages before implementing
2. ✅ **Aspire components** - Use official packages for orchestration
3. ✅ **Observability by default** - OpenTelemetry + structured logging from day 1
4. ✅ **AppHost pattern** - All services defined in AppHost.cs
5. ✅ **Health checks** - Every service has /health endpoint
6. ✅ **Comprehensive testing** - 10+ tests per story minimum
7. ✅ **Documentation** - Patterns documented for team reference

**For Epic 2+:** Start with these practices to avoid rework and ensure quality from the beginning.

---

**Created:** 2026-01-25  
**Status:** ✅ Active (mandatory for all future stories)  
**Approved By:** Cris (based on Epic 1 retrospective feedback)
