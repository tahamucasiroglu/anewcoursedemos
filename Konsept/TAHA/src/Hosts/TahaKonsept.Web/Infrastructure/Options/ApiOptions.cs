namespace TahaKonsept.Web.Infrastructure.Options;

/// <summary>
/// MVC → API bağlantı ayarları (appsettings "Api" bölümü).
/// <see cref="BaseUrl"/> API kök adresi (Pi'de http://127.0.0.1:7111), <see cref="TimeoutSeconds"/> istek zaman aşımı,
/// <see cref="RefreshSkewSeconds"/> erişim token'ı bitmeden kaç saniye önce yenileneceği.
/// </summary>
public sealed class ApiOptions
{
    /// <summary>Yapılandırma bölüm adı.</summary>
    public const string SectionName = "Api";

    /// <summary>API kök adresi.</summary>
    public string BaseUrl { get; set; } = "http://127.0.0.1:7111";

    /// <summary>Tek bir API isteğinin zaman aşımı (saniye). LLM çağrıları uzun sürebildiği için varsayılan 120.</summary>
    public int TimeoutSeconds { get; set; } = 120;

    /// <summary>Erişim token'ının bitişine bu kadar saniye kala önceden yenileme yapılır.</summary>
    public int RefreshSkewSeconds { get; set; } = 60;
}
