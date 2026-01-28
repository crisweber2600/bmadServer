using bmadServer.ApiService.Data.Entities;
using bmadServer.ApiService.DTOs;
using FluentValidation;

namespace bmadServer.ApiService.Validators;

public class UpdatePersonaRequestValidator : AbstractValidator<UpdatePersonaRequest>
{
    public UpdatePersonaRequestValidator()
    {
        RuleFor(x => x.PersonaType)
            .NotNull()
            .WithMessage("PersonaType is required")
            .IsInEnum()
            .WithMessage("PersonaType must be a valid persona type (Business=0, Technical=1, Hybrid=2)");
    }
}
