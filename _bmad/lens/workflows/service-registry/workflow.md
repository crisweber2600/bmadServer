---
name: service-registry
description: Manage service and microservice mappings
---

# Service Registry Workflow

**Goal:** View, search, and manage the registry of services and microservices.

## Use Cases

1. **List** — Show all registered services and microservices
2. **Search** — Find services by name, owner, or description
3. **Info** — Get detailed information about a specific service
4. **Edit** — Update service metadata
5. **Dependencies** — View and manage service dependencies

## Execution Steps

### 1. Select Operation

```yaml
operation:
  prompt: "Service Registry Operation:"
  options:
    - list: "List all services"
    - search: "Search services"
    - info: "Get service details"
    - edit: "Edit service metadata"
    - deps: "View/manage dependencies"
    - export: "Export registry"
```

---

## Operation: List

### Display All Services

```
📦 Service Registry

┌──────────────────────────────────────────────────────────────┐
│ Service          │ Microservices │ Owner       │ Status      │
├──────────────────────────────────────────────────────────────┤
│ identity         │ 3             │ @platform   │ 🟢 Active   │
│ payments         │ 2             │ @payments   │ 🟢 Active   │
│ notifications    │ 2             │ @comms      │ 🟡 Partial  │
│ analytics        │ 1             │ @data       │ 🔴 Inactive │
└──────────────────────────────────────────────────────────────┘

Total: {service_count} services | {microservice_count} microservices

[View service details] [Search] [Export]
```

### Status Indicators

| Status | Meaning |
|--------|---------|
| 🟢 Active | All microservices present and documented |
| 🟡 Partial | Some microservices missing docs or config |
| 🔴 Inactive | Service defined but directory not found |
| ⚠️ Drift | Structure differs from config |

---

## Operation: Search

### Search Query

```yaml
search_query:
  prompt: "Search for:"
  example: "auth, payment, @platform-team"

search_scope:
  options:
    - all: "Search all fields"
    - name: "Service/microservice names only"
    - owner: "Owner/team"
    - description: "Descriptions"
```

### Search Results

```
🔍 Search Results for "auth"

Exact Matches:
  📦 identity/auth-api — Authentication endpoints

Related:
  📦 identity/token-service — JWT token management
  🧩 shared/auth-middleware — Authentication middleware

{match_count} results found

[View details] [New search]
```

---

## Operation: Info

### Service Detail View

```yaml
service_name:
  prompt: "Which service to view?"
  source: "list or search results"
```

```
📦 Service: identity

Description: Authentication and authorization
Owner: @platform-team
Path: services/identity/
Status: 🟢 Active

Microservices (3):
┌─────────────────────────────────────────────────────────────┐
│ auth-api          │ Authentication endpoints      │ 🟢      │
│ token-service     │ JWT token management          │ 🟢      │
│ user-profile      │ User profile management       │ 🟢      │
└─────────────────────────────────────────────────────────────┘

Dependencies (outbound):
  → notifications/email-service (email verification)
  → payments/billing-service (subscription check)

Dependents (inbound):
  ← payments/billing-service (authentication)
  ← analytics/tracking (user context)

Documentation:
  📄 services/identity/README.md
  📄 services/identity/docs/auth-flow.md
  📄 services/identity/docs/api-contracts.md

Recent Activity:
  🔄 12 commits this week
  🎫 3 open issues
  📋 2 active PRs

[Edit] [View microservice] [Show dependencies]
```

---

## Operation: Edit

### Edit Service Metadata

```yaml
edit_field:
  prompt: "What to edit?"
  options:
    - description: "Service description"
    - owner: "Owner/team"
    - docs: "Documentation paths"
    - microservices: "Microservice list"
    - dependencies: "Dependencies"
```

### Edit Flow

1. Show current value
2. Accept new value
3. Validate (if applicable)
4. Update relevant files:
   - `_lens/domain-map.yaml`
   - `_lens/services/{service}/service.yaml`
   - `services/{service}/service.yaml`

### Confirm Changes

```
📝 Update Service: identity

Changes:
  owner: "@platform-team" → "@identity-team"
  description: "Authentication and authorization" → 
               "User identity, authentication, and authorization services"

Save changes? [Yes] [No] [Preview files]
```

---

## Operation: Dependencies

### View Dependency Graph

```yaml
view_mode:
  options:
    - outbound: "Services this depends on"
    - inbound: "Services that depend on this"
    - full: "Complete dependency graph"
```

### Outbound View

```
📦 identity → Dependencies

Direct:
  → notifications/email-service
    Reason: Email verification, password reset
  
  → payments/billing-service
    Reason: Subscription status check

Transitive:
  → payments/payment-gateway (via billing-service)
```

### Add/Remove Dependency

```yaml
dependency_action:
  options:
    - add: "Add new dependency"
    - remove: "Remove dependency"
    - edit: "Edit dependency reason"
```

---

## Operation: Export

### Export Options

```yaml
export_format:
  options:
    - yaml: "YAML (for backup/transfer)"
    - json: "JSON (for tooling)"
    - markdown: "Markdown (for documentation)"
    - csv: "CSV (for spreadsheets)"

export_scope:
  options:
    - all: "Full registry"
    - service: "Single service"
    - summary: "Summary only"
```

### Export to File

```bash
# Output location
_lens/exports/registry-{timestamp}.{format}
```

---

## Validation

### Registry Health Check

```
🔍 Registry Health Check

✅ Valid:
   identity — Structure matches config
   payments — Structure matches config

⚠️ Warnings:
   notifications — Missing README.md in push-service
   
🔴 Issues:
   analytics — Directory not found
   
Suggestions:
   • Run `lens sync` to update registry
   • Add README.md to notifications/push-service
   • Create or remove analytics service
```

---

## Integration

- **domain-map workflow** — Registry is subset of domain map
- **lens-sync workflow** — Keep registry in sync with actual structure
- **impact-analysis workflow** — Uses dependencies for impact detection
- **new-service/new-microservice** — Auto-registers new services
