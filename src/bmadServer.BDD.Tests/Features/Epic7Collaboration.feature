Feature: Collaboration & Multi-User Support
  As a workflow owner
  I want to invite collaborators
  So that others can participate in the workflow

  Background:
    Given I am authenticated as a workflow owner
    And I have an active workflow

  # Story 7.1: Multi-User Workflow Participation
  @epic-7 @story-7-1
  Scenario: Invite user as Contributor
    Given another user exists with userId "contributor-user-id"
    When I send POST to "/api/v1/workflows/{id}/participants" with:
      | userId | contributor-user-id |
      | role   | Contributor         |
    Then the response status should be 201 Created
    And the user should be added to participants list
    And they should receive an invitation notification

  @epic-7 @story-7-1
  Scenario: Contributor can interact with workflow
    Given a user is added as Contributor to my workflow
    When they access the workflow
    Then they should be able to send messages
    And they should be able to make decisions
    And they should be able to advance steps
    And their actions should be attributed to them

  @epic-7 @story-7-1
  Scenario: Observer has read-only access
    Given a user is added as Observer to my workflow
    When they access the workflow
    Then they should be able to view messages and decisions
    But they should not be able to make changes
    And they should not be able to send messages
    And UI should show read-only mode

  @epic-7 @story-7-1
  Scenario: Presence indicators show online users
    Given multiple users are connected to the workflow
    When I view the workflow
    Then I should see presence indicators for online users
    And I should see typing indicators when others compose

  @epic-7 @story-7-1
  Scenario: Remove participant revokes access
    Given a user is a participant in my workflow
    When I send DELETE to "/api/v1/workflows/{id}/participants/{userId}"
    Then the response status should be 204 No Content
    And the user should lose access immediately
    And they should receive a notification
    And future access should be denied
