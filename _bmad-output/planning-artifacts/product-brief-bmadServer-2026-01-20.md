---
stepsCompleted: [1, 2, 3, 4, 5]
inputDocuments:
  - /root/bmadServer/_bmad-output/analysis/brainstorming-session-2026-01-20.md
date: 2026-01-20
author: Cris
---

# Product Brief: BMAD-as-a-Service

## Executive Summary

BMAD + OpenCode is a product formation and collaboration system that turns early-stage product intent into a structured, machine-operable product graph. It helps founders and technical co-founders maintain momentum, clarity, and alignment through the critical pre-implementation phase by capturing decisions, freezing requirements, and producing traceable, machine-readable specs. The result is less rework, faster validation cycles, and safer AI-assisted implementation.

**Core differentiator:** BMAD shifts product formation from *document-first* to *graph-first*—Sarah isn't writing a doc, she's building a machine she can run.

---

## Core Vision

### Problem Statement

Early-stage product teams struggle to maintain shared clarity and momentum as they move from idea to implementation. Requirements are often ambiguous, decisions are undocumented, and alignment between product and engineering breaks down, creating churn and rework downstream.

### Problem Impact

When this goes unsolved, teams lose months to rework, architecture churn, missed market timing, and growing mistrust between product and engineering. Founders stall when engineering becomes a bottleneck; engineers build against unstable or vague requirements and must rewrite work later. AI codegen amplifies this risk because it assumes stable, validated intent.

### Why Existing Solutions Fall Short

Current solutions—docs (Notion, Google Docs), PM tools (Jira, Linear), collaboration platforms (Slack, Figma), frameworks (Shape Up, Lean Canvas), and AI codegen tools (Copilot, OpenCode, RAG/agent systems)—are siloed by function and optimized for humans, not machines. None create a unified product graph that captures decisions, validates assumptions before implementation, traces intent to code, and makes the process machine-operable.

### Proposed Solution

A workflow engine that captures decisions, freezes requirements, prevents churn, and unblocks cross-role dependencies while producing machine-readable specs for code generation. Teams move from idea → validated requirements → decision freeze → scenario simulation → spec → implementation without unnecessary translation or rework.

### Day 1 Experience (Sarah)

Sarah lands in an uncluttered workspace answering: "What problem are you trying to advance today?" In five minutes she moves through:
1. **Orientation** — her intent reflected back for confirmation
2. **Structure** — choose a mode (Explore / Draft / Stress-test)
3. **Momentum** — ideas as cards she can select or discard
4. **Confidence** — a storyline emerges from her choices
5. **Exit with agency** — a clear next step and a reusable artifact

She leaves with clarity, structure, momentum, and one obvious next action.

### Key Differentiators

- **Machine-operable product formation:** requirements become structured product graphs, not scattered documents
- **Decision freeze + traceability:** explicit, auditable decisions before implementation
- **Role-aware collaboration:** founders gain momentum; engineers gain stable inputs
- **AI-safe execution:** agents operate on explicit, validated intent
- **Graph-first disruption:** changes the substrate, not just the UI
- **Lived insight advantage:** grounded in Cris's evolution from implementation to Speckit/PBRs to BMAD

---

## Target Users

### Primary Users

#### Sarah — Non-Technical Co-Founder / Product Lead

**Profile:** Business strategist with strong market and customer instincts. Comfortable with vision, positioning, and user needs. Code and technical implementation feel foreign and potentially intimidating.

**Goals:**
- Maintain forward momentum on product formation
- Make decisions without getting blocked by technical ambiguity
- Understand project status without learning engineering terminology
- Validate ideas before committing engineering resources

**Pain Points:**
- Feels blocked when waiting on technical decisions she can't influence
- Overwhelmed by technical detail, schemas, JSON, or engineering jargon
- Loses trust when decisions are undocumented or revisited without explanation
- Frustrated by tools that require artifacts she doesn't have

**Hierarchy of Needs:**
1. Clarity — What's going on?
2. Momentum — What's next?
3. Completion Confidence — Is it good enough?
4. Traceability — What decisions were made and why?
5. Visibility — How far from demoable?
6. Validation — Will customers want this?

**Tension Moment — Almost Closes the Tab:**
When the system removes or obscures her sense of agency. Triggered by funnel pressure, irreversible steps, forced identity capture, or unclear scope. Her mental check: *"Am I still in control?"* If uncertain, she exits.

**Rescue Pattern:** Always show an exit, always preview the commitment, never force identity before value.

**UX Principle:** *"Sarah never loses the exit."*

**Success Moment:** Sarah logs in, sees exactly where the project stands, answers 2-3 business questions, and leaves knowing the project moved forward — all in under 10 minutes.

---

#### Marcus — Technical Co-Founder / Engineer

**Profile:** Engineer who lives in IDE, terminal, and GitHub. Comfortable with ambiguity in code, deeply uncomfortable with ambiguity in requirements. Values flow state, stability, and traceability.

**Goals:**
- Receive unambiguous requirements before implementation
- Understand the "why" behind business decisions
- Avoid rework caused by mid-flight requirement changes
- Commit to implementation only after requirements are validated

**Pain Points:**
- Forced to commit before requirements are stable
- Context-switching into product tools that don't respect his workflow
- Building against vague or shifting goals
- Lack of traceability when things go wrong

**Core Requirements:**
1. Non-ambiguous requirements (who, what, success, out-of-scope)
2. Decision traceability (why, who decided, based on what evidence)
3. Stability before implementation (no churn mid-build)

**Tension Moment — Almost Ignores the Notification:**
When the system asks for execution without previewing the impact. Triggered by missing diffs, missing guarantees, unclear cost/time, or unclear ownership of failure. His mental check: *"Can I verify the consequences before committing?"* If uncertain, he dismisses or shelves the action.

**Rescue Pattern:** Always show the diff, always show the blast radius, always let him simulate before approving.

**UX Principle:** *"Marcus always sees the delta."*

**Success Moment:** Marcus receives a PR or CLI notification, reviews a clear decision with full context and diff, approves in 2 minutes, and returns to coding — knowing requirements are frozen.

---

#### AI Agents — Code Generation and Validation Systems

**Profile:** Autonomous or semi-autonomous systems (Copilot, OpenCode, custom agents) that generate, validate, or execute code based on requirements.

**Goals:**
- Receive stable, machine-readable requirements
- Operate on explicit, validated intent — not ambiguous prose
- Know which decisions are frozen vs. provisional
- Execute safely without human re-validation of every step

**Pain Points:**
- Requirements written for humans, not machines
- No clear signal of what's decided vs. still in flux
- Generates code against unstable or incomplete intent
- Blamed for "hallucinations" that are actually requirement gaps

**Core Requirements:**
1. Structured product graph (not scattered docs)
2. Explicit decision freeze status
3. Machine-readable specs with traceable lineage
4. Validation checkpoints before execution

**Success Moment:** An AI agent queries the product graph, confirms all upstream decisions are frozen, generates implementation code, and passes validation — no human translation required.

---

### User Journey

| Phase | Sarah | Marcus | AI Agent |
|-------|-------|--------|----------|
| **Discovery** | Referred by founder network or finds via "product formation" search | Discovers via technical co-founder or GitHub integration | Integrated by Marcus or auto-detected in toolchain |
| **Onboarding** | 5-minute guided flow → first artifact | CLI install or PR notification → first approval | API connection → schema validation |
| **Core Usage** | Daily: answer questions, review progress, unblock decisions | Async: approve/override decisions via Git/CLI/Teams | Continuous: query graph, generate code, validate |
| **Tension Moment** | "Am I still in control?" | "Can I verify before committing?" | "Is this intent frozen?" |
| **Rescue** | Visible exit, preview commitment | Diff view, blast radius, simulate | Freeze status, validation checkpoint |
| **Aha Moment** | "I see what I'm actually building" | "I finally have stable requirements" | "I can execute without re-asking" |
| **Long-term** | Product formation becomes a repeatable, trusted ritual | BMAD becomes the requirements firewall he always wanted | Graph-first specs become the standard interface |

---

## Success Metrics

### North Star Metric

**Cycle Time Compression:** Days from "intent captured" to "first implementation commit"

This single metric encompasses alignment quality, decision velocity, and workflow efficiency. If Sarah and Marcus are aligned, decisions freeze cleanly, and AI agents can execute without re-asking — cycle time compresses naturally.

| Timeframe | Target | Baseline Method |
|-----------|--------|-----------------|
| Baseline (without BMAD) | 4-8 weeks (feature), 3-6 months (product increment) | Same-team before/after comparison |
| 3 months | 30-40% reduction | Measured from first BMAD project to third |
| 12 months | 50-60% reduction | Rolling average across all active teams |

**Measurement Method:** Timestamp delta between `intent_captured` event and `first_commit_merged` event in product graph.

### Leading Indicator

**Decision Queue Depth:** Count of decisions proposed but not yet frozen

This is the canary in the coal mine — if queue depth climbs, a bottleneck is forming before it shows up in cycle time.

| Threshold | Signal |
|-----------|--------|
| < 5 pending decisions | Healthy flow |
| 5-10 pending decisions | Review needed |
| > 10 pending decisions | Bottleneck — immediate attention |

**Measurement Method:** Count of decisions with status `proposed` older than 48 hours.

### User Success Metrics

| User | Success Metric | Target | Measurement Method |
|------|----------------|--------|-------------------|
| **Sarah** | Decision velocity | < 48 hours from proposed to frozen | Timestamp delta on decision status change |
| **Marcus** | Rework rate | < 10% stories reopened post-freeze | Count of stories with status changed from `ready_for_dev` back to `in_progress` or `blocked` |
| **AI Agents** | Spec completeness | < 5% of generated code requires human clarification | Count of agent queries that return `insufficient_context` or require human override |

### Supporting Metrics

| Metric | Target | Measurement Method |
|--------|--------|-------------------|
| **Decision Freeze Rate** | > 80% within 48 hours | Decisions reaching `frozen` status within 48h / total decisions proposed |
| **Parallel Work Efficiency** | Time gap decreasing over time | Average hours between Sarah's last action and Marcus's first action on same decision |
| **Artifact Completion Rate** | > 70% | Artifacts reaching `frozen` or `ready_for_dev` / total artifacts started |
| **Human-Hours per Decision** | < 2 hours by month 3 | Total logged session time / count of frozen decisions |

### Retention Metric

**Second Project Rate:** % of users who complete a second project within 90 days of first

This signals whether BMAD delivers enough value to become a repeatable workflow, not a one-time experiment.

| Timeframe | Target |
|-----------|--------|
| 3 months | > 30% second project rate |
| 12 months | > 50% second project rate |

**Measurement Method:** Users with ≥2 projects reaching `frozen` status / total users with ≥1 project reaching `frozen` status.

### Business Objectives

**3-Month Milestone:**
- Continuous velocity improvement demonstrated across multiple project cycles
- Measurable cycle time reduction validated with early adopters (30-40% target)
- Decision Queue Depth consistently < 5 across active teams
- Second Project Rate > 30%

**12-Month Milestone:**
- One large-scale enterprise application (50+ stories, 6+ month equivalent scope) delivered end-to-end using BMAD in record time
- Documented case study with before/after comparison
- BMAD positioned as the standard for AI-assisted product formation

**Progress Tracking:** "Largest project completed to date (story count)" — running metric with 12-month target of first project crossing 50 stories.

### Anti-Metrics (Failure Signals)

| Signal | Threshold | What It Means | Measurement Method |
|--------|-----------|---------------|-------------------|
| **Frozen→Unfrozen Rate** | > 20% within 7 days | Decision freeze has no teeth; false stability | Decisions unfrozen within 7 days / total frozen decisions |
| **Speed + Rework Spike** | Cycle time ↓ but post-launch bugs ↑ | Shipping faster but shipping garbage | Correlation analysis: cycle time vs. bug count in first 30 days post-launch |
| **One-Sided Adoption** | Sarah:Marcus activity ratio > 3:1 | Tool is a silo, not a collaboration bridge | Session count ratio between personas |
| **Artifact Graveyards** | Completion rate < 50% | Engagement without outcome | Started artifacts never reaching frozen status |
| **Decision Queue Stagnation** | Queue depth > 10 for > 7 days | Workflow bottleneck | Pending decision count over time |

**North Star Anti-Metric:** "Frozen decisions unfrozen within 7 days" — if this exceeds 20%, BMAD's core promise is broken.

### Key Performance Indicators

| KPI | Target | Timeframe | Measurement Method |
|-----|--------|-----------|-------------------|
| Cycle time (intent → implementation) | 30-40% reduction | 3 months | Event timestamp delta |
| Cycle time (intent → implementation) | 50-60% reduction | 12 months | Event timestamp delta |
| Decision freeze velocity | < 48 hours | Ongoing | Status change timestamp |
| Rework rate (post-freeze) | < 10% | Ongoing | Story status regression count |
| Decision Queue Depth | < 5 pending | Ongoing | Pending decision count |
| Second Project Rate | > 30% / > 50% | 3mo / 12mo | User project completion count |
| Human-hours per decision | < 2 hours | Month 3 | Session time / frozen decisions |

### Monetization (Post-PMF)

**Current stance:** Focus on PMF before monetization; pricing will follow value delivery.

**Leading Indicators for Monetization Viability:**
- Feature requests for Pro-tier capabilities
- Enterprise inquiry rate (inbound interest from teams > 10 people)
- Users who hit usage limits and don't churn

**Likely model:**
- **Free tier:** 1 active project, basic workflows (maximize learning and adoption)
- **Pro tier:** Unlimited projects, advanced AI agent integrations, priority support
- **Enterprise:** SSO, audit trails, custom integrations, dedicated success manager

**Pricing logic:** Usage-based or per-project (value scales with project complexity), not per-seat (value isn't "more users," it's "faster, cleaner product formation").

---

## MVP Scope

### Core Features
- **End-to-end workflow:** Idea → Decision Freeze → Spec → Ready for Dev → Git/PR handoff (as previously described)
- **Core artifact output:** Frozen product graph (primary), with decision log + spec auto-generated as derivative outputs
- **Guided collaboration interface:** Static node map showing current stage + next steps to guide users through async collaboration
- **AI integration minimum:** MBAM correctness defined as: workflow enforces correct sequencing AND AI agents can only operate on frozen decisions

### Out of Scope for MVP
- Enterprise SSO

### MVP Success Criteria
- End-to-end flow completed by internal Sarah + Marcus team
- Decision freeze recorded
- Spec artifact exported
- PR created
- No manual overrides required

### Future Vision
- BMAD evolves into a platform for co‑founder collaboration and product formation
