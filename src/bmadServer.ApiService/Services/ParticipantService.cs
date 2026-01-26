using bmadServer.ApiService.Data;
using bmadServer.ApiService.Models.Workflows;
using Microsoft.EntityFrameworkCore;

namespace bmadServer.ApiService.Services;

public class ParticipantService : IParticipantService
{
    private readonly ApplicationDbContext _context;

    public ParticipantService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<WorkflowParticipant> AddParticipantAsync(
        Guid workflowId, 
        Guid userId, 
        ParticipantRole role, 
        Guid addedBy)
    {
        // Verify user exists
        var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
        if (!userExists)
        {
            throw new InvalidOperationException($"User {userId} does not exist");
        }

        // Check if participant already exists
        var existingParticipant = await _context.WorkflowParticipants
            .FirstOrDefaultAsync(p => p.WorkflowId == workflowId && p.UserId == userId);

        if (existingParticipant != null)
        {
            throw new InvalidOperationException($"User {userId} is already a participant of workflow {workflowId}");
        }

        var participant = new WorkflowParticipant
        {
            Id = Guid.NewGuid(),
            WorkflowId = workflowId,
            UserId = userId,
            Role = role,
            AddedAt = DateTime.UtcNow,
            AddedBy = addedBy
        };

        _context.WorkflowParticipants.Add(participant);
        await _context.SaveChangesAsync();

        // Reload with User navigation property
        return await _context.WorkflowParticipants
            .Include(p => p.User)
            .FirstAsync(p => p.Id == participant.Id);
    }

    public async Task<bool> RemoveParticipantAsync(Guid workflowId, Guid userId)
    {
        var participant = await _context.WorkflowParticipants
            .FirstOrDefaultAsync(p => p.WorkflowId == workflowId && p.UserId == userId);

        if (participant == null)
        {
            return false;
        }

        _context.WorkflowParticipants.Remove(participant);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<List<WorkflowParticipant>> GetParticipantsAsync(Guid workflowId)
    {
        return await _context.WorkflowParticipants
            .Include(p => p.User)
            .Where(p => p.WorkflowId == workflowId)
            .ToListAsync();
    }

    public async Task<WorkflowParticipant?> GetParticipantAsync(Guid workflowId, Guid userId)
    {
        return await _context.WorkflowParticipants
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.WorkflowId == workflowId && p.UserId == userId);
    }

    public async Task<bool> IsParticipantAsync(Guid workflowId, Guid userId)
    {
        return await _context.WorkflowParticipants
            .AnyAsync(p => p.WorkflowId == workflowId && p.UserId == userId);
    }

    public async Task<bool> IsWorkflowOwnerAsync(Guid workflowId, Guid userId)
    {
        var workflow = await _context.WorkflowInstances
            .FirstOrDefaultAsync(w => w.Id == workflowId);

        return workflow?.UserId == userId;
    }
}
