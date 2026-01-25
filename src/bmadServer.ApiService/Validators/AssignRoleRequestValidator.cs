using bmadServer.ApiService.Data.Entities;
using bmadServer.ApiService.DTOs;
using FluentValidation;

namespace bmadServer.ApiService.Validators;

/// <summary>
/// Validator for role assignment requests
/// </summary>
public class AssignRoleRequestValidator : AbstractValidator<AssignRoleRequest>
{
    public AssignRoleRequestValidator()
    {
        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("Invalid role value");
    }
}
