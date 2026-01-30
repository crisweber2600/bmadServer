# LENS: Layered Enterprise Navigation System

**Git-aware architectural navigation for large interconnected projects**

LENS automatically detects and switches between four architectural lenses — Domain, Service, Microservice, and Feature — based on git branch state and working directory context. It provides continuous operational awareness, loading appropriate context for each lens and notifying you of meaningful transitions.

## 🎯 What LENS Does

- **Automatic context detection** — Knows where you are based on git branch or working directory
- **Smart context loading** — Loads relevant files, recent commits, and related issues for your current lens
- **Intelligent lens switching** — Detects when you need to zoom in or out
- **User notification** — Always tells you before switching context (configurable verbosity)
- **Session continuity** — Remembers your last context and offers to restore it

## 🔭 The Four Lenses

| Lens | Icon | Scope | When Active |
|------|------|-------|-------------|
| **Domain** | 🛰️ | All bounded contexts, cross-cutting concerns | `main`/`develop` branch or root directory |
| **Service** | 🗺️ | Logical service (contains microservices) | `service/*` branch or `services/{name}/` |
| **Microservice** | 🏘️ | Single bounded context | Inferred from feature branch or directory |
| **Feature** | 📍 | Specific capability implementation | `feature/*` branch or specific file context |

## ⚡ Quick Start

LENS works **zero-config** out of the box. Just install and start working:

1. Install LENS module
2. Work on your project normally
3. LENS automatically detects your architectural context
4. Get relevant context loaded and notifications on meaningful transitions

### Installation

```bash
bmad install lens
```

### Documentation

See the usage guide: [docs/usage-guide.md](docs/usage-guide.md)

### Example Summary Card

```
📍 Feature Lens: oauth-refresh-tokens
   Service: identity → Microservice: auth-api
   📄 3 related files | 🔄 2 recent commits | 🎫 1 open issue
   [Expand for details]
```

## 📂 Module Structure

```
lens/
├── module.yaml              # Module configuration
├── README.md                # This file
├── agents/
│   └── navigator.agent.yaml   # Single Navigator agent
├── workflows/
│   ├── lens-detect/         # Detect current lens
│   ├── lens-switch/         # Switch lens with notification
│   ├── context-load/        # Load context for current lens
│   ├── new-service/         # Create new service
│   ├── new-microservice/    # Create new microservice
│   ├── new-feature/         # Create new feature
│   ├── domain-map/          # Generate domain overview
│   ├── onboarding/          # First-time walkthrough
│   ├── lens-configure/      # Configure detection rules
│   ├── service-registry/    # Manage service mappings
│   ├── impact-analysis/     # Cross-boundary analysis
│   ├── lens-sync/           # Sync auto-discovered config
│   └── lens-restore/        # Restore session context
├── data/
│   └── lens-schemas/        # Configuration schemas
└── templates/
    └── config-templates/    # Template configs
```

## ⚙️ Configuration

LENS uses **progressive disclosure** — start with zero config, add detail as needed:

### Level 0: No Config (Auto-Discovery)

LENS infers structure from directory layout and git branches. Works immediately.

### Level 1: Minimal Config

Create `_lens/lens-config.yaml` with branch patterns only:

```yaml
branch_patterns:
  domain: ["main", "master", "develop"]
  service: ["service/{name}"]
  feature: ["feature/{service}/{microservice}/{name}", "feature/{name}"]
```

### Level 2: Full Config

Complete domain mapping for maximum intelligence:

```yaml
# _lens/domain-map.yaml
services:
  identity:
    description: "Authentication and authorization"
    microservices:
      - auth-api
      - token-service
      - user-profile
  payments:
    description: "Payment processing"
    microservices:
      - payment-gateway
      - billing-service
```

## 🔧 Variables Provided

LENS provides these variables to all other modules:

| Variable | Description | Example |
|----------|-------------|---------|
| `{current_lens}` | Active lens level | `feature` |
| `{active_domain}` | Current domain context | `ecommerce` |
| `{active_service}` | Current service | `identity` |
| `{active_microservice}` | Current microservice | `auth-api` |
| `{active_feature}` | Current feature | `oauth-refresh-tokens` |
| `{lens_summary}` | Brief context summary | "3 files, 2 commits" |

## 🔌 Integration with Other Modules

LENS integrates deeply with BMM workflows:

- **Story creation** auto-populates service context
- **Architecture docs** scoped to current lens
- **PRD discovery** aware of domain boundaries
- **Implementation** context pre-loaded based on feature lens

## 📋 Workflows

**Workflow format:** LENS workflows are single-file `workflow.md` specifications executed by the core workflow runner (no steps/ subfolders).

### Core Workflows

| Workflow | Description | Path |
|----------|-------------|------|
| `lens-detect` | Automatic lens detection from git/directory state | [`workflows/lens-detect/workflow.md`](workflows/lens-detect/workflow.md) |
| `lens-switch` | Switch lens with appropriate notification | [`workflows/lens-switch/workflow.md`](workflows/lens-switch/workflow.md) |
| `context-load` | Load relevant context for current lens | [`workflows/context-load/workflow.md`](workflows/context-load/workflow.md) |

### Feature Workflows

| Workflow | Description | Path |
|----------|-------------|------|
| `new-service` | Create new bounded context/service | [`workflows/new-service/workflow.md`](workflows/new-service/workflow.md) |
| `new-microservice` | Scaffold microservice within service | [`workflows/new-microservice/workflow.md`](workflows/new-microservice/workflow.md) |
| `new-feature` | Create feature branch and load context | [`workflows/new-feature/workflow.md`](workflows/new-feature/workflow.md) |
| `domain-map` | Generate/update domain overview | [`workflows/domain-map/workflow.md`](workflows/domain-map/workflow.md) |
| `onboarding` | First-time domain walkthrough | [`workflows/onboarding/workflow.md`](workflows/onboarding/workflow.md) |

### Utility Workflows

| Workflow | Description | Path |
|----------|-------------|------|
| `lens-configure` | Configure detection rules | [`workflows/lens-configure/workflow.md`](workflows/lens-configure/workflow.md) |
| `service-registry` | Manage service → microservice mappings | [`workflows/service-registry/workflow.md`](workflows/service-registry/workflow.md) |
| `impact-analysis` | Analyze cross-boundary impacts | [`workflows/impact-analysis/workflow.md`](workflows/impact-analysis/workflow.md) |
| `lens-sync` | Sync auto-discovered structure with explicit config | [`workflows/lens-sync/workflow.md`](workflows/lens-sync/workflow.md) |
| `lens-restore` | Restore previous session's lens context | [`workflows/lens-restore/workflow.md`](workflows/lens-restore/workflow.md) |

## 🔌 BMM Integration

LENS integrates with BMM story creation workflows to provide architectural context:

- **Story creation** (`create-story`) auto-populates service/microservice context from current lens
- LENS variables (`{lens_active_service}`, `{lens_active_microservice}`, `{lens_active_feature}`) are available in story workflows
- Domain-map context provides cross-service dependency awareness during story scoping

### Integration Variables

| Variable | Source | Used By |
|----------|--------|---------|
| `{lens_current_lens}` | LENS config | create-story, dev-story |
| `{lens_active_service}` | LENS config | create-story (service scoping) |
| `{lens_active_microservice}` | LENS config | create-story (microservice scoping) |
| `{lens_active_feature}` | LENS config | create-story (feature context) |

## ⚠️ Known Limitations

| Limitation | Scope |
|------------|-------|
| **Multi-repo** | LENS assumes single-repo or monorepo |
| **IDE integration** | v1 is CLI/BMAD-native |
| **Real-time hooks** | Detects on session start, not every git op |

## 🎨 Personality

LENS uses a navigation metaphor:

- 🛰️ Domain = "Satellite View"
- 🗺️ Service = "City Map"
- 🏘️ Microservice = "Street Level"
- 📍 Feature = "Indoor Navigation"

---

_LENS: Layered Enterprise Navigation System — See the architecture clearly at every level._
