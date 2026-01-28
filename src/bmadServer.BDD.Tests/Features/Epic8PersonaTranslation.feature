Feature: Persona Translation & Language Adaptation
  As a user
  I want to configure my communication persona
  So that responses match my preferred style

  Background:
    Given I am authenticated as a valid user

  # Story 8.1: Persona Profile Configuration
  @epic-8 @story-8-1
  Scenario: Persona options are available in settings
    When I access persona settings
    Then I should see the option "Business"
    And I should see the option "Technical"
    And I should see the option "Hybrid" as adaptive

  @epic-8 @story-8-1
  Scenario: Select Business persona
    When I select Business persona
    And I save my preferences
    Then my user profile should include personaType "business"
    And the setting should persist across sessions

  @epic-8 @story-8-1
  Scenario: Select Technical persona
    When I select Technical persona
    And I save my preferences
    Then my user profile should include personaType "technical"
    And the setting should persist across sessions

  @epic-8 @story-8-1
  Scenario: Default persona is Hybrid
    Given I have not set a persona preference
    When I start using the system
    Then my default persona should be Hybrid
    And responses should adapt based on context

  @epic-8 @story-8-1
  Scenario: User profile includes persona information
    Given I have configured my persona
    When I send GET to "/api/v1/users/me"
    Then the response should include personaType
    And the response should include language preferences
