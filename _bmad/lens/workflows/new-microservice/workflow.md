---
name: new-microservice
description: Scaffold a new microservice within an existing service
---

# New Microservice Workflow

**Goal:** Scaffold a new microservice within an existing service with proper structure, documentation, and registration.

## Prerequisites

- Should be at **Service Lens** or **Microservice Lens**
- Parent service must exist
- Git repository initialized

## Execution Steps

### 1. Determine Parent Service

If not already in Service Lens:
- List available services from domain-map or auto-discovery
- Prompt user to select parent service

```yaml
parent_service:
  prompt: "Which service should this microservice belong to?"
  source: "domain-map or auto-discovered services"
  current: "{active_service if in Service Lens}"
```

### 2. Gather Microservice Information

```yaml
microservice_name:
  prompt: "Microservice name (kebab-case recommended):"
  validation: "^[a-z][a-z0-9-]*$"
  example: "auth-api, worker, admin-portal"

microservice_description:
  prompt: "Brief description of what this microservice does:"
  example: "Authentication REST API endpoints"

microservice_type:
  prompt: "Type of microservice:"
  default: "api"
  options:
    - api: "REST/GraphQL API service"
    - worker: "Background job processor"
    - gateway: "API gateway or BFF"
    - library: "Shared library/package"
    - frontend: "Web frontend application"
    - custom: "Custom type"

tech_stack:
  prompt: "Primary technology (auto-detect from service or specify):"
  auto_detect: true
  example: "dotnet, nodejs, python, go"
```

### 3. Determine Directory Structure

Based on parent service structure and project conventions:

**Standard Structure:**
```
services/{service}/
└── {microservice}/
    ├── README.md
    ├── src/
    │   └── (tech-specific structure)
    └── tests/
```

**Tech-Specific Templates:**

| Tech | Structure |
|------|-----------|
| dotnet | `src/{Microservice}.csproj`, `Program.cs` |
| nodejs | `src/index.ts`, `package.json` |
| python | `src/__init__.py`, `pyproject.toml` |
| go | `main.go`, `go.mod` |

### 4. Create Directory Structure

```bash
mkdir -p services/{service}/{microservice}/src
mkdir -p services/{service}/{microservice}/tests
```

### 5. Generate Core Files

**services/{service}/{microservice}/README.md:**
```markdown
# {microservice}

Part of the **{service}** service.

## Overview

{microservice_description}

## Type

{microservice_type}

## Responsibilities

_Key responsibilities of this microservice_

## API / Interface

_Endpoints, events, or interfaces exposed_

| Method | Endpoint | Description |
|--------|----------|-------------|
| _TBD_ | _TBD_ | _TBD_ |

## Dependencies

### Internal (within {service})
_Other microservices in this service that this depends on_

### External (other services)
_Microservices from other services_

### Third-Party
_External APIs and services_

## Local Development

```bash
# How to run locally
```

## Testing

```bash
# How to run tests
```

## Deployment

_Deployment notes and configuration_
```

**microservice.yaml (optional metadata):**
```yaml
name: {microservice}
service: {service}
type: {microservice_type}
description: "{microservice_description}"

tech_stack:
  language: {tech_stack}
  framework: _TBD_

endpoints: []

dependencies:
  internal: []
  external: []
```

### 6. Generate Tech-Specific Files

Based on `tech_stack` and `microservice_type`:

**For dotnet + api:**
- Basic .csproj file
- Program.cs with minimal API setup
- appsettings.json

**For nodejs + api:**
- package.json
- tsconfig.json (if TypeScript)
- src/index.ts

**For python + worker:**
- pyproject.toml
- src/__init__.py
- src/worker.py

### 7. Update Service Configuration

Update `services/{service}/service.yaml`:

```yaml
microservices:
  # ... existing microservices ...
  - name: {microservice}
    path: services/{service}/{microservice}
    description: "{microservice_description}"
    type: {microservice_type}
```

### 8. Update Domain Map

If `_lens/domain-map.yaml` exists, add under the service:

```yaml
services:
  {service}:
    microservices:
      # ... existing ...
      - name: {microservice}
        description: "{microservice_description}"
```

### 9. Create Feature Branch (Optional)

```bash
git checkout -b feature/{service}/{microservice}/initial-setup
git add services/{service}/{microservice}
git commit -m "feat({service}): Add {microservice} microservice"
```

### 10. Switch to Microservice Lens

After creation, switch to Microservice Lens:

```
🏘️ Microservice Lens: {microservice}
   Service: {service}
   📁 Created | 🚀 Ready to build
```

---

## Output Summary

```
✅ Created new microservice: {microservice}

📂 Structure:
   services/{service}/{microservice}/
   ├── README.md
   ├── src/
   │   └── (initial files)
   └── tests/

📝 Service config updated
📝 Domain map updated
🌿 Branch: feature/{service}/{microservice}/initial-setup (if created)

🏘️ Now in Microservice Lens: {microservice}
```

---

## Validation Checks

Before creating:
- [ ] Parent service exists
- [ ] Microservice name is unique within service
- [ ] Microservice name follows naming convention
- [ ] Target directory doesn't already exist

---

## Error Handling

| Error | Resolution |
|-------|------------|
| Service not found | Offer to create service first (link to new-service) |
| Microservice exists | Offer to edit existing or choose new name |
| Directory exists | Warn and ask to proceed or abort |
| Unknown tech stack | Use minimal structure, let user customize |
