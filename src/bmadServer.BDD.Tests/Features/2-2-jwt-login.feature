Feature: JWT Token Generation & Login (Story 2-2)
  As a registered user
  I want to login with email and password
  So that I can receive a JWT token to access protected resources

  Background:
    Given the API is running
    And a test user with email "existing@example.com" and password "SecurePass123!"

  @authentication @login @jwt
  Scenario: Valid login generates JWT token
    When I login with email "existing@example.com" and password "SecurePass123!"
    Then I receive an access token
    And the access token is a valid JWT
    And the JWT contains user email "existing@example.com"

  @authentication @login @jwt
  Scenario: Invalid password rejected
    When I login with email "existing@example.com" and password "WrongPassword123!"
    Then the response returns 401 Unauthorized
    And I do not receive an access token

  @authentication @login @jwt
  Scenario: Non-existent email rejected
    When I login with email "nonexistent@example.com" and password "SecurePass123!"
    Then the response returns 401 Unauthorized
    And I do not receive an access token

  @authentication @login @jwt
  Scenario: JWT token expires after 15 minutes
    When I login with email "existing@example.com" and password "SecurePass123!"
    Then I receive an access token
    And the JWT token expires in 15 minutes

  @authentication @login @jwt
  Scenario: Tampered JWT token is rejected
    When I login with email "existing@example.com" and password "SecurePass123!"
    And I modify the JWT token by changing the payload
    And I attempt to use the modified token
    Then the response returns 401 Unauthorized
