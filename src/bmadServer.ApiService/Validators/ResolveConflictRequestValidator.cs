using FluentValidation;
using bmadServer.ApiService.DTOs;

namespace bmadServer.ApiService.Validators;

/// <summary>
/// Validator for ResolveConflictRequest DTO
/// </summary>
public class ResolveConflictRequestValidator : AbstractValidator<ResolveConflictRequest>
{
    private static readonly string[] ValidResolutionTypes = 
    { 
        "AcceptA", "AcceptB", "Merge", "RejectBoth", "Custom" 
    };

    public ResolveConflictRequestValidator()
    {
        RuleFor(x => x.ResolutionType)
            .NotEmpty()
            .WithMessage("Resolution type is required")
            .MaximumLength(50)
            .WithMessage("Resolution type cannot exceed 50 characters")
            .Must(x => ValidResolutionTypes.Contains(x, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Resolution type must be one of: {string.Join(", ", ValidResolutionTypes)}");

        RuleFor(x => x.FinalValue)
            .MaximumLength(5000)
            .WithMessage("Final value cannot exceed 5000 characters")
            .When(x => x.FinalValue != null);

        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("Reason is required")
            .MaximumLength(2000)
            .WithMessage("Reason cannot exceed 2000 characters");
    }
}
