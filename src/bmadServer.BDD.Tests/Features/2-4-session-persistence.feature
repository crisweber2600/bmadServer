Feature: Session Persistence (Story 2-4)
  As a user
  I want my session to be saved and recovered
  So that I can resume work even after a connection loss or browser crash

  Background:
    Given the API is running
    And a test user with email "existing@example.com" and password "SecurePass123!"

  @authentication @session @persistence
  Scenario: Session state persisted in database
    When I login with email "existing@example.com" and password "SecurePass123!"
    And I access protected endpoints to establish session state
    Then the session is stored in the database with user data

  @authentication @session @persistence
  Scenario: Session recovered after connection loss
    When I login with email "existing@example.com" and password "SecurePass123!"
    And I establish a session with state
    And my connection is lost
    And I reconnect with the same refresh token
    Then my session is recovered with the previous state
    And no re-authentication is required

  @authentication @session @persistence
  Scenario: Multi-device sessions tracked independently
    When I login with email "existing@example.com" and password "SecurePass123!" from device A
    And I login with email "existing@example.com" and password "SecurePass123!" from device B
    Then I have 2 active sessions in the database
    And each session has its own unique refresh token
    And actions on device A do not affect device B session

  @authentication @session @persistence
  Scenario: Session recovery within 60 seconds
    When I login with email "existing@example.com" and password "SecurePass123!"
    And I establish a session with state
    And the connection is lost for 45 seconds
    Then the session can be recovered within the 60-second window
    And the session state is fully preserved

  @authentication @session @persistence
  Scenario: Session expired after 60 seconds of no recovery
    When I login with email "existing@example.com" and password "SecurePass123!"
    And I establish a session with state
    And the connection is lost for 65 seconds
    Then the session is marked as expired
    And re-authentication is required
