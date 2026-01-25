Feature: SignalR Hub Setup & WebSocket Connection (Story 3-1)
  As a user
  I want to establish a persistent WebSocket connection to bmadServer
  So that I can receive real-time updates and send messages without page refreshes

  Background:
    Given the API is running
    And I have a valid JWT token

  @realtime @signalr @websocket
  Scenario: Establish WebSocket connection with valid JWT
    When I connect to SignalR hub "/chathub" with JWT token
    Then the connection is established successfully
    And OnConnectedAsync is called on the server
    And the connection ID is logged

  @realtime @signalr @websocket
  Scenario: Send message via SignalR
    Given I am connected to the SignalR hub
    When I invoke "SendMessage" with message "Hello from test"
    Then the server receives the message within 100ms
    And the message is acknowledged within 2 seconds

  @realtime @signalr @websocket
  Scenario: Join workflow room
    Given I am connected to the SignalR hub
    When I invoke "JoinWorkflow" with workflowId "test-workflow-123"
    Then I am added to the workflow group
    And I can receive workflow-specific messages

  @realtime @signalr @websocket
  Scenario: Leave workflow room
    Given I am connected to the SignalR hub
    And I have joined workflow "test-workflow-123"
    When I invoke "LeaveWorkflow" with workflowId "test-workflow-123"
    Then I am removed from the workflow group

  @realtime @signalr @reconnection
  Scenario: Automatic reconnection after connection drop
    Given I am connected to the SignalR hub
    When the WebSocket connection drops unexpectedly
    Then SignalR attempts reconnection with exponential backoff
    And the connection is re-established
    And session recovery flow executes

  @realtime @signalr @authentication
  Scenario: Connection rejected with invalid JWT
    When I attempt to connect to SignalR hub with invalid JWT token
    Then the connection is rejected
    And I receive an authentication error

  @realtime @signalr @authentication
  Scenario: Connection rejected without JWT
    When I attempt to connect to SignalR hub without JWT token
    Then the connection is rejected
    And I receive an authentication error
