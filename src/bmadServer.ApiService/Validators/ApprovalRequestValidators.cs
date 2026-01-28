using FluentValidation;
using bmadServer.ApiService.DTOs;

namespace bmadServer.ApiService.Validators;

/// <summary>
/// Validator for ApprovalModifyRequest DTO
/// </summary>
public class ApprovalModifyRequestValidator : AbstractValidator<ApprovalModifyRequest>
{
    public ApprovalModifyRequestValidator()
    {
        RuleFor(x => x.ModifiedResponse)
            .NotEmpty()
            .WithMessage("Modified response is required")
            .MaximumLength(50000)
            .WithMessage("Modified response cannot exceed 50000 characters");
    }
}

/// <summary>
/// Validator for ApprovalRejectRequest DTO
/// </summary>
public class ApprovalRejectRequestValidator : AbstractValidator<ApprovalRejectRequest>
{
    public ApprovalRejectRequestValidator()
    {
        RuleFor(x => x.RejectionReason)
            .NotEmpty()
            .WithMessage("Rejection reason is required")
            .MaximumLength(2000)
            .WithMessage("Rejection reason cannot exceed 2000 characters");
    }
}
