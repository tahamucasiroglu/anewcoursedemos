using Konsept.Shared.Kernel.Enums;
using Konsept.Shared.Kernel.Results;

namespace Konsept.Web.ApiClient;

/// <summary>
/// Web tarafında API çağrısının beklenen hataları.
/// </summary>
public static class ApiClientErrors
{
    /// <summary>Hata kodu: API'ye ulaşılamadı.</summary>
    public const string UnavailableCode = "api.unavailable";

    /// <summary>Gets API'ye ulaşılamadı.</summary>
    public static Error Unavailable { get; } = new(UnavailableCode, "API erişilemiyor", ErrorType.Unavailable);
}
