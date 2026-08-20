using Athenex.Whimclean.Domain.Results;

namespace Athenex.Whimclean.Domain.Tests.Results;

public sealed class ErrorTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Factory_WithInvalidCode_ThrowsArgumentException(string? code)
    {
        Assert.ThrowsAny<ArgumentException>(
            () => Error.Validation(code!, "Description"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Factory_WithInvalidDescription_ThrowsArgumentException(
        string? description)
    {
        Assert.ThrowsAny<ArgumentException>(
            () => Error.Validation("Code", description!));
    }

    [Theory]
    [InlineData(ErrorType.Failure)]
    [InlineData(ErrorType.Validation)]
    [InlineData(ErrorType.NotFound)]
    [InlineData(ErrorType.Conflict)]
    public void Factory_CreatesExpectedErrorType(ErrorType expectedType)
    {
        var error = expectedType switch
        {
            ErrorType.Failure => Error.Failure("Code", "Description"),
            ErrorType.Validation => Error.Validation("Code", "Description"),
            ErrorType.NotFound => Error.NotFound("Code", "Description"),
            ErrorType.Conflict => Error.Conflict("Code", "Description"),
            _ => throw new ArgumentOutOfRangeException(nameof(expectedType))
        };

        Assert.Equal(expectedType, error.Type);
        Assert.Equal("Code", error.Code);
        Assert.Equal("Description", error.Description);
    }
}
