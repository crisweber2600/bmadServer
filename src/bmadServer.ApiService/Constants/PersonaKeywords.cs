namespace bmadServer.ApiService.Constants;

/// <summary>
/// Shared keyword constants for persona translation and context analysis.
/// Used by ContextAnalysisService and ResponseMetadataService.
/// </summary>
public static class PersonaKeywords
{
    /// <summary>
    /// Technical indicators for content classification
    /// </summary>
    public static readonly string[] TechnicalKeywords = new[]
    {
        "API", "endpoint", "REST", "GraphQL", "HTTP", "HTTPS", "JSON", "XML",
        "database", "SQL", "query", "schema", "migration", "index",
        "cache", "Redis", "memcached", "CDN", "PostgreSQL",
        "authentication", "authorization", "JWT", "OAuth", "token", "session",
        "microservices", "service", "container", "Docker", "Kubernetes", "pod",
        "architecture", "infrastructure", "deployment", "CI/CD", "pipeline",
        "algorithm", "data structure", "complexity", "performance", "optimization",
        "latency", "throughput", "bandwidth", "scalability", "load balancing",
        "code", "function", "class", "method", "interface", "abstract",
        "version", "dependency", "library", "framework", "SDK", "package",
        "git", "commit", "branch", "merge", "pull request", "repository",
        "test", "unit test", "integration test", "TDD", "BDD", "mock",
        "bug", "error", "exception", "stack trace", "debugging", "logging"
    };

    /// <summary>
    /// Business indicators for content classification
    /// </summary>
    public static readonly string[] BusinessKeywords = new[]
    {
        "user", "customer", "client", "stakeholder", "team member",
        "business", "revenue", "cost", "profit", "ROI", "budget",
        "strategy", "goal", "objective", "KPI", "metric", "target",
        "market", "competition", "advantage", "opportunity", "risk",
        "product", "feature", "requirement", "specification", "use case",
        "workflow", "process", "procedure", "policy", "compliance",
        "decision", "approval", "review", "feedback", "iteration",
        "timeline", "deadline", "milestone", "deliverable", "schedule",
        "impact", "benefit", "value", "priority", "importance",
        "collaboration", "communication", "meeting", "presentation", "report",
        "quality", "satisfaction", "experience", "engagement", "adoption",
        "growth", "scale", "expansion", "innovation", "transformation"
    };

    /// <summary>
    /// Technical workflow steps
    /// </summary>
    public static readonly string[] TechnicalWorkflowSteps = new[]
    {
        "architecture", "implementation", "code review", "testing",
        "deployment", "configuration", "debugging", "optimization",
        "integration", "migration", "refactoring", "setup"
    };

    /// <summary>
    /// Business workflow steps
    /// </summary>
    public static readonly string[] BusinessWorkflowSteps = new[]
    {
        "planning", "requirements", "PRD", "specification", "review",
        "approval", "strategy", "analysis", "evaluation", "decision",
        "presentation", "documentation", "training"
    };
}
