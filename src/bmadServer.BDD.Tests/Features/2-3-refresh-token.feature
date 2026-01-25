Feature: Refresh Token Flow (Story 2-3)
  As a logged-in user
  I want to refresh my access token using a refresh token
  So that I can maintain my session without re-entering credentials

  Background:
    Given the API is running
    And a test user with email "existing@example.com" and password "SecurePass123!"

  @authentication @refresh-token
  Scenario: Refresh token issued on login
    When I login with email "existing@example.com" and password "SecurePass123!"
    Then I receive an access token
    And I receive a refresh token
    And the refresh token is stored in a secure HttpOnly cookie

  @authentication @refresh-token
  Scenario: Token refresh generates new access token
    When I login with email "existing@example.com" and password "SecurePass123!"
    And I use the refresh token to get a new access token
    Then I receive a new access token
    And the new access token is valid

  @authentication @refresh-token
  Scenario: Expired refresh token rejected
    When I login with email "existing@example.com" and password "SecurePass123!"
    And I wait for the refresh token to expire (more than 7 days)
    And I attempt to use the expired refresh token
    Then the response returns 401 Unauthorized

  @authentication @refresh-token
  Scenario: Revoked token rejected
    When I login with email "existing@example.com" and password "SecurePass123!"
    And I revoke the refresh token
    And I attempt to use the revoked refresh token
    Then the response returns 401 Unauthorized

  @authentication @refresh-token
  Scenario: Concurrent refresh requests handled safely
    When I login with email "existing@example.com" and password "SecurePass123!"
    And I initiate 5 concurrent refresh token requests
    Then exactly one request succeeds with a new access token
    And the other 4 requests fail with 401 Unauthorized
