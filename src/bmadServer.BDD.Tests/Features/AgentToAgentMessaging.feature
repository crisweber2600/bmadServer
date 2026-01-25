Feature: Agent-to-Agent Messaging
  As an agent (Architect)
  I want to request information from other agents
  So that I can gather inputs needed for my work

  @timeout:30000
  Scenario: Agent can request information from another agent
    Given an agent "architect" is processing a step
    When it needs input from agent "product-manager"
    Then it can call RequestFromAgent with targetAgentId, request, and context

  @timeout:30000
  Scenario: Agent request includes all required fields
    Given an agent request is made from "architect" to "product-manager"
    When the target agent receives the request
    Then the request includes sourceAgentId
    And the request includes requestType
    And the request includes payload
    And the request includes workflowContext
    And the request includes conversationHistory

  @timeout:30000
  Scenario: Agent receives response from target agent
    Given an agent request is sent from "architect" to "product-manager"
    When the target agent processes the request
    Then a response is generated
    And the response is returned to the source agent
    And the exchange is logged for transparency

  @timeout:30000
  Scenario: Message format includes all required metadata
    Given agent-to-agent communication occurs
    When I check the message format
    Then the message includes messageId
    And the message includes timestamp
    And the message includes sourceAgent
    And the message includes targetAgent
    And the message includes messageType
    And the message includes content
    And the message includes workflowInstanceId

  @timeout:30000
  Scenario: Agent request times out after 30 seconds
    Given an agent request is made with a 30 second timeout
    When no response is received after 30 seconds
    Then the system retries once
    And if still no response, returns error to source agent
    And the timeout is logged for debugging

  @timeout:30000
  Scenario: Successful agent-to-agent request without timeout
    Given an agent "architect" needs information from "product-manager"
    When it sends a request with valid parameters
    Then the request completes successfully within timeout
    And a valid response is returned
    And no retry is needed
