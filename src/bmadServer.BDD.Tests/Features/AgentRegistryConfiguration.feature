Feature: Agent Registry & Configuration
  As a developer
  I want a centralized agent registry
  So that the system knows all available agents and their capabilities

  @timeout:30000
  Scenario: AgentDefinition includes required properties
    Given I need to define BMAD agents
    When I create an AgentDefinition
    Then it includes AgentId property
    And it includes Name property
    And it includes Description property
    And it includes Capabilities list property
    And it includes SystemPrompt property
    And it includes ModelPreference property

  @timeout:30000
  Scenario: AgentRegistry provides all required methods
    Given agent definitions exist
    When I create an AgentRegistry
    Then it provides GetAllAgents method
    And it provides GetAgent by id method
    And it provides GetAgentsByCapability method

  @timeout:30000
  Scenario: Registry is populated with all BMAD agents
    Given the registry is populated
    When I query GetAllAgents
    Then I receive ProductManager agent
    And I receive Architect agent
    And I receive Designer agent
    And I receive Developer agent
    And I receive Analyst agent
    And I receive Orchestrator agent

  @timeout:30000
  Scenario: Each agent has capabilities mapped to workflow steps
    Given each agent has capabilities
    When I examine the Architect agent definition
    Then it has capability "create-architecture"
    And capabilities map to workflow steps they can handle

  @timeout:30000
  Scenario: Agents have model preferences
    Given agents have model preferences
    When I query an agent from the registry
    Then the agent has a configured ModelPreference
    And the system can route to the preferred model

  @timeout:30000
  Scenario: Get agent by specific ID
    Given the registry is populated
    When I query GetAgent with "architect" id
    Then I receive the Architect agent
    And the agent has the correct name "Architect"

  @timeout:30000
  Scenario: Get agents by capability
    Given the registry is populated
    When I query GetAgentsByCapability with "create-architecture"
    Then I receive agents that have this capability
    And the Architect agent is in the results
