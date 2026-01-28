Feature: User Authentication & Session Management
  As a user
  I want secure authentication
  So that my data is protected

  Background:
    Given bmadServer API is running

  # Story 2.1: User Registration & Local Database Authentication
  @epic-2 @story-2-1 @security
  Scenario: Register new user with valid credentials
    Given no user exists with email "newuser@example.com"
    When I send POST to "/api/v1/auth/register" with:
      | email       | newuser@example.com |
      | password    | SecurePass123!      |
      | displayName | New User            |
    Then the response status should be 201 Created
    And a User record should be created in the database
    And the password should be hashed using bcrypt with cost 12

  @epic-2 @story-2-1
  Scenario: Reject duplicate email registration
    Given a user exists with email "existing@example.com"
    When I send POST to "/api/v1/auth/register" with:
      | email       | existing@example.com |
      | password    | SecurePass123!       |
      | displayName | Duplicate User       |
    Then the response status should be 409 Conflict
    And the error should indicate "user already exists"

  @epic-2 @story-2-1
  Scenario: Reject weak password registration
    When I send POST to "/api/v1/auth/register" with:
      | email       | weakpass@example.com |
      | password    | 123                  |
      | displayName | Weak Pass User       |
    Then the response status should be 400 Bad Request
    And the error should specify password requirements

  @epic-2 @story-2-1
  Scenario: Reject invalid email format
    When I send POST to "/api/v1/auth/register" with:
      | email       | invalid-email        |
      | password    | SecurePass123!       |
      | displayName | Invalid Email User   |
    Then the response status should be 400 Bad Request
    And the error should indicate invalid email format

  # Story 2.2: JWT Token Generation & Validation
  @epic-2 @story-2-2 @security
  Scenario: Login generates JWT token with correct expiry
    Given a user exists with email "testuser@example.com" and password "SecurePass123!"
    When I send POST to "/api/v1/auth/login" with:
      | email    | testuser@example.com |
      | password | SecurePass123!       |
    Then the response status should be 200 OK
    And the response should contain an accessToken
    And the JWT should expire in 15 minutes

  @epic-2 @story-2-2
  Scenario: Login with incorrect password fails
    Given a user exists with email "testuser@example.com" and password "SecurePass123!"
    When I send POST to "/api/v1/auth/login" with:
      | email    | testuser@example.com |
      | password | WrongPassword!       |
    Then the response status should be 401 Unauthorized
    And the error message should be "Invalid email or password"

  @epic-2 @story-2-2 @security
  Scenario: Protected endpoint requires valid JWT
    Given I have a valid JWT token
    When I send GET to "/api/v1/users/me" with the Authorization header
    Then the response status should be 200 OK
    And the response should contain my user profile

  @epic-2 @story-2-2
  Scenario: Expired JWT is rejected
    Given I have an expired JWT token
    When I send GET to "/api/v1/users/me" with the Authorization header
    Then the response status should be 401 Unauthorized
    And the error should indicate token expired

  @epic-2 @story-2-2
  Scenario: Malformed JWT is rejected
    Given I have a malformed JWT token
    When I send GET to "/api/v1/users/me" with the Authorization header
    Then the response status should be 401 Unauthorized
    And the error should indicate invalid token

  # Story 2.3: Refresh Token Flow with HttpOnly Cookies
  @epic-2 @story-2-3 @security
  Scenario: Login sets HttpOnly refresh token cookie
    Given a user exists with email "testuser@example.com" and password "SecurePass123!"
    When I successfully login
    Then a refresh token UUID should be generated
    And the token hash should be stored in RefreshTokens table
    And an HttpOnly cookie should be set with Secure and SameSite=Strict

  @epic-2 @story-2-3
  Scenario: Refresh token rotation works correctly
    Given I have a valid refresh token cookie
    And my access token is about to expire
    When I send POST to "/api/v1/auth/refresh"
    Then the response status should be 200 OK
    And a new access token should be returned
    And the refresh token should be rotated

  @epic-2 @story-2-3 @security
  Scenario: Reused refresh token revokes all user tokens
    Given I have a previously used refresh token
    When I send POST to "/api/v1/auth/refresh" with the old token
    Then the response status should be 401 Unauthorized
    And ALL user tokens should be revoked

  @epic-2 @story-2-3
  Scenario: Logout clears refresh token
    Given I am logged in with a valid session
    When I send POST to "/api/v1/auth/logout"
    Then the response status should be 204 No Content
    And the refresh token should be revoked
    And the cookie should be cleared

  # Story 2.4: Session Persistence & Recovery
  @epic-2 @story-2-4
  Scenario: SignalR connection creates session record
    Given I am authenticated
    When my SignalR connection establishes
    Then a Session record should be created
    And it should include UserId and ConnectionId
    And ExpiresAt should be set to 30 minutes from now

  @epic-2 @story-2-4
  Scenario: Session recovery within 60 seconds
    Given I have an active session
    And my network connection drops
    When I reconnect within 60 seconds
    Then the system should match my userId
    And validate that the session is not expired
    And associate the new ConnectionId
    And send SESSION_RESTORED SignalR message

  @epic-2 @story-2-4
  Scenario: Session recovery after 60 seconds creates new session
    Given I have an active session
    And I disconnect
    When I reconnect after 61 seconds
    Then a NEW session should be created
    But workflow state should be recovered
    And message "Session recovered from last checkpoint" should display

  @epic-2 @story-2-4 @concurrency
  Scenario: Optimistic concurrency prevents conflicts
    Given I have multiple active sessions on different devices
    When two devices update workflow state simultaneously
    Then the system should detect version mismatch
    And the second update should fail with 409 Conflict
