Feature: Multi-Agent Collaboration
  As a workflow participant
  I want multiple agents to collaborate
  So that complex tasks are handled by specialists

  Background:
    Given the agent system is initialized
    And I am authenticated as a valid user

  # Story 5.1: Agent Registry & Configuration
  @epic-5 @story-5-1
  Scenario: Query all registered agents
    When I query GetAllAgents()
    Then I should receive at least 6 agents
    And the agents should include "ProductManager"
    And the agents should include "Architect"
    And the agents should include "Designer"
    And the agents should include "Developer"
    And the agents should include "Analyst"
    And the agents should include "Orchestrator"

  @epic-5 @story-5-1
  Scenario: Agent has required properties
    When I examine any agent in the registry
    Then it should include AgentId
    And it should include Name
    And it should include Description
    And it should include Capabilities
    And it should include SystemPrompt
    And it should include ModelPreference

  @epic-5 @story-5-1
  Scenario: Query agents by capability
    When I call GetAgentsByCapability with "create-prd"
    Then I should receive the ProductManager agent
    And the agent should have matching capability

  @epic-5 @story-5-1
  Scenario: Agent model preference is configurable
    Given agents have model preferences configured
    When an agent is invoked
    Then the system should route to the preferred model

  # Story 5.2: Agent-to-Agent Messaging
  @epic-5 @story-5-2
  Scenario: Agent can send message to another agent
    Given two agents are registered
    When Agent A sends a message to Agent B
    Then Agent B should receive the message
    And the message should include sender
    And the message should include content
    And the message should include timestamp
    And the message should include correlation ID

  @epic-5 @story-5-2
  Scenario: Message routing to correct agent by capability
    Given a workflow step requires collaboration
    When the orchestrator routes message to specialist
    Then the correct agent should receive based on capability match

  @epic-5 @story-5-2
  Scenario: Message delivery is reliable
    Given multiple messages are in flight
    When messages are processed
    Then all messages should be delivered in order
    And no messages should be lost

  # Story 5.3: Shared Workflow Context
  @epic-5 @story-5-3
  Scenario: Agent accesses shared context
    Given a workflow has multiple completed steps
    When an agent receives a request
    Then it should have access to SharedContext
    And SharedContext should contain all step outputs

  @epic-5 @story-5-3
  Scenario: Query specific prior output from context
    Given a workflow has completed step "step-1"
    When agent queries SharedContext.GetStepOutput("step-1")
    Then it should receive the structured output

  @epic-5 @story-5-3
  Scenario: Query non-existent step returns null
    Given a workflow has not completed step "step-99"
    When agent queries SharedContext.GetStepOutput("step-99")
    Then it should receive null

  @epic-5 @story-5-3
  Scenario: Output automatically added to context on step completion
    Given an agent produces output
    When the step completes
    Then output should be automatically added to SharedContext
    And subsequent agents should access it immediately

  @epic-5 @story-5-3
  Scenario: Large context is summarized
    Given context grows large and exceeds token limits
    When agent accesses context
    Then older context should be summarized
    And key decisions should be preserved
    And full context should be available in database

  @epic-5 @story-5-3 @concurrency
  Scenario: Concurrent context access is safe
    Given multiple agents access context simultaneously
    When reads and writes occur
    Then optimistic concurrency control should prevent conflicts
    And version numbers should track changes

  # Story 5.4: Agent Handoff & Attribution
  @epic-5 @story-5-4
  Scenario: Handoff displays transition message
    Given a workflow step changes agents
    When handoff occurs
    Then UI should display "Handing off to [AgentName]..."

  @epic-5 @story-5-4
  Scenario: Chat history shows agent attribution
    Given an agent completes work
    When I view chat history
    Then each message should show agent avatar
    And each message should show agent name

  @epic-5 @story-5-4
  Scenario: Decisions show agent attribution
    Given a decision was made by an agent
    When I review the decision
    Then I should see "Decided by [AgentName] at [timestamp]"
    And I should see the reasoning

  @epic-5 @story-5-4
  Scenario: Handoffs are logged for audit
    Given handoffs occur during workflow
    When I query the audit log
    Then I should see all handoffs with fromAgent
    And I should see toAgent
    And I should see timestamp
    And I should see workflowStep
    And I should see reason
