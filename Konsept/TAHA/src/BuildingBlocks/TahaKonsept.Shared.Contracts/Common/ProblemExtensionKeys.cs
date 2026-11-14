namespace TahaKonsept.Shared.Contracts.Common;

/// <summary>
/// API'nin döndürdüğü ProblemDetails gövdesindeki ek alan adları. API yazar, Web/ajan istemcisi okur.
/// </summary>
public static class ProblemExtensionKeys
{
    /// <summary>Makine okunur hata kodu (ör. <c>concepts.not_found</c>).</summary>
    public const string Code = "code";

    /// <summary>Hata kategorisi (<c>Validation</c>, <c>NotFound</c> ...).</summary>
    public const string ErrorType = "errorType";

    /// <summary>Alan adı → hata mesajları sözlüğü.</summary>
    public const string Errors = "errors";

    /// <summary>İzleme kimliği.</summary>
    public const string TraceId = "traceId";
}
