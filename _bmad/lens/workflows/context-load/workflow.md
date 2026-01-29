---
name: context-load
description: Load appropriate context files for the current lens
---

# Context Load Workflow

**Goal:** Load relevant files, recent commits, and related issues based on the current lens level.

## Context Loading Rules

### 🛰️ Domain Lens Context

**Load these files:**
- `README.md` (project root)
- `ARCHITECTURE.md` or `docs/architecture.md`
- `_lens/domain-map.yaml` (if exists)
- Cross-cutting documentation (`docs/shared/`, `docs/patterns/`)

**Show:**
- All services with brief descriptions
- Service relationships and dependencies
- Shared patterns and cross-cutting concerns
- High-level metrics (service count, total microservices)

**Summary Card:**
```
🛰️ Domain Lens: {project_name}
   📦 {service_count} services | 🔧 {microservice_count} microservices
   📄 Architecture docs loaded
```

---

### 🗺️ Service Lens Context

**Load these files:**
- `services/{service}/README.md`
- `services/{service}/service.yaml` or similar config
- `_lens/services/{service}/dependencies.yaml`
- Service-level documentation

**Show:**
- Service description and purpose
- Microservices within this service
- Dependencies on other services
- Recent commits to this service
- Open issues tagged to this service

**Summary Card:**
```
🗺️ Service Lens: {service_name}
   🏘️ {microservice_count} microservices | 🔗 {dependency_count} dependencies
   📄 {doc_count} docs | 🔄 {commit_count} recent commits
```

---

### 🏘️ Microservice Lens Context

**Load these files:**
- `services/{service}/{microservice}/README.md`
- API contracts/OpenAPI specs
- Boundary documentation
- Test coverage info

**Show:**
- Microservice responsibilities
- API surface (endpoints, contracts)
- Internal structure overview
- Dependencies (what it calls, what calls it)
- Recent commits
- Open issues

**Summary Card:**
```
🏘️ Microservice Lens: {microservice_name}
   Service: {service_name}
   📡 {endpoint_count} endpoints | 🧪 {test_coverage}% coverage
   📄 {file_count} files | 🎫 {issue_count} open issues
```

---

### 📍 Feature Lens Context

**Load these files:**
- Source files related to the feature
- Related test files
- Recent commits touching these files
- Open issues/PRs related to this work

**Show:**
- Files you're working on
- Related tests
- Recent changes by you and others
- Blockers or related issues
- Impact on other areas (if detected)

**Summary Card:**
```
📍 Feature Lens: {feature_name}
   Service: {service_name} → Microservice: {microservice_name}
   📄 {file_count} related files | 🔄 {commit_count} recent commits | 🎫 {issue_count} open issues
   [Expand for details]
```

---

## Progressive Disclosure

**Default:** Show summary card only

**On "[Expand]" or "details":**
- Show file list
- Show commit summaries
- Show issue titles

**On specific request:**
- Load full file contents
- Show full commit diffs
- Show issue details

---

## Noise Reduction

**Active filtering per lens:**

| Current Lens | Hide |
|--------------|------|
| Domain | Implementation details, test files |
| Service | Other services' internals |
| Microservice | Other microservices' code |
| Feature | Unrelated domain documentation |

---

## Output

Context is loaded silently. Only display summary card unless:
- This is a lens transition (show transition message + new summary)
- User explicitly requests details
- Drift or issues detected (warn user)
