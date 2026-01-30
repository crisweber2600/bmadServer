# LENS Configuration Schemas

## lens-config.yaml

Main configuration file for LENS. Location: `_lens/lens-config.yaml`

```yaml
# LENS Configuration
# All fields are optional - LENS works zero-config

# Branch pattern overrides (default patterns used if not specified)
branch_patterns:
  domain:
    - main
    - master
    - develop
    - "release/*"
    - "hotfix/*"
  service:
    - "service/{name}"
  feature:
    - "feature/{service}/{microservice}/{name}"
    - "feature/{microservice}/{name}"
    - "feature/{name}"

# Directory patterns for working directory detection
directory_patterns:
  service: "services/{name}/"
  microservice: "services/{service}/{name}/"

# Custom lenses (extend the default 4)
custom_lenses:
  - name: component
    icon: 🧩
    level: between microservice and feature
    pattern: "components/{name}/"
    context_files:
      - "components/{name}/README.md"
      - "components/{name}/component.yaml"

# Notification settings
notifications:
  level: smart  # silent | smart | verbose
  show_summary_cards: true
  show_file_counts: true
  show_commit_counts: true
  show_issue_counts: true

# Session settings
session:
  auto_restore: true
  state_file: ".lens-state"

# Auto-discovery settings
discovery:
  enabled: true
  scan_directories:
    - services/
    - apps/
    - src/
  ignore_patterns:
    - node_modules/
    - .git/
    - dist/
    - build/

# Activation conditions (for conditionally global behavior)
activation:
  # LENS activates if ANY of these are true
  conditions:
    - has_directory: services/
    - has_directory: apps/
    - has_file: _lens/lens-config.yaml
    - has_file: _lens/domain-map.yaml
    - branch_matches: "service/*"
    - branch_matches: "feature/*"
```

---

## domain-map.yaml

Domain overview file. Location: `_lens/domain-map.yaml`

```yaml
# Domain Map
# Describes all services and their relationships

domain:
  name: "My Project"
  description: "Project-level description"

services:
  identity:
    description: "Authentication and authorization"
    owner: "platform-team"
    microservices:
      - name: auth-api
        description: "Authentication endpoints"
        dependencies:
          - token-service
      - name: token-service
        description: "JWT token management"
      - name: user-profile
        description: "User profile management"
        dependencies:
          - auth-api

  payments:
    description: "Payment processing"
    owner: "payments-team"
    microservices:
      - name: payment-gateway
        description: "Payment provider integration"
      - name: billing-service
        description: "Subscription and billing"
        dependencies:
          - payment-gateway

# Cross-cutting concerns
shared:
  - name: logging
    description: "Centralized logging infrastructure"
  - name: monitoring
    description: "Metrics and alerting"
  - name: auth-middleware
    description: "Shared authentication middleware"

# Service relationships
relationships:
  - from: payments/billing-service
    to: identity/auth-api
    type: authenticates-with
  - from: "*"
    to: shared/logging
    type: logs-to
```

---

## service.yaml

Individual service definition. Location: `_lens/services/{service-name}/service.yaml`

```yaml
# Service Definition
name: identity
description: "Authentication and authorization service"
owner: platform-team

# Microservices in this service
microservices:
  - name: auth-api
    path: src/services/identity/auth-api
    description: "Authentication endpoints"
    
  - name: token-service
    path: src/services/identity/token-service
    description: "JWT token management"
    
  - name: user-profile
    path: src/services/identity/user-profile
    description: "User profile management"

# Dependencies on other services
dependencies:
  - service: payments
    reason: "User subscription status"
  - service: notifications
    reason: "Email verification"

# Key documentation
docs:
  - README.md
  - docs/authentication-flow.md
  - docs/api-contracts.md
```

---

## .lens-state

Session state file. Location: `_lens/.lens-state`

```yaml
# LENS Session State
# Auto-generated - do not edit manually

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

# Session metrics
session_stats:
  lens_switches: 5
  files_loaded: 12
  duration_minutes: 45
```
