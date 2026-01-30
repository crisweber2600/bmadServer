---
name: impact-analysis
description: Analyze cross-boundary impacts of changes
---

# Impact Analysis Workflow

**Goal:** Detect and report how changes in one area affect other services and microservices.

## Use Cases

1. **Pre-commit analysis** — Check impact before committing
2. **PR review** — Analyze PR changes for cross-boundary effects
3. **Refactoring planning** — Understand blast radius of proposed changes
4. **API change assessment** — What breaks if I change this contract?

## Execution Steps

### 1. Determine Analysis Scope

```yaml
analysis_scope:
  prompt: "What would you like to analyze?"
  options:
    - current: "Current uncommitted changes"
    - branch: "Changes in current branch vs {base_branch}"
    - files: "Specific files or directories"
    - api: "API contract changes"
    - dependency: "Dependency change impact"
```

---

## Scope: Current Changes

### Detect Changed Files

```bash
git status --porcelain
git diff --name-only
```

### Categorize Changes

```
📊 Analyzing {file_count} changed files...

Changes by location:
  📦 identity/auth-api: 3 files
  📦 identity/token-service: 1 file
  🧩 shared/auth-middleware: 2 files
```

### Analyze Impact

For each changed file:
1. Identify which service/microservice it belongs to
2. Check if it's a contract file (API, interface, types)
3. Find dependents from service registry
4. Check for cross-boundary imports

---

## Scope: Branch Analysis

### Compare Branches

```bash
git diff {base_branch}...HEAD --name-only
```

### Generate Diff Summary

```
📊 Branch Analysis: feature/oauth-refresh → main

Changed: {file_count} files
Added: {added_count} | Modified: {modified_count} | Deleted: {deleted_count}

By service:
  📦 identity: 8 files (3 contract changes)
  📦 shared: 2 files (1 contract change)
```

---

## Impact Detection Rules

### Contract Files

Files considered "contracts" (changes affect dependents):

| Pattern | Type |
|---------|------|
| `*.proto` | gRPC contract |
| `**/openapi.yaml`, `**/swagger.*` | REST API contract |
| `**/types.ts`, `**/interfaces.ts` | TypeScript types |
| `**/*Contract.cs`, `**/*Interface.cs` | .NET contracts |
| `**/events/*.yaml` | Event schemas |
| `**/package.json` (exports) | Package interface |

### Cross-Boundary Detection

Check for:
1. **Direct imports** from other services
2. **Shared type changes** used across services
3. **API endpoint changes** with known consumers
4. **Event schema changes** with subscribers
5. **Database schema changes** affecting multiple services

---

## Impact Report

### Summary View

```
⚠️ Impact Analysis Report

Risk Level: 🟡 MEDIUM

Cross-Boundary Impacts Detected: 3

📦 identity/auth-api
   Changed: src/auth/oauth.ts (contract file)
   
   Affects:
   ├── 📦 payments/billing-service
   │   └── Uses: OAuthToken type
   │   └── Risk: Type signature changed
   │
   └── 📦 analytics/tracking
       └── Uses: AuthEvent schema
       └── Risk: New required field

🧩 shared/auth-middleware
   Changed: src/middleware.ts
   
   Affects:
   ├── 📦 identity/* (all microservices)
   ├── 📦 payments/* (all microservices)
   └── 📦 notifications/email-service
```

### Detailed View

For each impact:

```
┌─────────────────────────────────────────────────────────────┐
│ Impact #1: OAuthToken type change                           │
├─────────────────────────────────────────────────────────────┤
│ Source: identity/auth-api/src/types.ts                     │
│ Change: Added required field 'scope: string[]'              │
│                                                             │
│ Affected consumers:                                         │
│   payments/billing-service/src/auth.ts:45                  │
│   analytics/tracking/src/events.ts:23                      │
│                                                             │
│ Risk: 🟡 MEDIUM — Compilation will fail in dependents       │
│                                                             │
│ Recommendation:                                             │
│   1. Make 'scope' optional, or                              │
│   2. Update all consumers before merging, or                │
│   3. Use versioned API approach                             │
└─────────────────────────────────────────────────────────────┘
```

---

## Risk Levels

| Level | Criteria |
|-------|----------|
| 🟢 LOW | No cross-boundary changes, internal only |
| 🟡 MEDIUM | Contract changes with known consumers |
| 🔴 HIGH | Breaking changes to widely-used contracts |
| ⛔ CRITICAL | Database schema or event changes with many subscribers |

---

## Recommendations Engine

Based on detected impacts:

### For API Changes
```
💡 Recommendations:

1. Version the API endpoint
   /api/v1/oauth → /api/v2/oauth

2. Or use feature flags
   Enable new behavior gradually

3. Coordinate with teams:
   @payments-team — billing-service
   @analytics-team — tracking
```

### For Type Changes
```
💡 Recommendations:

1. Make new fields optional with defaults
   scope?: string[] = []

2. Or create migration path
   OAuthToken → OAuthTokenV2

3. Update all consumers in same PR
```

### For Database Changes
```
💡 Recommendations:

1. Use migration scripts
2. Consider backward-compatible changes first
3. Plan deployment order carefully
```

---

## Integration

### Pre-Commit Hook

Can be run as pre-commit hook:
```yaml
# .lens/hooks.yaml
pre_commit:
  - impact_analysis:
      block_on: HIGH
      warn_on: MEDIUM
```

### PR Integration

Automatically comment on PRs with impact summary.

### CI/CD Integration

Export report for CI/CD pipelines:
```bash
lens impact --format=json --output=impact-report.json
```

---

## Output Options

```yaml
output_format:
  options:
    - interactive: "Interactive terminal view"
    - summary: "Summary only"
    - detailed: "Full report"
    - json: "JSON for tooling"
    - markdown: "Markdown for PR comments"
```
