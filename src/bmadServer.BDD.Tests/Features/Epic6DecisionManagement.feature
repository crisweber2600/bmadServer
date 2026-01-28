Feature: Decision Management & Locking
  As a workflow participant
  I want decisions captured and stored
  So that I have a record of all choices made

  Background:
    Given I am authenticated as a valid user
    And I have an active workflow

  # Story 6.1: Decision Capture & Storage
  @epic-6 @story-6-1
  Scenario: Decision is captured when confirmed
    Given I am in a workflow step that requires a decision
    When I make a decision and confirm my choice
    Then a Decision record should be created
    And it should include id
    And it should include workflowInstanceId
    And it should include stepId
    And it should include decisionType
    And it should include value
    And it should include decidedBy
    And it should include decidedAt timestamp

  @epic-6 @story-6-1
  Scenario: Query all workflow decisions
    Given a workflow has multiple decisions recorded
    When I send GET to "/api/v1/workflows/:id/decisions"
    Then the response status should be 200 OK
    And I should receive all decisions in chronological order

  @epic-6 @story-6-1
  Scenario: Decision includes full context
    Given a decision was made in a workflow
    When I view decision details
    Then I should see the question asked
    And I should see the options presented
    And I should see the selected option
    And I should see the reasoning
    And I should see the context at time of decision

  @epic-6 @story-6-1
  Scenario: Structured data stored as validated JSON
    Given a decision involves structured data
    When the decision is captured
    Then the value should be stored as validated JSON
    And it should match the expected schema
    And JSONB columns should be properly indexed
