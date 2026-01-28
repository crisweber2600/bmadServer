using bmadServer.ApiService.DTOs;
using FluentValidation;

namespace bmadServer.ApiService.Validators;

public class TranslationMappingRequestValidator : AbstractValidator<TranslationMappingRequest>
{
    public TranslationMappingRequestValidator()
    {
        RuleFor(x => x.TechnicalTerm)
            .NotEmpty()
            .WithMessage("Technical term is required")
            .MaximumLength(200)
            .WithMessage("Technical term must not exceed 200 characters")
            .Must(x => x == null || x == x.Trim())
            .WithMessage("Technical term must not have leading or trailing whitespace");

        RuleFor(x => x.BusinessTerm)
            .NotEmpty()
            .WithMessage("Business term is required")
            .MaximumLength(500)
            .WithMessage("Business term must not exceed 500 characters");

        RuleFor(x => x.Context)
            .MaximumLength(500)
            .WithMessage("Context must not exceed 500 characters")
            .When(x => x.Context != null);
    }
}
