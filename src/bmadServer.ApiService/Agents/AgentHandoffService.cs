using bmadServer.ApiService.Data;
using bmadServer.ApiService.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace bmadServer.ApiService.Agents;

/// <summary>
/// Implementation of agent handoff tracking and attribution service.
/// </summary>
public class AgentHandoffService : IAgentHandoffService
{
    private readonly ApplicationDbContext _context;
    private readonly IAgentRegistry _agentRegistry;
    private readonly ILogger<AgentHandoffService> _logger;

    public AgentHandoffService(
        ApplicationDbContext context,
        IAgentRegistry agentRegistry,
        ILogger<AgentHandoffService> logger)
    {
        _context = context;
        _agentRegistry = agentRegistry;
        _logger = logger;
    }

    public async Task<AgentHandoffRecord> RecordHandoffAsync(
        Guid workflowInstanceId,
        string? fromAgent,
        string toAgent,
        string workflowStep,
        string? reason = null)
    {
        var toAgentDef = _agentRegistry.GetAgent(toAgent);
        if (toAgentDef == null)
        {
            throw new InvalidOperationException($"Agent '{toAgent}' not found in registry.");
        }

        AgentDefinition? fromAgentDef = null;
        if (fromAgent != null)
        {
            fromAgentDef = _agentRegistry.GetAgent(fromAgent);
        }

        var handoff = new AgentHandoff
        {
            WorkflowInstanceId = workflowInstanceId,
            FromAgent = fromAgent,
            ToAgent = toAgent,
            Timestamp = DateTime.UtcNow,
            WorkflowStep = workflowStep,
            Reason = reason,
            ToAgentName = toAgentDef.Name,
            FromAgentName = fromAgentDef?.Name
        };

        _context.Add(handoff);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Agent handoff recorded: {FromAgent} -> {ToAgent} at step {Step} for workflow {WorkflowId}",
            fromAgent ?? "INITIAL",
            toAgent,
            workflowStep,
            workflowInstanceId);

        return new AgentHandoffRecord
        {
            Id = handoff.Id,
            WorkflowInstanceId = handoff.WorkflowInstanceId,
            FromAgent = handoff.FromAgent,
            ToAgent = handoff.ToAgent,
            Timestamp = handoff.Timestamp,
            WorkflowStep = handoff.WorkflowStep,
            Reason = handoff.Reason,
            ToAgentName = handoff.ToAgentName,
            FromAgentName = handoff.FromAgentName
        };
    }

    public async Task<List<AgentHandoffRecord>> GetHandoffsAsync(Guid workflowInstanceId)
    {
        var handoffs = await _context.Set<AgentHandoff>()
            .Where(h => h.WorkflowInstanceId == workflowInstanceId)
            .OrderBy(h => h.Timestamp)
            .ToListAsync();

        return handoffs.Select(h => new AgentHandoffRecord
        {
            Id = h.Id,
            WorkflowInstanceId = h.WorkflowInstanceId,
            FromAgent = h.FromAgent,
            ToAgent = h.ToAgent,
            Timestamp = h.Timestamp,
            WorkflowStep = h.WorkflowStep,
            Reason = h.Reason,
            ToAgentName = h.ToAgentName,
            FromAgentName = h.FromAgentName
        }).ToList();
    }

    public async Task<string?> GetCurrentAgentAsync(Guid workflowInstanceId)
    {
        var latestHandoff = await _context.Set<AgentHandoff>()
            .Where(h => h.WorkflowInstanceId == workflowInstanceId)
            .OrderByDescending(h => h.Timestamp)
            .FirstOrDefaultAsync();

        return latestHandoff?.ToAgent;
    }

    public async Task<AgentDetails?> GetAgentDetailsAsync(string agentId, string? workflowStep = null)
    {
        var agent = _agentRegistry.GetAgent(agentId);
        if (agent == null)
        {
            return null;
        }

        string? currentStepResponsibility = null;
        if (workflowStep != null && agent.Capabilities.Contains(workflowStep))
        {
            currentStepResponsibility = $"Responsible for: {workflowStep}";
        }

        return new AgentDetails
        {
            AgentId = agent.AgentId,
            Name = agent.Name,
            Description = agent.Description,
            Capabilities = agent.Capabilities,
            CurrentStepResponsibility = currentStepResponsibility,
            Avatar = GenerateAgentAvatar(agent.AgentId)
        };
    }

    private string GenerateAgentAvatar(string agentId)
    {
        // Generate a simple avatar identifier based on agent ID
        // In a real implementation, this could return URLs to avatar images
        return agentId.ToUpperInvariant().Substring(0, Math.Min(2, agentId.Length));
    }
}
