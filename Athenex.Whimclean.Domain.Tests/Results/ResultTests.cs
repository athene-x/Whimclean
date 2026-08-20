using Athenex.Whimclean.Domain.Results;

namespace Athenex.Whimclean.Domain.Tests.Results;

public sealed class ResultTests
{
    [Fact]
    public void Success_HasNoError()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(Error.None, result.Error);
    }

    [Fact]
    public void Failure_ContainsProvidedError()
    {
        var error = Error.Failure("Failure", "Something failed.");

        var result = Result.Failure(error);

        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public void Failure_WithNoError_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(
            () => Result.Failure(Error.None));
    }

    [Fact]
    public void GenericSuccess_ContainsValue()
    {
        var result = Result<string>.Success("value");

        Assert.True(result.IsSuccess);
        Assert.Equal("value", result.Value);
    }

    [Fact]
    public void GenericSuccess_WithNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => Result<string>.Success(null!));
    }

    [Fact]
    public void GenericFailure_ValueAccessThrowsInvalidOperationException()
    {
        var result = Result<string>.Failure(
            Error.Failure("Failure", "Something failed."));

        Assert.Throws<InvalidOperationException>(() => result.Value);
    }
}
