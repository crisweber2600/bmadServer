---
name: lens-detect
description: Detect current architectural lens from git state and working directory
---

# Lens Detection Workflow

**Goal:** Automatically detect the current architectural lens based on git branch, working directory, and project configuration.

## Detection Priority Order

1. **Explicit config** — `_lens/lens-config.yaml` overrides always win
2. **Git branch** — Branch name patterns
3. **Working directory** — Fallback for trunk-based development
4. **Auto-discovery** — Infer from directory structure

## Execution

### 1. Check for Explicit Configuration

Load `{project-root}/_lens/lens-config.yaml` if exists.

If `current_lens_override` is set, use that lens immediately.

### 2. Detect Git Branch

```bash
git branch --show-current
```

Match against patterns:

| Pattern | Lens |
|---------|------|
| `main`, `master`, `develop` | Domain |
| `release/*`, `hotfix/*` | Domain |
| `service/{name}` | Service (extract name) |
| `feature/{service}/{microservice}/{name}` | Feature (extract all) |
| `feature/{microservice}/{name}` | Feature (infer service) |
| `feature/{name}` | Feature (infer microservice from directory) |

### 3. Working Directory Fallback

If branch doesn't match patterns (trunk-based development):

- Check current working directory path
- If in `services/{name}/` → Service Lens
- If in `services/{service}/{microservice}/` → Microservice Lens
- If editing specific files → Feature Lens
- Otherwise → Domain Lens

### 4. Auto-Discovery

If no config and unclear context:

- Scan for `services/`, `apps/`, `src/` directories
- Build implicit domain map
- Infer current lens from file locations

### 5. Set Lens Variables

Set these variables for other workflows:

```yaml
current_lens: domain | service | microservice | feature
active_domain: {domain_name}
active_service: {service_name}
active_microservice: {microservice_name}
active_feature: {feature_name}
```

### 6. Check for Drift

If auto-discovered structure differs from explicit config:

- Note discrepancies
- Suggest `lens sync` if significant drift

## Output

Return detected lens with confidence level:

```yaml
lens: feature
confidence: high  # high | medium | low
source: git_branch  # git_branch | working_directory | auto_discovery | explicit_config
context:
  service: identity
  microservice: auth-api
  feature: oauth-refresh-tokens
drift_detected: false
```
