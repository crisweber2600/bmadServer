# Story 5.2: Agent-to-Agent Messaging

**Status:** done

## Story

As an agent (Architect), I want to request information from other agents, so that I can gather inputs needed for my work.

## Acceptance Criteria

**Given** an agent is processing a step  
**When** it needs input from another agent  
**Then** it can call AgentMessaging.RequestFromAgent(targetAgentId, request, context)

**Given** an agent request is made  
**When** the target agent receives it  
**Then** the request includes: sourceAgentId, requestType, payload, workflowContext, conversationHistory

**Given** the target agent processes the request  
**When** a response is generated  
**Then** the response is returned to the source agent  
**And** the exchange is logged for transparency

**Given** agent-to-agent communication occurs  
**When** I check the message format  
**Then** I see: messageId, timestamp, sourceAgent, targetAgent, messageType, content, workflowInstanceId

**Given** an agent request times out (> 30 seconds)  
**When** no response is received  
**Then** the system retries once  
**And** if still no response, returns error to source agent  
**And** the timeout is logged for debugging

## Tasks / Subtasks

- [x] Analyze acceptance criteria and create detailed implementation plan
- [x] Design data models (AgentMessage, AgentRequest, AgentResponse)
- [x] Implement core business logic (AgentMessaging service with timeout and retry)
- [x] Register service in DI container
- [x] Write unit tests for critical paths (13 unit tests)
- [x] Write BDD tests for all acceptance criteria (6 scenarios)
- [x] All tests passing (158 unit tests + 19 BDD tests)
- [x] Code review and address feedback

## Dev Notes

### Implementation Guidance

This story should be implemented following the patterns established in the codebase:
- Follow the architecture patterns defined in `architecture.md`
- Use existing service patterns and dependency injection
- Ensure proper error handling and logging
- Add appropriate authorization checks based on user roles
- Follow the coding standards and conventions of the project

### Testing Strategy

- Unit tests should cover business logic and edge cases
- Integration tests should verify API endpoints and database interactions
- Consider performance implications for database queries
- Test error scenarios and validation rules

### Dependencies

Review the acceptance criteria for dependencies on:
- Other stories or epics that must be completed first
- External packages or services that need to be configured
- Database migrations that need to be created

## Files to Create/Modify

### Files Created:
- **src/bmadServer.ApiService/Agents/AgentMessage.cs** - Data model for agent messages with all required metadata fields
- **src/bmadServer.ApiService/Agents/AgentRequest.cs** - Data model for agent requests with sourceAgentId, requestType, payload, workflowContext, conversationHistory
- **src/bmadServer.ApiService/Agents/AgentResponse.cs** - Data model for agent responses with success status, data, error, respondingAgentId, timestamp
- **src/bmadServer.ApiService/Agents/IAgentMessaging.cs** - Interface for agent messaging service
- **src/bmadServer.ApiService/Agents/AgentMessaging.cs** - Implementation with 30-second timeout and 1 retry logic, comprehensive logging
- **src/bmadServer.Tests/Unit/AgentMessagingTests.cs** - 13 unit tests covering all core functionality
- **src/bmadServer.BDD.Tests/Features/AgentToAgentMessaging.feature** - 6 BDD scenarios covering all acceptance criteria
- **src/bmadServer.BDD.Tests/StepDefinitions/AgentToAgentMessagingSteps.cs** - Step definitions for BDD tests

### Files Modified:
- **src/bmadServer.ApiService/Program.cs** - Added AgentMessaging service registration in DI container

### Implementation Details:
- AgentMessaging service uses IAgentRegistry to validate target agents
- Timeout configured at 30 seconds with 1 retry as per requirements
- Comprehensive logging for request initiation, completion, timeouts, and errors
- Message format includes messageId, timestamp, sourceAgent, targetAgent, messageType, content, workflowInstanceId
- MVP implementation with stub ProcessAgentRequestAsync method (will be replaced with actual AI model invocation in Phase 2)
- All 158 unit tests + 19 BDD tests passing


---

## Aspire Development Standards

### PostgreSQL Connection Pattern

This story uses PostgreSQL configured in Story 1.2 via Aspire:
- Connection string automatically injected from Aspire AppHost
- Pattern: `builder.AddServiceDefaults();` (inherits PostgreSQL reference)
- See Story 1.2 for AppHost configuration pattern

### Project-Wide Standards

This story follows the Aspire-first development pattern:
- **Reference:** [PROJECT-WIDE-RULES.md](../../../PROJECT-WIDE-RULES.md)
- **Primary Documentation:** https://aspire.dev
- **GitHub:** https://github.com/microsoft/aspire

---


### Future: Distributed Messaging Pattern

When distributed agents needed in Phase 2:
- Check: https://aspire.dev for messaging components
- Options: RabbitMQ (`aspire add RabbitMq.Aspire`) or Kafka
- Current MVP: In-process messaging via Service Collection DI

## References
- **Aspire Rules:** [PROJECT-WIDE-RULES.md](../../../PROJECT-WIDE-RULES.md)
- **Aspire Docs:** https://aspire.dev

- Source: [epics.md - Story 5.2](../planning-artifacts/epics.md)
- Architecture: [architecture.md](../planning-artifacts/architecture.md)
- PRD: [prd.md](../planning-artifacts/prd.md)
