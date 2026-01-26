using Microsoft.AspNetCore.Mvc;
using bmadServer.ApiService.Services.Workflows.Agents;
using Microsoft.AspNetCore.Authorization;

namespace bmadServer.ApiService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AgentsController : ControllerBase
{
    private readonly IAgentRegistry _agentRegistry;
    private readonly ILogger<AgentsController> _logger;

    public AgentsController(IAgentRegistry agentRegistry, ILogger<AgentsController> logger)
    {
        _agentRegistry = agentRegistry;
        _logger = logger;
    }

    /// <summary>
    /// Get all available BMAD agents
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AgentDefinition>), StatusCodes.Status200OK)]
    public IActionResult GetAllAgents()
    {
        _logger.LogDebug("Getting all agents");
        var agents = _agentRegistry.GetAllAgents();
        return Ok(agents);
    }

    /// <summary>
    /// Get a specific agent by ID
    /// </summary>
    [HttpGet("{agentId}")]
    [ProducesResponseType(typeof(AgentDefinition), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetAgent(string agentId)
    {
        _logger.LogDebug("Getting agent {AgentId}", agentId);
        
        var agent = _agentRegistry.GetAgent(agentId);
        if (agent == null)
        {
            _logger.LogWarning("Agent {AgentId} not found", agentId);
            return NotFound(new { message = $"Agent '{agentId}' not found" });
        }

        return Ok(agent);
    }

    /// <summary>
    /// Get agents by capability
    /// </summary>
    [HttpGet("by-capability/{capability}")]
    [ProducesResponseType(typeof(IReadOnlyList<AgentDefinition>), StatusCodes.Status200OK)]
    public IActionResult GetAgentsByCapability(string capability)
    {
        _logger.LogDebug("Getting agents with capability {Capability}", capability);
        
        var agents = _agentRegistry.GetAgentsByCapability(capability);
        return Ok(agents);
    }
}
