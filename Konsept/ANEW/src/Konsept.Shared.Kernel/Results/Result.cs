namespace Konsept.Shared.Kernel.Results;

/// <summary>
/// Application servislerinin dönüş tipi: başarı durumunda değer, beklenen hatada <see cref="Error"/>.
/// Beklenen iş hataları exception olarak fırlatılmaz (docs/conventions.md, Error handling).
/// </summary>
/// <typeparam name="T">Başarı durumundaki değerin tipi.</typeparam>
public sealed class Result<T>
{
    private readonly T? _value;

    private Result(T? value, Error? error)
    {
        _value = value;
        Error = error;
    }

    /// <summary>Gets a value indicating whether işlem başarılı.</summary>
    public bool IsSuccess => Error is null;

    /// <summary>Gets hata; başarı durumunda <c>null</c>.</summary>
    public Error? Error { get; }

    /// <summary>Gets başarı değeri; hata durumunda okunursa <see cref="InvalidOperationException"/> fırlatır.</summary>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Başarısız bir sonucun değeri okunamaz.");

    /// <summary>Başarılı bir sonuç oluşturur.</summary>
    /// <param name="value">Sonuç değeri.</param>
    /// <returns>Başarılı sonuç.</returns>
    public static Result<T> Success(T value) => new(value, null);

    /// <summary>Hatalı bir sonuç oluşturur.</summary>
    /// <param name="error">Beklenen iş hatası.</param>
    /// <returns>Hatalı sonuç.</returns>
    public static Result<T> Failure(Error error) => new(default, error ?? throw new ArgumentNullException(nameof(error)));
}
