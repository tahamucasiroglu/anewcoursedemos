namespace TahaKonsept.BuildingBlocks.Domain.Results;

/// <summary>
/// Değer taşıyan işlem sonucu. <see cref="Value"/> yalnızca başarılıysa okunabilir.
/// Hem değerden hem <see cref="Results.Error"/>'dan örtük dönüşüm vardır:
/// <c>return dto;</c> veya <c>return Error.NotFound("x.not_found", "Bulunamadı");</c>
/// </summary>
/// <typeparam name="T">Değer türü.</typeparam>
public sealed class Result<T> : Result
{
    private readonly T? _value;

    private Result(T? value, bool isSuccess, Error error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    /// <summary>Başarılı sonucun değeri; başarısız sonuçta erişim <see cref="InvalidOperationException"/> fırlatır.</summary>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException($"Başarısız sonucun değeri okunamaz ({Error.Code}).");

    /// <summary>Başarılı sonuç üretir.</summary>
    /// <param name="value">Değer.</param>
    /// <returns>Sonuç.</returns>
    public static Result<T> Success(T value) => new(value, true, Error.None);

    /// <summary>Başarısız sonuç üretir.</summary>
    /// <param name="error">Hata.</param>
    /// <returns>Sonuç.</returns>
    public static new Result<T> Failure(Error error) => new(default, false, error);

    /// <summary>Değerden başarılı sonuca örtük dönüşüm.</summary>
    /// <param name="value">Değer.</param>
    public static implicit operator Result<T>(T value) => Success(value);

    /// <summary>Hatadan başarısız sonuca örtük dönüşüm.</summary>
    /// <param name="error">Hata.</param>
    public static implicit operator Result<T>(Error error) => Failure(error);

    /// <summary>Başarılıysa değeri dönüştürür, değilse hatayı aynen taşır.</summary>
    /// <typeparam name="TOut">Hedef tür.</typeparam>
    /// <param name="map">Dönüştürücü.</param>
    /// <returns>Yeni sonuç.</returns>
    public Result<TOut> Map<TOut>(Func<T, TOut> map) =>
        IsSuccess ? Result<TOut>.Success(map(Value)) : Result<TOut>.Failure(Error);
}
