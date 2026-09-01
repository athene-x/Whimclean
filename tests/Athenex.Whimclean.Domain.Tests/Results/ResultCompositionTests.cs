using Athenex.Whimclean.Domain.Results;

namespace Athenex.Whimclean.Domain.Tests.Results;

public sealed class ResultCompositionTests
{
    private static readonly Error TestError =
        Error.Failure("Test.Failure", "Operation failed.");

    [Fact]
    public void Match_OnSuccessfulResult_UsesSuccessBranch()
    {
        var outcome = Result.Success().Match(
            () => "success",
            error => error.Code);

        Assert.Equal("success", outcome);
    }

    [Fact]
    public void Match_OnFailedResult_UsesFailureBranch()
    {
        var outcome = Result.Failure(TestError).Match(
            () => "success",
            error => error.Code);

        Assert.Equal(TestError.Code, outcome);
    }

    [Fact]
    public void Match_OnSuccessfulTypedResult_ReceivesValue()
    {
        var outcome = Result<int>.Success(21).Match(
            value => value * 2,
            _ => 0);

        Assert.Equal(42, outcome);
    }

    [Fact]
    public void ImplicitConversion_FromValue_CreatesSuccess()
    {
        Result<int> result = 42;

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void ImplicitConversion_FromError_CreatesFailure()
    {
        Result<int> typedResult = TestError;
        Result result = TestError;

        Assert.True(typedResult.IsFailure);
        Assert.True(result.IsFailure);
        Assert.Equal(TestError, typedResult.Error);
        Assert.Equal(TestError, result.Error);
    }

    [Fact]
    public void Map_OnSuccess_TransformsValue()
    {
        var result = Result<int>.Success(21).Map(value => value * 2);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Map_OnFailure_PropagatesError()
    {
        var result = Result<int>.Failure(TestError).Map(value => value * 2);

        Assert.True(result.IsFailure);
        Assert.Equal(TestError, result.Error);
    }

    [Fact]
    public void Bind_OnSuccess_ChainsResult()
    {
        var result = Result<int>.Success(21)
            .Bind(value => Result<string>.Success(value.ToString()));

        Assert.True(result.IsSuccess);
        Assert.Equal("21", result.Value);
    }

    [Fact]
    public void Bind_OnSuccess_CanFail()
    {
        var result = Result<int>.Success(21)
            .Bind(_ => Result<string>.Failure(TestError));

        Assert.True(result.IsFailure);
        Assert.Equal(TestError, result.Error);
    }

    [Fact]
    public void Bind_OnFailure_SkipsBinder()
    {
        var binderCalled = false;

        var result = Result<int>.Failure(TestError).Bind(value =>
        {
            binderCalled = true;
            return Result<string>.Success(value.ToString());
        });

        Assert.False(binderCalled);
        Assert.Equal(TestError, result.Error);
    }

    [Theory]
    [InlineData(ErrorType.Unauthorized)]
    [InlineData(ErrorType.Forbidden)]
    public void Error_AccessControlFactories_UseMatchingType(ErrorType type)
    {
        var error = type == ErrorType.Unauthorized
            ? Error.Unauthorized("Test.Unauthorized", "Authentication required.")
            : Error.Forbidden("Test.Forbidden", "Access denied.");

        Assert.Equal(type, error.Type);
    }
}
