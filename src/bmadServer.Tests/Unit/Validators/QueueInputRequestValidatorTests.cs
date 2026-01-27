using bmadServer.ApiService.DTOs.Checkpoints;
using bmadServer.ApiService.Validators.Checkpoints;
using System.Text.Json;
using Xunit;

namespace bmadServer.Tests.Unit.Validators;

public class QueueInputRequestValidatorTests
{
    private readonly QueueInputRequestValidator _validator;

    public QueueInputRequestValidatorTests()
    {
        _validator = new QueueInputRequestValidator();
    }

    [Fact]
    public void Should_Pass_Validation_For_Valid_Request()
    {
        // Arrange
        var content = JsonDocument.Parse("{\"message\": \"test\"}");
        var request = new QueueInputRequest("message", content);

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Should_Fail_When_InputType_Is_Empty()
    {
        // Arrange
        var content = JsonDocument.Parse("{}");
        var request = new QueueInputRequest("", content);

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "InputType");
    }

    [Fact]
    public void Should_Fail_When_InputType_Exceeds_MaxLength()
    {
        // Arrange
        var longType = new string('a', 51);
        var content = JsonDocument.Parse("{}");
        var request = new QueueInputRequest(longType, content);

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "InputType" && e.ErrorMessage.Contains("50 characters"));
    }

    [Fact]
    public void Should_Fail_When_Content_Is_Null()
    {
        // Arrange
        var request = new QueueInputRequest("message", null!);

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Content");
    }
}
