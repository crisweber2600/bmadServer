Feature: Chat Input Component with Rich Interactions (Story 3-3)
  As a user
  I want a responsive input field with helpful features
  So that I can communicate effectively with BMAD agents

  Background:
    Given the chat interface is loaded
    And I am authenticated

  @ui @chat @input
  Scenario: Display multi-line input field
    When I view the chat input area
    Then I see a multi-line text input
    And I see a Send button that is disabled
    And I see a character count display
    And I see a keyboard shortcut hint "Ctrl+Enter to send"

  @ui @chat @input @keyboard
  Scenario: Send message with Ctrl+Enter
    Given I have typed a message "Hello BMAD"
    When I press Ctrl+Enter
    Then the message is sent immediately
    And the input field is cleared
    And focus remains on the input field

  @ui @chat @input @keyboard
  Scenario: Send message with Cmd+Enter on Mac
    Given I have typed a message "Hello BMAD"
    When I press Cmd+Enter
    Then the message is sent immediately
    And the input field is cleared

  @ui @chat @input @validation
  Scenario: Character limit warning
    Given I have typed 1999 characters
    When I type one more character
    Then the character count shows "2000 / 2000"
    And the character count is displayed normally

  @ui @chat @input @validation
  Scenario: Character limit exceeded
    Given I have typed 2000 characters
    When I attempt to type more characters
    Then the character count turns red
    And the Send button becomes disabled
    And I see "2001 / 2000" in red

  @ui @chat @input @draft
  Scenario: Draft message persistence
    Given I have typed a partial message "This is a draft"
    When I navigate away from the chat
    And I return to the chat
    Then my draft message "This is a draft" is restored
    And the character count reflects the draft length

  @ui @chat @input @draft
  Scenario: Draft cleared after sending
    Given I have a saved draft message
    When I send the message
    Then the draft is removed from localStorage
    And the input field is empty

  @ui @chat @input @commands
  Scenario: Command palette appears on slash
    Given the input field is focused
    When I type "/"
    Then the command palette appears
    And I see options: /help, /status, /pause, /resume

  @ui @chat @input @commands
  Scenario: Navigate command palette with keyboard
    Given the command palette is open
    When I press the down arrow key
    Then the next command is highlighted
    When I press the up arrow key
    Then the previous command is highlighted
    When I press Enter
    Then the selected command is executed

  @ui @chat @input @commands
  Scenario: Command palette closes on Escape
    Given the command palette is open
    When I press Escape
    Then the command palette closes
    And focus returns to the input field

  @ui @chat @input @cancellation
  Scenario: Cancel button during processing
    Given I have sent a message
    And the server is processing (>5 seconds)
    When I see the processing indicator
    Then I can click a "Cancel" button
    And the request is aborted
    And the processing indicator disappears

  @ui @chat @input @accessibility
  Scenario: Keyboard navigation accessibility
    When I navigate to the chat input with Tab
    Then the input field receives focus
    And all interactive elements are keyboard accessible
    And focus indicators are visible
