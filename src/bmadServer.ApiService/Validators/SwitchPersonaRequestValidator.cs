using bmadServer.ApiService.DTOs;
using FluentValidation;

namespace bmadServer.ApiService.Validators;

public class SwitchPersonaRequestValidator : AbstractValidator<SwitchPersonaRequest>
{
    private static readonly string[] ValidPersonaTypes = { "Business", "Technical", "Hybrid" };

    public SwitchPersonaRequestValidator()
    {
        RuleFor(x => x.PersonaType)
            .NotEmpty()
            .WithMessage("PersonaType is required")
            .Must(x => ValidPersonaTypes.Contains(x, StringComparer.OrdinalIgnoreCase))
            .WithMessage("PersonaType must be one of: Business, Technical, Hybrid");
    }
}
