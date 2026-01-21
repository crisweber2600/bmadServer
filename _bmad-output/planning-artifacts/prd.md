---
stepsCompleted: [step-01-init, step-02-discovery, step-03-success, step-04-journeys, step-05-domain, step-06-innovation, step-07-project-type, step-08-scoping, step-09-functional, step-10-nonfunctional, step-11-polish]
inputDocuments:
  - /Users/cris/bmadServer/_bmad-output/planning-artifacts/product-brief-bmadServer-2026-01-20.md
  - /Users/cris/bmadServer/_bmad-output/analysis/brainstorming-session-2026-01-20.md
workflowType: 'prd'
documentCounts:
  productBrief: 1
  research: 0
  brainstorming: 1
  projectDocs: 0
classification:
  projectType: developer_tool
  domain: devtools_devops
  complexity: medium
  projectContext: greenfield
createdAt: 2026-01-21
updatedAt: 2026-01-21
---

# Product Requirements Document - bmadServer

**Author:** Cris
**Date:** 2026-01-21

## Success Criteria

### User Success

- **First BMAD workflow completes end-to-end** through bmadServer (not CLI)
- **Chat interface successfully guides users** through workflows without them needing to know BMAD internals
- **Both Sarah-types and Marcus-types can complete workflows** without switching to terminal
- **Workflow state persists** across sessions - users can leave and return
- **Multi-agent collaboration works** - at least 2 agents can pass work between each other

### Business Success

- **Replace 100% of BMAD CLI usage** within the team within 30 days of deployment
- **Complete at least 5 real project workflows** through the system (PRD, Architecture, Epics, etc.)
- **Time to complete workflow ≤ current CLI time** (not slower than existing process)
- **Zero workflow failures** due to server issues (router errors don't count as failures, just need proper handling)

### Technical Success

- **WebSocket connections stable** for full workflow duration (30+ minutes)
- **Message routing works** between web UI, server, and agents
- **State management handles** interruptions, refreshes, disconnections gracefully
- **Agent responses render properly** in web UI (markdown, code blocks, menus)
- **System handles concurrent workflows** without cross-contamination

### Measurable Outcomes

- **Primary Success Metric:** First complete BMAD workflow via chat interface
- **Adoption Metric:** 100% replacement of CLI-based BMAD usage
- **Performance Metric:** Workflow completion time ≤ current CLI approach
- **Reliability Metric:** 95% workflow completion rate without server failures
- **The "fuck yes" moment:** User types request in chat, bmadServer orchestrates agents, delivers result WITHOUT touching terminal

## User Journeys

### Sarah (Non-Technical Co-Founder) - "Finally, I Can Move Forward"

**Opening Scene:** Sarah opens her laptop with that familiar knot in her stomach. The roadmap meeting is tomorrow and she still doesn't know if the auth system supports the enterprise features their biggest prospect needs. She has been blocked for a week waiting on technical decisions she cannot influence.

**Rising Action:** She opens bmadServer and types: "I need to understand what our authentication can handle for enterprise customers." The PM agent responds in plain English, asks about specific enterprise needs. As she answers, the system orchestrates a conversation between PM and Architect agents while she watches.

**Climax:** Within 10 minutes, she has a clear answer in business terms: "Current auth design supports multi-tenant with modifications. SSO requires 2-week integration. Here's what customers would experience..." She sees a confidence check and approves the summary before it is locked.

**Resolution:** She walks into the roadmap meeting prepared and unblocked. For the first time in months, she feels like a co-founder instead of a spectator.

### Marcus (Technical Co-Founder) - "Finally, Stable Ground"

**Opening Scene:** Marcus stares at the authentication module he is refactoring for the third time this month. Requirements keep shifting and his git history is a graveyard of half-implemented features.

**Rising Action:** He gets a notification: "PRD section updated: Authentication Requirements pending your review." He opens bmadServer, sees Sarah's enterprise discussion captured and structured. The Architect agent has mapped it to technical constraints and proposed an implementation path.

**Climax:** He asks: "Show me what changed from last week's auth spec." The system shows a clear decision diff: what's new, what's unchanged, what's deferred. He approves the changes and syncs the new constraints into his CI checks.

**Resolution:** He codes for hours without interruptions. The auth module ships and stays done because the decisions are locked and traceable.

### Cris (System Operator) - "The System That Runs Itself"

**Opening Scene:** You are exhausted from manually running workflows and copy-pasting between terminals. Every workflow feels like starting from scratch.

**Rising Action:** You deploy bmadServer locally and run a PRD workflow through the chat interface. The server loads state, restores context, and keeps multiple agents aligned.

**Climax:** A router failure triggers an alert. You open the incident panel, roll back to a known-good workflow state, and restart the session without losing context. The system resumes where it left off.

**Resolution:** By day three, you haven't opened a terminal for BMAD workflows once. The system now runs the workflow end-to-end and survives failures without derailing your progress.

### BMAD Agent (System User) - "Finally, A Proper Stage"

**Opening Scene:** The PM agent activates in response to a user request but now has access to session state, peer agents, and persistent context.

**Rising Action:** It needs input from the Architect agent. Instead of asking the user to rerun a command, the PM agent sends a structured request through bmadServer. The Architect responds with constraints and trade-offs.

**Climax:** The agents hit a low-confidence zone. The system pauses for a human checkpoint and asks for explicit approval before committing the decision.

**Resolution:** Agents collaborate without losing context, and decisions are captured with attribution, confidence, and traceability.

### New Journey: Product Owner - "Turning Decisions into Direction"

**Opening Scene:** A product owner needs to translate founder intent into a real backlog. Requirements are scattered across docs, chats, and meetings.

**Rising Action:** She uses bmadServer to consolidate decisions into a single requirements view and prioritizes features against success metrics.

**Climax:** She locks the next sprint scope, exports a structured spec, and gets stakeholder sign-off in one place.

**Resolution:** The backlog reflects reality, and the team executes without ambiguity.

### New Journey: Security/Compliance - "Trust the System"

**Opening Scene:** Security needs to ensure no sensitive data leaks and that decisions are auditable.

**Rising Action:** They configure policies, restrict model usage, and set audit log retention.

**Climax:** An audit request arrives. They produce a full decision trail with timestamps, actors, and approvals.

**Resolution:** Security signs off because governance is built-in, not bolted on.

### New Journey: Support/CS - "Fix It Without Guessing"

**Opening Scene:** A user reports a broken workflow. Support needs to diagnose fast without deep technical context.

**Rising Action:** Support pulls the workflow run, sees errors and decision history, and replays the session with guardrails.

**Climax:** They patch the config and notify the user with a clear explanation.

**Resolution:** Issues resolve quickly and the system learns from the support intervention.

### New Journey: External Integrator - "Build on the Platform"

**Opening Scene:** A developer wants to integrate bmadServer with their internal tools.

**Rising Action:** They authenticate, read API docs, and subscribe to workflow events.

**Climax:** Their integration triggers workflows and receives structured results via webhook.

**Resolution:** bmadServer becomes part of their core tooling, not a sidecar.

### Journey Requirements Summary

These journeys reveal core capability requirements:
- Conversational UI that translates between business and technical language
- Agent orchestration with structured agent-to-agent messaging
- Decision locking, approval gates, and requirement diffs
- Session persistence across time and device changes
- Notifications and alerts for critical updates
- Observability: logs, metrics, traces, and replay
- Role-based access control with audit trails
- Cost and rate-limit controls with graceful fallback
- Integration APIs, webhooks, and event streaming

## Innovation & Novel Patterns

### Detected Innovation Areas

- **Flow-preserving collaboration:** Multiple people can contribute without breaking the BMAD agent's step-by-step flow.
- **Conversation-native orchestration:** Collaboration happens inside the flow instead of moving to external tools.
- **Decision traceability without interrupting momentum:** The flow keeps progressing while decisions are captured and frozen.

### Market Context & Competitive Landscape

- Existing tools (Notion/Jira/Linear/Slack) break flow and require manual translation.
- AI copilots focus on individual users, not shared orchestration.
- bmadServer treats collaboration as a first-class part of the workflow, not a side channel.

### Validation Approach

- Dogfood with multi-user sessions: 2+ people collaborate in a single workflow without derailing it.
- Measure interruption cost: compare completion time and decision churn vs. CLI BMAD.
- Prove continuity: workflows resume correctly after collaborative inputs and context switching.

### Risk Mitigation

- If collaboration interrupts flow: introduce a "collab window" mode that buffers inputs until the agent reaches a safe checkpoint.
- If multiple users conflict: add decision arbitration (voting/approval gates).
- If flow slows down: allow "solo mode" to temporarily reduce collaboration overhead.

## Developer Tool Specific Requirements

### Project-Type Overview

bmadServer must preserve the full BMAD capability set and present it through a chat-first orchestration layer. It should be feature-parity with existing BMAD workflows, not a reduced subset.

### Technical Architecture Considerations

- Language-agnostic workflow execution: no hard language constraints beyond what BMAD already supports.
- Agent and workflow parity: every BMAD module/workflow supported today must be runnable via the server.
- Provider parity: all model providers supported by BMAD must be routable without regression.
- Artifact parity: outputs and file formats match existing BMAD artifacts.
- State parity: pause/resume/restore semantics match BMAD CLI behavior.
- Self-hosted deployment as the default (dogfooding-first).
- WebSocket-first interaction model for chat + agent orchestration.
- State persistence to allow long-running, multi-user workflows without context loss.
- Offline-capable deployment: no hard requirement for external services.

### Language Matrix

- **Support scope:** Match BMAD's current language support matrix.
- **Constraint:** No language-specific exclusions introduced by the server.
- **Future:** Explicit language list can be documented once BMAD's matrix is finalized.

### Installation Methods

- Self-hosted deployment required for MVP.
- Provide a minimal, repeatable deployment path aligned with existing BMAD setup.

### API Surface

- WebSocket API for chat, events, and agent orchestration.
- Admin/ops endpoints for session management, audit, and health checks.
- Webhooks/event stream for integrations.

### Code Examples

- End-to-end PRD workflow example.
- Architecture workflow example.
- Multi-user collaboration example (flow-preserving).
- Agent handoff example (PM to Architect to Dev).

### Migration Guide

- Clear mapping from current BMAD CLI usage to bmadServer chat flows.
- How to import existing sessions or artifacts.
- Compatibility notes for existing workflows and prompts.
- Workflow parity matrix and known gaps (if any).

### Collaboration UX Guardrails

- Safe checkpoints for collaboration inputs.
- Clear workflow position and ownership indicators.
- Approval gates for decision locking.

### Implementation Considerations

- Preserve BMAD workflow structure, menus, and step sequencing.
- Version workflows to prevent breaking changes.
- Keep outputs compatible with existing BMAD artifacts.

## Scope & Phased Development

### MVP Strategy & Philosophy

**MVP Approach:** Experience MVP focused on flow-preserving chat orchestration.
**Resource Requirements:** Small core team (1-2 engineers + PM/UX input) to reach dogfoodable parity.

### MVP Feature Set (Phase 1)

**Core User Journeys Supported:**
- Sarah (non-technical co-founder) completes a BMAD workflow via chat.
- Marcus (technical co-founder) consumes stable outputs and decision diffs.
- Cris (operator) runs workflows end-to-end with persistence and recovery.

**Core Functionality (Must Work Day 1):**
- Chat interface (send messages, see responses with proper formatting).
- WebSocket server (handle connections, route messages reliably).
- BMAD agent integration (PM, Architect, Dev agents minimum).
- Session persistence (can refresh browser without losing context).
- Basic workflow state tracking (know where you are in a workflow).

**Must-Have Capabilities:**
- Chat interface with flow-preserving guidance.
- Async progress across BMAD workflows (pause/resume across sessions).
- Technical and business persona support (language translation + role context).
- Workflow parity for core BMAD flows needed for dogfooding.

**Key Constraint:** Must be dogfoodable for the team's actual BMAD workflows immediately.

### Post-MVP Features

**Phase 2 (Post-MVP):**
- Workflow visualization/progress map.
- Usage analytics (completion rates, time saved, churn).
- Integrations (GitHub/Slack/Jira).
- Webhooks/event stream for external tools.
- Workflow templates and shortcuts for common patterns.
- Parallel agent execution for faster workflows.
- Audit trail and decision history.
- Role-based access control (Sarah vs Marcus permissions).
- Agent voting/consensus UI and arbitration.

**Phase 3 (Expansion):**
- Multi-tenant workspaces and advanced RBAC.
- Full BMAD-as-a-Service platform for external teams.
- Marketplace for workflows and agents.
- AI-powered workflow optimization and suggestions.
- Workflow branching and merging capabilities.
- Real-time collaborative editing.
- Natural language workflow creation.
- Self-improving through usage analytics.

### Risk Mitigation Strategy

**Technical Risks:** Flow-preserving collaboration is harder than expected. Mitigate with collaboration checkpoints and buffering.
**Market Risks:** Users may not switch from CLI. Mitigate by matching BMAD parity and measuring time-to-completion.
**Resource Risks:** Scope parity is too big for MVP. Mitigate with strict parity focus on dogfood workflows only.

**Riskiest Assumption:** We can deliver BMAD parity without delaying the MVP.

## Functional Requirements

### Workflow Orchestration

- FR1: Users can start any supported BMAD workflow via chat.
- FR2: Users can resume a paused workflow at the correct step.
- FR3: Users can view current workflow step, status, and next required input.
- FR4: Users can safely advance, pause, or exit a workflow.
- FR5: The system can route workflow steps to the correct agent.

### Collaboration & Flow Preservation

- FR6: Multiple users can contribute to the same workflow without breaking step order.
- FR7: Users can submit inputs that are applied at safe checkpoints.
- FR8: Users can see who provided each input and when.
- FR9: Users can lock decisions to prevent further changes.
- FR10: Users can request a decision review before locking.
- FR11: The system can buffer conflicting inputs and require human arbitration.

### Personas & Communication

- FR12: Users can interact using business language and receive translated outputs.
- FR13: Users can interact using technical language and receive technical details.
- FR14: The system can adapt responses to a selected persona profile.
- FR15: Users can switch persona mode within a session.

### Session & State Management

- FR16: Users can return to a session and retain full context.
- FR17: The system can recover a workflow after a disconnect or restart.
- FR18: Users can view the history of workflow interactions.
- FR19: Users can export workflow artifacts and outputs.
- FR20: The system can restore previous workflow checkpoints.

### Agent Collaboration

- FR21: Agents can request information from other agents with shared context.
- FR22: Agents can contribute structured outputs to a shared workflow state.
- FR23: The system can display agent handoffs and attribution.
- FR24: The system can pause for human approval when agent confidence is low.

### Parity & Compatibility

- FR25: The system can execute all BMAD workflows supported by the current BMAD version.
- FR26: The system can produce outputs compatible with existing BMAD artifacts.
- FR27: The system can maintain workflow menus and step sequencing parity.
- FR28: Users can run workflows without CLI access.
- FR29: The system can surface parity gaps or unsupported workflows.

### Admin & Ops

- FR30: Admins can view system health and active sessions.
- FR31: Admins can manage access and permissions for users.
- FR32: Admins can configure providers and model routing rules.
- FR33: Admins can audit workflow activity and decision history.
- FR34: Admins can configure self-hosted deployment settings.

### Integrations

- FR35: The system can send workflow events via webhooks.
- FR36: The system can integrate with external tools for notifications.

## Non-Functional Requirements

### Performance

- Chat UI acknowledges inputs within 2 seconds.
- Agent response streaming starts within 5 seconds for typical prompts.
- Standard workflow step responses complete within 30 seconds.

### Reliability

- 99.5% uptime for dogfood deployments.
- Fewer than 5% workflow failures excluding provider outages.
- Session recovery after reconnect within 60 seconds.

### Security

- TLS for all traffic in transit.
- Encryption at rest for stored sessions and artifacts.
- Audit logs retained for 90 days (configurable).

### Scalability

- Support 25 concurrent users and 10 concurrent workflows in MVP.
- Graceful degradation beyond limits via queueing or throttling.

### Integration

- Webhooks deliver at-least-once with retries for 24 hours.
- Event stream ordering is guaranteed per workflow.

### Usability

- Time to first successful workflow under 10 minutes.
- Resume after interruption in under 2 minutes.
