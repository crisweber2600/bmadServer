Feature: Chat Message Component with Ant Design (Story 3-2)
  As a user
  I want to see chat messages in a clean, readable format
  So that I can easily follow the conversation with BMAD agents

  Background:
    Given the React frontend is loaded
    And I am authenticated

  @ui @chat @messages
  Scenario: Display user message
    When a user message "Hello, BMAD!" is rendered
    Then the message is aligned to the right
    And the message has a blue background
    And the message shows a timestamp
    And the message does not show an avatar

  @ui @chat @messages
  Scenario: Display agent message
    When an agent message "Hello! How can I help?" is rendered
    Then the message is aligned to the left
    And the message has a gray background
    And the message shows a timestamp
    And the message shows the agent avatar

  @ui @chat @messages @markdown
  Scenario: Render markdown in messages
    When an agent message with markdown "Here is **bold** and `code`" is rendered
    Then the markdown is converted to HTML
    And bold text is displayed correctly
    And inline code has proper formatting

  @ui @chat @messages @markdown
  Scenario: Render code block with syntax highlighting
    When an agent message contains a code block:
      """
      ```javascript
      const greeting = "Hello";
      console.log(greeting);
      ```
      """
    Then the code block is syntax highlighted
    And the code block has proper formatting

  @ui @chat @messages @markdown
  Scenario: Render clickable links
    When an agent message contains a link "[GitHub](https://github.com)"
    Then the link is clickable
    And the link opens in a new tab
    And the link has proper ARIA attributes

  @ui @chat @messages @typing
  Scenario: Display typing indicator
    When an agent starts typing a response
    Then a typing indicator appears within 500ms
    And the indicator shows animated ellipsis
    And the indicator displays the agent name

  @ui @chat @messages @accessibility
  Scenario: Screen reader announces messages
    When a new message is received
    Then the message has proper ARIA labels
    And the message triggers a live region announcement
    And screen readers can navigate the message history

  @ui @chat @messages @scrolling
  Scenario: Auto-scroll on long message
    When a long message is received
    Then the chat container scrolls automatically
    And the scroll animation is smooth
    And the message is fully visible
