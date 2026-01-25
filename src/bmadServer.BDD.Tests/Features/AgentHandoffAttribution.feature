Feature: Agent Handoff & Attribution
  As a user (Sarah)
  I want to see when different agents take over
  So that I understand who is responsible for each part of the workflow

  @timeout:30000
  Scenario: Record agent handoff with all metadata
    Given a workflow instance exists
    When I record a handoff from "product-manager" to "architect" at step "design"
    Then the handoff should include fromAgent "product-manager"
    And the handoff should include toAgent "architect"
    And the handoff should include workflowStep "design"
    And the handoff should include agent names

  @timeout:30000
  Scenario: Initial agent assignment has no fromAgent
    Given a workflow instance exists
    When I record a handoff from initial to "product-manager" at step "requirements"
    Then the handoff should have null fromAgent
    And the handoff should include toAgent "product-manager"

  @timeout:30000
  Scenario: Get all handoffs for a workflow in chronological order
    Given a workflow instance with multiple handoffs
    When I query the handoffs for the workflow
    Then I should receive all handoffs in chronological order

  @timeout:30000
  Scenario: Get current agent for a workflow
    Given a workflow instance with multiple handoffs
    When I query the current agent
    Then I should receive the most recent agent

  @timeout:30000
  Scenario: Get agent details for tooltip display
    Given an agent "architect" exists in the registry
    When I request agent details for "architect"
    Then I should receive the agent name
    And I should receive the agent description
    And I should receive the agent capabilities
    And I should receive an avatar identifier

  @timeout:30000
  Scenario: Agent details include step responsibility when provided
    Given an agent "architect" exists in the registry
    When I request agent details for "architect" with step "create-architecture"
    Then the details should include current step responsibility

  @timeout:30000
  Scenario: Invalid agent handoff throws exception
    Given a workflow instance exists
    When I attempt to record a handoff to invalid agent "non-existent"
    Then the operation should throw InvalidOperationException
    And the error should mention agent not found
