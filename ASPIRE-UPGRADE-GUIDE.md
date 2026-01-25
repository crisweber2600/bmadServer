# Aspire Project Upgrade Guide

## 📊 Current Project Status

### Current Aspire Package Versions

| Package | Current | Latest | Status |
|---------|---------|--------|--------|
| **Aspire.Hosting.AppHost** | 8.2.2 | 13.1.0 | ⚠️ **Needs Update** |
| Microsoft.Extensions.Http.Resilience | 8.10.0 | Latest | ✅ Current |
| Microsoft.Extensions.ServiceDiscovery | 8.2.2 | 13.1.0 | ⚠️ **Needs Update** |
| OpenTelemetry.Exporter.OpenTelemetryProtocol | 1.9.0 | Latest | ✅ Current |
| OpenTelemetry.Extensions.Hosting | 1.9.0 | Latest | ✅ Current |
| OpenTelemetry.Instrumentation.AspNetCore | 1.9.0 | Latest | ✅ Current |
| OpenTelemetry.Instrumentation.Http | 1.9.0 | Latest | ✅ Current |
| OpenTelemetry.Instrumentation.Runtime | 1.9.0 | Latest | ✅ Current |

### Aspire CLI Status

```bash
$ aspire --version
13.1.0+8a4db1775c3fbae1c602022b636299cb04971fde
```

**Aspire CLI is already at the latest version!** ✅

---

## 🔄 Upgrade Process

### Step 1: Understand What `aspire update` Does

The `aspire update` command is a **preview feature** that automatically upgrades Aspire packages to newer versions while:

- ✅ Maintaining Central Package Management (CPM) compatibility
- ✅ Handling diamond dependencies (when multiple packages depend on the same library)
- ✅ Respecting your configured Aspire channel (stable, preview, or daily)
- ✅ Updating package sources if needed
- ✅ Backing up your project file before making changes

### Step 2: Run the Upgrade (Interactive Mode)

To upgrade interactively (allows you to review changes):

```bash
cd /Users/cris/bmadServer

# Run in interactive mode - shows what will change and asks for confirmation
aspire update
```

**Interactive mode shows:**
- Which packages will be updated
- Old version → New version
- Prompts you to confirm before making changes

### Step 3: Non-Interactive Upgrade (Automated)

For CI/CD pipelines or automated updates:

```bash
cd /Users/cris/bmadServer

# Run without prompts - auto-confirms all changes
aspire update --non-interactive
```

### Step 4: Upgrade to a Specific Channel

Aspire offers different release channels:

```bash
# Stable channel (default - recommended for production)
aspire update --channel stable

# Daily channel (cutting-edge, bleeding-edge features)
aspire update --channel daily
```

### Step 5: Verify the Build

After upgrading:

```bash
cd src
dotnet clean
dotnet build
```

Expected output:
```
Build succeeded with 0 errors and 0 warnings.
```

---

## 🔧 Manual Upgrade (If Needed)

If `aspire update` encounters issues, you can manually upgrade specific packages:

### Update AppHost Package

In `src/bmadServer.AppHost/bmadServer.AppHost.csproj`:

```xml
<!-- Change from: -->
<PackageReference Include="Aspire.Hosting.AppHost" Version="8.2.2" />

<!-- To: -->
<PackageReference Include="Aspire.Hosting.AppHost" Version="13.1.0" />
```

### Update Service Discovery Package

In `src/bmadServer.ServiceDefaults/bmadServer.ServiceDefaults.csproj`:

```xml
<!-- Change from: -->
<PackageReference Include="Microsoft.Extensions.ServiceDiscovery" Version="8.2.2" />

<!-- To: -->
<PackageReference Include="Microsoft.Extensions.ServiceDiscovery" Version="13.1.0" />
```

### Restore and Build

```bash
cd src
dotnet restore
dotnet build
```

---

## 📋 What Gets Updated in This Project

### Files That Will Change

When you run `aspire update`, these files will be modified:

1. **`src/bmadServer.AppHost/bmadServer.AppHost.csproj`**
   - Aspire.Hosting.AppHost: 8.2.2 → 13.1.0

2. **`src/bmadServer.ServiceDefaults/bmadServer.ServiceDefaults.csproj`**
   - Microsoft.Extensions.ServiceDiscovery: 8.2.2 → 13.1.0

3. **`.aspire/settings.json`** (auto-generated)
   - Stores Aspire configuration and upgrade history
   - Safe to track in git or add to `.gitignore`

### Files That Will NOT Change

- `src/bmadServer.ApiService/bmadServer.ApiService.csproj` - Already pinned correctly
- `src/bmadServer.Web/bmadServer.Web.Web.csproj` - Web framework, not Aspire-specific
- All OpenTelemetry packages - Already at compatible versions (1.9.0+)
- All resilience packages - Already compatible

---

## 🚀 Recommended Upgrade Path

### For Development (Recommended)

```bash
# 1. Create a feature branch for the upgrade
git checkout -b chore/aspire-update

# 2. Run the upgrade interactively to see what changes
cd /Users/cris/bmadServer
aspire update

# 3. Review the changes
git diff

# 4. Build and test
cd src
dotnet clean
dotnet build
aspire run

# 5. Commit the changes
git add .
git commit -m "chore: upgrade Aspire packages to 13.1.0"

# 6. Push and create a PR
git push origin chore/aspire-update
```

### For CI/CD (Automated)

```bash
# Run in non-interactive mode
aspire update --non-interactive

# Then build and test
dotnet clean
dotnet build
dotnet test
```

---

## ✅ After the Upgrade

### Verify Everything Works

```bash
# 1. Build succeeds
cd src
dotnet build

# 2. Run the project
aspire run

# 3. Check Aspire dashboard at https://localhost:17360
# 4. Call health endpoint
curl http://localhost:8080/health

# 5. Run any tests
dotnet test
```

### Expected Improvements with 13.1.0

- 🚀 **Performance improvements** in Aspire orchestration
- 🔒 **Security updates** to dependencies
- 🐛 **Bug fixes** since version 8.2.2
- 📊 **Enhanced telemetry** and observability
- ✨ **New features** (check Aspire release notes)

---

## 📚 OpenCode Aspire MCP Configuration

The `.opencode/opencode.json` file has been updated with Aspire MCP server configuration:

```json
{
  "$schema": "https://opencode.ai/config.json",
  "plugin": [
    {
      "name": "aspire-mcp",
      "type": "mcp",
      "enabled": true,
      "command": "aspire",
      "args": ["mcp", "server"],
      "env": {
        "DOTNET_CLI_TELEMETRY_OPTOUT": "1"
      }
    }
  ],
  "mcpServers": {
    "aspire": {
      "command": "aspire",
      "args": ["mcp", "server"],
      "description": "Aspire MCP server for .NET Aspire project management and orchestration",
      "env": {
        "DOTNET_CLI_TELEMETRY_OPTOUT": "1"
      }
    }
  }
}
```

### What This Enables

The MCP server provides OpenCode (and AI assistants) access to:

- ✅ **Resource Management** - List, configure, and manage Aspire resources
- ✅ **Health Monitoring** - Check real-time status of services
- ✅ **Logging & Traces** - Access structured logs and distributed traces
- ✅ **Resource Commands** - Execute diagnostics and debugging commands
- ✅ **Service Discovery** - Understand service mesh and connections
- ✅ **Integration Docs** - Reference how to integrate new services

### Using Aspire MCP in OpenCode

Once configured, you can ask OpenCode questions like:

```
"What services are currently running?"
"Show me logs from the ApiService"
"What's the health status of the database?"
"How do I add a new service to the Aspire project?"
```

---

## ⚙️ Aspire CLI Quick Reference

| Command | Purpose | Example |
|---------|---------|---------|
| `aspire --version` | Check CLI version | `aspire --version` |
| `aspire new` | Create new Aspire project | `aspire new aspire-starter` |
| `aspire add` | Add component to project | `aspire add PostgreSQL.Server` |
| `aspire run` | Start development environment | `aspire run` |
| `aspire update` | Upgrade packages | `aspire update` |
| `aspire update --self` | Upgrade CLI itself | `aspire update --self` |
| `aspire mcp init` | Set up MCP server | `aspire mcp init` |
| `aspire mcp server` | Start MCP server | `aspire mcp server` |
| `aspire deploy` | Deploy to Azure | `aspire deploy` |

---

## 🔗 Resources

- **Aspire Docs**: https://aspire.dev
- **Release Notes**: https://github.com/microsoft/aspire/releases
- **GitHub Issues**: https://github.com/microsoft/aspire/issues
- **Samples**: https://github.com/microsoft/aspire-samples

---

**Last Updated:** January 24, 2026  
**Status:** Ready for Upgrade  
**Current Aspire CLI Version:** 13.1.0
