Feature: User Registration (Story 2-1)
  As a new user
  I want to create an account with email and password
  So that I can securely access bmadServer

  Background:
    Given the API is running

  @authentication @registration
  Scenario: Valid user registration
    When I register with email "newuser@example.com" and password "SecurePass123!"
    Then the user is created successfully
    And the response returns 201 Created

  @authentication @registration
  Scenario: Duplicate email registration
    Given a test user with email "existing@example.com" and password "SecurePass123!"
    When I register with email "existing@example.com" and password "AnotherPass456!"
    Then the response returns 409 Conflict

  @authentication @registration
  Scenario: Invalid email format
    When I register with email "invalid-email" and password "SecurePass123!"
    Then the response returns 400 Bad Request

  @authentication @registration
  Scenario: Weak password
    When I register with email "test@example.com" and password "weak"
    Then the response returns 400 Bad Request

  @authentication @registration
  Scenario: Missing required fields
    When I register with email "" and password ""
    Then the response returns 400 Bad Request
