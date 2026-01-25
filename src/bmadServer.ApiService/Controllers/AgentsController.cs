using bmadServer.ApiService.Agents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace bmadServer.ApiService.Controllers;

/// <summary>
/// Agent registry controller for querying available BMAD agents
/// </summary>
[ApiController]
[Route("api/v1/agents")]
public class AgentsController : ControllerBase
{
    private readonly IAgentRegistry _agentRegistry;
    private readonly ILogger<AgentsController> _logger;

    public AgentsController(
        IAgentRegistry agentRegistry,
        ILogger<AgentsController> logger)
    {
        _agentRegistry = agentRegistry;
        _logger = logger;
    }

    /// <summary>
    /// Get all registered agents
    /// </summary>
    /// <returns>List of all available agents</returns>
    /// <response code="200">Returns all agent definitions</response>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<AgentDefinition>), StatusCodes.Status200OK)]
    public IActionResult GetAllAgents()
    {
        _logger.LogInformation("Retrieving all agents from registry");
        var agents = _agentRegistry.GetAllAgents();
        return Ok(agents);
    }

    /// <summary>
    /// Get a specific agent by ID
    /// </summary>
    /// <param name="agentId">Agent identifier</param>
    /// <returns>Agent definition</returns>
    /// <response code="200">Returns the agent definition</response>
    /// <response code="404">Agent not found</response>
    [HttpGet("{agentId}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AgentDefinition), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public IActionResult GetAgent(string agentId)
    {
        _logger.LogInformation("Retrieving agent with ID: {AgentId}", agentId);
        
        var agent = _agentRegistry.GetAgent(agentId);
        
        if (agent == null)
        {
            _logger.LogWarning("Agent not found: {AgentId}", agentId);
            return NotFound(new ProblemDetails
            {
                Type = "https://bmadserver.dev/errors/agent-not-found",
                Title = "Agent Not Found",
                Status = StatusCodes.Status404NotFound,
                Detail = $"No agent found with ID: {agentId}"
            });
        }

        return Ok(agent);
    }

    /// <summary>
    /// Get agents by capability
    /// </summary>
    /// <param name="capability">Capability to search for</param>
    /// <returns>List of agents with the specified capability</returns>
    /// <response code="200">Returns agents with the capability</response>
    [HttpGet("by-capability/{capability}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<AgentDefinition>), StatusCodes.Status200OK)]
    public IActionResult GetAgentsByCapability(string capability)
    {
        _logger.LogInformation("Retrieving agents with capability: {Capability}", capability);
        var agents = _agentRegistry.GetAgentsByCapability(capability);
        return Ok(agents);
    }
}
