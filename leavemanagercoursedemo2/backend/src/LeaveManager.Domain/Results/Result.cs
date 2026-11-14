namespace LeaveManager.Domain.Results;

/// <summary>
/// Outcome of an operation that can violate a business rule. Rule violations are returned,
/// not thrown (docs/conventions.md, Error handling).
/// </summary>
public class Result
{
    protected Result(Error? error)
    {
        Error = error;
    }

    public bool IsSuccess => Error is null;

    public bool IsFailure => !IsSuccess;

    public Error? Error { get; }

    public static Result Success() => new(null);

    public static Result Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new Result(error);
    }

    public static Result<T> Success<T>(T value) => Result<T>.Success(value);

    public static Result<T> Failure<T>(Error error) => Result<T>.Failure(error);
}

/// <summary>A <see cref="Result"/> that carries a value on success.</summary>
public sealed class Result<T> : Result
{
    private readonly T? _value;

    private Result(T? value, Error? error)
        : base(error)
    {
        _value = value;
    }

    /// <summary>The value; reading it from a failed result is a programming error and throws.</summary>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException($"Başarısız sonucun değeri okunamaz ({Error!.Code}).");

    public static Result<T> Success(T value) => new(value, null);

    public static new Result<T> Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new Result<T>(default, error);
    }
}
