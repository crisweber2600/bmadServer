@epic-3 @ui
Feature: Real-Time Chat Interface
  As a user
  I want a real-time chat interface
  So that I can communicate with AI agents seamlessly

  Background:
    Given I am authenticated
    And I am on the chat page

  # Story 3.1: SignalR Hub Setup & WebSocket Connection
  @story-3-1 @signalr
  Scenario: WebSocket connection establishes with valid JWT
    Given I have a valid JWT token
    When I connect to the SignalR hub with accessTokenFactory
    Then a WebSocket connection should be established
    And OnConnectedAsync should be called on the server

  @story-3-1 @signalr @performance
  Scenario: Message transmission completes within 100ms
    Given I have an active SignalR connection
    When I send a message through the hub
    Then the message should be received within 100 milliseconds

  @story-3-1 @signalr @resilience
  Scenario: Automatic reconnection with exponential backoff
    Given I have an active SignalR connection
    When the connection drops unexpectedly
    Then reconnection should be attempted at 0s, 2s, 10s, 30s intervals
    And the connection should recover automatically

  @story-3-1 @signalr @session
  Scenario: Session recovery on reconnect
    Given I have an active session with workflow state
    And my connection drops
    When I reconnect successfully
    Then my session should be recovered
    And workflow state should be preserved

  # Story 3.2: Chat Message Component with Ant Design
  @story-3-2 @ui @layout
  Scenario: User messages display on the right with blue background
    Given I have sent a message
    When the message renders in the chat
    Then it should be aligned to the right
    And it should have a blue background color

  @story-3-2 @ui @layout
  Scenario: Agent messages display on the left with gray background
    Given an agent has responded
    When the message renders in the chat
    Then it should be aligned to the left
    And it should have a gray background color

  @story-3-2 @ui @markdown
  Scenario: Markdown content renders as HTML
    Given a message contains markdown formatting
    When the message renders
    Then markdown should be converted to HTML
    And code blocks should have syntax highlighting

  @story-3-2 @ui @accessibility
  Scenario: Messages have proper ARIA labels
    Given messages are displayed in chat
    Then each message should have an aria-label
    And new messages should update an aria-live region

  # Story 3.3: Chat Input Component with Rich Interactions
  @story-3-3 @ui @input
  Scenario: Multi-line input with Send button
    Given I am viewing the chat input
    Then I should see a multi-line text input
    And I should see a Send button

  @story-3-3 @ui @input @validation
  Scenario: Send button is disabled when input is empty
    Given the message input is empty
    Then the Send button should be disabled

  @story-3-3 @ui @input @keyboard
  Scenario: Ctrl+Enter sends message
    Given I have typed a message
    When I press Ctrl+Enter
    Then the message should be sent
    And the input should be cleared

  @story-3-3 @ui @input @validation
  Scenario: Character count indicator at 2000+ characters
    Given I have typed more than 2000 characters
    Then the character count should turn red
    And the Send button should remain enabled

  @story-3-3 @ui @commands
  Scenario: Slash commands show command palette
    Given I am in the chat input
    When I type "/"
    Then a command palette should appear
    And it should show /help, /status, /pause, /resume options

  # Story 3.4: Real-Time Message Streaming
  @story-3-4 @streaming @performance
  Scenario: Streaming starts within 5 seconds
    Given I have sent a message
    When the agent begins responding
    Then streaming should start within 5 seconds

  @story-3-4 @streaming @ui
  Scenario: Tokens append smoothly without flickering
    Given streaming is in progress
    When new tokens arrive
    Then they should append without visual flickering

  @story-3-4 @streaming @protocol
  Scenario: MESSAGE_CHUNK format is validated
    Given I receive a streaming chunk
    Then it should contain messageId, chunk, isComplete, and agentId

  @story-3-4 @streaming @controls
  Scenario: Stop Generating button stops streaming
    Given streaming is in progress
    When I click "Stop Generating"
    Then streaming should stop
    And "(Stopped)" indicator should appear

  # Story 3.5: Chat History & Scroll Management
  @story-3-5 @history @pagination
  Scenario: Last 50 messages load at page bottom
    Given a chat has more than 50 messages
    When I open the chat
    Then the last 50 messages should be visible
    And the view should scroll to the bottom

  @story-3-5 @history @pagination
  Scenario: Load More button loads older messages
    Given I am viewing the chat history
    When I scroll to the top and click "Load More"
    Then 50 additional messages should load
    And scroll position should be maintained

  @story-3-5 @history @notifications
  Scenario: New message badge when scrolled up
    Given I am scrolled up viewing older messages
    When a new message arrives
    Then a "New Message" badge should appear

  @story-3-5 @history @empty-state
  Scenario: Empty chat shows welcome message
    Given this is a new chat with no messages
    When I view the chat
    Then I should see a welcome message
    And quick-start action buttons should be visible

  # Story 3.6: Mobile-Responsive Chat Interface
  @story-3-6 @mobile @layout
  Scenario: Single-column layout on mobile devices
    Given I am viewing on a device with width less than 768px
    Then the layout should be single-column
    And a hamburger menu should be visible for the sidebar

  @story-3-6 @mobile @accessibility
  Scenario: Touch targets meet accessibility requirements
    Given I am on a mobile device
    Then all interactive elements should be at least 44px
    And sufficient spacing should exist between targets

  @story-3-6 @mobile @keyboard
  Scenario: Virtual keyboard does not hide input
    Given I am on a mobile device
    When the virtual keyboard opens
    Then the chat input should remain visible
    And it should stick to the bottom above the keyboard

  @story-3-6 @mobile @accessibility
  Scenario: VoiceOver announces elements correctly
    Given VoiceOver is enabled
    When navigating the chat interface
    Then all elements should be properly announced
