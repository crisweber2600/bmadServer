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

## Phase 4: Data & Templates

- [x] **lens-config.yaml schema** — Branch pattern configuration (in schemas.md)
- [x] **domain-map.yaml schema** — Service/microservice mapping (in schemas.md)
- [x] **service.yaml schema** — Individual service definition (in schemas.md)
- [x] **.lens-state schema** — Session state persistence (in schemas.md)
- [x] **Config templates** — Starter configs for common setups (in templates/)
- [ ] **service.yaml template** — Add to templates/config-templates/
- [ ] **feature-context.yaml template** — For feature tracking

## Phase 5: Integration

- [ ] **BMM integration** — Story creation context population
- [ ] **Agent manifest entry** — Add Navigator to manifest
- [ ] **Variable injection** — Ensure LENS variables available globally
- [ ] **Activation conditions** — Implement conditional global behavior
- [ ] **Core workflow.xml integration** — Ensure workflows execute via core task runner

## Phase 6: Testing & Documentation

- [ ] **Test on monorepo** — Validate multi-service detection
- [ ] **Test trunk-based** — Validate directory fallback
- [ ] **Test session restore** — Validate .lens-state persistence
- [ ] **User documentation** — Usage guides and examples
- [ ] **Update README** — Add workflow documentation links

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

### Next Steps

1. Add remaining templates (service.yaml, feature-context.yaml)
2. Complete BMM integration for story→feature workflow
3. Add Navigator to agent manifest
4. Create integration tests
5. Write user documentation
