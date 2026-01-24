# Developer Onboarding Guide - bmadServer

**Generated:** 2026-01-23  
**Target Audience:** Intermediate developers (familiar with web development)  
**Estimated Time:** 30 minutes to first working endpoint  
**Prerequisites:** .NET 10, PostgreSQL 17, Node.js 20+, Git

---

## 1. Architecture Overview (10-Minute Read)

### What is bmadServer?

bmadServer is a **web interface for BMAD** (a powerful CLI tool for product formation). It lets teams collaborate in real-time to build products, write requirements, and make architectural decisions—without using the terminal.

### Tech Stack at a Glance

**Backend:**
- **.NET 10 + ASP.NET Core** (web server)
- **SignalR** (real-time WebSocket chat)
- **PostgreSQL + JSONB** (flexible state storage)
- **Entity Framework Core 9** (database ORM)

**Frontend:**
- **React 18 + TypeScript** (UI components)
- **Zustand 4.5** (global state)
- **TanStack Query 5** (server state)
- **React Router v7** (page navigation)
- **Tailwind CSS** (styling)

**Infrastructure:**
- **Docker Compose** (MVP deployment)
- **GitHub Actions** (CI/CD)
- **.NET Aspire** (service orchestration)

### Key Concepts You'll Encounter

1. **Workflows** - A series of steps to complete a product task (PRD, Architecture, etc.)
2. **Decisions** - Key choices made during workflows (approved, rejected, locked)
3. **State** - Current progress of a workflow (stored as JSONB in PostgreSQL)
4. **Concurrency Control** - Multiple users working simultaneously without conflicts
5. **Agent Router** - Routes requests to BMAD agents (in-process for MVP)

---

## 2. Local Development Setup (5 Minutes)

### Prerequisites Installation

```bash
# Install .NET 10 SDK
# macOS:
brew install dotnet

# Linux:
sudo apt-get install dotnet-sdk-10.0

# Windows:
# Download from https://dotnet.microsoft.com/download

# Verify installation
dotnet --version  # Should be 10.x.x

# Install PostgreSQL 17
# macOS:
brew install postgresql@17

# Linux:
sudo apt-get install postgresql-17

# Windows:
# Download from https://www.postgresql.org/download/windows/

# Start PostgreSQL
postgres -D /usr/local/var/postgres  # macOS
# or use systemctl on Linux

# Create development database
createdb bmadserver_dev
```

### Clone & Build

```bash
# Clone repository
git clone https://github.com/your-org/bmadserver.git
cd bmadserver

# Restore NuGet packages
dotnet restore

# Verify build
dotnet build

# Run database migrations
cd bmadServer/bmadServer.ApiService
dotnet ef database update

# You should see: "Done. No pending migrations."
```

### Start the Application

```bash
# Terminal 1: Backend (Aspire AppHost)
cd bmadServer
dotnet run --project bmadServer.AppHost/bmadServer.AppHost.csproj

# Expected output:
# info: Microsoft.Hosting.Lifetime[14]
#       Now listening on: https://localhost:5001
# info: Aspire.Hosting.Dashboard[0]
#       Aspire dashboard available at http://localhost:17360

# Terminal 2: Frontend
cd bmadServer/client
npm install
npm run dev

# Expected output:
# ➜  Local:   http://localhost:5173/
# ➜  Press h to show help

# Terminal 3: Open browser
# Frontend:  http://localhost:5173/
# Backend API: https://localhost:5001/api/v1
# Aspire Dashboard: http://localhost:17360
# Swagger Docs: https://localhost:5001/swagger
```

### Verify Everything Works

```bash
# Test API
curl -X GET https://localhost:5001/health \
  -H "Authorization: Bearer YOUR_TEST_TOKEN"

# Expected: { "status": "Healthy" }

# Test WebSocket (SignalR)
# Open browser console:
const hub = new signalR.HubConnectionBuilder()
  .withUrl("https://localhost:5001/workflowhub")
  .build();
await hub.start();
console.log("Connected to WebSocket");
```

---

## 3. Folder Structure Walkthrough

```
bmadserver/
├── bmadServer/                          # Backend solution
│   ├── bmadServer.ApiService/          # Main API service
│   │   ├── Program.cs                   # Service configuration
│   │   ├── appsettings.json            # Settings
│   │   ├── Controllers/                 # API endpoints (Minimal APIs)
│   │   ├── Services/                    # Business logic
│   │   ├── Data/
│   │   │   ├── BmadServerContext.cs    # EF Core DbContext
│   │   │   ├── Models/                  # Entity models
│   │   │   └── Migrations/              # EF Core migrations
│   │   └── Hubs/                        # SignalR hubs
│   │
│   ├── bmadServer.AppHost/              # Aspire orchestration
│   │   ├── Program.cs                   # Configure all services
│   │   └── appsettings.json
│   │
│   └── bmadServer.ServiceDefaults/      # Shared patterns
│       └── Extensions.cs                 # Common config
│
├── client/                               # Frontend (React)
│   ├── src/
│   │   ├── App.tsx                      # Root component
│   │   ├── main.tsx                     # Entry point
│   │   ├── features/
│   │   │   ├── auth/                    # Authentication feature
│   │   │   ├── workflows/               # Workflow management
│   │   │   ├── decisions/               # Decision tracking
│   │   │   └── collaborators/           # Team collaboration
│   │   ├── shared/                      # Shared components
│   │   ├── stores/                      # Zustand stores
│   │   └── api/                         # API client functions
│   │
│   ├── package.json                     # Dependencies
│   ├── tsconfig.json                    # TypeScript config
│   └── vite.config.ts                   # Vite build config
│
├── docker-compose.yml                   # MVP deployment
├── Dockerfile                           # Container image
└── README.md                            # Getting started
```

**Key Files to Know:**

| File | Purpose |
|------|---------|
| `Program.cs` (ApiService) | Register services, middleware setup |
| `BmadServerContext.cs` | Database configuration, entity mappings |
| `WorkflowHub.cs` | Real-time communication endpoints |
| `AuthenticationController.cs` | Login/token endpoints |
| `App.tsx` | Frontend root component |
| `main.tsx` | React app initialization |
| `appsettings.json` | Configuration (connection strings, JWT keys) |
| `docker-compose.yml` | Run full stack with one command |

---

## 4. First API Endpoint: Create a Workflow

### Step 1: Create Entity Model

**File:** `bmadServer/bmadServer.ApiService/Data/Models/Workflow.cs`

```csharp
public class Workflow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string WorkflowType { get; set; } = string.Empty;  // "prd", "architecture", etc.
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Column(TypeName = "jsonb")]
    public JsonDocument State { get; set; } = JsonDocument.Parse("{}");

    public int Version { get; set; } = 1;
    public string? LastModifiedBy { get; set; }
    public DateTime? LastModifiedAt { get; set; }
}
```

### Step 2: Add DbContext Configuration

**File:** `BmadServerContext.cs`

```csharp
public DbSet<Workflow> Workflows { get; set; }

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Workflow>()
        .Property(w => w.State)
        .HasColumnType("jsonb");

    modelBuilder.Entity<Workflow>()
        .HasIndex(w => w.State)
        .HasMethod("gin");
}
```

### Step 3: Create Database Migration

```bash
cd bmadServer/bmadServer.ApiService
dotnet ef migrations add CreateWorkflowTable
dotnet ef database update

# Output: "Done. Applying migration '20260123_CreateWorkflowTable'..."
```

### Step 4: Create Service

**File:** `Services/WorkflowService.cs`

```csharp
public class WorkflowService
{
    private readonly BmadServerContext _context;
    private readonly ILogger<WorkflowService> _logger;

    public async Task<Workflow> CreateAsync(
        string workflowType,
        Guid userId,
        CancellationToken ct = default)
    {
        var workflow = new Workflow
        {
            WorkflowType = workflowType,
            CreatedByUserId = userId
        };

        _context.Workflows.Add(workflow);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Workflow created: {WorkflowId}", workflow.Id);
        return workflow;
    }
}
```

### Step 5: Register Service in Program.cs

```csharp
builder.Services.AddScoped<WorkflowService>();
```

### Step 6: Create API Endpoint

**File:** `Program.cs` (add after `builder.Build()`)

```csharp
app.MapPost("/api/v1/workflows", CreateWorkflow)
    .WithName("CreateWorkflow")
    .WithOpenApi()
    .Produces<Workflow>(StatusCodes.Status201Created)
    .RequireAuthorization();

async Task<IResult> CreateWorkflow(
    CreateWorkflowRequest request,
    WorkflowService workflowService,
    ILogger<Program> logger,
    ClaimsPrincipal user)
{
    var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? throw new UnauthorizedAccessException();

    var workflow = await workflowService.CreateAsync(
        request.WorkflowType,
        Guid.Parse(userId));

    return Results.Created($"/api/v1/workflows/{workflow.Id}", workflow);
}

// Request DTO
public record CreateWorkflowRequest(string WorkflowType);
```

### Step 7: Test Your Endpoint

```bash
# In Swagger UI: https://localhost:5001/swagger
# Or via curl:
curl -X POST https://localhost:5001/api/v1/workflows \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TEST_TOKEN" \
  -d '{ "workflowType": "prd" }'

# Expected response:
# {
#   "id": "550e8400-e29b-41d4-a716-446655440000",
#   "workflowType": "prd",
#   "createdAt": "2026-01-23T10:30:00Z",
#   "version": 1
# }
```

✅ **Congratulations! You've created your first endpoint.**

---

## 5. First React Component: Workflow List

### Step 1: Create Custom Hook

**File:** `client/src/features/workflows/hooks/useWorkflows.ts`

```typescript
import { useQuery } from "@tanstack/react-query";

export function useWorkflows() {
  return useQuery({
    queryKey: ["workflows"],
    queryFn: async () => {
      const response = await fetch("/api/v1/workflows");
      if (!response.ok) throw new Error("Failed to fetch workflows");
      return response.json();
    },
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
}
```

### Step 2: Create Component

**File:** `client/src/features/workflows/components/WorkflowList.tsx`

```typescript
import React from "react";
import { useWorkflows } from "../hooks/useWorkflows";

export function WorkflowList() {
  const { data: workflows, isLoading, error } = useWorkflows();

  if (isLoading) {
    return <div className="p-4">Loading workflows...</div>;
  }

  if (error) {
    return <div className="p-4 text-red-600">Error: {error.message}</div>;
  }

  return (
    <div className="p-4">
      <h1 className="text-2xl font-bold mb-4">Workflows</h1>
      <div className="space-y-2">
        {workflows?.map((workflow: any) => (
          <div key={workflow.id} className="p-3 border rounded">
            <h2 className="font-semibold">{workflow.workflowType}</h2>
            <p className="text-gray-600 text-sm">
              Created: {new Date(workflow.createdAt).toLocaleDateString()}
            </p>
          </div>
        ))}
      </div>
    </div>
  );
}
```

### Step 3: Use in App

**File:** `client/src/App.tsx`

```typescript
import { WorkflowList } from "./features/workflows/components/WorkflowList";

export function App() {
  return (
    <div>
      <WorkflowList />
    </div>
  );
}
```

### Step 4: Test in Browser

```bash
# Frontend should already be running on http://localhost:5173
# You should see the workflow list with your newly created workflow
```

✅ **Your first React component is working!**

---

## 6. First Database Model: User Authentication

### Entity + Migration + Endpoint

```csharp
// Step 1: Entity
public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string[] Roles { get; set; } = ["Participant"];
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// Step 2: Add to DbContext
public DbSet<User> Users { get; set; }

// Step 3: Create migration
dotnet ef migrations add CreateUserTable
dotnet ef database update

// Step 4: Create login service
public class AuthenticationService
{
    public async Task<LoginResponse> LoginAsync(string email, string password)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email)
            ?? throw new UnauthorizedAccessException("Invalid credentials");

        var isPasswordValid = VerifyPassword(password, user.PasswordHash);
        if (!isPasswordValid)
            throw new UnauthorizedAccessException("Invalid credentials");

        var token = GenerateJwt(user);
        return new LoginResponse(token, user.Email);
    }
}

// Step 5: Add login endpoint
app.MapPost("/api/v1/auth/login", Login);

// Step 6: Test
curl -X POST https://localhost:5001/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{ "email": "cris@example.com", "password": "password123" }'
```

---

## 7. Running Tests Locally

### Backend Tests

```bash
# Create test project (if not exists)
dotnet new xunit -n BmadServer.Tests

# Add to solution
dotnet sln add BmadServer.Tests

# Run tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true

# Run specific test
dotnet test --filter "TestName=CreateWorkflow_ValidInput_Success"
```

### Frontend Tests

```bash
cd client

# Run all tests
npm test

# Run in watch mode
npm test -- --watch

# Run specific test
npm test -- WorkflowList.test.tsx
```

---

## 8. Common Troubleshooting

| Problem | Solution |
|---------|----------|
| **Port 5001 already in use** | `lsof -i :5001` then `kill -9 <PID>` |
| **PostgreSQL connection failed** | Verify `appsettings.json` has correct connection string |
| **CORS error in browser** | Add origin to CORS policy in `Program.cs` |
| **Database migration failed** | Check migration file in `/Data/Migrations`, run `dotnet ef database update` |
| **WebSocket connection refused** | Verify SignalR hub is mapped in `Program.cs` |
| **React module not found** | Run `npm install` in client folder |
| **Token expires quickly** | Check JWT expiry in auth config (should be 15 minutes) |

---

## 9. Performance Baseline Check

```bash
# Check response time (should be < 100ms)
curl -w "%{time_total}\n" -o /dev/null -s \
  https://localhost:5001/api/v1/workflows

# Expected output: 0.025 (25ms)

# Load test with 100 requests
ab -n 100 -c 10 https://localhost:5001/health

# Monitor database query performance
# In appsettings.json, enable EF Core logging:
"Logging": {
  "LogLevel": {
    "Microsoft.EntityFrameworkCore.Database.Command": "Debug"
  }
}
```

---

## 10. Next Steps

### What to Do Next

1. **Read ADRs** - Understand design decisions in `_bmad-output/planning-artifacts/adr/`
2. **Implement a Feature** - Pick a small feature from the backlog
3. **Write Tests** - Add unit + integration tests
4. **Review Code** - Get peer review before merging
5. **Deploy Locally** - Try Docker Compose deployment

### Useful Commands Reference

```bash
# Backend
dotnet build                           # Compile
dotnet run                             # Start
dotnet test                            # Run tests
dotnet ef migrations add NAME          # Create migration
dotnet ef database update              # Apply migrations

# Frontend
npm install                            # Install dependencies
npm run dev                            # Start dev server
npm run build                          # Build for production
npm test                               # Run tests
npm run lint                           # Check code style

# Git
git status                             # See changes
git add .                              # Stage changes
git commit -m "message"                # Commit
git push origin feature-name           # Push branch
git pull                               # Get latest
```

### Key Documentation

- **Architecture Decisions:** `/planning-artifacts/adr/`
- **Implementation Patterns:** `/planning-artifacts/implementation-patterns.md`
- **API Swagger Docs:** `https://localhost:5001/swagger`
- **Aspire Dashboard:** `http://localhost:17360`

---

## Getting Help

**Questions?**
- Check existing ADRs (decision context)
- Review `implementation-patterns.md` (code examples)
- Ask in team chat (Slack/Discord)
- Create a GitHub issue

**Found a Bug?**
- Create an issue with reproduction steps
- Include logs/error messages
- Link relevant ADRs

---

**Welcome to the team! 🚀**
