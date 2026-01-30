---
name: new-service
description: Create new bounded context/service at Domain Lens
---

# New Service Workflow

**Goal:** Create a new bounded context/service with proper structure, documentation, and registration in the domain map.

## Prerequisites

- Should be at **Domain Lens** or **Service Lens**
- Git repository initialized
- LENS activated (auto or explicit)

## Execution Steps

### 1. Gather Service Information

Prompt user for:

```yaml
service_name:
  prompt: "Service name (kebab-case recommended):"
  validation: "^[a-z][a-z0-9-]*$"
  example: "identity, payments, notifications"

service_description:
  prompt: "Brief description of what this service does:"
  example: "Authentication and authorization service"

service_owner:
  prompt: "Team/person responsible for this service:"
  example: "platform-team, @username"

initial_microservices:
  prompt: "Initial microservices (comma-separated, or 'none'):"
  default: "api"
  example: "api, worker, admin"
```

### 2. Determine Directory Structure

Based on project conventions (auto-detect or config):

**Option A: Standard Structure**
```
services/
└── {service_name}/
    ├── README.md
    ├── service.yaml
    └── {microservice}/
        ├── README.md
        └── src/
```

**Option B: Flat Structure**
```
src/
└── {service_name}/
    ├── README.md
    ├── service.yaml
    └── {microservice}/
```

**Option C: Custom (from lens-config.yaml)**
Use `service_path_pattern` from config.

### 3. Create Directory Structure

```bash
mkdir -p services/{service_name}
```

For each initial microservice:
```bash
mkdir -p services/{service_name}/{microservice}/src
```

### 4. Generate Service Files

**services/{service_name}/README.md:**
```markdown
# {service_name}

{service_description}

## Overview

_Service overview and purpose_

## Microservices

| Microservice | Description | Status |
|--------------|-------------|--------|
{for each microservice}
| {name} | _Description_ | 🟢 Active |
{end for}

## Dependencies

_External dependencies and other services this depends on_

## Getting Started

_How to run and develop this service locally_

## Team

**Owner:** {service_owner}
```

**services/{service_name}/service.yaml:**
```yaml
# Service Definition
name: {service_name}
description: "{service_description}"
owner: {service_owner}

microservices:
{for each microservice}
  - name: {microservice}
    path: services/{service_name}/{microservice}
    description: "_Description_"
{end for}

dependencies: []

docs:
  - README.md
```

### 5. Generate Microservice Stubs

For each initial microservice, create:

**services/{service_name}/{microservice}/README.md:**
```markdown
# {microservice}

Part of the **{service_name}** service.

## Responsibilities

_What this microservice does_

## API

_Endpoints or interfaces exposed_

## Dependencies

_What this microservice depends on_
```

### 6. Update Domain Map

If `_lens/domain-map.yaml` exists, add the new service:

```yaml
services:
  # ... existing services ...
  {service_name}:
    description: "{service_description}"
    owner: {service_owner}
    microservices:
{for each microservice}
      - name: {microservice}
        description: "_Description_"
{end for}
```

If no domain-map.yaml, offer to create one.

### 7. Create Service Branch (Optional)

If user wants:
```bash
git checkout -b service/{service_name}
git add services/{service_name}
git commit -m "feat: Initialize {service_name} service"
```

### 8. Switch to Service Lens

After creation, switch to Service Lens for the new service:

```
🗺️ Service Lens: {service_name}
   🏘️ {microservice_count} microservices | 📄 Created
   Ready to build!
```

---

## Output Summary

```
✅ Created new service: {service_name}

📂 Structure:
   services/{service_name}/
   ├── README.md
   ├── service.yaml
   └── {microservice}/
       └── README.md

📝 Domain map updated
🌿 Branch: service/{service_name} (if created)

🗺️ Now in Service Lens: {service_name}
```

---

## Validation Checks

Before creating:
- [ ] Service name is unique (not in domain-map)
- [ ] Service name follows naming convention
- [ ] Target directory doesn't already exist
- [ ] Git working tree is clean (if creating branch)

---

## Error Handling

| Error | Resolution |
|-------|------------|
| Service name exists | Offer to edit existing or choose new name |
| Directory exists | Warn and ask to proceed or abort |
| Invalid name format | Show format requirements |
| Git dirty | Warn about uncommitted changes |
