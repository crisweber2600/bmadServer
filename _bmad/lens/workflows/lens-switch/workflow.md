---
name: lens-switch
description: Switch to a different lens with appropriate notification
---

# Lens Switch Workflow

**Goal:** Transition between lenses with smart notifications and context loading.

## Switch Types

### 1. User-Initiated Switch

User explicitly requests a lens change via menu or command.

**Behavior:**
- Acknowledge the switch
- Load new context
- Display summary card
- Update .lens-state

### 2. Automatic Switch

LENS detects that work requires a different lens.

**Behavior:**
- Notify user BEFORE switching
- Explain WHY the switch is happening
- Load new context
- Display summary card
- Update .lens-state

### 3. Silent Switch

Routine context updates that don't require notification.

**Behavior:**
- Load context silently
- Update internal state
- No user notification

---

## Notification Levels

Based on `{notification_level}` config:

### Silent Mode
- Only errors and warnings
- No transition notifications

### Smart Mode (Default)
- Notify on meaningful transitions:
  - Domain ↔ Service (significant scope change)
  - Entering/leaving Feature lens
  - Cross-boundary impact detected
- Silent on:
  - Minor context refreshes
  - Same-lens updates

### Verbose Mode
- Notify on all transitions
- Show detailed context changes
- Debug information available

---

## Switch Flow

### 1. Validate Target Lens

If switching to Service/Microservice/Feature lens, ensure we have:
- Service name (for Service lens)
- Microservice name (for Microservice lens)
- Feature context (for Feature lens)

If missing, prompt user to select.

### 2. Pre-Switch Notification (if not silent)

```
⚠️ Switching from {current_lens} to {target_lens}
   Reason: {reason}
   Detected via: {detection_source} ({detection_signal})
   Loading context...
```

### 3. Save Current State

Update `_lens/.lens-state` with current context before switching.

### 4. Execute Context Load

Call `context-load` workflow for new lens.

### 5. Post-Switch Summary

Display summary card for new lens context.

---

## Transition Messages

### Domain → Service
```
🗺️ Zooming in to Service Lens: {service_name}
   Leaving satellite view, entering city map...
```

### Service → Microservice
```
🏘️ Zooming in to Microservice Lens: {microservice_name}
   From city map to street level...
```

### Microservice → Feature
```
📍 Zooming in to Feature Lens: {feature_name}
   Street level to indoor navigation...
```

### Feature → Domain (zoom out)
```
🛰️ Zooming out to Domain Lens
   From indoor navigation back to satellite view...
   {reason_if_applicable}
```

---

## Cross-Boundary Detection

If user's changes affect other services/microservices:

```
⚠️ This change affects {other_service}.
   Zooming out to Service Lens to show impact...
   
   Affected areas:
   - {service_1}: {reason}
   - {service_2}: {reason}
   
   [Return to Previous Lens] [Show Impact Analysis]
```

---

## State Update

After every switch, update `.lens-state`:

```yaml
last_lens: {new_lens}
last_service: {service_name}
last_microservice: {microservice_name}
last_feature: {feature_name}
last_context_files:
  - {file_1}
  - {file_2}
timestamp: {current_timestamp}
previous_lens: {old_lens}
```
