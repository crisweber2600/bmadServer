Feature: Idle Timeout & Session Extension (Story 2-6)
  As a user
  I want to be notified when my session is about to expire due to inactivity
  So that I can extend my session without losing work

  Background:
    Given the API is running
    And a test user with email "existing@example.com" and password "SecurePass123!"

  @authentication @idle-timeout @session
  Scenario: Idle timeout warning after 25 minutes
    When I login with email "existing@example.com" and password "SecurePass123!"
    And I remain inactive for 25 minutes
    Then a timeout warning modal appears
    And the modal indicates 5 minutes remain before session expires

  @authentication @idle-timeout @session
  Scenario: Session extended after user interaction
    When I login with email "existing@example.com" and password "SecurePass123!"
    And I remain inactive for 25 minutes
    And I see the timeout warning modal
    And I click the "Extend Session" button
    Then the session is extended for another 30 minutes
    And the warning modal closes

  @authentication @idle-timeout @session
  Scenario: Session expires after 30 minutes of inactivity
    When I login with email "existing@example.com" and password "SecurePass123!"
    And I remain inactive for 30 minutes
    And the timeout warning modal appeared at 25 minutes
    And I did not click "Extend Session"
    Then the session is terminated
    And I am logged out automatically
    And a login page is displayed

  @authentication @idle-timeout @session
  Scenario: Activity resets idle timer
    When I login with email "existing@example.com" and password "SecurePass123!"
    And I remain inactive for 20 minutes
    And I interact with the application
    Then the idle timer is reset
    And the timeout countdown is 30 minutes from the interaction

  @authentication @idle-timeout @session
  Scenario: Countdown timer displayed in warning modal
    When I login with email "existing@example.com" and password "SecurePass123!"
    And I remain inactive for 25 minutes
    And the timeout warning modal appears
    Then a countdown timer is displayed
    And the timer decrements every second
    And the timer reaches 0 when session expires
