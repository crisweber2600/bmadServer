namespace bmadServer.ApiService.Data.Entities;

/// <summary>
/// Persona types for communication preferences
/// </summary>
public enum PersonaType
{
    /// <summary>
    /// Business persona - Non-technical language, business-focused responses
    /// Example: "The system will validate your product requirements"
    /// </summary>
    Business = 0,

    /// <summary>
    /// Technical persona - Full technical details with code snippets and specifications
    /// Example: "The API will execute JSON schema validation on the PRD payload"
    /// </summary>
    Technical = 1,

    /// <summary>
    /// Hybrid persona - Adaptive based on context (default)
    /// Adjusts language based on the situation and workflow step
    /// </summary>
    Hybrid = 2
}
