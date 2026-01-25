Feature: Role-Based Access Control (Story 2-5)
  As a system administrator
  I want to assign roles and control access
  So that users have appropriate permissions based on their responsibilities

  Background:
    Given the API is running
    And a test user with email "admin@example.com" assigned role "Admin"
    And a test user with email "participant@example.com" assigned role "Participant"
    And a test user with email "viewer@example.com" assigned role "Viewer"

  @authentication @rbac @authorization
  Scenario: Admin role has full access
    When I login with email "admin@example.com" and password "SecurePass123!"
    And I access the admin management endpoint
    Then the response returns 200 OK
    And I can create, read, update, delete resources

  @authentication @rbac @authorization
  Scenario: Participant role has limited access
    When I login with email "participant@example.com" and password "SecurePass123!"
    And I attempt to access the admin management endpoint
    Then the response returns 403 Forbidden
    And I can only access participant endpoints

  @authentication @rbac @authorization
  Scenario: Viewer role has read-only access
    When I login with email "viewer@example.com" and password "SecurePass123!"
    And I attempt to modify a resource
    Then the response returns 403 Forbidden
    And I can read resources

  @authentication @rbac @authorization
  Scenario: Unauthorized role rejection
    When I login with email "participant@example.com" and password "SecurePass123!"
    And I attempt to delete a resource
    Then the response returns 403 Forbidden

  @authentication @rbac @authorization
  Scenario: Role assignment persisted
    When I assign role "Admin" to email "newuser@example.com"
    And I verify the assignment in the database
    Then the user has role "Admin" persisted
    And the role persists across sessions
