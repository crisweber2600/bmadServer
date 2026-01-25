Feature: Chat History & Scroll Management (Story 3-5)
  As a user
  I want to review previous messages in our conversation
  So that I can reference earlier context and decisions

  Background:
    Given the chat interface is loaded
    And I am authenticated
    And I have a workflow with chat history

  @ui @chat @history
  Scenario: Load last 50 messages on chat open
    When I open a workflow chat
    Then the last 50 messages are displayed
    And the scroll position is at the bottom
    And the most recent message is visible

  @ui @chat @history @pagination
  Scenario: Load more messages with pagination
    Given I am viewing a chat with more than 50 messages
    When I scroll to the top of the chat
    Then I see a "Load More" button
    When I click "Load More"
    Then the next 50 messages load
    And my scroll position does not jump
    And the previously visible message remains in view

  @ui @chat @history @pagination
  Scenario: No more messages to load
    Given I am viewing a chat with 30 total messages
    When all messages are loaded
    Then the "Load More" button is not displayed
    And I see a "Beginning of conversation" indicator

  @ui @chat @history @new-messages
  Scenario: New message badge when scrolled up
    Given I am scrolled up reading old messages
    When a new message arrives
    Then a "New message" badge appears at the bottom
    And my scroll position is not disrupted
    And the badge shows the count of unread messages

  @ui @chat @history @new-messages
  Scenario: Badge disappears when scrolling to bottom
    Given I am scrolled up with a "New message" badge visible
    When I scroll to the bottom
    Then the "New message" badge disappears
    And the unread count resets to 0

  @ui @chat @history @new-messages
  Scenario: Click badge to scroll to bottom
    Given I am scrolled up with new messages
    When I click the "New message" badge
    Then the chat scrolls smoothly to the bottom
    And the latest message is visible
    And the badge disappears

  @ui @chat @history @persistence
  Scenario: Restore scroll position on reload
    Given I am viewing messages at scroll position 500px
    When I close and reopen the chat
    Then my scroll position is restored to 500px
    And the same messages are visible
    And the scroll position is retrieved from sessionStorage

  @ui @chat @history @persistence
  Scenario: Clear scroll position when starting new workflow
    Given I have a saved scroll position for workflow "old-123"
    When I start a new workflow "new-456"
    Then the scroll position starts at the bottom
    And the old scroll position is not applied

  @ui @chat @history @empty
  Scenario: Welcome message for new workflow
    Given I open a new workflow with no chat history
    Then I see a welcome message "Welcome to BMAD Server! 👋"
    And I see text "Start a conversation to begin your workflow journey"
    And I see a "Quick Start" button
    And no "Load More" button is visible

  @ui @chat @history @empty
  Scenario: Quick start button initiates workflow
    Given I see the welcome message for an empty chat
    When I click the "Quick Start" button
    Then a sample workflow message is sent
    And the welcome message is replaced with chat messages

  @ui @chat @history @performance
  Scenario: Smooth scrolling performance
    Given the chat has 200+ messages loaded
    When I scroll through the messages
    Then scrolling is smooth without lag
    And messages render efficiently
    And no memory leaks occur
