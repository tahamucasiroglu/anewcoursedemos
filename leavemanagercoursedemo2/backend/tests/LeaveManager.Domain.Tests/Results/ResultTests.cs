using LeaveManager.Domain.Results;

namespace LeaveManager.Domain.Tests.Results;

public class ResultTests
{
    private static readonly Error SampleError = new("BR-2", "Yetersiz bakiye");

    [Fact]
    public void Success_WhenCreated_IsSuccessWithoutError()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Failure_WhenCreated_CarriesRuleCodeAndMessage()
    {
        var result = Result.Failure(SampleError);

        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);
        Assert.Equal("BR-2", result.Error!.Code);
        Assert.Equal("Yetersiz bakiye", result.Error.Message);
    }

    [Fact]
    public void Failure_WithNullError_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => Result.Failure(null!));
        Assert.Throws<ArgumentNullException>(() => Result.Failure<int>(null!));
    }

    [Fact]
    public void SuccessOfT_WhenCreated_ExposesValue()
    {
        var result = Result.Success(5);

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value);
        Assert.Null(result.Error);
    }

    [Fact]
    public void FailureOfT_ReadingValue_ThrowsWithRuleCode()
    {
        var result = Result.Failure<int>(SampleError);

        Assert.True(result.IsFailure);
        var ex = Assert.Throws<InvalidOperationException>(() => result.Value);
        Assert.Contains("BR-2", ex.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("", "mesaj")]
    [InlineData("  ", "mesaj")]
    [InlineData("BR-1", "")]
    [InlineData("BR-1", " ")]
    public void Error_WithBlankCodeOrMessage_Throws(string code, string message)
    {
        Assert.ThrowsAny<ArgumentException>(() => new Error(code, message));
    }
}
