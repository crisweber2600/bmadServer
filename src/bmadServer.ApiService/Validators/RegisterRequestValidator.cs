using bmadServer.ApiService.DTOs;
using FluentValidation;

namespace bmadServer.ApiService.Validators;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain a special character")
            .Matches("[0-9]").WithMessage("Password must contain a number");

        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("Display name is required")
            .MaximumLength(100).WithMessage("Display name must be 100 characters or less");
    }
}
