namespace LeaveManager.Domain.Results;

/// <summary>
/// A business-rule violation: <see cref="Code"/> carries the rule number (e.g. "BR-2"),
/// <see cref="Message"/> the Turkish text shown to the user (docs/conventions.md, Error handling).
/// </summary>
public sealed record Error
{
    public Error(string code, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        Code = code;
        Message = message;
    }

    public string Code { get; }

    public string Message { get; }
}
