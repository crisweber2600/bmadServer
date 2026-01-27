using bmadServer.ApiService.DTOs;
using FluentValidation;

namespace bmadServer.ApiService.Validators;

public class AddParticipantRequestValidator : AbstractValidator<AddParticipantRequest>
{
    public AddParticipantRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("UserId is required");

        RuleFor(x => x.Role)
            .NotEmpty()
            .WithMessage("Role is required")
            .Must(BeValidRole)
            .WithMessage("Role must be one of: Owner, Contributor, Observer");
    }

    private bool BeValidRole(string role)
    {
        return role == "Owner" || role == "Contributor" || role == "Observer";
    }
}
