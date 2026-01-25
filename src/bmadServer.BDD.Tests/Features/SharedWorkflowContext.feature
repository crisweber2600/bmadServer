Feature: Shared Workflow Context
  As an agent (Designer)
  I want access to the full workflow context
  So that I can make decisions informed by previous steps

  @timeout:30000
  Scenario: SharedContext contains all required components
    Given a workflow has multiple completed steps
    When an agent receives a request
    Then SharedContext contains all step outputs
    And SharedContext contains decision history
    And SharedContext contains user preferences
    And SharedContext contains artifact references

  @timeout:30000
  Scenario: GetStepOutput returns structured output for completed step
    Given a workflow step "gather-requirements" has completed
    And the step produced output data
    When an agent queries GetStepOutput with "gather-requirements"
    Then it receives the structured output from that step
    And the output matches the original step output

  @timeout:30000
  Scenario: GetStepOutput returns null for incomplete step
    Given a workflow step "create-architecture" has not completed
    When an agent queries GetStepOutput with "create-architecture"
    Then it receives null
    And no exception is thrown

  @timeout:30000
  Scenario: Output is automatically added when step completes
    Given a workflow step is executing
    When the step completes with output
    Then the output is automatically added to SharedContext
    And subsequent agents can access it immediately
    And the context version is incremented

  @timeout:30000
  Scenario: Context summarization when exceeding token limits
    Given context size grows large
    When the context exceeds 8000 tokens
    Then the system summarizes older context
    And key decisions are preserved in summary
    And full context remains available in database

  @timeout:30000
  Scenario: Optimistic concurrency control prevents conflicts
    Given concurrent agents access context
    When simultaneous writes occur
    Then optimistic concurrency control prevents conflicts
    And version numbers track context changes
    And the first write succeeds
    And the second write detects version mismatch

  @timeout:30000
  Scenario: Context tracks all step outputs chronologically
    Given multiple steps complete in sequence
    When I query the SharedContext
    Then all step outputs are available
    And they are ordered chronologically
    And each has a timestamp

  @timeout:30000
  Scenario: Context includes user preferences
    Given a user has set preferences
    When an agent accesses SharedContext
    Then user preferences are available
    And preferences include display settings
    And preferences include model preferences

  @timeout:30000
  Scenario: Context includes decision history
    Given multiple decisions have been made
    When an agent accesses SharedContext
    Then decision history is available
    And each decision includes who made it
    And each decision includes when it was made
    And each decision includes the rationale

  @timeout:30000
  Scenario: Context includes artifact references
    Given artifacts have been created during workflow
    When an agent accesses SharedContext
    Then artifact references are available
    And each reference includes artifact type
    And each reference includes storage location
    And each reference includes creation timestamp
