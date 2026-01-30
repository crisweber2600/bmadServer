# Project Setup & Quick Start Guide

## Prerequisites

### Required
- **.NET 10 SDK** (or equivalent LTS version)
  - Verify: `dotnet --version` (should show 10.0.x)
  - Download: https://dotnet.microsoft.com/download
  
- **Git** (any recent version)
  - Verify: `git --version`
  
- **PostgreSQL 17+** (via .NET Aspire - no manual install needed!)
  - Aspire handles PostgreSQL container automatically via AppHost.cs

### Recommended
- **Visual Studio Code** with C# extension (Dev Kit)
- **Visual Studio 2024** (optional, for full IDE experience)
- **curl** or **Postman** (for API testing)

### macOS Specific
- If certificate errors occur with Aspire: See **Troubleshooting** section

---

## Quick Start (Local Development)

### 1️⃣ Clone the Repository
```bash
git clone https://github.com/crisweber2600/bmadServer.git
cd bmadServer
```

### 2️⃣ Verify Prerequisites
```bash
dotnet --version        # Should show 10.0.x
git --version          # Any recent version
```

### 3️⃣ Restore Dependencies
```bash
cd src
dotnet restore
```

### 4️⃣ Build the Solution
```bash
dotnet build --configuration Release
```

### 5️⃣ Start Development Environment (Aspire)
```bash
aspire run
# or with certificate bypass (macOS):
ASPIRE_ALLOW_UNSECURED_TRANSPORT=true aspire run
```

This starts:
- 🐘 **PostgreSQL** container (port 5432)
- 🌐 **API Service** (port 8080)
- 💻 **Web Frontend** (port 5173)
- 📊 **Aspire Dashboard** (https://localhost:17360)

### 6️⃣ Verify Everything Works
```bash
# In another terminal, test the health endpoint:
curl http://localhost:8080/health

# Expected response: 200 OK with health status
```

### 7️⃣ Open the Dashboard
```
https://localhost:17360
```

You should see:
- ✅ PostgreSQL: "running" (green)
- ✅ ApiService: "running" (green)
- ✅ Web Frontend: "running" (green)

---

## Project Structure

```
bmadServer/
├── src/
│   ├── bmadServer.AppHost/              # Service orchestration (Aspire)
│   │   ├── AppHost.cs                   # Defines all services & dependencies
│   │   └── Program.cs                   # Entry point
│   │
│   ├── bmadServer.ApiService/           # REST API + SignalR hub
│   │   ├── Program.cs                   # Service registration
│   │   ├── Data/
│   │   │   ├── ApplicationDbContext.cs  # EF Core context
│   │   │   ├── Entities/                # Database models
│   │   │   └── Migrations/              # Database schema changes
│   │   └── Controllers/                 # API endpoints
│   │
│   ├── bmadServer.ServiceDefaults/      # Shared patterns
│   │   └── Extensions.cs                # Health checks, telemetry, resilience
│   │
│   ├── bmadServer.Web/                  # Frontend (React/Blazor)
│   │
│   └── bmadServer.Tests/                # Unit & integration tests
│       ├── HealthCheckTests.cs
│       └── Integration/
│           ├── DatabaseMigrationTests.cs
│           └── ApplicationStartupTests.cs
│
├── .github/workflows/                   # CI/CD automation
│   └── ci.yml                           # Build, test, deploy
│
├── .env.development                     # Local environment variables
├── bmadServer.sln                       # Solution file
├── Directory.Build.props                # Solution-wide build settings
├── README.md                            # Project overview
├── SETUP.md                             # This file
├── ARCHITECTURE.md                      # System design
└── PROJECT-WIDE-RULES.md                # Development standards

```

---

## Development Workflow

### 1. Create a Feature Branch
```bash
git checkout -b feature/my-feature
```

### 2. Make Your Changes
```bash
# Edit code in bmadServer.ApiService/ or other services
vim src/bmadServer.ApiService/Controllers/MyController.cs
```

### 3. Run Tests Locally
```bash
cd src
dotnet test -c Release
```

### 4. Start Aspire to Test Manually
```bash
aspire run
# Access API at http://localhost:8080
# Access Dashboard at https://localhost:17360
```

### 5. Commit and Push
```bash
git add .
git commit -m "feat: Add my feature"
git push origin feature/my-feature
```

### 6. Create Pull Request
- Go to https://github.com/crisweber2600/bmadServer
- Click **"New Pull Request"**
- Select your feature branch
- Fill in description and submit

### 7. CI/CD Runs Automatically
- Build job: Compiles Release configuration
- Test job: Runs all unit tests
- Both must pass before merge is allowed

---

## Adding New Services

### Example: Add a Payment Service

**1. Update AppHost.cs**
```csharp
var paymentService = builder.AddProject<Projects.bmadServer_Payment>("payment")
    .WithHttpHealthCheck("/health")
    .WithReference(db)
    .WaitFor(db);
```

**2. Update ApiService to call Payment**
```csharp
var paymentServiceUri = builder.Configuration["services:payment"];
builder.Services.AddHttpClient<IPaymentService>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri(paymentServiceUri));
```

**3. Run and Test**
```bash
aspire run
# New service will appear in dashboard
```

---

## Database Management

### View Database with pgAdmin

**Note:** pgAdmin is disabled by default for security reasons. To enable it during development:

1. Edit `src/bmadServer.AppHost/appsettings.Development.json` and set:
   ```json
   "EnablePgAdmin": true
   ```

2. Restart Aspire:
   ```bash
   aspire run
   ```

3. Access pgAdmin at:
   ```
   https://localhost:5050
   Login: admin@admin.com / admin
   ```

**Security Note:** Never enable pgAdmin in production environments as it exposes database credentials.

### Run Migrations
```bash
cd src/bmadServer.ApiService

# Create new migration
dotnet ef migrations add AddPaymentTable

# Apply migrations (happens automatically on startup)
dotnet ef database update
```

### Backup Database
```bash
# Aspire uses named volumes - data persists between restarts
docker volume ls | grep postgres  # View volumes
```

---

## Running Tests

### Unit Tests Only
```bash
cd src
dotnet test bmadServer.Tests/bmadServer.Tests.csproj -c Release
```

### Integration Tests
```bash
dotnet test bmadServer.Tests/Integration/ -c Release
```

### All Tests
```bash
dotnet test -c Release
```

### Watch Mode (auto-run on changes)
```bash
dotnet watch test
```

---

## Monitoring & Logs

### View Logs in Dashboard
1. Open https://localhost:17360
2. Click on any service
3. Scroll to **Logs** section
4. All structured logs appear with timestamps and trace IDs

### Console Logs (from Aspire)
```bash
# Aspire terminal shows real-time logs:
ApiService | 2026-01-25 14:30:45 [INF] Database connection healthy
ApiService | 2026-01-25 14:30:46 [INF] Server started on port 8080
```

### Database Queries
Enabled in development (Program.cs line 27):
```csharp
if (builder.Environment.IsDevelopment())
{
    options.EnableSensitiveDataLogging();  // See SQL queries
}
```

---

## Troubleshooting

### 🔴 Error: "Unable to configure HTTPS endpoint"
**Cause:** Certificate generation/trust failure (common on macOS)

**Solution:**
```bash
# Use HTTP in development
ASPIRE_ALLOW_UNSECURED_TRANSPORT=true aspire run

# Or use the helper script
./scripts/dev-run.sh
```

### 🔴 Error: "Port 5432 already in use"
**Cause:** PostgreSQL already running (or Aspire didn't clean up)

**Solution:**
```bash
# Kill existing containers
docker ps | grep postgres
docker kill <container-id>

# Then retry
aspire run
```

### 🔴 Error: "Cannot connect to database"
**Cause:** PostgreSQL container not started

**Solution:**
```bash
# Check Aspire dashboard - PostgreSQL should show "running"
# Wait 5-10 seconds for container to start
# Check logs in dashboard for startup errors
```

### 🔴 Error: "dotnet: command not found"
**Cause:** .NET SDK not in PATH

**Solution:**
```bash
# Verify SDK is installed
dotnet --version

# Add to PATH if needed (macOS with Homebrew)
export PATH="/usr/local/opt/dotnet/bin:$PATH"

# Then add to ~/.zshrc or ~/.bash_profile for persistence
```

### 🔴 Build fails with "MSB1003: Specify a project or solution file"
**Cause:** Running dotnet commands from wrong directory

**Solution:**
```bash
cd src                  # Navigate to src directory
dotnet build           # Now works
```

### 🔴 Tests fail: "Database connection refused"
**Cause:** Tests run in "Test" environment which skips database

**Solution:**
```bash
# This is expected! Test environment uses in-memory database
# Check Program.cs line 20:
# if (!builder.Environment.IsEnvironment("Test"))
# {
#     builder.AddNpgsqlDbContext<...>("bmadserver");
# }
```

---

## Performance Tips

### 1. Build Only Changed Projects
```bash
dotnet build --no-incremental  # Full rebuild
dotnet build                     # Incremental (faster)
```

### 2. Run Tests in Parallel
```bash
dotnet test -p:ParallelizeTestCollections=true
```

### 3. Hot Reload During Development
- Aspire automatically detects code changes
- Services restart with new code
- No need to manually restart

### 4. Database Connection Pooling
- Enabled by default via ServiceDefaults
- Minimum 5, maximum 100 connections
- Configurable in Program.cs

---

## Environment Variables

### Local Development (.env.development)
```
ASPIRE_ALLOW_UNSECURED_TRANSPORT=true
ASPNETCORE_ENVIRONMENT=Development
POSTGRES_DB=bmadserver
POSTGRES_USER=bmadserver_dev
```

### Production
```
ASPIRE_ALLOW_UNSECURED_TRANSPORT=false  # Use HTTPS!
ASPNETCORE_ENVIRONMENT=Production
POSTGRES_USER=<secure-username>
POSTGRES_PASSWORD=<secure-password>
```

---

## Support & Documentation

- **Architecture Details:** See [ARCHITECTURE.md](./ARCHITECTURE.md)
- **Development Rules:** See [PROJECT-WIDE-RULES.md](./PROJECT-WIDE-RULES.md)
- **Aspire Docs:** https://learn.microsoft.com/en-us/dotnet/aspire
- **EF Core Migration Docs:** https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations
- **GitHub Actions CI/CD:** See [.github/workflows/ci.yml](./.github/workflows/ci.yml)

---

**Last Updated:** 2026-01-25  
**Status:** ✅ Complete (Story 1-6 Initial Documentation)
