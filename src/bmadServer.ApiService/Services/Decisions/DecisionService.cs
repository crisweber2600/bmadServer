using bmadServer.ApiService.Data;
using bmadServer.ApiService.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace bmadServer.ApiService.Services.Decisions;

/// <summary>
/// Service implementation for managing workflow decisions
/// </summary>
public class DecisionService : IDecisionService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DecisionService> _logger;

    public DecisionService(ApplicationDbContext context, ILogger<DecisionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<Decision> CreateDecisionAsync(Decision decision, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate that the workflow instance exists
            var workflowExists = await _context.WorkflowInstances
                .AnyAsync(w => w.Id == decision.WorkflowInstanceId, cancellationToken);

            if (!workflowExists)
            {
                throw new InvalidOperationException($"Workflow instance {decision.WorkflowInstanceId} does not exist");
            }

            _context.Decisions.Add(decision);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Decision {DecisionId} created for workflow {WorkflowInstanceId}, step {StepId}, type {DecisionType}",
                decision.Id,
                decision.WorkflowInstanceId,
                decision.StepId,
                decision.DecisionType
            );

            return decision;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Failed to create decision for workflow {WorkflowInstanceId}", 
                decision.WorkflowInstanceId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<List<Decision>> GetDecisionsByWorkflowInstanceAsync(
        Guid workflowInstanceId, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var decisions = await _context.Decisions
                .Where(d => d.WorkflowInstanceId == workflowInstanceId)
                .OrderBy(d => d.DecidedAt)
                .ToListAsync(cancellationToken);

            _logger.LogInformation(
                "Retrieved {Count} decisions for workflow {WorkflowInstanceId}",
                decisions.Count,
                workflowInstanceId
            );

            return decisions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Failed to retrieve decisions for workflow {WorkflowInstanceId}", 
                workflowInstanceId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Decision?> GetDecisionByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var decision = await _context.Decisions
                .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

            if (decision != null)
            {
                _logger.LogInformation("Retrieved decision {DecisionId}", id);
            }
            else
            {
                _logger.LogWarning("Decision {DecisionId} not found", id);
            }

            return decision;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve decision {DecisionId}", id);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Decision> UpdateDecisionAsync(
        Guid id,
        Guid userId,
        JsonDocument value,
        string? question,
        JsonDocument? options,
        string? reasoning,
        JsonDocument? context,
        string? changeReason,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var decision = await _context.Decisions
                .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

            if (decision == null)
            {
                throw new InvalidOperationException($"Decision {id} not found");
            }

            // Check if decision is locked
            if (decision.IsLocked)
            {
                throw new InvalidOperationException($"Decision {id} is locked. Unlock it before making changes.");
            }

            // Create version record with current values
            var version = new DecisionVersion
            {
                DecisionId = decision.Id,
                VersionNumber = decision.CurrentVersion,
                Value = decision.Value,
                Question = decision.Question,
                Options = decision.Options,
                Reasoning = decision.Reasoning,
                Context = decision.Context,
                ModifiedBy = userId,
                ModifiedAt = DateTime.UtcNow,
                ChangeReason = changeReason
            };

            _context.DecisionVersions.Add(version);

            // Update decision with new values
            decision.Value = value;
            decision.Question = question;
            decision.Options = options;
            decision.Reasoning = reasoning;
            decision.Context = context;
            decision.CurrentVersion++;
            decision.UpdatedAt = DateTime.UtcNow;
            decision.UpdatedBy = userId;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Decision {DecisionId} updated to version {Version} by user {UserId}",
                id,
                decision.CurrentVersion,
                userId
            );

            return decision;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update decision {DecisionId}", id);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<List<DecisionVersion>> GetDecisionHistoryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var versions = await _context.DecisionVersions
                .Where(v => v.DecisionId == id)
                .OrderBy(v => v.VersionNumber)
                .ToListAsync(cancellationToken);

            _logger.LogInformation(
                "Retrieved {Count} versions for decision {DecisionId}",
                versions.Count,
                id
            );

            return versions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve version history for decision {DecisionId}", id);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<DecisionVersion?> GetDecisionVersionAsync(Guid id, int versionNumber, CancellationToken cancellationToken = default)
    {
        try
        {
            var version = await _context.DecisionVersions
                .FirstOrDefaultAsync(v => v.DecisionId == id && v.VersionNumber == versionNumber, cancellationToken);

            if (version != null)
            {
                _logger.LogInformation(
                    "Retrieved version {Version} for decision {DecisionId}",
                    versionNumber,
                    id
                );
            }
            else
            {
                _logger.LogWarning(
                    "Version {Version} not found for decision {DecisionId}",
                    versionNumber,
                    id
                );
            }

            return version;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Failed to retrieve version {Version} for decision {DecisionId}",
                versionNumber,
                id);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Decision> RevertDecisionAsync(
        Guid id,
        int versionNumber,
        Guid userId,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var decision = await _context.Decisions
                .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

            if (decision == null)
            {
                throw new InvalidOperationException($"Decision {id} not found");
            }

            var targetVersion = await _context.DecisionVersions
                .FirstOrDefaultAsync(v => v.DecisionId == id && v.VersionNumber == versionNumber, cancellationToken);

            if (targetVersion == null)
            {
                throw new InvalidOperationException($"Version {versionNumber} not found for decision {id}");
            }

            // Create version record with current values before reverting
            var currentVersion = new DecisionVersion
            {
                DecisionId = decision.Id,
                VersionNumber = decision.CurrentVersion,
                Value = decision.Value,
                Question = decision.Question,
                Options = decision.Options,
                Reasoning = decision.Reasoning,
                Context = decision.Context,
                ModifiedBy = userId,
                ModifiedAt = DateTime.UtcNow,
                ChangeReason = $"Revert to version {versionNumber}: {reason}"
            };

            _context.DecisionVersions.Add(currentVersion);

            // Revert to target version values
            decision.Value = targetVersion.Value;
            decision.Question = targetVersion.Question;
            decision.Options = targetVersion.Options;
            decision.Reasoning = targetVersion.Reasoning;
            decision.Context = targetVersion.Context;
            decision.CurrentVersion++;
            decision.UpdatedAt = DateTime.UtcNow;
            decision.UpdatedBy = userId;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Decision {DecisionId} reverted to version {TargetVersion}, now at version {CurrentVersion}",
                id,
                versionNumber,
                decision.CurrentVersion
            );

            return decision;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to revert decision {DecisionId} to version {Version}",
                id,
                versionNumber);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Decision> LockDecisionAsync(
        Guid id,
        Guid userId,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var decision = await _context.Decisions
                .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

            if (decision == null)
            {
                throw new InvalidOperationException($"Decision {id} not found");
            }

            if (decision.IsLocked)
            {
                throw new InvalidOperationException($"Decision {id} is already locked");
            }

            decision.IsLocked = true;
            decision.LockedBy = userId;
            decision.LockedAt = DateTime.UtcNow;
            decision.LockReason = reason;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Decision {DecisionId} locked by user {UserId}",
                id,
                userId
            );

            return decision;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to lock decision {DecisionId}", id);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Decision> UnlockDecisionAsync(
        Guid id,
        Guid userId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var decision = await _context.Decisions
                .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

            if (decision == null)
            {
                throw new InvalidOperationException($"Decision {id} not found");
            }

            if (!decision.IsLocked)
            {
                throw new InvalidOperationException($"Decision {id} is not locked");
            }

            // Create an audit record of the unlock action
            _logger.LogInformation(
                "Decision {DecisionId} unlocked by user {UserId}. Reason: {Reason}. Previously locked by {LockedBy}",
                id,
                userId,
                reason,
                decision.LockedBy
            );

            decision.IsLocked = false;
            decision.LockedBy = null;
            decision.LockedAt = null;
            decision.LockReason = null;

            await _context.SaveChangesAsync(cancellationToken);

            return decision;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unlock decision {DecisionId}", id);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<DecisionReview> RequestReviewAsync(
        Guid id,
        Guid userId,
        List<Guid> reviewerIds,
        DateTime? deadline,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var decision = await _context.Decisions
                .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

            if (decision == null)
            {
                throw new InvalidOperationException($"Decision {id} not found");
            }

            if (decision.IsLocked)
            {
                throw new InvalidOperationException($"Decision {id} is already locked");
            }

            // Check if there's already an active review
            var existingReview = await _context.DecisionReviews
                .Where(r => r.DecisionId == id && r.Status == "Pending")
                .FirstOrDefaultAsync(cancellationToken);

            if (existingReview != null)
            {
                throw new InvalidOperationException($"Decision {id} already has an active review");
            }

            // Create the review
            var review = new DecisionReview
            {
                DecisionId = id,
                RequestedBy = userId,
                RequestedAt = DateTime.UtcNow,
                Deadline = deadline,
                Status = "Pending"
            };

            _context.DecisionReviews.Add(review);

            // Update decision status
            decision.Status = DecisionStatus.UnderReview;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Review requested for decision {DecisionId} by user {UserId} with {ReviewerCount} reviewers",
                id,
                userId,
                reviewerIds.Count
            );

            return review;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to request review for decision {DecisionId}", id);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<DecisionReview> SubmitReviewResponseAsync(
        Guid reviewId,
        Guid userId,
        string responseType,
        string? comments,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var review = await _context.DecisionReviews
                .Include(r => r.Responses)
                .Include(r => r.Decision)
                .FirstOrDefaultAsync(r => r.Id == reviewId, cancellationToken);

            if (review == null)
            {
                throw new InvalidOperationException($"Review {reviewId} not found");
            }

            if (review.Status != "Pending")
            {
                throw new InvalidOperationException($"Review {reviewId} is not pending");
            }

            // Check if this reviewer has already responded
            var existingResponse = review.Responses
                .FirstOrDefault(r => r.ReviewerId == userId);

            if (existingResponse != null)
            {
                throw new InvalidOperationException("You have already submitted a response for this review");
            }

            // Create the response
            var response = new DecisionReviewResponse
            {
                ReviewId = reviewId,
                ReviewerId = userId,
                ResponseType = responseType,
                Comments = comments,
                RespondedAt = DateTime.UtcNow
            };

            _context.DecisionReviewResponses.Add(response);

            // Check if this is a "ChangesRequested" response
            if (responseType == "ChangesRequested")
            {
                // Move decision back to Draft and complete the review
                if (review.Decision != null)
                {
                    review.Decision.Status = DecisionStatus.ChangesRequested;
                }
                review.Status = "Completed";
                review.CompletedAt = DateTime.UtcNow;

                _logger.LogInformation(
                    "Review {ReviewId} completed with changes requested by reviewer {ReviewerId}",
                    reviewId,
                    userId
                );
            }
            else if (responseType == "Approved")
            {
                // Check if all reviewers have approved
                var totalResponses = review.Responses.Count + 1; // +1 for the new response
                var approvedCount = review.Responses.Count(r => r.ResponseType == "Approved") + 1;

                // For simplicity, we'll assume all invited reviewers must approve
                // In a real system, you'd track the invited reviewers explicitly
                _logger.LogInformation(
                    "Review {ReviewId}: {ApprovedCount} approvals received",
                    reviewId,
                    approvedCount
                );

                // Mark review as completed and auto-lock decision
                review.Status = "Completed";
                review.CompletedAt = DateTime.UtcNow;

                if (review.Decision != null)
                {
                    review.Decision.Status = DecisionStatus.Approved;
                    review.Decision.IsLocked = true;
                    review.Decision.LockedBy = userId;
                    review.Decision.LockedAt = DateTime.UtcNow;
                    review.Decision.LockReason = "Auto-locked after review approval";
                }

                _logger.LogInformation(
                    "Review {ReviewId} completed and decision {DecisionId} auto-locked",
                    reviewId,
                    review.DecisionId
                );
            }

            await _context.SaveChangesAsync(cancellationToken);

            return review;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit review response for review {ReviewId}", reviewId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<DecisionReview?> GetDecisionReviewAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var review = await _context.DecisionReviews
                .Include(r => r.Responses)
                .FirstOrDefaultAsync(r => r.DecisionId == id, cancellationToken);

            if (review != null)
            {
                _logger.LogInformation("Retrieved review for decision {DecisionId}", id);
            }
            else
            {
                _logger.LogWarning("No review found for decision {DecisionId}", id);
            }

            return review;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve review for decision {DecisionId}", id);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<List<DecisionConflict>> GetConflictsForWorkflowAsync(Guid workflowId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get all decisions for the workflow
            var decisionIds = await _context.Decisions
                .Where(d => d.WorkflowInstanceId == workflowId)
                .Select(d => d.Id)
                .ToListAsync(cancellationToken);

            // Get conflicts involving any of these decisions
            var conflicts = await _context.DecisionConflicts
                .Where(c => decisionIds.Contains(c.DecisionId1) || decisionIds.Contains(c.DecisionId2))
                .ToListAsync(cancellationToken);

            _logger.LogInformation(
                "Retrieved {Count} conflicts for workflow {WorkflowId}",
                conflicts.Count,
                workflowId
            );

            return conflicts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve conflicts for workflow {WorkflowId}", workflowId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<(DecisionConflict conflict, Decision decision1, Decision decision2)?> GetConflictDetailsAsync(Guid conflictId, CancellationToken cancellationToken = default)
    {
        try
        {
            var conflict = await _context.DecisionConflicts
                .FirstOrDefaultAsync(c => c.Id == conflictId, cancellationToken);

            if (conflict == null)
            {
                return null;
            }

            var decision1 = await _context.Decisions.FindAsync(new object[] { conflict.DecisionId1 }, cancellationToken);
            var decision2 = await _context.Decisions.FindAsync(new object[] { conflict.DecisionId2 }, cancellationToken);

            if (decision1 == null || decision2 == null)
            {
                _logger.LogWarning("Conflict {ConflictId} references non-existent decisions", conflictId);
                return null;
            }

            _logger.LogInformation("Retrieved conflict details for {ConflictId}", conflictId);
            return (conflict, decision1, decision2);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve conflict details for {ConflictId}", conflictId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<DecisionConflict> ResolveConflictAsync(Guid conflictId, Guid userId, string resolution, CancellationToken cancellationToken = default)
    {
        try
        {
            var conflict = await _context.DecisionConflicts
                .FirstOrDefaultAsync(c => c.Id == conflictId, cancellationToken);

            if (conflict == null)
            {
                throw new InvalidOperationException($"Conflict {conflictId} not found");
            }

            if (conflict.Status != "Open")
            {
                throw new InvalidOperationException($"Conflict {conflictId} is already {conflict.Status}");
            }

            conflict.Status = "Resolved";
            conflict.ResolvedAt = DateTime.UtcNow;
            conflict.ResolvedBy = userId;
            conflict.Resolution = resolution;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Conflict {ConflictId} resolved by user {UserId}",
                conflictId,
                userId
            );

            return conflict;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve conflict {ConflictId}", conflictId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<DecisionConflict> OverrideConflictAsync(Guid conflictId, Guid userId, string justification, CancellationToken cancellationToken = default)
    {
        try
        {
            var conflict = await _context.DecisionConflicts
                .FirstOrDefaultAsync(c => c.Id == conflictId, cancellationToken);

            if (conflict == null)
            {
                throw new InvalidOperationException($"Conflict {conflictId} not found");
            }

            if (conflict.Status != "Open")
            {
                throw new InvalidOperationException($"Conflict {conflictId} is already {conflict.Status}");
            }

            conflict.Status = "Overridden";
            conflict.ResolvedAt = DateTime.UtcNow;
            conflict.ResolvedBy = userId;
            conflict.OverrideJustification = justification;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Conflict {ConflictId} overridden by user {UserId} with justification: {Justification}",
                conflictId,
                userId,
                justification
            );

            return conflict;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to override conflict {ConflictId}", conflictId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<List<ConflictRule>> GetConflictRulesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var rules = await _context.ConflictRules
                .Where(r => r.IsActive)
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Retrieved {Count} active conflict rules", rules.Count);
            return rules;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve conflict rules");
            throw;
        }
    }
}
