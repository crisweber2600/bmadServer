# LENS Module TODO

## Phase 1: Core Infrastructure ✅

- [x] **Navigator Agent** — Create full agent spec with menu commands
- [x] **lens-detect workflow** — Implement git branch + directory detection logic
- [x] **lens-switch workflow** — Implement lens transition with notifications
- [x] **context-load workflow** — Implement context file loading per lens

## Phase 2: Feature Workflows ✅

- [x] **new-service workflow** — Service creation at Domain Lens
- [x] **new-microservice workflow** — Microservice scaffolding at Service Lens
- [x] **new-feature workflow** — Feature branch + context creation
- [x] **domain-map workflow** — Domain overview generation
- [x] **onboarding workflow** — First-time user walkthrough

## Phase 3: Utility Workflows ✅

- [x] **lens-configure workflow** — Project-specific configuration
- [x] **service-registry workflow** — Service mapping management
- [x] **impact-analysis workflow** — Cross-boundary impact detection
- [x] **lens-sync workflow** — Auto-discovery ↔ explicit config sync
- [x] **lens-restore workflow** — Session state restoration

## Phase 4: Data & Templates ✅

- [x] **lens-config.yaml schema** — Branch pattern configuration (in schemas.md)
- [x] **domain-map.yaml schema** — Service/microservice mapping (in schemas.md)
- [x] **service.yaml schema** — Individual service definition (in schemas.md)
- [x] **.lens-state schema** — Session state persistence (in schemas.md)
- [x] **Config templates** — Starter configs for common setups (in templates/)
- [x] **service.yaml template** — Added to templates/config-templates/service-template.yaml
- [x] **feature-context.yaml template** — Added to templates/config-templates/feature-context-template.yaml

## Phase 5: Integration ✅

- [x] **BMM integration** — Story creation context population (create-story workflow.yaml + instructions.xml)
- [x] **Agent manifest entry** — Navigator added to agent-manifest.csv
- [x] **Variable injection** — LENS exports declared in module.yaml, config.yaml created
- [x] **Activation conditions** — Conditional activation implemented in module.yaml
- [x] **Core workflow.xml integration** — Workflows use standard .md format executed via core task runner
- [x] **Module registration** — LENS added to manifest.yaml modules list
- [x] **Workflow registration** — All 13 workflows added to workflow-manifest.csv

## Phase 6: Testing & Documentation

- [ ] **Test on monorepo** — Validate multi-service detection
- [ ] **Test trunk-based** — Validate directory fallback
- [ ] **Test session restore** — Validate .lens-state persistence
- [x] **User documentation** — Created docs/usage-guide.md with full usage guide
- [x] **Update README** — Added workflow documentation links and BMM integration docs
- [x] **Navigator customization** — Created lens-navigator.customize.yaml
- [x] **Files manifest** — All 24 LENS files registered in files-manifest.csv

---

## Quick Wins (Completed) ✅

1. ~~Navigator agent spec~~ ✅
2. ~~lens-detect workflow (core logic)~~ ✅
3. ~~lens-config.yaml schema~~ ✅
4. ~~Basic integration test~~ (pending Phase 6)

## Design Decisions Captured

- **Conditionally Global** — Only activates on multi-service indicators
- **Single Agent with Modes** — Navigator handles all lenses
- **Zero-config progressive disclosure** — Works OOTB, config adds power
- **Smart notifications** — Meaningful transitions only
- **Session persistence** — .lens-state for continuity
- **Flexible detection** — Git-first, Directory-first, or Auto
- **Custom lens extensibility** — Projects can add lenses
- **Active noise reduction** — Hide irrelevant, not just show relevant

---

## Implementation Progress Summary

### Completed Workflows

| Workflow | Description | Status |
|----------|-------------|--------|
| lens-detect | Git branch + directory detection | ✅ Full spec |
| lens-switch | Transition notifications | ✅ Full spec |
| context-load | Context file loading | ✅ Full spec |
| new-service | Service scaffolding | ✅ Full spec |
| new-microservice | Microservice scaffolding | ✅ Full spec |
| new-feature | Feature branch creation | ✅ Full spec |
| domain-map | Domain overview | ✅ Full spec |
| onboarding | First-time walkthrough | ✅ Full spec |
| lens-configure | Configuration setup | ✅ Full spec |
| service-registry | Service management | ✅ Full spec |
| impact-analysis | Cross-boundary analysis | ✅ Full spec |
| lens-sync | Config synchronization | ✅ Full spec |
| lens-restore | Session restoration | ✅ Full spec |

### Integration Points

| Integration | Target | Status |
|-------------|--------|--------|
| Agent manifest | `_config/agent-manifest.csv` | ✅ Navigator registered |
| Module manifest | `_config/manifest.yaml` | ✅ lens module added |
| Workflow manifest | `_config/workflow-manifest.csv` | ✅ 13 workflows registered |
| BMM create-story | `bmm/workflows/4-implementation/create-story/` | ✅ LENS variables + instructions |
| Module config | `lens/config.yaml` | ✅ Runtime variables defined |
| Variable exports | `lens/module.yaml` | ✅ 6 exports declared |
| Activation conditions | `lens/module.yaml` | ✅ Conditional global behavior |

### Next Steps

1. Test LENS on a real monorepo project
2. Test trunk-based development (directory fallback) scenario
3. Test session restore via .lens-state persistence
4. Write user documentation and usage guides
