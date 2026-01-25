Feature: Mobile-Responsive Chat Interface (Story 3-6)
  As a user on mobile
  I want the chat interface to work seamlessly on my phone
  So that I can approve decisions and monitor workflows on the go

  Background:
    Given I am accessing bmadServer
    And I am authenticated

  @ui @mobile @responsive
  Scenario: Mobile layout adapts below 768px
    When I access the chat on a mobile device (< 768px width)
    Then the layout adapts to single-column
    And the sidebar is collapsed to a hamburger menu
    And the chat takes full width
    And all content is readable without horizontal scroll

  @ui @mobile @input
  Scenario: Full-width input on mobile
    Given I am on a mobile device
    When I view the chat input area
    Then the input expands to full width
    And the Send button is at least 44px in size
    And all tap targets are at least 44px (WCAG 2.1 AA)

  @ui @mobile @input
  Scenario: Mobile input expands to 48px minimum
    Given I am on a mobile device
    When I interact with the chat input
    Then all touch targets are at least 48px
    And buttons are easily tappable
    And no accidental taps occur

  @ui @mobile @keyboard
  Scenario: Virtual keyboard handling
    Given I am on a mobile device
    When the virtual keyboard appears
    Then the chat scrolls to keep the input visible
    And the input stays fixed at the bottom
    And previous messages remain accessible above

  @ui @mobile @keyboard
  Scenario: Chat adjusts for keyboard height
    Given I am typing a message on mobile
    And the virtual keyboard is visible
    When I scroll the chat
    Then the visible area adjusts for keyboard height
    And I can still see recent messages
    And the input remains accessible

  @ui @mobile @gestures
  Scenario: Swipe down to refresh
    Given I am on a mobile device
    When I swipe down on the chat
    Then the chat refreshes
    And the latest messages are loaded
    And a refresh indicator is shown

  @ui @mobile @gestures
  Scenario: Tap and hold to copy message
    Given I am viewing a message on mobile
    When I tap and hold on the message
    Then a context menu appears
    And I can copy the message text
    And I can perform other message actions

  @ui @mobile @gestures
  Scenario: Swipe to dismiss notifications
    Given I receive a notification on mobile
    When I swipe the notification
    Then the notification is dismissed
    And it does not reappear

  @ui @mobile @accessibility @screenreader
  Scenario: VoiceOver support on iOS
    Given I am using VoiceOver on iOS
    When I navigate the chat interface
    Then all interactive elements are announced
    And gestures work with VoiceOver enabled
    And messages have proper ARIA labels
    And focus order is logical

  @ui @mobile @accessibility @screenreader
  Scenario: TalkBack support on Android
    Given I am using TalkBack on Android
    When I navigate the chat interface
    Then all interactive elements are announced
    And gestures work with TalkBack enabled
    And the chat is fully navigable by touch
    And proper content descriptions are provided

  @ui @mobile @accessibility @motion
  Scenario: Reduced motion preference
    Given I have enabled reduced motion preference
    When animations would normally play
    Then animations are disabled or significantly reduced
    And scroll behavior is instant instead of smooth
    And no motion-triggered discomfort occurs

  @ui @mobile @accessibility @motion
  Scenario: Respect prefers-reduced-motion CSS
    Given my system has "prefers-reduced-motion: reduce" set
    When I interact with the chat
    Then CSS animations respect the preference
    And transitions are instant or minimal
    And the interface remains fully functional

  @ui @mobile @viewport
  Scenario: Landscape orientation support
    Given I am on a mobile device
    When I rotate to landscape orientation
    Then the chat layout adjusts appropriately
    And the input remains accessible
    And content is not cut off
    And the interface remains usable

  @ui @mobile @performance
  Scenario: Smooth performance on mobile devices
    Given I am on a mobile device
    When I interact with the chat interface
    Then scrolling is smooth at 60fps
    And touch responses are immediate
    And the app feels native
    And memory usage is optimized
