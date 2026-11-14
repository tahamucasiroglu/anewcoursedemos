namespace TahaKonsept.BuildingBlocks.Domain.Results;

/// <summary>
/// Değer taşımayan işlem sonucu. Servisler istisna yerine bunu döner.
/// Önemli üyeler: <see cref="IsSuccess"/>, <see cref="IsFailure"/>, <see cref="Error"/>;
/// fabrikalar: <see cref="Success()"/>, <see cref="Failure(Results.Error)"/>, <see cref="Success{T}(T)"/>, <see cref="Failure{T}(Results.Error)"/>.
/// <see cref="Error"/>'dan örtük dönüşüm vardır: <c>return Error.NotFound(...);</c>
/// </summary>
public class Result
{
    /// <summary>Sonuç oluşturur; başarı ile <see cref="Results.Error.None"/> tutarlı olmalıdır.</summary>
    /// <param name="isSuccess">Başarılı mı.</param>
    /// <param name="error">Hata (başarılıysa <see cref="Results.Error.None"/>).</param>
    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
        {
            throw new ArgumentException("Başarılı sonuç hata taşıyamaz.", nameof(error));
        }

        if (!isSuccess && error == Error.None)
        {
            throw new ArgumentException("Başarısız sonuç bir hata taşımalıdır.", nameof(error));
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>İşlem başarılı mı.</summary>
    public bool IsSuccess { get; }

    /// <summary>İşlem başarısız mı.</summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>Başarısızlık hatası (başarılıysa <see cref="Results.Error.None"/>).</summary>
    public Error Error { get; }

    /// <summary>Başarılı sonuç.</summary>
    /// <returns>Sonuç.</returns>
    public static Result Success() => new(true, Error.None);

    /// <summary>Başarısız sonuç.</summary>
    /// <param name="error">Hata.</param>
    /// <returns>Sonuç.</returns>
    public static Result Failure(Error error) => new(false, error);

    /// <summary>Değerli başarılı sonuç.</summary>
    /// <typeparam name="T">Değer türü.</typeparam>
    /// <param name="value">Değer.</param>
    /// <returns>Sonuç.</returns>
    public static Result<T> Success<T>(T value) => Result<T>.Success(value);

    /// <summary>Değerli başarısız sonuç.</summary>
    /// <typeparam name="T">Değer türü.</typeparam>
    /// <param name="error">Hata.</param>
    /// <returns>Sonuç.</returns>
    public static Result<T> Failure<T>(Error error) => Result<T>.Failure(error);

    /// <summary>Hatadan başarısız sonuca örtük dönüşüm.</summary>
    /// <param name="error">Hata.</param>
    public static implicit operator Result(Error error) => Failure(error);
}
