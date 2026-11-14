using System.Globalization;

namespace TahaKonsept.Agent.Core.Http;

/// <summary>Ajanın çağırdığı API yolları (MIMARI §4–5). Hepsi sunucu taban adresine göre görelidir.</summary>
public static class AgentApiRoutes
{
    /// <summary>Cihaz eşleme başlatma (anonim).</summary>
    public const string DeviceStart = "api/agents/device/start";

    /// <summary>Cihaz token yoklama (anonim).</summary>
    public const string DeviceToken = "api/agents/device/token";

    /// <summary>Token yenileme (anonim, refresh token ile).</summary>
    public const string Refresh = "api/identity/refresh";

    /// <summary>Artefakt indirme adresi.</summary>
    /// <param name="artifactId">Artefakt kimliği.</param>
    /// <returns>Göreli adres.</returns>
    public static string Artifact(Guid artifactId) => "api/agents/artifacts/" + artifactId.ToString("D", CultureInfo.InvariantCulture);
}
