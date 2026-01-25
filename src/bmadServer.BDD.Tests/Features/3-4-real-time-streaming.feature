Feature: Real-Time Message Streaming (Story 3-4)
  As a user
  I want to see agent responses stream in real-time
  So that I get immediate feedback and can follow long responses as they're generated

  Background:
    Given the API is running
    And I am connected to SignalR hub
    And I am authenticated

  @realtime @streaming @performance
  Scenario: Streaming starts within 5 seconds (NFR2)
    When I send a message to an agent
    Then streaming begins within 5 seconds
    And the first token appears on screen
    And the typing indicator is shown

  @realtime @streaming
  Scenario: Smooth token-by-token streaming
    Given I have sent a message to an agent
    And the agent is generating a response
    When tokens arrive via SignalR
    Then each token appends to the message smoothly
    And there is no flickering
    And the message updates in real-time

  @realtime @streaming @message-format
  Scenario: MESSAGE_CHUNK format validation
    When I receive a streaming message chunk
    Then the chunk has field "messageId"
    And the chunk has field "chunk" with text content
    And the chunk has field "isComplete" as boolean
    And the chunk has field "agentId"
    And the chunk has field "timestamp"

  @realtime @streaming
  Scenario: Complete streaming response
    Given the agent is streaming a response
    When the final chunk arrives with isComplete: true
    Then the typing indicator disappears
    And the full message displays with proper formatting
    And markdown is rendered correctly
    And the message is marked as complete

  @realtime @streaming @interruption
  Scenario: Streaming interrupted by network issues
    Given the agent is streaming a response
    When the SignalR connection drops mid-stream
    Then the partial message is preserved on screen
    And a reconnection attempt is made
    And streaming resumes from the last received chunk

  @realtime @streaming @interruption
  Scenario: Resume streaming after reconnection
    Given streaming was interrupted mid-response
    And the connection has been restored
    When I request to resume the message
    Then streaming continues from the last chunk index
    And no tokens are duplicated
    And the message completes successfully

  @realtime @streaming @cancellation
  Scenario: User cancels streaming response
    Given the agent is streaming a long response
    When I click the "Stop Generating" button
    Then streaming stops immediately
    And the partial message is preserved
    And a "(Stopped)" indicator is appended
    And the input field is re-enabled

  @realtime @streaming @cancellation
  Scenario: Cancel streaming via SignalR
    Given the agent is streaming a response with messageId "msg-123"
    When I invoke "StopGenerating" with messageId "msg-123"
    Then the server stops generating tokens
    And no more chunks are sent
    And the stream is marked as cancelled

  @realtime @streaming @multiple
  Scenario: Handle multiple concurrent streams
    Given I send two messages in quick succession
    When both agents start streaming responses
    Then each message streams to the correct message container
    And message IDs are tracked separately
    And chunks are not mixed between messages
