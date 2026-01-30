---
name: onboarding
description: First-time user walkthrough for LENS
---

# Onboarding Workflow

**Goal:** Guide first-time users through LENS setup and demonstrate key features.

## Trigger Conditions

This workflow runs automatically when:
- No `_lens/` directory exists AND
- `enable_onboarding: true` in config (default) AND
- Multi-service indicators detected

Or manually via `lens onboarding` or Navigator menu.

## Walkthrough Steps

### Step 1: Welcome

```
🧭 Welcome to LENS!

LENS (Layered Enterprise Navigation System) helps you navigate
large codebases by automatically detecting your architectural
context and loading relevant information.

This quick walkthrough will help you get started.

[Continue] [Skip - I know what I'm doing]
```

### Step 2: Detect Project Structure

```
📊 Analyzing your project...

I found:
   📂 {directory_count} potential service directories
   🌿 Git repository: {yes/no}
   📋 Branch: {current_branch}

Let me identify the services...
```

Run auto-discovery and show results:

```
✅ Project Analysis Complete

📦 Services detected: {count}
{for each service}
   • {service_name}/
     └── {microservices}
{end for}

Does this look right? [Yes] [No - let me configure]
```

### Step 3: Explain Lenses

```
🔭 Understanding LENS Lenses

LENS uses four "lenses" to show you relevant context:

  🛰️ Domain Lens — Satellite view
     See all services, architecture, cross-cutting concerns
     Active on: main, develop, release branches
  
  🗺️ Service Lens — City map view
     See one service and its microservices
     Active on: service/* branches
  
  🏘️ Microservice Lens — Street level view
     See one microservice's details
     Active on: Within a specific microservice directory
  
  📍 Feature Lens — Indoor navigation
     See files you're working on, related tests, commits
     Active on: feature/* branches

LENS detects your lens automatically from your git branch
and working directory!

[Got it!]
```

### Step 4: Current Lens Demo

```
📍 Your Current Lens

Based on your current state:
   Branch: {current_branch}
   Directory: {current_directory}

LENS determined you're at: {detected_lens} Lens

Why this lens:
   {detection_reason} (source: {detection_source})

{appropriate_summary_card}

This context was loaded automatically.

[Show what would change if I switch]
```

### Step 5: Quick Tour

```
🚀 Quick Tour

Here are the key things you can do:

1️⃣ Check your context anytime:
   "lens status" — See current lens and loaded context

2️⃣ Create new architecture:
   "lens new-service" — Create a new service
   "lens new-micro" — Create a new microservice
   "lens new-feature" — Start a feature branch

3️⃣ Navigate the domain:
   "lens map" — View domain overview
   "lens impact" — Check cross-boundary impacts

4️⃣ LENS works automatically!
   Switch branches → Context updates
   Change directories → Lens adjusts
   
[Try it out!]
```

### Step 6: Configuration Options

```
⚙️ Configuration (Optional)

LENS works great with zero configuration, but you can
customize it for your project:

Would you like to:

1. Use defaults (recommended for most projects)
2. Create minimal config (customize branch patterns)
3. Create full config (domain map, all options)

[1 - Use defaults]
```

**If user chooses 2:**
Run `lens-configure` workflow in minimal mode.

**If user chooses 3:**
Run `lens-configure` workflow in full mode.

### Step 7: Completion

```
✅ You're all set!

LENS is now active and will:
   ✓ Detect your context automatically
   ✓ Load relevant files and info
   ✓ Notify you on meaningful lens changes
   ✓ Remember your session for continuity

Next time you return, LENS will offer to restore your last context.

Quick Reference:
   📍 "lens status" — Check current lens
   🗺️ "lens map" — View domain
   ❓ "lens help" — Get help

{summary_card_for_current_context}

Happy navigating! 🧭
```

---

## Skip Options

At any step, user can:
- **Skip step:** Move to next step
- **Skip all:** Exit onboarding, mark as complete
- **Come back later:** Exit without marking complete

---

## State Tracking

Create `_lens/.onboarding-state`:

```yaml
status: completed  # not_started | in_progress | completed | skipped
completed_steps:
  - welcome
  - project_analysis
  - explain_lenses
  - current_lens_demo
  - quick_tour
  - configuration
  - completion
completed_at: {timestamp}
configuration_level: defaults  # defaults | minimal | full
```

---

## Re-Running Onboarding

User can re-run with:
- `lens onboarding` — Run full walkthrough
- `lens onboarding --reset` — Reset state and run fresh

---

## Adaptive Content

Onboarding adapts based on:

| Situation | Adaptation |
|-----------|------------|
| No services found | Offer to create first service |
| Single service | Focus on microservice/feature lenses |
| Existing _lens/ config | Skip config step, explain current setup |
| Complex project | Offer detailed domain mapping |

---

## Error States

| Issue | Response |
|-------|----------|
| Not a git repo | Explain directory-based detection only |
| No services detected | Offer to create project structure |
| Config exists but invalid | Offer to fix or regenerate |
