---
name: new-feature
description: Create feature branch with context initialization
---

# New Feature Workflow

**Goal:** Create a feature branch with proper naming, context files, and automatic lens detection setup.

## Prerequisites

- Git repository initialized
- Working tree clean (or user confirms to proceed)
- LENS activated

## Execution Steps

### 1. Determine Context

Detect or prompt for the feature's architectural context:

```yaml
feature_service:
  source: "current Service Lens or prompt"
  prompt: "Which service is this feature for?"
  auto_detect: true

feature_microservice:
  source: "current Microservice Lens or prompt"
  prompt: "Which microservice will implement this feature?"
  auto_detect: true

feature_name:
  prompt: "Feature name (kebab-case, descriptive):"
  validation: "^[a-z][a-z0-9-]*$"
  example: "oauth-refresh-tokens, user-search, payment-webhooks"

feature_description:
  prompt: "Brief description of what this feature does:"
  example: "Implement OAuth 2.0 refresh token rotation"
```

### 2. Generate Branch Name

Based on config branch patterns:

**Full Context Pattern (default):**
```
feature/{service}/{microservice}/{feature_name}
```
Example: `feature/identity/auth-api/oauth-refresh-tokens`

**Simplified Pattern (if configured):**
```
feature/{microservice}/{feature_name}
```
Example: `feature/auth-api/oauth-refresh-tokens`

**Minimal Pattern:**
```
feature/{feature_name}
```
Example: `feature/oauth-refresh-tokens`

### 3. Create Git Branch

```bash
git checkout -b {branch_name}
```

### 4. Create Feature Context File (Optional)

If enabled in config, create `_lens/features/{feature_name}.yaml`:

```yaml
# Feature Context
name: {feature_name}
branch: {branch_name}
description: "{feature_description}"
created: {timestamp}
author: {git_user_name}

context:
  service: {feature_service}
  microservice: {feature_microservice}

# Files likely to be involved (auto-populated as you work)
related_files: []

# Tasks and progress
tasks:
  - description: "Initial implementation"
    status: not-started
  - description: "Tests"
    status: not-started
  - description: "Documentation"
    status: not-started

# Related links
related:
  issues: []    # GitHub/Jira issue links
  prs: []       # Related PRs
  docs: []      # Design docs
```

### 5. Link to Issue (Optional)

If issue tracking is configured:

```yaml
issue_link:
  prompt: "Link to issue (GitHub/Jira URL or ID, or 'skip'):"
  example: "#123, PROJ-456, https://github.com/org/repo/issues/123"
```

If provided, update feature context file with issue link.

### 6. Initialize Working Files

Optionally create starter files based on microservice type:

**For API features:**
- `features/{feature_name}/` directory in microservice
- Placeholder endpoint file
- Placeholder test file

**For worker features:**
- Handler/processor stub
- Job definition

### 7. Update .lens-state

```yaml
last_lens: feature
last_service: {feature_service}
last_microservice: {feature_microservice}
last_feature: {feature_name}
timestamp: {current_timestamp}
```

### 8. Switch to Feature Lens

Automatically switch to Feature Lens with new context:

```
📍 Feature Lens: {feature_name}
   Service: {feature_service} → Microservice: {feature_microservice}
   🌿 Branch: {branch_name}
   📄 0 files | 🔄 0 commits | 🎫 {issue_count} linked issues
   
   Ready to start!
```

---

## Output Summary

```
✅ Created new feature: {feature_name}

🌿 Branch: {branch_name}
📂 Feature context: _lens/features/{feature_name}.yaml (if created)
🔗 Linked to: {issue_link} (if provided)

📍 Now in Feature Lens: {feature_name}
   Service: {feature_service}
   Microservice: {feature_microservice}

💡 Tip: LENS will track related files as you work.
```

---

## Quick Start Variations

### From Domain Lens
1. Prompt for service
2. Prompt for microservice
3. Prompt for feature name
4. Create branch and switch

### From Service Lens
1. Use current service
2. Prompt for microservice
3. Prompt for feature name
4. Create branch and switch

### From Microservice Lens
1. Use current service and microservice
2. Prompt for feature name only
3. Create branch and switch

### With Issue Link
```
new-feature #123
```
Auto-extract context from issue metadata if available.

---

## Validation Checks

Before creating:
- [ ] Branch name doesn't already exist
- [ ] Feature name follows naming convention
- [ ] Service/microservice exist (if specified)
- [ ] Git working tree clean (or user confirms)

---

## Error Handling

| Error | Resolution |
|-------|------------|
| Branch exists | Offer to checkout existing or choose new name |
| Dirty working tree | Warn, offer to stash or commit first |
| Service/microservice not found | Offer to create or choose different |
| Invalid feature name | Show naming requirements |

---

## Integration with BMM

When creating a feature from a story:
- Pre-populate feature_name from story ID
- Link to story document
- Include acceptance criteria in feature context
