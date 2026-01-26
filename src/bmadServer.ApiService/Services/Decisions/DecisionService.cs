using bmadServer.ApiService.Data;
using bmadServer.ApiService.Data.Entities;
using bmadServer.ApiService.Models.Decisions;
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

            // Detect conflicts automatically
            var conflicts = await DetectConflictsAsync(decision, cancellationToken);
            if (conflicts.Count > 0)
            {
                _context.DecisionConflicts.AddRange(conflicts);
                await _context.SaveChangesAsync(cancellationToken);
            }

            _logger.LogInformation(
                "Decision {DecisionId} created for workflow {WorkflowInstanceId}, step {StepId}, type {DecisionType}. Detected {ConflictCount} conflicts",
                decision.Id,
                decision.WorkflowInstanceId,
                decision.StepId,
                decision.DecisionType,
                conflicts.Count
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

    /// <summary>
    /// Get the diff between two versions of a decision
    /// </summary>
    public async Task<DecisionVersionDiffResponse> GetVersionDiffAsync(Guid decisionId, int fromVersion, int toVersion, CancellationToken cancellationToken = default)
    {
        try
        {
            var fromVersionData = await _context.DecisionVersions
                .FirstOrDefaultAsync(v => v.DecisionId == decisionId && v.VersionNumber == fromVersion, cancellationToken);
            
            var toVersionData = await _context.DecisionVersions
                .FirstOrDefaultAsync(v => v.DecisionId == decisionId && v.VersionNumber == toVersion, cancellationToken);

            if (fromVersionData == null || toVersionData == null)
            {
                throw new InvalidOperationException($"One or both versions not found for decision {decisionId}");
            }

            var changes = new List<FieldChange>();

            // Compare main value field (compare JSON strings)
            var fromValueStr = fromVersionData.Value?.RootElement.ToString() ?? "";
            var toValueStr = toVersionData.Value?.RootElement.ToString() ?? "";
            
            if (fromValueStr != toValueStr)
            {
                changes.Add(new FieldChange
                {
                    FieldName = "Value",
                    ChangeType = "modified",
                    OldValue = fromVersionData.Value?.RootElement,
                    NewValue = toVersionData.Value?.RootElement
                });
            }

            // Compare question
            if (fromVersionData.Question != toVersionData.Question)
            {
                changes.Add(new FieldChange
                {
                    FieldName = "Question",
                    ChangeType = "modified",
                    OldValue = fromVersionData.Question != null ? JsonDocument.Parse($"\"{fromVersionData.Question}\"").RootElement : null,
                    NewValue = toVersionData.Question != null ? JsonDocument.Parse($"\"{toVersionData.Question}\"").RootElement : null
                });
            }

            // Compare reasoning
            if (fromVersionData.Reasoning != toVersionData.Reasoning)
            {
                changes.Add(new FieldChange
                {
                    FieldName = "Reasoning",
                    ChangeType = "modified",
                    OldValue = fromVersionData.Reasoning != null ? JsonDocument.Parse($"\"{fromVersionData.Reasoning}\"").RootElement : null,
                    NewValue = toVersionData.Reasoning != null ? JsonDocument.Parse($"\"{toVersionData.Reasoning}\"").RootElement : null
                });
            }

            _logger.LogInformation(
                "Diff generated between versions {FromVersion} and {ToVersion} for decision {DecisionId}. {ChangeCount} changes found.",
                fromVersion,
                toVersion,
                decisionId,
                changes.Count);

            return new DecisionVersionDiffResponse
            {
                FromVersion = fromVersion,
                ToVersion = toVersion,
                Changes = changes
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to generate diff between versions {FromVersion} and {ToVersion} for decision {DecisionId}",
                fromVersion,
                toVersion,
                decisionId);
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
                Status = "Pending",
                ReviewerIds = string.Join(",", reviewerIds)
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
            var response = new Data.Entities.DecisionReviewResponse
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
                var requiredApprovals = !string.IsNullOrEmpty(review.ReviewerIds) 
                    ? review.ReviewerIds.Split(",").Length 
                    : 1;
                var approvedCount = review.Responses.Count(r => r.ResponseType == "Approved") + 1;

                _logger.LogInformation(
                    "Review {ReviewId}: {ApprovedCount}/{RequiredApprovals} approvals received",
                    reviewId,
                    approvedCount,
                    requiredApprovals
                );

                // Only lock if all required reviewers have approved
                if (approvedCount == requiredApprovals && requiredApprovals > 0)
                {
                    // Mark review as completed and auto-lock decision
                    review.Status = "Completed";
                    review.CompletedAt = DateTime.UtcNow;

                    if (review.Decision != null)
                    {
                        review.Decision.Status = DecisionStatus.Approved;
                        review.Decision.IsLocked = true;
                        review.Decision.LockedBy = userId;
                        review.Decision.LockedAt = DateTime.UtcNow;
                        review.Decision.LockReason = "Auto-locked after all reviewers approved";
                    }

                    _logger.LogInformation(
                        "Review {ReviewId} completed and decision {DecisionId} auto-locked after all approvals",
                        reviewId,
                        review.DecisionId
                    );
                }
                else
                {
                    _logger.LogInformation(
                        "Review {ReviewId} awaiting remaining approvals ({ApprovedCount}/{RequiredApprovals})",
                        reviewId,
                        approvedCount,
                        requiredApprovals
                    );
                }
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

    /// <summary>
    /// Detects conflicts between the given decision and other decisions in the workflow
    /// </summary>
    private async Task<List<DecisionConflict>> DetectConflictsAsync(Decision decision, CancellationToken cancellationToken)
    {
        var conflicts = new List<DecisionConflict>();

        try
        {
            // Load all active conflict rules
            var rules = await _context.ConflictRules
                .Where(r => r.IsActive)
                .ToListAsync(cancellationToken);

            if (rules.Count == 0)
            {
                return conflicts;
            }

            // Load other decisions in the same workflow for comparison
            var otherDecisions = await _context.Decisions
                .Where(d => d.WorkflowInstanceId == decision.WorkflowInstanceId && d.Id != decision.Id)
                .ToListAsync(cancellationToken);

            // Evaluate each rule against this decision
            foreach (var rule in rules)
            {
                if (EvaluateRule(rule, decision))
                {
                    // Find conflicting decisions with other decisions if applicable
                    foreach (var otherDecision in otherDecisions)
                    {
                        if (ShouldCreateConflict(rule, decision, otherDecision))
                        {
                            var conflict = new DecisionConflict
                            {
                                DecisionId1 = decision.Id,
                                DecisionId2 = otherDecision.Id,
                                ConflictType = rule.ConflictType,
                                Description = $"Rule '{rule.Name}' violated",
                                Severity = rule.Severity,
                                DetectedAt = DateTime.UtcNow,
                                Status = "Open"
                            };

                            conflicts.Add(conflict);
                        }
                    }
                }
            }

            if (conflicts.Count > 0)
            {
                _logger.LogInformation(
                    "Detected {ConflictCount} conflicts for decision {DecisionId}",
                    conflicts.Count,
                    decision.Id
                );
            }

            return conflicts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting conflicts for decision {DecisionId}", decision.Id);
            return conflicts;
        }
    }

    /// <summary>
    /// Evaluates whether a rule is violated by the given decision
    /// </summary>
    private bool EvaluateRule(ConflictRule rule, Decision decision)
    {
        try
        {
            if (decision.Value == null)
            {
                return false;
            }

            // Parse the decision value
            var decisionValue = decision.Value.RootElement;

            // Simple rule evaluation based on common patterns
            // Examples: "Budget > 1000000", "Timeline < 30", "Status == Urgent"
            if (rule.Configuration != null)
            {
                var config = rule.Configuration.RootElement;
                
                // Check if configuration has a field to evaluate
                if (config.TryGetProperty("field", out var fieldProperty) &&
                    config.TryGetProperty("operator", out var operatorProperty) &&
                    config.TryGetProperty("value", out var valueProperty))
                {
                    var field = fieldProperty.GetString();
                    var op = operatorProperty.GetString();
                    
                    if (!string.IsNullOrEmpty(field) && decisionValue.TryGetProperty(field, out var decisionFieldValue))
                    {
                        return EvaluateCondition(decisionFieldValue, op, valueProperty);
                    }
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error evaluating rule {RuleName}", rule.Name);
            return false;
        }
    }

    /// <summary>
    /// Evaluates a condition between a decision field value and a target value
    /// </summary>
    private bool EvaluateCondition(JsonElement fieldValue, string? op, JsonElement valueProperty)
    {
        try
        {
            return op switch
            {
                ">" => fieldValue.TryGetInt64(out var num) && 
                       valueProperty.TryGetInt64(out var target) && 
                       num > target,
                "<" => fieldValue.TryGetInt64(out var num2) && 
                       valueProperty.TryGetInt64(out var target2) && 
                       num2 < target2,
                "==" => fieldValue.GetString() == valueProperty.GetString(),
                "!=" => fieldValue.GetString() != valueProperty.GetString(),
                ">=" => fieldValue.TryGetInt64(out var num3) && 
                        valueProperty.TryGetInt64(out var target3) && 
                        num3 >= target3,
                "<=" => fieldValue.TryGetInt64(out var num4) && 
                        valueProperty.TryGetInt64(out var target4) && 
                        num4 <= target4,
                _ => false
            };
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Determines if two decisions should be marked as conflicting
    /// </summary>
    private bool ShouldCreateConflict(ConflictRule rule, Decision decision1, Decision decision2)
    {
        // Check if both decisions are affected by the conflict rule
        if (decision1.Value == null || decision2.Value == null)
        {
            return false;
        }

        // Only create conflict if both decisions violate the rule or are related
        // For now, we create conflict if they have overlapping concerns based on decision type
        return decision1.DecisionType == decision2.DecisionType ||
               rule.ConflictType.ToLower().Contains(decision1.DecisionType.ToLower()) ||
               rule.ConflictType.ToLower().Contains(decision2.DecisionType.ToLower());
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
