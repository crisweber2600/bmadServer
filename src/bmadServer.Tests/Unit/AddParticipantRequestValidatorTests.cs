using bmadServer.ApiService.DTOs;
using bmadServer.ApiService.Validators;
using FluentValidation.TestHelper;
using Xunit;

namespace bmadServer.Tests.Unit;

public class AddParticipantRequestValidatorTests
{
    private readonly AddParticipantRequestValidator _validator;

    public AddParticipantRequestValidatorTests()
    {
        _validator = new AddParticipantRequestValidator();
    }

    [Fact]
    public void Should_HaveError_When_UserId_IsEmpty()
    {
        var request = new AddParticipantRequest
        {
            UserId = Guid.Empty,
            Role = "Contributor"
        };

        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public void Should_HaveError_When_Role_IsEmpty()
    {
        var request = new AddParticipantRequest
        {
            UserId = Guid.NewGuid(),
            Role = string.Empty
        };

        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Role);
    }

    [Theory]
    [InlineData("Owner")]
    [InlineData("Contributor")]
    [InlineData("Observer")]
    public void Should_NotHaveError_When_Role_IsValid(string role)
    {
        var request = new AddParticipantRequest
        {
            UserId = Guid.NewGuid(),
            Role = role
        };

        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.Role);
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("InvalidRole")]
    [InlineData("contributor")] // Case sensitive
    public void Should_HaveError_When_Role_IsInvalid(string role)
    {
        var request = new AddParticipantRequest
        {
            UserId = Guid.NewGuid(),
            Role = role
        };

        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Role);
    }

    [Fact]
    public void Should_NotHaveError_When_Request_IsValid()
    {
        var request = new AddParticipantRequest
        {
            UserId = Guid.NewGuid(),
            Role = "Contributor"
        };

        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
