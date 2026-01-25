Feature: Human Approval for Low-Confidence Decisions
  As a user (Marcus)
  I want the system to pause for my approval when agents are uncertain
  So that I maintain control over important decisions

  @timeout:30000
  Scenario: Confidence below threshold requires approval
    Given an agent generates a response with confidence 0.65
    When I check if approval is required with threshold 0.7
    Then approval is required

  @timeout:30000
  Scenario: Confidence at or above threshold does not require approval
    Given an agent generates a response with confidence 0.75
    When I check if approval is required with threshold 0.7
    Then approval is not required

  @timeout:30000
  Scenario: Approval request created for low-confidence decision
    Given an agent generates a low-confidence response
    When the approval request is created
    Then the workflow transitions to WaitingForApproval state
    And the approval request has status "Pending"
    And the approval request includes proposed response
    And the approval request includes confidence score
    And the approval request includes reasoning

  @timeout:30000
  Scenario: User approves the decision
    Given an approval request exists with status "Pending"
    When I approve the decision with my userId
    Then the approval request status is "Approved"
    And the final response equals the proposed response
    And the approval is logged with my userId
    And the responded timestamp is set

  @timeout:30000
  Scenario: User modifies the decision
    Given an approval request exists with status "Pending"
    When I modify the proposed response and confirm
    Then the approval request status is "Modified"
    And the final response contains my modifications
    And both original and modified versions are logged
    And the approval is logged with my userId

  @timeout:30000
  Scenario: User rejects the decision
    Given an approval request exists with status "Pending"
    When I reject the decision with a reason
    Then the approval request status is "Rejected"
    And the rejection reason is logged
    And the approval is logged with my userId

  @timeout:30000
  Scenario: Cannot approve non-pending request
    Given an approval request exists with status "Approved"
    When I try to approve it again
    Then the approval fails
    And the status remains "Approved"

  @timeout:30000
  Scenario: Reminder needed after 24 hours
    Given an approval request was created 25 hours ago
    And no reminder has been sent
    When I query for pending requests needing reminders
    Then the approval request is included in the results

  @timeout:30000
  Scenario: No reminder needed before 24 hours
    Given an approval request was created 20 hours ago
    When I query for pending requests needing reminders
    Then the approval request is not included in the results

  @timeout:30000
  Scenario: Timeout warning after 72 hours
    Given an approval request was created 73 hours ago
    And the request is still pending
    When I query for timed out requests
    Then the approval request is included in the results

  @timeout:30000
  Scenario: Mark reminder as sent
    Given an approval request exists with status "Pending"
    When I mark the reminder as sent
    Then the LastReminderSentAt timestamp is set
    And the status remains "Pending"

  @timeout:30000
  Scenario: Timeout request after 72 hours
    Given an approval request exists with status "Pending"
    When I timeout the request
    Then the approval request status is "TimedOut"
    And the responded timestamp is set
