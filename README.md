# bmadServer: Cloud-Native Workflow Orchestration Platform

A real-time, multi-agent workflow orchestration system built with .NET Aspire, featuring distributed tracing, event-driven architecture, and collaborative decision-making capabilities.

## 📚 Documentation

- **[Quick Start & Setup Guide](./SETUP.md)** ← Start here!
  - Installation prerequisites
  - Local development setup
  - Troubleshooting guide
  - Adding new services

- **[System Architecture](./ARCHITECTURE.md)** ← Understand the design
  - Component diagram
  - Data flow
  - Technology choices
  - Deployment strategies

- **[Project-Wide Rules](./PROJECT-WIDE-RULES.md)** ← Development standards
  - Code conventions
  - Testing requirements
  - Aspire best practices

## 🚀 Quick Start

### 1. Prerequisites

- **.NET 10 SDK** (verify: `dotnet --version`)
- **Git** (verify: `git --version`)
- **Docker Desktop** (for Aspire container management)

### 2. Clone & Setup

```bash
git clone https://github.com/crisweber2600/bmadServer.git
cd bmadServer/src
dotnet restore
```

### 3. Run Aspire

```bash
aspire run
# or on macOS if certificate issues:
ASPIRE_ALLOW_UNSECURED_TRANSPORT=true aspire run
```

Dashboard: https://localhost:17360  
API: http://localhost:8080  
Health: http://localhost:8080/health

## 📋 Project Structure

### Core Services

| Service | Port | Purpose |
|---------|------|---------|
| **AppHost** | N/A | Service orchestration & service discovery |
| **ApiService** | 8080 | REST API & SignalR WebSocket hub |
| **ServiceDefaults** | N/A | Shared resilience, telemetry, and health checks |
| **Web** | (auto) | Frontend application |

### Key Directories

```
src/
├── bmadServer.AppHost/           # Service orchestration (runs Aspire dashboard)
│   └── Program.cs                # Defines all services and relationships
├── bmadServer.ApiService/        # REST API & real-time communication
│   └── Program.cs                # Service registration & middleware pipeline
├── bmadServer.ServiceDefaults/   # Shared patterns and utilities
│   └── Extensions.cs             # Health checks, logging, OpenTelemetry
├── bmadServer.Web/               # Frontend (React/Blazor)
└── bmadServer.sln                # Solution file
```

## 🔧 Service Registration Patterns

### Adding a New Service

All services are configured in `AppHost/Program.cs` and follow this pattern:

```csharp
// Example: Adding a PostgreSQL database
var postgres = builder.AddPostgres("postgres");
var db = postgres.AddDatabase("bmadserver_dev");

// Adding an API service with database reference
var api = builder.AddProject<Projects.bmadServer_ApiService>("apiservice")
    .WithReference(db);  // Automatic service discovery
```

### Service Registration in ApiService

In `ApiService/Program.cs`, services are registered like this:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults (health checks, telemetry, service discovery)
builder.AddServiceDefaults();

// Add application-specific services
builder.Services.AddSignalR();                          // Real-time communication
builder.Services.AddDbContext<ApplicationDbContext>();  // Database
builder.Services.AddAuthentication();                   // Security
builder.Services.AddAuthorization();

var app = builder.Build();

// Configure middleware pipeline
app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

// Map endpoints
app.MapHub<ChatHub>("/hubs/chat");                     // SignalR hub
app.MapControllers();
app.MapDefaultEndpoints();                              // Health checks, etc.

app.Run();
```

## 🏥 Health Checks

### Endpoint: `/health`

Returns the overall health status of all dependencies:

```bash
curl http://localhost:8080/health

# Response (when healthy):
{
  "status": "Healthy"
}
```

**Included Checks:**
- ✅ Application is responsive
- ✅ Database connection (when PostgreSQL is configured)
- ✅ All required services are reachable

### Endpoint: `/alive`

Liveness probe for orchestration tools (Kubernetes, etc.):

```bash
curl http://localhost:8080/alive
```

## 📊 Observability

### Structured Logging

All logs are output as structured JSON with trace IDs automatically included:

```json
{
  "Timestamp": "2026-01-24T14:30:45.1234567Z",
  "Level": "Information",
  "Message": "Request received",
  "TraceId": "0hfhf3k5r7m2n9q1",
  "SpanId": "a1b2c3d4e5f6g7h8"
}
```

**Configuration:** See `ServiceDefaults/Extensions.cs:ConfigureOpenTelemetry()`

### Distributed Tracing

OpenTelemetry is pre-configured to:
- ✅ Trace HTTP requests across services
- ✅ Include gRPC calls (when enabled)
- ✅ Generate trace IDs for correlation
- ✅ Export to OTLP endpoint (when configured)

**Enable OTEL Export:**

```bash
export OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
aspire run
```

### Aspire Dashboard

Access the Aspire dashboard at: **https://localhost:17360**

Shows:
- ✅ All running services and their status
- ✅ Resource utilization (CPU, memory)
- ✅ Logs from all services (in real-time)
- ✅ Structured trace information
- ✅ OpenTelemetry metrics

## 🔌 Integration Points for Developers

### Adding Authentication

**Location:** `ServiceDefaults/Extensions.cs` or `ApiService/Program.cs`

```csharp
// In ApiService/Program.cs
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer(/*...*/);

builder.Services.AddAuthorization();
```

**Middleware mapping:**
```csharp
app.UseAuthentication();
app.UseAuthorization();
```

### Adding a Database

**Step 1:** Add PostgreSQL component in `src` directory
```bash
cd src
aspire add PostgreSQL.Server
```

**Step 2:** Configure in `AppHost/Program.cs`
```csharp
var postgres = builder.AddPostgres("postgres");
var bmadDb = postgres.AddDatabase("bmadserver_dev");

var api = builder.AddProject<Projects.bmadServer_ApiService>("apiservice")
    .WithReference(bmadDb);
```

**Step 3:** Use connection string in `ApiService/Program.cs`
```csharp
builder.Services.AddDbContext<ApplicationDbContext>();

// Connection string is automatically injected from Aspire
```

### Adding Real-Time Communication (SignalR)

**Location:** `ApiService/Program.cs`

```csharp
// Service registration
builder.Services.AddSignalR();

// Endpoint mapping
app.MapHub<ChatHub>("/hubs/chat");
```

**Hub implementation:**
```csharp
public class ChatHub : Hub
{
    public async Task SendMessage(string user, string message)
    {
        await Clients.All.SendAsync("ReceiveMessage", user, message);
    }
}
```

### Adding New Controllers

**Location:** `ApiService/Controllers/` directory

```csharp
[ApiController]
[Route("api/[controller]")]
public class ExampleController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { message = "Hello" });
    }
}
```

## 🛠️ Development Workflow

### Daily Development Loop

```bash
cd src
aspire run
# All services start automatically
# Dashboard opens at https://localhost:17360
```

### Creating Database Migrations (EF Core)

```bash
# Terminal 1: Keep aspire run active
cd src
aspire run

# Terminal 2: Create and apply migrations
cd src
dotnet ef migrations add InitialCreate --project bmadServer.ApiService
dotnet ef database update --project bmadServer.ApiService
```

### Running Tests

```bash
cd src
dotnet test
```

### Building for Deployment

```bash
cd src
dotnet publish -c Release -o ./publish
```

## 📚 Architecture References

- **Framework**: .NET 8.0 with ASP.NET Core + Aspire
- **Orchestration**: .NET Aspire (AppHost pattern)
- **Database**: PostgreSQL (added in Epic 1, Story 2)
- **Real-Time**: SignalR WebSocket (added in Epic 3)
- **State Management**: Event Log + JSONB (Epic 9)
- **Agents**: Multi-agent system (Epic 5)

## 🔑 Key Configuration Files

| File | Purpose |
|------|---------|
| `AppHost/Program.cs` | Service definitions and service discovery configuration |
| `ApiService/Program.cs` | Service registration, middleware, endpoint mapping |
| `ServiceDefaults/Extensions.cs` | Shared health checks, logging, OpenTelemetry configuration |
| `bmadServer.sln` | Solution file (all projects listed here) |

## 📖 Official Documentation

- **Aspire Docs**: https://aspire.dev
- **GitHub Repo**: https://github.com/microsoft/aspire
- **Samples**: https://github.com/microsoft/aspire-samples

## ⚠️ Important: Use Aspire CLI

**ALWAYS use Aspire commands before dotnet for project setup:**

```bash
# ✅ CORRECT
aspire new aspire-starter --name MyProject
aspire add PostgreSQL.Server
aspire run

# ❌ INCORRECT
dotnet new aspnet --name MyProject
dotnet run
```

See [PROJECT-WIDE-RULES.md](./PROJECT-WIDE-RULES.md) for detailed guidelines.

## 🐛 Troubleshooting

### Port Already in Use

If you get a port conflict error:

```bash
# Find process using port 17360 (Aspire dashboard)
lsof -i :17360

# Kill the process
kill -9 <PID>

# Try again
aspire run
```

### Build Fails

```bash
# Clean build
cd src
dotnet clean
dotnet build
```

### Health Check Not Responding

```bash
# Verify service is running in Aspire dashboard: https://localhost:17360
# Check logs in dashboard for any errors
# Verify endpoint URL: http://localhost:8080/health
```

## 📝 Development Guidelines

1. **Always run with `aspire run`** - ensures correct service discovery
2. **Check Aspire dashboard** - easy way to see all services and logs
3. **Use service discovery** - don't hardcode localhost/connection strings
4. **Health checks** - include in all new services
5. **Structured logging** - automatic via OpenTelemetry (no extra config needed)

---

**Last Updated:** January 24, 2026  
**Status:** Active - Production Foundation  
**Reference:** Epic 1, Story 1-1
