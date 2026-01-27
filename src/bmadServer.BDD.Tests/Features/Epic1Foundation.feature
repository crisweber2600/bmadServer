Feature: Aspire Foundation & Project Setup
  As a developer
  I want a properly configured Aspire foundation
  So that I can build reliable microservices

  Background:
    Given the project is initialized

  # Story 1.1: Initialize bmadServer from .NET Aspire Starter Template
  @epic-1 @story-1-1
  Scenario: Project structure created from Aspire template
    Given .NET 10 SDK is installed
    When the project is created from Aspire starter template
    Then the project structure should include AppHost
    And the project structure should include ApiService
    And the project structure should include ServiceDefaults
    And Directory.Build.props should exist

  @epic-1 @story-1-1
  Scenario: Aspire dashboard accessible after startup
    Given the project is built successfully
    When I run the Aspire application
    Then the Aspire dashboard should be accessible
    And the API should respond to GET /health with 200 OK

  @epic-1 @story-1-1
  Scenario: Distributed tracing is configured
    Given the AppHost is running
    When I check the AppHost logs
    Then I should see structured JSON logs
    And logs should include trace IDs

  # Story 1.2: Configure PostgreSQL Database Resource
  @epic-1 @story-1-2
  Scenario: PostgreSQL resource is configured in AppHost
    Given the AppHost project exists
    When I examine the PostgreSQL configuration
    Then PostgreSQL container should be configured
    And connection string should be exposed to services
    And database migrations should be applied on startup
