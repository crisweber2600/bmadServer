---
name: lens-restore
description: Restore previous session context
---

# Lens Restore Workflow

**Goal:** Restore session state from `.lens-state` to continue where you left off.

## Trigger Conditions

This workflow is triggered:
1. Automatically on Navigator activation if `auto_restore: true`
2. Manually via `lens restore` command
3. When switching to a previously active branch

## Execution Steps

### 1. Check for State File

Look for `_lens/.lens-state`:

```yaml
# .lens-state structure
last_lens: feature
last_service: identity
last_microservice: auth-api
last_feature: oauth-refresh-tokens

last_context_files:
  - src/services/identity/auth-api/oauth.ts
  - src/services/identity/auth-api/oauth.test.ts
  - src/services/identity/auth-api/types.ts

timestamp: 2026-01-29T19:30:00Z
previous_lens: microservice

session_stats:
  lens_switches: 5
  files_loaded: 12
  duration_minutes: 45
```

### 2. Validate State

Check if saved state is still valid:

```yaml
validation_checks:
  - branch_exists: "Does the feature branch still exist?"
  - files_exist: "Do the context files still exist?"
  - service_exists: "Is the service still in domain-map?"
  - time_threshold: "Is the session recent enough?"
```

### 3. Present Restore Option

**If auto-restore enabled:**
```
🔄 Previous Session Found

Last working on:
   📍 Feature: oauth-refresh-tokens
   Service: identity → Microservice: auth-api
   📄 3 files | 🕐 2 hours ago

Resume this session? [Y]es / [N]o / [D]etails
```

**If manual restore:**
```
🔄 Available Sessions

1. oauth-refresh-tokens (2 hours ago)
   📍 Feature Lens | identity/auth-api
   📄 3 files

2. payment-webhooks (yesterday)
   📍 Feature Lens | payments/payment-gateway
   📄 5 files

3. Domain overview (3 days ago)
   🛰️ Domain Lens
   📄 Full domain context

Select session to restore: [1-3] or [Cancel]
```

---

## Restore Actions

### 4. Restore Lens State

Set current lens variables:

```yaml
current_lens: {last_lens}
active_service: {last_service}
active_microservice: {last_microservice}
active_feature: {last_feature}
```

### 5. Load Context Files

For each file in `last_context_files`:
1. Check if file exists
2. If exists, add to context
3. If not, note as "changed since last session"

```
📂 Loading context files...

✅ Loaded:
   src/services/identity/auth-api/oauth.ts
   src/services/identity/auth-api/oauth.test.ts

⚠️ Changed/Missing:
   src/services/identity/auth-api/types.ts (modified)
   
💡 File was modified since last session. Refreshing...
```

### 6. Switch to Branch (Optional)

If feature was on a different branch:

```
🌿 Session was on branch: feature/identity/auth-api/oauth-refresh-tokens
   Current branch: main

Switch to session branch? [Y/n]
```

If yes:
```bash
git checkout feature/identity/auth-api/oauth-refresh-tokens
```

### 7. Display Summary Card

Show restored context:

```
✅ Session Restored

📍 Feature Lens: oauth-refresh-tokens
   Service: identity → Microservice: auth-api
   📄 3 files loaded | 🔄 2 new commits since last session
   
Changes since last session:
   🔄 auth-api: 2 commits by @teammate
   📝 types.ts modified
   
Ready to continue!
```

---

## Advanced Features

### Multiple Sessions

Store multiple session states for different contexts:

```yaml
# _lens/.lens-state
sessions:
  - id: oauth-refresh-tokens
    lens: feature
    service: identity
    microservice: auth-api
    feature: oauth-refresh-tokens
    timestamp: 2026-01-29T19:30:00Z
    files: [...]
    
  - id: payment-integration
    lens: feature
    service: payments
    microservice: payment-gateway
    feature: stripe-webhooks
    timestamp: 2026-01-28T14:00:00Z
    files: [...]
```

### Per-Branch State

Automatically save state per branch:

```yaml
# _lens/.lens-state
branch_states:
  "feature/identity/auth-api/oauth-refresh-tokens":
    lens: feature
    files: [...]
    timestamp: ...
    
  "main":
    lens: domain
    files: [...]
    timestamp: ...
```

On branch switch, offer to restore branch-specific state.

### Session Bookmarks

Save named sessions for quick access:

```
💾 Save current session as bookmark?

Name: [oauth-work]
Description: [Working on OAuth 2.0 refresh token rotation]

Saved! Restore anytime with: lens restore oauth-work
```

---

## State Persistence

### Auto-Save Triggers

State is automatically saved:
- On lens switch
- On file context load
- Before Navigator exit
- Periodically (configurable interval)

### State File Location

```yaml
locations:
  primary: "_lens/.lens-state"
  backup: "_lens/.lens-state.bak"
  
# Also support per-user state (not committed):
user_state: "_lens/.lens-state.{username}"
```

### Cleanup Policy

```yaml
cleanup:
  max_sessions: 10          # Keep last 10 sessions
  max_age_days: 30          # Remove sessions older than 30 days
  auto_cleanup: true        # Run cleanup on restore
```

---

## Error Handling

| Situation | Action |
|-----------|--------|
| State file missing | Offer to detect context fresh |
| State file corrupted | Attempt recovery from backup |
| Branch deleted | Warn, offer to restore files only |
| Files moved/deleted | Show diff, load what exists |
| Service removed | Warn, fallback to domain lens |

---

## Output Options

### Silent Restore

For automation/scripts:
```bash
lens restore --silent
# Restores without prompts, uses defaults
```

### JSON Output

For tooling integration:
```bash
lens restore --format=json
# Returns restore status as JSON
```

---

## Integration

- **Navigator activation** — Auto-offers restore
- **Git hooks** — Save state on branch switch
- **IDE integration** — Sync with IDE workspace state
