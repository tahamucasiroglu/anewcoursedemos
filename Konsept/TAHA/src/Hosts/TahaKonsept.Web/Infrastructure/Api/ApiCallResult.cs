namespace TahaKonsept.Web.Infrastructure.Api;

/// <summary>
/// MVC'nin API'ye yaptığı bir çağrının sonucu. İstisna fırlatmak yerine durum taşır:
/// <see cref="IsSuccess"/>, HTTP <see cref="StatusCode"/>, başarıda <see cref="Data"/>, hatada API'nin ham gövdesi
/// (<see cref="ErrorBody"/>, çoğunlukla ProblemDetails JSON) ve okunur <see cref="ErrorMessage"/>.
/// </summary>
/// <typeparam name="T">Beklenen cevap türü (bilinmiyorsa <c>JsonElement</c> ile olduğu gibi aktarılır).</typeparam>
public sealed record ApiCallResult<T>
{
    /// <summary>API 2xx döndüyse true.</summary>
    public bool IsSuccess { get; init; }

    /// <summary>HTTP durum kodu (API'ye ulaşılamadıysa 503).</summary>
    public int StatusCode { get; init; }

    /// <summary>Başarılı cevabın gövdesi (204 / boş gövdede varsayılan değer).</summary>
    public T? Data { get; init; }

    /// <summary>Hata cevabının ham gövdesi.</summary>
    public string? ErrorBody { get; init; }

    /// <summary>Hata gövdesinin içerik türü (ör. application/problem+json).</summary>
    public string? ErrorContentType { get; init; }

    /// <summary>Kullanıcıya gösterilebilecek kısa hata mesajı.</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>Başarılı sonuç üretir.</summary>
    /// <param name="data">Cevap verisi.</param>
    /// <param name="statusCode">HTTP durum kodu.</param>
    public static ApiCallResult<T> Success(T? data, int statusCode = 200) =>
        new() { IsSuccess = true, StatusCode = statusCode, Data = data };

    /// <summary>Hatalı sonuç üretir.</summary>
    /// <param name="statusCode">HTTP durum kodu.</param>
    /// <param name="message">Okunur mesaj.</param>
    /// <param name="body">Ham hata gövdesi.</param>
    /// <param name="contentType">Gövdenin içerik türü.</param>
    public static ApiCallResult<T> Failure(int statusCode, string message, string? body = null, string? contentType = null) =>
        new() { IsSuccess = false, StatusCode = statusCode, ErrorMessage = message, ErrorBody = body, ErrorContentType = contentType };

    /// <summary>Hata bilgisini başka bir veri türüne taşır (zincirleme servis çağrıları için).</summary>
    /// <typeparam name="TOther">Hedef sonuç türü.</typeparam>
    public ApiCallResult<TOther> CastFailure<TOther>() =>
        ApiCallResult<TOther>.Failure(StatusCode, ErrorMessage ?? "İstek başarısız.", ErrorBody, ErrorContentType);
}
