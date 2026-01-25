using bmadServer.ServiceDefaults.Models.Workflows;
using Microsoft.Extensions.Logging;

namespace bmadServer.ServiceDefaults.Services.Workflows;

public class WorkflowRegistry : IWorkflowRegistry
{
    private readonly Dictionary<string, WorkflowDefinition> _workflows;
    private readonly ILogger<WorkflowRegistry>? _logger;

    public WorkflowRegistry(ILogger<WorkflowRegistry>? logger = null)
    {
        _logger = logger;
        _workflows = new Dictionary<string, WorkflowDefinition>(StringComparer.OrdinalIgnoreCase);
        InitializeWorkflows();
    }

    public IReadOnlyList<WorkflowDefinition> GetAllWorkflows()
    {
        return _workflows.Values.ToList().AsReadOnly();
    }

    public WorkflowDefinition? GetWorkflow(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            _logger?.LogWarning("GetWorkflow called with null or empty workflow id");
            return null;
        }

        if (_workflows.TryGetValue(id, out var workflow))
        {
            return workflow;
        }

        _logger?.LogWarning("Workflow with id '{WorkflowId}' not found", id);
        return null;
    }

    public bool ValidateWorkflow(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return false;
        }

        return _workflows.ContainsKey(id);
    }

    private void InitializeWorkflows()
    {
        // Create PRD Workflow
        var createPrdWorkflow = new WorkflowDefinition
        {
            WorkflowId = "create-prd",
            Name = "Create Product Requirements Document",
            Description = "Guides users through creating a comprehensive PRD with AI assistance",
            EstimatedDuration = TimeSpan.FromHours(2),
            RequiredRoles = new List<string> { "user", "product-owner" }.AsReadOnly(),
            Steps = new List<WorkflowStep>
            {
                new WorkflowStep
                {
                    StepId = "prd-1",
                    Name = "Define Project Vision",
                    AgentId = "prd-agent",
                    InputSchema = "{ \"projectName\": \"string\", \"description\": \"string\" }",
                    OutputSchema = "{ \"vision\": \"string\", \"objectives\": \"array\" }",
                    IsOptional = false,
                    CanSkip = false
                },
                new WorkflowStep
                {
                    StepId = "prd-2",
                    Name = "Identify User Stories",
                    AgentId = "prd-agent",
                    InputSchema = "{ \"vision\": \"string\" }",
                    OutputSchema = "{ \"userStories\": \"array\" }",
                    IsOptional = false,
                    CanSkip = false
                },
                new WorkflowStep
                {
                    StepId = "prd-3",
                    Name = "Define Requirements",
                    AgentId = "prd-agent",
                    InputSchema = "{ \"userStories\": \"array\" }",
                    OutputSchema = "{ \"functionalRequirements\": \"array\", \"nonFunctionalRequirements\": \"array\" }",
                    IsOptional = false,
                    CanSkip = false
                }
            }.AsReadOnly()
        };

        // Create Architecture Workflow
        var createArchitectureWorkflow = new WorkflowDefinition
        {
            WorkflowId = "create-architecture",
            Name = "Create Architecture Document",
            Description = "Creates system architecture documentation with AI-guided design",
            EstimatedDuration = TimeSpan.FromHours(3),
            RequiredRoles = new List<string> { "architect", "developer" }.AsReadOnly(),
            Steps = new List<WorkflowStep>
            {
                new WorkflowStep
                {
                    StepId = "arch-1",
                    Name = "System Overview",
                    AgentId = "architecture-agent",
                    InputSchema = "{ \"prdReference\": \"string\" }",
                    OutputSchema = "{ \"systemOverview\": \"string\", \"keyComponents\": \"array\" }",
                    IsOptional = false,
                    CanSkip = false
                },
                new WorkflowStep
                {
                    StepId = "arch-2",
                    Name = "Define Components",
                    AgentId = "architecture-agent",
                    InputSchema = "{ \"keyComponents\": \"array\" }",
                    OutputSchema = "{ \"componentDetails\": \"array\" }",
                    IsOptional = false,
                    CanSkip = false
                },
                new WorkflowStep
                {
                    StepId = "arch-3",
                    Name = "Integration Patterns",
                    AgentId = "architecture-agent",
                    InputSchema = "{ \"componentDetails\": \"array\" }",
                    OutputSchema = "{ \"integrationPatterns\": \"array\" }",
                    IsOptional = true,
                    CanSkip = true
                }
            }.AsReadOnly()
        };

        // Create Stories Workflow
        var createStoriesWorkflow = new WorkflowDefinition
        {
            WorkflowId = "create-stories",
            Name = "Create User Stories",
            Description = "Generates development-ready user stories from requirements",
            EstimatedDuration = TimeSpan.FromHours(1.5),
            RequiredRoles = new List<string> { "product-owner", "scrum-master" }.AsReadOnly(),
            Steps = new List<WorkflowStep>
            {
                new WorkflowStep
                {
                    StepId = "story-1",
                    Name = "Extract Stories from Requirements",
                    AgentId = "story-agent",
                    InputSchema = "{ \"requirements\": \"array\" }",
                    OutputSchema = "{ \"stories\": \"array\" }",
                    IsOptional = false,
                    CanSkip = false
                },
                new WorkflowStep
                {
                    StepId = "story-2",
                    Name = "Define Acceptance Criteria",
                    AgentId = "story-agent",
                    InputSchema = "{ \"stories\": \"array\" }",
                    OutputSchema = "{ \"storiesWithAC\": \"array\" }",
                    IsOptional = false,
                    CanSkip = false
                }
            }.AsReadOnly()
        };

        // Design UX Workflow
        var designUxWorkflow = new WorkflowDefinition
        {
            WorkflowId = "design-ux",
            Name = "Design User Experience",
            Description = "Creates UX designs and user flows with AI assistance",
            EstimatedDuration = TimeSpan.FromHours(4),
            RequiredRoles = new List<string> { "ux-designer", "product-owner" }.AsReadOnly(),
            Steps = new List<WorkflowStep>
            {
                new WorkflowStep
                {
                    StepId = "ux-1",
                    Name = "Define User Flows",
                    AgentId = "ux-agent",
                    InputSchema = "{ \"userStories\": \"array\" }",
                    OutputSchema = "{ \"userFlows\": \"array\" }",
                    IsOptional = false,
                    CanSkip = false
                },
                new WorkflowStep
                {
                    StepId = "ux-2",
                    Name = "Create Wireframes",
                    AgentId = "ux-agent",
                    InputSchema = "{ \"userFlows\": \"array\" }",
                    OutputSchema = "{ \"wireframes\": \"array\" }",
                    IsOptional = false,
                    CanSkip = false
                },
                new WorkflowStep
                {
                    StepId = "ux-3",
                    Name = "Design Interactions",
                    AgentId = "ux-agent",
                    InputSchema = "{ \"wireframes\": \"array\" }",
                    OutputSchema = "{ \"interactionDesigns\": \"array\" }",
                    IsOptional = true,
                    CanSkip = true
                }
            }.AsReadOnly()
        };

        // Dev Story Workflow
        var devStoryWorkflow = new WorkflowDefinition
        {
            WorkflowId = "dev-story",
            Name = "Execute Development Story",
            Description = "Implements a user story with TDD and comprehensive testing",
            EstimatedDuration = TimeSpan.FromHours(6),
            RequiredRoles = new List<string> { "developer" }.AsReadOnly(),
            Steps = new List<WorkflowStep>
            {
                new WorkflowStep
                {
                    StepId = "dev-1",
                    Name = "Load Story Context",
                    AgentId = "dev-agent",
                    InputSchema = "{ \"storyId\": \"string\" }",
                    OutputSchema = "{ \"storyContext\": \"object\" }",
                    IsOptional = false,
                    CanSkip = false
                },
                new WorkflowStep
                {
                    StepId = "dev-2",
                    Name = "Write Failing Tests",
                    AgentId = "dev-agent",
                    InputSchema = "{ \"storyContext\": \"object\" }",
                    OutputSchema = "{ \"tests\": \"array\" }",
                    IsOptional = false,
                    CanSkip = false
                },
                new WorkflowStep
                {
                    StepId = "dev-3",
                    Name = "Implement Solution",
                    AgentId = "dev-agent",
                    InputSchema = "{ \"tests\": \"array\" }",
                    OutputSchema = "{ \"implementation\": \"object\" }",
                    IsOptional = false,
                    CanSkip = false
                },
                new WorkflowStep
                {
                    StepId = "dev-4",
                    Name = "Refactor Code",
                    AgentId = "dev-agent",
                    InputSchema = "{ \"implementation\": \"object\" }",
                    OutputSchema = "{ \"refactoredCode\": \"object\" }",
                    IsOptional = true,
                    CanSkip = false
                }
            }.AsReadOnly()
        };

        // Code Review Workflow
        var codeReviewWorkflow = new WorkflowDefinition
        {
            WorkflowId = "code-review",
            Name = "Perform Code Review",
            Description = "AI-assisted code review focusing on quality, security, and best practices",
            EstimatedDuration = TimeSpan.FromMinutes(30),
            RequiredRoles = new List<string> { "reviewer", "senior-developer" }.AsReadOnly(),
            Steps = new List<WorkflowStep>
            {
                new WorkflowStep
                {
                    StepId = "review-1",
                    Name = "Analyze Code Changes",
                    AgentId = "review-agent",
                    InputSchema = "{ \"changeSet\": \"array\" }",
                    OutputSchema = "{ \"analysis\": \"object\" }",
                    IsOptional = false,
                    CanSkip = false
                },
                new WorkflowStep
                {
                    StepId = "review-2",
                    Name = "Identify Issues",
                    AgentId = "review-agent",
                    InputSchema = "{ \"analysis\": \"object\" }",
                    OutputSchema = "{ \"issues\": \"array\" }",
                    IsOptional = false,
                    CanSkip = false
                },
                new WorkflowStep
                {
                    StepId = "review-3",
                    Name = "Generate Review Report",
                    AgentId = "review-agent",
                    InputSchema = "{ \"issues\": \"array\" }",
                    OutputSchema = "{ \"reviewReport\": \"object\" }",
                    IsOptional = false,
                    CanSkip = false
                }
            }.AsReadOnly()
        };

        // Register all workflows
        _workflows[createPrdWorkflow.WorkflowId] = createPrdWorkflow;
        _workflows[createArchitectureWorkflow.WorkflowId] = createArchitectureWorkflow;
        _workflows[createStoriesWorkflow.WorkflowId] = createStoriesWorkflow;
        _workflows[designUxWorkflow.WorkflowId] = designUxWorkflow;
        _workflows[devStoryWorkflow.WorkflowId] = devStoryWorkflow;
        _workflows[codeReviewWorkflow.WorkflowId] = codeReviewWorkflow;

        _logger?.LogInformation("Initialized {Count} BMAD workflows", _workflows.Count);
    }
}
