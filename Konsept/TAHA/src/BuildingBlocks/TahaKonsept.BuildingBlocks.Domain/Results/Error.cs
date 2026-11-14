namespace TahaKonsept.BuildingBlocks.Domain.Results;

/// <summary>
/// Servislerin istisna fırlatmak yerine döndürdüğü hata bilgisi.
/// Önemli üyeler: <see cref="Code"/> (makine kodu, ör. <c>concepts.not_found</c>), <see cref="Message"/> (Türkçe, kullanıcıya gösterilebilir),
/// <see cref="Type"/> (HTTP eşlemesi), <see cref="Details"/> (alan bazlı doğrulama hataları).
/// Fabrika metotları: <see cref="Validation(string, string, IReadOnlyDictionary{string, string[]}?)"/>, <see cref="NotFound"/>, <see cref="Conflict"/>,
/// <see cref="Unauthorized"/>, <see cref="Forbidden"/>, <see cref="Failure"/>.
/// </summary>
/// <param name="Code">Makine tarafından okunur hata kodu.</param>
/// <param name="Message">İnsan tarafından okunur mesaj.</param>
/// <param name="Type">Hata kategorisi.</param>
public sealed record Error(string Code, string Message, ErrorType Type)
{
    /// <summary>Hata yok değeri (başarılı sonuçlarda kullanılır).</summary>
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.None);

    /// <summary>Alan adı → hata mesajları (yalnızca doğrulama hatalarında dolu).</summary>
    public IReadOnlyDictionary<string, string[]>? Details { get; init; }

    /// <summary>Doğrulama hatası üretir.</summary>
    /// <param name="code">Hata kodu.</param>
    /// <param name="message">Mesaj.</param>
    /// <param name="details">Alan bazlı hatalar.</param>
    /// <returns>Hata.</returns>
    public static Error Validation(string code, string message, IReadOnlyDictionary<string, string[]>? details = null) =>
        new(code, message, ErrorType.Validation) { Details = details };

    /// <summary>Bulunamadı hatası üretir.</summary>
    /// <param name="code">Hata kodu.</param>
    /// <param name="message">Mesaj.</param>
    /// <returns>Hata.</returns>
    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);

    /// <summary>Çakışma hatası üretir.</summary>
    /// <param name="code">Hata kodu.</param>
    /// <param name="message">Mesaj.</param>
    /// <returns>Hata.</returns>
    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);

    /// <summary>Kimlik doğrulama hatası üretir.</summary>
    /// <param name="code">Hata kodu.</param>
    /// <param name="message">Mesaj.</param>
    /// <returns>Hata.</returns>
    public static Error Unauthorized(string code, string message) => new(code, message, ErrorType.Unauthorized);

    /// <summary>Yetki hatası üretir.</summary>
    /// <param name="code">Hata kodu.</param>
    /// <param name="message">Mesaj.</param>
    /// <returns>Hata.</returns>
    public static Error Forbidden(string code, string message) => new(code, message, ErrorType.Forbidden);

    /// <summary>Genel hata üretir.</summary>
    /// <param name="code">Hata kodu.</param>
    /// <param name="message">Mesaj.</param>
    /// <returns>Hata.</returns>
    public static Error Failure(string code, string message) => new(code, message, ErrorType.Failure);
}
