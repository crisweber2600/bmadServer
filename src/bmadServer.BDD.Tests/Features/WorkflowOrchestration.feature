Feature: Workflow Orchestration Engine
  As a user
  I want to manage workflow execution
  So that I can orchestrate multi-step BMAD processes

  Background:
    Given the workflow registry is initialized
    And I am authenticated as a valid user

  # Story 4-1: Workflow Definition & Registry
  @workflow @story-4-1
  Scenario: Workflow registry contains BMAD workflows
    When I query all available workflows
    Then the registry should contain at least 6 workflows
    And the workflows should include "create-prd"
    And the workflows should include "create-architecture"
    And the workflows should include "create-stories"
    And the workflows should include "design-ux"
    And the workflows should include "dev-story"
    And the workflows should include "code-review"

  @workflow @story-4-1
  Scenario: Get specific workflow by ID
    When I query workflow "create-prd"
    Then the workflow should exist
    And the workflow should have a name
    And the workflow should have a description
    And the workflow should have steps

  @workflow @story-4-1
  Scenario: Workflow validation
    When I validate workflow "create-prd"
    Then the validation should succeed
    And all steps should have valid agent IDs

  # Story 4-2: Workflow Instance Creation & State Machine
  @workflow @story-4-2
  Scenario: Create a new workflow instance
    Given I have a valid workflow ID "create-prd"
    When I create a new workflow instance
    Then the workflow instance should be created
    And the status should be "Created"
    And the instance should have a unique ID
    And the instance should be associated with my user

  @workflow @story-4-2
  Scenario: Start a workflow instance
    Given I have created a workflow instance for "create-prd"
    When I start the workflow instance
    Then the status should transition to "Running"
    And the current step should be set to the first step
    And a workflow event should be logged

  @workflow @story-4-2
  Scenario: Invalid state transition rejected
    Given I have a completed workflow instance
    When I try to start the workflow instance
    Then the request should fail with 400 Bad Request
    And the error should indicate state transition invalid state transition

  @workflow @story-4-2
  Scenario: Workflow state machine follows valid transitions
    Given I have created a workflow instance
    When I start the workflow
    Then valid transitions should be: "Running", "Paused", "WaitingForInput", "Completed", "Failed", "Cancelled"
    And invalid transitions from "Created" should be rejected

  # Story 4-3: Step Execution & Agent Routing
  @workflow @story-4-3
  Scenario: Execute workflow step
    Given I have a running workflow instance
    When I execute the current step
    Then the step should route to the correct agent
    And step history should be created
    And the step output should be stored

  @workflow @story-4-3
  Scenario: Step execution with error handling
    Given I have a running workflow instance
    When step execution fails
    Then the error should be logged
    And the workflow status should transition to "Failed"
    And the error details should be preserved

  # Story 4-4: Workflow Pause & Resume
  @workflow @story-4-4
  Scenario: Pause a running workflow
    Given I have a running workflow instance
    When I pause the workflow
    Then the status should transition to "Paused"
    And the PausedAt timestamp should be set
    And a pause event should be logged

  @workflow @story-4-4
  Scenario: Resume a paused workflow
    Given I have a paused workflow instance
    When I resume the workflow
    Then the status should transition to "Running"
    And a resume event should be logged

  @workflow @story-4-4
  Scenario: Cannot resume a cancelled workflow
    Given I have a cancelled workflow instance
    When I try to resume the workflow
    Then the request should fail with 400 Bad Request
    And the error should indicate state transition Cannot resume a cancelled

  # Story 4-5: Workflow Exit & Cancellation
  @workflow @story-4-5
  Scenario: Cancel a running workflow
    Given I have a running workflow instance
    When I cancel the workflow
    Then the status should transition to "Cancelled"
    And the CancelledAt timestamp should be set
    And a cancellation event should be logged

  @workflow @story-4-5
  Scenario: Cancel a paused workflow
    Given I have a paused workflow instance
    When I cancel the workflow
    Then the status should transition to "Cancelled"
    And the workflow should remain in database for audit

  @workflow @story-4-5
  Scenario: Cannot cancel a completed workflow
    Given I have a completed workflow instance
    When I try to cancel the workflow
    Then the request should fail with 400 Bad Request
    And the error should indicate state transition Cannot cancel a completed

  @workflow @story-4-5
  Scenario: Filter cancelled workflows
    Given I have multiple workflow instances including cancelled ones
    When I query workflows with showCancelled=false
    Then cancelled workflows should be excluded
    When I query workflows with showCancelled=true
    Then cancelled workflows should be included

  # Story 4-6: Workflow Step Navigation & Skip
  # Note: Skip functionality requires workflows with optional/skippable steps
  # The create-architecture workflow has an optional 3rd step (arch-3)
  @workflow @story-4-6 @skip
  Scenario: Skip an optional step
    Given I have a running workflow instance
    And the current step is optional
    And the current step can be skipped
    When I skip the current step with reason "Not needed"
    Then the step should be marked as skipped
    And the skip reason should be recorded
    And the workflow should advance to the next step

  @workflow @story-4-6
  Scenario: Cannot skip a required step
    Given I have a running workflow instance
    And the current step is required
    When I try to skip the current step
    Then the request should fail with 400 Bad Request
    And the error should indicate state transition This step is required

  @workflow @story-4-6
  Scenario: Cannot skip when CanSkip is false
    Given I have a running workflow instance
    And the current step is optional
    But the current step has CanSkip set to false
    When I try to skip the current step
    Then the request should fail with 400 Bad Request
    And the error should indicate state transition This step is required

  @workflow @story-4-6 @skip
  Scenario: Navigate to a previous step
    Given I have a running workflow instance
    And I have completed step 2
    And I am now on step 3
    When I navigate to step 2
    Then the current step should be set to step 2
    And the previous step output should be available
    And a step revisit event should be logged

  @workflow @story-4-6
  Scenario: Cannot navigate to non-existent step
    Given I have a running workflow instance
    When I try to navigate to step "invalid-step"
    Then the request should fail with 400 Bad Request
    And the error should indicate state transition not found in workflow

  @workflow @story-4-6
  Scenario: Cannot navigate to unvisited step
    Given I have a running workflow instance
    And I am on step 2
    When I try to navigate to step 5
    Then the request should fail with 400 Bad Request
    And the error should indicate state transition not found in workflow

  # Integration Tests
  @workflow @integration @skip
  Scenario: Complete workflow lifecycle
    Given I create a workflow instance for "create-prd"
    When I start the workflow
    And I execute step 1
    And I pause the workflow
    And I resume the workflow
    And I execute step 2
    And I complete all remaining steps
    Then the workflow status should be "Completed"
    And all steps should have history records
    And all events should be logged

  @workflow @integration @skip
  Scenario: Workflow with skip and navigation
    Given I create a workflow instance with optional steps
    When I start the workflow
    And I execute step 1
    And I skip optional step 2
    And I execute step 3
    And I navigate back to step 1
    And I re-execute step 1
    Then the step history should show the revisit
    And the workflow should track all navigation

  @workflow @integration @skip
  Scenario: Workflow error recovery
    Given I create a workflow instance
    When I start the workflow
    And step execution fails
    Then the workflow status should be "Failed"
    And the error should be logged
    And I should be able to view the error details
    And the workflow should preserve all history
