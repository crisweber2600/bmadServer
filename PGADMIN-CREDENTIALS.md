# PgAdmin Credentials Feature Flag

## Overview

The AppHost now supports a feature flag to control whether PgAdmin uses custom credentials or default credentials. This is useful for debugging scenarios where you don't want to manage credentials.

## Configuration

### Default Behavior (Debugging/Development)

By default, the `PgAdmin:UseCredentials` flag is set to `false` in both `appsettings.json` and `appsettings.Development.json`. This means:

- PgAdmin will use its default credentials
- No user secrets or environment variables are required
- This is the recommended setting for local development and debugging

### Enabling Custom Credentials (Production)

To enable custom credentials for PgAdmin:

1. Update your configuration file or environment variables:
   ```json
   {
     "PgAdmin": {
       "UseCredentials": true
     }
   }
   ```

2. Configure the credentials using user secrets:
   ```bash
   cd src/bmadServer.AppHost
   dotnet user-secrets set "Parameters:pgadminusername" "admin@example.com"
   dotnet user-secrets set "Parameters:pgadminpassword" "your-secure-password"
   ```

   Or using environment variables:
   ```bash
   export Parameters__pgadminusername="admin@example.com"
   export Parameters__pgadminpassword="your-secure-password"
   ```

   Note: Environment variables use double underscores (__) to represent nested configuration keys.

## How It Works

- **When `UseCredentials` is `false`** (default for debugging):
  - PgAdmin starts with default credentials
  - No additional configuration needed
  - Simplifies local development workflow

- **When `UseCredentials` is `true`** (production/staging):
  - PgAdmin requires `pgadminusername` and `pgadminpassword` parameters
  - Credentials must be configured via user secrets or environment variables
  - If parameters are not configured, Aspire will prompt for them at runtime
  - Provides better security for non-development environments

## Security Considerations

- Never commit credentials to source control
- Use user secrets for local development
- Use environment variables or Azure Key Vault for production deployments
- The `pgadmin-password` parameter is marked as secret and will be masked in logs
