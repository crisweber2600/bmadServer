---
name: lens-sync
description: Synchronize auto-discovered structure with explicit configuration
---

# Lens Sync Workflow

**Goal:** Keep LENS configuration in sync with actual project structure by detecting drift and offering resolution options.

## Use Cases

1. **Discovery → Config** — Add newly discovered services to config
2. **Config → Structure** — Identify configured services that no longer exist
3. **Bidirectional sync** — Full reconciliation
4. **Periodic maintenance** — Regular sync checks

## Execution Steps

### 1. Analyze Current State

**Auto-Discovery Scan:**
```bash
# Scan for service directories
find . -type d -name "services" -o -name "apps" | ...

# Identify microservices within services
# Look for indicators: package.json, *.csproj, go.mod, etc.
```

**Load Configuration:**
```yaml
sources:
  - _lens/domain-map.yaml
  - _lens/services/*/service.yaml
  - services/*/service.yaml
```

### 2. Compare Structures

```
📊 Sync Analysis

Scanning project structure...
Loading configuration...

Comparison complete.
```

### 3. Report Drift

```
🔄 Sync Report

Configuration vs Actual Structure:

✅ Matching ({count}):
   identity/auth-api ✓
   identity/token-service ✓
   payments/payment-gateway ✓

🆕 Discovered but not in config ({count}):
   identity/admin-api (new directory found)
   notifications/sms-service (new directory found)
   
⚠️ In config but not found ({count}):
   analytics/tracking (directory missing)
   
📝 Metadata out of date ({count}):
   identity/user-profile
     Config: "User profile management"
     README: "User profile and preferences service"

Would you like to sync? [Yes - all] [Select items] [No]
```

---

## Sync Actions

### Add Discovered to Config

For each newly discovered item:

```yaml
action: add_to_config
item: identity/admin-api

generated_entry:
  name: admin-api
  description: "_Discovered - please add description_"
  path: services/identity/admin-api
```

Add to:
- `_lens/domain-map.yaml` under appropriate service
- Create `_lens/services/identity/service.yaml` if needed

### Handle Missing Items

For each item in config but not found:

```yaml
options:
  - comment_out: "Comment out in config (preserves for later)"
  - remove: "Remove from config entirely"
  - ignore: "Keep in config (maybe on different branch)"
  - create: "Create the missing directory structure"
```

### Update Metadata

For items with outdated metadata:

```yaml
options:
  - use_readme: "Update config with README content"
  - use_config: "Keep config value (update README)"
  - manual: "Enter new value manually"
```

---

## Sync Modes

### 1. Interactive Mode (Default)

Step through each discrepancy and choose action.

### 2. Auto Mode

```yaml
auto_sync:
  add_discovered: true    # Auto-add new discoveries
  remove_missing: false   # Don't auto-remove (safer)
  update_metadata: readme # Prefer README content
```

### 3. Report Only Mode

```bash
lens sync --dry-run
```

Only show what would change, don't modify anything.

---

## Detailed Operations

### Add New Service

```
🆕 New service discovered: notifications

Directory: services/notifications/
Microservices found:
  - email-service
  - push-service

Add to domain-map.yaml? [Yes] [No] [Customize]
```

If yes:
```yaml
# Added to _lens/domain-map.yaml
services:
  notifications:
    description: "_Please add description_"
    owner: "_TBD_"
    microservices:
      - name: email-service
        description: "_Please add description_"
      - name: push-service
        description: "_Please add description_"
```

### Add New Microservice

```
🆕 New microservice discovered: identity/admin-api

Add to identity service config? [Yes] [No]
```

### Remove Stale Entry

```
⚠️ Stale entry: analytics/tracking

Not found at: services/analytics/tracking/

Options:
  1. Comment out (recommended)
  2. Remove entirely
  3. Ignore (keep in config)
  4. Create directory structure

Choice: [1]
```

### Update Metadata

```
📝 Metadata mismatch: identity/user-profile

Config description:
  "User profile management"

README description:
  "User profile and preferences service with avatar handling"

Use which? [README] [Config] [Enter new]
```

---

## Post-Sync Summary

```
✅ Sync Complete

Changes made:
  ➕ Added: 2 services, 3 microservices
  📝 Updated: 1 description
  💬 Commented out: 1 stale entry

Files modified:
  _lens/domain-map.yaml
  _lens/services/identity/service.yaml
  _lens/services/notifications/service.yaml (created)

Next steps:
  • Review and add descriptions to new entries
  • Verify commented entries are intentionally missing
  • Run `lens sync` periodically to stay current

💡 Tip: Run `lens sync --dry-run` to preview changes without applying.
```

---

## Automation

### Git Hook Integration

```yaml
# _lens/hooks.yaml
post_checkout:
  - sync_check:
      mode: warn  # warn | auto | block
      
post_merge:
  - sync_check:
      mode: auto
```

### CI Integration

```yaml
# In CI pipeline
- name: Check LENS sync
  run: |
    lens sync --dry-run --format=json > sync-report.json
    if [ $(jq '.drift_count' sync-report.json) -gt 0 ]; then
      echo "::warning::LENS configuration drift detected"
    fi
```

---

## Edge Cases

| Situation | Handling |
|-----------|----------|
| Empty service directory | Warn, don't add to config |
| Nested services | Detect and handle appropriately |
| Monorepo root confusion | Use config hints to clarify |
| Branch-specific services | Support per-branch configs |
