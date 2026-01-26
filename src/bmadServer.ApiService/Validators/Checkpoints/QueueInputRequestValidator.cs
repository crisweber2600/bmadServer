using bmadServer.ApiService.DTOs.Checkpoints;
using FluentValidation;

namespace bmadServer.ApiService.Validators.Checkpoints;

public class QueueInputRequestValidator : AbstractValidator<QueueInputRequest>
{
    public QueueInputRequestValidator()
    {
        RuleFor(x => x.InputType)
            .NotEmpty().WithMessage("InputType is required")
            .MaximumLength(50).WithMessage("InputType cannot exceed 50 characters");

        RuleFor(x => x.Content)
            .NotNull().WithMessage("Content is required");
    }
}
