using bmadServer.ApiService.Data;
using bmadServer.ApiService.Models.Workflows;
using Microsoft.EntityFrameworkCore;

namespace bmadServer.ApiService.Services;

public class ParticipantService : IParticipantService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ParticipantService> _logger;
    private const int MaxParticipantsPerWorkflow = 100;

    public ParticipantService(ApplicationDbContext context, ILogger<ParticipantService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<WorkflowParticipant> AddParticipantAsync(
        Guid workflowId, 
        Guid userId, 
        ParticipantRole role, 
        Guid addedBy,
        CancellationToken cancellationToken = default)
    {
        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // Verify workflow exists
            var workflow = await _context.WorkflowInstances
                .FirstOrDefaultAsync(w => w.Id == workflowId, cancellationToken);
            
            if (workflow == null)
            {
                throw new InvalidOperationException($"Workflow {workflowId} does not exist");
            }

            // Verify user exists
            var userExists = await _context.Users.AnyAsync(u => u.Id == userId, cancellationToken);
            if (!userExists)
            {
                throw new InvalidOperationException($"User {userId} does not exist");
            }

            // Check if participant already exists
            var existingParticipant = await _context.WorkflowParticipants
                .FirstOrDefaultAsync(p => p.WorkflowId == workflowId && p.UserId == userId, cancellationToken);

            if (existingParticipant != null)
            {
                throw new InvalidOperationException($"User {userId} is already a participant of workflow {workflowId}");
            }

            // Check capacity limit
            var participantCount = await _context.WorkflowParticipants
                .CountAsync(p => p.WorkflowId == workflowId, cancellationToken);

            if (participantCount >= MaxParticipantsPerWorkflow)
            {
                throw new InvalidOperationException(
                    $"Cannot add participant: workflow has reached maximum capacity of {MaxParticipantsPerWorkflow} participants");
            }

            // Verify adder has permission (must be owner or existing participant with admin role)
            var adderIsOwner = workflow.UserId == addedBy;
            var adderIsParticipant = await _context.WorkflowParticipants
                .AnyAsync(p => p.WorkflowId == workflowId && p.UserId == addedBy && p.Role == ParticipantRole.Admin,
                    cancellationToken);

            if (!adderIsOwner && !adderIsParticipant)
            {
                throw new InvalidOperationException(
                    $"User {addedBy} does not have permission to add participants to workflow {workflowId}");
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
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Added participant {UserId} to workflow {WorkflowId} with role {Role}",
                userId, workflowId, role);

            // Reload with User navigation property
            return await _context.WorkflowParticipants
                .Include(p => p.User)
                .FirstAsync(p => p.Id == participant.Id, cancellationToken);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Failed to add participant {UserId} to workflow {WorkflowId}", userId, workflowId);
            throw;
        }
    }

    public async Task<bool> RemoveParticipantAsync(Guid workflowId, Guid userId, CancellationToken cancellationToken = default)
    {
        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var participant = await _context.WorkflowParticipants
                .FirstOrDefaultAsync(p => p.WorkflowId == workflowId && p.UserId == userId, cancellationToken);

            if (participant == null)
            {
                return false;
            }

            _context.WorkflowParticipants.Remove(participant);
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Removed participant {UserId} from workflow {WorkflowId}",
                userId, workflowId);

            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Failed to remove participant {UserId} from workflow {WorkflowId}", userId, workflowId);
            throw;
        }
    }

    public async Task<List<WorkflowParticipant>> GetParticipantsAsync(
        Guid workflowId, 
        CancellationToken cancellationToken = default)
    {
        return await _context.WorkflowParticipants
            .Include(p => p.User)
            .Where(p => p.WorkflowId == workflowId)
            .ToListAsync(cancellationToken);
    }

    public async Task<WorkflowParticipant?> GetParticipantAsync(
        Guid workflowId, 
        Guid userId, 
        CancellationToken cancellationToken = default)
    {
        return await _context.WorkflowParticipants
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.WorkflowId == workflowId && p.UserId == userId, cancellationToken);
    }

    public async Task<bool> IsParticipantAsync(Guid workflowId, Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.WorkflowParticipants
            .AnyAsync(p => p.WorkflowId == workflowId && p.UserId == userId, cancellationToken);
    }

    public async Task<bool> IsWorkflowOwnerAsync(Guid workflowId, Guid userId, CancellationToken cancellationToken = default)
    {
        var workflow = await _context.WorkflowInstances
            .FirstOrDefaultAsync(w => w.Id == workflowId, cancellationToken);

        return workflow?.UserId == userId;
    }
}
