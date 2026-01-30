# LENS Usage Guide

## Quick Start

### 1. Launch the Navigator

Load the Navigator agent in your BMAD session:

```
*navigator
```

The Navigator will automatically:
- Detect your current git branch
- Determine the appropriate lens level
- Load relevant architectural context
- Offer to restore your previous session (if available)

### 2. Understand the Lenses

LENS provides four architectural zoom levels:

| Lens | Icon | When Active | What You See |
|------|------|-------------|-------------|
| **Domain** | 🛰️ | `main`, `develop`, `release/*` branches | All services, relationships, shared patterns |
| **Service** | 🗺️ | `service/{name}` branches | Service overview, microservices, dependencies |
| **Microservice** | 🏘️ | Working in a specific microservice directory | Responsibilities, contracts, internal structure |
| **Feature** | 📍 | `feature/*` branches | Implementation context, related files, recent commits |

### 3. Use the Menu

Once Navigator is active, use any of these commands:

| # | Command | Description |
|---|---------|-------------|
| 1 | `status` | Show current lens and loaded context |
| 2 | `domain` | Switch to Domain Lens (satellite view) |
| 3 | `service` | Switch to Service Lens (city map) |
| 4 | `micro` | Switch to Microservice Lens (street level) |
| 5 | `feature` | Switch to Feature Lens (indoor navigation) |
| 6 | `new-service` | Create a new bounded context/service |
| 7 | `new-micro` | Scaffold a new microservice |
| 8 | `new-feature` | Create a feature branch with context |
| 9 | `map` | View or generate domain overview |
| 10 | `impact` | Analyze cross-boundary impacts |
| 11 | `config` | Configure LENS detection rules |
| 12 | `sync` | Sync auto-discovered with explicit config |

You can type the command name, the number, or a fuzzy match (e.g., "feat" matches "feature").

### Mental Model: Commands → Goals

Use this cheat sheet to pick the fastest path:

- **Understand the whole system** → `domain` (Domain Lens)
- **Focus on one service** → `service`
- **Focus on one microservice** → `micro`
- **Focus on active work** → `feature`
- **Start new work** → `new-service`, `new-micro`, `new-feature`
- **Keep config accurate** → `map` (overview) and `sync` (drift fixes)
- **Assess blast radius** → `impact`

---

## Common Workflows

### Starting a New Feature

```
*navigator
> new-feature
```

This will:
1. Ask which service/microservice the feature belongs to
2. Create a feature branch following your naming convention
3. Load architectural context for the target area
4. Create a `feature-context.yaml` tracking file
5. Display a summary card with relevant files and recent activity

### Viewing the Domain Map

```
*navigator
> map
```

Generates an overview of all services, their microservices, and relationships. Useful for understanding the full architecture before making cross-cutting changes.

### Analyzing Impact

```
*navigator
> impact
```

Before making changes that span service boundaries, run impact analysis to identify:
- Which services are affected
- What dependencies exist
- Which teams should be notified
- Potential breaking changes

### Creating a New Service

```
*navigator
> new-service
```

Scaffolds a new bounded context with:
- Directory structure
- `service.yaml` definition
- Domain map registration
- README template

---

## Configuration

### Zero-Config Mode

LENS works out of the box with no configuration. It uses sensible defaults:
- Git branch patterns for lens detection
- Auto-discovery of services from directory structure
- Smart notifications (meaningful transitions only)

### Explicit Configuration

For more control, create `_lens/lens-config.yaml`:

```yaml
# Detection rules
detection:
  mode: auto  # git-first | directory-first | auto
  branch_patterns:
    domain: ["main", "master", "develop", "release/*"]
    service: ["service/{name}", "svc/{name}"]
    feature: ["feature/{service}/{name}", "feat/{name}"]

# Notification settings
notifications:
  level: smart  # silent | smart | verbose

# Session persistence
session:
  auto_restore: true
```

### Domain Map

Create `_lens/domain-map.yaml` to explicitly define your architecture:

```yaml
domain:
  name: "My Platform"
  description: "E-commerce platform"

services:
  identity:
    description: "Authentication and authorization"
    microservices:
      - name: auth-api
        description: "OAuth2 endpoints"
      - name: user-store
        description: "User data management"

  catalog:
    description: "Product catalog"
    microservices:
      - name: product-api
        description: "Product CRUD"
      - name: search
        description: "Full-text search"

shared:
  - name: logging
    description: "Centralized logging"
```

---

## How Detection Works

### Priority Order

1. **Explicit config** (`_lens/lens-config.yaml`) — always wins
2. **Git branch patterns** — primary detection method
3. **Working directory** — fallback for trunk-based development
4. **Auto-discovery** — infer from directory structure

When LENS switches lenses, it will explain **why** (e.g., branch pattern match or directory context) so you can verify the decision.

### Branch Pattern Matching

| Branch | Detected Lens | Context |
|--------|--------------|---------|
| `main` | Domain | Full project overview |
| `develop` | Domain | Full project overview |
| `service/identity` | Service | Identity service context |
| `feature/identity/auth-api/oauth-refresh` | Feature | Feature within auth-api |
| `feature/add-logging` | Feature | Feature (service inferred) |

### Trunk-Based Development

If you use trunk-based development (no feature branches), LENS falls back to directory detection:
- Working in `services/identity/auth-api/` → Microservice Lens: auth-api
- Working in `services/identity/` → Service Lens: identity
- Working in project root → Domain Lens

---

## Session Continuity

LENS persists session state to `_lens/.lens-state`:

```yaml
last_lens: feature
last_service: identity
last_microservice: auth-api
last_feature: oauth-refresh-tokens
last_files:
  - src/services/identity/auth-api/oauth.ts
  - src/services/identity/auth-api/oauth.test.ts
timestamp: 2026-01-29T10:30:00Z
```

When you start a new session, Navigator offers to restore:

```
🧭 Welcome back! Resume working on `oauth-refresh-tokens` in `auth-api`? [Y/n]
```

---

## BMM Integration

When LENS is active and you run BMM story creation workflows, LENS automatically provides architectural context:

- **Service scoping** — Stories are pre-scoped to the active service
- **Microservice targeting** — Implementation context includes the correct microservice
- **Feature linking** — Feature branch context is linked to the story
- **Dependency awareness** — Domain map provides cross-service dependency context

This means story files created via `create-story` will include:
- Which service the story belongs to
- Which microservice to target
- Cross-service dependencies to be aware of
- Feature branch context for implementation

---

## Summary Card Format

LENS provides compact summary cards for context:

```
📍 Feature Lens: oauth-refresh-tokens
   Service: identity → Microservice: auth-api
   📄 3 related files | 🔄 2 recent commits | 🎫 1 open issue
   [Expand for details]
```

Type "expand" or ask for more details to see the full context.

---

## Testing & Validation Checklist

Use this checklist to validate LENS behavior in real-world scenarios. These are manual tests intended to confirm detection, switching, and session persistence.

### 1. Monorepo Detection (Multi-Service)

- [ ] Ensure repo has multiple services (e.g., services/identity, services/payments)
- [ ] Start Navigator on main/develop branch
- [ ] Expected: **Domain Lens** detected with service count > 1
- [ ] Switch to service branch `service/identity`
- [ ] Expected: **Service Lens** with identity context
- [ ] Switch to feature branch `feature/identity/auth-api/oauth-refresh`
- [ ] Expected: **Feature Lens** with service + microservice inferred

### 2. Trunk-Based Directory Fallback

- [ ] Stay on main branch (no feature/service branches)
- [ ] Change working directory to `services/identity/`
- [ ] Expected: **Service Lens** detected via directory
- [ ] Change directory to `services/identity/auth-api/`
- [ ] Expected: **Microservice Lens** detected
- [ ] Return to repo root
- [ ] Expected: **Domain Lens** detected

### 3. Session Restore (.lens-state)

- [ ] Enter Feature Lens and open several feature files
- [ ] Exit Navigator (or end session)
- [ ] Start Navigator again
- [ ] Expected: Restore prompt appears
- [ ] Accept restore
- [ ] Expected: Previous lens + files loaded, summary card displayed

### 4. Drift Detection & Sync

- [ ] Add a new microservice folder without updating domain-map
- [ ] Run `sync`
- [ ] Expected: Drift detected, new microservice listed
- [ ] Apply sync and confirm domain-map updated

---

## Troubleshooting

### LENS Doesn't Activate

LENS activates conditionally. It requires at least one of:
- A `services/` or `apps/` directory
- A `_lens/` configuration folder
- Branch patterns matching `service/*` or `feature/*`

If none of these exist, LENS stays dormant.

### Wrong Lens Detected

Run `config` to customize detection rules:
```
*navigator
> config
```

Or manually create `_lens/lens-config.yaml` with explicit branch patterns.

### Config Drift

If auto-discovered services don't match your domain map:
```
*navigator
> sync
```

This compares auto-discovery results with explicit config and proposes updates.
